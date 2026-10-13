-- Experimental campaign adapter, scoped to its exported inventory prefab namespace.
local Gauntlet = {}
Gauntlet.__index = Gauntlet

local VARIANTS = {
    { weapon = 67, item = "CH9_WP006", bank = 9910, list = "GauntletW", dual = true, guard = 0.15,
        combo = { {2441, 0, 43}, {2641, 1, 40}, {2443, 2, 45}, {2643, 6, 42} },
        charge_start = 2688, charge_loop = 2689, charge = { {2690, 4, 32}, {2690, 4, 32}, {2691, 5, 33} } },
    { weapon = 61, item = "CH9_WP000", bank = 9911, list = "GauntletR", guard = 0.05,
        combo = { {2441, 0, 16}, {2641, 1, 37}, {2443, 2, 17}, {2643, 3, 39} },
        charge_start = 2660, charge_loop = 2661, charge = { {2662, 4, 23}, {2663, 5, 24}, {2664, 6, 25} } },
    { weapon = 62, item = "CH9_WP001", bank = 9912, list = "Gauntlet", guard = 0.05,
        combo = { {2441, 0, 16}, {2641, 1, 34}, {2443, 2, 17}, {2643, 3, 36} },
        charge_start = 2660, charge_loop = 2661, charge = { {2662, 4, 20}, {2663, 5, 21}, {2664, 6, 22} } },
}
Gauntlet.variants = VARIANTS
local LAYERS = { 1, 2, 9 }
local CAMPAIGN_PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }
local AIM = { ["Melee.ReadyToAim"] = true, ["Melee.AimIdle"] = true,
    ["Melee.AimIdleSp"] = true, ["Melee.AimMove"] = true }
local BOTH = { ["Melee.ReadyStart"] = true, ["Melee.ReadyIdle"] = true,
    ["Melee.ReadyIdleSp"] = true, ["Melee.ReadyMove"] = true, ["Melee.ReadyJogStart"] = true,
    ["Melee.ReadyJogEnd"] = true, ["Melee.AttackL"] = true, ["Melee.AttackR"] = true,
    ["Melee.AttackLToReady"] = true, ["Melee.AttackRToReady"] = true,
    ["Melee.AimAttackC"] = true, ["Melee.AimToReady"] = true }
local CHANGE = "changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"
local REQUEST = "requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"

function Gauntlet.new(game, enabled, root)
    return setmetatable({ game = game, enabled = enabled, root = root,
        resources = {}, combo = 0, track_indices = {} }, Gauntlet)
end

function Gauntlet:matches(player, variant)
    if not player or player ~= self.game:player() or not CAMPAIGN_PLAYERS[player:call("get_Name")] then return false end
    if not variant then
        for _, entry in ipairs(VARIANTS) do if self:matches(player, entry) then return true end end
        return false
    end
    local manager = self.game:singleton("app.ItemManager")
    local data = manager and manager:call("findItemData", variant.item)
    local prefab = data and data:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    return path ~= nil and path:lower() == (self.root .. "/" .. variant.item .. "/Item.pfb"):lower()
end

function Gauntlet:resource(kind, path)
    local key = kind .. ":" .. path
    if self.resources[key] then return self.resources[key] end
    local resource = assert(sdk.create_resource(kind, path), "Missing gauntlet resource: " .. path):add_ref()
    local ok, holder = pcall(function()
        return assert(resource:create_holder(kind .. "Holder"), "Cannot create gauntlet resource holder"):add_ref()
    end)
    resource:release()
    if not ok then error(holder) end
    assert(holder:call("get_ResourcePath"):lower() == path:lower(), "Unexpected gauntlet resource")
    self.resources[key] = holder
    return holder
end

function Gauntlet:owns_banks()
    local s = self.session
    if not s or s.player ~= self.game:player() then return false end
    if not s.bank_count or s.bank_count < 3 or #s.banks ~= s.bank_count then return false end
    for i = 1, s.bank_count do
        local entry = s.banks[i]
        if not entry then return false end
        local bank = s.motion:call("getDynamicMotionBank", entry.index)
        if bank ~= entry.bank or bank:call("get_BankID") ~= 0 or bank:call("get_BankType") ~= entry.kind then return false end
    end
    return true
end

