return function()
    local Grenade = require("BioRand7/dlc_grenade")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            set_field = function(_, key, value) fields[key] = value end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local now, enabled, chapter, player_name = 0.0, true, 3, "Pl0000"
    local path = "BioRand/DlcWeapons/Grenadebomb/Item.pfb"
    local paused, loading, menu_open, health = false, false, false, 500
    local motion_id, frame, ended, name = 2001, 0.0, false, "Melee.ReadyIdle"
    local motion_ready, end_frame = true, 284.0
    local held, force, stock, infinite, valid, pool_ready, available = false, false, 3, false, true, true, true
    local events, requests, hooks, storage, resets, pool_calls = {}, {}, {}, {}, 0, {}
    local weapon, current_weapon
    os.clock = function() return now end
    thread = { get_hook_storage = function() return storage end }
    sdk = { to_int64 = function(value) return value end, to_ptr = function(value) return value end,
        is_managed_object = function(value) return not value.stale end,
        PreHookResult = { SKIP_ORIGINAL = "skip" },
        create_instance = function(kind)
            assert(kind == "via.motion.MotionInfo")
            return object({}, { get_MotionEndFrame = function() return motion_ready and 50.0 or 0.0 end })
        end }
    local player = object({}, { get_Name = function() return player_name end })
    local current_player = player
    local data = object({ ItemPrefab = object({}, { get_Path = function() return path end }) }, {})
    local items = object({}, { findItemData = function(id) return id == "Grenadebomb" and data or nil end })
    local gm = object({}, { get_IsPause = function() return paused end, get_IsSceneLoading = function() return loading end })
    local menu = object({}, { isOpenInventoryMenu = function() return menu_open end })
    local damage = object({}, { get_health = function() return health end })
    local equip = object({}, { get_equipWeaponRight = function() return current_weapon end })
    local input = object({}, { isRequested = function(id) assert(id == 9); return held end })
    local pitch = 0.0
    local camera = object({}, { getCameraTransformRotation = function() return { w = math.cos(pitch / 2), x = math.sin(pitch / 2), y = 0.0, z = 0.0 } end })
    local status = object({}, { get_isCrouch = function() return false end })
    local inventory = object({}, {
        ["reduceItem(System.String, System.Int32, System.Boolean)"] = function(id, amount, check)
            assert(id == "Grenadebomb" and amount == 1 and check == false)
            stock = stock - 1
            events[#events + 1] = "consume"
            if stock == 0 then valid = false; current_weapon = nil end
            return true
        end,
    })
    local task = {}
    local manager_fields = { OwnerTask = task, CurrentTask = task }
    local manager = object(manager_fields, { getCurrentMotionFsmStateName = function() return name end })
    local controller = object({ CurrentWeaponID = 58 }, {
        updateTargetBankType = function() events[#events + 1] = "bank" end,
        ["requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"] = function(state, layer, start, blend, priority)
            assert(layer == 1 and priority == 0 and math.type(start) == "float" and math.type(blend) == "float")
            requests[#requests + 1] = state
        end,
    })
    local bank = object({}, { get_BankID = function() return 0 end, get_BankType = function() return 9950 end })
    local banks = {}
    for i = 1, 3 do banks[i] = { bank = bank, index = i, kind = 9950 } end
    local track_missing = false
    local node = object({}, {
        get_MotionID = function() return motion_id end, get_Weight = function() return 1.0 end,
        get_Frame = function() return frame end, get_SequenceTracksCount = function() return 2 end,
        ["getSequenceTracksTypeinfo(System.UInt32)"] = function(index)
            return object({}, { get_FullName = function() return "app.CH8SequenceTrackObject." ..
                (index == 0 and "CH8PlayerPinPulledTrack" or "CH8PlayerThrowTrack") end })
        end,
        ["getSequenceTracks(System.UInt32, via.motion.Tracks, System.Single, System.Single)"] = function(index, track, from, to)
            assert(from == 0.0 and to == frame and math.type(from) == "float" and math.type(to) == "float")
            track:set_field(index == 0 and "IsPinPulled" or "IsThrow", not track_missing and frame >= (index == 0 and 20.0 or 29.0))
            return true
        end,
    })
    local layer = object({}, {
        get_MotionID = function() return motion_id end, get_Frame = function() return frame end,
        get_EndFrame = function() return end_frame end,
        get_StateEndOfMotion = function() return ended end,
        getRawMotionNodeCount = function() return 1 end, getRawMotionNode = function() return node end,
        ["changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"] = function(id, clip, start, blend)
            assert(id == 0 and math.type(start) == "float" and math.type(blend) == "float")
            motion_id, frame, ended = clip, start, false
        end,
    })
    local motion = object({}, { getLayer = function() return layer end,
        ["getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)"] = function(id, kind, clip, info)
            assert(not info.stale, "Never reuse expired motion scratch")
            assert(id == 0 and kind == 9950); return motion_ready
        end,
        getDynamicMotionBank = function(index) return banks[index] and banks[index].bank end })
    weapon = object({ WeaponID = 58, Inventory = inventory }, {
        get_Valid = function() return valid end,
        get_stockNum = function() assert(valid); return stock end,
        get_isBulletStackNumInfinity = function() assert(valid); return infinite end,
        ["onStartStandby(via.GameObject)"] = function(owner) assert(owner == player and valid); events[#events + 1] = "standby" end,
        ["onPinPulled(via.GameObject)"] = function(owner) assert(owner == player and valid); events[#events + 1] = "pin" end,
        ["onUpdateStandby(via.GameObject)"] = function(owner) assert(owner == player and valid); events[#events + 1] = "cook" end,
        ["onStartThrow(via.GameObject, System.Boolean)"] = function(owner, under)
            assert(owner == player and valid)
            events[#events + 1] = under and "underthrow" or "throw"
        end,
        onExitThrow = function() assert(valid, "Never invoke a destroyed native weapon"); events[#events + 1] = "exit" end,
        isForceThrow = function() return force end,
        set_weaponCategory = function(value) assert(value == 0); events[#events + 1] = "category" end,
    })
    weapon.get_type_definition = function() return { get_full_name = function() return "app.CH8WeaponThrowable" end } end
    current_weapon = weapon
    local game = {
        player = function() return current_player end, chapter = function() return chapter end,
        singleton = function(_, kind) return ({ ["app.ItemManager"] = items, ["app.GameManager"] = gm, ["app.MenuManager"] = menu })[kind] end,
        component = function(_, owner, kind)
            assert(owner == player)
            return ({ ["app.EquipManager"] = equip, ["app.PlayerDamageController"] = damage, ["app.PlayerCommandUpdater"] = input,
                ["app.PlayerCamera"] = camera, ["app.PlayerStatus"] = status, ["app.Inventory"] = inventory })[kind]
        end,
        static_field = function(_, kind, field) assert(kind == "app.Command.PlayerCommandID" and field == "AttackRight"); return 9 end,
        object = function(_, value) return value end,
        hook = function(_, kind, method, pre, post) hooks[kind .. ":" .. method] = { pre, post } end,
    }
    local adapter = Grenade.new(game, function() return enabled end, "BioRand/DlcWeapons")
    adapter.pool = {
        reset = function() resets = resets + 1 end,
        update = function(_, owner) pool_calls[#pool_calls + 1] = owner or false; return pool_ready end,
        available = function(_, kind) assert(kind == 0); return available end,
    }
    adapter.session = { player = player, controller = controller, motion = motion, manager = manager,
        inventory = inventory, banks = banks, variants = { Grenade.variants[1] } }
    adapter.pin, adapter.release = object({}, { initialize = function() end }), object({}, { initialize = function() end })
    assert(adapter:matches(player) and adapter:owns_banks())
    for _, other in ipairs({ "Pl1000", "Pl9000", "Pl2000_Birthday" }) do player_name = other; assert(not adapter:matches(player)) end
    for _, campaign in ipairs({ "Pl0000", "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000" }) do player_name = campaign; assert(adapter:matches(player)) end
    for _, campaign in ipairs({ "Pl0000", "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000" }) do
        player_name = campaign
        for flow = 0, 13 do chapter = flow; assert(adapter:matches(player), "Campaign game flow " .. flow) end
    end
    player_name = "Pl0000"
    for _, flow in ipairs({ -1, 14, 15, 16, 17, 18, 19, 20, 21 }) do chapter = flow; assert(not adapter:matches(player)) end
    chapter = nil; assert(not adapter:matches(player)); chapter = 3
    path = "CH8/Vanilla.pfb"; assert(not adapter:matches(player)); path = "BioRand/DlcWeapons/Grenadebomb/Item.pfb"
    adapter:install(); adapter:install()
    local bank_hook = hooks["app.PlayerMotionController:getBankType(app.WeaponID)"]
    bank_hook[1]({ nil, controller, 58 }); assert(bank_hook[2](260) == 9950 and next(storage) == nil)
    bank_hook[1]({ nil, {}, 58 }); assert(bank_hook[2](260) == 260)
    local start_hook = hooks["app.CH8WeaponThrowable:doStart"]
    start_hook[1]({ nil, weapon }); assert(start_hook[2](0) == 0 and events[#events] == "category" and next(storage) == nil)
    chapter = 18; local before = #events; start_hook[1]({ nil, weapon }); start_hook[2](0); assert(#events == before); chapter = 3

    local function update(state, clip, at, complete)
        name, motion_id, frame, ended = state or name, clip or motion_id, at or frame, complete or false
        now = now + 0.1
        adapter:update()
    end
    local function count(event)
        local total = 0; for _, value in ipairs(events) do if value == event then total = total + 1 end end; return total
    end
    local function start()
        update("Melee.ReadyIdle", 2001, 0.0)
        update("Melee.AttackL", 2001, 1.0)
        assert(adapter.pending and not adapter.attack, "Wait until native FSM installs its attack clip")
        update(nil, 2400, 0.0)
        assert(adapter.attack.phase == "standby" and motion_id == 8001)
    end
    weapon:set_field("Inventory", nil)
    before = #events
    update("Melee.ReadyStart", 4294967295, 0.0)
    assert(adapter.owner_wait and not adapter.weapon and #events == before, "Wait for native doStart ownership")
    weapon:set_field("Inventory", inventory)
    motion_ready, end_frame = false, 0.0
    update("Melee.ReadyStart", 4294967295, 0.0)
    assert(adapter.equip_pending and #requests == 0)
    local expired_info = adapter.motion_info
    expired_info.stale = true
    motion_ready = true; update()
    assert(adapter.motion_info ~= expired_info, "Refresh scratch without resetting the player or adapter")
    assert(not adapter.equip_pending and requests[#requests] == "Melee.ReadyIdle")
    local before_ready = #requests; update(); assert(#requests == before_ready, "Repair an empty saved equip only once")
    end_frame = 284.0
    start()
    update(nil, nil, 22.0); assert(adapter.attack.pulled and count("pin") == 1)
    update(nil, nil, 25.0); assert(adapter.attack.phase == "throw" and motion_id == 2400)
    update(nil, nil, 35.0); assert(stock == 2 and count("throw") == 1 and count("consume") == 1)
    update(nil, nil, 40.0); assert(stock == 2 and count("throw") == 1, "Do not replay sampled release tracks")
    update(nil, nil, 56.0, true); assert(not adapter.attack and requests[#requests] == "Melee.AttackLToReady")

    held = true; start()
    update(nil, nil, 35.0); update(nil, nil, 50.0, true)
    assert(adapter.attack.phase == "standby" and motion_id == 8002)
    paused = true; now = now + 100.0; before = #events; update(); assert(#events == before and stock == 2); paused = false
    force, pitch = true, 0.5; update()
    assert(adapter.attack.phase == "throw" and motion_id == 2401)
    update(nil, nil, 35.0); assert(stock == 1 and count("underthrow") == 1)
    update(nil, nil, 56.0, true)
    held, force, pitch = false, false, 0.0

    start(); update(nil, nil, 25.0); update(nil, nil, 35.0)
    assert(stock == 0 and not valid and adapter.empty and adapter.weapon == nil and adapter.attack == nil)
    before = count("exit"); update(nil, nil, 56.0, true)
    assert(not adapter.empty and requests[#requests] == "Hands.ReadyStart" and count("exit") == before)

    valid, current_weapon, stock, infinite = true, weapon, 2, true
    start(); update(nil, nil, 25.0); update(nil, nil, 35.0); assert(stock == 2)
    update(nil, nil, 56.0, true); infinite = false
    start(); update(nil, nil, 22.0)
    before = #requests; manager_fields.CurrentTask = {}
    update(); assert(not adapter.attack and #requests == before and stock == 2, "Never force animation through another task")
    manager_fields.CurrentTask = task
    start(); health = 0; before = #requests; update(); assert(not adapter.attack and #requests == before); health = 500
    start(); before = #requests; update("Damage", 5000, 0.0); assert(not adapter.attack and #requests == before)
    start(); update(nil, 9999, 1.0); assert(not adapter.attack, "Respect external clip replacement")
    start(); menu_open = true; before = #events; update(); assert(#events == before and adapter.attack); menu_open = false
    adapter:reset(); assert(resets == 1 and adapter.attack, "Reset defers native work")
    update(); assert(not adapter.attack and pool_calls[#pool_calls] == false)
    available = false
    update("Melee.ReadyIdle", 2001, 0.0); before = count("standby"); update("Melee.AttackL", 2400, 0.0)
    assert(not adapter.attack and count("standby") == before and stock == 2)
    available = true
    start(); track_missing = true
    local ok, message = pcall(function() update(nil, nil, 50.0, true) end)
    assert(not ok and message:find("Missing grenade pin track") and not adapter.attack and requests[#requests] == "Melee.AttackLToReady")
    track_missing = false
    start(); adapter.attack.age = 8.1
    assert(not pcall(update) and not adapter.attack)
    start(); current_player = nil; before = #events; update()
    assert(not adapter.attack and #events == before, "Do not dereference old-player weapon components")
    current_player = player
    weapon:set_field("Inventory", {})
    local ok, message = pcall(update)
    assert(not ok and message:find("Unexpected grenade inventory owner"), "Never adopt a foreign inventory")
    weapon:set_field("Inventory", nil)
    update("Melee.ReadyIdle", 2001, 0.0)
    adapter.owner_wait.age = 2.1
    ok, message = pcall(update)
    assert(not ok and message:find("inventory owner readiness timed out") and not adapter.weapon)
    enabled = false; update(); assert(not adapter.owner_wait, "Disabled adapters clear pending ownership")
end
