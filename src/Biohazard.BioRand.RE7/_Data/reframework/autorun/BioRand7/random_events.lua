local Rng = require("BioRand7/rng")
local ObjectCache = require("BioRand7/object_cache")

local RandomEvents = {}
RandomEvents.__index = RandomEvents

local KINDS = {
    "player_status", "player_blindness", "player_freeze", "player_scale",
    "weapon_infinite_ammo", "weapon_neuro_ammo", "weapon_explosive_ammo",
    "enemy_speed", "enemy_invisible", "enemy_weak", "enemy_strong", "enemy_paused",
}

local EVENT_CONFIG = {
    player_status = { enabled = "event-player-status-effects", duration = "event-player-status-duration", default = 30 },
    player_blindness = { enabled = "event-player-blindness", duration = "event-player-blindness-duration", default = 4 },
    player_freeze = { enabled = "event-player-freeze", duration = "event-player-freeze-duration", default = 5 },
    player_scale = { enabled = "event-player-scale", duration = "event-player-scale-duration", default = 25 },
    weapon_infinite_ammo = { enabled = "event-weapon-infinite-ammo", duration = "event-weapon-infinite-ammo-duration", default = 25 },
    weapon_neuro_ammo = { enabled = "event-weapon-neuro-ammo", duration = "event-weapon-neuro-ammo-duration", default = 20 },
    weapon_explosive_ammo = { enabled = "event-weapon-explosive-ammo", duration = "event-weapon-explosive-ammo-duration", default = 20 },
    enemy_speed = { enabled = "event-enemy-speed", duration = "event-enemy-speed-duration", default = 25 },
    enemy_invisible = { enabled = "event-enemy-invisible", duration = "event-enemy-invisible-duration", default = 15 },
    enemy_weak = { enabled = "event-enemy-weak", duration = "event-enemy-weak-duration", default = 25 },
    enemy_strong = { enabled = "event-enemy-strong", duration = "event-enemy-strong-duration", default = 25 },
    enemy_paused = { enabled = "event-enemy-paused", duration = "event-enemy-paused-duration", default = 8 },
}

local DISPLAY_NAMES = {
    player_status = "player status effect", player_blindness = "brief blindness",
    player_freeze = "movement lock", player_scale = "player scale",
    weapon_infinite_ammo = "infinite ammo", weapon_neuro_ammo = "neuro ammo",
    weapon_explosive_ammo = "explosive ammo", enemy_speed = "enemy speed shuffle",
    enemy_invisible = "invisible enemies", enemy_weak = "weak enemies",
    enemy_strong = "strong enemies", enemy_paused = "paused enemies",
}

local STATUS_DELTAS = {
    { label = "firepower up", attack = 0.35 },
    { label = "firepower down", attack = -0.30 },
    { label = "toughness up", damage = -0.25 },
    { label = "vulnerable", damage = 0.35 },
    { label = "speed up", walk = 0.35, move = 0.35, dying_move = 0.35 },
    { label = "heavy legs", walk = -0.35, move = -0.35, dying_move = -0.35 },
    { label = "quick reload", reload = 0.45 },
    { label = "bottomless pockets", infinity = 1 },
}

local INFINITE_AMMO_DELTA = { label = "infinite ammo", infinity = 1 }
local TARGET_REFRESH_INTERVAL = 0.25

local MOVEMENT_FIELDS = {
    "ExternalWalkSpeedRate", "ExternalJogSpeedRate", "ExternalDyingWalkSpeedRate",
    "ExternalDyingJogSpeedRate", "ActionSpeedRate",
}

local function random_between(rng, minimum, maximum)
    if maximum < minimum then minimum, maximum = maximum, minimum end
    return minimum + rng:float() * (maximum - minimum)
end

function RandomEvents.new(context)
    local self = setmetatable({
        context = context,
        rng = nil,
        seed = nil,
        next_event_at = nil,
        active = nil,
        started_from_ui = false,
        movement_states = {},
        scale_states = {},
        passive_states = {},
        enemy_states = {},
        explosive_shots = {},
        blindness = nil,
        targets = {},
        next_targets_at = 0,
    }, RandomEvents)
    self.enemy_cache = ObjectCache.new(context.game, function(object, existing)
        if existing ~= nil and existing.controller:call("get_Valid") then return existing end
        local controller = context.game:component(object, "app.EnemyActionController")
        if controller ~= nil then return { game_object = object, controller = controller } end
    end)
    return self
end

