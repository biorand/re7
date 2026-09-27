local Helpers = {}

local list_type = {}
function list_type:get_field(name)
    assert(name == "mItems" or name == "mSize", name)
    return { get_data = function(_, object) return object[name] end }
end

function Helpers.list(items, count)
    local array = { get_element = function(_, index)
        assert(index >= 0 and index < count, "Array index out of bounds")
        return items[index]
    end }
    return { mItems = array, mSize = count, get_type_definition = function() return list_type end }
end

return Helpers
