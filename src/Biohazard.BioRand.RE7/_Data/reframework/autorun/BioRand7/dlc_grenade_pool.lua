local Pool = {}
Pool.__index = Pool
local LISTS = { "Throwable01List", "Throwable02List", "Throwable03List" }

function Pool.new(game, root)
    return setmetatable({ game = game, root = root }, Pool)
end

function Pool:reset()
    -- Save/load hooks can run outside UpdateBehavior. Defer native destruction.
    self.reset_pending = true
end

function Pool:owned()
    return self.object and self.game:valid(self.object) and self.manager
        and self.manager:call("get_Valid") and self.game:singleton("app.CH8ShellManager") == self.manager
end

function Pool:clear()
    self.ready = false
    if self.object and self.game:valid(self.object) then
        self.manager = self.manager or self.game:component(self.object, "app.CH8ShellManager")
        -- A foreign/DLC singleton must never be cleared by our object's destructor.
        local current = self.game:singleton("app.CH8ShellManager")
        if current and current ~= self.manager then return false, "foreign shell manager" end
        if not self.destroy_pending then
            self.game:method("via.GameObject", "destroy(via.GameObject)"):call(nil, self.object)
            self.destroy_pending = true
        end
        return false, "disposing shell manager"
    end
    if self.object then self.object:release() end
    self.object, self.manager, self.player, self.since = nil, nil, nil, nil
    self.destroy_pending, self.reset_pending = nil, nil
    return true
end

function Pool:available(kind)
    if not self.ready or not self:owned() then return false end
    local field = LISTS[kind + 1]
    local list = field and self.manager:get_field(field)
    return list ~= nil and list:get_field("UnusedCount") > 0
end

function Pool:update(player)
    if self.reset_pending or self.destroy_pending or self.player and self.player ~= player or not player then
        local cleared, reason = self:clear()
        if not cleared or not player then return false, reason end
    end
    local gm = self.game:singleton("app.GameManager")
    if not gm or gm:call("get_IsSceneLoading") then self.since = nil; return false, "scene loading" end
    if gm:call("get_IsPause") then self.since = nil; return false, "paused" end
    self.since = self.since or os.clock()
    local current = self.game:singleton("app.CH8ShellManager")
    if self.object then
        if not self.game:valid(self.object) then self:clear(); return false, "shell manager unloaded" end
        self.manager = self.game:component(self.object, "app.CH8ShellManager")
        if current and current ~= self.manager then self.ready = false; return false, "foreign shell manager" end
        if current == self.manager and self.manager then
            local ready = true
            for _, field in ipairs(LISTS) do
                local list = self.manager:get_field(field)
                local elements = list and list:get_field("List")
                if not elements or elements:call("get_Count") ~= 6 then ready = false end
            end
            if ready then self.ready = true; return true end
        end
        assert(os.clock() - self.since < 10.0, "Grenade shell pool initialization timed out")
        return false, "initializing shell pools"
    end
    if current then return false, "foreign shell manager" end
    if not self.prefab then
        self.prefab = assert(sdk.create_instance("via.Prefab", true), "Cannot create grenade pool prefab"):add_ref()
        self.prefab:call("set_Standby", false)
        self.prefab:call("set_Path", self.root .. "/GrenadeShellManager.pfb")
        self.prefab:call("set_Standby", true)
        self.since = os.clock()
    end
    if not self.prefab:call("get_Ready") then
        self.since = self.since or os.clock()
        assert(os.clock() - self.since < 10.0, "Grenade shell prefab loading timed out")
        return false, "loading shell prefab"
    end
    assert(self.prefab:call("get_Valid"), "Grenade shell prefab is invalid")
    self.object = assert(self.prefab:call("instantiate(via.vec3)", player:call("get_Transform"):call("get_Position")),
        "Cannot instantiate grenade shell manager"):add_ref()
    self.player, self.since = player, os.clock()
    -- Parent lifetime to the active player. The engine runs awake/start/update/destroy.
    self.object:call("get_Transform"):call("set_Parent", player:call("get_Transform"))
    return false, "starting shell manager"
end

return Pool
