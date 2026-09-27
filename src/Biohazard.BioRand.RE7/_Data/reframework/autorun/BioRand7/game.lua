local Game = {}
Game.__index = Game

local function type_definition(name)
    return sdk.find_type_definition(name)
end

function Game.new()
    return setmetatable({ methods = {}, fields = {} }, Game)
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
    return game_object:call("getComponent(System.Type)", sdk.typeof(type_name))
end

function Game:list(collection)
    local index = 0
    local count = collection ~= nil and collection:call("get_Count") or 0
    return function()
        while index < count do
            local value = collection:call("get_Item", index)
            index = index + 1
            if value ~= nil then return value end
        end
    end
end

function Game:address(object)
    return object:get_address()
end

return Game
