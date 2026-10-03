return function()
    local Crafting = require("BioRand7/crafting")
    local function object(fields, methods)
        return {
            get_field = function(_, name) return fields[name] end,
            call = function(_, name, ...) assert(methods and methods[name], name); return methods[name](...) end,
            add_ref = function(self) self.references = (self.references or 0) + 1; return self end,
        }
    end
    local function cache(value)
        return object({}, { get_Element = function() return value end })
    end
    local hooks, draw_frame = {}
    local game = {
        object = function(_, obj) return obj end,
        hook = function(_, name, signature, before) hooks[name .. ":" .. signature] = before end,
    }
    re = { on_frame = function(callback) draw_frame = callback end }
    local crafting = Crafting.new({ game = game })
    crafting:install()
    local info = "app.InventoryMenu.DictionaryCombineUIController.CombineInfoController:"
    local open = hooks[info .. "open(app.ItemCombineData.Data, System.Func`2<System.String,System.Int32>)"]

    local display = { x = 1920, y = 1080 }
    local scale, offset_x, offset_y, root_visible = 1, 0, 0, true
    local gui = object({}, {
        ["getScreenPos(via.vec3)"] = function(p) return { x = p.x * scale + offset_x, y = p.y * scale + offset_y } end,
        ["getScreenSize(via.Size)"] = function(s) return { w = s.w * scale, h = s.h * scale } end,
    })
    local panels = { { x = 1510, y = 625, visible = true }, { x = 1730, y = 625, visible = true } }
    local icons = {}
    for i, p in ipairs(panels) do
        local panel = object({}, {
            get_ActualVisible = function() return p.visible end,
            get_GlobalPosition = function() return p end,
            get_CaptureSize = function() return { w = 100, h = 100 } end,
        })
        icons[i] = object({ _Icon = object({ _ItemIconElement = cache(panel) }) })
    end
    local fields = {
        _GUIController = object({}, { get_Component = function() return gui end }),
        _RootControl = cache(object({}, { get_ActualVisible = function() return root_visible end })),
        _Icons = { get_size = function() return #icons end, get_element = function(_, i) return icons[i + 1] end },
    }
    local controller = object(fields)
    local recipe = { SrcItemID1 = "Herb", SrcItemNum1 = 3, SrcItemID2 = "ChemicalS", SrcItemNum2 = 12,
        ResultItemID = "RemedyM", ResultItemNum = 4 }
    local labels, backgrounds, fonts_loaded, font, font_depth = {}, {}, 0, nil, 0
    imgui = {
        get_display_size = function() return display end,
        load_font = function(_, size) fonts_loaded = fonts_loaded + 1; return size end,
        push_font = function(value) font = value; font_depth = font_depth + 1 end,
        pop_font = function() font_depth = font_depth - 1 end,
        calc_text_size = function(text) return { x = #text * font / 2, y = font } end,
    }
    draw = {
        filled_rect = function(x, y, w, h) backgrounds[#backgrounds + 1] = { x = x, y = y, w = w, h = h } end,
        text = function(text, x, y) labels[#labels + 1] = { text = text, x = x, y = y, font = font } end,
    }
    local function frame()
        labels, backgrounds = {}, {}
        draw_frame()
        assert(font_depth == 0, "Drawing must restore the font stack")
    end
    local function select_recipe(target)
        open({ nil, target or controller, object(recipe) })
    end
    select_recipe()
    frame()
    assert(#labels == 2 and labels[1].text == "3" and labels[2].text == "12", "Required counts must follow ingredient order")
    assert(labels[1].x >= 1470 and labels[1].x < 1510 and labels[1].y > 625 and labels[1].y + labels[1].font <= 665)
    assert(labels[2].x >= 1690 and labels[2].x < 1730, "Each badge must be inside its own icon")
    for i, label in ipairs(labels) do
        local bg = backgrounds[i]
        assert(bg.x < label.x and bg.y < label.y and bg.w > #label.text * label.font / 2 and bg.h > label.font,
            "The contrast background must fit multi-digit counts")
    end
    for _ = 1, 60 do select_recipe(); frame() end
    assert(controller.references == 1 and fonts_loaded == 1, "Repeated selections must reuse references and fonts")

    recipe.SrcItemNum1 = 7
    select_recipe()
    frame()
    assert(labels[1].text == "7" and labels[2].text == "12", "Alternate recipes for one output must update")
    recipe.SrcItemID2 = "Herb"
    select_recipe()
    frame()
    assert(labels[1].text == "7" and labels[2].text == "12", "Identical ingredients still need separate quantities")

    local first_x, first_y = labels[1].x, labels[1].y
    offset_x, offset_y = -100, 40
    frame()
    assert(labels[1].x == first_x - 100 and labels[1].y == first_y + 40, "Badges must follow native menu movement")
    scale, offset_x, offset_y = 2 / 3, 0, 0
    display = { x = 1280, y = 720 }
    frame()
    assert(labels[1].font == 16 and labels[1].x >= 980 and labels[1].x < 1007 and fonts_loaded == 2,
        "Coordinates and text must scale with the native GUI")
    panels[1].visible = false
    frame()
    assert(#labels == 1 and labels[1].text == "12", "Hidden ingredient icons must not leave floating counts")
    panels[1].visible = true
    root_visible = false
    frame()
    assert(#labels == 0, "A hidden preview must suppress both badges")
    root_visible = true
    display.x = 0
    frame()
    assert(#labels == 0, "Minimized windows must not draw")
    display.x = 1280

    for _, reset in ipairs({
        hooks[info .. "close()"],
        hooks["app.InventoryMenu:changeStepClose(app.InventoryMenu.StepType)"],
        function() crafting:reset("load") end,
    }) do
        select_recipe()
        reset()
        frame()
        assert(#labels == 0 and crafting.controller == nil, "Closing or loading must release the preview")
    end
    local replacement = object(fields)
    select_recipe(replacement)
    frame()
    assert(#labels == 2 and replacement.references == 1, "A recreated menu must bind to the new controller")
    open({ nil, controller, nil })
    frame()
    assert(#labels == 0)
    recipe.SrcItemNum2 = 0
    select_recipe()
    frame()
    assert(#labels == 0, "Invalid recipes must clear old badges")
    recipe.SrcItemNum2 = 1
    fields._Icons = nil
    select_recipe()
    frame()
    assert(#labels == 0, "An uninitialized preview must not draw stale quantities")
end
