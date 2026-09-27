return function()
    local ObjectCache = require("BioRand7/object_cache")
    local Game = require("BioRand7/game")
    local list = dofile("tests/lua/helpers.lua").list
    local groups, manager
    local game = Game.new()
    local type_lookups, field_lookups, method_lookups = 0, 0, 0
    sdk = {
        get_managed_singleton = function() return manager end,
        typeof = function(name) type_lookups = type_lookups + 1; return name end,
        find_type_definition = function(name)
            assert(name == "via.GameObject")
            return { get_method = function(_, signature)
                method_lookups = method_lookups + 1
                assert(signature == "get_Valid" or signature == "getComponent(System.Type)")
                return { call = function(_, object)
                    if signature == "get_Valid" then return object.valid end
                    return object.component
                end }
            end }
        end,
    }
    manager = { get_field = function(_, name) assert(name == "ManagedObjects"); return groups end }
    local function object(address)
        return { valid = true, get_address = function() return address end }
    end
    local objects = {}
    for index = 0, 129 do objects[index] = object(index + 1) end
    local first, last = objects[0], objects[129]
    objects[30] = nil
    objects[31].valid = false
    local group = list(objects, 130)
    groups = list({ [0] = group, [2] = list({ [0] = first }, 1) }, 3)
    local selected = 0
    local cache = ObjectCache.new(game, function(value)
        selected = selected + 1
        return value
    end)

    cache:register(last)
    assert(cache.values[130] == last, "An FSM registration must be available before the first sweep")
    selected = 0

    cache:update(0)
    assert(selected == 62 and cache.values[1] == first and cache.values[130] == last)
    cache:update(0.01)
    assert(selected == 126)
    cache:update(0.02)
    assert(selected == 129 and cache.values[130] == last)
    local count = 0
    for _ in pairs(cache.values) do count = count + 1 end
    assert(count == 128, "Duplicate registrations must produce only one cached target")
    cache:update(0.1)
    assert(selected == 129, "Completed sweeps must be throttled")

    -- Shrinking a list mid-sweep must not index its old count or prune early.
    cache:update(0.6)
    assert(cache.values[130] == last)
    group.mSize = 1
    cache:update(0.61)
    assert(cache.values[130] == nil and cache.values[1] == first)

    local second = object(200)
    groups = list({ [0] = list({ [0] = second }, 1) }, 1)
    cache:update(0.62)
    assert(cache.values[1] == nil and cache.values[200] == second,
        "Replacing the collection must invalidate the old cache even during the scan cooldown")
    manager = nil
    cache:update(0.63)
    assert(next(cache.values) == nil, "Unloading the manager must release cached objects")

    first.component = {}
    assert(game:component(first, "app.EnemyActionController") == first.component)
    assert(game:component(first, "app.EnemyActionController") == first.component)
    assert(type_lookups == 1 and method_lookups == 2, "Cache method and runtime type definitions")

    local definitions = { get_field = function(_, name)
        field_lookups = field_lookups + 1
        return { get_data = function(_, value) return value[name] end }
    end }
    local raw = { mItems = { get_element = function(_, index)
        assert(index < 3, "Use mSize rather than array capacity")
        return ({ [0] = first, [2] = second })[index]
    end }, mSize = 3, get_type_definition = function() return definitions end }
    local iterator = game:list(raw)
    assert(iterator() == first and iterator() == second and iterator() == nil)
    game:list_storage(raw)
    assert(field_lookups == 2, "List fields must only be resolved once per specialization")
end
