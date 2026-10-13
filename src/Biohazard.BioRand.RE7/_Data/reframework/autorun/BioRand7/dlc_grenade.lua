local Grenade = {}
Grenade.__index = Grenade
local VARIANTS = {
    { weapon = 58, item = "Grenadebomb", kind = 0, bank = 9950 },
    { weapon = 59, item = "Thermatebomb", kind = 1, bank = 9951 },
    { weapon = 60, item = "Stangrenadebomb", kind = 2, bank = 9952 },
}
Grenade.variants = VARIANTS
local PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }
local ATTACKS = { ["Melee.AttackL"] = 2400, ["Melee.AttackR"] = 2401, ["Melee.AimAttackC"] = 2407 }
local CHANGE = "changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"
local REQUEST = "requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"

function Grenade.new(game, enabled, root)
    return setmetatable({ game = game, enabled = enabled, root = root, resources = {}, tracks = {},
        pool = require("BioRand7/dlc_grenade_pool").new(game, root) }, Grenade)
end

function Grenade:matches(player, variant)
    if not player or player ~= self.game:player() or not PLAYERS[player:call("get_Name")] then return false end
    -- Game:chapter returns GameFlowKindEnum: C00 through FF050 are the campaign (0..13).
    local flow = self.game:chapter()
    if not flow or flow < 0 or flow > 13 then return false end
    if not variant then
        for _, entry in ipairs(VARIANTS) do if self:matches(player, entry) then return true end end
        return false
    end
    local manager = self.game:singleton("app.ItemManager")
    local item = manager and manager:call("findItemData", variant.item)
    local prefab = item and item:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    return path ~= nil and path:lower() == (self.root .. "/" .. variant.item .. "/Item.pfb"):lower()
end

function Grenade:resource(path)
    if self.resources[path] then return self.resources[path] end
    local resource = assert(sdk.create_resource("via.motion.MotionListResource", path)):add_ref()
    local ok, holder = pcall(function()
        return assert(resource:create_holder("via.motion.MotionListResourceHolder")):add_ref()
    end)
    resource:release()
    if not ok then error(holder) end
    assert(holder:call("get_ResourcePath"):lower() == path:lower(), "Unexpected grenade motion resource")
    self.resources[path] = holder
    return holder
end

function Grenade:owns_banks()
    local s = self.session
    if not s or s.player ~= self.game:player() or #s.banks ~= #s.variants * 3 then return false end
    for _, entry in ipairs(s.banks) do
        local bank = s.motion:call("getDynamicMotionBank", entry.index)
        if bank ~= entry.bank or bank:call("get_BankID") ~= 0 or bank:call("get_BankType") ~= entry.kind then return false end
    end
    return #s.banks > 0
end

