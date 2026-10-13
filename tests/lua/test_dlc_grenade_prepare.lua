return function()
    local Grenade = require("BioRand7/dlc_grenade")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local banks, resources, released, tracks = {}, {}, 0, 0
    local fallback_ready, conflict = false, false
    local original_list = {}
    local fallback = object({}, { get_BankID = function() return 0 end, get_BankType = function() return 10 end,
        get_MotionList = function() return original_list end })
    local motion = object({}, {
        ["findMotionBank(System.UInt32, System.UInt32)"] = function(id, kind)
            assert(id == 0)
            if kind == 10 then return fallback_ready and fallback or nil end
            if conflict then return object({}, { get_BankType = function() return kind end }) end
        end,
        getDynamicMotionBankCount = function() return #banks end,
        setDynamicMotionBankCount = function(count) assert(count == #banks + 1) end,
        setDynamicMotionBank = function(index, bank) assert(index == #banks); banks[index + 1] = bank end,
        getDynamicMotionBank = function(index) return banks[index + 1] end,
    })
    local controller = object({ Motion = motion, MotionManager = {} }, {})
    local player = object({}, { get_Name = function() return "Pl0000" end })
    local current_player = player
    local inventory = {}
    local items = object({}, { findItemData = function(id)
        return object({ ItemPrefab = object({}, { get_Path = function() return "BioRand/DlcWeapons/" .. id .. "/Item.pfb" end }) }, {})
    end })
    local game = { player = function() return current_player end, chapter = function() return 3 end,
        singleton = function(_, kind) assert(kind == "app.ItemManager"); return items end,
        component = function(_, owner, kind)
            assert(owner == player)
            return kind == "app.PlayerMotionController" and controller or kind == "app.Inventory" and inventory
        end }
    sdk = {
        create_resource = function(kind, path)
            assert(kind == "via.motion.MotionListResource")
            assert(not resources[path], "Reuse shared motion holders")
            local holder = object({}, { get_ResourcePath = function() return path end })
            resources[path] = holder
            return { add_ref = function(self) return self end, release = function() released = released + 1 end,
                create_holder = function(_, holder_kind) assert(holder_kind == kind .. "Holder"); return holder end }
        end,
        create_instance = function(kind)
            if kind ~= "via.motion.DynamicMotionBank" then
                assert(kind == "app.CH8SequenceTrackObject.CH8PlayerPinPulledTrack" or kind == "app.CH8SequenceTrackObject.CH8PlayerThrowTrack")
                tracks = tracks + 1; return object({}, {})
            end
            local fields = {}
            return object(fields, {
                set_MotionList = function(value) fields.list = value end,
                set_OverwriteBankID = function(value) assert(value) end, set_BankID = function(value) fields.id = value end,
                set_OverwriteBankType = function(value) assert(value) end, set_BankType = function(value) fields.kind = value end,
                get_BankID = function() return fields.id end, get_BankType = function() return fields.kind end,
            })
        end,
    }
    local adapter = Grenade.new(game, function() return true end, "BioRand/DlcWeapons")
    assert(not adapter:prepare(player) and #banks == 0 and next(resources) == nil)
    fallback_ready, conflict = true, true
    assert(not pcall(function() adapter:prepare(player) end) and #banks == 0)
    conflict = false
    adapter.motion_info = { stale = true }
    assert(adapter:prepare(player) and #banks == 9 and adapter:owns_banks())
    assert(adapter.motion_info == nil, "Never carry MotionInfo across player sessions")
    assert(released == 4 and tracks == 2)
    for i = 1, 3 do
        assert(banks[i * 3]:get_field("list") == original_list)
        assert(banks[i * 3]:call("get_BankID") == 0 and banks[i * 3]:call("get_BankType") == 9949 + i)
    end
    assert(adapter:prepare(player) and #banks == 9 and released == 4)
    local original = banks[1]; banks[1] = {}
    assert(not pcall(function() adapter:prepare(player) end), "Never overwrite banks after ownership changes")
    banks[1] = original
    current_player = nil; assert(not adapter:owns_banks())
    adapter.motion_info = { stale = true }
    adapter:reset()
    assert(adapter.motion_info == nil and adapter.reset_pending, "Reset must discard scene-owned motion scratch data without native calls")
end
