return function()
    local Pool = require("BioRand7/dlc_ch9_pool")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local now, paused, loading, ready, valid, prefab_ready = 0.0, false, false, false, true, false
    local singleton, path, standby, missing = nil, nil, nil, nil
    local created, instantiated, destroyed, released = 0, 0, 0, 0
    os.clock = function() return now end
    local transform = object({}, { get_Position = function() return "position" end })
    local player = object({}, { get_Transform = function() return transform end })
    local replacement = object({}, { get_Transform = function() return transform end })
    -- No transform/set_Parent method: projectiles must never inherit player movement.
    local game_object = object({}, {})
    game_object.release = function() released = released + 1 end
    local lists = {}
    for name, count in pairs({ ThrowingWp1500PrefabList = 2, ThrowingWp1800PrefabList = 10,
        LiquidBombList = 5, ThrowingWp0000PrefabList = 2 }) do
        local fields = { UnusedCount = count }
        fields.List = object({}, { get_Count = function() return name == missing and count - 1 or count end })
        lists[name] = object(fields, {})
    end
    local shell = object(lists, { get_Valid = function() return valid end, get_GameObject = function() return game_object end })
    local gm = object({}, { get_IsSceneLoading = function() return loading end, get_IsPause = function() return paused end })
    local game = {
        valid = function(_, value) assert(value == game_object); return valid end,
        singleton = function(_, kind) return kind == "app.GameManager" and gm or singleton end,
        component = function(_, go, kind) assert(go == game_object and kind == "app.CH9ShellManager"); return ready and shell or nil end,
        method = function(_, kind, signature)
            assert(kind == "via.GameObject" and signature == "destroy(via.GameObject)")
            return { call = function(_, receiver, go) assert(receiver == nil and go == game_object); destroyed = destroyed + 1 end }
        end,
    }
    sdk = { create_instance = function(kind, construct)
        assert(kind == "via.Prefab" and construct)
        created = created + 1
        return object({}, {
            set_Standby = function(value) standby = value end,
            set_Path = function(value) path = value end,
            get_Ready = function() return prefab_ready end,
            get_Valid = function() return true end,
            ["instantiate(via.vec3)"] = function(position)
                assert(position == "position" and standby)
                instantiated = instantiated + 1
                return game_object
            end,
        })
    end }
    local pool = Pool.new(game, "BioRand/Research/CH9Projectiles")
    assert(not pool:update(nil) and created == 0)
    loading = true; assert(not pool:update(player) and created == 0); loading = false
    paused = true; assert(not pool:update(player) and created == 0); paused = false
    singleton = {}; assert(not pool:update(player) and created == 0); singleton = nil
    assert(not pool:update(player) and created == 1 and path == "BioRand/Research/CH9Projectiles/CH9ShellManager.pfb")
    now = 1.0; prefab_ready = true
    assert(not pool:update(player) and instantiated == 1 and not pool:available(4))
    assert(not pool:update(player) and not pool:owned())
    ready, singleton = true, shell
    for name in pairs(lists) do missing = name; assert(not pool:update(player)) end
    missing = nil
    assert(pool:update(player) and pool:owned())
    for kind = 4, 6 do assert(pool:available(kind)) end
    assert(not pool:available(nil) and not pool:available(0) and not pool:available(7))
    local saved = lists.ThrowingWp1800PrefabList
    lists.ThrowingWp1800PrefabList = object({ UnusedCount = 0 }, {})
    assert(not pool:available(5) and pool:available(4)); lists.ThrowingWp1800PrefabList = saved
    for _ = 1, 10 do assert(pool:update(player)) end
    assert(created == 1 and instantiated == 1, "Do not manually tick or duplicate native pools")

    pool:reset(); assert(destroyed == 0 and not pool:available(5), "Reset disables hooks before deferred destruction")
    singleton = {}; assert(not pool:update(nil) and destroyed == 0)
    singleton = shell
    assert(not pool:update(nil) and destroyed == 1)
    assert(not pool:update(nil) and destroyed == 1)
    valid, singleton = false, nil
    assert(not pool:update(nil) and released == 1 and pool.object == nil)

    valid, ready = true, false
    assert(not pool:update(player) and instantiated == 2)
    ready, singleton = true, shell
    assert(pool:update(player))
    assert(not pool:update(replacement) and destroyed == 2 and pool.player == player)
    valid, singleton = false, nil
    assert(not pool:update(nil) and released == 2)
    valid, ready = true, false
    assert(not pool:update(replacement) and instantiated == 3 and pool.player == replacement)
    now = 12.0
    local ok, message = pcall(function() pool:update(replacement) end)
    assert(not ok and message:find("initialization timed out"))
    pool:reset(); pool:update(nil); valid = false; pool:update(nil)
    assert(destroyed == 3 and released == 3)

    local timeout_pool = Pool.new(game, "BioRand/Research/CH9Projectiles")
    prefab_ready = false
    assert(not timeout_pool:update(player))
    now = 30.0; paused = true; assert(not timeout_pool:update(player)); paused = false
    assert(not timeout_pool:update(player), "Paused time does not spend the readiness budget")
    now = 41.0
    assert(not pcall(function() timeout_pool:update(player) end))
end
