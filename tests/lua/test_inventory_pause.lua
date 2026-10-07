return function()
    local InventoryPause = require("BioRand7/inventory_pause")
    local Context = require("BioRand7/context")
    local enabled, open, ready = false, false, true
    local reads, writes = 0, {}
    local inventory_bit = 0x40000000
    local game_manager, menu_manager

    local function manager(mask)
        return { mask = mask or 0, get_field = function(self, name)
            reads = reads + 1
            assert(name == "CurrentPause", "Pause state is a raw TDB field")
            return self.mask
        end }
    end
    local menu = { call = function(_, signature)
        reads = reads + 1
        if signature == "isOpenInventoryMenu()" then return open end
        assert(signature == "isEnableControlInventoryScreen()")
        return ready
    end }
    local hooks = {}
    local game = {
        singleton = function(_, name)
            reads = reads + 1
            if name == "app.GameManager" then return game_manager end
            assert(name == "app.MenuManager")
            return menu_manager
        end,
        method = function(_, name, signature)
            assert(name == "app.GameManager")
            local request = signature == "requestPause(app.GameManager.PauseRequestType)"
            assert(request or signature == "requestReleasePause(app.GameManager.PauseRequestType)")
            return { call = function(_, receiver, bit)
                assert(receiver == nil, "Pause request methods are static")
                assert(bit == inventory_bit and math.type(bit) == "integer")
                writes[#writes + 1] = request
                -- Native requests OR/AND the flag; they do not reference count it.
                if request then game_manager.mask = game_manager.mask | bit
                else game_manager.mask = game_manager.mask & ~bit end
            end }
        end,
        hook = function(_, name, signature, before)
            assert(name == "app.SaveDataManager", "Inventory pause needs no native hooks")
            hooks[signature] = before
        end,
    }
    local config = { get = function(_, key, default)
        assert(key == "pause-inventory" and default == false)
        if enabled == nil then return default end
        return enabled
    end }
    local context = setmetatable({ game = game, config = config, features = {} }, Context)
    local feature = InventoryPause.new(context)
    context.features.inventory_pause = feature
    context:install_session_hooks()
    feature:install()

    for _ = 1, 60 do feature:update() end
    assert(reads == 0 and #writes == 0, "Disabled profiles must not poll the engine")
    enabled = nil
    feature:update()
    assert(reads == 0, "Legacy profiles without the setting retain vanilla behavior")

    enabled = true
    feature:on_config_changed()
    feature:update() -- Title/loading screens can lack both singletons.
    assert(not feature.active and #writes == 0)
    game_manager, menu_manager = manager(), menu
    feature:update()
    assert(game_manager.mask == 0 and #writes == 0)

    open, ready = true, false
    feature:update()
    assert(game_manager.mask == 0, "Allow reload/heal animations to finish before pausing")
    ready = true
    feature:update()
    assert(game_manager.mask == inventory_bit and feature.active)
    local first_writes, first_reads = #writes, reads
    for _ = 1, 60 do feature:update() end
    assert(#writes == first_writes, "An open menu must not repeatedly request pause")
    assert(reads - first_reads == 60 * 5, "Polling must stay bounded without object scans")
    open = false
    feature:update()
    assert(game_manager.mask == 0 and not feature.active, "Closing resumes gameplay")
    local closed_writes = #writes
    feature:update()
    assert(#writes == closed_writes)

    -- Preserve other owners regardless of which pause starts or ends first.
    for _, native_pause in ipairs({ 2, 4, 8, 64, 128, 262144, 4 | 64 }) do
        game_manager.mask = native_pause
        open = true
        feature:update()
        assert(game_manager.mask == (native_pause | inventory_bit))
        open = false
        feature:update()
        assert(game_manager.mask == native_pause)

        game_manager.mask = 0
        open = true
        feature:update()
        game_manager.mask = game_manager.mask | native_pause
        feature:update()
        game_manager.mask = game_manager.mask & ~native_pause
        feature:update()
        assert(game_manager.mask == inventory_bit, "Closing another menu must retain inventory pause")
        open = false
        feature:update()
        assert(game_manager.mask == 0)
    end

    open = true
    feature:update()
    ready = false
    feature:update()
    assert(game_manager.mask == 0, "A new player action must not deadlock inventory control")
    ready = true
    feature:update()
    enabled = false
    feature:on_config_changed()
    assert(game_manager.mask == 0 and not feature.active, "Disabling releases an active pause immediately")
    local disabled_reads = reads
    feature:update()
    feature:on_config_changed()
    feature:reset()
    assert(reads == disabled_reads)
    enabled = true
    feature:on_config_changed()
    feature:update()
    assert(game_manager.mask == inventory_bit, "Enabling works with an already open inventory")
    enabled = false
    feature:update()
    assert(game_manager.mask == 0, "Update also cleans up a disabled option")
    enabled = true

    for _, reset in ipairs({ function() context:reset() end,
        hooks["newGameInit()"], hooks["loadLevelUsingLoadData()"] }) do
        game_manager.mask = 64
        feature:update()
        reset()
        assert(game_manager.mask == 64 and not feature.active, "Reset must release only BioRand's pause")
        reset() -- Repeated reset must be harmless.
    end

    game_manager.mask = 0
    feature:update()
    menu_manager = nil
    feature:update()
    assert(game_manager.mask == 0, "An unloaded menu manager must release the pause")
    menu_manager = menu
    feature:update()
    game_manager = nil
    feature:update()
    feature:reset()
    assert(not feature.active)
    game_manager = manager(4)
    feature:update()
    assert(game_manager.mask == (4 | inventory_bit), "A replacement game manager must acquire its own pause")
    game_manager.mask = 4 -- Native initialization can clear requests on the same instance.
    feature:update()
    assert(game_manager.mask == (4 | inventory_bit))
    open = false
    feature:update()
    assert(game_manager.mask == 4)
end
