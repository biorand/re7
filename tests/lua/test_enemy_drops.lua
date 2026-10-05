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
        get_Rotation = constant({ x = 0, y = 0, z = 0, w = 1 }),
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

    configuration["enemy-drop-probability"] = 0
    configuration["enemy-drop-ammo-only-available-weapons"] = false
    context.game.enemy_identity = constant("boss-test")
    for _, boss in ipairs({ "Em2000", "Em3001", "Em3600", "Em8000", "Em8001", "Em8100" }) do
        assert(drops:probability(boss) == 1)
        local id, amount = drops:select(enemy, 1, boss)
        assert(id == "RemedyL" and amount == 1, "Bosses must reward even an empty configured pool")
    end
    assert(drops:select(enemy, 1, "Em4000") == nil, "Normal enemies still honor zero drop probability")
    configuration["enemy-drop-ratio-chemicalm"] = 1
    assert(drops:select(enemy, 1, "Em3600") == "ChemicalM", "Boss rewards must honor configured weights")
    configuration["enemy-drop-ratio-chemicalm"] = nil

    configuration["enemy-drop-probability"] = 1
    configuration["enemy-drop-ratio-flower"] = 0.05
    local supplements_id, supplements_amount = drops:select(enemy, 1, "Em4000")
    assert(supplements_id == "Flower" and supplements_amount == 1, "Normal enemies must be able to drop Supplements")
    configuration["enemy-drop-ratio-flower"] = 0
    assert(drops:select(enemy, 1, "Em4000") == nil, "Zero weight must exclude Supplements")
    configuration["enemy-drop-ratio-flower"] = nil
    configuration["enemy-drop-probability"] = nil

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

    -- The native factory returns a drop at (0, 0, 0), not at its owner's location.
    -- Exercise the complete placement path with and without a terrain hit.
    local placed, detached, rotated
    local drop_transform = object({
        get_Position = constant(vector(0, 0, 0)),
        get_Rotation = constant({}),
        ["setParent(via.Transform, System.Boolean)"] = function(_, parent, keep_world)
            detached = parent == nil and keep_world
        end,
        set_Position = function(_, value) placed = value end,
        set_Rotation = function(_, value) rotated = value end,
    })
    local valid = true
    local drop_object = object({ get_Transform = constant(drop_transform), get_Valid = function() return valid end })
    singletons["app.ItemManager"] = object({
        ["createDropItemInstance(via.GameObject, System.String, System.Int32)"] = function(_, owner, id, amount)
            assert(owner == enemy and id == "Gunpowder" and amount == 1)
            return drop_object
        end,
    })
    local placement_drops = EnemyDrops.new(context)
    placement_drops.select = function() return "Gunpowder", 1 end
    for _, ground_found in ipairs({ true, false }) do
        placed, detached, rotated = nil, nil, nil
        placement_drops.project_to_ground = function(_, position)
            assert(position.x == -1.125 and position.y == 2 and position.z == 3)
            return ground_found and vector(position.x, 1.8, position.z) or nil
        end
        placement_drops:spawn(controller, enemy, 0)
        assert(placed == nil and detached == nil, "Do not move an uninitialized prefab")
        placement_drops:update()
        assert(detached == nil, "The pickup child must update before detachment")
        placement_drops:update()
        assert(detached and rotated.w == 1)
        assert(placed.x == -1.125 and placed.z == 3)
        assert(placed.y == (ground_found and 1.8 or 2))
        assert(#placement_drops.pending == 0)
    end
    placement_drops:spawn(controller, enemy, 0)
    valid = false
    placement_drops:update()
    assert(#placement_drops.pending == 0, "Discard a destroyed prefab without moving it")
    valid = true
    placement_drops:spawn(controller, enemy, 0)
    placement_drops:reset()
    assert(#placement_drops.pending == 0, "A new session must not retain old prefab references")

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

    context.game.guid_string = require("BioRand7/game").guid_string
    local guid = object({}, {mData1=0x01234567,mData2=0x89ab,mData3=0xcdef,
        mData4_0=0x01,mData4_1=0x23,mData4_2=0x45,mData4_3=0x67,
        mData4_4=0x89,mData4_5=0xab,mData4_6=0xcd,mData4_7=0xef})
    local empty_guid = object({}, {mData1=0,mData2=0,mData3=0,mData4_0=0,mData4_1=0,
        mData4_2=0,mData4_3=0,mData4_4=0,mData4_5=0,mData4_6=0,mData4_7=0})
    local mia_controller = object({}, { SpawnerGuid = guid, ActualUsingGuid = empty_guid })
    local mia = object({
        get_Name = constant("BioRandExtraEnemyStatic_Em2000_1"),
        get_Transform = constant(transform),
        get_Folder = constant(nil),
    })
    local static_mia = context.features.static_mia
    assert(not static_mia:is_killed({}, {}), "No reflection is needed until a static Mia has died")
    local keys = static_mia:keys(mia_controller, mia)
    assert(#keys == 2 and keys[1] == "guid:spawner:01234567-89ab-cdef-0123-456789abcdef")
    assert(keys[2] == "fallback::BioRandExtraEnemyStatic_Em2000_1:-112:200:300")
    assert(static_mia:remember(mia_controller, mia))
    assert(static_mia:is_killed(mia_controller, mia))
    local guid_only = object({ get_Name = constant("BioRandExtraEnemyStatic_Em2000_1") })
    assert(static_mia:is_killed(mia_controller, guid_only), "Known GUIDs must not read folder or transform")
    assert(static_mia:is_killed(object({}, { SpawnerGuid = empty_guid, ActualUsingGuid = empty_guid }), mia),
        "Position-based fallback must still work when neither GUID is available")
    local deactivated = false
    context.game.method = function(_, type_name, signature)
        assert(type_name == "app.Util" and signature == "setActive(via.GameObject, System.Boolean, System.Boolean)")
        return { call = function(_, _, target, active, recursive)
            assert(target == mia and active == false and recursive == false)
            deactivated = true
        end }
    end
    assert(static_mia:suppress(mia_controller, mia) and deactivated)

    static_mia:reset()
    deactivated = false
    local mia_action = object({ get_GameObject = constant(mia) }, { SpawnerGuid = guid, ActualUsingGuid = empty_guid })
    static_mia:install()
    drops:death(mia_action, mia_action)
    local update_mia = hooks["app.Em2000.Em2000ActionController:doUpdate()"]
    assert(update_mia({ nil, mia_action }) == nil and not deactivated,
        "Lethal damage must allow Mia's native death animation to update")
    drops:death(mia_action, mia_action)
    assert(not deactivated, "Duplicate death notifications must not hide the dying actor")
    hooks["app.EnemyActionController:finishDead(System.Boolean, System.Boolean)"]({ nil, mia_action })
    assert(update_mia({ nil, mia_action }) == sdk.PreHookResult.SKIP_ORIGINAL and deactivated)
end
