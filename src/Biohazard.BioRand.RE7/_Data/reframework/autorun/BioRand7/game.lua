local Game = {}
Game.__index = Game

local function type_definition(name)
    return sdk.find_type_definition(name)
end

function Game.new()
    return setmetatable({ methods = {}, fields = {}, runtime_types = {}, list_fields = {} }, Game)
end

function Game:method(type_name, signature)
    local key = type_name .. ":" .. signature
    local method = self.methods[key]
    if method == nil then
        method = type_definition(type_name):get_method(signature)
        self.methods[key] = method
    end
    return method
end

function Game:field(type_name, name)
    local key = type_name .. ":" .. name
    local field = self.fields[key]
    if field == nil then
        field = type_definition(type_name):get_field(name)
        self.fields[key] = field
    end
    return field
end

function Game:static_field(type_name, name)
    return self:field(type_name, name):get_data(nil)
end

function Game:set_static_field(type_name, name, value)
    sdk.set_native_field(nil, type_definition(type_name), name, value)
end

function Game:singleton(type_name)
    return sdk.get_managed_singleton(type_name)
end

function Game:object(pointer)
    return sdk.to_managed_object(pointer)
end

function Game:hook(type_name, signature, before, after)
    sdk.hook(self:method(type_name, signature), before, after)
end

function Game:player()
    local manager = self:singleton("app.ObjectManager")
    if manager == nil then return nil end
    local player = manager:get_field("PlayerObj")
    if player ~= nil and player:call("get_Valid") then return player end
    player = manager:call("findActivePlayer")
    if player ~= nil and player:call("get_Valid") then return player end
    player = self:method("app.GameManager", "getPlayer()"):call(nil)
    if player ~= nil and player:call("get_Valid") then return player end
    return nil
end

function Game:difficulty()
    local manager = self:singleton("app.GameManager")
    if manager ~= nil then return manager:get_field("GameDifficulty") end
    return nil
end

function Game:chapter()
    local manager = self:singleton("app.GameFlowFsmManager")
    if manager ~= nil then return manager:call("get_CurrentMainGameFlow") end
    return nil
end

function Game:component(game_object, type_name)
    if game_object == nil then return nil end
    local runtime_type = self.runtime_types[type_name]
    if runtime_type == nil then
        runtime_type = sdk.typeof(type_name)
        self.runtime_types[type_name] = runtime_type
    end
    self.component_method = self.component_method or self:method("via.GameObject", "getComponent(System.Type)")
    return self.component_method:call(game_object, runtime_type)
end

function Game:valid(game_object)
    if game_object == nil then return false end
    self.valid_method = self.valid_method or self:method("via.GameObject", "get_Valid")
    return self.valid_method:call(game_object)
end

function Game:list_storage(collection)
    if collection == nil then return nil, 0 end
    local definition = collection:get_type_definition()
    local fields = self.list_fields[definition]
    if fields == nil then
        -- RE7 List<T> uses mItems/mSize, not the newer games' _items/_size.
        fields = { items = definition:get_field("mItems"), count = definition:get_field("mSize") }
        self.list_fields[definition] = fields
    end
    return fields.items:get_data(collection), fields.count:get_data(collection)
end

function Game:list(collection)
    local index = 0
    local items, count = self:list_storage(collection)
    return function()
        while index < count do
            local value = items:get_element(index)
            index = index + 1
            if value ~= nil then return value end
        end
    end
end

function Game:address(object)
    return object:get_address()
end

return Game
