return function()
    local Item = require("BioRand7/dlc_ch9_item")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            add_ref = function(self) return self end, release = function() end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local now, enabled, name, flow = 0.0, true, "Pl0000", 3
    local root, path = "BioRand/DlcWeapons", "BioRand/DlcWeapons/CH9_WP005/Item.pfb"
    local paused, loading, menu, health = false, false, false, 500
    local native_ready, fallback_ready, duration, state = true, true, 0.0, "Item.ReadyStart"
    local banks, requests, hooks, storage, scratch, clips = {}, {}, {}, {}, {}, {}
    local motion_updates, mirrored, fsm_updates, both = 0, 0, 0, 0
    local player = object({}, { get_Name = function() return name end })
    local current_player, current_weapon, pool_owned = player, nil, true
    local inventory, owner = {}, {}
    local manager_fields = { OwnerTask = owner, CurrentTask = owner }
    local manager = object(manager_fields, { getCurrentMotionFsmStateName = function(layer, partial)
        assert(layer == 1 and partial == false); return state
    end })
    local right_fields = { StateName = "Item.ReadyIdle", StartFrame = 0.0, InterpolationFrame = 4.0,
        InterpolationMode = 2, InterpolationCurve = 1, Priority = 0, IsSet = true }
    local left_fields = { StateName = "Item.ReadyIdle", StartFrame = 0.0, InterpolationFrame = 4.0,
        InterpolationMode = 2, InterpolationCurve = 1, Priority = 0, IsSet = true }
    local works = { get_element = function(_, index)
        assert(index == 1 or index == 2); return object(index == 1 and right_fields or left_fields, {})
    end }
    local fsm = object({}, {
        ["setCurrentStateFullName(System.String, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)"] = function(n, layer, start, blend, mode, curve)
            assert(n == left_fields.StateName and layer == 2 and start == 0.0 and blend == 4.0 and mode == 2 and curve == 1)
            fsm_updates = fsm_updates + 1
        end,
    })
    local controller_fields = { CurrentWeaponID = 66, MotionManager = manager, MotionWorks = works, MotionFsm = fsm }
    local controller = object(controller_fields, {
        updateTargetBankType = function() motion_updates = motion_updates + 1 end,
        ["requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"] = function(n, layer, start, blend, priority)
            assert(layer == 1 and math.type(start) == "float" and math.type(blend) == "float" and priority == 0)
            requests[#requests + 1] = n
        end,
        setMotionWorkCore = function(n, start, blend, mode, curve, priority, layer)
            assert(n == right_fields.StateName and layer == 2 and priority == 0 and mode == 2 and curve == 1)
            assert(start == 0.0 and blend == 4.0); mirrored = mirrored + 1
        end,
        set_isBothHands = function(value) assert(value); both = both + 1 end,
    })
    local function bank(kind, holder)
        local fields, calls = { BankID = 0, BankType = kind, MotionList = holder }, {}
        for _, key in ipairs({ "BankID", "BankType", "MotionList", "OverwriteBankID", "OverwriteBankType" }) do
            calls["get_" .. key] = function() return fields[key] end
            calls["set_" .. key] = function(v) fields[key] = v end
        end
        return object(fields, calls)
    end
    local fallback = bank(200, {})
    local motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function(id, kind)
            assert(id == 0)
            if kind == 200 then return fallback_ready and fallback or nil end
            assert(kind == 9962)
            for _, value in ipairs(banks) do if value:call("get_BankType") == kind then return value end end
        end,
        getDynamicMotionBankCount = function() return #banks end,
        setDynamicMotionBankCount = function(count) assert(count == #banks + 1) end,
        setDynamicMotionBank = function(index, value) banks[index + 1] = value end,
        getDynamicMotionBank = function(index) return banks[index + 1] end,
        getLayer = function(index) assert(index == 1); return object({}, { get_EndFrame = function() return duration end }) end,
        ["getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)"] = function(id, kind, clip, info)
            assert(id == 0 and kind == 9962 and not info.stale)
            clips[#clips + 1] = clip; return native_ready
        end,
    })
    controller_fields.Motion = motion
    local weapon_fields = { WeaponID = 66, Inventory = inventory }
    local weapon_valid, weapon_type = true, "app.CH9Weapon1900"
    local weapon = object(weapon_fields, { get_Valid = function() return weapon_valid end })
    weapon.get_type_definition = function() return { get_full_name = function() return weapon_type end } end
    current_weapon = weapon
    local item_fields = { WeaponItem = weapon }
    local player_item = object(item_fields, {})
    local gm = object({}, { get_IsPause = function() return paused end, get_IsSceneLoading = function() return loading end })
    local items = object({}, { findItemData = function(id)
        assert(id == "CH9_WP005")
        return object({ ItemPrefab = object({}, { get_Path = function() return path end }) }, {})
    end })
    local game = {
        player = function() return current_player end, chapter = function() return flow end,
        object = function(_, value) return value end,
        singleton = function(_, kind)
            return ({ ["app.ItemManager"] = items, ["app.GameManager"] = gm,
                ["app.MenuManager"] = object({}, { isOpenInventoryMenu = function() return menu end }) })[kind]
        end,
        component = function(_, p, kind)
            assert(p == player)
            return ({ ["app.Inventory"] = inventory, ["app.PlayerItem"] = player_item, ["app.PlayerMotionController"] = controller,
                ["app.PlayerDamageController"] = object({}, { get_health = function() return health end }),
                ["app.EquipManager"] = object({}, { get_equipWeaponRight = function() return current_weapon end }) })[kind]
        end,
        hook = function(_, kind, method, pre, post)
            local key = kind .. ":" .. method
            assert(not hooks[key]); hooks[key] = { pre, post }
        end,
    }
    os.clock = function() return now end
    sdk = { PreHookResult = { SKIP_ORIGINAL = "skip" }, to_ptr = function(v) return v end, to_int64 = function(v) return v end,
        is_managed_object = function(v) return not v.stale end,
        create_resource = function(kind, resource_path)
            assert(kind == "via.motion.MotionListResource" and resource_path == "CH9/Animation/Player/pl9000/motlist/pl9000_LiquidBomb.motlist")
            local resource = object({}, {})
            resource.create_holder = function(_, t)
                assert(t == "via.motion.MotionListResourceHolder")
                return object({}, { get_ResourcePath = function() return resource_path end })
            end
            return resource
        end,
        create_instance = function(kind)
            if kind == "via.motion.DynamicMotionBank" then return bank(nil, nil) end
            assert(kind == "via.motion.MotionInfo")
            local info = object({}, { get_MotionEndFrame = function() return native_ready and 40.0 or 0.0 end })
            scratch[#scratch + 1] = info; return info
        end,
    }
    thread = { get_hook_storage = function() return storage end }
    -- Deliberately no pool update/reset/available or inventory stock mutation APIs.
    local pool = { ready = true, player = player, owned = function() return pool_owned end }
    local adapter = Item.new(game, function() return enabled end, root, pool)
    adapter:install(); adapter:install()
    local getbank = hooks["app.PlayerMotionController:getBankType(app.WeaponID)"]
    local use = hooks["app.PlayerItem:use"]
    local try = hooks["app.PlayerItem:tryUse"]
    local arms = hooks["app.PlayerMotionController:updateLArmMotion()"]
    local function blocked()
        assert(adapter:block_use(player_item))
        for _, hook in ipairs({ use, try }) do
            assert(hook[1]({ nil, player_item }) == "skip" and hook[2](true) == 0)
        end
    end
    blocked()
    adapter:update()
    assert(#banks == 2 and adapter.session.ready and #requests == 1 and requests[1] == "Item.ReadyIdle")
    assert(clips[1] == 2000 and clips[2] == 2001 and clips[3] == 2400)
    assert(not adapter:block_use(player_item))
    assert(use[1]({ nil, player_item }) == nil and use[2](123) == 123)
    for _ = 1, 3 do adapter:update() end
    assert(#requests == 1 and #banks == 2, "No repeated idle forcing or bank append")
    getbank[1]({ nil, controller, 66 }); assert(getbank[2](0) == 9962)
    getbank[1]({ nil, controller, 65 }); assert(getbank[2](10) == 10)
    getbank[1]({ nil, {}, 66 }); assert(getbank[2](0) == 0)
    assert(arms[1]({ nil, controller }) == "skip" and arms[2](123) == 123)
    assert(mirrored == 1 and fsm_updates == 1 and both == 1)
    right_fields.IsSet = false; assert(arms[1]({ nil, controller }) == "skip" and mirrored == 1); right_fields.IsSet = true
    left_fields.IsSet = false; arms[1]({ nil, controller }); assert(mirrored == 2 and fsm_updates == 1); left_fields.IsSet = true
    right_fields.StateName = "Hands.ReadyStart"; assert(arms[1]({ nil, controller }) == nil); right_fields.StateName = "Item.ReadyIdle"
    controller_fields.CurrentWeaponID = 65; assert(arms[1]({ nil, controller }) == nil); controller_fields.CurrentWeaponID = 66
    enabled = false; blocked(); getbank[1]({ nil, controller, 66 }); assert(getbank[2](0) == 0); enabled = true
    adapter.error = "failure"; blocked(); adapter.error = nil
    paused = true; blocked(); paused = false
    loading = true; blocked(); loading = false
    menu = true; blocked(); menu = false
    health = 0; blocked(); health = 500
    manager_fields.CurrentTask = {}; blocked(); manager_fields.CurrentTask = owner
    manager_fields.OwnerTask = nil; blocked(); manager_fields.OwnerTask = owner
    pool.ready = false; blocked(); pool.ready = true
    pool_owned = false; blocked(); pool_owned = true
    pool.player = {}; blocked(); pool.player = player
    pool.reset_pending = true; blocked(); pool.reset_pending = nil
    pool.destroy_pending = true; blocked(); pool.destroy_pending = nil
    current_weapon = object({ WeaponID = 1 }, { get_Valid = function() return true end }); blocked(); current_weapon = weapon
    weapon_fields.Inventory = {}; blocked(); weapon_fields.Inventory = inventory
    for _, p in ipairs({ "Pl0000", "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000" }) do name = p; assert(adapter:matches(player)) end
    name = "Pl9000"; assert(not adapter:matches(player) and not adapter:block_use(player_item)); name = "Pl0000"
    for f = 0, 13 do flow = f; assert(adapter:matches(player)) end
    for _, f in ipairs({ -1, 14, 18, 21 }) do flow = f; assert(not adapter:matches(player)) end
    flow = nil; assert(not adapter:matches(player)); flow = 3
    path = "CH9/Native/Item.pfb"; assert(not adapter:matches(player) and not adapter:block_use(player_item))
    path = root .. "/CH9_WP005/Item.pfb.bak"; assert(not adapter:matches(player))
    path = nil; assert(not adapter:matches(player)); path = (root .. "/CH9_WP005/Item.pfb"):upper()
    assert(adapter:matches(player))
    current_player = nil; assert(not adapter:owns_banks() and not adapter:block_use(player_item)); current_player = player
    assert(not adapter:block_use(nil) and not adapter:block_use({}))
    weapon_fields.WeaponID = 0; assert(not adapter:block_use(player_item)); weapon_fields.WeaponID = 66
    weapon_valid = false; assert(not adapter:block_use(player_item)); weapon_valid = true
    item_fields.WeaponItem = nil; assert(not adapter:block_use(player_item)); item_fields.WeaponItem = weapon
    local owned = banks[1]
    banks[1] = bank(9962, {}); blocked(); assert(not pcall(function() adapter:prepare(player) end)); banks[1] = owned
    owned:call("set_BankID", 1); blocked(); owned:call("set_BankID", 0)
    owned:call("set_BankType", 1); blocked(); owned:call("set_BankType", 9962)
    adapter:reset(); blocked(); assert(adapter.motion_info == nil and not adapter.session.ready)
    adapter:update(); blocked(); adapter:update(); assert(not adapter:block_use(player_item) and #banks == 2)
    -- Pausing during async readiness must resume polling, not lose the pending attempt.
    adapter:reset(); adapter:update(); native_ready = false; adapter:update(); assert(adapter.pending)
    paused = true; now = 50.0; adapter:update(); paused = false; adapter:update(); assert(adapter.pending)
    adapter.motion_info.stale = true; local old = adapter.motion_info
    native_ready = true; duration = 40.0; adapter:update()
    assert(adapter.motion_info ~= old and adapter.session.ready and not adapter.pending)
    current_weapon = nil; adapter:update(); assert(not adapter.weapon)
    current_weapon = weapon; weapon_fields.Inventory = nil; adapter:update(); assert(adapter.pending)
    weapon_fields.Inventory = inventory; adapter:update(); assert(not adapter.pending)
    weapon_type = "app.Weapon"; assert(not pcall(function() adapter:update() end)); weapon_type = "app.CH9Weapon1900"
    adapter:reset(); adapter:update(); native_ready = false; adapter:update(); now = now + 11.0
    assert(not pcall(function() adapter:update() end), "Bounded readiness timeout")
    local old_session, old_scratch = adapter.session, adapter.motion_info
    player = object({}, { get_Name = function() return name end })
    current_player, pool.player, banks, native_ready = player, player, {}, true
    adapter:update()
    assert(adapter.session ~= old_session and adapter.session.player == player and #banks == 2)
    assert(adapter.motion_info ~= old_scratch and not adapter:block_use(player_item))
    adapter = Item.new(game, function() return true end, root, pool)
    banks, fallback_ready = {}, false
    adapter:update(); now = now + 11.0; assert(not pcall(function() adapter:update() end))
end
