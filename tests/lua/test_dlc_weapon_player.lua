return function()
    local Player = require("BioRand7/dlc_weapon_player")
    local enabled, name, path = true, "Pl0000", "BioRand/DlcWeaponLab/CH9_WP002/Item.pfb"
    local hooks, storage, conversions, lookups = {}, {}, 0, 0
    local player = { call = function(_, method) assert(method == "get_Name"); return name end }
    local owner = player
    local banks = { [10] = true, [30] = true }
    local motion = { call = function(_, method, id, bank)
        assert(method == "findMotionBank(System.UInt32, System.UInt32)" and id == 0)
        local actual = banks[bank] and bank or 0
        return { call = function(_, member)
            if member == "get_BankID" then return 0 end
            assert(member == "get_BankType")
            return actual
        end }
    end }
    local controller = {
        call = function(_, method) assert(method == "get_GameObject"); return owner end,
        get_field = function(_, field) assert(field == "Motion"); return motion end,
    }
    local prefab = { call = function(_, method) assert(method == "get_Path"); return path end }
    local data = { get_field = function(_, field) assert(field == "ItemPrefab"); return prefab end }
    local manager = { call = function(_, method, id)
        assert(method == "findItemData" and id == "CH9_WP002")
        lookups = lookups + 1
        return data
    end }
    local game = {
        player = function() return player end,
        singleton = function(_, type_name) assert(type_name == "app.ItemManager"); return manager end,
        object = function(_, value) conversions = conversions + 1; return value end,
        hook = function(_, type_name, signature, before, after)
            assert(type_name == "app.PlayerMotionController" or type_name == "app.DamageController")
            hooks[#hooks + 1] = { before = before, after = after }
        end,
    }
    sdk = { to_int64 = function(value) return value end, to_ptr = function(value) return { pointer = value } end }
    thread = { get_hook_storage = function() return storage end }
    local adapter = Player.new(game, function() return enabled end, "BioRand/DlcWeaponLab")
    adapter:install(); adapter:install()
    assert(#hooks == 2)
    local original = {}
    local function bank(id)
        local args = { nil, controller, id }
        hooks[1].before(args)
        assert(args[3] == id, "Native weapon identity must never be rewritten")
        local result = hooks[1].after(original)
        assert(next(storage) == nil, "Do not leak a previous call's bank override")
        return result
    end
    assert(bank(63).pointer == 10)
    for _, id in ipairs({ 0, 1, 3, 13, 48, 49, 50, 61, 62, 64, 65, 66, 67 }) do
        assert(bank(id) == original)
    end
    assert(conversions == 1 and lookups == 1, "Unrelated weapons must take the cheap pass-through path")
    enabled = false
    assert(bank(63) == original and conversions == 1)
    enabled = true
    for _, campaign_player in ipairs({ "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000" }) do
        name = campaign_player
        assert(bank(63).pointer == 10)
        banks[10] = nil
        assert(bank(63).pointer == 30, "Use an available melee bank in reduced player setups")
        banks[30] = nil
        assert(bank(63) == original, "Never select an absent animation bank")
        banks[10], banks[30] = true, true
    end
    for _, other_name in ipairs({ "Pl8000", "Pl9000", "Pl1000", "Pl3100_Chapter7_1", "Pl3100_Chapter7_2", "Pl3100_Chapter7_3", "Pl3400" }) do
        name = other_name
        assert(bank(63) == original)
    end
    name, owner = "Pl0000", nil
    assert(bank(63) == original)
    owner = {}
    assert(bank(63) == original, "An inactive or duplicated Ethan is not the current player")
    owner = player
    for _, foreign_path in ipairs({ "CH9/Weapon/Original.pfb", "OtherMod/CH9_WP002/Item.pfb", "BioRand/DlcWeapons/CH9_WP002/Item.pfb" }) do
        path = foreign_path
        assert(bank(63) == original)
    end
    path = "BIORAND/DLCWEAPONLAB/CH9_WP002/ITEM.PFB"
    assert(bank(63).pointer == 10)
    path = nil
    assert(bank(63) == original)
    manager = nil
    assert(bank(63) == original)
end
