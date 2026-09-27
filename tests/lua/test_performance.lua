return function()
    local Config = require("BioRand7/config")
    local Context = require("BioRand7/context")
    local RandomEvents = require("BioRand7/random_events")
    local UI = require("BioRand7/ui")
    local now, serialized, vectors = 0, 0, 0
    os.clock = function() return now end
    local values = { Zebra = false, apple = 1, Apple = 2, nested = {} }
    json = {
        load_file = function() return values end,
        dump_string = function()
            serialized = serialized + 1
            return "{}"
        end,
    }
    local config = Config.new()
    local entries = config:entries()
    assert(entries[1].key == "Apple" and entries[2].key == "apple" and entries[4].key == "Zebra")
    assert(config:get(entries[4].key, true) == false)
    assert(config:entries() == entries, "Sorted entries must be reused until config reload")

    local notifications = 0
    local context = setmetatable({ config = config, log = {}, features = {
        example = { on_config_changed = function() notifications = notifications + 1 end },
    } }, Context)
    local ui = UI.new(context)
    local lines = {}
    imgui = {
        tree_node = function() return true end,
        tree_pop = function() end,
        text = function(line) lines[#lines + 1] = line end,
        set_next_window_pos = function() end,
        push_style_color = function() end,
        pop_style_color = function() end,
        begin_window = function() return true end,
        end_window = function() end,
    }
    for _ = 1, 60 do ui:config_values() end
    assert(serialized == 1, "Do not reformat configuration tables every UI frame")
    values = { replacement = 42, ["verbose-reframework-plugin-logging"] = true }
    context:reload_config()
    assert(notifications == 1 and context.log.verbose)
    assert(config:entries() ~= entries and config:get(entries[2].key) == nil)
    lines = {}
    ui:config_values()
    assert(#lines == 2 and lines[1] == "replacement: 42", "Reload invalidates displayed values too")

    local runtime_reads = 0
    ui.read_runtime_info = function()
        runtime_reads = runtime_reads + 1
        return { { "Player", "test" } }
    end
    for frame = 0, 59 do
        now = frame / 60
        ui:runtime_info()
    end
    assert(runtime_reads == 4, "Runtime diagnostics should poll at 4 Hz, not render rate")

    local function vector(x, y, z, w)
        vectors = vectors + 1
        return { x = x, y = y, z = z, w = w }
    end
    Vector2f, Vector3f, Vector4f = { new = vector }, { new = vector }, { new = vector }
    context.features.random_events = { overlay_label = function() return "test event" end }
    for _ = 1, 60 do ui:draw_overlay() end
    assert(vectors == 3, "Overlay vectors must not be allocated every frame")

    local passive_lookups, passive_reads, passive_writes = 0, 0, 0
    local manager = { attack = 1 }
    function manager:get_field(field)
        assert(field == "AttackChangeRate", "Do not read unrelated passive fields")
        passive_reads = passive_reads + 1
        return self.attack
    end
    function manager:set_field(field, value)
        assert(field == "AttackChangeRate", "Do not write unrelated passive fields")
        passive_writes = passive_writes + 1
        self.attack = value
    end
    function manager:call(method)
        assert(method == "get_Valid")
        return true
    end
    local game = { address = function(_, value) return value end }
    local events = RandomEvents.new({ game = game })
    events.passive_manager = function()
        passive_lookups = passive_lookups + 1
        return manager
    end
    local delta = { attack = 0.5 }
    for frame = 0, 59 do
        now = frame / 60
        events:apply_passive(delta)
    end
    assert(passive_lookups == 4 and passive_reads == 1 and passive_writes == 1)
    assert(manager.attack == 1.5)
    local first = manager
    manager = setmetatable({ attack = 2 }, { __index = first })
    now = 1
    events:apply_passive(delta)
    assert(manager.attack == 2.5, "A replacement passive manager must receive the current effect")
    events:restore()
    assert(first.attack == 1 and manager.attack == 2, "Restore every affected passive manager")
    events:apply_passive(delta)
    assert(manager.attack == 2.5, "A new event must not inherit the previous discovery cooldown")
    events:restore()

    local scale_writes = 0
    local transform = { scale = { x = 1, y = 2, z = 3 } }
    function transform:call(method, value)
        if method == "get_LocalScale" then return self.scale end
        assert(method == "set_LocalScale")
        scale_writes = scale_writes + 1
        self.scale = value
    end
    local player = { call = function(_, method)
        if method == "get_Valid" then return true end
        assert(method == "get_Transform")
        return transform
    end }
    game.player = function() return player end
    local before = vectors
    for _ = 1, 60 do events:apply_scale({ scale = 2 }) end
    assert(vectors == before + 1 and scale_writes == 60, "Reuse the scale vector but retain per-frame writes")
    assert(transform.scale.x == 2 and transform.scale.y == 4 and transform.scale.z == 6)
    events:restore()
    assert(transform.scale.x == 1 and transform.scale.y == 2 and transform.scale.z == 3)
end
