local UI = {}
UI.__index = UI

local DIFFICULTIES = { [0] = "Easy", [1] = "Normal", [2] = "Hard" }
-- No decoration, movement, saved settings, focus, navigation, or input.
local OVERLAY_WINDOW_FLAGS = 791407
local WINDOW_BACKGROUND_COLOR = 2

local function count(values)
    local result = 0
    for _ in pairs(values) do result = result + 1 end
    return result
end

local function enabled(value)
    return value and "enabled" or "disabled"
end

function UI.new(context)
    return setmetatable({ context = context }, UI)
end

function UI:label(name, value)
    imgui.text(('%s: %s'):format(name, tostring(value)))
end

function UI:runtime_info()
    local game = self.context.game
    local player = game:player()
    if player == nil then
        self:label("Player", "unavailable")
        return
    end
    local transform = player:call("get_Transform")
    self:label("Player", player:call("get_Name"))
    self:label("Chapter", game:chapter())
    self:label("Difficulty", DIFFICULTIES[game:difficulty()] or "unknown")
    if transform ~= nil then
        local position = transform:call("get_Position")
        self:label("Position", ("%.3f, %.3f, %.3f"):format(position.x, position.y, position.z))
    end
end

function UI:feature_info()
    local config = self.context.config
    local drops = self.context.features.enemy_drops
    local mia = self.context.features.static_mia
    local em3300 = self.context.features.em3300_explosions
    local events = self.context.features.random_events
    self:label("Key item locations", enabled(config:get("random-key-item-locations", false)))
    self:label("Static item locations", enabled(config:get("random-items", true)))
    self:label("Additional items", enabled(config:get("additional-items", false)))
    self:label("Enemy drops", ("%s (%d dropped, %d tracked)"):format(
        enabled(config:get("random-enemy-drops", true)), count(drops.dropped), count(drops.generations)))
    self:label("Static Mia memory", ("%d keys, %d suppressed"):format(count(mia.killed), count(mia.suppressed)))
    self:label("Em3300 explosions", ("%s (%d tracked)"):format(enabled(em3300:enabled()), count(em3300.states)))
    self:label("Random events", ("%s (%s)"):format(enabled(config:get("random-events", false)), events:state_label()))
    self:label("Madhouse saves", enabled(self.context.features.madhouse_saves:enabled()))
    self:label("Reload speed", enabled(config:get("weapon-mod-reload-speed", false)))
    self:label("Ethan inventory", config:get("random-starting-inventory-size-ethan", "12"))
    self:label("Mia inventory", config:get("random-starting-inventory-size-mia", "12"))
end

function UI:debug_tools()
    if not imgui.tree_node("Debug tools") then return end

    local changed, verbose = imgui.checkbox("Verbose logging", self.context.log.verbose)
    if changed then self.context.log.verbose = verbose end
    if imgui.button("Reload config") then
        self.context.config:reload()
        self.context.log.verbose = self.context.config:get(
            "verbose-reframework-plugin-logging", self.context.log.verbose)
        self.context.log:info("Configuration reloaded from UI")
    end
    imgui.same_line()
    if imgui.button("Log snapshot") then
        local game = self.context.game
        local player = game:player()
        local player_name = player == nil and "unavailable" or player:call("get_Name")
        self.context.log:info(("Snapshot: seed=%s, player=%s, chapter=%s, difficulty=%s"):format(
            tostring(self.context.config:get("biorand-seed", "not present")),
            player_name,
            tostring(game:chapter()),
            tostring(game:difficulty())))
    end
    imgui.same_line()
    if imgui.button("Clear enemy drop state") then self.context.features.enemy_drops:reset() end
    if imgui.button("Clear static Mia state") then self.context.features.static_mia:reset() end
    imgui.same_line()
    if imgui.button("Clear Em3300 state") then self.context.features.em3300_explosions:reset() end
    imgui.same_line()
    if imgui.button("Clear random event state") then self.context.features.random_events:clear() end

    local events = self.context.features.random_events
    if imgui.tree_node("Random event effects") then
        self:label("State", events:state_label())
        if imgui.button("Random player status") then events:start("player_status", true) end
        for index, delta in ipairs(events.status_deltas) do
            if imgui.button(delta.label .. "##random-status-" .. index) then
                events:start("player_status", true, delta)
            end
        end
        imgui.separator()
        for _, kind in ipairs(events.kinds) do
            if kind ~= "player_status" and imgui.button(events.display_names[kind] .. "##random-event-" .. kind) then
                events:start(kind, true)
            end
        end
        imgui.tree_pop()
    end
    imgui.tree_pop()
end

function UI:config_values()
    if not imgui.tree_node("Config values") then return end
    for _, entry in ipairs(self.context.config:entries()) do
        local value = type(entry.value) == "table" and json.dump_string(entry.value) or tostring(entry.value)
        if #value > 160 then value = value:sub(1, 157) .. "..." end
        imgui.text(entry.key .. ": " .. value)
    end
    imgui.tree_pop()
end

function UI:draw_settings()
    if not imgui.tree_node("BioRand 7") then return end
    self:label("Seed", self.context.config:get("biorand-seed", "not present"))
    self:label("Config entries", #self.context.config:entries())
    imgui.separator()
    self:runtime_info()
    imgui.separator()
    self:feature_info()
    imgui.separator()
    self:debug_tools()
    imgui.separator()
    self:config_values()
    imgui.tree_pop()
end

function UI:draw_overlay()
    local label = self.context.features.random_events:overlay_label()
    if label == nil then return end
    imgui.set_next_window_pos(Vector2f.new(32, 72), 1, Vector2f.new(0, 0))
    imgui.push_style_color(WINDOW_BACKGROUND_COLOR, Vector4f.new(0.06, 0.06, 0.06, 0.45))
    if imgui.begin_window("BioRand random event##biorand-random-event-overlay", nil, OVERLAY_WINDOW_FLAGS) then
        imgui.text(label)
    end
    imgui.end_window()
    imgui.pop_style_color(1)
end

function UI:install()
    re.on_draw_ui(function() self:draw_settings() end)
    re.on_frame(function() self:draw_overlay() end)
end

return UI
