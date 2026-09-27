local MadhouseSaves = {}
MadhouseSaves.__index = MadhouseSaves

local HARD_DIFFICULTY = 2

function MadhouseSaves.new(context)
    return setmetatable({ context = context, pending_menu = nil }, MadhouseSaves)
end

function MadhouseSaves:enabled()
    if not self.context.config:get("madhouse-normal-saves", true) then
        return false
    end
    return self.context.game:difficulty() == HARD_DIFFICULTY
end

function MadhouseSaves:bypass(manager)
    if self.pending_menu == nil or not manager:call("get_IsNowSaveHardSelectDispGUI") then
        return
    end

    local handle = self.pending_menu
    self.pending_menu = nil
    local inventory_menu = handle:get_field("_Menu")
    if inventory_menu == nil then return end
    inventory_menu:call("setSelectItemResult(System.Boolean, System.String)", false, "SaveTape")
    handle:call("requestClose")
end

function MadhouseSaves:install()
    local game = self.context.game
    game:hook("app.MenuManager", "openSelectItemMenu", nil, function(retval)
        if self:enabled() then
            self.pending_menu = game:object(retval)
        end
        return retval
    end)

    local function update(args)
        if self.pending_menu ~= nil and self:enabled() then
            self:bypass(game:object(args[2]))
        end
    end
    game:hook("app.SaveDataManager", "doUpdate()", update)
    game:hook("app.SaveDataManager", "doLateUpdate()", update)

    local function skip_tape(args)
        if self:enabled() then
            game:object(args[2]):set_field("IsTapeSub", false)
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
    end
    game:hook("app.SaveDataManager", "isHardModeSubTape()", skip_tape)
    game:hook("app.SaveDataManager", "isHardModeAddTape()", skip_tape)
end

function MadhouseSaves:reset()
    self.pending_menu = nil
end

return MadhouseSaves
