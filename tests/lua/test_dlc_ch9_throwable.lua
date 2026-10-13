return function()
    local Throwable = require("BioRand7/dlc_ch9_throwable")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            set_field = function(_, key, value) fields[key] = value end,
            add_ref = function(self) return self end, release = function() end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local now, enabled, flow, player_name, root = 0.0, true, 3, "Pl0000", "BioRand/DlcWeapons"
    local paused, loading, menu_open, health = false, false, false, 500
    local motion_id, frame, ended, name = 2001, 0.0, false, "Melee.ReadyIdle"
    local variant, stock, weapon_valid, infinite = Throwable.variants[1], 3, true, false
    local pool_ready, available, refuse_shell, motion_ready, end_frame = true, true, false, true, 240.0
    local missing_track, child_id, weight = false, 2406, 1.0
    local events, requests, hooks, storage, banks, resources = {}, {}, {}, {}, {}, {}
    local current_weapon, weapon, inventory, player, current_player, motion, adapter
    local hit_fields, scratch, scratch_invalid, replace_hit, info_clip = {}, {}, false, false, nil
    local first, second, first_pos, second_pos = false, false, nil, nil
    os.clock = function() return now end
    Vector3f = { new = function(x, y, z) return { x = x, y = y, z = z } end }
    local from, dir, rotation = Vector3f.new(0.0, 0.0, 0.0), Vector3f.new(0.0, 0.0, 1.0), {}
    ValueType = { new = function(kind)
        assert(kind == "System.UInt64")
        local value
        return { write_qword = function(_, offset, v) assert(offset == 0); value = v end,
            read_qword = function(_, offset) assert(offset == 0); return replace_hit and 99 or value end }
    end }
    local function native_bank(kind, holder)
        local fields = { BankType = kind, BankID = 0, MotionList = holder }
        local calls = {}
        for _, key in ipairs({ "BankType", "BankID", "MotionList", "OverwriteBankType", "OverwriteBankID" }) do
            calls["get_" .. key] = function() return fields[key] end
            calls["set_" .. key] = function(v) fields[key] = v end
        end
        return object(fields, calls)
    end
    sdk = { to_int64 = function(v) return v end, to_ptr = function(v) return v end,
        is_managed_object = function(v) return not v.stale end,
        PreHookResult = { SKIP_ORIGINAL = "skip" }, find_type_definition = function(kind) return kind end,
        create_resource = function(kind, path)
            assert(kind == "via.motion.MotionListResource")
            resources[#resources + 1] = path
            local holder = object({}, { get_ResourcePath = function() return path end })
            local resource = object({}, {})
            resource.create_holder = function(_, t) assert(t == "via.motion.MotionListResourceHolder"); return holder end
            return resource
        end,
        create_instance = function(kind)
            if kind == "via.motion.DynamicMotionBank" then return native_bank(nil, nil) end
            local value
            if kind == "via.motion.MotionInfo" then
                value = object({}, { get_MotionEndFrame = function()
                    assert(not scratch_invalid)
                    if info_clip == 2400 or info_clip == 2405 then return -1.0 end
                    return motion_ready and 90.0 or 0.0
                end })
            elseif kind == "app.SequenceTrackObject.CH9PlayerActionTrack" then
                value = object({}, { initialize = function(index) assert(index == 1) end })
            elseif kind == "app.Collision.CollisionSystem.HitResult" then
                value = object(hit_fields, {}); value.get_address = function() return 42 end
            else error("Unexpected instance: " .. kind) end
            scratch[#scratch + 1] = value
            return value
        end }
    thread = { get_hook_storage = function() return storage end }
    player = object({}, { get_Name = function() return player_name end })
    current_player = player
    local task = {}
    local manager_fields = { OwnerTask = task, CurrentTask = task }
    local manager = object(manager_fields, { getCurrentMotionFsmStateName = function() return name end })
    local controller_fields = { CurrentWeaponID = 64, MotionManager = manager }
    local controller = object(controller_fields, {
        updateTargetBankType = function() events[#events + 1] = "bank" end,
        ["requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"] = function(state, index, start, blend, priority)
            assert(index == 1 and priority == 0 and math.type(start) == "float" and math.type(blend) == "float")
            requests[#requests + 1] = state
        end,
    })
    local fallback = native_bank(10, {})
    local node = object({}, {
        get_MotionID = function() return child_id end, get_Weight = function() return weight end,
        get_Frame = function() return frame end, get_SequenceTracksCount = function() return 1 end,
        ["getSequenceTracksTypeinfo(System.UInt32)"] = function(index)
            assert(index == 0); return object({}, { get_FullName = function() return "app.SequenceTrackObject.CH9PlayerActionTrack" end })
        end,
        ["getSequenceTracks(System.UInt32, via.motion.Tracks, System.Single, System.Single)"] = function(index, track, start, finish)
            assert(index == 0 and start == 0.0 and finish == frame and math.type(start) == "float" and math.type(finish) == "float")
            track:set_field("IsWepThrowTiming", not missing_track and frame >= (variant.kind == 4 and 27.0 or 32.0))
            return true
        end,
    })
    local layer = object({}, {
        get_MotionID = function() return motion_id end, get_Frame = function() return frame end,
        get_EndFrame = function() return end_frame end, get_StateEndOfMotion = function() return ended end,
        getRawMotionNodeCount = function() return 1 end, getRawMotionNode = function(index) assert(index == 0); return node end,
        ["changeMotion(System.UInt32, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"] = function(id, clip, start, blend)
            assert(id == 0 and math.type(start) == "float" and math.type(blend) == "float")
            motion_id, frame, ended = clip, start, false
        end,
    })
    motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function(id, kind)
            assert(id == 0)
            if kind == 10 then return fallback end
            for _, bank in ipairs(banks) do if bank:call("get_BankType") == kind then return bank end end
        end,
        getDynamicMotionBankCount = function() return #banks end,
        setDynamicMotionBankCount = function(count) assert(count == #banks + 1) end,
        setDynamicMotionBank = function(index, bank) banks[index + 1] = bank end,
        getDynamicMotionBank = function(index) return banks[index + 1] end,
        getLayer = function(index) assert(index == 1 or index == 2 or index == 9); return layer end,
        ["getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)"] = function(id, kind, clip, info)
            assert(not info.stale, "Never pass expired scratch to a native motion call")
            assert(id == 0 and kind == variant.bank and ({ [2001]=true, [2400]=true, [2405]=true, [2406]=true, [2407]=true })[clip])
            info_clip = clip
            return motion_ready
        end,
    })
    controller_fields.Motion = motion
    local items = object({}, { findItemData = function(id)
        if id ~= "CH9_WP003" and id ~= "CH9_WP004" then return nil end
        return object({ ItemPrefab = object({}, { get_Path = function() return root .. "/" .. id .. "/Item.pfb" end }) }, {})
    end })
    inventory = object({}, { getTotalStackNum = function(id, check) assert(id == variant.item and check == false); return stock end })
    local weapon_fields = { Inventory = inventory, WeaponID = 64 }
    local weapon_object = {}
    weapon = object(weapon_fields, {
        get_Valid = function() return weapon_valid end,
        get_GameObject = function() assert(weapon_valid); return weapon_object end,
        set_weaponCategory = function(category) assert(weapon_valid and category == 0); events[#events + 1] = "category" end,
        reduceWeapon = function()
            assert(weapon_valid)
            events[#events + 1] = "consume"
            if not infinite then stock = stock - 1 end
            if stock == 0 then weapon_valid = false; current_weapon = nil end
        end,
    })
    weapon.get_type_definition = function() return { get_full_name = function() return variant.class end } end
    current_weapon = weapon
    local shell = object({}, {
        ["activate(via.vec3, via.Quaternion)"] = function(pos, q) assert(pos and q == rotation); events[#events + 1] = "activate" end,
        get_isActive = function() return true end, get_ownerEquip = function() return player end,
    })
    local native_pool = object({}, {
        ["setupThrowingWeapon(via.GameObject, app.CH9ShellManager.ThrowingWeaponType, System.Int32)"] = function(owner, kind, id)
            assert(owner == player and kind == variant.kind and id == 77)
            return not refuse_shell and shell or nil
        end,
    })
    local collision, gm = {}, object({}, { get_IsPause = function() return paused end, get_IsSceneLoading = function() return loading end })
    local equip = object({}, { get_equipWeaponRight = function() return current_weapon end })
    local menu = object({}, { isOpenInventoryMenu = function() return menu_open end })
    local damage = object({}, { get_health = function() return health end })
    local camera = object({}, {
        get_shootRay = function() return object({ from = from, dir = dir }, {}) end,
        getCameraTransformRotation = function() return rotation end,
    })
    local game = {
        player = function() return current_player end, chapter = function() return flow end,
        valid = function(_, v) return v == weapon_object end,
        object = function(_, v) return v end,
        component = function(_, owner, kind)
            assert(owner == player)
            return ({ ["app.Inventory"] = inventory, ["app.EquipManager"] = equip, ["app.PlayerMotionController"] = controller,
                ["app.PlayerDamageController"] = damage, ["app.PlayerCamera"] = camera })[kind]
        end,
        singleton = function(_, kind)
            return ({ ["app.ItemManager"] = items, ["app.GameManager"] = gm, ["app.MenuManager"] = menu,
                ["app.Collision.CollisionSystem"] = collision, ["app.ShellManager"] = object({}, { makeShellID = function() return 77 end }) })[kind]
        end,
        static_field = function(_, kind, field) assert(kind == "app.Collision.CollisionSystem.Filter"); return field end,
        hook = function(_, kind, method, pre, post)
            assert(not hooks[kind .. ":" .. method], "Duplicate hook")
            hooks[kind .. ":" .. method] = { pre, post }
        end,
        method = function(_, kind, method)
            if method == "doStart" then return { get_function = function() return 123 end } end
            return { call = function(_, receiver, ...)
                local a = { ... }
                if kind == "app.Util" then
                    assert(receiver == nil and a[1] == weapon_object and a[2] == a[3] and weapon_valid)
                    events[#events + 1] = a[2] and "show" or "hide"
                else
                    assert(kind == "app.Collision.CollisionSystem" and receiver == collision and a[1] == from and a[2] == dir)
                    assert(a[3] == 1.0 and math.type(a[3]) == "float")
                    if method:find("via.GameObject", 1, true) then
                        assert(a[4] == player and a[5]:read_qword(0) == (replace_hit and 99 or 42) and a[6] == "DamageCheckDefault" and a[7] == false)
                        hit_fields.Position = first_pos; return first
                    end
                    assert(a[4]:read_qword(0) == 42 and a[5] == "EffectCheckBullet" and a[6] == false)
                    hit_fields.Position = second_pos; return second
                end
            end }
        end,
    }
    adapter = Throwable.new(game, function() return enabled end, "BioRand/DlcWeapons")
    local pool_calls, resets = {}, 0
    adapter.pool = { manager = native_pool, install = function() end,
        reset = function() resets = resets + 1 end,
        update = function(_, p) pool_calls[#pool_calls + 1] = p or false; return pool_ready end,
        available = function(_, kind) assert(kind == variant.kind); return available end }
    assert(adapter:prepare(player) and #banks == 4 and #resources == 2 and adapter:owns_banks())
    assert(adapter:prepare(player) and #banks == 4)
    for _, p in ipairs({ "Pl0000", "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000" }) do
        player_name = p; for f = 0, 13 do flow = f; assert(adapter:matches(player)) end
    end
    for _, p in ipairs({ "Pl1000", "Pl9000", "Pl2000_Birthday" }) do player_name = p; assert(not adapter:matches(player)) end
    player_name = "Pl0000"
    for _, f in ipairs({ -1, 14, 18, 21 }) do flow = f; assert(not adapter:matches(player)) end
    flow = 3; root = "CH9/Vanilla"; assert(not adapter:matches(player)); root = "BioRand/DlcWeapons"
    adapter:install(); adapter:install()
    assert(hooks["app.CH9Weapon1500:doStart"] and not hooks["app.CH9Weapon1800:doStart"], "Shared native body hooked only once")
    local start_hook = hooks["app.CH9Weapon1500:doStart"]
    local bank_hook = hooks["app.PlayerMotionController:getBankType(app.WeaponID)"]
    bank_hook[1]({ nil, controller, 65 }); assert(bank_hook[2](261) == 9961 and next(storage) == nil)
    bank_hook[1]({ nil, {}, 65 }); assert(bank_hook[2](261) == 261)
    local function count(event) local n = 0; for _, e in ipairs(events) do if e == event then n = n + 1 end end; return n end
    local function update(state, clip, at, complete)
        name, motion_id, frame, ended = state or name, clip or motion_id, at or frame, complete or false
        now = now + 0.1; adapter:update()
    end
    local function start(aim)
        update("Melee.ReadyIdle", 2001, 0.0)
        update(aim and "Melee.AimAttackC" or "Melee.AttackL", 2001, 0.0)
        assert(adapter.pending and not adapter.attack)
        update(nil, aim and 2407 or 2400, 0.0)
        assert(adapter.attack and motion_id == (aim and 2405 or 2400))
    end
    assert(adapter:motion_ready(variant))
    local expired = adapter.motion_info
    expired.stale = true
    variant = Throwable.variants[2]
    assert(adapter:motion_ready(variant) and adapter.motion_info ~= expired,
        "Recreate expired scratch when switching variants within the same player session")
    local retained = adapter.motion_info
    adapter.session.ready = {}
    assert(adapter:motion_ready(variant) and adapter.motion_info == retained, "Reuse valid scratch")
    for _, v in ipairs(Throwable.variants) do
        variant = v; weapon_fields.WeaponID = v.weapon; controller_fields.CurrentWeaponID = v.weapon
        adapter.weapon = nil
        start_hook[1]({ nil, weapon }); start_hook[2](0); assert(events[#events] == "category" and next(storage) == nil)
        for _, aim in ipairs({ false, true }) do
            stock, weapon_valid, current_weapon = 3, true, weapon
            start(aim)
            local before = count("activate")
            update(nil, nil, 26.0); assert(count("activate") == before)
            update(nil, nil, 40.0); assert(count("activate") == before + 1 and stock == 2 and adapter.attack.hidden)
            update(nil, nil, 50.0); assert(count("activate") == before + 1 and stock == 2)
            update(nil, nil, 90.0, true)
            assert(not adapter.attack and requests[#requests] == (aim and "Melee.AimToReady" or "Melee.AttackLToReady"))
            assert(events[#events] == "show")
        end
    end
    -- Sample release before completion so a low-FPS frame crossing both still throws once.
    stock = 2; start(true); local before = count("activate"); update(nil, nil, 90.0, true)
    assert(count("activate") == before + 1 and stock == 1 and not adapter.attack)
    start(true); update(nil, nil, 40.0)
    assert(stock == 0 and adapter.empty and not adapter.weapon and not adapter.attack and not weapon_valid)
    before = count("show"); update(nil, nil, 90.0, true)
    assert(not adapter.empty and requests[#requests] == "Hands.ReadyStart" and count("show") == before)
    stock, weapon_valid, current_weapon = 3, true, weapon
    infinite = true; start(false); update(nil, nil, 40.0); assert(stock == 3 and not adapter.empty); update(nil, nil, 90.0, true); infinite = false
    start(false); before = count("activate"); available = false; update(nil, nil, 40.0)
    assert(stock == 3 and not adapter.attack and count("activate") == before); available = true
    refuse_shell = true; start(false); update(nil, nil, 40.0); assert(stock == 3 and not adapter.attack); refuse_shell = false
    start(false); update(nil, nil, 40.0); before = #requests; update("Damage", 5000, 0.0)
    assert(not adapter.attack and #requests == before and events[#events] == "show")
    start(false); manager_fields.CurrentTask = {}; before = #requests; update(); assert(not adapter.attack and #requests == before); manager_fields.CurrentTask = task
    start(false); health = 0; update(); assert(not adapter.attack); health = 500
    start(false); update(nil, 9999, 1.0); assert(not adapter.attack)
    start(false); paused = true; now = now + 100.0; before = #events; update(); assert(#events == before and adapter.attack.age < 1); paused = false
    menu_open = true; update(); assert(#events == before); menu_open = false
    missing_track = true; local ok, err = pcall(function() update(nil, nil, 90.0, true) end)
    assert(not ok and err:find("Missing CH9 release track") and not adapter.attack); missing_track = false
    start(false); child_id = 1234; update(nil, nil, 40.0); assert(not adapter.attack.released); child_id = 2407
    weight = 0.1; update(nil, nil, 40.0); assert(not adapter.attack.released); weight = 1.0
    adapter.attack.age = 5.1; assert(not pcall(update) and not adapter.attack)
    -- Native collision offsets, filters, ref cell, and comparison against the adjusted first hit.
    adapter.variant = variant
    local p, q = adapter:geometry(); assert(p.z == 1.0 and q == rotation)
    first, first_pos = true, Vector3f.new(0.0, 0.0, 0.8)
    p = adapter:geometry(); assert(math.abs(p.z - 0.6) < 0.0001)
    second, second_pos = true, Vector3f.new(0.0, 0.0, 0.7)
    p = adapter:geometry(); assert(math.abs(p.z - 0.6) < 0.0001)
    second_pos.z = 0.4; p = adapter:geometry(); assert(math.abs(p.z - 0.04) < 0.0001)
    first = false; p = adapter:geometry(); assert(math.abs(p.z - 0.04) < 0.0001)
    adapter.variant = Throwable.variants[1]; second = false; p = adapter:geometry(); assert(p.z == 0.1)
    first = true; p = adapter:geometry(); assert(math.abs(p.z + 0.2) < 0.0001)
    replace_hit = true; ok, err = pcall(function() adapter:geometry() end); assert(not ok and err:find("replaced CH9 hit scratch")); replace_hit = false
    first = nil; ok, err = pcall(function() adapter:geometry() end); assert(not ok and err:find("damage ray did not return"))
    first, second = false, nil; ok, err = pcall(function() adapter:geometry() end); assert(not ok and err:find("effect ray did not return"))
    first, second = false, false
    adapter:reset(); assert(resets == 1 and not adapter.hit and not adapter.hit_ref and not adapter.track_object and not adapter.motion_info)
    scratch_invalid = true; update(); assert(pool_calls[#pool_calls] == false); scratch_invalid = false
    weapon_fields.Inventory = nil; update("Melee.ReadyStart", 4294967295, 0.0); assert(adapter.owner_wait and not adapter.weapon)
    adapter.owner_wait.age = 2.1; assert(not pcall(update)); weapon_fields.Inventory = inventory
    adapter.session.ready = {}; motion_ready, end_frame = false, 0.0
    update(); assert(adapter.equip_pending); motion_ready = true; update(); assert(requests[#requests] == "Melee.ReadyIdle"); end_frame = 240.0
    local old = adapter.motion_info; adapter:reset(); assert(not adapter.motion_info); update(); update(); assert(adapter.motion_info == nil or adapter.motion_info ~= old)
    start(false); current_player = nil; before = #events; update(); assert(not adapter.attack and #events == before)
    current_player = player
    adapter.session.player = {}; banks = {}
    adapter.motion_info, adapter.track_object, adapter.hit, adapter.hit_ref = {}, {}, {}, {}
    assert(adapter:prepare(player) and #banks == 4)
    assert(not adapter.motion_info and not adapter.track_object and not adapter.hit and not adapter.hit_ref, "Discard scratch on player replacement")
    banks[1] = native_bank(9960, {}); assert(not adapter:owns_banks())
    bank_hook[1]({ nil, controller, 64 }); assert(bank_hook[2](260) == 260)
    enabled = false; update(); assert(not adapter.weapon)
end
