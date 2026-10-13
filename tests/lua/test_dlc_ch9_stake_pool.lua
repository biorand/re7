return function()
    local Pool = require("BioRand7/dlc_ch9_pool")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local root, path = "BioRand/DlcWeapons", "BioRand/DlcWeapons/CH9_WP005/Item.pfb"
    local name, flow, valid, tutorial = "Pl0000", 3, true, nil
    local player = object({}, { get_Name = function() return name end })
    local manager_object, fields = {}, { InstallationType = 0 }
    local shell = object(fields, {})
    local list = object({ List = { object({ Behavior = shell }, {}) } }, {})
    local pool_fields = { LiquidBombList = list }
    local manager = object(pool_fields, {
        get_Valid = function() return valid end, get_GameObject = function() return manager_object end,
    })
    local active, current = player, manager
    local prefab = object({}, { get_Path = function() return path end })
    local data = object({ ItemPrefab = prefab }, {})
    local items = object({}, { findItemData = function(id) assert(id == "CH9_WP005"); return data end })
    local hooks, sound = {}, {}
    local game = {
        player = function() return active end, chapter = function() return flow end,
        valid = function(_, value) assert(value == manager_object); return valid end,
        singleton = function(_, kind)
            if kind == "app.CH9ShellManager" then return current end
            if kind == "app.CH9TutorialManager" then return tutorial end
            if kind == "app.ItemManager" then return items end
            error(kind)
        end,
        list = function(_, values) local i = 0; return function() i = i + 1; return values[i] end end,
        object = function(_, value) return value end,
        hook = function(_, kind, signature, before, after)
            local key = kind .. ":" .. signature
            assert(not hooks[key], "Hooks must be installed only once")
            hooks[key] = { before, after }
        end,
        method = function(_, kind, method)
            assert(kind == "app.Bomb" and method == "callSE", "Call the base method, not the CH9 virtual override")
            return { call = function(_, receiver, trigger)
                assert(receiver == shell); sound[#sound + 1] = trigger
            end }
        end,
    }
    sdk = { PreHookResult = { SKIP_ORIGINAL = "skip" }, to_int64 = function(v) return v end }
    local pool = Pool.new(game, root)
    pool.object, pool.manager, pool.player, pool.ready = manager_object, manager, player, true
    pool:install(); pool:install()
    local recover = hooks["app.CH9InstallationWp1900:onSuccessInteractCh9"]
    local audio = hooks["app.CH9InstallationWp1900:callSE"]
    local args = { nil, shell, 0x73ce5ae3 }
    assert(recover[1](args) == "skip" and recover[2](456) == 456)
    for installation = 0, 4 do
        fields.InstallationType = installation
        assert(audio[1](args) == "skip" and audio[2](123) == 123)
        assert(sound[#sound] == (installation == 2 and 0xe350a1fc or 0x73ce5ae3))
    end
    assert(#sound == 5)
    for _, trigger in ipairs({ 0, 0x6b1aa8da, 0xaf351d05, 0xe350a1fc }) do
        args[3] = trigger; assert(audio[1](args) == nil and #sound == 5)
    end
    args[3] = 0x73ce5ae3
    local function untouched()
        local before = #sound
        assert(recover[1](args) == nil and audio[1](args) == nil and #sound == before)
    end
    args[2] = {}; untouched(); args[2] = nil; untouched(); args[2] = shell
    current = {}; untouched(); current = manager
    tutorial = {}; untouched(); tutorial = nil
    active = {}; untouched(); active = nil; untouched(); active = player
    name = "Pl9000"; untouched(); name = "Pl0000"
    for chapter = 0, 13 do flow = chapter; assert(recover[1](args) == "skip") end
    for _, chapter in ipairs({ -1, 14, 18, 21 }) do flow = chapter; untouched() end
    flow = nil; untouched(); flow = 3
    path = "CH9/Prefab/Weapon/Stake/Item.pfb"; untouched()
    path = root .. "/CH9_WP005/Item.pfb.bak"; untouched()
    path = nil; untouched()
    path = (root .. "/CH9_WP005/Item.pfb"):upper(); assert(recover[1](args) == "skip")
    pool_fields.LiquidBombList = nil; untouched(); pool_fields.LiquidBombList = list
    valid = false; untouched(); valid = true
    pool.destroy_pending = true; untouched(); pool.destroy_pending = nil
    pool:reset(); untouched()
    pool.ready = true; untouched()
end
