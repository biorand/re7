return function()
    local item_resets, item_failure, item_enabled = 0, false
    local pool = {}
    package.loaded["BioRand7/dlc_ch9_item"] = { new = function(_, enabled, root, shared_pool)
        assert(root == "BioRand/DlcWeaponLab" and shared_pool == pool)
        item_enabled = enabled
        return { install = function() end, reset = function() item_resets = item_resets + 1 end,
            update = function() if item_failure then error("CH9 Item failed") end end }
    end }
    local ch9_resets, ch9_failure, ch9_updates = 0, false, 0
    package.loaded["BioRand7/dlc_ch9_throwable"] = { new = function(_, _, root)
        assert(root == "BioRand/DlcWeaponLab")
        return { pool = pool, install = function() end, reset = function() ch9_resets = ch9_resets + 1 end,
            update = function(self)
                ch9_updates = ch9_updates + 1
                if ch9_failure and not self.error then error("CH9 throwable failed") end
            end }
    end }
    local grenade_resets, grenade_failure, grenade_updates = 0, false, 0
    package.loaded["BioRand7/dlc_grenade"] = { new = function(_, _, root)
        assert(root == "BioRand/DlcWeaponLab")
        return { install = function() end, reset = function() grenade_resets = grenade_resets + 1 end,
            update = function(self)
                grenade_updates = grenade_updates + 1
                if grenade_failure and not self.error then error("Grenade failed") end
            end }
    end }
    local gauntlet_resets, gauntlet_failure = 0, false
    package.loaded["BioRand7/dlc_gauntlet"] = { new = function(_, _, root)
        assert(root == "BioRand/DlcWeaponLab")
        return { install = function() end, reset = function() gauntlet_resets = gauntlet_resets + 1 end,
            update = function(self) if gauntlet_failure and not self.error then error("Gauntlet failed") end end }
    end }
    local Lab = require("BioRand7/dlc_weapon_lab")
    assert(#Lab.candidates == 14)
    assert(Lab.candidates[5].id == "CH9_WP002")
    assert(Lab.candidates[6].id == "CH9_WP006")
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
        component = function(_, _, type_name)
            if type_name == "app.EquipManager" then return nil end
            assert(type_name == "app.Inventory"); return inventory
        end,
        singleton = function(_, type_name) assert(type_name == "app.ItemManager"); return manager end,
        address = function(_, value) assert(value == box); return box_id end,
        hook = function(_, type_name, signature, before)
            assert(type_name == "app.SaveDataManager" or type_name == "app.PlayerMotionController" or type_name == "app.DamageController")
            hooks[signature] = before
        end,
    }
    local lab = Lab.new(game)
    lab:install()
    assert(not lab:queue("add", "CKnife"))
    lab.armed = true
    assert(not lab:queue("box"), "A menu manager call alone does not initialize the item-box UI")
    assert(not lab:queue("add", "UnknownWeapon"))
    for _, id in ipairs({ "CH9_WP003", "CH9_WP004", "CH9_WP005" }) do
        assert(lab:queue("prepare", id))
        lab.pending = nil
    end
    assert(lab:queue("prepare", "CH9_WP006"))
    lab.pending = nil
    assert(lab:queue("prepare", "CH9_WP002"), "Spirit Blade is an explicit lab candidate only")
    lab.pending = nil
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
    assert(gauntlet_resets == 2, "Both load/new-game paths discard the gauntlet's transient state")
    assert(grenade_resets == 2, "Both load/new-game paths invalidate the grenade session")
    assert(ch9_resets == 2, "Both load/new-game paths invalidate the CH9 throwable session")
    assert(item_resets == 2, "Item reset must not reset the shared pool again")
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
    gauntlet_failure, lab.armed = true, true
    lab:update(); lab:update()
    assert(not lab.armed and lab.gauntlet.error and gauntlet_resets == 3, "Disarm failed adapters without retrying mutations")
    grenade_failure, lab.armed = true, true
    lab:update()
    local updates = grenade_updates
    lab:update()
    assert(not lab.armed and lab.grenade.error and grenade_resets == 3 and grenade_updates == updates + 1,
        "A failed grenade controller must still tick deferred pool cleanup")
    ch9_failure, lab.armed = true, true
    lab:update()
    updates = ch9_updates
    lab:update()
    assert(not lab.armed and lab.ch9.error and ch9_resets == 3 and ch9_updates == updates + 1,
        "A failed CH9 controller must still tick deferred pool cleanup")
    lab.armed = true
    assert(not item_enabled(), "Pool owner failure disables Item use")
    item_failure = true
    lab:update(); lab:update()
    assert(not lab.armed and lab.ch9_item.error and item_resets == 3 and ch9_resets == 3)
    lab:reset()
    assert(not lab.ch9_item.error and not lab.ch9.error)
end
