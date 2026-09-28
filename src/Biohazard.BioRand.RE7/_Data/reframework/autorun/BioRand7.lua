local Context = require("BioRand7/context")
local Em3300Explosions = require("BioRand7/em3300_explosions")
local Em8000KneeDown = require("BioRand7/em8000_knee_down")
local EnemyDrops = require("BioRand7/enemy_drops")
local Inventory = require("BioRand7/inventory")
local MadhouseSaves = require("BioRand7/madhouse_saves")
local RandomEvents = require("BioRand7/random_events")
local ReloadSpeed = require("BioRand7/reload_speed")
local StaticMia = require("BioRand7/static_mia")
local UI = require("BioRand7/ui")

local context = Context.new()

context:add("inventory", Inventory.new(context))
context:add("madhouse_saves", MadhouseSaves.new(context))
context:add("reload_speed", ReloadSpeed.new(context))
context:add("em8000_knee_down", Em8000KneeDown.new(context))
context:add("static_mia", StaticMia.new(context))
context:add("enemy_drops", EnemyDrops.new(context))
context:add("em3300_explosions", Em3300Explosions.new(context))
context:add("random_events", RandomEvents.new(context))
context:add("ui", UI.new(context))
context:install_session_hooks()

local runtime_errors = {}

local function update_feature(name)
    local feature = context.features[name]
    local ok, error_message = xpcall(feature.update, debug.traceback, feature)
    if ok then
        runtime_errors[name] = nil
    elseif runtime_errors[name] ~= error_message then
        runtime_errors[name] = error_message
        context.log:error(("%s update failed: %s"):format(name, error_message))
    end
end

re.on_application_entry("UpdateBehavior", function()
    update_feature("em3300_explosions")
    update_feature("random_events")
end)

re.on_script_reset(function()
    context:reset()
end)

context.log:info(("Loaded with %d configuration entries"):format(#context.config:entries()))
