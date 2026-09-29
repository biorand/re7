local Data = require("BioRand7/data")
local Rng = require("BioRand7/rng")

local EnemyDrops = {}
EnemyDrops.__index = EnemyDrops

local DIFFICULTY_FACTORS = { [0] = 1.5, [1] = 1.0, [2] = 0.75 }
local WALL_CLEARANCES = { 0.6, 0.9, 1.2 }

local function round(value)
    local lower = math.floor(value)
    local fraction = value - lower
    if fraction > 0.5 or (fraction == 0.5 and lower % 2 ~= 0) then
        return lower + 1
    end
    return lower
end

local function enemy_id(name)
    local digits = name:match("[Ee][Mm](%d%d%d%d)")
    if digits ~= nil then
        return "Em" .. digits
    end
end

local function vector(x, y, z)
    return Vector3f.new(x, y, z)
end

local function add(left, right)
    return vector(left.x + right.x, left.y + right.y, left.z + right.z)
end

local function subtract(left, right)
    return vector(left.x - right.x, left.y - right.y, left.z - right.z)
end

local function multiply(value, scalar)
    return vector(value.x * scalar, value.y * scalar, value.z * scalar)
end

local function horizontal_normal(value)
    local length = math.sqrt(value.x * value.x + value.z * value.z)
    if length <= 0.0001 then
        return nil
    end
    return vector(value.x / length, 0, value.z / length)
end

function EnemyDrops.new(context)
    return setmetatable({ context = context, dropped = {}, generations = {}, pending = {} }, EnemyDrops)
end

function EnemyDrops:config(enemy_key, fallback_key, default)
    local value = self.context.config:get(enemy_key)
    if value == nil then
        value = self.context.config:get(fallback_key, default)
    end
    return value
end

function EnemyDrops:enemy_type(source, game_object)
    local runtime_name = source:get_type_definition():get_full_name()
    local runtime_id = enemy_id(runtime_name)
    local object_id = enemy_id(game_object:call("get_Name"))
    if runtime_id == "Em3000" and object_id == "Em8000" then
        return object_id
    end
    return runtime_id or object_id
end

function EnemyDrops:probability(enemy_type)
    local probability = self.context.config:get("enemy-drop-probability", 0.5)
    local config_id = Data.drop_config_ids[enemy_type]
    if config_id ~= nil then
        probability = self.context.config:get("enemy-drop-probability-" .. config_id, probability)
    end
    return math.max(0, math.min(1, probability))
end

function EnemyDrops:rng(game_object, generation)
    local seed = tonumber(self.context.config:get("biorand-seed", 0)) or 0
    return Rng.for_enemy(seed, self.context.game:enemy_identity(game_object))
end

function EnemyDrops:stack_amount(item_id, rng)
    if not Data.ammo[item_id] then
        return 1
    end

    local limit = Data.stack_limits[item_id] or 1
    local minimum = self:config("enemy-drop-ammo-min", "item-drop-ammo-min", 0.1)
    local maximum = self:config("enemy-drop-ammo-max", "item-drop-ammo-max", 0.4)
    if maximum < minimum then
        minimum, maximum = maximum, minimum
    end

    local min_amount = math.max(1, round(minimum * limit))
    local max_amount = math.max(min_amount, math.min(limit, round(maximum * limit)))
    local amount = rng:int(min_amount, max_amount)
    if self:config("enemy-drop-respect-difficulty", "item-drop-respect-difficulty", true) then
        local difficulty = self.context.game:singleton("app.GameManager"):get_field("GameDifficulty")
        amount = math.max(1, round(amount * (DIFFICULTY_FACTORS[difficulty] or 1)))
    end
    return amount
end