function Grenade:prepare(player)
    if self.session and self.session.player == player then
        assert(self:owns_banks(), "Grenade motion banks changed ownership")
        return true
    end
    self:cancel(false)
    -- MotionInfo can be invalidated by scene unload even while its Lua wrapper survives.
    self.motion_info = nil
    self.weapon, self.variant, self.empty, self.last_state, self.owner_wait = nil, nil, nil, nil, nil
    local controller = self.game:component(player, "app.PlayerMotionController")
    local motion = controller and controller:get_field("Motion")
    local manager = controller and controller:get_field("MotionManager")
    local inventory = self.game:component(player, "app.Inventory")
    if not motion or not manager or not inventory then return false, "player motion components" end
    local fallback = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, 10)
    if not fallback or fallback:call("get_BankID") ~= 0 or fallback:call("get_BankType") ~= 10
        or not fallback:call("get_MotionList") then return false, "campaign axe fallback bank" end
    local variants, holders = {}, {}
    for _, variant in ipairs(VARIANTS) do
        if self:matches(player, variant) then
            local existing = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, variant.bank)
            assert(not existing or existing:call("get_BankType") ~= variant.bank, "Grenade bank type is already occupied")
            variants[#variants + 1] = variant
            for _, holder in ipairs({
                self:resource("CH8/Animation/Player/pl1000/motlist/pl1000_" .. variant.item .. ".motlist"),
                self:resource("CH8/Animation/Player/pl1000/motlist/pl1000_Grenade.motlist"),
                fallback:call("get_MotionList"),
            }) do holders[#holders + 1] = { holder = holder, kind = variant.bank } end
        end
    end
    self.pin = self.pin or sdk.create_instance("app.CH8SequenceTrackObject.CH8PlayerPinPulledTrack"):add_ref()
    self.release = self.release or sdk.create_instance("app.CH8SequenceTrackObject.CH8PlayerThrowTrack"):add_ref()
    local s = { player = player, controller = controller, motion = motion, manager = manager,
        inventory = inventory, variants = variants, banks = {} }
    self.session = s
    for _, entry in ipairs(holders) do
        local bank = sdk.create_instance("via.motion.DynamicMotionBank"):add_ref()
        bank:call("set_MotionList", entry.holder)
        bank:call("set_OverwriteBankID", true); bank:call("set_BankID", 0)
        bank:call("set_OverwriteBankType", true); bank:call("set_BankType", entry.kind)
        local index = motion:call("getDynamicMotionBankCount")
        motion:call("setDynamicMotionBankCount", index + 1)
        motion:call("setDynamicMotionBank", index, bank)
        s.banks[#s.banks + 1] = { bank = bank, index = index, kind = entry.kind }
    end
    return true
end

function Grenade:request(name)
    self.session.controller:call(REQUEST, name, 1, 0.0, 4.0, 0)
end

function Grenade:motion_ready(variant)
    local s = self.session
    s.ready = s.ready or {}
    if s.ready[variant.weapon] then return true end
    if self.motion_info and not sdk.is_managed_object(self.motion_info) then self.motion_info = nil end
    self.motion_info = self.motion_info or sdk.create_instance("via.motion.MotionInfo"):add_ref()
    for _, clip in ipairs({ 8000, 8001, 8002, 2001, 2400, 2401 }) do
        if not s.motion:call("getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)",
            0, variant.bank, clip, self.motion_info) or self.motion_info:call("get_MotionEndFrame") <= 0.0 then return false end
    end
    s.ready[variant.weapon] = true
    return true
end

function Grenade:clip(id)
    self.attack.clip = id
    for _, index in ipairs({ 1, 2, 9 }) do self.session.motion:call("getLayer", index):call(CHANGE, 0, id, 0.0, 4.0, 2, 1) end
end

function Grenade:track(layer, kind, object, field)
    for ni = 0, layer:call("getRawMotionNodeCount") - 1 do
        local node = layer:call("getRawMotionNode", ni)
        if node:call("get_MotionID") == self.attack.clip and node:call("get_Weight") > 0.5 then
            local key = self.attack.clip .. ":" .. kind
            local index = self.tracks[key]
            if index == nil then
                index = false
                for ti = 0, node:call("get_SequenceTracksCount") - 1 do
                    if node:call("getSequenceTracksTypeinfo(System.UInt32)", ti):call("get_FullName") == kind then index = ti; break end
                end
                self.tracks[key] = index
            end
            if index ~= false then
                object:call("initialize", 1)
                -- Range sampling cannot lose a one-frame event at low frame rates.
                if node:call("getSequenceTracks(System.UInt32, via.motion.Tracks, System.Single, System.Single)",
                    index, object, 0.0, node:call("get_Frame")) and object:get_field(field) then return true end
            end
        end
    end
    return false
end

function Grenade:cancel(recover)
    local s, attack = self.session, self.attack
    self.attack, self.pending = nil, nil
    if not attack or not s or s.player ~= self.game:player() then return end
    if self.weapon and self.weapon:call("get_Valid") then self.weapon:call("onExitThrow") end
    if recover and s.manager:call("getCurrentMotionFsmStateName", 1, false) == attack.state then
        self:request(attack.state == "Melee.AimAttackC" and "Melee.AimToReady" or attack.state .. "ToReady")
    end
end

function Grenade:reset()
    self.reset_pending = true
    self.motion_info = nil
    self.pool:reset()
end

function Grenade:begin_throw()
    local player = self.session.player
    local q = self.game:component(player, "app.PlayerCamera"):call("getCameraTransformRotation")
    local vertical = math.deg(math.asin(math.max(-1.0, math.min(1.0, 2.0 * (q.y * q.z - q.w * q.x)))))
    local crouched = self.game:component(player, "app.PlayerStatus"):call("get_isCrouch")
    self.attack.under, self.attack.phase = vertical < (crouched and 0.0 or -10.0), "throw"
    self:clip(self.attack.under and 2401 or 2400)
end

function Grenade:update_motion(name, layer, dt)
    if name ~= self.last_state then
        self:cancel(false)
        self.pending = ATTACKS[name] and { state = name, age = 0.0 } or nil
    end
    self.last_state = name
    if self.pending then
        self.pending.age = self.pending.age + dt
        if self.pending.age > 1.0 or not self.pool:available(self.variant.kind) then
            self.pending = nil
            self:request(name == "Melee.AimAttackC" and "Melee.AimToReady" or name .. "ToReady")
        elseif layer:call("get_MotionID") == ATTACKS[name] then
            assert(self.weapon:call("get_stockNum") > 0, "No grenade stock")
            self.attack = { state = name, phase = "standby", age = 0.0 }
            self.pending = nil
            self.weapon:call("onStartStandby(via.GameObject)", self.session.player)
            self:clip(8001)
        end
    end
    local attack = self.attack
    if not attack then return end
    attack.age = attack.age + dt
    if attack.age > 8.0 then self:cancel(true); error("Grenade animation timed out") end
    if layer:call("get_MotionID") ~= attack.clip then self:cancel(false); return end
    if attack.phase == "standby" then
        if not attack.pulled and attack.clip == 8001
            and self:track(layer, "app.CH8SequenceTrackObject.CH8PlayerPinPulledTrack", self.pin, "IsPinPulled") then
            self.weapon:call("onPinPulled(via.GameObject)", self.session.player); attack.pulled = true
        end
        if attack.pulled then self.weapon:call("onUpdateStandby(via.GameObject)", self.session.player) end
        self.attack_command = self.attack_command or self.game:static_field("app.Command.PlayerCommandID", "AttackRight")
        local held = self.game:component(self.session.player, "app.PlayerCommandUpdater"):call("isRequested", self.attack_command)
        if attack.pulled and (attack.clip == 8002 or layer:call("get_Frame") >= 23.0)
            and (not held or self.weapon:call("isForceThrow")) then self:begin_throw()
        elseif layer:call("get_StateEndOfMotion") then
            if not attack.pulled then self:cancel(true); error("Missing grenade pin track") end
            self:clip(8002)
        end
    else
        if not attack.released and self:track(layer, "app.CH8SequenceTrackObject.CH8PlayerThrowTrack", self.release, "IsThrow") then
            assert(self.pool:available(self.variant.kind), "Grenade pool exhausted before release")
            attack.released = true
            local weapon, item = self.weapon, self.variant.item
            local stock, infinite = weapon:call("get_stockNum"), weapon:call("get_isBulletStackNumInfinity")
            weapon:call("onStartThrow(via.GameObject, System.Boolean)", self.session.player, attack.under)
            if not infinite then
                -- Native reduction may immediately destroy the last equipped item.
                if stock == 1 then
                    weapon:call("onExitThrow")
                    self.empty = { clip = attack.clip, state = name, age = 0.0 }
                    self.attack, self.weapon, self.variant = nil, nil, nil
                end
                assert(self.session.inventory:call("reduceItem(System.String, System.Int32, System.Boolean)", item, 1, false),
                    "Inventory refused grenade consumption")
                if stock == 1 then return end
            end
        end
        if layer:call("get_StateEndOfMotion") then
            if not attack.released then self:cancel(true); error("Missing grenade release track") end
            self:cancel(true)
        end
    end
end

function Grenade:update()
    local now = os.clock()
    local dt = math.max(0.0, math.min(1.0, now - (self.last_tick or now)))
    self.last_tick = now
    local player = self.game:player()
    if self.reset_pending or not self.enabled() or self.error or not self:matches(player) then
        self:cancel(false)
        self.weapon, self.variant, self.empty, self.last_state, self.equip_pending, self.owner_wait = nil, nil, nil, nil, nil, nil
        self.reset_pending = nil
        self.pool:update(nil)
        return
    end
    local gm = self.game:singleton("app.GameManager")
    local pool_ready = self.pool:update(player)
    if not gm or gm:call("get_IsSceneLoading") then self:cancel(false); return end
    if gm:call("get_IsPause") then return end
    local ready, reason = self:prepare(player)
    if not ready then
        if not self.waiting or self.waiting.player ~= player then self.waiting = { player = player, since = now } end
        assert(now - self.waiting.since < 10.0, "Grenade player readiness timed out: " .. reason)
        return
    end
    self.waiting = nil
    local s = self.session
    local owner = s.manager:get_field("OwnerTask")
    local damage = self.game:component(player, "app.PlayerDamageController")
    if not owner or s.manager:get_field("CurrentTask") ~= owner or not damage or damage:call("get_health") <= 0 then
        self:cancel(false); self.empty = nil; return
    end
    local menu = self.game:singleton("app.MenuManager")
    if menu and menu:call("isOpenInventoryMenu") then return end
    local weapon = self.game:component(player, "app.EquipManager"):call("get_equipWeaponRight")
    local name, layer = s.manager:call("getCurrentMotionFsmStateName", 1, false), s.motion:call("getLayer", 1)
    if self.empty then
        local empty = self.empty
        empty.age = empty.age + dt
        if weapon or name ~= empty.state or empty.age > 2.0 or layer:call("get_MotionID") ~= empty.clip then
            self.empty = nil
        elseif layer:call("get_StateEndOfMotion") then
            s.controller:call("updateTargetBankType")
            self:request("Hands.ReadyStart")
            self.empty = nil
        end
        if not weapon then return end
    end
    local variant
    if weapon and weapon:call("get_Valid") then
        for _, entry in ipairs(s.variants) do
            if weapon:get_field("WeaponID") == entry.weapon and self:matches(player, entry) then variant = entry; break end
        end
    end
    if not variant then self:cancel(false); self.weapon, self.variant, self.last_state, self.equip_pending, self.owner_wait = nil, nil, nil, nil, nil; return end
    assert(weapon:get_type_definition():get_full_name() == "app.CH8WeaponThrowable", "Unexpected grenade component")
    local inventory = weapon:get_field("Inventory")
    assert(not inventory or inventory == s.inventory, "Unexpected grenade inventory owner")
    if not inventory then
        -- Quick-slot selection can expose the component before native doStart binds its inventory.
        self:cancel(false)
        self.weapon, self.variant, self.last_state, self.equip_pending = nil, nil, nil, nil
        if not self.owner_wait or self.owner_wait.weapon ~= weapon then self.owner_wait = { weapon = weapon, age = 0.0 } end
        self.owner_wait.age = self.owner_wait.age + dt
        assert(self.owner_wait.age < 2.0, "Grenade inventory owner readiness timed out")
        return
    end
    self.owner_wait = nil
    if self.weapon ~= weapon then
        self:cancel(false)
        self.weapon, self.variant, self.last_state = weapon, variant, nil
        self.equip_pending = { age = 0.0 }
        s.controller:call("updateTargetBankType")
    end
    if not pool_ready then self:cancel(true); return end
    if self.equip_pending then
        self.equip_pending.age = self.equip_pending.age + dt
        assert(self.equip_pending.age < 10.0, "Grenade motion loading timed out")
        if not self:motion_ready(variant) then return end
        self.equip_pending = nil
        -- Saved equips can select ReadyStart before our banks have loaded.
        if name == "Melee.ReadyStart" and layer:call("get_EndFrame") <= 0.0 then
            s.controller:call("updateTargetBankType")
            self:request("Melee.ReadyIdle")
            return
        end
    end
    self:update_motion(name, layer, dt)
end

function Grenade:scope(controller)
    local s = self.session
    if not s or not self.enabled() or self.error or controller ~= s.controller or not self:owns_banks() then return false end
    for _, variant in ipairs(s.variants) do
        if controller:get_field("CurrentWeaponID") == variant.weapon and self:matches(s.player, variant) then return true end
    end
    return false
end

function Grenade:install()
    if self.installed then return end
    self.installed = true
    local bank_key, weapon_key = {}, {}
    self.game:hook("app.CH8WeaponThrowable", "doStart", function(args)
        thread.get_hook_storage()[weapon_key] = self.game:object(args[2])
    end, function(ret)
        local storage = thread.get_hook_storage()
        local weapon = storage[weapon_key]; storage[weapon_key] = nil
        local player = self.game:player()
        if self.enabled() and not self.error and weapon and weapon:call("get_Valid") then
            for _, variant in ipairs(VARIANTS) do
                if weapon:get_field("WeaponID") == variant.weapon and self:matches(player, variant)
                    and weapon:get_field("Inventory") == self.game:component(player, "app.Inventory") then
                    weapon:call("set_weaponCategory", 0); break
                end
            end
        end
        return ret
    end)
    self.game:hook("app.PlayerMotionController", "getBankType(app.WeaponID)", function(args)
        local storage = thread.get_hook_storage(); storage[bank_key] = nil
        local s = self.session
        -- Keep installed banks valid while an already-equipped item is being removed.
        if s and self.game:object(args[2]) == s.controller and self:owns_banks() then
            for _, variant in ipairs(s.variants) do
                if sdk.to_int64(args[3]) == variant.weapon and self:matches(s.player, variant) then
                    storage[bank_key] = variant.bank; break
                end
            end
        end
    end, function(ret)
        local storage = thread.get_hook_storage(); local bank = storage[bank_key]; storage[bank_key] = nil
        return bank and sdk.to_ptr(bank) or ret
    end)
    self.game:hook("app.PlayerMotionController", "updateLArmMotion()", function(args)
        local controller = self.game:object(args[2])
        if not self:scope(controller) then return end
        local work = controller:get_field("MotionWorks"):get_element(1)
        local name = work:get_field("StateName")
        if not name or name:sub(1, 6) ~= "Melee." then return end
        if work:get_field("IsSet") then
            controller:call("setMotionWorkCore", name, work:get_field("StartFrame"), work:get_field("InterpolationFrame"),
                work:get_field("InterpolationMode"), work:get_field("InterpolationCurve"), work:get_field("Priority"), 2)
            local left = controller:get_field("MotionWorks"):get_element(2)
            if left:get_field("IsSet") then
                controller:get_field("MotionFsm"):call("setCurrentStateFullName(System.String, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)",
                    left:get_field("StateName"), 2, left:get_field("StartFrame"), left:get_field("InterpolationFrame"),
                    left:get_field("InterpolationMode"), left:get_field("InterpolationCurve"))
            end
        end
        controller:call("set_isBothHands", true)
        return sdk.PreHookResult.SKIP_ORIGINAL
    end, function(ret) return ret end)
end

return Grenade
