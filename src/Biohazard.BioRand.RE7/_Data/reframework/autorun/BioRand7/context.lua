local Config = require("BioRand7/config")
local Game = require("BioRand7/game")
local Logger = require("BioRand7/logger")

local Context = {}
Context.__index = Context

function Context.new()
    local config = Config.new()
    return setmetatable({
        config = config,
        game = Game.new(),
        log = Logger.new("BioRand7", config),
        features = {},
    }, Context)
end

function Context:add(name, feature)
    self.features[name] = feature
    feature:install()
    self.log:info(name .. " loaded")
    return feature
end

function Context:reset(reason)
    for _, feature in pairs(self.features) do
        if feature.reset ~= nil then
            feature:reset(reason)
        end
    end
end

function Context:install_session_hooks()
    -- These are game-session boundaries; folderLoad also runs during normal room streaming.
    self.game:hook("app.SaveDataManager", "newGameInit()", function() self:reset("new_game") end)
    self.game:hook("app.SaveDataManager", "loadLevelUsingLoadData()", function() self:reset("load") end)
end

function Context:reload_config()
    self.config:reload()
    self.log.verbose = self.config:get("verbose-reframework-plugin-logging", self.log.verbose)
    for _, feature in pairs(self.features) do
        if feature.on_config_changed ~= nil then feature:on_config_changed() end
    end
end

return Context
