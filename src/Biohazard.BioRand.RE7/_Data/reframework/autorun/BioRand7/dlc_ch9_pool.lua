local Pool = {}
Pool.__index = Pool
local LISTS = {
    { field = "ThrowingWp1500PrefabList", count = 2, kind = 4 },
    { field = "ThrowingWp1800PrefabList", count = 10, kind = 5 },
    { field = "LiquidBombList", count = 5, kind = 6 },
    { field = "ThrowingWp0000PrefabList", count = 2 },
}
local PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }

-- The caller gates campaign support; constructing this module never grants items.
function Pool.new(game, root)
    return setmetatable({ game = game, root = root }, Pool)
end

function Pool:reset()
    self.ready = false
    self.reset_pending = true
end

function Pool:owned()
    return self.object and self.game:valid(self.object) and self.manager
        and self.manager:call("get_Valid") and self.manager:call("get_GameObject") == self.object
        and self.game:singleton("app.CH9ShellManager") == self.manager
end

function Pool:clear()
    self.ready = false
    if self.object and self.game:valid(self.object) then
        self.manager = self.manager or self.game:component(self.object, "app.CH9ShellManager")
        local current = self.game:singleton("app.CH9ShellManager")
        if current and current ~= self.manager then return false, "foreign CH9 shell manager" end
        if not self.destroy_pending then
            self.game:method("via.GameObject", "destroy(via.GameObject)"):call(nil, self.object)
            self.destroy_pending = true
        end
        return false, "disposing CH9 shell manager"
    end
    if self.object then self.object:release() end
    self.object, self.manager, self.player, self.since = nil, nil, nil, nil
    self.destroy_pending, self.reset_pending = nil, nil
    return true
end

function Pool:available(kind)
    if not self.ready or not self:owned() then return false end
    for _, entry in ipairs(LISTS) do
        if entry.kind ~= nil and entry.kind == kind then
            local list = self.manager:get_field(entry.field)
            return list ~= nil and list:get_field("UnusedCount") > 0
        end
    end
    return false
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
    local current = self.game:singleton("app.CH9ShellManager")
    if self.object then
        if not self.game:valid(self.object) then self:clear(); return false, "CH9 shell manager unloaded" end
        self.manager = self.game:component(self.object, "app.CH9ShellManager")
        self.ready = false
        if current and current ~= self.manager then return false, "foreign CH9 shell manager" end
        if current == self.manager and self.manager then
            local ready = true
            for _, entry in ipairs(LISTS) do
                local list = self.manager:get_field(entry.field)
                local elements = list and list:get_field("List")
                if not elements or elements:call("get_Count") ~= entry.count then ready = false end
            end
            if ready then self.ready = true; return true end
        end
        assert(os.clock() - self.since < 10.0, "CH9 shell pool initialization timed out")
        return false, "initializing CH9 shell pools"
    end
    if current then return false, "foreign CH9 shell manager" end
    if not self.prefab then
        self.prefab = assert(sdk.create_instance("via.Prefab", true), "Cannot create CH9 pool prefab"):add_ref()
        self.prefab:call("set_Standby", false)
        self.prefab:call("set_Path", self.root .. "/CH9ShellManager.pfb")
        self.prefab:call("set_Standby", true)
        self.since = os.clock()
    end
    if not self.prefab:call("get_Ready") then
        assert(os.clock() - self.since < 10.0, "CH9 shell prefab loading timed out")
        return false, "loading CH9 shell prefab"
    end
    assert(self.prefab:call("get_Valid"), "CH9 shell prefab is invalid")
    self.object = assert(self.prefab:call("instantiate(via.vec3)", player:call("get_Transform"):call("get_Position")),
        "Cannot instantiate CH9 shell manager"):add_ref()
    self.player, self.since = player, os.clock()
    -- Native shells are children of this object. Parenting it to the player moves lodged spears too.
    -- Keep a stationary root and retire it explicitly on player replacement or save/load reset.
    return false, "starting CH9 shell manager"
