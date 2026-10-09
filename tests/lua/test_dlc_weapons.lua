return function()
    local Weapons = require("BioRand7/dlc_weapons")
    local enabled, allow, available, ready, foreign, fail = false, true, false, false, false, false
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
        config = { get = function(_, key) if key == "dlc-campaign-weapons" then return enabled else return allow end end },
        game = { singleton = function(_, name) assert(name == "app.ItemManager"); return manager end },
        log = { error = function() errors = errors + 1 end },
    }
    local weapons = Weapons.new(context)
    weapons:update(); assert(calls == 0)
    enabled, allow = true, false
    weapons:update(); assert(calls == 0)
    allow = true
    weapons:update(); assert(calls == 4 and loads == 0)
    available, now = true, 1
    weapons:update(); assert(loads == 12)
    now = 3; weapons:update(); assert(loads == 12 and calls == 8)
    weapons:reset(); ready = true
    weapons:update(); assert(loads == 12)
    weapons:reset(); ready, foreign = false, true
    weapons:update(); assert(loads == 12)
    weapons:reset(); foreign, fail = false, true
    weapons:update(); assert(loads == 16 and errors == 4)
    now = 5; weapons:update(); assert(loads == 16, "Failed mutations must not retry every frame")
end
