local Em8000KneeDown = {}
Em8000KneeDown.__index = Em8000KneeDown

local LARGE_RESIST = 2
local LARGE_REACTION = 2
local EM8000_HAND_MODE = 6
local VANILLA_GROUPS = { [1] = true, [2] = true, [3] = true, [4] = true }

function Em8000KneeDown.new(context)
    return setmetatable({ context = context }, Em8000KneeDown)
end

function Em8000KneeDown:should_force(controller, result, weapon_group)
    if VANILLA_GROUPS[weapon_group] then
        return false
    end
    if result == nil or result:get_field("resistType") ~= LARGE_RESIST then
        return false
    end
    if controller:get_field("MyEm8000ActionStatus") == nil then
        return false
    end

    local think = controller:get_field("MyThink")
    if think == nil or think:get_field("_Mode") == EM8000_HAND_MODE then
        return false
    end
    local flags = controller:get_field("DictForbidDamageReactionTypeFlag")
    return flags ~= nil and flags:call("ContainsKey", LARGE_REACTION)
        and not flags:call("get_Item", LARGE_REACTION)
end

function Em8000KneeDown:install()
    local game = self.context.game
    game:hook("app.Em3000.Em3000ActionController",
        "isEm8000KneeDownDamage(app.EnemyActionController.ResistResultSet, app.Em8000.Em8000Define.WeaponGroup.Group)",
        function(args)
            local controller = game:object(args[2])
            local result = game:object(args[3])
            local weapon_group = sdk.to_int64(args[4])
            thread.get_hook_storage().biorand_force_knee_down =
                self:should_force(controller, result, weapon_group)
        end,
        function(retval)
            local storage = thread.get_hook_storage()
            if storage.biorand_force_knee_down and sdk.to_int64(retval) % 256 == 0 then
                storage.biorand_force_knee_down = false
                return sdk.to_ptr(1)
            end
            storage.biorand_force_knee_down = false
            return retval
        end)
end

return Em8000KneeDown
