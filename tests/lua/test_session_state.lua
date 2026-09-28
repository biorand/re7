return function()
    local Context = require("BioRand7/context")
    local Game = require("BioRand7/game")
    local StaticMia = require("BioRand7/static_mia")
    local EnemyDrops = require("BioRand7/enemy_drops")
    local hooks, storage = {}, {}
    sdk = { PreHookResult = { SKIP_ORIGINAL = 1 } }
    thread = { get_hook_storage = function() return storage end }
    local game = Game.new()
    game.object = function(_, value) return value end
    game.hook = function(_, name, signature, before, after)
        hooks[name .. ":" .. signature] = { before = before, after = after }
    end
    local function object(fields, methods)
        return {
            get_field = function(_, field) return fields[field] end,
            set_field = function(_, field, value) fields[field] = value end,
            call = function(_, method, ...)
                return assert(methods[method], "Unexpected method: " .. method)(...)
            end,
        }
    end
    local guid = "01234567-89ab-cdef-0123-456789abcdef"
    local function guid_value(get_text)
        return {get_field=function(_, field)
            local hex=get_text():gsub("-", "")
            local ranges={mData1={1,8},mData2={9,12},mData3={13,16}}
            local range=ranges[field]
            if range then return tonumber(hex:sub(range[1],range[2]),16) end
            local index=assert(tonumber(field:match("mData4_(%d)")))
            return tonumber(hex:sub(17+index*2,18+index*2),16)
        end, call=function() error("Unboxed GUIDs must not invoke ToString") end}
    end
    local empty_guid = guid_value(function() return "00000000-0000-0000-0000-000000000000" end)
    local controller = object({
        SpawnerGuid = guid_value(function() return guid end),
        ActualUsingGuid = empty_guid,
    }, {})
    local position = { x = 1, y = 2, z = 3 }
    local transform = object({}, { get_Position = function() return position end })
    local enemy = object({}, {
        get_Name = function() return "BioRandExtraEnemyStatic_Em2000_test" end,
        get_Folder = function() return nil end,
        get_Transform = function() return transform end,
    })
    enemy.get_address = function() return 123 end
    game.component = function(_, _, name) if name == "app.EnemyActionController" then return controller end end
    local context = setmetatable({ game = game, features = {}, config = { get = function(_, _, default) return default end } }, Context)
    local mia = StaticMia.new(context)
    local drops = EnemyDrops.new(context)
    context.features = { static_mia = mia, enemy_drops = drops }
    context:install_session_hooks()
    mia:install()
    assert(hooks["app.SaveDataManager:folderLoad(via.Folder)"] == nil, "Room streaming must preserve the current session")

    local first_identity = game:enemy_identity(enemy)
    assert(first_identity == "enemy:" .. guid)
    local first_drop = drops:rng(enemy, 1):next()
    enemy.get_address = function() return 987654 end
    position.x = 100
    assert(game:enemy_identity(enemy) == first_identity, "Allocation and movement cannot change spawn identity")
    assert(drops:rng(enemy, 50):next() == first_drop, "Load order and respawn counters cannot change a seed's drop")
    guid = "11111111-89ab-cdef-0123-456789abcdef"
    assert(game:enemy_identity(enemy) ~= first_identity)
    assert(drops:rng(enemy, 1):next() ~= first_drop)

    assert(mia:remember(controller, enemy))
    drops:begin(enemy)
    local fields = { Health = 100, IsUpdate = true, IsDraw = true }
    local data = object(fields, {})
    local save = object({}, { get_GameObject = function() return enemy end })
    local save_hook = hooks["app.Em2000Order:saveData(app.EnemyStatus.EnemySaveDataClass)"]
    save_hook.before({ nil, save, data })
    assert(save_hook.after(0) == 0)
    assert(fields.Health == 0 and not fields.IsUpdate and not fields.IsDraw)

    hooks["app.SaveDataManager:loadLevelUsingLoadData()"].before()
    assert(next(mia.killed) == nil and next(drops.dropped) == nil)
    hooks["app.Em2000Order:loadData(app.EnemyStatus.EnemySaveDataClass)"].before({ nil, save, data })
    assert(mia:is_killed(controller, enemy), "Loading a saved death restores suppression")
    fields.Health = 100
    hooks["app.Em2000Order:loadData(app.EnemyStatus.EnemySaveDataClass)"].before({ nil, save, data })
    assert(not mia:is_killed(controller, enemy), "An earlier save must restore the living enemy")
    mia:remember(controller, enemy)
    hooks["app.SaveDataManager:newGameInit()"].before()
    assert(not mia:is_killed(controller, enemy), "Deaths cannot carry into a new game")
end
