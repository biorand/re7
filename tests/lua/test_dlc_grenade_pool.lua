return function()
    local Pool = require("BioRand7/dlc_grenade_pool")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            add_ref = function(self) return self end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local now, paused, loading, ready, valid, prefab_ready = 0.0, false, false, false, true, false
    local singleton, owner, path, standby, count, unused = nil, nil, nil, nil, 6, 6
    local created, destroyed, released = 0, 0, 0
    os.clock = function() return now end
    local transform = object({}, { get_Position = function() return "position" end })
    local player = object({}, { get_Transform = function() return transform end })
    local game_object = object({}, { get_Transform = function()
        return object({}, { set_Parent = function(parent) assert(parent == transform); owner = parent end })
    end })
    game_object.release = function() released = released + 1 end
    local elements = object({}, { get_Count = function() return count end })
    local list = { get_field = function(_, key) return key == "List" and elements or unused end }
    local shell = object({ Throwable01List = list, Throwable02List = list, Throwable03List = list },
        { get_Valid = function() return valid end })
    local gm = object({}, { get_IsSceneLoading = function() return loading end, get_IsPause = function() return paused end })
    local game = {
        valid = function(_, value) assert(value == game_object); return valid end,
        singleton = function(_, kind) return kind == "app.GameManager" and gm or singleton end,
        component = function(_, go, kind) assert(go == game_object and kind == "app.CH8ShellManager"); return ready and shell or nil end,
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
                return game_object
            end,
        })
    end }
    local pool = Pool.new(game, "BioRand/DlcWeapons")
    assert(not pool:update(nil) and created == 0)
    loading = true; assert(not pool:update(player) and created == 0); loading = false
    paused = true; assert(not pool:update(player) and created == 0); paused = false
    singleton = {}; assert(not pool:update(player) and created == 0); singleton = nil
    assert(not pool:update(player) and created == 1 and path == "BioRand/DlcWeapons/GrenadeShellManager.pfb")
    now = 1.0; prefab_ready = true
    assert(not pool:update(player) and owner == transform and not pool:available(0))
    assert(not pool:update(player) and not pool:owned())
    ready, singleton = true, shell
    count = 5; assert(not pool:update(player)); count = 6
    assert(pool:update(player) and pool:owned())
    for kind = 0, 2 do assert(pool:available(kind)) end
    assert(not pool:available(3))
    unused = 0; assert(not pool:available(0)); unused = 6
    for _ = 1, 10 do assert(pool:update(player)) end
    assert(created == 1, "Do not create another manager or manually tick native pools")

    pool:reset(); assert(destroyed == 0, "Reset hooks defer native changes")
    singleton = {}; assert(not pool:update(nil) and destroyed == 0 and not pool:available(0))
    singleton = shell
    assert(not pool:update(nil) and destroyed == 1)
    assert(not pool:update(nil) and destroyed == 1, "Do not repeat asynchronous destruction")
    valid, singleton = false, nil
    assert(not pool:update(nil) and released == 1 and pool.object == nil)

    valid, ready = true, false
    assert(not pool:update(player) and pool.object == game_object)
    now = 12.0
    local ok, message = pcall(function() pool:update(player) end)
    assert(not ok and message:find("initialization timed out"))
    pool:reset(); pool:update(nil); valid = false; pool:update(nil)
    assert(destroyed == 2 and released == 2)

    local timeout_pool = Pool.new(game, "BioRand/DlcWeapons")
    prefab_ready = false
    assert(not timeout_pool:update(player))
    now = 23.0
    assert(not pcall(function() timeout_pool:update(player) end))
end
