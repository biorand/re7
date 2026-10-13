return function()
    local Gauntlet = require("BioRand7/dlc_gauntlet")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local player, owner, ready, fallback_kind = nil, nil, true, 10
    local name = "Pl0000"
    player = object({}, {get_Name = function() return name end})
    owner = player
    local transform = object({}, {get_GameObject = function() return owner end, get_Parent = function() return nil end})
    local arms = {}
    for _, field in ipairs({"RArmMesh", "LArmMesh"}) do
        local hand = object({}, {get_Transform = function() return transform end})
        arms[field] = object({}, {get_GameObject = function() return hand end,
            getMesh = function() return {} end, get_Material = function() return {} end,
            get_MeshReady = function() return ready end, get_MaterialReady = function() return ready end})
    end
    local mesh_controller = object(arms, {})
    local fallback_list = {}
    local fallback = object({}, {get_BankID = function() return 0 end, get_BankType = function() return fallback_kind end,
        get_MotionList = function() return fallback_list end})
    local count, banks, allocations = 0, {}, 0
    local motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function() return fallback end,
        getDynamicMotionBankCount = function() return count end,
        setDynamicMotionBankCount = function(value) count = value end,
        setDynamicMotionBank = function(index, value) banks[index] = value end,
        getDynamicMotionBank = function(index) return banks[index] end,
    })
    local controller = object({Motion = motion, MotionManager = {}}, {})
    local item_manager = object({}, {findItemData = function(id)
        return object({ItemPrefab = object({}, {get_Path = function() return "BioRand/DlcWeapons/" .. id .. "/Item.pfb" end})}, {})
    end})
    local game = {
        player = function() return player end,
        singleton = function(_, kind)
            return kind == "app.ItemManager" and item_manager or object({}, {get_IsSceneLoading = function() return false end})
        end,
        component = function(_, target, kind)
            assert(target == player)
            return ({["app.PlayerMotionController"] = controller, ["app.PlayerSequenceManager"] = {},
                ["app.PlayerMeshController"] = mesh_controller})[kind]
        end,
    }
    sdk = {create_instance = function(kind)
        allocations = allocations + 1
        if kind ~= "via.motion.DynamicMotionBank" then return object({}, {}) end
        local values = {}
        return object({}, {
            set_MotionList = function(value) values.list = value end,
            set_OverwriteBankID = function(value) assert(value) end,
            set_OverwriteBankType = function(value) assert(value) end,
            set_BankID = function(value) values.id = value end,
            set_BankType = function(value) values.kind = value end,
            get_BankID = function() return values.id end,
            get_BankType = function() return values.kind end,
            get_MotionList = function() return values.list end,
        })
    end}
    local resources = 0
    local function adapter()
        local result = Gauntlet.new(game, function() return true end, "BioRand/DlcWeapons")
        result.resource = function(_, kind, path) resources = resources + 1; return {kind = kind, path = path} end
        return result
    end
    local pending = adapter()
    fallback_kind = 0
    local ok, reason = pending:prepare(player)
    assert(not ok and reason == "campaign axe fallback bank" and count == 0 and allocations == 0 and resources == 0,
        "findMotionBank's default fallback is not evidence that the requested bank exists")
    fallback_kind, ready = 10, false
    ok, reason = pending:prepare(player)
    assert(not ok and reason == "RArmMesh resources" and resources == 0)
    ready, owner = true, {}
    ok, reason = pending:prepare(player)
    assert(not ok and reason == "RArmMesh ownership" and allocations == 0 and resources == 0,
        "Never adopt a foreign or stale arm renderer")
    owner = player
    for _, player_name in ipairs({"Pl0000", "Pl0000_Chapter1", "Pl2000", "Pl2100", "Pl3000"}) do
        name, count, banks = player_name, 0, {}
        local current = adapter()
        assert(current:prepare(player) and count == 9 and current:owns_banks())
        assert(current.session.hands[1].mesh == arms.RArmMesh and current.session.hands[1].left == false)
        assert(current.session.hands[2].mesh == arms.LArmMesh and current.session.hands[2].left == true)
        for i = 0, 8 do
            assert(banks[i]:call("get_BankType") == Gauntlet.variants[math.floor(i / 3) + 1].bank)
            if i % 3 == 2 then assert(banks[i]:call("get_MotionList") == fallback_list) end
        end
        local previous_allocations = allocations
        assert(current:prepare(player) and count == 9 and allocations == previous_allocations, "Preparation is idempotent")
    end
end
