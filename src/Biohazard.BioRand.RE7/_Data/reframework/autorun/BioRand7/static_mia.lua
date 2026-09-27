local StaticMia = {}
StaticMia.__index = StaticMia

local NAME_PREFIX = "BioRandExtraEnemyStatic_Em2000_"
local EMPTY_GUID = "00000000-0000-0000-0000-000000000000"

local function round(value)
    local lower = math.floor(value)
    local fraction = value - lower
    if fraction > 0.5 or (fraction == 0.5 and lower % 2 ~= 0) then
        return lower + 1
    end
    return lower
end

function StaticMia.new(context)
    return setmetatable({ context = context, killed = {}, suppressed = {} }, StaticMia)
end

function StaticMia:is_static(game_object)
    return game_object ~= nil and game_object:call("get_Name"):sub(1, #NAME_PREFIX) == NAME_PREFIX
end

function StaticMia:controller_game_object(controller)
    return controller:call("get_GameObject")
end

function StaticMia:fallback_key(game_object)
    local folder = game_object:call("get_Folder")
    local folder_path = folder == nil and "" or folder:call("get_Path")
    local position = game_object:call("get_Transform"):call("get_Position")
    return ("fallback:%s:%s:%d:%d:%d"):format(
        folder_path,
        game_object:call("get_Name"),
        round(position.x * 100),
        round(position.y * 100),
        round(position.z * 100))
end

function StaticMia:guid_key(controller, field, prefix)
    if controller == nil then return nil end
    local guid = controller:get_field(field):call("ToString()")
    if guid ~= EMPTY_GUID then return prefix .. guid end
end

function StaticMia:keys(controller, game_object)
    controller = controller or self.context.game:component(game_object, "app.EnemyActionController")
    local keys = {}
    keys[#keys + 1] = self:guid_key(controller, "SpawnerGuid", "guid:spawner:")
    keys[#keys + 1] = self:guid_key(controller, "ActualUsingGuid", "guid:actual:")
    keys[#keys + 1] = self:fallback_key(game_object)
    return keys
end

function StaticMia:is_killed(controller, game_object)
    if next(self.killed) == nil then return false end
    if not self:is_static(game_object) then
        return false
    end
    controller = controller or self.context.game:component(game_object, "app.EnemyActionController")
    return self.killed[self:guid_key(controller, "SpawnerGuid", "guid:spawner:")]
        or self.killed[self:guid_key(controller, "ActualUsingGuid", "guid:actual:")]
        or self.killed[self:fallback_key(game_object)] or false
end

function StaticMia:remember(controller, game_object)
    if not self:is_static(game_object) then
        return false
    end
    for _, key in ipairs(self:keys(controller, game_object)) do
        self.killed[key] = true
    end
    return true
end

function StaticMia:suppress(controller, game_object)
    if not self:is_killed(controller, game_object) then
        return false
    end
    self.context.game:method("app.Util", "setActive(via.GameObject, System.Boolean, System.Boolean)")
        :call(nil, game_object, false, false)
    self.suppressed[self.context.game:address(game_object)] = true
    return true
end

function StaticMia:install()
    local game = self.context.game
    local function suppress(args)
        if next(self.killed) == nil then return end
        local controller = game:object(args[2])
        if self:suppress(controller, self:controller_game_object(controller)) then
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
    end
    game:hook("app.Em2000.Em2000ActionController", "reactivate()", suppress)
    game:hook("app.Em2000.Em2000ActionController", "doStart()", suppress)
    game:hook("app.Em2000.Em2000ActionController", "doUpdate()", suppress)
end

function StaticMia:reset()
    self.killed = {}
    self.suppressed = {}
end

return StaticMia