end

function Pool:skip_spear_tutorial(interact, inventory, weapon_object)
    if not interact or not self.ready or not self:owned() or self.reset_pending or self.destroy_pending then return false end
    local player = self.game:player()
    if not player or player ~= self.player or not PLAYERS[player:call("get_Name")] then return false end
    local flow = self.game:chapter()
    if not flow or flow < 0 or flow > 13 or self.game:singleton("app.CH9TutorialManager") then return false end
    if not inventory or inventory ~= self.game:component(player, "app.Inventory") or not weapon_object then return false end
    local weapon = self.game:component(weapon_object, "app.Weapon")
    local item = self.game:component(weapon_object, "app.Item")
    if not weapon or weapon:get_field("WeaponID") ~= 65 or not item or item:get_field("ItemDataID") ~= "CH9_WP004" then return false end
    local items = self.game:singleton("app.ItemManager")
    local data = items and items:call("findItemData", "CH9_WP004")
    local prefab = data and data:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    if not path or path:lower() ~= (self.root .. "/CH9_WP004/Item.pfb"):lower() then return false end
    for entry in self.game:list(self.manager:get_field("ThrowingWp1800PrefabList"):get_field("List")) do
        local shell = entry:get_field("Behavior")
        if shell and shell:get_field("Interact") == interact then return true end
    end
    return false
end

function Pool:owns_campaign_stake(shell)
    if not shell or not self.ready or not self:owned() or self.reset_pending or self.destroy_pending then return false end
    local player = self.game:player()
    if not player or player ~= self.player or not PLAYERS[player:call("get_Name")] then return false end
    local flow = self.game:chapter()
    if not flow or flow < 0 or flow > 13 or self.game:singleton("app.CH9TutorialManager") then return false end
    local items = self.game:singleton("app.ItemManager")
    local data = items and items:call("findItemData", "CH9_WP005")
    local prefab = data and data:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    if not path or path:lower() ~= (self.root .. "/CH9_WP005/Item.pfb"):lower() then return false end
    local list = self.manager:get_field("LiquidBombList")
    if not list then return false end
    for entry in self.game:list(list:get_field("List")) do
        if entry:get_field("Behavior") == shell then return true end
    end
    return false
end

function Pool:install()
    if self.installed then return end
    self.installed = true
    self.game:hook("app.CH9InteractWeapon", "equipWeapon(app.Inventory, app.EquipManager, via.GameObject, System.Boolean)",
        function(args)
            if self:skip_spear_tutorial(self.game:object(args[2]), self.game:object(args[3]), self.game:object(args[5])) then
                -- WeaponID 65's entire native branch opens HarpoonChange; it does not equip.
                -- Leave getWeapon/insertInventry/successInteract and native pool recycling intact.
                return sdk.PreHookResult.SKIP_ORIGINAL
            end
        end, function(ret) return ret end)
    self.game:hook("app.CH9InstallationWp1900", "onSuccessInteractCh9", function(args)
        -- This callback only closes tutorial 5. Base recovery and stock transfer stay native.
        if self:owns_campaign_stake(self.game:object(args[2])) then return sdk.PreHookResult.SKIP_ORIGINAL end
    end, function(ret) return ret end)
    self.game:hook("app.CH9InstallationWp1900", "callSE", function(args)
        local trigger = sdk.to_int64(args[3]) & 0xffffffff
        if trigger ~= 0x73ce5ae3 then return end
        local shell = self.game:object(args[2])
        if not self:owns_campaign_stake(shell) then return end
        -- The CH9 override closes a missing tutorial before sound, aborting Bomb.doUpdate
        -- after damage registration but before Wait -> Explosion. Preserve its water sound.
        if shell:get_field("InstallationType") == 2 then trigger = 0xe350a1fc end
        self.game:method("app.Bomb", "callSE"):call(shell, trigger)
        return sdk.PreHookResult.SKIP_ORIGINAL
    end, function(ret) return ret end)
end

return Pool
