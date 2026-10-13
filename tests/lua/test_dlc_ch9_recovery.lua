return function()
    local Pool = require("BioRand7/dlc_ch9_pool")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local root = "BioRand/Research/CH9Projectiles"
    local name, flow, valid = "Pl0000", 3, true
    local path = root .. "/CH9_WP004/Item.pfb"
    local weapon_fields, item_fields = { WeaponID = 65 }, { ItemDataID = "CH9_WP004" }
    local interact, inventory, weapon_object, manager_object = {}, {}, {}, {}
    local player = object({}, { get_Name = function() return name end })
    local weapon, item = object(weapon_fields, {}), object(item_fields, {})
    local shell = object({ Interact = interact }, {})
    local elements = { object({ Behavior = shell }, {}) }
    local manager = object({ ThrowingWp1800PrefabList = object({ List = elements }, {}) }, {
        get_Valid = function() return valid end, get_GameObject = function() return manager_object end,
    })
    local active, current, tutorial = player, manager, nil
    local prefab = object({}, { get_Path = function() return path end })
    local data = object({ ItemPrefab = prefab }, {})
    local items = object({}, { findItemData = function(id) assert(id == "CH9_WP004"); return data end })
    local pre, post, installs = nil, nil, 0
    local game = {
        player = function() return active end, chapter = function() return flow end,
        valid = function(_, go) assert(go == manager_object); return valid end,
        singleton = function(_, kind)
            if kind == "app.CH9ShellManager" then return current end
            if kind == "app.CH9TutorialManager" then return tutorial end
            if kind == "app.ItemManager" then return items end
            error(kind)
        end,
        component = function(_, go, kind)
            if go == player and kind == "app.Inventory" then return inventory end
            if go == weapon_object and kind == "app.Weapon" then return weapon end
            if go == weapon_object and kind == "app.Item" then return item end
            error(kind)
        end,
        list = function(_, list) local i = 0; return function() i = i + 1; return list[i] end end,
        object = function(_, value) return value end,
        hook = function(_, kind, signature, before, after)
            if kind == "app.CH9InstallationWp1900" then return end
            assert(kind == "app.CH9InteractWeapon" and signature == "equipWeapon(app.Inventory, app.EquipManager, via.GameObject, System.Boolean)")
            pre, post, installs = before, after, installs + 1
        end,
    }
    sdk = { PreHookResult = { SKIP_ORIGINAL = "skip" } }
    local pool = Pool.new(game, root)
    pool.object, pool.manager, pool.player, pool.ready = manager_object, manager, player, true
    pool:install(); pool:install(); assert(installs == 1)
    local args = { nil, interact, inventory, {}, weapon_object, false }
    assert(pre(args) == "skip" and post(123) == 123)
    assert(not pool:skip_spear_tutorial(nil, inventory, weapon_object))
    assert(not pool:skip_spear_tutorial({}, inventory, weapon_object), "Do not bypass unrelated CH9 interactions")
    assert(not pool:skip_spear_tutorial(interact, {}, weapon_object))
    assert(not pool:skip_spear_tutorial(interact, inventory, nil))
    current = {}; assert(pre(args) == nil); current = manager
    tutorial = {}; assert(pre(args) == nil); tutorial = nil
    active = {}; assert(pre(args) == nil); active = player
    active = nil; assert(pre(args) == nil); active = player
    name = "Pl9000"; assert(pre(args) == nil); name = "Pl0000"
    for chapter = 0, 13 do flow = chapter; assert(pre(args) == "skip") end
    for _, chapter in ipairs({ -1, 14, 18, 21 }) do flow = chapter; assert(pre(args) == nil) end
    flow = nil; assert(pre(args) == nil); flow = 3
    weapon_fields.WeaponID = 64; assert(pre(args) == nil); weapon_fields.WeaponID = 65
    item_fields.ItemDataID = "Handgun_G17"; assert(pre(args) == nil); item_fields.ItemDataID = "CH9_WP004"
    path = "CH9/Prefab/Weapon/Harpoon/Item.pfb"; assert(pre(args) == nil)
    path = root .. "/CH9_WP004/Item.pfb.bak"; assert(pre(args) == nil)
    path = (root .. "/CH9_WP004/Item.pfb"):upper(); assert(pre(args) == "skip")
    path = nil; assert(pre(args) == nil); path = root .. "/CH9_WP004/Item.pfb"
    valid = false; assert(pre(args) == nil); valid = true
    pool.destroy_pending = true; assert(pre(args) == nil); pool.destroy_pending = nil
    pool:reset(); assert(pre(args) == nil)
    pool.ready = true; assert(pre(args) == nil, "Pending save/load invalidation must also disarm recovery")
end
