return function()
    local RandomEvents = require("BioRand7/random_events")
    local list = dofile("tests/lua/helpers.lua").list
    local now, lookups, positions, writes, damage_lookups = 0, 0, 0, 0, 0
    os.clock = function() return now end
    local function object(address, x)
        local value = { address = address, valid = true, x = x, scale = 1 }
        function value:call(method, argument)
            if method == "get_Valid" then return self.valid end
            assert(self.valid, "A destroyed target must never be used")
            if method == "get_Transform" then return self end
            if method == "get_Position" then
                positions = positions + 1
                return { x = self.x, y = 0, z = 0 }
            end
            if method == "get_TimeScale" then return self.scale end
            if method == "set_TimeScale" then
                writes = writes + 1
                self.scale = argument
                return
            end
            if method == "get_enemyDamageController" then
                damage_lookups = damage_lookups + 1
                return nil
            end
            error(method)
        end
        return value
    end
    local player = object(1, 0)
    local first, second = object(1001, 2), object(1002, -2)
    first.controller, second.controller = object(2001), object(2002)
    local objects = {}
    for index = 0, 933 do objects[index] = object(index + 10) end
    objects[934], objects[935] = second, first
    local groups = list({ [0] = list(objects, 936) }, 1)
    local manager = { get_field = function(_, name)
        if name == "PlayerObj" then return player end
        assert(name == "ManagedObjects")
        return groups
    end }
    local game = {}
    function game:singleton() return manager end
    function game:player() return manager and player end
    function game:address(value) return value.address end
    function game:valid(value) return value ~= nil and value.valid end
    function game:list_storage(value) return value.mItems, value.mSize end
    function game:component(value, name)
        if name == "app.EnemyActionController" then
            lookups = lookups + 1
            return value.controller
        end
        assert(name == "app.EnemyDamageController")
    end
    local events = RandomEvents.new({ game = game, config = { get = function(_, key, default)
        if key == "event-enemy-max-targets" then return 1 end
        if key == "event-enemy-radius" then return 10 end
        return default
    end } })
    local event = { kind = "enemy_speed", enemy_speed = 2 }
    for frame = 0, 59 do
        now = frame / 60
        local before = lookups
        events:apply_enemies(event)
        assert(lookups - before <= 64, "Component discovery must stay within its per-frame budget")
    end
    assert(lookups < 2000 and positions < 20, "Target discovery and sorting must not run every frame")
    assert(first.scale == 2 and second.scale == 1, "Equal distances must use the address tie-breaker")
    assert(#events.targets == 1 and events.targets[1].game_object == first)
    assert(damage_lookups == 0, "Speed events do not need damage controllers")

    now = 1.05
    events:apply_enemies({ kind = "enemy_strong", enemy_health = 2.25 })
    assert(damage_lookups == 1, "Only resolve damage for selected health-event targets")

    first.x = 100
    now = 1.31
    events:apply_enemies(event)
    assert(second.scale == 2 and events.targets[1].game_object == second,
        "Moving targets must be ranked again using current positions and radius")
    second.valid = false
    local before = writes
    now = 1.32
    events:apply_enemies(event)
    assert(writes == before, "Discard destroyed cached targets between ranking refreshes")
    second.valid = true
    manager = nil
    now = 1.33
    events:apply_enemies(event)
    assert(writes == before and #events.targets == 0, "Scene unload must invalidate cached target selection")
    events:restore()
    assert(first.scale == 1 and second.scale == 1, "Restore every touched target, not only the current selection")
    assert(next(events.enemy_cache.values) == nil and #events.targets == 0)
end
