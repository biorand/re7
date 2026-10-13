local Weapons = {}
Weapons.__index = Weapons
local IDS = { "CKnife", "Handgun_Albert_C", "Shotgun_Albert", "NumaItem072", "CH9_WP002", "CH9_WP000", "CH9_WP001", "CH9_WP006",
    "Grenadebomb", "Thermatebomb", "Stangrenadebomb", "CH9_WP003", "CH9_WP004", "CH9_WP005" }

function Weapons.new(context)
    return setmetatable({ context = context, requested = {}, next_check = 0 }, Weapons)
end

function Weapons:enabled()
    return not self.context.config:get("base-game-only", true)
        and self.context.config:get("dlc-campaign-weapons", false)
        and self.context.config:get("allow-dlc-items", false)
end

function Weapons:install()
    if self.player_adapter then return end
    self.player_adapter = require("BioRand7/dlc_weapon_player").new(self.context.game,
        function() return self:enabled() and not self.adapter_failed end, "BioRand/DlcWeapons")
    self.player_adapter:install()
    self.gauntlet_adapter = require("BioRand7/dlc_gauntlet").new(self.context.game,
        function() return self:enabled() and not self.gauntlet_failed end, "BioRand/DlcWeapons")
    self.gauntlet_adapter:install()
    self.grenade_adapter = require("BioRand7/dlc_grenade").new(self.context.game,
        function() return self:enabled() and not self.grenade_failed end, "BioRand/DlcWeapons")
    self.grenade_adapter:install()
    self.ch9_adapter = require("BioRand7/dlc_ch9_throwable").new(self.context.game,
        function() return self:enabled() and not self.ch9_failed end, "BioRand/DlcWeapons")
    self.ch9_adapter:install()
    self.ch9_item_adapter = require("BioRand7/dlc_ch9_item").new(self.context.game,
        function() return self:enabled() and not self.ch9_failed and not self.ch9_item_failed end,
        "BioRand/DlcWeapons", self.ch9_adapter.pool)
    self.ch9_item_adapter:install()
end

function Weapons:reset()
    if self.player_adapter then self.player_adapter:reset() end
    if self.gauntlet_adapter then self.gauntlet_adapter:reset() end
    if self.grenade_adapter then self.grenade_adapter:reset(); self.grenade_adapter.error = nil end
    if self.ch9_adapter then self.ch9_adapter:reset(); self.ch9_adapter.error = nil end
    if self.ch9_item_adapter then self.ch9_item_adapter:reset(); self.ch9_item_adapter.error = nil end
    self.adapter_failed = false
    self.gauntlet_failed = false
    self.grenade_failed = false
    self.ch9_failed = false
    self.ch9_item_failed = false
    self.requested, self.next_check, self.finished = {}, 0, false
    self.pending_add, self.grant_status = nil, nil
end

function Weapons:on_config_changed() self:reset() end

function Weapons:request_add_to_item_box()
    self.pending_add = true
    self.grant_status = "Queued"
end