function EnemyDrops:candidates(rng, boss)
    local candidates = {}
    local allowed_ammo = nil
    if self:config("enemy-drop-ammo-only-available-weapons", "item-drop-ammo-only-available-weapons", true) then
        local chapter = self.context.game:singleton("app.GameFlowFsmManager"):call("get_CurrentMainGameFlow")
        allowed_ammo = Data.chapter_ammo[chapter]
    end

    for _, item_id in ipairs(Data.generic_drop_items) do
        local allowed_for_boss = not boss or Data.boss_drop_items[item_id]
        local allowed_for_chapter = allowed_ammo == nil or not Data.ammo[item_id] or allowed_ammo[item_id]
        if allowed_for_boss and allowed_for_chapter then
            local ratio = self:config(
                "enemy-drop-ratio-" .. item_id:lower(),
                "item-drop-ratio-" .. item_id:lower(),
                0)
            if ratio > 0 then
                candidates[#candidates + 1] = { value = item_id, weight = ratio * 100 }
            end
        end
    end

    if self:config("enemy-drop-valuable-weapon", "item-drop-valuable-weapon", false) then
        candidates[#candidates + 1] = { value = "LiquidBomb", weight = 1 }
    end
    if self:config("enemy-drop-valuable-lock-pick", "item-drop-valuable-lock-pick", false) then
        candidates[#candidates + 1] = { value = "CylinderKey", weight = 3 }
    end
    if self:config("enemy-drop-valuable-repair-kit", "item-drop-valuable-repair-kit", false) then
        candidates[#candidates + 1] = { value = "RepairKit", weight = 3 }
    end
    if self:config("enemy-drop-valuable-dlc-coin", "item-drop-valuable-dlc-coin", false) then
        for _, coin in ipairs(Data.dlc_coin_weights) do
            candidates[#candidates + 1] = {
                value = coin.id,
                weight = rng:int(coin.minimum, coin.maximum),
            }
        end
    end
    if self.context.config:get("allow-dlc-items", false)
        and self:config("enemy-drop-valuable-birthday-skill", "item-drop-valuable-birthday-skill", false) then
        candidates[#candidates + 1] = {
            value = Data.birthday_skills[rng:int(1, #Data.birthday_skills)],
            weight = 3,
        }
    end
    return candidates
end

function EnemyDrops:select(game_object, generation, enemy_type)
    local rng = self:rng(game_object, generation)
    local probability = self:probability(enemy_type)
    if probability <= 0 or (probability < 1 and not rng:chance(probability)) then
        return nil
    end

    local candidates = self:candidates(rng, Data.bosses[enemy_type] == true)
    if #candidates == 0 then
        return nil
    end
    local item_id = rng:weighted(candidates)
    local amount = self:stack_amount(item_id, rng)
    local multiplier = math.max(1, Data.drop_multipliers[enemy_type] or 1)
    if multiplier > 1 then
        amount = round(math.min(Data.stack_limits[item_id] or 1, amount * multiplier))
    end
    return item_id, amount
end

function EnemyDrops:cast_terrain_ray(start_position, end_position)
    local ok, hit_position, hit_normal = pcall(function()
        local game = self.context.game
        local collision = game:singleton("app.Collision.CollisionSystem")
        local layer = game:static_field("app.Collision.CollisionSystem.Layer", "TerrainCheck")
        local mask = game:static_field("app.Collision.CollisionSystem.MaskTerrain", "TbEmHit")
        local filter = collision:call("createFilterInfo(System.UInt32, System.UInt32)", layer, mask)
        local query = sdk.create_instance("via.physics.CastRayQuery")
        local result = sdk.create_instance("via.physics.CastRayResult")

        query:call("clearOptions")
        query:call("enableNearSort")
        query:call("enableOneHitBreak")
        query:call("disableInsideHits")
        query:call("set_FilterInfo", filter)
        query:call("setRay(via.vec3, via.vec3)", start_position, end_position)
        result:call("clear")
        game:method("via.physics.System", "castRay(via.physics.CastRayQuery, via.physics.CastRayResult)")
            :call(nil, query, result)

        if not result:call("get_Finished") or result:call("get_AsyncResult") ~= 0
            or result:call("get_NumContactPoints") == 0 then
            return nil
        end
        local contact = result:call("getContactPoint(System.UInt32)", 0)
        return contact:get_field("Position"), contact:get_field("Normal")
    end)
    if not ok then
        self.context.log:info("Enemy drop terrain ray failed: " .. tostring(hit_position), true)
        return nil
    end
    return hit_position, hit_normal
end

function EnemyDrops:project_to_ground(position)
    local hit, normal = self:cast_terrain_ray(
        vector(position.x, position.y + 0.25, position.z),
        vector(position.x, position.y - 50, position.z))
    if hit == nil or normal.y < 0.5 or hit.y > position.y + 0.25 then
        return nil
    end
    return vector(position.x, hit.y, position.z)
end

function EnemyDrops:wall_direction(enemy_object, position)
    local object_manager = self.context.game:singleton("app.ObjectManager")
    local player = object_manager and (object_manager:get_field("PlayerObj") or object_manager:call("findActivePlayer"))
    local player_transform = player and player:call("get_Transform")
    local player_position = player_transform and player_transform:call("get_Position")
    local direction = player_position and horizontal_normal(subtract(player_position, position))
    if direction ~= nil then
        local _, wall_normal = self:cast_terrain_ray(
            add(position, multiply(direction, 0.75)),
            add(position, multiply(direction, -0.75)))
        local wall_direction = wall_normal and horizontal_normal(wall_normal)
        if wall_direction ~= nil then
            if wall_direction.x * direction.x + wall_direction.z * direction.z < 0 then
                wall_direction = multiply(wall_direction, -1)
            end
            return wall_direction
        end
        return direction
    end

    local transform = enemy_object:call("get_Transform")
    return horizontal_normal(transform:call("get_AxisY"))
        or horizontal_normal(transform:call("get_AxisZ"))
        or horizontal_normal(transform:call("get_AxisX"))
end

function EnemyDrops:project_hive_drop(enemy_object, position)
    local direction = self:wall_direction(enemy_object, position)
    if direction == nil then
        return nil
    end
    for _, distance in ipairs(WALL_CLEARANCES) do
        local ground = self:project_to_ground(add(position, multiply(direction, distance)))
        if ground ~= nil then
            return ground
        end
    end
    return nil
end

function EnemyDrops:spawn(source, enemy_object, generation)
    local enemy_type = self:enemy_type(source, enemy_object)
    local item_id, amount = self:select(enemy_object, generation, enemy_type)
    if item_id == nil then
        return
    end

    -- Newly created drops start at the world origin. Their transform does not yet
    -- contain the enemy's location, even when the factory receives its owner.
    local enemy_transform = enemy_object:call("get_Transform")
    local position = enemy_transform:call("get_Position")
    local rotation = enemy_transform:call("get_Rotation")
    local item_manager = self.context.game:singleton("app.ItemManager")
    if item_manager == nil then
        self.context.log:warn("Unable to create enemy drop because app.ItemManager is unavailable")
        return
    end
    local drop = item_manager:call(
        "createDropItemInstance(via.GameObject, System.String, System.Int32)", enemy_object, item_id, amount)
    if drop == nil then
        self.context.log:warn("Unable to create enemy drop " .. item_id)
        return
    end
    local transform = drop:call("get_Transform")
    if transform == nil then return end
    local ground = self:project_to_ground(position)
    if ground == nil and Data.single_drop_per_spawn[enemy_type] then
        ground = self:project_hive_drop(enemy_object, position)
    end
    self.pending[#self.pending + 1] = { drop = drop, position = ground or position, rotation = rotation }
end

function EnemyDrops:update()
    for index = #self.pending, 1, -1 do
        local entry = self.pending[index]
        if not entry.drop:call("get_Valid") then
            table.remove(self.pending, index)
        else
            local transform = entry.drop:call("get_Transform")
            if not entry.positioned then
                -- Allow the native prefab to initialize before moving it, then let
                -- its pickup child update before detaching from the dying enemy.
                transform:call("set_Position", entry.position)
                transform:call("set_Rotation", entry.rotation)
                entry.positioned = true
            else
                transform:call("setParent(via.Transform, System.Boolean)", nil, true)
                table.remove(self.pending, index)
            end
        end
    end
end

function EnemyDrops:begin(enemy_object)
    local address = self.context.game:address(enemy_object)
    if self.dropped[address] then
        return nil
    end
    self.dropped[address] = true
    return self.generations[address] or 0
end

function EnemyDrops:reset_enemy(enemy_object)
    local address = self.context.game:address(enemy_object)
    self.dropped[address] = nil
    self.generations[address] = (self.generations[address] or 0) + 1
end

function EnemyDrops:death(source, controller)
    local enemy_object = source:call("get_GameObject")
    if enemy_object == nil then return end
    local static_mia = self.context.features.static_mia
    if static_mia:suppress(controller, enemy_object) then
        return
    end

    if self.context.config:get("random-enemy-drops", true) then
        local generation = self:begin(enemy_object)
        if generation ~= nil then
            self:spawn(source, enemy_object, generation)
        end
    end
    static_mia:remember(controller, enemy_object)
end

function EnemyDrops:install()
    local game = self.context.game
    game:hook("app.EnemyActionController", "spawn(app.EnemySpawnInfo, app.EnemySpawnInfoOptionBase)", function(args)
        local controller = game:object(args[2])
        local enemy_object = controller:call("get_GameObject")
        if self.context.features.static_mia:suppress(controller, enemy_object) then
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
        self:reset_enemy(enemy_object)
    end)

    game:hook("app.EnemyActionController", "forgetDie()", function(args)
        local controller = game:object(args[2])
        local enemy_object = controller:call("get_GameObject")
        if self.context.features.static_mia:suppress(controller, enemy_object) then
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
        local enemy_type = self:enemy_type(controller, enemy_object)
        if not Data.single_drop_per_spawn[enemy_type] and not self.context.features.static_mia:is_static(enemy_object) then
            self:reset_enemy(enemy_object)
        end
    end)

    game:hook("app.EnemyActionController", "finishDead(System.Boolean, System.Boolean)", function(args)
        local controller = game:object(args[2])
        self:death(controller, controller)
    end)

    game:hook("app.EnemyDamageController", "doDie(app.DamageController.DamageRecord)", function(args)
        local damage_controller = game:object(args[2])
        self:death(damage_controller, damage_controller:call("get_enemyActionController"))
    end)
end

function EnemyDrops:reset()
    self.dropped = {}
    self.generations = {}
    self.pending = {}
end

return EnemyDrops
