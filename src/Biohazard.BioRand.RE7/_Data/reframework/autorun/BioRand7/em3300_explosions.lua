local Rng = require("BioRand7/rng")

local Em3300Explosions = {}
Em3300Explosions.__index = Em3300Explosions

local EM3300_ID = 7
local MARKER_TAG = "BioRandExplosiveEm3300"
local PROXIMITY_SQUARED = 25

function Em3300Explosions.new(context)
    return setmetatable({ context = context, states = {} }, Em3300Explosions)
end

function Em3300Explosions:enabled()
    return self.context.config:get("random-enemies", true)
        and self.context.config:get("enemy-evelineelderly-explosive-behavior", true)
end

function Em3300Explosions:is_target(game_object)
    if game_object == nil or not game_object:call("get_Valid") then
        return false
    end

    local name = game_object:call("get_Name")
    local marked = game_object:call("get_Tag") == MARKER_TAG or name:lower() == "em3300_static"
    if not marked then
        return false
    end
    if name:lower() == "em3300" or name:lower():sub(1, 7) == "em3300_" then
        return true
    end
    local enemy_id = self.context.game:method("app.ObjectManager", "getEnemyID(via.GameObject)")
        :call(nil, game_object)
    return enemy_id:call("get_HasValue") and enemy_id:call("get_Value") == EM3300_ID
end

function Em3300Explosions:game_object(action, action_arg)
    local game_object = action:call("get_gameObj")
    if game_object ~= nil and game_object:call("get_Valid") then
        return game_object
    end
    if action_arg ~= nil then return action_arg:call("get_OwnerGameObject") end
    return nil
end

function Em3300Explosions:near_player(enemy_object)
    local player = self.context.game:player()
    if player == nil then return false end
    local player_transform = player:call("get_Transform")
    local enemy_transform = enemy_object:call("get_Transform")
    if player_transform == nil or enemy_transform == nil then return false end
    local player_position = player_transform:call("get_Position")
    local enemy_position = enemy_transform:call("get_Position")
    local x = player_position.x - enemy_position.x
    local y = player_position.y - enemy_position.y
    local z = player_position.z - enemy_position.z
    return x * x + y * y + z * z <= PROXIMITY_SQUARED
end

function Em3300Explosions:delay(game_object)
    local seed = tonumber(self.context.config:get("biorand-seed", 0)) or 0
    local rng = Rng.for_em3300(seed, self.context.game:address(game_object))
    return 3 + rng:float() * 5
end

function Em3300Explosions:shell_manager(player)
    local shell_manager = self.context.game:component(player, "app.ShellManager")
    if shell_manager ~= nil then
        return shell_manager
    end
    shell_manager = sdk.get_managed_singleton("app.ShellManager")
    if shell_manager ~= nil then
        return shell_manager
    end
    local object_manager = self.context.game:singleton("app.ObjectManager")
    local shell_object = object_manager and object_manager:call("findObject(System.String)", "ShellManager")
    if shell_object == nil then
        shell_object = self.context.game:method("app.ObjectManager", "findObjectInCurrentScene(System.String)")
            :call(nil, "ShellManager")
    end
    return self.context.game:component(shell_object, "app.ShellManager")
end

