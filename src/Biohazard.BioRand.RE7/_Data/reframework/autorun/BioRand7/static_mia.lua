local StaticMia = {}
StaticMia.__index = StaticMia

local NAME_PREFIX = "BioRandExtraEnemyStatic_Em2000_"
local EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

local function round(value)
    local lower = math.floor(value)
    local fraction = value - lower
    if fraction > 0.5 or (fraction == 0.5 and lower % 2 ~= 0) then
        return lower + 1
    end
    return lower
end

function StaticMia.new(context)
    return setmetatable({ context = context, killed = {}, suppressed = {}, dying = {} }, StaticMia)
end

function StaticMia:is_static(game_object)
    return game_object ~= nil and game_object:call("get_Name"):sub(1, #NAME_PREFIX) == NAME_PREFIX
end

function StaticMia:controller_game_object(controller)
    return controller:call("get_GameObject")
end

function StaticMia:fallback_key(game_object)
    local folder = game_object:call("get_Folder")
    local folder_path = folder == nil and "" or folder:call("get_Path")
    local position = game_object:call("get_Transform"):call("get_Position")
    return ("fallback:%s:%s:%d:%d:%d"):format(
        folder_path,
        game_object:call("get_Name"),
        round(position.x * 100),
        round(position.y * 100),
        round(position.z * 100))
end

function StaticMia:guid_key(controller, field, prefix)
    if controller == nil then return nil end
    local value = controller:get_field(field)
    if value == nil then return nil end
    local guid = self.context.game:guid_string(value)
    if guid ~= EMPTY_GUID then return prefix .. guid end
end

function StaticMia:restore(save, data)
    local game_object = save:call("get_GameObject")
    if data == nil or not self:is_static(game_object) then return end
    local controller = self.context.game:component(game_object, "app.EnemyActionController")
    for _, key in ipairs(self:keys(controller, game_object)) do
        self.killed[key] = data:get_field("Health") <= 0 or nil
    end
end

function StaticMia:save(save, data)
    local game_object = save:call("get_GameObject")
    if data == nil or not self:is_killed(nil, game_object) then return end
    -- Persist the suppression through the game's own per-enemy save record.
    data:set_field("Health", 0)
    data:set_field("IsUpdate", false)
    data:set_field("IsDraw", false)
end

function StaticMia:keys(controller, game_object)
    controller = controller or self.context.game:component(game_object, "app.EnemyActionController")
    local keys = {}
    keys[#keys + 1] = self:guid_key(controller, "SpawnerGuid", "guid:spawner:")
    keys[#keys + 1] = self:guid_key(controller, "ActualUsingGuid", "guid:actual:")
    keys[#keys + 1] = self:fallback_key(game_object)
    return keys
end

function StaticMia:is_killed(controller, game_object)
    if next(self.killed) == nil then return false end
    if not self:is_static(game_object) then
        return false
    end
    controller = controller or self.context.game:component(game_object, "app.EnemyActionController")
    return self.killed[self:guid_key(controller, "SpawnerGuid", "guid:spawner:")]
        or self.killed[self:guid_key(controller, "ActualUsingGuid", "guid:actual:")]
        or self.killed[self:fallback_key(game_object)] or false
end

function StaticMia:remember(controller, game_object)
    if not self:is_static(game_object) then
        return false
    end
    for _, key in ipairs(self:keys(controller, game_object)) do
        self.killed[key] = true
    end
    return true
end

function StaticMia:suppress(controller, game_object)
    if not self:is_killed(controller, game_object) then
        return false
    end
    self.context.game:method("app.Util", "setActive(via.GameObject, System.Boolean, System.Boolean)")
        :call(nil, game_object, false, false)
    self.suppressed[self.context.game:address(game_object)] = true
    return true
end

function StaticMia:begin_death(controller, game_object)
    if self:remember(controller, game_object) then
        self.dying[self.context.game:address(game_object)] = true
    end
end

function StaticMia:finish_death(game_object)
    if game_object ~= nil then self.dying[self.context.game:address(game_object)] = nil end
end

function StaticMia:install()
    local game = self.context.game
    local function suppress(args)
        if next(self.killed) == nil then return end
        local controller = game:object(args[2])
        if self:suppress(controller, self:controller_game_object(controller)) then
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
    end
    game:hook("app.Em2000.Em2000ActionController", "reactivate()", suppress)
    game:hook("app.Em2000.Em2000ActionController", "doStart()", suppress)
    game:hook("app.Em2000.Em2000ActionController", "doUpdate()", function(args)
        if next(self.killed) == nil then return end
        local controller = game:object(args[2])
        local game_object = self:controller_game_object(controller)
        -- Keep the native death animation running. Reactivation and loading a
        -- dead save still suppress immediately so she cannot fight again.
        if self.dying[game:address(game_object)] then return end
        return suppress(args)
    end)
    game:hook("app.Em2000Order", "loadData(app.EnemyStatus.EnemySaveDataClass)", function(args)
        self:restore(game:object(args[2]), game:object(args[3]))
    end)
    -- Hook the save target: the native caller can inline EnemySave's accessors.
    game:hook("app.Em2000Order", "saveData(app.EnemyStatus.EnemySaveDataClass)", function(args)
        local storage = thread.get_hook_storage()
        storage.biorand_enemy_save = game:object(args[2])
        storage.biorand_enemy_data = game:object(args[3])
    end, function(retval)
        local storage = thread.get_hook_storage()
        self:save(storage.biorand_enemy_save, storage.biorand_enemy_data)
        return retval
    end)
end

function StaticMia:reset()
    self.killed = {}
    self.suppressed = {}
    self.dying = {}
end

return StaticMia