function Weapons:process_item_box_request()
    if not self.pending_add then return end
    self.pending_add = nil
    local added, existing = 0, 0
    local context = self.context
    local ok, message = pcall(function()
        if not self:enabled() then
            error("Campaign DLC weapons are disabled", 0)
        end
        local game = context.game
        local player = game:player()
        if not player or player:call("get_Name") ~= "Pl0000" then
            error("A loaded Ethan campaign is required", 0)
        end
        local inventory = game:component(player, "app.Inventory")
        local box = inventory and inventory:call("get_ItemBoxData")
        if not box then error("Inventory/item box is not ready", 0) end
        local manager = game:singleton("app.ItemManager")
        local missing = {}
        -- Validate the whole adapter set before making any inventory changes.
        for _, id in ipairs(IDS) do
            local data = manager and manager:call("findItemData", id)
            local prefab = data and data:get_field("ItemPrefab")
            local path = prefab and prefab:call("get_Path")
            if not path or path:lower() ~= ("BioRand/DlcWeapons/" .. id .. "/Item.pfb"):lower() then
                error("Campaign adapter is not loaded: " .. id, 0)
            end
            if not prefab:call("get_Ready") then error("Prefab is not ready: " .. id, 0) end
            if inventory:call("hasItemIncludeItemBox", id, false) then
                existing = existing + 1
            else
                missing[#missing + 1] = id
            end
        end
        for _, id in ipairs(missing) do
            if not box:call("addItem(System.String, System.Int32, app.WeaponGun.WeaponGunSaveData)", id, 1, nil) then
                error("Item box refused " .. id, 0)
            end
            added = added + 1
        end
    end)
    if ok then
        self.grant_status = ("Added %d to item box; %d already owned"):format(added, existing)
        context.log:info("DLC weapons: " .. self.grant_status)
    else
        self.grant_status = ("Stopped after adding %d: %s"):format(added, tostring(message))
        context.log:error("DLC weapons: " .. self.grant_status)
    end
end

function Weapons:update()
    if self.ch9_adapter then
        local ok, message = pcall(function() self.ch9_adapter:update() end)
        if not ok and not self.ch9_failed then
            self.ch9_failed = true
            self.ch9_adapter.error = tostring(message)
            self.ch9_adapter:reset()
            self.context.log:error("DLC CH9 throwable adapter failed: " .. tostring(message))
        end
    end
    if self.ch9_item_adapter and not self.ch9_item_failed then
        local ok, message = pcall(function() self.ch9_item_adapter:update() end)
        if not ok then
            self.ch9_item_failed = true
            self.ch9_item_adapter.error = tostring(message)
            self.ch9_item_adapter:reset()
            self.context.log:error("DLC CH9 Item adapter failed: " .. tostring(message))
        end
    end
    if self.grenade_adapter then
        -- Failed adapters must still drain deferred native pool destruction.
        local ok, message = pcall(function() self.grenade_adapter:update() end)
        if not ok and not self.grenade_failed then
            self.grenade_failed = true
            self.grenade_adapter.error = tostring(message)
            self.grenade_adapter:reset()
            self.context.log:error("DLC grenade adapter failed: " .. tostring(message))
        end
    end
    if self.gauntlet_adapter and not self.gauntlet_failed then
        local ok, message = pcall(function() self.gauntlet_adapter:update() end)
        if not ok then
            self.gauntlet_failed = true
            pcall(function() self.gauntlet_adapter:reset() end)
            self.context.log:error("DLC gauntlet adapter failed: " .. tostring(message))
        end
    end
    if self.player_adapter and not self.adapter_failed then
        local ok, message = pcall(function() self.player_adapter:update() end)
        if not ok then
            self.adapter_failed = true
            self.player_adapter:reset()
            self.context.log:error("DLC weapon player adapter failed: " .. tostring(message))
        end
    end
    self:process_item_box_request()
    local context = self.context
    if self.finished or not self:enabled() then return end
    local now = os.clock()
    if now < self.next_check then return end
    self.next_check = now + 1
    local manager = context.game:singleton("app.ItemManager")
    if not manager then return end
    local complete = true
    for _, id in ipairs(IDS) do
        if not self.requested[id] then
            local data = manager:call("findItemData", id)
            local prefab = data and data:get_field("ItemPrefab")
            local path = prefab and prefab:call("get_Path")
            -- Never touch source DLC prefabs or a foreign mod's item registrations.
            if path and path:lower() == ("BioRand/DlcWeapons/" .. id .. "/Item.pfb"):lower() then
                self.requested[id] = true
                local ok, message = pcall(function()
                    if not prefab:call("get_Ready") then
                        prefab:call("set_Standby", false)
                        prefab:call("set_Path", path)
                        prefab:call("set_Standby", true)
                    end
                end)
                if not ok then context.log:error("DLC weapon load request failed for " .. id .. ": " .. tostring(message)) end
            else
                complete = false
            end
        end
    end
    -- Automatic loading is bounded; item-box grants require an explicit debug request.
    self.finished = complete
end

return Weapons
