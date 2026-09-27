local ReloadSpeed = {}
ReloadSpeed.__index = ReloadSpeed

local WEAPON_NAMES = {
    [0] = "hand", [1] = "handaxe", [2] = "circularsaw", [3] = "knife", [4] = "bar",
    [5] = "handgun", [6] = "handgun-m19", [7] = "handgun-g17", [8] = "handgun-mpm",
    [9] = "handgun-albert", [10] = "shotgun", [11] = "shotgun-m37", [12] = "shotgun-m37s",
    [13] = "shotgun-db", [14] = "machinegun", [15] = "magnum", [16] = "grenadelauncher",
    [17] = "burner", [18] = "candle", [19] = "glasses", [20] = "evelynradar",
    [21] = "liquidbomb", [22] = "timebomb", [23] = "flare", [24] = "remedy", [25] = "eyedrops",
    [26] = "stimulant", [27] = "depressant", [28] = "kitchenknife", [29] = "chainsaw",
    [30] = "woodchip", [31] = "handlight", [32] = "chaincutter", [33] = "screwdriver",
    [34] = "shovel", [35] = "lantern", [36] = "roller", [37] = "scissors", [38] = "stick",
    [39] = "lanternbar", [40] = "glasspiece", [41] = "fireaxe", [42] = "miaknife",
    [43] = "goldenbar", [44] = "hyperblaster", [45] = "barcircularsaw",
    [46] = "handgun-albert-reward", [47] = "fireaxebreakable", [48] = "cknife",
    [49] = "handgun-albert-c", [50] = "shotgun-albert", [51] = "blueblaster", [52] = "redblaster",
    [53] = "birthday003", [54] = "birthday004", [55] = "lantern-c", [56] = "lighter-z",
    [57] = "gimmickknife", [58] = "grenadebomb", [59] = "thermatebomb", [60] = "stangrenadebomb",
    [61] = "ch9-wp000", [62] = "ch9-wp001", [63] = "ch9-wp002", [64] = "ch9-wp003",
    [65] = "ch9-wp004", [66] = "ch9-wp005", [67] = "ch9-wp006", [68] = "ch9-wp007",
    [69] = "ch9-wp008", [70] = "ch9-wp009", [71] = "num", [9999] = "etc",
}

local MULTIPLIER_KEYS = {}
for id, name in pairs(WEAPON_NAMES) do
    MULTIPLIER_KEYS[id] = "weapon-reload-speed-multiplier-" .. name
end

local function round_rate(value)
    local scaled = value * 100
    local lower = math.floor(scaled)
    local fraction = scaled - lower
    if fraction > 0.5 or (fraction == 0.5 and lower % 2 ~= 0) then
        lower = lower + 1
    end
    return lower / 100
end

function ReloadSpeed.new(context)
    return setmetatable({ context = context }, ReloadSpeed)
end

function ReloadSpeed:multiplier(weapon_id)
    local key = MULTIPLIER_KEYS[weapon_id]
    if key == nil then
        return nil
    end
    return self.context.config:get(key)
end

function ReloadSpeed:apply(controller)
    local weapon_id = controller:get_field("CurrentWeaponID")
    local multiplier = self:multiplier(weapon_id)
    if multiplier == nil then
        local weapon = controller:get_field("CurrentWeapon")
        if weapon ~= nil then
            weapon_id = weapon:get_field("WeaponID")
            multiplier = self:multiplier(weapon_id)
        end
    end
    if multiplier == nil then
        return
    end

    local depressant = math.max(0, controller:get_field("DepressantLevel"))
    if depressant > 0 and not self.context.config:get("weapon-mod-reload-speed-include-stabilizers", true) then
        multiplier = 1.0
    end

    local table_data = controller:get_field("PlayerReloadSpeedRateTable")
    local motion_manager = controller:get_field("MotionManager")
    if table_data == nil or motion_manager == nil then return end
    local base_rate = table_data:call("getReloadSpeedRate(System.Int32)", depressant)
    local rate = math.max(0.1, round_rate(base_rate * multiplier))
    controller:set_field("ReloadSpeedRate", rate)
    local hash = self.context.game:static_field("app.PlayerMotionController.VariableNameHash", "fReloadSpeedRate")
    motion_manager:call("setFloatToMotionVariable(System.UInt32, System.Single)", hash, rate)
end

function ReloadSpeed:install()
    if self.hooked or not self.context.config:get("weapon-mod-reload-speed", false) then return end
    local game = self.context.game
    game:hook("app.PlayerMotionController", "update()", function(args)
        local storage = thread.get_hook_storage()
        storage.biorand_reload_controller = nil
        if self.context.config:get("weapon-mod-reload-speed", false) then
            storage.biorand_reload_controller = game:object(args[2])
        end
    end, function(retval)
        local storage = thread.get_hook_storage()
        local controller = storage.biorand_reload_controller
        storage.biorand_reload_controller = nil
        if controller ~= nil then
            self:apply(controller)
        end
        return retval
    end)
    self.hooked = true
end

function ReloadSpeed:on_config_changed()
    self:install()
end

return ReloadSpeed