function RandomEvents:random()
    local seed = tonumber(self.context.config:get("biorand-seed", 0)) or 0
    if self.rng == nil or self.seed ~= seed then
        self.seed = seed
        self.rng = Rng.for_events(seed)
        self.next_event_at = nil
    end
    return self.rng
end

function RandomEvents:duration(kind)
    local definition = EVENT_CONFIG[kind]
    return math.max(1, math.min(600, self.context.config:get(definition.duration, definition.default)))
end

function RandomEvents:candidates()
    local candidates = {}
    for _, kind in ipairs(KINDS) do
        if self.context.config:get(EVENT_CONFIG[kind].enabled, true) then
            candidates[#candidates + 1] = kind
        end
    end
    return candidates
end

function RandomEvents:schedule(now)
    local minimum = self.context.config:get("random-events-interval-min", 90)
    local maximum = self.context.config:get("random-events-interval-max", 210)
    if maximum < minimum then
        minimum, maximum = maximum, minimum
    end
    minimum = math.max(1, math.min(3600, minimum))
    maximum = math.max(minimum, math.min(3600, maximum))
    self.next_event_at = now + minimum + self:random():float() * (maximum - minimum)
end

function RandomEvents:create(kind, now, status)
    local event = { kind = kind, started_at = now, duration = self:duration(kind) }
    event.ends_at = now + event.duration
    if kind == "player_status" then
        event.status = status or STATUS_DELTAS[self:random():int(1, #STATUS_DELTAS)]
    elseif kind == "player_scale" then
        local minimum = self.context.config:get("event-player-scale-min", 0.65)
        local maximum = self.context.config:get("event-player-scale-max", 1.55)
        event.scale = random_between(self:random(), minimum, maximum)
    elseif kind == "enemy_speed" then
        local minimum = self.context.config:get("event-enemy-speed-min", 0.4)
        local maximum = self.context.config:get("event-enemy-speed-max", 2.5)
        event.enemy_speed = random_between(self:random(), minimum, maximum)
    elseif kind == "enemy_weak" then
        event.enemy_health = 0.35
    elseif kind == "enemy_strong" then
        event.enemy_health = 2.25
    end
    return event
end

function RandomEvents:start(kind, from_ui, status)
    self:restore()
    local event = self:create(kind, os.clock(), status)
    self.active = event
    self.started_from_ui = from_ui == true
    self.next_event_at = nil
    self.explosive_shots = {}
    return event
end

function RandomEvents:is_active(kind)
    return self.active ~= nil and self.active.kind == kind and os.clock() < self.active.ends_at
end

function RandomEvents:suffix(event)
    if event.kind == "player_status" then
        return " (" .. event.status.label .. ")"
    elseif event.kind == "player_scale" then
        return (" (x%.2g)"):format(event.scale)
    elseif event.kind == "enemy_speed" then
        return (" (x%.2g)"):format(event.enemy_speed)
    end
    return ""
end

function RandomEvents:state_label()
    if self.active ~= nil then
        return ("%s%s active, %.1fs left"):format(
            DISPLAY_NAMES[self.active.kind], self:suffix(self.active), math.max(0, self.active.ends_at - os.clock()))
    end
    if self.next_event_at ~= nil then
        return ("next in %.1fs"):format(math.max(0, self.next_event_at - os.clock()))
    end
    return "idle"
end

function RandomEvents:overlay_label()
    if self.active == nil or os.clock() >= self.active.ends_at then
        return nil
    end
    return ("BioRand event: %s%s | %.1fs"):format(
        DISPLAY_NAMES[self.active.kind], self:suffix(self.active), self.active.ends_at - os.clock())
end

function RandomEvents:player()
    return self.context.game:player()
end

function RandomEvents:passive_manager()
    local game = self.context.game
    local player = self:player()
    local manager = game:component(player, "app.PlayerPassiveSkillManager")
    if manager ~= nil then
        return manager
    end
    local order = game:component(player, "app.PlayerOrder")
    if order ~= nil then
        manager = order:get_field("PlayerPassiveSkillManager")
        if manager ~= nil then
            return manager
        end
    end
    local status = game:component(player, "app.PlayerStatus")
    if status ~= nil then
        return status:get_field("PlayerPassiveSkillManager")
    end
end

function RandomEvents:apply_passive_delta(manager, delta, direction)
    direction = direction or 1
    local changes = {
        AttackChangeRate = delta.attack or 0,
        DamageChangeRate = delta.damage or 0,
        WalkSpeedChangeRate = delta.walk or 0,
        MoveSpeedChangeRate = delta.move or 0,
        DyingMoveSpeedChangeRate = delta.dying_move or 0,
        ReloadSpeedChangeRate = delta.reload or 0,
    }
    for field, change in pairs(changes) do
        manager:set_field(field, manager:get_field(field) + change * direction)
    end
    local infinity = manager:get_field("BulletStackNumInfinityCount") + (delta.infinity or 0) * direction
    manager:set_field("BulletStackNumInfinityCount", math.max(0, infinity))
end

function RandomEvents:apply_passive(delta)
    local manager = self:passive_manager()
    if manager == nil then
        return
    end
    local address = self.context.game:address(manager)
    if self.passive_states[address] == nil then
        self:apply_passive_delta(manager, delta)
        self.passive_states[address] = { manager = manager, delta = delta }
    end
end

function RandomEvents:blackout_manager()
    local game = self.context.game
    local manager = sdk.get_managed_singleton("app.BlackOutManager")
    if manager ~= nil then
        return manager
    end
    local object_manager = game:singleton("app.ObjectManager")
    local object = object_manager and object_manager:call("findObject(System.String)", "BlackOutManager")
    if object == nil then
        object = game:method("app.ObjectManager", "findObjectInCurrentScene(System.String)")
            :call(nil, "BlackOutManager")
    end
    return game:component(object, "app.BlackOutManager")
        or game:component(self:player(), "app.BlackOutManager")
end

function RandomEvents:apply_blindness()
    if self.blindness then
        return
    end
    local manager = self:blackout_manager()
    if manager ~= nil then
        manager:call("setupFadeTime(System.Single)", 0.1)
        manager:call("requestFadeOut_forEvent(app.BlackOutManager.FadeColorEnum, System.Boolean)", 0, true)
        self.blindness = manager
    end
end

function RandomEvents:apply_freeze()
    local movement = self.context.game:component(self:player(), "app.PlayerMovement")
    if movement == nil then
        return
    end
    local address = self.context.game:address(movement)
    if self.movement_states[address] == nil then
        local state = { movement = movement, forbid_terrain_move = movement:call("get_IsForbidTerrainMove") }
        for _, field in ipairs(MOVEMENT_FIELDS) do
            state[field] = movement:get_field(field)
        end
        self.movement_states[address] = state
    end
    for _, field in ipairs(MOVEMENT_FIELDS) do
        movement:set_field(field, 0)
    end
    movement:call("set_IsForbidTerrainMove", true)
end

function RandomEvents:apply_scale(event)
    local player = self:player()
    if player == nil or not player:call("get_Valid") then return end
    local transform = player:call("get_Transform")
    if transform == nil then return end
    local address = self.context.game:address(player)
    local state = self.scale_states[address]
    if state == nil then
        state = { player = player, scale = transform:call("get_LocalScale") }
        self.scale_states[address] = state
    end
    transform:call("set_LocalScale", Vector3f.new(
        state.scale.x * event.scale, state.scale.y * event.scale, state.scale.z * event.scale))
end

function RandomEvents:enemy_targets()
    local game = self.context.game
    local now = os.clock()
    local enemies = self.enemy_cache:update(now)
    if self.enemies ~= enemies then
        self.enemies = enemies
        self.next_targets_at = 0
    end
    if now < self.next_targets_at then return self.targets end

    self.targets = {}
    self.next_targets_at = now + TARGET_REFRESH_INTERVAL
    local player = self:player()
    if player == nil then return self.targets end
    local player_transform = player:call("get_Transform")
    if player_transform == nil then return self.targets end
    local player_position = player_transform:call("get_Position")
    local radius = self.context.config:get("event-enemy-radius", 25)
    local maximum = math.max(1, math.floor(self.context.config:get("event-enemy-max-targets", 8) + 0.5))
    local targets = self.targets
    for address, enemy in pairs(enemies) do
        local game_object, controller = enemy.game_object, enemy.controller
        if game:valid(game_object) and controller:call("get_Valid") then
            local transform = game_object:call("get_Transform")
            if transform ~= nil then
                local position = transform:call("get_Position")
                local x, y, z = position.x - player_position.x, position.y - player_position.y,
                    position.z - player_position.z
                local distance = x * x + y * y + z * z
                if distance <= radius * radius then
                    targets[#targets + 1] = {
                        game_object = game_object,
                        address = address,
                        damage = controller:call("get_enemyDamageController")
                            or game:component(game_object, "app.EnemyDamageController"),
                        distance = distance,
                    }
                end
            end
        else
            enemies[address] = nil
        end
    end
    table.sort(targets, function(left, right)
        if left.distance == right.distance then return left.address < right.address end
        return left.distance < right.distance
    end)
    while #targets > maximum do
        table.remove(targets)
    end
    return targets
end

function RandomEvents:apply_enemies(event)
    for _, target in ipairs(self:enemy_targets()) do
        local object = target.game_object
        if self.context.game:valid(object) then
            self:apply_enemy(event, target)
        end
    end
end

function RandomEvents:apply_enemy(event, target)
    local object = target.game_object
    local address = self.context.game:address(object)
    local state = self.enemy_states[address]
    if state == nil then
        state = { game_object = object, damage = target.damage }
        self.enemy_states[address] = state
    end
    state.damage = state.damage or target.damage

    if event.kind == "enemy_speed" or event.kind == "enemy_paused"
        or event.kind == "enemy_weak" or event.kind == "enemy_strong" then
        state.time_scale = state.time_scale or object:call("get_TimeScale")
        local multiplier = event.enemy_speed or 1
        if event.kind == "enemy_paused" then multiplier = 0 end
        if event.kind == "enemy_weak" then multiplier = 0.85 end
        if event.kind == "enemy_strong" then multiplier = 1.2 end
        object:call("set_TimeScale", state.time_scale * multiplier)
    end
    if event.kind == "enemy_invisible" then
        if state.draw_self == nil then state.draw_self = object:call("get_DrawSelf") end
        object:call("set_DrawSelf", false)
    end
    if (event.kind == "enemy_weak" or event.kind == "enemy_strong") and state.damage ~= nil
        and state.damage:call("get_Valid") then
        state.health = state.health or state.damage:call("get_defaultMaxHealth")
        state.damage:call("set_defaultMaxHealth", math.max(1, state.health * event.enemy_health))
    end
end

function RandomEvents:apply(event)
    if event.kind == "player_status" then
        self:apply_passive(event.status)
    elseif event.kind == "player_blindness" then
        self:apply_blindness()
    elseif event.kind == "player_freeze" then
        self:apply_freeze()
    elseif event.kind == "player_scale" then
        self:apply_scale(event)
    elseif event.kind == "weapon_infinite_ammo" then
        self:apply_passive(INFINITE_AMMO_DELTA)
    elseif event.kind:sub(1, 6) == "enemy_" then
        self:apply_enemies(event)
    end
end

function RandomEvents:restore_states(name, restore)
    local states = self[name]
    self[name] = {}
    for _, state in pairs(states) do
        local ok, message = pcall(restore, state)
        if not ok then
            self.context.log:warn("Unable to restore random event " .. name .. ": " .. tostring(message))
        end
    end
end

function RandomEvents:restore()
    self.enemy_cache:reset()
    self.enemies = nil
    self.targets = {}
    self.next_targets_at = 0
    self:restore_states("movement_states", function(state)
        if not state.movement:call("get_Valid") then return end
        for _, field in ipairs(MOVEMENT_FIELDS) do
            state.movement:set_field(field, state[field])
        end
        state.movement:call("set_IsForbidTerrainMove", state.forbid_terrain_move)
    end)

    self:restore_states("scale_states", function(state)
        if state.player:call("get_Valid") then
            local transform = state.player:call("get_Transform")
            if transform ~= nil then transform:call("set_LocalScale", state.scale) end
        end
    end)

    self:restore_states("passive_states", function(state)
        if state.manager:call("get_Valid") then
            self:apply_passive_delta(state.manager, state.delta, -1)
        end
    end)

    self:restore_states("enemy_states", function(state)
        if state.game_object:call("get_Valid") then
            if state.time_scale ~= nil then state.game_object:call("set_TimeScale", state.time_scale) end
            if state.draw_self ~= nil then state.game_object:call("set_DrawSelf", state.draw_self) end
        end
        if state.damage ~= nil and state.health ~= nil and state.damage:call("get_Valid") then
            state.damage:call("set_defaultMaxHealth", state.health)
        end
    end)

    if self.blindness then
        local manager = self.blindness
        self.blindness = nil
        local ok, message = pcall(function()
            if manager:call("get_Valid") then
                manager:call("setupFadeTime(System.Single)", 0.25)
                manager:call("requestFadeIn_forEvent")
            end
        end)
        if not ok then
            self.context.log:warn("Unable to clear random event blindness: " .. tostring(message))
        end
    end
end

function RandomEvents:clear()
    self:restore()
    self.rng = nil
    self.seed = nil
    self.next_event_at = nil
    self.active = nil
    self.started_from_ui = false
    self.explosive_shots = {}
end

function RandomEvents:update()
    local now = os.clock()
    if not self.context.config:get("random-events", false) then
        if self.started_from_ui and self.active ~= nil and now < self.active.ends_at then
            self:apply(self.active)
        elseif self.active ~= nil or self.next_event_at ~= nil then
            self:clear()
        end
        return
    end

    self:random()
    if self.active ~= nil and now >= self.active.ends_at then
        self:restore()
        self.active = nil
        self.started_from_ui = false
        self:schedule(now)
    end
    if self.active == nil then
        if self.next_event_at == nil then self:schedule(now) end
        if now >= self.next_event_at then
            local candidates = self:candidates()
            if #candidates == 0 then
                self:schedule(now)
            else
                self:start(candidates[self:random():int(1, #candidates)], false)
            end
        end
    end
    if self.active ~= nil then
        self:apply(self.active)
    end
end

function RandomEvents:request_explosive_bomb(gun)
    local gun_object = gun:call("get_GameObject")
    local address = self.context.game:address(gun_object or gun)
    local now = os.clock()
    if self.explosive_shots[address] ~= nil and now - self.explosive_shots[address] < 0.25 then
        return
    end
    self.explosive_shots[address] = now

    local owner = self:player() or gun_object
    if owner == nil then return end
    local transform = gun_object and gun_object:call("get_Transform") or owner:call("get_Transform")
    if transform == nil then return end
    local shell_manager = self.context.features.em3300_explosions:shell_manager(owner)
    if shell_manager ~= nil then
        local bomb = shell_manager:call(
            "createBomb(via.GameObject, via.Transform, via.vec3, via.Quaternion)",
            owner, transform, Vector3f.new(0, 0, 1.25), Quaternion.identity())
        if bomb ~= nil then bomb:call("requestExplosion") end
    end
end

function RandomEvents:install_weapon_hooks()
    local game = self.context.game
    game:hook("app.WeaponGun", "expendBullet()", function(args)
        local storage = thread.get_hook_storage()
        storage.biorand_infinite_gun = nil
        if self:is_active("weapon_infinite_ammo") then
            local gun = game:object(args[2])
            storage.biorand_infinite_gun = gun
            storage.biorand_infinite_load = gun:call("get_loadNum")
            if storage.biorand_infinite_load <= 0 then gun:call("set_loadNum", 1) end
        end
    end, function(retval)
        local storage = thread.get_hook_storage()
        if storage.biorand_infinite_gun ~= nil then
            storage.biorand_infinite_gun:call("set_loadNum", math.max(1, storage.biorand_infinite_load))
            storage.biorand_infinite_gun = nil
            return sdk.to_ptr(1)
        end
        return retval
    end)

    game:hook("app.WeaponGun", "set_loadNum(System.Int32)", function(args)
        if self:is_active("weapon_infinite_ammo") then
            local gun = game:object(args[2])
            local requested = sdk.to_int64(args[3]) % 0x100000000
            if requested >= 0x80000000 then requested = requested - 0x100000000 end
            if requested < gun:call("get_loadNum") then
                return sdk.PreHookResult.SKIP_ORIGINAL
            end
        end
    end)

    game:hook("app.WeaponGun", "setupBullet(app.ShellManager.BulletType, System.Int32)", function(args)
        if self:is_active("weapon_neuro_ammo") then
            args[3] = sdk.to_ptr(25)
        end
    end)

    game:hook("app.WeaponGun", "shoot(via.Ray, System.Boolean, System.Boolean)", function(args)
        if not self:is_active("weapon_explosive_ammo") then
            return
        end
        local no_bullet = sdk.to_int64(args[5]) % 2 ~= 0
        if not no_bullet or self:is_active("weapon_infinite_ammo") then
            self:request_explosive_bomb(game:object(args[2]))
        end
    end)
end

function RandomEvents:install()
    self:install_weapon_hooks()
end

function RandomEvents:reset()
    self:clear()
end

RandomEvents.kinds = KINDS
RandomEvents.display_names = DISPLAY_NAMES
RandomEvents.status_deltas = STATUS_DELTAS

return RandomEvents
