return function()
    local SpawnGroups = require("BioRand7/spawn_groups")
    local Engine = require("BioRand7/spawn_group_engine")
    local messages, requests = {}, {}
    local context = {
        config = { get = function() return 123 end },
        log = { error = function(_, s) messages[#messages + 1] = s end, info = function() end, warn = function() end },
    }
    local member = { runtimeGuid = "slot", aggro = true }
    local present, started, active, paused, state, position = true, false, false, false, "", { x = 0, y = 0, z = 0 }
    local engine = {
        reset = function() end, refresh = function() end,
        restore = function() return present, started, active end,
        frame = function() if not paused then return 0.25, position end end,
        state_matches = function(_, s) return state == s end,
        apply = function(_, _, desired) requests[#requests + 1] = desired end,
    }
    local feature = SpawnGroups.new(context, engine)
    local function condition(parameter, s, time, x)
        return { parameter = parameter, state = s or "", time = time or 0, x = x, y = x and 0, z = x and 0, radius = x and 1 }
    end
    local function load(conditions)
        feature:load({ version = 1, seed = 123, groups = {{name = "test", members = {member}, conditions = conditions}} })
        requests = {}
    end
    local function tick(count) for _ = 1, count or 1 do feature:update() end end
    local function desired() return requests[#requests] end

    load({ condition("spawn", "Start", 1, 0), condition("resume", "Go"), condition("suspend", "Stop"), condition("despawn", "End") })
    tick(); assert(desired() == "suspend")
    state, position.x = "Start", 5
    tick(); assert(not feature.groups[1].rules[1].deadline, "State and position must both match")
    position.x = 0
    tick(); local deadline = feature.groups[1].rules[1].deadline
    assert(deadline)
    state, position.x, paused = "", 5, true
    tick(20); assert(feature.groups[1].rules[1].deadline == deadline)
    paused = false
    tick(4); assert(feature.groups[1].spawned and desired() == "suspend", "Delay latches outside trigger; resume still gates activation")
    state = "Go"; tick(); assert(desired() == "active")
    state = "Stop"; tick(); assert(desired() == "suspend")
    state = "Go"; tick(); assert(desired() == "active", "Resume must rearm")
    state = "End"; tick(); assert(desired() == "despawn")
    state = "Go"; tick(); assert(desired() == "despawn", "Terminal groups cannot resume")

    present, state = false, ""
    load({condition("spawn", "", 1)})
    tick(20); assert(not feature.groups[1].spawned, "Do not start timers for unloaded chapters")
    present = true; tick(5); assert(desired() == "active")
    present = false; tick(); assert(desired() == "active", "Streaming must retain fired triggers")
    started, present, active = true, true, false
    feature:reset(); tick(); assert(desired() == "suspend", "Native suspended saves stay suspended")
    active = true
    feature:reset(); tick(); assert(desired() == "active", "Native active saves restore without waiting for a past state")
    started, active = false, false
    feature:reset(); tick(); assert(desired() == "suspend", "Earlier saves/new games must discard later trigger state")
    load({condition("spawn"), condition("suspend", "Go"), condition("resume", "Go"), condition("despawn", "Go")})
    state = "Go"; tick(); assert(desired() == "despawn", "Despawn wins simultaneous events")
    feature:load({version = 1, seed = 999, groups = {}})
    assert(#feature.groups == 0 and #messages == 1, "Reject stale seed manifest")

    local loading, native_paused = false, false
    local frame_engine = Engine.new({ game = {
        method = function(_, type_name, method)
            if type_name == "via.SceneManager" then
                assert(method == "get_Loading()")
                return {call = function() return loading end}
            end
            assert(type_name == "via.Application" and method == "get_ElapsedSecond()", "Delays must use seconds, not 60 Hz DeltaTime")
            return {call = function() return 0.008 end}
        end,
        singleton = function(_, name)
            assert(name == "app.GameManager")
            return {call = function(_, method) assert(method == "get_IsPause"); return native_paused end}
        end,
        player = function() return {call = function(_, method)
            assert(method == "get_Transform")
            return {call = function(_, name) assert(name == "get_Position"); return position end}
        end} end,
    }})
    local delta, frame_position = frame_engine:frame()
    assert(delta == 0.008 and frame_position == position)
    loading = true; assert(frame_engine:frame() == nil)
    loading, native_paused = false, true; assert(frame_engine:frame() == nil)

    -- Execute the real native adapter against strict field/method mocks.
    local fields = { IsCompleted = false, IsSpawned = false, suspendType = 0, RequestedOperation = 0 }
    local option = { get_type_definition = function() return { get_field = function() return {} end } end, set_field = function(_, key, value) assert(key == "IsForceTargetingToPlayer" and value) end }
    local go = {}
    local spawn = {
        get_field = function(_, key) assert(fields[key] ~= nil, "Unexpected field " .. key); return fields[key] end,
        set_field = function(_, key, value) assert(key == "IsPlayerTargetingAtStart" and value) end,
        call = function(_, method)
            if method == "get_GameObject" then return go end
            assert(method == "get_option", "Unexpected method " .. method); return option
        end,
    }
    local native = Engine.new({ game = {valid = function(_, object) return object == go end}, log = context.log })
    local calls = {}
    native.manager = {call = function(_, method, ...)
        calls[#calls + 1] = { method, ... }
        return false -- Simulate a temporarily full native pool: retry later.
    end}
    native.members.slot = spawn
    native:apply(member, "active", 0)
    assert(calls[1][1] == "requestSpawn(app.EnemySpawnInfo, System.Int32)")
    native:apply(member, "active", 0.1); assert(#calls == 1)
    native:apply(member, "active", 1); assert(#calls == 2)
    fields.IsSpawned = true
    native:apply(member, "active", 2); assert(#calls == 2)
    native:apply(member, "suspend", 2)
    assert(calls[3][1] == "requestSuspend(app.EnemySpawnInfo)")
    fields.RequestedOperation = 101
    native:apply(member, "active", 3); assert(#calls == 3, "Wait for asynchronous suspension")
    fields.RequestedOperation, fields.suspendType = 0, 1
    native:apply(member, "active", 4)
    assert(calls[4][1] == "requestResume(app.EnemySpawnInfo, System.Boolean, System.Boolean, System.Int32)")
    assert(calls[4][3] == false and calls[4][4] == false, "Resume without reset/dead-corner constraints")
    native:apply(member, "despawn", 5); assert(calls[5][1] == "requestKill(app.EnemySpawnInfo)")
    -- Some insect/boss options have no force-targeting field.
    option.get_type_definition = function() return {get_field = function() return nil end} end
    fields.IsSpawned, fields.suspendType = false, 0
    native:apply(member, "active", 6); assert(#calls == 6)
    fields.IsCompleted = true
    native:apply(member, "active", 6); assert(#calls == 6, "Never resurrect completed encounters")
    native.members = {}
    native:apply(member, "active", 7); assert(#calls == 6, "Unloaded slots are harmless")
end
