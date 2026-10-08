return function()
    local Static = require("BioRand7/spawn_group_static")
    local Engine = require("BioRand7/spawn_group_engine")
    local function fields(values)
        return {
            get_field = function(_, key) assert(values[key] ~= nil, "Unknown field " .. key); return values[key] end,
            set_field = function(_, key, value) assert(values[key] ~= nil, "Unknown field " .. key); values[key] = value end,
        }
    end
    local function array(values)
        return {
            get_size = function() return #values end,
            get_element = function(_, index) assert(values[index + 1] ~= nil); return values[index + 1] end,
            set_element = function(_, index, value) assert(values[index + 1] ~= nil); values[index + 1] = value end,
        }
    end
    sdk = {
        typeof = function(name) return name end,
        create_managed_array = function(type_name, count)
            assert(type_name == "System.Int32")
            local values = {}; for i = 1, count do values[i] = 0 end
            return array(values)
        end,
    }
    local storage = {}
    thread = {get_hook_storage = function() return storage end}
    local hooks, saves, objects, calls, dead = {}, {}, {}, {}, {}
    local game = {
        guid_string = function(_, guid) return guid end,
        valid = function(_, go) return go ~= nil and not go.invalid end,
        object = function(_, object) return object end,
        address = function(_, object) return object.id end,
        singleton = function() return nil end,
        hook = function(_, type_name, method, before, after) hooks[type_name .. ":" .. method] = {before, after} end,
        component = function(_, go, type_name)
            if type_name == "app.EnemySave" then return go.enemy_save end
            assert(type_name == "app.OtherObjectSave" or type_name == "app.Em3300.Em3300Save")
            return saves[go.id]
        end,
        method = function(_, type_name, method)
            if type_name == "via.SceneManager" then
                assert(method == "get_CurrentScene()")
                return {call = function() return {call = function(_, name, type_info)
                    assert(name == "findComponents(System.Type)")
                    assert(type_info == "app.OtherObjectSave" or type_info == "app.Em3300.Em3300Save")
                    local list = {}; for _, save in pairs(saves) do list[#list + 1] = save end
                    return array(list)
                end} end}
            end
            assert(type_name == "app.Util" and method == "setActive(via.GameObject, System.Boolean, System.Boolean)")
            return {call = function(_, _, go, update, draw)
                calls[#calls + 1] = {go.id, update, draw}
                go.update, go.draw = update, draw
            end}
        end,
    }
    local function actor(id, health)
        local go = {id = id, update = false, draw = false}
        go.call = function(_, method)
            if method == "get_UpdateSelf" then return go.update end
            assert(method == "get_DrawSelf"); return go.draw
        end
        if health ~= nil then go.enemy_save = fields({SaveData = fields({Health = health, MaxHealth = 1000})}) end
        local data = fields({OtherInt = array({27, 39}), IsUpdate = false})
        local save = fields({SaveGUID = id, OtherInt = array({27, 39}), OtherObjectSaveData = data})
        save.call = function(_, method) assert(method == "get_GameObject"); return go end
        objects[id], saves[id] = go, save
        return {kind = "static", runtimeGuid = id}, save, go
    end
    local mia, mia_save, mia_go = actor("mia", 700)
    local eve, eve_save, eve_go = actor("eve")
    local unrelated, unrelated_save, unrelated_go = actor("vanilla", 1000)
    local context = {game = game, log = {warn = function(_, msg) error(msg) end}, features = {static_mia = {
        dying = {},
        is_killed = function(_, _, go) return dead[go.id] end,
        remember = function(_, _, go) if go.id == "mia" then dead[go.id] = true end end,
    }}}
    local groups = {{members = {mia, eve}, conditions = {}}}
    local engine = Engine.new(context)
    engine:reset(groups)
    engine:refresh(0, groups)
    local static = engine.static
    local present, started, active = engine:restore(groups[1].members)
    assert(present and not started and not active)
    engine:apply(mia, "suspend", 0)
    assert(static:state(mia) == 0 and #calls == 0, "Waiting slots must keep their spawn gate")
    engine:apply(mia, "active", 1)
    engine:apply(eve, "active", 1)
    assert(mia_go.update and eve_go.draw)
    engine:apply(mia, "active", 1.1)
    assert(#calls == 2, "No repeated activation calls")
    engine:apply(mia, "suspend", 2)
    assert(not mia_go.update and not mia_go.draw and static:state(mia) == 2)
    assert(mia_go.enemy_save:get_field("SaveData"):get_field("Health") == 700, "Suspension preserves health")

    local saved = mia_save:get_field("OtherObjectSaveData")
    local values = saved:get_field("OtherInt")
    assert(values:get_size() == 4 and values:get_element(0) == 27 and values:get_element(1) == 39)
    static:save(mia_save, saved)
    assert(saved:get_field("OtherInt"):get_size() == 4, "Do not append duplicate metadata")
    engine:reset(groups)
    static:load(mia_save, saved)
    engine:refresh(0, groups)
    present, started, active = engine:restore({mia})
    assert(present and started and not active, "Suspension survives session reset/native load")
    engine:apply(mia, "active", 3)
    assert(mia_go.update and static:state(mia) == 1)
    local held = saves.mia
    saves.mia = nil; engine:refresh(2, groups)
    engine:apply(mia, "suspend", 4)
    saves.mia = held; engine:refresh(3, groups)
    assert(static:state(mia) == 1, "Streaming keeps state until the object returns")
    engine:apply(mia, "despawn", 5)
    assert(static:state(mia) == 3 and dead.mia and not mia_go.update)
    engine:apply(mia, "active", 6)
    assert(not mia_go.update, "Completed Mia cannot resume")

    -- Native health/death also wins when no despawn rule fired.
    dead.mia = nil
    engine:reset(groups)
    mia_go.enemy_save:get_field("SaveData"):set_field("Health", 0)
    engine:refresh(0, groups)
    engine:apply(mia, "active", 0)
    assert(static:state(mia) == 3 and not mia_go.update)

    -- The explosive Eveline feature marks completion before destroying the actor.
    static:complete(eve_go)
    local eve_data = eve_save:get_field("OtherObjectSaveData")
    engine:reset(groups)
    static:load(eve_save, eve_data)
    engine:refresh(0, groups)
    eve_go.update, eve_go.draw = true, true -- Simulate a native load/activation attempt.
    engine:apply(eve, "active", 0)
    assert(static:state(eve) == 3 and not eve_go.update and not eve_go.draw)

    -- Earlier saves restore waiting state instead of leaking future completion.
    local earlier = fields({OtherInt = array({27, 39, 0x42525347, 0}), IsUpdate = false})
    engine:reset(groups)
    static:load(eve_save, earlier)
    engine:refresh(0, groups)
    assert(static:state(eve) == 0)
    engine:apply(eve, "suspend", 0)
    assert(static:state(eve) == 0)

    -- Exercise the actual save hooks, including nested base/derived calls.
    local base = hooks["app.OtherObjectSave:getOtherObjectSaveData()"]
    local derived = hooks["app.Em3300.Em3300Save:getOtherObjectSaveData()"]
    derived[1]({nil, eve_save}); base[1]({nil, eve_save})
    base[2](earlier); derived[2](earlier)
    assert(#storage.biorand_group_saves == 0)
    local load_hook = hooks["app.Em3300.Em3300Save:setOtherObjectSaveData(app.OtherObjectSave.OtherObjectSaveDataClass)"]
    load_hook[1]({nil, eve_save, earlier})
    static:save(unrelated_save, unrelated_save:get_field("OtherObjectSaveData"))
    assert(unrelated_save:get_field("OtherObjectSaveData"):get_field("OtherInt"):get_size() == 2)
    assert(not unrelated_go.update and not dead.vanilla, "Ungrouped actors are untouched")
    engine:reset({})
    assert(next(static.wanted) == nil)
end
