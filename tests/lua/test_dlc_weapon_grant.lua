return function()
    local Weapons = require("BioRand7/dlc_weapons")
    local UI = require("BioRand7/ui")
    local ids = { "CKnife", "Handgun_Albert_C", "Shotgun_Albert", "NumaItem072" }
    local flags = { ["dlc-campaign-weapons"] = true, ["allow-dlc-items"] = true }
    local owned, boxed, paths, ready, missing = {}, {}, {}, {}, {}
    local calls, errors, infos = {}, {}, {}
    local player_name, player_loaded, inventory_loaded, box_loaded, manager_loaded = "Pl0000", true, true, true, true
    local failure_id, throw = nil, false
    local player = { call = function(_, method)
        assert(method == "get_Name")
        return player_name
    end }
    local box = { call = function(_, method, id, amount, saved)
        assert(method == "addItem(System.String, System.Int32, app.WeaponGun.WeaponGunSaveData)")
        assert(amount == 1 and saved == nil)
        calls[#calls + 1] = id
        if id == failure_id then
            if throw then error("native failure") end
            return false
        end
        boxed[id] = true
        return true
    end }
    local inventory = { call = function(_, method, id, include_equipped)
        if method == "get_ItemBoxData" then return box_loaded and box or nil end
        assert(method == "hasItemIncludeItemBox" and include_equipped == false)
        return owned[id] or boxed[id] or false
    end }
    local manager = { call = function(_, method, id)
        assert(method == "findItemData")
        if missing[id] then return nil end
        return { get_field = function(_, field)
            assert(field == "ItemPrefab")
            return { call = function(_, name)
                if name == "get_Path" then return paths[id] or "BioRand/DlcWeapons/" .. id .. "/Item.pfb" end
                assert(name == "get_Ready", "Grant must not force loading or native initialization")
                return ready[id] ~= false
            end }
        end }
    end }
    local context = {
        config = { get = function(_, key) return flags[key] end },
        game = {
            player = function() return player_loaded and player or nil end,
            component = function(_, owner, name)
                assert(owner == player and name == "app.Inventory")
                return inventory_loaded and inventory or nil
            end,
            singleton = function(_, name)
                assert(name == "app.ItemManager")
                return manager_loaded and manager or nil
            end,
        },
        log = {
            info = function(_, message) infos[#infos + 1] = message end,
            error = function(_, message) errors[#errors + 1] = message end,
        },
    }
    local weapons = Weapons.new(context)
    -- Requests must still execute after the one-time readiness work has finished.
    weapons.finished = true
    context.features = { dlc_weapons = weapons }
    local clicked = true
    imgui = {
        tree_node = function(label) return label == "Debug tools" end,
        tree_pop = function() end,
        same_line = function() end,
        checkbox = function(_, value) return false, value end,
        text = function() end,
        button = function(label)
            if clicked and label == "Add supported DLC weapons to item box" then clicked = false; return true end
            return false
        end,
    }
    UI.new(context):debug_tools()
    assert(weapons.pending_add and #calls == 0, "UI must only queue the grant")
    weapons:request_add_to_item_box()
    weapons:update()
    assert(#calls == 4 and #infos == 1 and #errors == 0)
    for i, id in ipairs(ids) do assert(calls[i] == id and boxed[id]) end
    assert(weapons.grant_status == "Added 4 to item box; 0 already owned")
    weapons:update(); assert(#calls == 4, "No repeated grants")
    boxed[ids[1]], owned[ids[1]] = nil, true
    weapons:request_add_to_item_box(); weapons:update()
    assert(#calls == 4 and weapons.grant_status == "Added 0 to item box; 4 already owned")

    local function rejected(expected)
        owned, boxed = {}, {}
        local before, before_errors = #calls, #errors
        weapons:request_add_to_item_box(); weapons:update()
        assert(#calls == before and #errors == before_errors + 1)
        assert(weapons.grant_status:find(expected, 1, true), weapons.grant_status)
        weapons:update(); assert(#calls == before and #errors == before_errors + 1)
    end
    for _, key in ipairs({ "dlc-campaign-weapons", "allow-dlc-items" }) do
        flags[key] = false; rejected("disabled"); flags[key] = true
    end
    player_loaded = false; rejected("Ethan"); player_loaded = true
    player_name = "Pl1000"; rejected("Ethan"); player_name = "Pl0000"
    inventory_loaded = false; rejected("not ready"); inventory_loaded = true
    box_loaded = false; rejected("not ready"); box_loaded = true
    manager_loaded = false; rejected("adapter"); manager_loaded = true
    missing[ids[4]] = true; rejected("adapter"); missing[ids[4]] = nil
    for _, path in ipairs({ "CH8/Weapon.pfb", "BioRand/DlcWeaponLab/NumaItem072/Item.pfb", "BioRand/DlcWeapons/CKnife/Item.pfb" }) do
        paths[ids[4]] = path; rejected("adapter")
    end
    paths[ids[4]] = nil
    ready[ids[4]] = false; rejected("Prefab is not ready"); ready[ids[4]] = true

    -- Native failure stops the batch, preserves successes and never retries automatically.
    for _, should_throw in ipairs({ false, true }) do
        owned, boxed, throw = {}, {}, should_throw
        local before = #calls
        failure_id = ids[2]
        weapons:request_add_to_item_box(); weapons:update()
        assert(#calls == before + 2 and boxed[ids[1]] and not boxed[ids[3]])
        assert(weapons.grant_status:find("Stopped after adding 1:", 1, true))
        weapons:update(); assert(#calls == before + 2)
    end
    failure_id = nil
    local before = #calls
    for _, reset in ipairs({ "reset", "on_config_changed" }) do
        weapons:request_add_to_item_box()
        weapons[reset](weapons)
        assert(not weapons.pending_add and not weapons.grant_status)
        weapons.finished = true
        weapons:update(); assert(#calls == before, "Session/config reset must cancel pending grants")
    end
end
