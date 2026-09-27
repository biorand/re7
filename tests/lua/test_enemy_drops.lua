return function()
    local EnemyDrops = require("BioRand7/enemy_drops")
    local StaticMia = require("BioRand7/static_mia")

    local function object(methods, fields, type_name)
        return {
            call = function(self, name, ...)
                assert(methods[name], "Unexpected method " .. name)
                return methods[name](self, ...)
            end,
            get_field = function(_, name)
                assert(fields and fields[name] ~= nil, "Unexpected field " .. name)
                return fields[name]
            end,
            get_type_definition = function()
                return { get_full_name = function() return type_name end }
            end,
        }
    end

    local function constant(value)
        return function() return value end
    end

    local function vector(x, y, z)
        return { x = x, y = y, z = z }
    end
    Vector3f = { new = vector }

    local configuration = {
        ["enemy-drop-ammo-min"] = 0.1,
        ["enemy-drop-ammo-max"] = 0.1,
    }
    local difficulty = { GameDifficulty = 0 }
    local hooks = {}
    local singletons = {
        ["app.GameManager"] = object({}, difficulty),
        ["app.ObjectManager"] = object({ findActivePlayer = constant(nil) }, { PlayerObj = false }),
    }
    local context = {
        config = { get = function(_, key, default)
            if configuration[key] ~= nil then return configuration[key] end
            return default
        end },
        game = {
            singleton = function(_, name) return singletons[name] end,
            address = constant(1250999896491),
            object = function(_, value) return value end,
            component = constant(nil),
            hook = function(_, type_name, signature, before)
                hooks[type_name .. ":" .. signature] = before
            end,
        },
        features = {},
        log = { info = function() end, warn = function() end },
    }
    context.features.static_mia = StaticMia.new(context)
    local drops = EnemyDrops.new(context)
    local transform = object({
        get_Position = constant(vector(-1.125, 2, 3)),
        get_AxisY = constant(vector(0, 1, 0)),
        get_AxisZ = constant(vector(0, 0, 1)),
        get_AxisX = constant(vector(1, 0, 0)),
    })
    local enemy = object({
        get_Name = constant("BioRand_Em8000_1"),
        get_Transform = constant(transform),
        get_Folder = constant(nil),
    })
    local controller = object({ get_GameObject = constant(enemy) }, nil, "app.EnemyActionController")
    assert(drops:enemy_type(controller, enemy) == "Em8000")
    assert(drops:enemy_type(object({}, nil, "app.Em3000.Em3000ActionController"), enemy) == "Em8000")
    local unnamed_enemy = object({ get_Name = constant("enemy") })
    assert(drops:enemy_type(controller, unnamed_enemy) == nil)
    assert(drops:enemy_type(object({}, nil, "app.Em4000ActionController"), unnamed_enemy) == "Em4000")

    local fixed_rng = { int = function(_, minimum, maximum)
        assert(minimum == 3 and maximum == 3)
        return minimum
    end }
    assert(drops:stack_amount("HandgunBullet", fixed_rng) == 4)
    difficulty.GameDifficulty = 2
    assert(drops:stack_amount("HandgunBullet", fixed_rng) == 2)

    local fallback_direction = drops:wall_direction(enemy, vector(0, 0, 0))
    assert(fallback_direction.z == 1)
    local player_transform = object({ get_Position = constant(vector(1, 0, 0)) })
    local player = object({ get_Transform = constant(player_transform) })
    singletons["app.ObjectManager"] = object({}, { PlayerObj = player })
    local cast_terrain_ray = drops.cast_terrain_ray
    drops.cast_terrain_ray = constant(nil)
    assert(drops:wall_direction(enemy, vector(0, 0, 0)).x == 1)
    drops.cast_terrain_ray = cast_terrain_ray

    local hit, normal = vector(0, 0, 0), vector(0, 1, 0)
    local contact = object({}, { Position = hit, Normal = normal })
    local query = object({
        clearOptions = function() end,
        enableNearSort = function() end,
        enableOneHitBreak = function() end,
        disableInsideHits = function() end,
        set_FilterInfo = function() end,
        ["setRay(via.vec3, via.vec3)"] = function() end,
    })
    local result = object({
        clear = function() end,
        get_Finished = constant(true),
        get_AsyncResult = constant(0),
        get_NumContactPoints = constant(1),
        ["getContactPoint(System.UInt32)"] = constant(contact),
    })
    sdk = {
        PreHookResult = { SKIP_ORIGINAL = 1 },
        create_instance = function(name)
            if name == "via.physics.CastRayQuery" then return query end
            assert(name == "via.physics.CastRayResult")
            return result
        end,
    }
    singletons["app.Collision.CollisionSystem"] = object({
        ["createFilterInfo(System.UInt32, System.UInt32)"] = constant({}),
    })
    context.game.static_field = constant(1)
    context.game.method = function(_, type_name, signature)
        assert(type_name == "via.physics.System")
        assert(signature == "castRay(via.physics.CastRayQuery, via.physics.CastRayResult)")
        return { call = function(_, _, actual_query, actual_result)
            assert(actual_query == query and actual_result == result)
        end }
    end
    local actual_hit, actual_normal = drops:cast_terrain_ray(vector(0, 1, 0), vector(0, -1, 0))
    assert(actual_hit == hit and actual_normal == normal)

    local spawn_count = 0
    drops.spawn = function() spawn_count = spawn_count + 1 end
    drops:install()
    local damage = object({ get_GameObject = constant(enemy), get_enemyActionController = constant(nil) })
    local on_death = hooks["app.EnemyDamageController:doDie(app.DamageController.DamageRecord)"]
    on_death({ nil, damage })
    on_death({ nil, damage })
    assert(spawn_count == 1)
    hooks["app.EnemyActionController:spawn(app.EnemySpawnInfo, app.EnemySpawnInfoOptionBase)"]({ nil, controller })
    on_death({ nil, damage })
    assert(spawn_count == 2)

    local guid = object({ ["ToString()"] = constant("01234567-89ab-cdef-0123-456789abcdef") })
    local empty_guid = object({ ["ToString()"] = constant("00000000-0000-0000-0000-000000000000") })
    local mia_controller = object({}, { SpawnerGuid = guid, ActualUsingGuid = empty_guid })
    local mia = object({
        get_Name = constant("BioRandExtraEnemyStatic_Em2000_1"),
        get_Transform = constant(transform),
        get_Folder = constant(nil),
    })
    local static_mia = context.features.static_mia
    local keys = static_mia:keys(mia_controller, mia)
    assert(#keys == 2 and keys[1] == "guid:spawner:01234567-89ab-cdef-0123-456789abcdef")
    assert(keys[2] == "fallback::BioRandExtraEnemyStatic_Em2000_1:-112:200:300")
    assert(static_mia:remember(mia_controller, mia))
    assert(static_mia:is_killed(mia_controller, mia))
    local deactivated = false
    context.game.method = function(_, type_name, signature)
        assert(type_name == "app.Util" and signature == "setActive(via.GameObject, System.Boolean, System.Boolean)")
        return { call = function(_, _, target, active, recursive)
            assert(target == mia and active == false and recursive == false)
            deactivated = true
        end }
    end
    assert(static_mia:suppress(mia_controller, mia) and deactivated)
end
