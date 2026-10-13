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
    local variant, player, weapon_object = Gauntlet.variants[1], {}, {}
    local position, rotation, joint, same_joints, draw
    local transform = object({}, {
        set_LocalPosition = function(value) position = value end,
        set_LocalRotation = function(value) rotation = value end,
        set_ParentJoint = function(value) joint = value end,
        set_SameJointsConstraint = function(value) same_joints = value end,
    })
    weapon_object = object({}, { get_Transform = function() return transform end })
    local body_mesh, body_material = {}, {}
    local attached_mesh, attached_material
    local body = object({}, { getMesh = function() return body_mesh end,
        get_Material = function() return body_material end })
    local skeleton = object({}, {
        set_DrawDefault = function(value) draw = value end,
        setMesh = function(value) attached_mesh = value end,
        set_Material = function(value) attached_material = value end,
    })
    local requests, off = {}, 0
    local collider = object({}, {
        get_Valid = function() return true end,
        ["getNumCollidables(System.UInt32)"] = function(index) assert(index == 6); return 1 end,
        ["unregisterRequestSet(System.UInt32)"] = function(index) requests[index] = true end,
    })
    local item = object({ ItemDataID = variant.item }, {})
    local hit = {}
    local weapon = object({ EquipParam = object({ JointName = "" }, {}) }, {
        get_GameObject = function() return weapon_object end,
        get_Valid = function() return true end,
        offAttackTrigger = function() off = off + 1 end,
    })
    local hand_entries, hand_states = {}, {}
    for _, left in ipairs({false, true}) do
        local original, material = object({}, {}), object({}, {})
        local state = { mesh = original, material = material, parts = {true, false, false, false, false} }
        hand_states[#hand_states + 1] = state
        hand_entries[#hand_entries + 1] = {
            object = {}, left = left,
            replacement = {}, material = {},
            mesh = object({}, {
                get_Valid = function() return true end,
                getMesh = function() return state.mesh end,
                get_Material = function() return state.material end,
                getPartsEnableCount = function() return #state.parts end,
                getPartsEnable = function(index) return state.parts[index + 1] end,
                setPartsEnable = function(index, value) state.parts[index + 1] = value end,
                setMesh = function(value) state.mesh = value end,
                set_Material = function(value) state.material = value end,
            }),
        }
    end
    local game = {
        player = function() return player end,
        valid = function() return true end,
        component = function(_, owner, kind)
            if owner == player then assert(kind == "via.render.Mesh"); return body end
            assert(owner == weapon_object)
            return ({["app.Item"] = item, ["via.render.Mesh"] = skeleton,
                ["via.physics.RequestSetCollider"] = collider, ["app.Collision.HitController"] = hit})[kind]
        end,
    }
    Vector3f = {new = function(x, y, z) return {x=x, y=y, z=z} end}
    Quaternion = {identity = function() return {w=1, x=0, y=0, z=0} end}
    local adapter = Gauntlet.new(game, function() return true end, "BioRand/DlcWeapons")
    adapter.matches = function(_, owner, entry) return owner == player and entry == variant end
    adapter.session = {player = player, hands = hand_entries}
    for _, entry in ipairs(Gauntlet.variants) do
        variant = entry
        item = object({ItemDataID = variant.item}, {})
        adapter:equip(weapon, variant)
        assert(position.x == 0 and position.y == 0 and position.z == 0)
        assert(rotation.w == 1 and rotation.x == 0 and rotation.y == 0 and rotation.z == 0,
            "Same-joint weapon attachment must have identity rotation")
        assert(joint == "" and same_joints and draw == false)
        assert(attached_mesh == body_mesh and attached_material == body_material)
        assert(adapter.weapon == weapon and adapter.collider == collider and adapter.hit == hit and adapter.equip_pending)
        for i, hand in ipairs(hand_entries) do
            local state = hand_states[i]
            assert(state.mesh == hand.replacement and state.material == hand.material)
            local gauntlet = variant.dual or i == 2
            for part = 1, 5 do
                assert(state.parts[part] == (gauntlet and (part == 3 or part == 4) or not gauntlet and part == 1))
            end
        end
        adapter:reset()
        assert(requests[6] and off > 0 and not adapter.weapon)
        for i, hand in ipairs(hand_entries) do
            assert(not hand.original and hand_states[i].parts[1] and not hand_states[i].parts[3])
        end
    end
end
