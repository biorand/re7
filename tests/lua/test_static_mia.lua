return function()
    local StaticMia = require("BioRand7/static_mia")
    local function component(fields)
        return { get_field = function(_, name) return fields[name] end }
    end
    local function enemy(save_guid, spawner_guid)
        local actor = {
            controller = component({ SpawnerGuid = spawner_guid, ActualUsingGuid = "shared-template-guid" }),
            save = save_guid and component({ SaveGUID = save_guid }),
        }
        function actor:call(method)
            if method == "get_Name" then return "BioRandExtraEnemyStatic_Em2000_000" end
            if method == "get_Folder" then return nil end
            assert(method == "get_Transform")
            return { call = function() return { x = 1, y = 2, z = 3 } end }
        end
        return actor
    end
    local suppressed = {}
    local game = {
        guid_string = function(_, guid) return guid end,
        address = function(_, actor) return actor end,
        component = function(_, actor, name)
            if name == "app.EnemySave" then return actor.save or nil end
            assert(name == "app.EnemyActionController")
            return actor.controller
        end,
        method = function()
            return { call = function(_, _, actor) suppressed[actor] = true end }
        end,
    }
    local mia = StaticMia.new({ game = game })
    for _, with_save in ipairs({ true, false }) do
        mia:reset()
        local first = enemy(with_save and "first-save", with_save and "shared-spawner" or "first-spawner")
        local second = enemy(with_save and "second-save", with_save and "shared-spawner" or "second-spawner")
        mia:begin_death(first.controller, first)
        assert(mia:is_killed(first.controller, first))
        assert(not mia:is_killed(second.controller, second),
            "Killing one Mia must not kill another with a shared template GUID or fallback location")
        assert(not mia:suppress(second.controller, second) and not suppressed[second])

        local living = { Health = 100, IsUpdate = true, IsDraw = true }
        local data = component(living)
        data.set_field = function(_, key, value) living[key] = value end
        local save = { call = function() return second end }
        mia:save(save, data)
        assert(living.Health == 100 and living.IsUpdate and living.IsDraw,
            "Another Mia's death must not poison a living enemy's save")
        mia:restore(save, data)
        assert(mia:is_killed(first.controller, first), "Loading the survivor must not revive the dead Mia")

        mia:reset()
        living.Health = 0
        mia:restore(save, data)
        assert(mia:is_killed(second.controller, second))
        assert(not mia:is_killed(first.controller, first), "Reloading a dead Mia must suppress only that instance")
        mia:reset()
        assert(not mia:is_killed(second.controller, second))
    end
end
