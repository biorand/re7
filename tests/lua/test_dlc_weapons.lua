return function()
    local Weapons = require("BioRand7/dlc_weapons")
    local enabled, allow, available, ready, foreign, fail = false, true, false, false, false, false
    local base_game_only = false
    local calls, loads, errors, now = 0, 0, 0, 0
    os.clock = function() return now end
    local manager = { call = function(_, method, id)
        assert(method == "findItemData")
        calls = calls + 1
        if not available then return end
        return { get_field = function(_, name)
            assert(name == "ItemPrefab")
            return { call = function(_, name, value)
                if name == "get_Path" then return foreign and "CH8/Weapon.pfb" or "BioRand/DlcWeapons/" .. id .. "/Item.pfb" end
                if name == "get_Ready" then return ready end
                assert(name == "set_Standby" or name == "set_Path")
                loads = loads + 1
                if fail then error("load failure") end
            end }
        end }
    end }
    local context = {
        config = { get = function(_, key, default)
            if key == "dlc-campaign-weapons" then return enabled end
            if key == "allow-dlc-items" then return allow end
            if key == "base-game-only" then
                if base_game_only == nil then return default end
                return base_game_only
            end
            return default
        end },
        game = { singleton = function(_, name) assert(name == "app.ItemManager"); return manager end },
        log = { error = function() errors = errors + 1 end },
    }
    local weapons = Weapons.new(context)
    weapons:update(); assert(calls == 0)
    enabled, allow = true, false
    weapons:update(); assert(calls == 0)
    allow = true
    base_game_only = true
    weapons:update(); assert(calls == 0, "Base-game mode must override both DLC weapon switches")
    base_game_only = nil
    weapons:update(); assert(calls == 0, "Missing compatibility setting must default to base-game mode")
    base_game_only = false
    weapons:update(); assert(calls == 14 and loads == 0)
    available, now = true, 1
    weapons:update(); assert(loads == 42)
    now = 3; weapons:update(); assert(loads == 42 and calls == 28)
    weapons:reset(); ready = true
    weapons:update(); assert(loads == 42)
    weapons:reset(); ready, foreign = false, true
    weapons:update(); assert(loads == 42)
    weapons:reset(); foreign, fail = false, true
    weapons:update(); assert(loads == 56 and errors == 14)
    now = 5; weapons:update(); assert(loads == 56, "Failed mutations must not retry every frame")

    local module_name = "BioRand7/dlc_weapon_player"
    local previous = package.loaded[module_name]
    local gauntlet_name = "BioRand7/dlc_gauntlet"
    local previous_gauntlet = package.loaded[gauntlet_name]
    local grenade_name = "BioRand7/dlc_grenade"
    local previous_grenade = package.loaded[grenade_name]
    local ch9_name = "BioRand7/dlc_ch9_throwable"
    local previous_ch9 = package.loaded[ch9_name]
    local item_name = "BioRand7/dlc_ch9_item"
    local previous_item = package.loaded[item_name]
    local item_installed, item_updated, item_reset, item_enabled = 0, 0, 0
    local pool = {}
    package.loaded[item_name] = { new = function(game, is_enabled, root, shared_pool)
        assert(game == context.game and root == "BioRand/DlcWeapons" and shared_pool == pool)
        item_enabled = is_enabled
        return {
            install = function() item_installed = item_installed + 1 end,
            update = function() item_updated = item_updated + 1; error("CH9 Item failure") end,
            reset = function() item_reset = item_reset + 1 end,
        }
    end }
    local ch9_installed, ch9_updated, ch9_reset, ch9_enabled = 0, 0, 0
    package.loaded[ch9_name] = { new = function(game, is_enabled, root)
        assert(game == context.game and root == "BioRand/DlcWeapons")
        ch9_enabled = is_enabled
        return {
            pool = pool,
            install = function() ch9_installed = ch9_installed + 1 end,
            update = function(self)
                ch9_updated = ch9_updated + 1
                if not self.error then error("CH9 throwable failure") end
            end,
            reset = function() ch9_reset = ch9_reset + 1 end,
        }
    end }
    local grenade_installed, grenade_updated, grenade_reset, grenade_enabled = 0, 0, 0
    package.loaded[grenade_name] = { new = function(game, is_enabled, root)
        assert(game == context.game and root == "BioRand/DlcWeapons")
        grenade_enabled = is_enabled
        return {
            install = function() grenade_installed = grenade_installed + 1 end,
            update = function(self)
                grenade_updated = grenade_updated + 1
                if not self.error then error("grenade failure") end
            end,
            reset = function() grenade_reset = grenade_reset + 1 end,
        }
    end }
    local gauntlet_installed, gauntlet_updated, gauntlet_reset, gauntlet_enabled = 0, 0, 0
    package.loaded[gauntlet_name] = { new = function(game, is_enabled, root)
        assert(game == context.game and root == "BioRand/DlcWeapons")
        gauntlet_enabled = is_enabled
        return {
            install = function() gauntlet_installed = gauntlet_installed + 1 end,
            update = function() gauntlet_updated = gauntlet_updated + 1; error("gauntlet failure") end,
            reset = function() gauntlet_reset = gauntlet_reset + 1 end,
        }
    end }
    local installed, updated, reset = 0, 0, 0
    local adapter_enabled
    package.loaded[module_name] = { new = function(game, is_enabled, root)
        assert(game == context.game and root == "BioRand/DlcWeapons")
        adapter_enabled = is_enabled
        return {
            install = function() installed = installed + 1 end,
            update = function() updated = updated + 1; error("adapter failure") end,
            reset = function() reset = reset + 1 end,
        }
    end }
    weapons:install(); weapons:install()
    assert(installed == 1 and adapter_enabled())
    assert(gauntlet_installed == 1 and gauntlet_enabled())
    assert(grenade_installed == 1 and grenade_enabled())
    assert(ch9_installed == 1 and ch9_enabled())
    assert(item_installed == 1 and item_enabled())
    weapons.ch9_failed = true
    assert(not item_enabled(), "The shared pool owner's failure disables Item use")
    weapons.ch9_failed = false
    weapons:update(); weapons:update()
    assert(updated == 1 and reset == 1 and not adapter_enabled(), "Adapter failures must stop until reset")
    assert(gauntlet_updated == 1 and gauntlet_reset == 1 and not gauntlet_enabled())
    assert(item_updated == 1 and item_reset == 1 and not item_enabled() and weapons.ch9_item_adapter.error,
        "Item failures latch without resetting the shared pool a second time")
    assert(grenade_updated == 2 and grenade_reset == 1 and not grenade_enabled() and weapons.grenade_adapter.error,
        "Failed grenade controllers still drain deferred native pool cleanup")
    assert(ch9_updated == 2 and ch9_reset == 1 and not ch9_enabled() and weapons.ch9_adapter.error,
        "Failed CH9 controllers still drain deferred native pool cleanup")
    weapons:reset()
    assert(reset == 2 and adapter_enabled(), "New sessions reset adapter state without reinstalling hooks")
    assert(gauntlet_reset == 2 and gauntlet_enabled())
    assert(grenade_reset == 2 and grenade_enabled() and not weapons.grenade_adapter.error)
    assert(ch9_reset == 2 and ch9_enabled() and not weapons.ch9_adapter.error)
    assert(item_reset == 2 and item_enabled() and not weapons.ch9_item_adapter.error)
    enabled = false
    assert(not adapter_enabled())
    assert(not gauntlet_enabled())
    assert(not grenade_enabled())
    assert(not ch9_enabled())
    assert(not item_enabled())
    enabled, allow = true, false
    assert(not adapter_enabled())
    assert(not ch9_enabled())
    assert(not item_enabled())
    package.loaded[module_name] = previous
    package.loaded[gauntlet_name] = previous_gauntlet
    package.loaded[grenade_name] = previous_grenade
    package.loaded[ch9_name] = previous_ch9
    package.loaded[item_name] = previous_item
end
