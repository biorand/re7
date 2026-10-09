return function()
    local Lab = require("BioRand7/dlc_weapon_lab")
    local writes, registered, owned, in_inventory = 0, true, false, false
    local box_id, name, fail = 1, "Pl0000", false
    local ready, loads = true, 0
    local hooks = {}
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected native call: " .. method)
                return calls[method](...)
            end }
    end
    local box = object({}, {
        addItem = function(id, count, save)
            assert(id == "CKnife" and count == 1 and save == nil)
            writes = writes + 1
            if fail then error("native failure") end
            owned = true
            return true
        end,
        ["removeItem(System.String, System.Int32)"] = function(id, count)
            assert(id == "CKnife" and count == 1)
            writes = writes + 1
            owned = false
            return 1
        end,
    })
    local inventory = object({}, {
        get_ItemBoxData = function() return box end,
        hasItemIncludeItemBox = function() return owned end,
        hasItem = function() return in_inventory end,
    })
    local player = object({}, { get_Name = function() return name end })
    local prefab = object({}, {
        get_Path = function() return registered and "BioRand/DlcWeaponLab/CKnife/Item.pfb" or "CH8/Raw.pfb" end,
        get_Ready = function() return ready end,
        set_Standby = function() loads = loads + 1 end,
        set_Path = function(path) assert(path == "BioRand/DlcWeaponLab/CKnife/Item.pfb"); loads = loads + 1 end,
    })
    local item = object({ ItemPrefab = prefab }, {})
    local manager = object({}, { findItemData = function() return item end })
    local game = {
        player = function() return player end,
        component = function(_, _, type_name) assert(type_name == "app.Inventory"); return inventory end,
        singleton = function(_, type_name) assert(type_name == "app.ItemManager"); return manager end,
        address = function(_, value) assert(value == box); return box_id end,
        hook = function(_, type_name, signature, before)
            assert(type_name == "app.SaveDataManager")
            hooks[signature] = before
        end,
    }
    local lab = Lab.new(game)
    lab:install()
    assert(not lab:queue("add", "CKnife"))
    lab.armed = true
    assert(not lab:queue("box"), "A menu manager call alone does not initialize the item-box UI")
    assert(not lab:queue("add", "CH9_WP006"))
    assert(not lab:queue("prepare", "CH9_WP006"))
    assert(lab:queue("add", "CKnife"))
    assert(not lab:queue("add", "CKnife"))
    assert(writes == 0, "UI must never mutate the game")
    lab:update()
    assert(writes == 1 and owned)
    lab:queue("add", "CKnife"); lab:update()
    assert(writes == 1, "Do not duplicate a weapon")
    in_inventory = true
    lab:queue("cleanup"); lab:update()
    assert(writes == 1 and not lab.armed, "Do not destroy an inventory weapon")
    in_inventory, lab.armed = false, true
    lab:queue("cleanup"); lab:update()
    assert(writes == 2 and not owned)

    lab:queue("add", "CKnife"); lab:update()
    assert(writes == 3)
    box_id = 2
    lab:queue("cleanup"); lab:update()
    assert(writes == 3, "A different session's item is not ours to remove")
    owned, registered = false, false
    lab:queue("add", "CKnife"); lab:update()
    assert(writes == 3 and not lab.armed, "Do not instantiate a raw DLC inventory prefab")
    registered, lab.armed, name = true, true, "Pl8000"
    lab:queue("add", "CKnife"); lab:update()
    assert(writes == 3 and not lab.armed, "Only test Ethan's campaign")
    name, lab.armed, fail = "Pl0000", true, true
    lab:queue("add", "CKnife"); lab:update(); lab:update()
    assert(writes == 4 and not lab.armed, "A failed mutation must not retry every frame")
    lab.armed = true
    lab:queue("add", "CKnife")
    lab.added.CKnife = true
    hooks["loadLevelUsingLoadData()"]()
    lab:update()
    assert(writes == 4 and not lab.armed and next(lab.added) == nil, "Loads invalidate pending commands and receipts")
    lab.armed = true
    hooks["newGameInit()"]()
    assert(not lab.armed)
    fail, owned, ready, lab.armed = false, false, false, true
    lab:queue("add", "CKnife"); lab:update()
    assert(writes == 4 and not lab.armed, "Do not add an unready prefab")
    lab.armed = true
    lab:queue("prepare", "CKnife"); lab:update(); lab:update()
    assert(loads == 3 and writes == 4, "Preparation requests loading once without inventory mutation")
    ready = true
    lab:queue("prepare", "CKnife"); lab:update()
    assert(loads == 3, "Do not unload a ready prefab")
    registered = false
    lab:queue("prepare", "CKnife"); lab:update()
    assert(loads == 3 and not lab.armed, "Preparation must not alter source DLC prefabs")
end
