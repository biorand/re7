return function()
    local Game = require("BioRand7/game")
    local EnemyDrops = require("BioRand7/enemy_drops")
    local empty_guid = "00000000-0000-0000-0000-000000000000"

    local function guid(value)
        if value == nil then return nil end
        local hex = value:gsub("-", "")
        local fields = {
            mData1 = tonumber(hex:sub(1, 8), 16),
            mData2 = tonumber(hex:sub(9, 12), 16),
            mData3 = tonumber(hex:sub(13, 16), 16),
        }
        for index = 0, 7 do
            fields["mData4_" .. index] = tonumber(hex:sub(17 + index * 2, 18 + index * 2), 16)
        end
        return { get_field = function(_, name) return assert(fields[name], name) end }
    end

    local function component(fields)
        return { get_field = function(_, name) return fields[name] end }
    end

    local function enemy(actual_guid, spawner_guid, save_guid)
        local fields = { ActualUsingGuid = guid(actual_guid), SpawnerGuid = guid(spawner_guid) }
        return {
            controller_fields = fields,
            controller = component(fields),
            save = component({ SaveGUID = guid(save_guid) }),
            call = function(_, method)
                if method == "get_Folder" then
                    return { call = function(_, name)
                        assert(name == "get_Path")
                        return "scene/enemies"
                    end }
                end
                assert(method == "get_Name", "Identity must not read mutable position or addresses")
                return "Em3600"
            end,
        }
    end

    local game = Game.new()
    game.component = function(_, object, name)
        if name == "app.EnemyActionController" then return object.controller end
        assert(name == "app.EnemySave")
        return object.save
    end
    game.singleton = function(_, name)
        if name == "app.GameFlowFsmManager" then
            return { call = function(_, method)
                assert(method == "get_CurrentMainGameFlow")
                return 8
            end }
        end
        assert(name == "app.GameManager")
        return component({ GameDifficulty = 1 })
    end

    -- Seed 670486's live Marguerites shared a template SpawnerGuid, even after
    -- death. ActualUsingGuid identified each encounter independently of its pool slot.
    local template_guid = "1cc82c27-c6f0-04f9-3bf1-a42e45bfde4b"
    local encounters = {
        { "53700d0c-65f7-484b-978a-8fb5955078da", "HandgunBulletL", 10 },
        { "e76985c3-1598-4d9b-ab7d-e38b174c72a3", "AcidBulletS", 4 },
        { "e563054b-e138-4bd7-9cc4-e6df71731d99", "Coin", 2 },
    }
    local config = {
        ["biorand-seed"] = 670486,
        ["allow-dlc-items"] = true,
        ["enemy-drop-valuable-weapon"] = true,
        ["enemy-drop-valuable-birthday-skill"] = true,
    }
    for id, ratio in pairs({
        liquidbomb = 0.05, handgunbulletl = 0.2, shotgunbullet = 0.2, magnumbullet = 0.05,
        flamebullets = 0.08, acidbullets = 0.08, remedyl = 0.05, chemicalm = 0.1, coin = 0.2,
    }) do config["enemy-drop-ratio-" .. id] = ratio end
    local context = { game = game, config = { get = function(_, key, default)
        if config[key] ~= nil then return config[key] end
        return default
    end } }
    local drops = EnemyDrops.new(context)
    local pooled_enemy = enemy(nil, template_guid, nil)
    for _, encounter in ipairs(encounters) do
        -- Reuse the same pool object for distinct encounters.
        pooled_enemy.controller_fields.ActualUsingGuid = guid(encounter[1])
        local id, amount = drops:select(pooled_enemy, 1, "Em3600")
        assert(id == encounter[2] and amount == encounter[3],
            "Pooled bosses must use their encounter GUID, not repeat the template's reward")
    end
    local reloaded_drops = EnemyDrops.new(context)
    for index = #encounters, 1, -1 do
        local encounter = encounters[index]
        -- Reload into a different pool slot, with a different save GUID and call order.
        local reloaded = enemy(encounter[1], template_guid, template_guid)
        local id, amount = reloaded_drops:select(reloaded, 50, "Em3600")
        assert(id == encounter[2] and amount == encounter[3], "Reload must preserve the encounter's roll")
    end

    for _, actual in ipairs({ empty_guid, false }) do
        local static = enemy(actual or nil, template_guid, encounters[1][1])
        assert(game:enemy_identity(static) == "enemy:" .. template_guid, "Static enemies retain their spawner fallback")
        static.controller_fields.SpawnerGuid = guid(empty_guid)
        assert(game:enemy_identity(static) == "enemy:" .. encounters[1][1], "Empty controller GUIDs use the save GUID")
        static.controller = nil
        assert(game:enemy_identity(static) == "enemy:" .. encounters[1][1], "Save-only enemies retain a stable identity")
        static.save = nil
        assert(game:enemy_identity(static) == "object:scene/enemies:Em3600", "Unidentified enemies use scene and name")
    end
end