function Em3300Explosions:detonate(enemy_object)
    local game = self.context.game
    local player = game:player()
    local transform = enemy_object:call("get_Transform")
    if transform == nil then return end
    local shell_manager = self:shell_manager(player)
    local exploded = false

    if player ~= nil and shell_manager ~= nil then
        local ok, error_message = pcall(function()
            local bomb = shell_manager:call(
                "createBomb(via.GameObject, via.Transform, via.vec3, via.Quaternion)",
                player, transform, Vector3f.new(0, 0, 0), Quaternion.identity())
            if bomb ~= nil then
                bomb:call("requestExplosion")
                exploded = true
            end
        end)
        if not ok then
            self.context.log:warn("Unable to create Em3300 bomb: " .. tostring(error_message))
        end
    end

    if not exploded then
        local effects = game:component(enemy_object, "app.ObjectEffectManager")
        if effects ~= nil then
            local ok, error_message = pcall(function()
                local effect_id = game:static_field("Em4200Effect.IDHolder", "Explosion")
                effects:call(
                    "requestEffect(app.EffectID, via.vec3, via.Quaternion, via.GameObject, System.String)",
                    effect_id,
                    transform:call("get_Position"),
                    Quaternion.identity(),
                    enemy_object,
                    "")
                exploded = true
            end)
            if not ok then
                self.context.log:warn("Unable to request Em3300 explosion effect: " .. tostring(error_message))
            end
        end
    end
    if not exploded then
        self.context.log:warn("Em3300 explosion unavailable: no bomb or fallback effect could be created")
    end
end

function Em3300Explosions:despawn(enemy_object)
    local game = self.context.game
    local ok, error_message = pcall(function()
        game:method("app.Util", "setActive(via.GameObject, System.Boolean, System.Boolean)")
            :call(nil, enemy_object, false, false)
    end)
    if not ok then self.context.log:warn("Unable to deactivate Em3300: " .. tostring(error_message)) end
    ok, error_message = pcall(function()
        game:method("via.GameObject", "destroy(via.GameObject)"):call(nil, enemy_object)
    end)
    if not ok then self.context.log:warn("Unable to destroy Em3300: " .. tostring(error_message)) end
end

function Em3300Explosions:update_object(enemy_object)
    local address = self.context.game:address(enemy_object)
    local state = self.states[address]
    if state == nil then
        state = { started = nil, delay = self:delay(enemy_object), exploded = nil, despawned = false }
        self.states[address] = state
    end
    if state.despawned then
        return true
    end

    local now = os.clock()
    if state.exploded ~= nil then
        if now - state.exploded >= 0.25 then
            state.despawned = true
            self:despawn(enemy_object)
        end
        return true
    end
    if state.started == nil then
        if self:near_player(enemy_object) then
            state.started = now
        end
        return false
    end
    if now - state.started >= state.delay then
        state.exploded = now
        self:detonate(enemy_object)
        return true
    end
    return false
end

function Em3300Explosions:update()
    if not self:enabled() then
        return
    end
    local active = {}
    local game = self.context.game
    local object_manager = game:singleton("app.ObjectManager")
    if object_manager == nil then return end
    local groups = object_manager:get_field("ManagedObjects")
    for group in game:list(groups) do
        for game_object in game:list(group) do
            if self:is_target(game_object) then
                local address = game:address(game_object)
                active[address] = true
                self:update_object(game_object)
            end
        end
    end
    for address in pairs(self.states) do
        if not active[address] then
            self.states[address] = nil
        end
    end
end

function Em3300Explosions:install()
    local game = self.context.game
    game:hook("app.fsm.EnemyThinkAction", "start(via.fsm.ActionArg)", function(args)
        if not self:enabled() then
            return
        end
        local action = game:object(args[2])
        if action:call("get_enemyID") ~= EM3300_ID then
            return
        end
        local enemy_object = self:game_object(action, game:object(args[3]))
        if self:is_target(enemy_object) then
            local state = self.states[game:address(enemy_object)]
            if state ~= nil and state.despawned then
                self.states[game:address(enemy_object)] = nil
            end
        end
    end)

    game:hook("app.fsm.EnemyThinkAction", "update(via.fsm.ActionArg)", function(args)
        if not self:enabled() then
            return
        end
        local action = game:object(args[2])
        if action:call("get_enemyID") ~= EM3300_ID then
            return
        end
        local enemy_object = self:game_object(action, game:object(args[3]))
        if self:is_target(enemy_object) and self:update_object(enemy_object) then
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
    end)
end

function Em3300Explosions:reset()
    self.states = {}
end

return Em3300Explosions
