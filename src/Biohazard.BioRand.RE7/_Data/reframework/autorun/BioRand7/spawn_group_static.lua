local Static = {}
Static.__index = Static

-- Append our state to the native extensible OtherInt array, preserving its prefix.
-- 0 = waiting, 1 = active, 2 = suspended, 3 = completed.
local MAGIC = 0x42525347

local function read_state(data)
    local values = data and data:get_field("OtherInt")
    local count = values and values:get_size() or 0
    if count >= 2 and values:get_element(count - 2) == MAGIC then
        local state = values:get_element(count - 1)
        if state >= 0 and state <= 3 then return state end
    end
end

local function write_state(save, data, state)
    local values = data:get_field("OtherInt")
    local count = values and values:get_size() or 0
    if count < 2 or values:get_element(count - 2) ~= MAGIC then
        local extended = sdk.create_managed_array("System.Int32", count + 2)
        for index = 0, count - 1 do extended:set_element(index, values:get_element(index)) end
        extended:set_element(count, MAGIC)
        values, count = extended, count + 2
    end
    values:set_element(count - 1, state)
    save:set_field("OtherInt", values)
    data:set_field("OtherInt", values)
end

function Static.new(context)
    return setmetatable({ context = context, game = context.game, wanted = {}, objects = {}, states = {} }, Static)
end

function Static:identity(save)
    if save == nil then return nil end
    local guid = self.game:guid_string(save:get_field("SaveGUID"))
    if self.wanted[guid] then return guid end
end

function Static:reset(groups)
    self.wanted, self.objects, self.states = {}, {}, {}
    for _, group in ipairs(groups or {}) do
        for _, member in ipairs(group.members) do
            if member.kind == "static" then self.wanted[member.runtimeGuid] = member end
        end
    end
    if next(self.wanted) ~= nil then self:install() end
end

function Static:save(save, data)
    local guid = self:identity(save)
    if guid == nil or data == nil then return end
    local state = self.states[guid] or read_state(data) or 0
    write_state(save, data, state)
end

function Static:load(save, data)
    local guid = self:identity(save)
    if guid == nil or data == nil then return end
    self.states[guid] = read_state(data) or (data:get_field("IsUpdate") and 1 or 0)
end

function Static:install()
    if self.installed then return end
    self.installed = true
    for _, type_name in ipairs({ "app.OtherObjectSave", "app.Em3300.Em3300Save" }) do
        self.game:hook(type_name, "setOtherObjectSaveData(app.OtherObjectSave.OtherObjectSaveDataClass)", function(args)
            self:load(self.game:object(args[2]), self.game:object(args[3]))
        end)
        self.game:hook(type_name, "getOtherObjectSaveData()", function(args)
            local storage = thread.get_hook_storage()
            storage.biorand_group_saves = storage.biorand_group_saves or {}
            table.insert(storage.biorand_group_saves, self.game:object(args[2]))
        end, function(retval)
            local saves = thread.get_hook_storage().biorand_group_saves
            self:save(table.remove(saves), self.game:object(retval))
            return retval
        end)
    end
end

function Static:refresh()
    self.objects = {}
    if next(self.wanted) == nil then return end
    local scene = self.game:method("via.SceneManager", "get_CurrentScene()"):call(nil)
    if scene == nil then return end
    self.types = self.types or { sdk.typeof("app.OtherObjectSave"), sdk.typeof("app.Em3300.Em3300Save") }
    for _, type_info in ipairs(self.types) do
        local saves = scene:call("findComponents(System.Type)", type_info)
        if saves ~= nil then
            for index = 0, saves:get_size() - 1 do
                local save = saves:get_element(index)
                local guid = self:identity(save)
                if guid ~= nil and self.game:valid(save:call("get_GameObject")) then
                    self.objects[guid] = save
                    if self.states[guid] == nil then
                        self.states[guid] = read_state(save:get_field("OtherObjectSaveData")) or 0
                    end
                end
            end
        end
    end
end

function Static:state(member)
    local save = self.objects[member.runtimeGuid]
    local go = save and save:call("get_GameObject")
    if not self.game:valid(go) then return nil end
    -- Mia's own save record remains authoritative for death and health.
    local enemy_save = self.game:component(go, "app.EnemySave")
    local data = enemy_save and enemy_save:get_field("SaveData")
    local mia = self.context.features and self.context.features.static_mia
    if (data ~= nil and data:get_field("MaxHealth") > 0 and data:get_field("Health") <= 0)
        or (mia ~= nil and mia:is_killed(nil, go)) then
        self:set_state(member.runtimeGuid, 3)
    end
    return self.states[member.runtimeGuid] or 0, go
end

function Static:set_state(guid, state)
    if self.states[guid] == state then return end
    self.states[guid] = state
    local save = self.objects[guid]
    if save == nil then return end
    local data = save:get_field("OtherObjectSaveData")
    if data ~= nil then write_state(save, data, state) end
end

function Static:complete(go)
    if next(self.wanted) == nil then return end
    local save = self.game:component(go, "app.OtherObjectSave")
        or self.game:component(go, "app.Em3300.Em3300Save")
    local guid = self:identity(save)
    if guid == nil then return end
    self.objects[guid] = save
    self:set_state(guid, 3)
end

function Static:apply(member, desired)
    local state, go = self:state(member)
    if state == nil then return end
    if state == 3 then
        -- Keep normal Mia death animations; never reactivate a completed actor.
        local mia = self.context.features and self.context.features.static_mia
        if desired ~= "despawn" and mia ~= nil and mia.dying[self.game:address(go)] then return end
    elseif desired == "despawn" then
        self:set_state(member.runtimeGuid, 3)
        local mia = self.context.features and self.context.features.static_mia
        if mia ~= nil then mia:remember(nil, go) end
    elseif desired == "active" then
        self:set_state(member.runtimeGuid, 1)
    elseif state ~= 0 then
        self:set_state(member.runtimeGuid, 2)
    end
    local active = desired == "active" and self.states[member.runtimeGuid] ~= 3
    if go:call("get_UpdateSelf") ~= active or go:call("get_DrawSelf") ~= active then
        self.game:method("app.Util", "setActive(via.GameObject, System.Boolean, System.Boolean)")
            :call(nil, go, active, active)
    end
end

return Static
