local Throwable = {}
Throwable.__index = Throwable
local VARIANTS = {
    { weapon = 64, item = "CH9_WP003", bank = 9960, motion = "NailKnife", class = "app.CH9Weapon1500", kind = 4 },
    { weapon = 65, item = "CH9_WP004", bank = 9961, motion = "BangStick", class = "app.CH9Weapon1800", kind = 5 },
}
Throwable.variants = VARIANTS
local PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }
local ATTACKS = { ["Melee.AttackL"] = 2400, ["Melee.AttackR"] = 2401, ["Melee.AimAttackC"] = 2407 }
local CHANGE = "changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"
local REQUEST = "requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"

function Throwable.new(game, enabled, root)
    return setmetatable({ game = game, enabled = enabled, root = root, resources = {},
        pool = require("BioRand7/dlc_ch9_pool").new(game, root) }, Throwable)
end

function Throwable:matches(player, variant)
    if not player or player ~= self.game:player() or not PLAYERS[player:call("get_Name")] then return false end
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

function Throwable:resource(path)
    if self.resources[path] then return self.resources[path] end
    local resource = assert(sdk.create_resource("via.motion.MotionListResource", path)):add_ref()
    local ok, holder = pcall(function()
        return assert(resource:create_holder("via.motion.MotionListResourceHolder")):add_ref()
    end)
    resource:release()
    if not ok then error(holder) end
    assert(holder:call("get_ResourcePath"):lower() == path:lower(), "Unexpected CH9 motion resource")
    self.resources[path] = holder
    return holder
end

function Throwable:owns_banks()
    local s = self.session
    if not s or s.player ~= self.game:player() or #s.banks ~= #s.variants * 2 then return false end
    for _, entry in ipairs(s.banks) do
        local bank = s.motion:call("getDynamicMotionBank", entry.index)
        if bank ~= entry.bank or bank:call("get_BankID") ~= 0 or bank:call("get_BankType") ~= entry.kind then return false end
    end
    return #s.banks > 0
end

function Throwable:clear_scratch()
    -- Scene unload can invalidate native scratch objects despite a retained Lua wrapper.
    self.motion_info, self.track_object, self.hit, self.hit_ref = nil, nil, nil, nil
end