function Gauntlet:prepare(player)
    if self.session and self.session.player == player then
        assert(self:owns_banks(), "Gauntlet motion banks changed ownership")
        return true
    end
    self:reset()
    self.session = nil
    assert(self:matches(player), "Gauntlets require a matching campaign player and exported prefab")
    local game_manager = self.game:singleton("app.GameManager")
    if not game_manager or game_manager:call("get_IsSceneLoading") then return false, "scene loading" end
    local controller = self.game:component(player, "app.PlayerMotionController")
    local motion = controller and controller:get_field("Motion")
    local manager = controller and controller:get_field("MotionManager")
    local sequence = self.game:component(player, "app.PlayerSequenceManager")
    if not motion or not manager or not sequence then return false, "player motion components" end
    local variants = {}
    for _, variant in ipairs(VARIANTS) do
        if self:matches(player, variant) then
            local existing = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, variant.bank)
            assert(not existing or existing:call("get_BankType") ~= variant.bank, "Gauntlet bank type is already occupied")
            variants[#variants + 1] = variant
        end
    end
    local fallback = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, 10)
    if not fallback or fallback:call("get_BankID") ~= 0 or fallback:call("get_BankType") ~= 10
        or not fallback:call("get_MotionList") then return false, "campaign axe fallback bank" end
    local mesh_controller = self.game:component(player, "app.PlayerMeshController")
    if not mesh_controller then return false, "player mesh controller" end
    local hands = {}
    for _, entry in ipairs({ { "RArmMesh", "pl9010", false }, { "LArmMesh", "pl9020", true } }) do
        -- Resolve the active player's arms, never a same-named object in another
        -- loaded chapter. These are TDB fields, not property getters.
        local mesh = mesh_controller:get_field(entry[1])
        local object = mesh and mesh:call("get_GameObject")
        if not mesh or not mesh:call("getMesh") or not mesh:call("get_Material") then return false, entry[1] end
        if not mesh:call("get_MeshReady") or not mesh:call("get_MaterialReady") then return false, entry[1] .. " resources" end
        local parent, owned = object:call("get_Transform"), false
        for _ = 1, 32 do
            if not parent then break end
            if parent:call("get_GameObject") == player then owned = true; break end
            parent = parent:call("get_Parent")
        end
        if not owned then return false, entry[1] .. " ownership" end
        local path = "CH9/Character/Player/pl9000/" .. entry[2] .. "/" .. entry[2]
        hands[#hands + 1] = { object = object, mesh = mesh, path = path, left = entry[3] }
    end
    -- A newly discovered player can precede its banks and hand renderers by seconds.
    -- Resolve every native dependency before allocating or attaching anything.
    local holders = {}
    for _, variant in ipairs(variants) do
        for _, holder in ipairs({
            self:resource("via.motion.MotionListResource", "CH9/Animation/Player/pl9000/motlist/pl9000_" .. variant.list .. ".motlist"),
            self:resource("via.motion.MotionListResource", "CH9/Animation/Player/pl9000/motlist/pl9000_Knuckle.motlist"),
            fallback:call("get_MotionList"),
        }) do holders[#holders + 1] = { holder = holder, kind = variant.bank } end
    end
    for _, hand in ipairs(hands) do
        hand.replacement = self:resource("via.render.MeshResource", hand.path .. ".mesh")
        hand.material = self:resource("via.render.MeshMaterialResource", hand.path .. ".mdf2")
    end
    self.collider_track = self.collider_track or sdk.create_instance("app.Collision.ColliderTrack"):add_ref()
    self.charge_track = self.charge_track or sdk.create_instance("app.SequenceTrackObject.CH9PlayerGauntletChargeLevel"):add_ref()
    local s = { player = player, controller = controller, motion = motion, hands = hands, banks = {},
        sequence = sequence, manager = manager, variants = variants, bank_count = #holders }
    self.session = s
    -- Append only our isolated bank types. Never replace a campaign bank.
    for _, entry in ipairs(holders) do
        local bank = sdk.create_instance("via.motion.DynamicMotionBank"):add_ref()
        bank:call("set_MotionList", entry.holder)
        bank:call("set_OverwriteBankID", true); bank:call("set_BankID", 0)
        bank:call("set_OverwriteBankType", true); bank:call("set_BankType", entry.kind)
        local index = motion:call("getDynamicMotionBankCount")
        motion:call("setDynamicMotionBankCount", index + 1)
        motion:call("setDynamicMotionBank", index, bank)
        s.banks[#s.banks + 1] = { index = index, bank = bank, kind = entry.kind }
    end
    return true
end

function Gauntlet:clear_attack()
    if self.session and self.session.player == self.game:player() then
        -- Item-box storage can destroy these components before the next update.
        -- Even get_GameObject throws on an invalid native component.
        if self.collider and self.collider:call("get_Valid") then
            for i = 0, 6 do self.collider:call("unregisterRequestSet(System.UInt32)", i) end
        end
        if self.weapon and self.weapon:call("get_Valid") then self.weapon:call("offAttackTrigger") end
    end
    self.active = false
end

function Gauntlet:reset()
    self.motion_info = nil
    self:clear_attack()
    local s = self.session
    if s and s.player == self.game:player() then
        for _, hand in ipairs(s.hands) do
            if hand.original and self.game:valid(hand.object) and hand.mesh:call("get_Valid") then
                hand.mesh:call("setMesh", hand.original)
                hand.mesh:call("set_Material", hand.original_material)
                for i, value in ipairs(hand.parts) do hand.mesh:call("setPartsEnable", i - 1, value) end
            end
            hand.original, hand.original_material, hand.parts = nil, nil, nil
        end
    end
    self.weapon, self.collider, self.hit, self.variant = nil, nil, nil, nil
    self.attack, self.charge, self.last_state, self.controllable = nil, nil, nil, false
    self.equip_pending, self.unequip_pending = nil, nil
    self.combo = 0
    -- Banks belong to the live player until its destruction. Dropping them during
    -- a load callback or while the weapon is still equipped invalidates animations.
end

function Gauntlet:equip(weapon, variant)
    self:reset()
    local s, game = self.session, self.game
    local object = weapon:call("get_GameObject")
    local item = game:component(object, "app.Item")
    assert(item and item:get_field("ItemDataID") == variant.item and self:matches(s.player, variant), "Unexpected gauntlet instance")
    local collider = assert(game:component(object, "via.physics.RequestSetCollider"))
    assert(collider:call("getNumCollidables(System.UInt32)", 6) > 0, "Gauntlet collider export is outdated")
    self.collider, self.hit = collider, assert(game:component(object, "app.Collision.HitController"))
    self.weapon, self.variant = weapon, variant
    self.equip_pending = true
    local skeleton = game:component(object, "via.render.Mesh")
        or object:call("createComponent", sdk.typeof("via.render.Mesh"))
    local body = assert(game:component(s.player, "via.render.Mesh"))
    skeleton:call("set_DrawDefault", false)
    skeleton:call("setMesh", body:call("getMesh"))
    skeleton:call("set_Material", body:call("get_Material"))
    local transform = object:call("get_Transform")
    transform:call("set_LocalPosition", Vector3f.new(0, 0, 0))
    transform:call("set_LocalRotation", Quaternion.identity())
    assert(weapon:get_field("EquipParam"):get_field("JointName") == "", "Unexpected gauntlet attachment")
    transform:call("set_ParentJoint", "")
    transform:call("set_SameJointsConstraint", true)
    for _, hand in ipairs(s.hands) do
        hand.original, hand.original_material = hand.mesh:call("getMesh"):add_ref(), hand.mesh:call("get_Material"):add_ref()
        hand.parts = {}
        for i = 0, hand.mesh:call("getPartsEnableCount") - 1 do
            hand.parts[#hand.parts + 1] = hand.mesh:call("getPartsEnable", i)
        end
        hand.mesh:call("setMesh", hand.replacement)
        hand.mesh:call("set_Material", hand.material)
        local gauntlet = variant.dual or hand.left
        for i = 0, 4 do hand.mesh:call("setPartsEnable", i, (gauntlet and (i == 2 or i == 3)) or (not gauntlet and i == 0)) end
    end
end

function Gauntlet:change_clip(id, frame)
    for _, index in ipairs(LAYERS) do
        self.session.motion:call("getLayer", index):call(CHANGE, 0, id, frame or 0.0, 4.0, 2, 1)
    end
end

function Gauntlet:request(name)
    self.session.controller:call(REQUEST, name, 1, 0.0, 4.0, 0)
end

function Gauntlet:recover_equip(name, layer)
    if not self.equip_pending then return end
    if name ~= "Melee.ReadyStart" or layer:call("get_EndFrame") > 0.0 then
        self.equip_pending = nil
        return
    end
    -- A saved equip can select its motion before our isolated bank exists.
    -- Wait for the actual idle clip, then make one ordinary motion request.
    if self.motion_info and not sdk.is_managed_object(self.motion_info) then self.motion_info = nil end
    self.motion_info = self.motion_info or sdk.create_instance("via.motion.MotionInfo"):add_ref()
    local s = self.session
    if not s.motion:call("getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)",
        0, self.variant.bank, 2001, self.motion_info) or self.motion_info:call("get_MotionEndFrame") <= 0.0 then return end
    s.controller:call("updateTargetBankType")
    assert(s.motion:call("get_TargetBankType") == self.variant.bank, "Gauntlet equip selected an unexpected bank")
    self.equip_pending = nil
    self:request("Melee.ReadyIdle")
end

function Gauntlet:recover_unequip(manager)
    local pending, s = self.unequip_pending, self.session
    if not pending then return end
    local damage = self.game:component(s.player, "app.PlayerDamageController")
    local owner = s.manager:get_field("OwnerTask")
    if not manager or manager:call("get_IsSceneLoading") or not damage or damage:call("get_health") <= 0
        or not owner or s.manager:get_field("CurrentTask") ~= owner then
        self.unequip_pending = nil; return
    end
    if manager:call("get_IsPause") then return end
    local hands = self.game:component(s.player, "app.PlayerHands")
    local menu = self.game:singleton("app.MenuManager")
    if not hands or not menu then self.unequip_pending = nil; return end
    if menu:call("isOpenInventoryMenu") or hands:call("get_isDownWeaponActionRequested") then return end
    pending.since = pending.since or os.clock()
    if os.clock() - pending.since > 2.0 then self.unequip_pending = nil; return end
    local name = s.manager:call("getCurrentMotionFsmStateName", 1, false)
    local layer = s.motion:call("getLayer", 1)
    if (name ~= "DownWeapon" and name ~= "Hands.ReadyStart") or layer:call("get_EndFrame") ~= 0.0
        or layer:call("get_MotionID") ~= 0xFFFFFFFF or hands:call("get_actionID") ~= 0 then
        self.unequip_pending = nil; return
    end
    -- Storage can leave an empty transition with no native action delegate.
    -- Confirm it across updates, then retry once through ordinary priority rules.
    if pending.state ~= name then pending.state = name; return end
    self.unequip_pending = nil
    s.controller:call("updateTargetBankType")
    assert(s.motion:call("get_TargetBankType") == 0, "Unexpected unarmed motion bank")
    self:request("Hands.ReadyStart")
end

function Gauntlet:read_track(node, kind, destination)
    local id = node:call("get_MotionID")
    local key = tostring(self.variant.bank) .. ":" .. tostring(id) .. kind
    local index = self.track_indices[key]
    if index == nil then
        index = false
        for i = 0, node:call("get_SequenceTracksCount") - 1 do
            local type = node:call("getSequenceTracksTypeinfo(System.UInt32)", i)
            if type and type:call("get_FullName") == kind then index = i; break end
        end
        self.track_indices[key] = index
    end
    if index == false then return false end
    destination:call("initialize", 1)
    return node:call("getSequenceTracks(System.UInt32, via.motion.Tracks)", index, destination)
end

function Gauntlet:update_motion(name, layer)
    local variant = self.variant
    local ordinary = name == "Melee.AttackL" or name == "Melee.AttackR"
    if name ~= self.last_state then
        self:clear_attack()
        self.attack = (ordinary or name == "Melee.AimAttackC") and { pending = true } or nil
        if name == "Melee.ReadyToAim" then self.charge = { id = variant.charge_start, frame = 0.0, level = 0 }
        elseif not AIM[name] and name ~= "Melee.AimAttackC" then self.charge = nil end
        self.last_state = name
    end
    if AIM[name] and self.charge then
        local charge, id = self.charge, layer:call("get_MotionID")
        if id >= 2200 and id <= 2208 then self:change_clip(charge.id, charge.frame) end
        if layer:call("get_MotionID") == charge.id then
            charge.frame = layer:call("get_Frame")
            for i = 0, layer:call("getRawMotionNodeCount") - 1 do
                local node = layer:call("getRawMotionNode", i)
                if node:call("get_MotionID") == charge.id and self:read_track(node,
                    "app.SequenceTrackObject.CH9PlayerGauntletChargeLevel", self.charge_track) then
                    if self.charge_track:get_field("IsChargeLevel2") then charge.level = 2
                    elseif self.charge_track:get_field("IsChargeLevel1") then charge.level = math.max(charge.level, 1) end
                end
            end
            if charge.id == variant.charge_start and layer:call("get_StateEndOfMotion") then
                charge.id, charge.frame = variant.charge_loop, 0.0
                self:change_clip(variant.charge_loop)
                self:request("Melee.AimIdle")
            end
        end
    end
    local attack, id = self.attack, layer:call("get_MotionID")
    if attack and attack.pending then
        if name == "Melee.AimAttackC" and id == 2407 then
            local entry = variant.charge[(self.charge and self.charge.level or 0) + 1]
            self.attack = { id = entry[1], request = entry[2], source = entry[3], charged = true }
            self.charge, self.combo = nil, 0
        elseif ordinary and id == (name == "Melee.AttackL" and 2400 or 2401) then
            self.combo = self.combo % #variant.combo + 1
            local entry = variant.combo[self.combo]
            self.attack = { id = entry[1], request = entry[2], source = entry[3] }
        end
        if self.attack.id then self:change_clip(self.attack.id) end
    end
    attack = self.attack
    local active = false
    if attack and attack.id and not attack.finished and not attack.interrupted then
        for i = 0, layer:call("getRawMotionNodeCount") - 1 do
            local node = layer:call("getRawMotionNode", i)
            if node:call("get_MotionID") == attack.id and node:call("get_Weight") > 0.5
                and self:read_track(node, "app.Collision.ColliderTrack", self.collider_track) then
                active = active or self.collider_track:get_field("RequestId") == attack.source
            end
        end
    end
    if active and not self.active then self.weapon:call("onAttackTrigger"); self.active = true
    elseif not active and self.active then self:clear_attack() end
    if active then self.hit:call("requestCollider", attack.request) end
    if attack and attack.id and not attack.finished and layer:call("get_MotionID") == attack.id
        and layer:call("get_StateEndOfMotion") then
        self:clear_attack()
        attack.finished = true
        self:request(attack.charged and "Melee.AimToReady" or name .. "ToReady")
    end
end

function Gauntlet:update()
    local player = self.game:player()
    if not self.enabled() or self.error or not self:matches(player) then
        self:reset(); self.waiting = nil; return
    end
    local manager = self.game:singleton("app.GameManager")
    local ready, reason = self:prepare(player)
    if not ready then
        local now = os.clock()
        if not self.waiting or self.waiting.player ~= player or not manager or manager:call("get_IsSceneLoading") then
            self.waiting = { player = player, since = now }
        end
        self.waiting.reason = reason
        assert(now - self.waiting.since < 10.0, "Gauntlet readiness timed out: " .. reason)
        return
    end
    self.waiting = nil
    local s = self.session
    local weapon = self.game:component(player, "app.EquipManager"):call("get_equipWeaponRight")
    local variant
    for _, entry in ipairs(s.variants) do
        if weapon and weapon:get_field("WeaponID") == entry.weapon and self:matches(player, entry) then variant = entry; break end
    end
    if not variant then
        local pending = self.weapon and {} or self.unequip_pending
        self:reset()
        if not weapon and s.controller:get_field("CurrentWeaponID") == 0 then
            self.unequip_pending = pending
            self:recover_unequip(manager)
        end
        return
    end
    local damage = self.game:component(player, "app.PlayerDamageController")
    local owner = s.manager:get_field("OwnerTask")
    self.controllable = manager and not manager:call("get_IsPause") and not manager:call("get_IsSceneLoading")
        and damage and damage:call("get_health") > 0 and owner ~= nil and s.manager:get_field("CurrentTask") == owner
    if not self.controllable then
        self:clear_attack()
        if self.attack then self.attack.interrupted = true end
        return
    end
    if self.weapon ~= weapon then self:equip(weapon, variant); self.controllable = true end
    local name, layer = s.manager:call("getCurrentMotionFsmStateName", 1, false), s.motion:call("getLayer", 1)
    self:recover_equip(name, layer)
    self:update_motion(name, layer)
end

function Gauntlet:scope(controller)
    local s = self.session
    if not (s and self.enabled() and not self.error and s.player == self.game:player() and controller == s.controller) then return false end
    for _, variant in ipairs(s.variants) do
        if controller:get_field("CurrentWeaponID") == variant.weapon then return true end
    end
    return false
end

function Gauntlet:install_guard()
    if self.guard_installed then return end
    self.guard_installed = true
    local key = {}
    self.game:hook("app.PlayerDamageController", "get_guardDamageCutRate", function(args)
        local storage = thread.get_hook_storage()
        storage[key] = nil
        local s, variant = self.session, self.variant
        if not (s and variant and self.weapon and self:scope(s.controller)
            and s.controller:get_field("CurrentWeaponID") == variant.weapon
            and self:matches(s.player, variant) and self.weapon:call("get_Valid")) then return end
        if self.game:object(args[2]) ~= self.game:component(s.player, "app.PlayerDamageController") then return end
        if self.game:component(s.player, "app.EquipManager"):call("get_equipWeaponRight") ~= self.weapon then return end
        storage[key] = variant.guard
    end, function(ret)
        local storage = thread.get_hook_storage()
        local bonus = storage[key]; storage[key] = nil
        -- CH9 adds to the already computed base/passive reduction, then clamps.
        return bonus and sdk.float_to_ptr(math.max(0.0, math.min(1.0, sdk.to_float(ret) + bonus))) or ret
    end)
end

function Gauntlet:install()
    if self.installed then return end
    self.installed = true
    self:install_guard()
    local bank_key, sequence_key, tag_key = {}, {}, {}
    self.game:hook("app.PlayerMotionController", "getBankType(app.WeaponID)", function(args)
        thread.get_hook_storage()[bank_key] = nil
        local controller = self.game:object(args[2])
        if self.session and controller == self.session.controller and self:owns_banks() then
            for _, variant in ipairs(self.session.variants) do
                if sdk.to_int64(args[3]) == variant.weapon then thread.get_hook_storage()[bank_key] = variant.bank; break end
            end
        end
    end, function(ret)
        local storage = thread.get_hook_storage()
        local bank = storage[bank_key]; storage[bank_key] = nil
        return bank and sdk.to_ptr(bank) or ret
    end)
    self.game:hook("app.PlayerMotionController", "updateLArmMotion()", function(args)
        local controller = self.game:object(args[2])
        if not self:scope(controller) then return end
        local work = controller:get_field("MotionWorks"):get_element(1)
        local name = work:get_field("StateName")
        if not BOTH[name] and not AIM[name] then return end
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
    self.game:hook("app.PlayerSequenceManager", "updateActionTrack()", function(args)
        local s = self.session
        thread.get_hook_storage()[sequence_key] = s and self.weapon and self.controllable
            and self:scope(s.controller) and self.game:object(args[2]) == s.sequence
    end, function(ret)
        local storage = thread.get_hook_storage()
        local scoped = storage[sequence_key]; storage[sequence_key] = nil
        if scoped then
            local s = self.session
            local name = s.manager:call("getCurrentMotionFsmStateName", 1, false)
            if name == "Melee.ReadyIdle" or name == "Melee.ReadyIdleSp" or name == "Melee.ReadyMove" then
                s.sequence:set_field("IsAimAccepted", true)
            elseif AIM[name] then
                s.sequence:set_field("IsAimCancelAccepted", true)
                s.sequence:set_field("IsAttackAccepted", true)
            end
        end
        return ret
    end)
    self.game:hook("app.MotionDelegateTagManager", "addTagFromMotion", function(args)
        local s = self.session
        thread.get_hook_storage()[tag_key] = s and self.weapon and self.controllable and self:scope(s.controller)
            and self.game:object(args[2]) == s.manager:get_field("TagManager") and sdk.to_int64(args[3]) == 1
    end, function(ret)
        local storage = thread.get_hook_storage()
        local scoped = storage[tag_key]; storage[tag_key] = nil
        if scoped then
            local s = self.session
            local id = s.motion:call("getLayer", 1):call("get_MotionID")
            if self.variant and (id == self.variant.charge_start or id == self.variant.charge_loop)
                and AIM[s.manager:call("getCurrentMotionFsmStateName", 1, false)] then
                self.aim_tag = self.aim_tag or self.game:static_field("app.PlayerDefine.MotionTag", "MeleeAimIdle")
                s.manager:get_field("TagManager"):call("addTag(System.Int32, System.UInt32, System.Type)",
                    1, self.aim_tag, sdk.typeof("app.PlayerMelee"))
            end
        end
        return ret
    end)
end

return Gauntlet
