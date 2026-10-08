return function()
    local Em3300Explosions = require("BioRand7/em3300_explosions")
    local now, player_present = 0, true
    local player_position = { x = 10, y = 0, z = 0 }
    local function object(id, position)
        return { id = id, position = position, active = true, call = function(self, method)
            if method == "get_Update" then return self.active end
            assert(method == "get_Transform", method)
            if self.position == nil then return nil end
            return { call = function(_, signature)
                assert(signature == "get_Position", signature)
                return self.position
            end }
        end }
    end
    local player = object(1, player_position)
    local enemy = object(2, { x = 0, y = 0, z = 0 })
    local second = object(3, { x = 0, y = 10, z = 0 })
    local feature = Em3300Explosions.new({ game = {
        address = function(_, value) return value.id end,
        player = function() if player_present then return player end end,
    } })
    local detonations, despawns = {}, {}
    feature.detonate = function(_, value) detonations[value.id] = (detonations[value.id] or 0) + 1 end
    feature.despawn = function(_, value) despawns[value.id] = (despawns[value.id] or 0) + 1 end
    feature.delay = function() return 5 end
    os.clock = function() return now end

    assert(not feature:update_object(enemy))
    assert(feature.states[2].idle_since == 0)
    now = 100
    feature:update_object(second)
    now = 179.999
    assert(not feature:update_object(enemy) and detonations[2] == nil)
    now = 180
    assert(feature:update_object(enemy) and detonations[2] == 1)
    assert(feature.states[2].started == nil, "The inactivity timeout must not require proximity")
    feature:update_object(enemy)
    assert(detonations[2] == 1, "Application and think hooks must not detonate twice")
    feature:update_object(second)
    assert(detonations[3] == nil, "Each Eveline must get her own three-minute timer")
    now = 180.249
    feature:update_object(enemy)
    assert(despawns[2] == nil)
    now = 180.25
    feature:update_object(enemy)
    feature:update_object(enemy)
    assert(despawns[2] == 1, "Idle explosions must use the existing one-time despawn path")
    now = 280
    feature:update_object(second)
    assert(detonations[3] == 1)

    feature:reset()
    detonations, despawns = {}, {}
    now = 300
    feature:update_object(enemy)
    now = 479.999
    player_position.x = 5
    feature:update_object(enemy)
    assert(feature.states[2].started == now and feature.states[2].idle_since == nil,
        "Coming within five metres must replace the idle timer with the normal short fuse")
    player_position.x = 100
    now = 480
    feature:update_object(enemy)
    assert(detonations[2] == nil, "The old inactivity deadline must not shorten the proximity fuse")
    now = 484.999
    feature:update_object(enemy)
    assert(detonations[2] == 1, "Leaving after approaching must preserve the existing armed behavior")

    feature:reset()
    detonations, despawns = {}, {}
    now = 500
    enemy.active = false
    feature:update_object(enemy)
    now = 1000
    feature:update_object(enemy)
    assert(feature.states[2].idle_since == nil and detonations[2] == nil,
        "Inactive pooled enemies must not time out before spawning")
    enemy.active = true
    feature:update_object(enemy)
    now = 1179
    feature:update_object(enemy)
    assert(detonations[2] == nil)
    player_present = false
    feature:update_object(enemy)
    now = 2000
    feature:update_object(enemy)
    assert(feature.states[2].idle_since == nil and detonations[2] == nil,
        "A missing player must not be mistaken for a distant player")
    player_present = true
    feature:update_object(enemy)
    now = 2100
    enemy.position = nil
    feature:update_object(enemy)
    assert(feature.states[2].idle_since == nil, "A missing transform must cancel the idle timer")
    enemy.position = { x = 0, y = 0, z = 0 }
    feature:update_object(enemy)
    now = 2279.999
    feature:update_object(enemy)
    assert(detonations[2] == nil)
    now = 2280
    feature:update_object(enemy)
    assert(detonations[2] == 1)

    feature:reset()
    assert(next(feature.states) == nil, "Save reloads and script resets must clear idle timers")

    detonations, despawns = {}, {}
    player_position.x = 1
    enemy.active = false
    now = 3000
    feature:update_object(enemy)
    now = 3010
    feature:update_object(enemy)
    assert(feature.states[2].started == nil and detonations[2] == nil,
        "A nearby player must not arm an inactive pooled or SpawnGroup enemy")

    enemy.active = true
    feature:update_object(enemy)
    assert(feature.states[2].started == now, "Activation must start a fresh proximity fuse")
    now = 3012
    enemy.active = false
    feature:update_object(enemy)
    now = 3020
    feature:update_object(enemy)
    assert(feature.states[2].started == nil and detonations[2] == nil,
        "Suspending an armed enemy must cancel its fuse without exploding or destroying it")
    enemy.active = true
    feature:update_object(enemy)
    now = 3024.999
    feature:update_object(enemy)
    assert(detonations[2] == nil)
    now = 3025
    feature:update_object(enemy)
    assert(detonations[2] == 1, "Resuming must give the player the full fuse duration")
end
