return function()
    local RandomEvents = require("BioRand7/random_events")
    local UI = require("BioRand7/ui")
    local list = dofile("tests/lua/helpers.lua").list
    local saved_imgui, saved_sdk, saved_thread = imgui, sdk, thread
    local saved_vector2, saved_vector3, saved_vector4 = Vector2f, Vector3f, Vector4f
    local saved_quaternion, saved_clock = Quaternion, os.clock
    local function vector(x, y, z, w) return { x = x, y = y, z = z, w = w } end
    Vector2f, Vector3f, Vector4f = { new = vector }, { new = vector }, { new = vector }
    local next_address = 0

    local function object(fields, methods)
        next_address = next_address + 1
        local result = { fields = fields or {}, methods = methods or {}, address = next_address, valid = true }
        function result:call(name, ...)
            if name == "get_Valid" then return self.valid end
            local method = assert(self.methods[name], "Unexpected method: " .. name)
            return method(self, ...)
        end
        function result:get_field(name)
            assert(self.fields[name] ~= nil, "Unexpected field: " .. name)
            return self.fields[name]
        end
        function result:set_field(name, value)
            assert(self.fields[name] ~= nil, "Unexpected field: " .. name)
            self.fields[name] = value
        end
        return result
    end

    local warnings = {}
    local game = { components = {}, hooks = {} }
    function game:singleton() return self.manager end
    function game:player() return self.manager and self.manager:get_field("PlayerObj") end
    function game:component(value, name)
        return value and value.components and value.components[name] or self.components[name]
    end
    function game:address(value) return value.address end
    function game:valid(value) return value ~= nil and value:call("get_Valid") end
    function game:list_storage(value)
        if value == nil then return nil, 0 end
        return value.mItems, value.mSize
    end
    function game:object(value) return value end
    function game:hook(_, signature, before, after)
        self.hooks[signature] = { before = before, after = after }
    end
    local context = {
        game = game,
        config = { get = function(_, _, default) return default end },
        log = { warn = function(_, message) warnings[#warnings + 1] = message end },
        features = {},
    }
    local events = RandomEvents.new(context)
    context.features.random_events = events

    assert(events:player() == nil)
    assert(events:passive_manager() == nil)
    events:apply_scale({ scale = 2 })

    local player = object()
    game.manager = object({ PlayerObj = player })
    assert(events:player() == player)

    local passive = object({
        AttackChangeRate = 0.1, DamageChangeRate = 0, WalkSpeedChangeRate = 0,
        MoveSpeedChangeRate = 0, DyingMoveSpeedChangeRate = 0,
        ReloadSpeedChangeRate = 0, BulletStackNumInfinityCount = 0,
    })
    game.components["app.PlayerStatus"] = object({ PlayerPassiveSkillManager = passive })
    assert(events:passive_manager() == passive)
    events:apply_passive({ attack = 0.35, infinity = 1 })
    events:apply_passive({ attack = 0.35, infinity = 1 })
    assert(math.abs(passive.fields.AttackChangeRate - 0.45) < 0.00001)
    assert(passive.fields.BulletStackNumInfinityCount == 1)
    events:restore()
    assert(math.abs(passive.fields.AttackChangeRate - 0.1) < 0.00001)
    assert(passive.fields.BulletStackNumInfinityCount == 0)

    local movement = object({
        ExternalWalkSpeedRate = 1, ExternalJogSpeedRate = 2,
        ExternalDyingWalkSpeedRate = 3, ExternalDyingJogSpeedRate = 4, ActionSpeedRate = 5,
    }, {
        get_IsForbidTerrainMove = function(self) return self.forbid == true end,
        set_IsForbidTerrainMove = function(self, value) self.forbid = value end,
    })
    game.components["app.PlayerMovement"] = movement
    events:apply_freeze()
    events:apply_freeze()
    assert(movement.fields.ExternalWalkSpeedRate == 0 and movement.forbid)
    events:restore()
    assert(movement.fields.ExternalWalkSpeedRate == 1)
    assert(movement.fields.ExternalJogSpeedRate == 2)
    assert(movement.fields.ExternalDyingWalkSpeedRate == 3)
    assert(movement.fields.ExternalDyingJogSpeedRate == 4)
    assert(movement.fields.ActionSpeedRate == 5 and not movement.forbid)

    local transform = object({}, {
        get_Position = function() return vector(0, 0, 0) end,
        get_LocalScale = function(self) return self.scale end,
        set_LocalScale = function(self, value) self.scale = value end,
    })
    transform.scale = vector(1, 2, 3)
    player.methods.get_Transform = function() return transform end
    events:apply_scale({ scale = 2 })
    events:apply_scale({ scale = 2 })
    assert(transform.scale.x == 2 and transform.scale.y == 4 and transform.scale.z == 6)
    events:restore()
    assert(transform.scale.x == 1 and transform.scale.y == 2 and transform.scale.z == 3)

    local damage = object({}, {
        get_defaultMaxHealth = function(self) return self.health end,
        set_defaultMaxHealth = function(self, value) self.health = value end,
    })
    damage.health = 100
    local enemy = object({}, {
        get_Transform = function() return transform end,
        get_TimeScale = function(self) return self.time_scale end,
        set_TimeScale = function(self, value) self.time_scale = value end,
        get_DrawSelf = function(self) return self.draw end,
        set_DrawSelf = function(self, value) self.draw = value end,
    })
    enemy.time_scale, enemy.draw = 0.5, false
    enemy.components = {
        ["app.EnemyActionController"] = object({}, {
            get_enemyDamageController = function() return damage end,
        }),
    }
    game.manager.fields.ManagedObjects = list({ [0] = list({ [0] = enemy }, 1) }, 1)
    events:apply_enemies({ kind = "enemy_strong", enemy_health = 2.25 })
    events:apply_enemies({ kind = "enemy_strong", enemy_health = 2.25 })
    assert(enemy.time_scale == 0.6 and damage.health == 225)
    events:restore()
    assert(enemy.time_scale == 0.5 and damage.health == 100)
    events:apply_enemies({ kind = "enemy_invisible" })
    events:restore()
    assert(enemy.draw == false)

    events:apply_passive({ attack = 0.35 })
    local stale = object()
    stale.methods.get_Transform = function() error("Scene object was destroyed") end
    events.scale_states[stale.address] = { player = stale }
    events:restore()
    events:restore()
    assert(#warnings == 1)
    assert(next(events.scale_states) == nil and next(events.passive_states) == nil)
    assert(math.abs(passive.fields.AttackChangeRate - 0.1) < 0.00001)

    local faded_in = 0
    local blackout = object({}, {
        ["setupFadeTime(System.Single)"] = function() end,
        ["requestFadeOut_forEvent(app.BlackOutManager.FadeColorEnum, System.Boolean)"] = function() end,
        requestFadeIn_forEvent = function() faded_in = faded_in + 1 end,
    })
    sdk = { get_managed_singleton = function() return blackout end }
    events:apply_blindness()
    sdk.get_managed_singleton = function() error("Must restore the manager that was faded out") end
    events:restore()
    assert(faded_in == 1 and events.blindness == nil)

    local hook_storage = {}
    sdk.to_int64 = function(value) return value end
    sdk.to_ptr = function(value) return value end
    sdk.PreHookResult = { SKIP_ORIGINAL = "skip" }
    thread = { get_hook_storage = function() return hook_storage end }
    events:install_weapon_hooks()
    local gun = object({}, {
        get_loadNum = function(self) return self.load end,
        set_loadNum = function(self, value) self.load = value end,
    })
    gun.load = 7
    events.active = { kind = "weapon_infinite_ammo", ends_at = math.huge }
    local expend = game.hooks["expendBullet()"]
    expend.before({ [2] = gun })
    gun.load = 6
    assert(expend.after(0) == 1 and gun.load == 7)
    local setter = game.hooks["set_loadNum(System.Int32)"]
    assert(setter.before({ [2] = gun, [3] = 0xFFFFFFFF }) == "skip")
    assert(setter.before({ [2] = gun, [3] = 8 }) == nil)

    events.active = { kind = "weapon_neuro_ammo", ends_at = math.huge }
    local args = { [3] = 0 }
    game.hooks["setupBullet(app.ShellManager.BulletType, System.Int32)"].before(args)
    assert(args[3] == 25)

    local identity = { w = 1, x = 0, y = 0, z = 0 }
    Quaternion = { identity = function() return identity end }
    local now, bombs_created, bombs_exploded = 100, 0, 0
    os.clock = function() return now end
    local gun_transform = object()
    local gun_object = object({}, { get_Transform = function() return gun_transform end })
    gun.methods.get_GameObject = function() return gun_object end
    local expected_owner, expected_transform = player, gun_transform
    local bomb = object({}, {
        requestExplosion = function() bombs_exploded = bombs_exploded + 1 end,
    })
    local shell_manager = object({}, {
        ["createBomb(via.GameObject, via.Transform, via.vec3, via.Quaternion)"] =
            function(_, owner, target, position, rotation)
                assert(owner == expected_owner and target == expected_transform)
                assert(position.x == 0 and position.y == 0 and position.z == 1.25)
                assert(rotation == identity)
                bombs_created = bombs_created + 1
                return bomb
            end,
    })
    context.features.em3300_explosions = {
        shell_manager = function(_, owner)
            assert(owner == expected_owner)
            return shell_manager
        end,
    }
    events:request_explosive_bomb(gun)
    assert(bombs_created == 1 and bombs_exploded == 1)
    now = 100.1
    events:request_explosive_bomb(gun)
    assert(bombs_created == 1)
    now = 100.25
    events:request_explosive_bomb(gun)
    assert(bombs_created == 2 and bombs_exploded == 2)

    local saved_manager = game.manager
    game.manager = nil
    expected_owner = gun_object
    now = 101
    events:request_explosive_bomb(gun)
    assert(bombs_created == 3 and bombs_exploded == 3)

    gun.methods.get_GameObject = function() return nil end
    now = 102
    events:request_explosive_bomb(gun)
    assert(bombs_created == 3)

    game.manager = saved_manager
    expected_owner, expected_transform = player, transform
    now = 103
    events:request_explosive_bomb(gun)
    assert(bombs_created == 4 and bombs_exploded == 4)
    os.clock = saved_clock

    events.active = { kind = "weapon_explosive_ammo", ends_at = math.huge }
    local explosions = 0
    events.request_explosive_bomb = function() explosions = explosions + 1 end
    local shoot = game.hooks["shoot(via.Ray, System.Boolean, System.Boolean)"]
    shoot.before({ [2] = gun, [5] = 0x100 })
    shoot.before({ [2] = gun, [5] = 0x101 })
    assert(explosions == 1)

    local windows, colors, labels = 0, 0, 0
    imgui = {
        set_next_window_pos = function() end,
        push_style_color = function(index, color)
            assert(index == 2 and color.w == 0.45)
            colors = colors + 1
        end,
        begin_window = function() windows = windows + 1; return true end,
        text = function() labels = labels + 1 end,
        end_window = function() windows = windows - 1 end,
        pop_style_color = function(count) colors = colors - count end,
    }
    UI.new(context):draw_overlay()
    assert(windows == 0 and colors == 0 and labels == 1)

    imgui, sdk, thread = saved_imgui, saved_sdk, saved_thread
    Vector2f, Vector3f, Vector4f = saved_vector2, saved_vector3, saved_vector4
    Quaternion = saved_quaternion
end
