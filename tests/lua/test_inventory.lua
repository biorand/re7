return function()
    local Inventory = require("BioRand7/inventory")
    local MadhouseSaves = require("BioRand7/madhouse_saves")
    local ReloadSpeed = require("BioRand7/reload_speed")
    local Em8000KneeDown = require("BioRand7/em8000_knee_down")
    local previous_sdk, previous_thread = sdk, thread
    local storage = {}
    sdk = {
        PreHookResult = { SKIP_ORIGINAL = 1 },
        to_int64 = function(value) return value end,
        to_ptr = function(value) return value end,
    }
    thread = { get_hook_storage = function() return storage end }

    local function object(fields, methods)
        return {
            get_field = function(_, name) return fields[name] end,
            set_field = function(_, name, value) fields[name] = value end,
            call = function(self, name, ...)
                assert(methods and methods[name], "Unknown TDB method: " .. name)
                return methods[name](self, ...)
            end,
        }
    end

    local hooks, settings, singletons = {}, {}, {}
    local static_fields = { RowNum = 4, fReloadSpeedRate = 123 }
    local difficulty, difficulty_reads = 2, 0
    local player_order
    local game = {
        hook = function(_, type_name, signature, before, after)
            hooks[type_name .. ":" .. signature] = { before = before, after = after }
        end,
        object = function(_, value) return value end,
        singleton = function(_, name) return singletons[name] end,
        component = function(_, player, type_name)
            assert(type_name == "app.PlayerOrder")
            return player and player_order
        end,
        difficulty = function()
            difficulty_reads = difficulty_reads + 1
            return difficulty
        end,
        static_field = function(_, _, name) return static_fields[name] end,
        set_static_field = function(_, _, name, value) static_fields[name] = value end,
    }
    local context = {
        game = game,
        config = { get = function(_, key, default)
            local value = settings[key]
            if value == nil then return default end
            return value
        end },
        log = { warn = function() end, info = function() end },
    }

    local inventory = Inventory.new(context)
    inventory:install()
    local name = "Pl0000"
    local player = object({}, { get_Name = function() return name end })
    singletons["app.ObjectManager"] = object({ PlayerObj = player })
    settings["random-starting-inventory-size-ethan"] = "20"
    settings["random-starting-inventory-size-mia"] = "16"
    assert(inventory:desired_level() == 2)
    name = "Pl2000"
    assert(inventory:desired_level() == 1)
    name = "Pl0000"

    local inventory_fields = { _ExtendLv = 0 }
    local owner_inventory = object(inventory_fields, {
        get_ExtendLv = function() return inventory_fields._ExtendLv end,
    })
    local setup = hooks["app.Inventory:setupItemSlotManager(app.Inventory.ExtendLvDef)"]
    local args = { 0, owner_inventory, 0 }
    setup.before(args)
    assert(inventory_fields._ExtendLv == 2)
    assert(args[3] == 2, "Slot allocation must receive the configured level")
    args[3] = 3
    setup.before(args)
    assert(args[3] == 3, "An existing larger inventory must be retained")
    args[3] = 0
    hooks["app.Inventory:setExtendLv(app.Inventory.ExtendLvDef)"].before(args)
    assert(args[3] == 2)

    local discard = hooks["app.Item:isCanDiscard()"]
    discard.before({ 0, {} })
    assert(discard.after(1) == 1, "Do not inspect items the game already permits discarding")
    local item_fields = { ItemDataID = "HandgunBullet" }
    local item_data_fields = { Category = 1 }
    local item_data = object(item_data_fields)
    local item = object(item_fields, { get_ItemData = function() return item_data end })
    discard.before({ 0, item })
    assert(discard.after(256) == 1, "Boolean results occupy the low byte")
    item_data_fields.Category = 4
    discard.before({ 0, item })
    assert(discard.after(0) == 0, "Ordinary key items must stay protected")
    item_fields.ItemDataID = "FoundFootage000"
    discard.before({ 0, item })
    assert(discard.after(0) == 1)
    for _, data_id in ipairs({ "FoundFootage050", "FuseCh4", "SpareKey" }) do
        item_fields.ItemDataID = data_id
        discard.before({ 0, item })
        assert(discard.after(0) == 0, data_id .. " must keep its native discard restriction")
    end
    item_data_fields.Category = 10
    item_fields.ItemDataID = "EthanCarKey"
    discard.before({ 0, item })
    assert(discard.after(0) == 0, "The car key is needed before the garage fight")
    item_data_fields.Category = 1
    for _, data_id in ipairs({ "Candle", "Candle_Lighted", "HandAxe", "Knife", "ChainSaw", "Handgun_Albert" }) do
        item_fields.ItemDataID = data_id
        discard.before({ 0, item })
        assert(discard.after(0) == 0, data_id .. " must keep its native discard restriction")
        discard.before({ 0, item })
        assert(discard.after(1) == 1, "Preserve native permission once the item is no longer needed")
    end
    item_fields.ItemDataID = nil
    item_data_fields.Category = 1
    discard.before({ 0, item })
    assert(discard.after(0) == 1)
    settings["inventory-unrestricted-management"] = false
    discard.before({ 0, item })
    assert(discard.after(0) == 0)

    local registered_skill
    player_order = object({}, {
        ["registerPassiveSkill(app.PlayerPassiveSkill)"] = function(_, skill)
            registered_skill = skill
        end,
    })
    local passive_skill = {}
    item_fields.ItemDataID = "skl001"
    local passive_fields = { Item = item, PassiveSkill = passive_skill }
    local passive_item = object(passive_fields)
    local insert = hooks["app.PassiveSkillItem:onInsertInventory(app.Inventory)"]
    assert(insert.before({ 0, passive_item, owner_inventory }) == sdk.PreHookResult.SKIP_ORIGINAL)
    assert(registered_skill == passive_skill and passive_fields.PlayerOrder == player_order)
    registered_skill = nil
    item_fields.ItemDataID = "skl001no"
    assert(insert.before({ 0, passive_item, owner_inventory }) == nil)
    assert(registered_skill == nil)
    item_fields.ItemDataID = "skl001"
    player_order = nil
    assert(insert.before({ 0, passive_item, owner_inventory }) == sdk.PreHookResult.SKIP_ORIGINAL)

    local unlocked = hooks["app.InventoryMenu:DictionaryCombine_UnlockedCombine(app.ItemCombineData.Data)"]
    hooks["app.InventoryMenu.DictionaryCombineUIController:deactivate()"].before()
    assert(static_fields.RowNum == 4)
    assert(unlocked.after(256) == 256, "Disabled recipe unlocks must retain the game's result")
    settings["recipes-add-new"] = true
    settings["recipes-unlock-from-start"] = true
    hooks["app.InventoryMenu.DictionaryCombineUIController:setup()"].before()
    assert(static_fields.RowNum == 5, "The first combine menu must initialize all rows before binding icons")
    hooks["app.InventoryMenu.DictionaryCombineUIController:deactivate()"].before()
    assert(static_fields.RowNum == 5)
    assert(unlocked.after(256) == 1)

    settings["recipes-add-new"] = false
    settings["debug-recipes-enabled"] = true
    static_fields.RowNum = 4
    hooks["app.InventoryMenu.DictionaryCombineUIController:setup()"].before()
    assert(static_fields.RowNum == 5, "Exact debug recipes also require the expanded combine UI")
    assert(unlocked.after(256) == 256, "Debug recipe flags must override the normal global unlock option")
    assert(unlocked.after(1) == 1)

    local dictionary_entries = { count = 17 }
    local dictionary = object({ _Datas = dictionary_entries })
    singletons["app.ItemManager"] = object({}, {
        get_DictionaryCombineData = function() return dictionary end,
    })
    local discovered, writes, guid_parses = 4105, 0, 0
    local unlock_guid = {}
    local unlock_variable = object({}, {
        get_U32 = function() return discovered end,
        ["set_U32(System.UInt32)"] = function(_, mask)
            assert(not math.type or math.type(mask) == "integer", "REFramework UInt32 arguments require a Lua integer")
            discovered, writes = mask, writes + 1
        end,
    })
    local variables_hub = object({}, {
        ["findVariable(System.Guid)"] = function(_, guid)
            assert(guid == unlock_guid, "Only the dictionary discovery flag may change")
            return unlock_variable
        end,
    })
    game.list_storage = function(_, entries)
        assert(entries == dictionary_entries)
        return {}, entries.count
    end
    game.method = function(_, type_name, signature)
        if type_name == "via.userdata.GlobalUserData" then
            assert(signature == "get_VariablesHub()")
            return { call = function(_, receiver) assert(receiver == nil); return variables_hub end }
        end
        assert(type_name == "System.Guid" and signature == "Parse(System.String)")
        return { call = function(_, receiver, text)
            assert(receiver == nil and text == "419a0691-1219-4447-b927-e31ac6e35486")
            guid_parses = guid_parses + 1
            return unlock_guid
        end }
    end
    local menu_setup = hooks["app.InventoryMenu:setup(app.Inventory, app.InventoryMenu.ModeType, app.InventoryMenu.SelectItemResult, System.Boolean)"]
    local menu_open = hooks["app.InventoryMenu:openProc()"]
    menu_setup.before()
    assert(discovered == 131071, "All 17 ShowInUI slots must open even on a save with only three discovered recipes")
    menu_open.before()
    assert(writes == 1 and guid_parses == 1, "An already unlocked menu must not rewrite flags or parse GUIDs repeatedly")
    assert(unlocked.after(0) == 0, "Discovery must not bypass a recipe's unmet EnableFlag")
    assert(unlocked.after(1) == 1, "An enabled, discovered recipe must stay available")
    discovered = 0 -- Loading an earlier save restores its discovery flags.
    menu_open.before()
    assert(discovered == 131071, "Reopening after a save load must restore the exact table's visibility")
    dictionary_entries.count = 20
    menu_open.before()
    assert(discovered == 1048575, "All supported dictionary slots must be discovered")
    dictionary_entries.count = 0
    menu_setup.before()
    assert(discovered == 0, "An empty visible dictionary must not leave a selectable empty tab")
    dictionary_entries.count = 1
    menu_setup.before()
    assert(discovered == 1, "Shrinking the dictionary must discard stale discovery bits")
    variables_hub = nil
    menu_setup.before()
    singletons["app.ItemManager"] = nil
    menu_setup.before()
    settings["debug-recipes-enabled"] = false
    game.method = function() error("Disabled debug mode must not access or modify discovery flags") end
    menu_setup.before()
    menu_open.before()

    local madhouse = MadhouseSaves.new(context)
    madhouse:install()
    for _ = 1, 60 do
        hooks["app.SaveDataManager:doUpdate()"].before({})
        hooks["app.SaveDataManager:doLateUpdate()"].before({})
    end
    assert(difficulty_reads == 0, "Idle save updates must not query game state")
    local selected, closed = false, false
    local menu = object({}, {
        ["setSelectItemResult(System.Boolean, System.String)"] = function(_, cancelled, data_id)
            assert(cancelled == false and data_id == "SaveTape")
            selected = true
        end,
    })
    local handle = object({ _Menu = menu }, { requestClose = function() closed = true end })
    local save_fields = { IsTapeSub = true }
    local save_manager = object(save_fields, { get_IsNowSaveHardSelectDispGUI = function() return true end })
    local open_menu = hooks["app.MenuManager:openSelectItemMenu"]
    assert(open_menu.after(handle) == handle)
    hooks["app.SaveDataManager:doUpdate()"].before({ 0, save_manager })
    assert(selected and closed and madhouse.pending_menu == nil)
    local sub_tape = hooks["app.SaveDataManager:isHardModeSubTape()"]
    assert(sub_tape.before({ 0, save_manager }) == sdk.PreHookResult.SKIP_ORIGINAL)
    assert(save_fields.IsTapeSub == false)
    difficulty = 1
    assert(sub_tape.before({ 0, save_manager }) == nil)
    difficulty = 2
    settings["madhouse-normal-saves"] = false
    assert(sub_tape.before({ 0, save_manager }) == nil)

    local reload = ReloadSpeed.new(context)
    reload:install()
    assert(hooks["app.PlayerMotionController:update()"] == nil, "Do not hook disabled reload updates")
    settings["weapon-mod-reload-speed"] = true
    reload:on_config_changed()
    local installed_hook = hooks["app.PlayerMotionController:update()"]
    reload:on_config_changed()
    assert(hooks["app.PlayerMotionController:update()"] == installed_hook, "Config reload must not duplicate hooks")
    settings["weapon-reload-speed-multiplier-handgun-g17"] = 2
    local applied_rate
    local reload_table = object({}, {
        ["getReloadSpeedRate(System.Int32)"] = function(_, level)
            assert(level == 1)
            return 1.5
        end,
    })
    local motion_manager = object({}, {
        ["setFloatToMotionVariable(System.UInt32, System.Single)"] = function(_, hash, rate)
            assert(hash == 123)
            applied_rate = rate
        end,
    })
    local motion_fields = {
        CurrentWeaponID = 7,
        DepressantLevel = 1,
        PlayerReloadSpeedRateTable = reload_table,
        MotionManager = motion_manager,
    }
    local controller = object(motion_fields)
    local update = hooks["app.PlayerMotionController:update()"]
    update.before({ 0, controller })
    assert(update.after(0) == 0)
    assert(motion_fields.ReloadSpeedRate == 3 and applied_rate == 3)
    settings["weapon-mod-reload-speed-include-stabilizers"] = false
    reload:apply(controller)
    assert(motion_fields.ReloadSpeedRate == 1.5)
    settings["weapon-mod-reload-speed-include-stabilizers"] = true
    motion_fields.CurrentWeaponID = 9999
    motion_fields.CurrentWeapon = object({ WeaponID = 7 })
    reload:apply(controller)
    assert(motion_fields.ReloadSpeedRate == 3)
    settings["weapon-reload-speed-multiplier-handgun-g17"] = 0.75
    reload:apply(controller)
    assert(motion_fields.ReloadSpeedRate == 1.12)
    settings["weapon-reload-speed-multiplier-handgun-g17"] = 0.25
    reload:apply(controller)
    assert(motion_fields.ReloadSpeedRate == 0.38)
    settings["weapon-mod-reload-speed"] = false
    storage.biorand_reload_controller = controller
    update.before({ 0, controller })
    assert(storage.biorand_reload_controller == nil)

    local knee_down = Em8000KneeDown.new(context)
    knee_down:install()
    local reaction_flags = { [2] = false }
    local flags = object({}, {
        ContainsKey = function(_, key) return reaction_flags[key] ~= nil end,
        get_Item = function(_, key) return reaction_flags[key] end,
    })
    local think_fields = { _Mode = 7 }
    local enemy = object({
        MyEm8000ActionStatus = {},
        MyThink = object(think_fields),
        DictForbidDamageReactionTypeFlag = flags,
    })
    local resist = object({ resistType = 2 })
    assert(knee_down:should_force(enemy, resist, 6))
    assert(not knee_down:should_force(enemy, resist, 1))
    assert(not knee_down:should_force(enemy, nil, 6))
    think_fields._Mode = 6
    assert(not knee_down:should_force(enemy, resist, 6))
    think_fields._Mode = 7
    reaction_flags[2] = nil
    assert(not knee_down:should_force(enemy, resist, 6), "Missing flags must not permit a reaction")
    reaction_flags[2] = false
    local knee_hook = hooks["app.Em3000.Em3000ActionController:"
        .. "isEm8000KneeDownDamage(app.EnemyActionController.ResistResultSet, app.Em8000.Em8000Define.WeaponGroup.Group)"]
    knee_hook.before({ 0, enemy, resist, 6 })
    assert(knee_hook.after(256) == 1)
    knee_hook.before({ 0, enemy, resist, 1 })
    assert(knee_hook.after(0) == 0)

    sdk, thread = previous_sdk, previous_thread
end
