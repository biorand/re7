local Crafting = {}
Crafting.__index = Crafting

local INFO_TYPE = "app.InventoryMenu.DictionaryCombineUIController.CombineInfoController"

local function element(cache)
    return cache and cache:call("get_Element")
end

function Crafting.new(context)
    return setmetatable({ context = context, fonts = {} }, Crafting)
end

function Crafting:select(controller, recipe)
    if controller == nil or recipe == nil then self:reset(); return end
    local first, second = recipe:get_field("SrcItemNum1"), recipe:get_field("SrcItemNum2")
    if first == nil or second == nil or first < 1 or second < 1 then self:reset(); return end
    if self.controller ~= controller then
        -- Retain the managed controller while the preview is open. REFramework
        -- releases this reference when the Lua wrapper is collected after reset.
        self.controller = controller:add_ref()
    end
    self.counts = { ("%d"):format(first), ("%d"):format(second) }
end

function Crafting:draw_icon(gui, icon, count, display)
    local controller = icon and icon:get_field("_Icon")
    local panel = controller and element(controller:get_field("_ItemIconElement"))
    if panel == nil or not panel:call("get_ActualVisible") then return end
    local position = gui:call("getScreenPos(via.vec3)", panel:call("get_GlobalPosition"))
    local size = gui:call("getScreenSize(via.Size)", panel:call("get_CaptureSize"))
    if position == nil or size == nil or size.w <= 0 or size.h <= 0
        or position.x < 0 or position.x >= display.x or position.y < 0 or position.y >= display.y then return end

    local font_size = math.max(12, math.min(64, math.floor(size.h * 0.24 + 0.5)))
    if self.fonts[font_size] == nil then
        self.fonts[font_size] = imgui.load_font(nil, font_size)
    end
    imgui.push_font(self.fonts[font_size])
    local text_size = imgui.calc_text_size(count)
    local padding = math.max(1, math.floor(font_size / 12))
    -- The native icon is centered on GlobalPosition, with transparent margins
    -- inside its capture rectangle. Inset the badge into its lower-left corner.
    local x = math.floor(position.x - size.w * 0.34)
    local y = math.floor(position.y + size.h * 0.34 - text_size.y)
    draw.filled_rect(x - padding, y - padding, text_size.x + padding * 2, text_size.y + padding * 2, 0xB0000000)
    draw.text(count, x, y, 0xFFFFFFFF)
    imgui.pop_font()
end

function Crafting:draw()
    local controller = self.controller
    if controller == nil or self.counts == nil then return end
    local display = imgui.get_display_size()
    if display.x <= 0 or display.y <= 0 then return end
    local root = element(controller:get_field("_RootControl"))
    if root == nil or not root:call("get_ActualVisible") then return end
    local gui_controller = controller:get_field("_GUIController")
    local gui = gui_controller and gui_controller:call("get_Component")
    local icons = controller:get_field("_Icons")
    if gui == nil or icons == nil or icons:get_size() < 2 then return end
    for i = 1, 2 do
        self:draw_icon(gui, icons:get_element(i - 1), self.counts[i], display)
    end
end

function Crafting:reset()
    self.controller, self.counts = nil, nil
end

function Crafting:install()
    local game = self.context.game
    -- Read the recipe actually selected by the native menu, including alternate
    -- recipes for one output. Each quantity belongs to its corresponding icon,
    -- even when both ingredients have the same item ID.
    game:hook(INFO_TYPE, "open(app.ItemCombineData.Data, System.Func`2<System.String,System.Int32>)", function(args)
        self:select(game:object(args[2]), game:object(args[3]))
    end)
    game:hook(INFO_TYPE, "close()", function() self:reset() end)
    game:hook("app.InventoryMenu", "changeStepClose(app.InventoryMenu.StepType)", function() self:reset() end)
    re.on_frame(function() self:draw() end)
end

return Crafting