function Throwable:prepare(player)
    if self.session and self.session.player == player then
        assert(self:owns_banks(), "CH9 motion banks changed ownership")
        return true
    end
    self:cancel(false)
    self:clear_scratch()
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
            assert(not existing or existing:call("get_BankType") ~= variant.bank, "CH9 bank type is already occupied")
            variants[#variants + 1] = variant
            for _, holder in ipairs({ self:resource("CH9/Animation/Player/pl9000/motlist/pl9000_" .. variant.motion .. ".motlist"),
                fallback:call("get_MotionList") }) do holders[#holders + 1] = { holder = holder, kind = variant.bank } end
        end
    end
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

function Throwable:request(name)
    self.session.controller:call(REQUEST, name, 1, 0.0, 4.0, 0)
end

function Throwable:motion_ready(variant)
    local s = self.session
    s.ready = s.ready or {}
    if s.ready[variant.weapon] then return true end
    -- Native scratch can expire between weapon equips without a player replacement.
    if self.motion_info and not sdk.is_managed_object(self.motion_info) then self.motion_info = nil end
    self.motion_info = self.motion_info or sdk.create_instance("via.motion.MotionInfo"):add_ref()
    for _, clip in ipairs({ 2001, 2400, 2405, 2406, 2407 }) do
        if not s.motion:call("getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)",
            0, variant.bank, clip, self.motion_info) then return false end
        -- 2400/2405 are blend roots with end frame -1; duration belongs to their children.
        if clip ~= 2400 and clip ~= 2405 and self.motion_info:call("get_MotionEndFrame") <= 0.0 then return false end
    end
    s.ready[variant.weapon] = true
    return true
end

function Throwable:set_visible(weapon, visible)
    if not weapon or not weapon:call("get_Valid") then return end
    local object = weapon:call("get_GameObject")
    if self.game:valid(object) then
        self.game:method("app.Util", "setActive(via.GameObject, System.Boolean, System.Boolean)"):call(nil, object, visible, visible)
    end
end

function Throwable:cancel(recover)
    local s, attack = self.session, self.attack
    self.attack, self.pending = nil, nil
    if not s or not attack or s.player ~= self.game:player() then return end
    if attack.hidden then self:set_visible(attack.weapon, true) end
    if recover and s.manager:call("getCurrentMotionFsmStateName", 1, false) == attack.state then
        self:request(attack.state == "Melee.AimAttackC" and "Melee.AimToReady" or attack.state .. "ToReady")
    end
end

function Throwable:reset()
    self.reset_pending = true
    self:clear_scratch()
    self.pool:reset()
end

function Throwable:geometry()
    local s = self.session
    local camera = assert(self.game:component(s.player, "app.PlayerCamera"))
    local ray = camera:call("get_shootRay")
    local from, dir = ray:get_field("from"), ray:get_field("dir")
    local knife = self.variant.kind == 4
    local offset = knife and 0.1 or 1.0
    local start = Vector3f.new(from.x + dir.x * offset, from.y + dir.y * offset, from.z + dir.z * offset)
    self.hit = self.hit or sdk.create_instance("app.Collision.CollisionSystem.HitResult"):add_ref()
    self.hit_ref = self.hit_ref or ValueType.new(sdk.find_type_definition("System.UInt64"))
    self.hit_ref:write_qword(0, self.hit:get_address())
    local collision = assert(self.game:singleton("app.Collision.CollisionSystem"))
    local damage = self.game:static_field("app.Collision.CollisionSystem.Filter", "DamageCheckDefault")
    local effect = self.game:static_field("app.Collision.CollisionSystem.Filter", "EffectCheckBullet")
    local first = self.game:method("app.Collision.CollisionSystem",
        "castRay(via.vec3, via.vec3, System.Single, via.GameObject, app.Collision.CollisionSystem.HitResult, via.physics.FilterInfo, System.Boolean)"):call(
        collision, from, dir, 1.0, s.player, self.hit_ref, damage, false)
    assert(first ~= nil, "CH9 damage ray did not return a result")
    assert(self.hit_ref:read_qword(0) == self.hit:get_address(), "Native ray replaced CH9 hit scratch")
    if first then
        local p = self.hit:get_field("Position")
        local back = knife and 1.0 or 0.2
        start = Vector3f.new(p.x - dir.x * back, p.y - dir.y * back, p.z - dir.z * back)
    end
    local second = self.game:method("app.Collision.CollisionSystem",
        "castRay(via.vec3, via.vec3, System.Single, app.Collision.CollisionSystem.HitResult, via.physics.FilterInfo, System.Boolean)"):call(
        collision, from, dir, 1.0, self.hit_ref, effect, false)
    assert(second ~= nil, "CH9 effect ray did not return a result")
    assert(self.hit_ref:read_qword(0) == self.hit:get_address(), "Native ray replaced CH9 hit scratch")
    if second then
        local p = self.hit:get_field("Position")
        local function distance2(a) return (a.x - from.x)^2 + (a.y - from.y)^2 + (a.z - from.z)^2 end
        -- Native compares the second raw hit against the first adjusted launch position.
        if not first or distance2(p) < distance2(start) then
            local back = knife and 1.0 or 0.36
            start = Vector3f.new(p.x - dir.x * back, p.y - dir.y * back, p.z - dir.z * back)
        end
    end
    return start, camera:call("getCameraTransformRotation")
end

function Throwable:release_track(layer)
    self.track_object = self.track_object or sdk.create_instance("app.SequenceTrackObject.CH9PlayerActionTrack"):add_ref()
    for ni = 0, layer:call("getRawMotionNodeCount") - 1 do
        local node = layer:call("getRawMotionNode", ni)
        local id = node:call("get_MotionID")
        if (id == 2406 or id == 2407) and node:call("get_Weight") > 0.5 then
            for ti = 0, node:call("get_SequenceTracksCount") - 1 do
                if node:call("getSequenceTracksTypeinfo(System.UInt32)", ti):call("get_FullName") == "app.SequenceTrackObject.CH9PlayerActionTrack" then
                    self.track_object:call("initialize", 1)
                    if node:call("getSequenceTracks(System.UInt32, via.motion.Tracks, System.Single, System.Single)",
                        ti, self.track_object, 0.0, node:call("get_Frame")) and self.track_object:get_field("IsWepThrowTiming") then return true end
                end
            end
        end
    end
    return false
end

function Throwable:launch()
    local s, attack, variant = self.session, self.attack, self.variant
    local stock = s.inventory:call("getTotalStackNum", variant.item, false)
    assert(stock > 0, "No CH9 throwing stock")
    if not self.pool:available(variant.kind) then self:cancel(true); return end
    local position, rotation = self:geometry()
    local manager = assert(self.game:singleton("app.ShellManager"), "Missing campaign shell manager")
    local shell = self.pool.manager:call("setupThrowingWeapon(via.GameObject, app.CH9ShellManager.ThrowingWeaponType, System.Int32)",
        s.player, variant.kind, manager:call("makeShellID"))
    if not shell then self:cancel(true); return end
    attack.released = true
    shell:call("activate(via.vec3, via.Quaternion)", position, rotation)
    assert(shell:call("get_isActive") and shell:call("get_ownerEquip") == s.player, "Native CH9 projectile failed activation")
    attack.hidden = true
    self:set_visible(attack.weapon, false)
    attack.weapon:call("reduceWeapon")
    -- Native reduction may destroy the weapon immediately. Keep only animation state.
    if s.inventory:call("getTotalStackNum", variant.item, false) == 0 then
        self.empty = { clip = attack.clip, state = attack.state, age = 0.0 }
        self.attack, self.weapon, self.variant = nil, nil, nil
    end
end

function Throwable:update_motion(name, layer, dt)
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
            self.pending = nil
            local clip = name == "Melee.AimAttackC" and 2405 or 2400
            self.attack = { state = name, clip = clip, weapon = self.weapon, age = 0.0 }
            if layer:call("get_MotionID") ~= clip then
                for _, index in ipairs({ 1, 2, 9 }) do self.session.motion:call("getLayer", index):call(CHANGE, 0, clip, 0.0, 4.0, 2, 1) end
            end
        end
    end
    local attack = self.attack
    if not attack then return end
    attack.age = attack.age + dt
    if attack.age > 5.0 then self:cancel(true); error("CH9 throw animation timed out") end
    if layer:call("get_MotionID") ~= attack.clip then self:cancel(false); return end
    if not attack.released and self:release_track(layer) then self:launch() end
    if self.attack ~= attack then return end
    if layer:call("get_StateEndOfMotion") then
        self:cancel(true)
        assert(attack.released, "Missing CH9 release track")
    end
end

function Throwable:update()
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
    if not gm or gm:call("get_IsSceneLoading") then self:cancel(false); self.empty = nil; return end
    if gm:call("get_IsPause") then return end
    local ready, reason = self:prepare(player)
    if not ready then
        if not self.waiting or self.waiting.player ~= player then self.waiting = { player = player, since = now } end
        assert(now - self.waiting.since < 10.0, "CH9 player readiness timed out: " .. reason)
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
        if weapon or name ~= empty.state or empty.age > 3.0 or layer:call("get_MotionID") ~= empty.clip then
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
    assert(weapon:get_type_definition():get_full_name() == variant.class, "Unexpected CH9 throwable component")
    local inventory = weapon:get_field("Inventory")
    assert(not inventory or inventory == s.inventory, "Unexpected CH9 throwable inventory owner")
    if not inventory then
        self:cancel(false)
        self.weapon, self.variant, self.last_state, self.equip_pending = nil, nil, nil, nil
        if not self.owner_wait or self.owner_wait.weapon ~= weapon then self.owner_wait = { weapon = weapon, age = 0.0 } end
        self.owner_wait.age = self.owner_wait.age + dt
        assert(self.owner_wait.age < 2.0, "CH9 throwable inventory owner readiness timed out")
        return
    end
    self.owner_wait = nil
    if self.weapon ~= weapon then
        self:cancel(false)
        self.weapon, self.variant, self.last_state = weapon, variant, nil
        self.equip_pending = { age = 0.0 }
        weapon:call("set_weaponCategory", 0)
        s.controller:call("updateTargetBankType")
    end
    if not pool_ready then self:cancel(true); return end
    if self.equip_pending then
        self.equip_pending.age = self.equip_pending.age + dt
        assert(self.equip_pending.age < 10.0, "CH9 motion loading timed out")
        if not self:motion_ready(variant) then return end
        self.equip_pending = nil
        if name == "Melee.ReadyStart" and layer:call("get_EndFrame") <= 0.0 then
            s.controller:call("updateTargetBankType")
            self:request("Melee.ReadyIdle")
            return
        end
    end
    self:update_motion(name, layer, dt)
end

function Throwable:scope(controller)
    local s = self.session
    if not s or not self.enabled() or self.error or self.reset_pending or controller ~= s.controller or not self:owns_banks() then return false end
    for _, variant in ipairs(s.variants) do
        if controller:get_field("CurrentWeaponID") == variant.weapon and self:matches(s.player, variant) then return true end
    end
    return false
end

function Throwable:install()
    if self.installed then return end
    self.installed = true
    self.pool:install()
    local hooked = {}
    for _, variant in ipairs(VARIANTS) do
        -- Both concrete doStart methods share a native body in the RT build.
        local address = tostring(assert(self.game:method(variant.class, "doStart")):get_function())
        if not hooked[address] then
            hooked[address] = true
            local key = {}
            self.game:hook(variant.class, "doStart", function(args)
                thread.get_hook_storage()[key] = self.game:object(args[2])
            end, function(ret)
                local storage = thread.get_hook_storage()
                local weapon = storage[key]; storage[key] = nil
                local player = self.game:player()
                if self.enabled() and not self.error and not self.reset_pending and weapon and weapon:call("get_Valid") then
                    for _, entry in ipairs(VARIANTS) do
                        if weapon:get_field("WeaponID") == entry.weapon and self:matches(player, entry)
                            and weapon:get_type_definition():get_full_name() == entry.class
                            and weapon:get_field("Inventory") == self.game:component(player, "app.Inventory") then
                            weapon:call("set_weaponCategory", 0); break
                        end
                    end
                end
                return ret
            end)
        end
    end
    local bank_key = {}
    self.game:hook("app.PlayerMotionController", "getBankType(app.WeaponID)", function(args)
        local storage = thread.get_hook_storage(); storage[bank_key] = nil
        local s = self.session
        if s and self.game:object(args[2]) == s.controller and self:owns_banks() then
            for _, variant in ipairs(s.variants) do
                if sdk.to_int64(args[3]) == variant.weapon and self:matches(s.player, variant) then storage[bank_key] = variant.bank; break end
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
                    left:get_field("StateName"), 2, left:get_field("StartFrame"), left:get_field("InterpolationFrame"), left:get_field("InterpolationMode"), left:get_field("InterpolationCurve"))
            end
        end
        controller:call("set_isBothHands", true)
        return sdk.PreHookResult.SKIP_ORIGINAL
    end, function(ret) return ret end)
end

return Throwable
