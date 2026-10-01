return function()
    local Game = require("BioRand7/game")
    local Em3300Explosions = require("BioRand7/em3300_explosions")
    local list = dofile("tests/lua/helpers.lua").list
    local now = 0
    local warnings, bombs, explosions, effects, destroyed = {}, 0, 0, 0, 0
    local player, object_manager, shell_manager
    local enemy_id_value = 7
    local game = Game.new()
    local hooks = {}
    game.hook = function(_, _, signature, before)
        assert(hooks[signature] == nil, "Hooks must not be registered twice")
        hooks[signature] = before
    end
    local config = { ["biorand-seed"] = 35825 }
    local context = {
        game = game,
        config = { get = function(_, key, default)
            if config[key] ~= nil then return config[key] end
            return default
        end },
        log = { warn = function(_, message) warnings[#warnings + 1] = message end },
    }

    local gating = Em3300Explosions.new(context)
    config["random-enemies"] = false
    config["extra-enemy-amount"] = 1
    assert(gating:enabled(), "Extra-only enemy profiles need the path-clearing behavior")
    config["enemy-evelineelderly-explosive-behavior"] = false
    assert(not gating:enabled())
    config["extra-enemy-amount"] = 0
    config["enemy-evelineelderly-explosive-behavior"] = true
    assert(not gating:enabled())
    config["random-enemies"] = true

    local function object(address, name, tag, position)
        local transform = { call = function(_, method)
            assert(method == "get_Position", "Unexpected transform method: " .. method)
            return position
        end }
        return {
            get_address = function() return address end,
            call = function(self, method, component)
                if method == "get_Valid" then return not self.destroyed end
                if method == "get_Update" then return self.active ~= false end
                if method == "get_Name" then return name end
                if method == "get_Tag" then return tag end
                if method == "get_Folder" then return nil end
                if method == "get_Transform" then return transform end
                if method == "getComponent(System.Type)" then return (self.components or {})[component] end
                error("Unexpected GameObject method: " .. method)
            end,
        }
    end

    local methods = {
        ["app.GameManager:getPlayer()"] = function() return player end,
        ["app.ObjectManager:getEnemyID(via.GameObject)"] = function()
            return { call = function(_, method)
                if method == "get_HasValue" then return enemy_id_value ~= nil end
                assert(method == "get_Value" and enemy_id_value ~= nil)
                return enemy_id_value
            end }
        end,
        ["app.ObjectManager:findObjectInCurrentScene(System.String)"] = function() return nil end,
        ["app.Util:setActive(via.GameObject, System.Boolean, System.Boolean)"] = function(_, enemy, active)
            assert(not active)
            enemy.active = active
        end,
        ["via.GameObject:destroy(via.GameObject)"] = function(_, enemy)
            enemy.destroyed = true
            destroyed = destroyed + 1
        end,
    }
    sdk = {
        to_managed_object = function(value) return value end,
        PreHookResult = { SKIP_ORIGINAL = "skip" },
        typeof = function(name) return name end,
        get_managed_singleton = function(name)
            if name == "app.ObjectManager" then return object_manager end
            if name == "app.ShellManager" then return shell_manager end
            if name == "app.GameManager" then
                return { get_field = function(_, field)
                    assert(field == "GameDifficulty")
                    return 2
                end }
            end
            error("Unexpected singleton: " .. name)
        end,
        find_type_definition = function(name)
            return {
                get_method = function(_, signature)
                    if name == "via.GameObject" and signature ~= "destroy(via.GameObject)" then
                        assert(signature == "get_Valid" or signature == "get_Name" or signature == "get_Tag"
                            or signature == "getComponent(System.Type)", signature)
                        return { call = function(_, object, ...) return object:call(signature, ...) end }
                    end
                    local method = assert(methods[name .. ":" .. signature], signature)
                    return { call = function(_, ...) return method(...) end }
                end,
                get_field = function(_, field)
                    assert(name == "Em4200Effect.IDHolder" and field == "Explosion")
                    return { get_data = function() return "explosion-effect" end }
                end,
            }
        end,
    }
    Vector3f = { new = function(x, y, z) return { x = x, y = y, z = z } end }
    Quaternion = { identity = function() return { x = 0, y = 0, z = 0, w = 1 } end }
    os.clock = function() return now end

    local player_position = { x = 10, y = 0, z = 0 }
    player = object(1, "Pl0000", "", player_position)
    local enemy = object(2, "Em3300_Static", "BioRandExplosiveEm3300", { x = 0, y = 0, z = 0 })
    local vanilla = object(3, "Em3300", "", { x = 0, y = 0, z = 0 })
    local group = list({ [1] = enemy, [3] = vanilla }, 4)
    local groups = list({ [1] = group }, 2)
    object_manager = {
        get_field = function(_, field)
            if field == "PlayerObj" then return player end
            assert(field == "ManagedObjects", field)
            return groups
        end,
        call = function(_, method)
            if method == "findActivePlayer" then return player end
            if method == "findObject(System.String)" then return nil end
            error("ObjectManager has no method " .. method)
        end,
    }
    shell_manager = { call = function(_, signature, owner, target, offset, rotation)
        assert(signature == "createBomb(via.GameObject, via.Transform, via.vec3, via.Quaternion)")
        assert(owner == player and target == enemy:call("get_Transform"))
        assert(offset.x == 0 and offset.y == 0 and offset.z == 0 and rotation.w == 1)
        bombs = bombs + 1
        return { call = function(_, method)
            assert(method == "requestExplosion")
            explosions = explosions + 1
        end }
    end }

    assert(game:difficulty() == 2)
    assert(game:list(nil)() == nil)
    local feature = Em3300Explosions.new(context)
    feature:install()
    assert(hooks["update(via.fsm.ActionArg)"] == nil, "No global think-update hook before discovery")
    feature:update()
    assert(hooks["update(via.fsm.ActionArg)"] ~= nil, "Discovery must enable think suppression")
    assert(feature.states[2].started == nil)
    assert(feature.states[2].delay == nil, "Build the random stream only when the countdown starts")
    assert(feature.states[3] == nil, "Vanilla Eveline must not explode")
    player_position.x = 4
    feature:update()
    local state = feature.states[2]
    assert(state.started == 0 and state.delay >= 3 and state.delay < 8)
    player_position.x = 100
    now = state.delay - 0.01
    feature:update()
    assert(bombs == 0)
    now = state.delay
    feature:update()
    feature:update()
    assert(bombs == 1 and explosions == 1 and destroyed == 0)
    local action = { call = function(_, method)
        if method == "get_enemyID" then return 7 end
        assert(method == "get_gameObj")
        return enemy
    end }
    assert(hooks["update(via.fsm.ActionArg)"]({ [2] = action }) == "skip",
        "The deferred hook must still suppress exploded Eveline's native think action")
    now = now + 0.26
    feature:update()
    assert(destroyed == 1 and enemy.active == false)
    feature:update()
    assert(feature.states[2] == nil and #warnings == 0)

    shell_manager = nil
    enemy.destroyed = false
    enemy.components = { ["app.ObjectEffectManager"] = { call = function(_, method, effect)
        assert(method == "requestEffect(app.EffectID, via.vec3, via.Quaternion, via.GameObject, System.String)")
        assert(effect == "explosion-effect")
        effects = effects + 1
    end } }
    feature:detonate(enemy)
    assert(effects == 1 and #warnings == 0)
    enemy.components = {}
    feature:detonate(enemy)
    assert(#warnings == 1, "Failed explosions must leave a diagnostic")

    local renamed = object(4, "BioRandExtraEnemyStatic_Em3300_1", "BioRandExplosiveEm3300", {})
    assert(feature:is_target(renamed))
    enemy_id_value = nil
    assert(not feature:is_target(renamed))
    assert(feature:game_object({ call = function() return nil end }, nil) == nil)

    object_manager = nil
    feature:update()
    assert(game:player() == nil, "Do not reuse a singleton from a previous scene")

    local calls, maximum = 0, 0
    local props = {}
    for index = 0, 935 do
        local prop = object(100 + index, "Prop" .. index, "", {})
        local call = prop.call
        prop.call = function(...)
            calls = calls + 1
            return call(...)
        end
        props[index] = prop
    end
    groups = list({ [0] = list(props, 936) }, 1)
    object_manager = { get_field = function() return groups end }
    feature:reset()
    now = 0
    for frame = 0, 59 do
        now = frame / 60
        local before = calls
        feature:update()
        maximum = math.max(maximum, calls - before)
        assert(next(feature.states) == nil)
    end
    assert(maximum <= 64 * 3, "Validate each entry once, then read its name and tag")
    assert(calls < 6000, "An idle scene must not perform a full scan every frame")
    print(("  936-object Eveline scan: %d calls/60 frames, peak %d/frame (previously 224640+)")
        :format(calls, maximum))

    local update = hooks["update(via.fsm.ActionArg)"]
    update({ [2] = {} })
    assert(next(feature.states) == nil, "Idle think hooks must not resolve unrelated actions")
end
