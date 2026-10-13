return function()
    local Gauntlet = require("BioRand7/dlc_gauntlet")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            set_field = function(_, key, value) fields[key] = value end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local enabled, name, path = true, "Pl0000", "BioRand/DlcWeaponLab/CH9_WP006/Item.pfb"
    local player = object({}, { get_Name = function() return name end })
    local current_player = player
    local data = object({ ItemPrefab = object({}, { get_Path = function() return path end }) }, {})
    local item_id = "CH9_WP006"
    local items = object({}, { findItemData = function(id) return id == item_id and data or nil end })
    local hooks, storage = {}, {}
    local paused, loading, health = false, false, 500
    local manager = object({}, { get_IsPause = function() return paused end, get_IsSceneLoading = function() return loading end })
    local damage = object({}, { get_health = function() return health end })
    local menu_open, down_requested, hands_action = false, false, 0
    local menu = object({}, { isOpenInventoryMenu = function() return menu_open end })
    local hands = object({}, { get_isDownWeaponActionRequested = function() return down_requested end,
        get_actionID = function() return hands_action end })
    local current_weapon
    local equip = object({}, { get_equipWeaponRight = function() return current_weapon end })
    local game = {
        player = function() return current_player end,
        valid = function() return true end,
        singleton = function(_, kind) return kind == "app.ItemManager" and items or kind == "app.MenuManager" and menu or manager end,
        component = function(_, owner, kind)
            assert(owner == player)
            return kind == "app.EquipManager" and equip or kind == "app.PlayerDamageController" and damage
                or kind == "app.PlayerHands" and hands
        end,
        object = function(_, value) return value end,
        hook = function(_, kind, method, pre, post) hooks[kind .. ":" .. method] = {pre, post} end,
    }
    local adapter = Gauntlet.new(game, function() return enabled end, "BioRand/DlcWeaponLab")
    assert(adapter:matches(player))
    for _, campaign_name in ipairs({"Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000"}) do
        name = campaign_name; assert(adapter:matches(player))
    end
    for _, foreign_name in ipairs({"Pl1000", "Pl2000_Birthday", "Pl3100_Chapter7_1"}) do
        name = foreign_name; assert(not adapter:matches(player))
    end
    name = "Pl9000"; assert(not adapter:matches(player)); name = "Pl0000"
    path = "BioRand/DlcWeapons/CH9_WP006/Item.pfb"; assert(not adapter:matches(player))
    path = "BioRand/DlcWeaponLab/CH9_WP006/Item.pfb"
    assert(not adapter:matches({}) and not adapter:matches(nil))

    local creations, releases, refs, fail = 0, 0, 0, false
    sdk = { to_int64 = function(value) return value end, to_ptr = function(value) return value end,
        is_managed_object = function(value) return not value.stale end,
        create_instance = function(kind)
            assert(kind == "via.motion.MotionInfo")
            return object({}, { get_MotionEndFrame = function() return 284.0 end })
        end,
        to_float = function(value) return value.float end, float_to_ptr = function(value) return { float = value } end,
        PreHookResult = { SKIP_ORIGINAL = "skip" },
        create_resource = function(kind, resource_path)
            creations = creations + 1
            return { add_ref = function(self) refs = refs + 1; return self end,
                release = function() releases = releases + 1 end,
                create_holder = function(_, holder_kind)
                    assert(holder_kind == kind .. "Holder")
                    if fail then error("holder failed") end
                    return object({}, { get_ResourcePath = function() return resource_path end })
                end }
        end }
    thread = { get_hook_storage = function() return storage end }
    local resource = adapter:resource("Resource", "test/path")
    assert(adapter:resource("Resource", "test/path") == resource and creations == 1 and releases == 1 and refs == 1)
    fail = true
    assert(not pcall(function() adapter:resource("Resource", "other/path") end))
    assert(releases == 2 and not adapter.resources["Resource:other/path"], "Release temporary resource on failure")

    local motion_id, ended, frame, source, charge_level = 2001, false, 0.0, -1, 0
    local end_frame, idle_ready, target_bank, bank_updates = 284.0, true, 9910, 0
    local change_count, on_count, off_count, collider_requests, cleared, returns = 0, 0, 0, {}, {}, {}
    local node = object({}, {
        get_MotionID = function() return motion_id end, get_Weight = function() return 1.0 end,
        get_SequenceTracksCount = function() return 2 end,
        ["getSequenceTracksTypeinfo(System.UInt32)"] = function(index)
            return object({}, { get_FullName = function() return index == 0 and "app.Collision.ColliderTrack" or "app.SequenceTrackObject.CH9PlayerGauntletChargeLevel" end })
        end,
        ["getSequenceTracks(System.UInt32, via.motion.Tracks)"] = function(index, destination)
            if index == 0 then destination:set_field("RequestId", source)
            else destination:set_field("IsChargeLevel1", charge_level == 1); destination:set_field("IsChargeLevel2", charge_level == 2) end
            return true
        end,
    })
    local layer = object({}, {
        get_MotionID = function() return motion_id end, get_StateEndOfMotion = function() return ended end,
        get_EndFrame = function() return end_frame end,
        get_Frame = function() return frame end, getRawMotionNodeCount = function() return 1 end,
        getRawMotionNode = function() return node end,
        ["changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"] = function(bank, id, start, interpolation)
            assert(bank == 0 and math.type(start) == "float" and math.type(interpolation) == "float")
            motion_id, frame, ended = id, start, false
            change_count = change_count + 1
        end,
    })
    local banks = {}
    for i = 1, 3 do banks[i] = {index = i, kind = 9910, bank = object({}, { get_BankID = function() return 0 end, get_BankType = function() return 9910 end })} end
    local motion = object({}, { getLayer = function() return layer end,
        get_TargetBankType = function() return target_bank end,
        ["getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)"] = function(bank, kind, id, info)
            assert(not info.stale, "Never reuse expired motion scratch")
            assert(bank == 0 and kind == 9910 and id == 2001)
            return idle_ready
        end,
        getDynamicMotionBank = function(index) return banks[index] and banks[index].bank end })
    local state_name = "Melee.ReadyIdle"
    local task = {}
    local manager_fields = { OwnerTask = task, CurrentTask = task }
    local motion_manager = object(manager_fields, { getCurrentMotionFsmStateName = function() return state_name end })
    local controller_fields = { CurrentWeaponID = 67 }
    local controller = object(controller_fields, {
        updateTargetBankType = function() bank_updates = bank_updates + 1 end,
        ["requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"] = function(state, index, start, interpolation, priority)
            assert(index == 1 and priority == 0 and math.type(start) == "float" and math.type(interpolation) == "float")
            returns[#returns + 1] = state
        end,
    })
    local weapon_valid, collider_valid = true, true
    local weapon = object({ WeaponID = 67 }, {
        get_Valid = function() return weapon_valid end,
        get_GameObject = function() return {} end,
        onAttackTrigger = function() on_count = on_count + 1 end,
        offAttackTrigger = function() off_count = off_count + 1 end,
    })
    current_weapon, adapter.weapon = weapon, weapon
    adapter.collider = object({}, { get_Valid = function() return collider_valid end,
        ["unregisterRequestSet(System.UInt32)"] = function(index)
            assert(collider_valid, "Never access a destroyed collider")
            cleared[index] = true
        end })
    adapter.hit = object({}, { requestCollider = function(index) collider_requests[#collider_requests + 1] = index end })
    adapter.collider_track = object({}, { initialize = function() end })
    adapter.charge_track = object({}, { initialize = function() end })
    adapter.variant = Gauntlet.variants[1]
    adapter.session = { player = player, motion = motion, controller = controller, manager = motion_manager,
        banks = banks, bank_count = 3, variants = {adapter.variant}, hands = {} }
    assert(adapter:owns_banks())
    adapter:prepare(player)
    adapter:install(); adapter:install()
    local bank_hook = hooks["app.PlayerMotionController:getBankType(app.WeaponID)"]
    bank_hook[1]({nil, controller, 67}); assert(bank_hook[2](10) == 9910 and next(storage) == nil)
    bank_hook[1]({nil, controller, 1}); assert(bank_hook[2](10) == 10)
    bank_hook[1]({nil, {}, 67}); assert(bank_hook[2](10) == 10)
    assert(adapter:scope(controller))
    enabled = false; assert(not adapter:scope(controller)); enabled = true
    current_player = nil; assert(not adapter:owns_banks() and not adapter:scope(controller)); current_player = player

    local guard_hook = hooks["app.PlayerDamageController:get_guardDamageCutRate"]
    local function guard(base, receiver)
        local original = sdk.float_to_ptr(base)
        guard_hook[1]({nil, receiver or damage})
        local result = guard_hook[2](original)
        assert(next(storage) == nil, "Guard hook must clear its per-call storage")
        return sdk.to_float(result), result == original
    end
    assert(math.abs(guard(0.75) - 0.9) < 0.00001)
    assert(guard(0.95) == 1.0 and guard(-1.0) == 0.0, "Clamp without changing the stored base stat")
    local value, unchanged = guard(0.75, {}); assert(value == 0.75 and unchanged)
    enabled = false; assert(guard(0.75) == 0.75); enabled = true
    adapter.error = "test"; assert(guard(0.75) == 0.75); adapter.error = nil
    current_player = nil; assert(guard(0.75) == 0.75); current_player = player
    current_weapon = {}; assert(guard(0.75) == 0.75); current_weapon = weapon
    controller_fields.CurrentWeaponID = 61; assert(guard(0.75) == 0.75); controller_fields.CurrentWeaponID = 67
    weapon_valid = false; assert(guard(0.75) == 0.75); weapon_valid = true
    path = "CH9/Vanilla.pfb"; assert(guard(0.75) == 0.75); path = "BioRand/DlcWeaponLab/CH9_WP006/Item.pfb"
    adapter:install_guard(); assert(hooks["app.PlayerDamageController:get_guardDamageCutRate"] == guard_hook)

    local function update(name, id)
        state_name, motion_id, ended = name, id or motion_id, false
        adapter:update()
    end
    local expected = { {2441, 0, 43}, {2641, 1, 40}, {2443, 2, 45}, {2643, 6, 42} }
    for _, entry in ipairs(expected) do
        update("Melee.ReadyIdle", 2001)
        source = -1
        update("Melee.AttackL", 2400)
        assert(motion_id == entry[1] and adapter.attack.request == entry[2] and not adapter.active)
        source = 999; adapter:update(); assert(not adapter.active, "Only the matching native hit track may activate damage")
        source = entry[3]; adapter:update(); adapter:update()
        assert(adapter.active and collider_requests[#collider_requests] == entry[2])
        source = -1; adapter:update(); assert(not adapter.active and cleared[6])
        ended = true; adapter:update(); adapter:update()
        assert(returns[#returns] == "Melee.AttackLToReady")
    end
    assert(#returns == 4 and on_count == 4 and change_count == 12, "One finite completion per punch; both arms and linked layer")

    update("Melee.ReadyIdle", 2001)
    update("Melee.ReadyToAim", 2205)
    assert(motion_id == 2688)
    charge_level = 2; adapter:update(); assert(adapter.charge.level == 2)
    ended = true; adapter:update(); assert(motion_id == 2689 and returns[#returns] == "Melee.AimIdle")
    update("Melee.AimIdle", 2200); assert(motion_id == 2689 and adapter.charge.level == 2)
    update("Melee.AimAttackC", 2407)
    assert(motion_id == 2691 and adapter.attack.request == 5 and not adapter.charge)
    source = 33; adapter:update(); assert(adapter.active)
    paused = true; adapter:update(); assert(not adapter.active and adapter.attack.interrupted)
    paused = false; adapter:update(); assert(not adapter.active, "Resuming an interrupted damage window must not hit again")
    ended = true; adapter:update(); assert(returns[#returns] == "Melee.AimToReady")
    update("Melee.ReadyIdle", 2001)
    charge_level = 0; update("Melee.ReadyToAim", 2205)
    update("Melee.AimAttackC", 2407); assert(motion_id == 2690 and adapter.attack.request == 4)
    source = 32; adapter:update(); assert(adapter.active)
    manager_fields.CurrentTask = {}; adapter:update(); assert(not adapter.active and not adapter.controllable)
    manager_fields.CurrentTask = task
    health = 0; adapter:update(); assert(not adapter.controllable)
    health, loading = 500, true; adapter:update(); assert(not adapter.controllable)
    loading, manager_fields.OwnerTask = false, nil; adapter:update(); assert(not adapter.controllable)
    manager_fields.OwnerTask = task
    local expired_info = { stale = true }
    adapter.motion_info = expired_info
    local before_recovery = #returns
    adapter.equip_pending, end_frame, idle_ready = true, 0.0, false
    update("Melee.ReadyStart", 2000)
    assert(adapter.motion_info ~= expired_info, "Refresh scratch within an unchanged player session")
    assert(adapter.equip_pending and #returns == before_recovery, "Wait for the actual idle clip")
    idle_ready, paused = true, true
    adapter:update(); assert(#returns == before_recovery, "Never recover while paused")
    paused = false
    adapter:update(); adapter:update()
    assert(#returns == before_recovery + 1 and returns[#returns] == "Melee.ReadyIdle" and bank_updates == 1,
        "One normal-priority request recovers a cold empty equip")
    adapter.equip_pending, end_frame = true, 30.0
    adapter:update(); assert(not adapter.equip_pending and #returns == before_recovery + 1, "Preserve a valid equip animation")
    adapter.equip_pending, end_frame = true, 0.0
    update("Melee.GuardStart", 2000)
    assert(not adapter.equip_pending and #returns == before_recovery + 1, "Never replace another native action")
    -- Each family member uses its own native markers and request-set export.
    for _, variant in ipairs({Gauntlet.variants[2], Gauntlet.variants[3]}) do
        item_id, path = variant.item, "BioRand/DlcWeaponLab/" .. variant.item .. "/Item.pfb"
        adapter.variant, adapter.session.variants = variant, {variant}
        weapon:set_field("WeaponID", variant.weapon)
        controller_fields.CurrentWeaponID = variant.weapon
        adapter.combo = 0
        assert(variant.combo[3][3] == 17, "Knuckle uppercut differs from Dual's BodyblowR marker")
        assert(adapter:matches(player) and adapter:scope(controller))
        assert(math.abs(guard(0.75) - 0.8) < 0.00001, "Single gauntlets preserve their five-point bonus")
        bank_hook[1]({nil, controller, variant.weapon}); assert(bank_hook[2](0) == variant.bank)
        bank_hook[1]({nil, controller, 67}); assert(bank_hook[2](0) == 0, "Do not map an absent variant")
        for _, entry in ipairs(variant.combo) do
            update("Melee.ReadyIdle", 2001)
            source = -1; update("Melee.AttackR", 2401)
            assert(motion_id == entry[1] and adapter.attack.request == entry[2] and not adapter.active)
            source = entry[3]; adapter:update()
            assert(adapter.active and collider_requests[#collider_requests] == entry[2])
            ended = true; adapter:update(); assert(not adapter.active)
        end
        for level = 0, 2 do
            update("Melee.ReadyIdle", 2001)
            source, charge_level = -1, level
            update("Melee.ReadyToAim", 2205); adapter:update()
            assert(motion_id == 2660 and adapter.charge.level == level)
            ended = true; adapter:update(); assert(motion_id == 2661)
            update("Melee.AimIdle", 2200)
            update("Melee.AimAttackC", 2407)
            local entry = variant.charge[level + 1]
            assert(motion_id == entry[1] and adapter.attack.request == entry[2] and adapter.attack.source == entry[3])
            source = entry[3]; adapter:update(); assert(adapter.active)
            ended = true; adapter:update(); assert(not adapter.active and returns[#returns] == "Melee.AimToReady")
        end
    end
    item_id, path = "CH9_WP006", "BioRand/DlcWeaponLab/CH9_WP006/Item.pfb"
    weapon:set_field("WeaponID", 67); controller_fields.CurrentWeaponID = 67
    adapter.variant, adapter.session.variants = Gauntlet.variants[1], {Gauntlet.variants[1]}
    local old_session = adapter.session
    local restored_mesh, restored_material, restored_parts = false, false, {}
    local original, original_material = {}, {}
    old_session.hands = { { object = {}, original = original, original_material = original_material,
        parts = {true, false, false}, mesh = object({}, {
            get_Valid = function() return true end,
            setMesh = function(value) assert(value == original); restored_mesh = true end,
            set_Material = function(value) assert(value == original_material); restored_material = true end,
            setPartsEnable = function(index, value) restored_parts[index] = value end,
        }) } }
    collider_valid = false
    local before_off = off_count
    adapter:clear_attack()
    assert(off_count == before_off + 1, "A destroyed collider does not prevent a live weapon trigger from clearing")
    weapon_valid, collider_valid, cleared = false, true, {}
    adapter:clear_attack()
    assert(cleared[6] and off_count == before_off + 1, "Clear a surviving collider independently of its weapon")
    collider_valid = false
    adapter.motion_info = { stale = true }
    adapter:reset(); assert(not adapter.weapon and not adapter.attack and not adapter.charge and adapter.session == old_session)
    assert(adapter.motion_info == nil, "Do not retain MotionInfo across scene/weapon resets")
    assert(restored_mesh and restored_material and restored_parts[0] and restored_parts[1] == false
        and not old_session.hands[1].original, "Storage destroys the weapon but must still restore Ethan's hands")
    adapter:reset()
    assert(adapter:owns_banks(), "Do not remove banks while the native player may still refer to them")
    local before_unequip = #returns
    adapter.weapon, current_weapon, controller_fields.CurrentWeaponID, target_bank = weapon, nil, 0, 0
    motion_id, end_frame, state_name, menu_open = 0xFFFFFFFF, 0.0, "DownWeapon", true
    adapter:update(); adapter:update()
    assert(adapter.unequip_pending and #returns == before_unequip, "Defer unequip recovery until the item box closes")
    menu_open, down_requested = false, true
    adapter:update(); assert(adapter.unequip_pending and #returns == before_unequip, "Respect genuine lowered-weapon requests")
    down_requested, paused = false, true
    adapter:update(); assert(#returns == before_unequip, "No unarmed recovery while paused")
    paused = false
    adapter:update(); assert(#returns == before_unequip, "Observe an empty transition across two updates")
    adapter:update(); adapter:update()
    assert(#returns == before_unequip + 1 and returns[#returns] == "Hands.ReadyStart" and not adapter.unequip_pending,
        "Only one ordinary request may repair the post-storage transition")
    adapter.unequip_pending, end_frame = {}, 20.0
    adapter:update(); assert(not adapter.unequip_pending and #returns == before_unequip + 1, "Preserve finite native transitions")
    adapter.unequip_pending, end_frame, hands_action = {}, 0.0, 13
    adapter:update(); assert(not adapter.unequip_pending, "Do not replace an executing native hands action")
    adapter.unequip_pending, hands_action, state_name = {}, 0, "Hands.GuardStart"
    adapter:update(); assert(not adapter.unequip_pending, "Never replace another unarmed action")
    adapter.unequip_pending, state_name, manager_fields.CurrentTask = {}, "DownWeapon", {}
    adapter:update(); assert(not adapter.unequip_pending, "External tasks cancel pending recovery")
    manager_fields.CurrentTask = task
    adapter.unequip_pending, loading = {}, true
    adapter:update(); assert(not adapter.unequip_pending, "Loading cancels pending recovery")
    loading = false
    adapter.unequip_pending, health = {}, 0
    adapter:update(); assert(not adapter.unequip_pending, "Death cancels pending recovery")
    health = 500
    adapter.unequip_pending = { since = os.clock() - 3.0 }
    adapter:update(); assert(not adapter.unequip_pending, "Recovery has a bounded active window")
    adapter.unequip_pending, current_weapon = {}, object({ WeaponID = 3 }, {})
    adapter:update(); assert(not adapter.unequip_pending, "Another weapon cancels pending recovery")
    banks[2] = nil
    assert(not adapter:owns_banks())
    assert(not pcall(function() adapter:prepare(player) end), "Never silently reappend after ownership loss")

    local component = game.component
    local early_controller, early_sequence
    game.component = function(_, _, kind)
        if kind == "app.PlayerMotionController" then return early_controller end
        if kind == "app.PlayerSequenceManager" then return early_sequence end
    end
    local pending = Gauntlet.new(game, function() return enabled end, "BioRand/DlcWeaponLab")
    local created_before = creations
    local ready, reason = pending:prepare(player)
    assert(not ready and reason == "player motion components" and not pending.session)
    early_sequence = {}
    early_controller = object({ MotionManager = {}, Motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function() return nil end,
    }) }, {})
    ready, reason = pending:prepare(player)
    assert(not ready and reason == "campaign axe fallback bank" and creations == created_before,
        "Normal startup must not allocate resources or append banks before native readiness")
    local clock, now = os.clock, 1.0
    os.clock = function() return now end
    loading = true
    pending:update(); now = 100.0; pending:update()
    assert(pending.waiting.since == now, "A long native scene load is not an adapter failure")
    loading = false
    now = 101.0; pending:update()
    assert(pending.waiting.reason == "campaign axe fallback bank")
    now = 111.0
    assert(not pcall(function() pending:update() end), "Missing post-load dependencies must time out")
    enabled = false; pending:update(); assert(not pending.waiting)
    enabled = true
    local hand_ready, material_ready = false, false
    local early_hand = {}
    local hand_mesh = object({}, {
        get_GameObject = function() return early_hand end,
        getMesh = function() return {} end, get_Material = function() return {} end,
        get_MeshReady = function() return hand_ready end, get_MaterialReady = function() return material_ready end,
    })
    local fallback = object({}, { get_BankID = function() return 0 end, get_BankType = function() return 10 end,
        get_MotionList = function() return {} end })
    early_controller = object({ MotionManager = {}, Motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function(_, bank) return bank == 10 and fallback or nil end,
    }) }, {})
    local early_components = game.component
    game.component = function(self, owner, kind)
        if kind == "app.PlayerMeshController" then return object({ RArmMesh = hand_mesh }, {}) end
        return early_components(self, owner, kind)
    end
    ready, reason = pending:prepare(player)
    assert(not ready and reason == "RArmMesh resources" and creations == created_before)
    hand_ready = true
    ready, reason = pending:prepare(player)
    assert(not ready and reason == "RArmMesh resources" and creations == created_before,
        "Do not replace a native hand before both its mesh and material finish loading")
    os.clock, game.component = clock, component
end
