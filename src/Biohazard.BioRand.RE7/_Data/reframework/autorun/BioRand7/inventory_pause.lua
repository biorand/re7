local InventoryPause = {}
InventoryPause.__index = InventoryPause

-- RE7's pause requests are a UInt32 bitmask, not reference counted. Reserve an
-- unused bit so closing inventory cannot release a menu, save, or cutscene pause.
-- Verified against the RT TDB and native requestPause/requestReleasePause code;
-- vanilla PauseRequestType occupies bits 0 through 18. See docs/reframework-lua.md.
local INVENTORY_PAUSE = 0x40000000

function InventoryPause.new(context)
    return setmetatable({ context = context, active = false }, InventoryPause)
end

function InventoryPause:set_paused(paused)
    local game = self.context.game
    local manager = game:singleton("app.GameManager")
    if manager == nil then
        self.active = false
        return
    end

    local requested = (manager:get_field("CurrentPause") & INVENTORY_PAUSE) ~= 0
    if requested ~= paused then
        local method = paused and "requestPause(app.GameManager.PauseRequestType)"
            or "requestReleasePause(app.GameManager.PauseRequestType)"
        game:method("app.GameManager", method):call(nil, INVENTORY_PAUSE)
    end
    self.active = paused
end

function InventoryPause:update()
    if not self.context.config:get("pause-inventory", false) then
        if self.active then self:reset() end
        return
    end

    local menu = self.context.game:singleton("app.MenuManager")
    -- This native query excludes closed/closing menus and scripted item selection
    -- or item-box modes. Wait for control readiness so a reload/heal animation
    -- cannot be frozen while the inventory is waiting for it to finish.
    local paused = menu ~= nil and menu:call("isOpenInventoryMenu()")
        and menu:call("isEnableControlInventoryScreen()")
    self:set_paused(paused)
end

function InventoryPause:reset()
    if self.active then self:set_paused(false) end
end

function InventoryPause:on_config_changed()
    if not self.context.config:get("pause-inventory", false) then self:reset() end
end

function InventoryPause:install()
    -- Polled by the application callback, which keeps running during a pause.
    -- No menu instances or native hooks survive a save load or ScriptRunner reset.
end

return InventoryPause
