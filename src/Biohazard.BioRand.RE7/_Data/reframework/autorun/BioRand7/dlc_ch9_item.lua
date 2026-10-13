local Item = {}
Item.__index = Item
local BANK = 9962
local PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }
local MOTION = "CH9/Animation/Player/pl9000/motlist/pl9000_LiquidBomb.motlist"
local REQUEST = "requestMotion(System.String, System.UInt32, System.Single, System.Single, app.PlayerMotionController.RequestPriority)"

-- Shares the throwable adapter's pool. Placement, stock, detonation and recovery remain native.
function Item.new(game, enabled, root, pool)
    return setmetatable({ game = game, enabled = enabled, root = root, pool = pool }, Item)
end

function Item:matches(player)
    if not player or player ~= self.game:player() or not PLAYERS[player:call("get_Name")] then return false end
    local flow = self.game:chapter()
    if not flow or flow < 0 or flow > 13 then return false end
    local manager = self.game:singleton("app.ItemManager")
    local data = manager and manager:call("findItemData", "CH9_WP005")
    local prefab = data and data:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    return path ~= nil and path:lower() == (self.root .. "/CH9_WP005/Item.pfb"):lower()
end

function Item:owns_banks()
    local s = self.session
    if not s or s.player ~= self.game:player() or #s.banks ~= 2 then return false end
    for _, entry in ipairs(s.banks) do
        local bank = s.motion:call("getDynamicMotionBank", entry.index)
        if bank ~= entry.bank or bank:call("get_BankID") ~= 0 or bank:call("get_BankType") ~= BANK then return false end
    end
    return true
end

function Item:scope(controller)
    local s = self.session
    return s ~= nil and self.enabled() and not self.error and not self.reset_pending
        and controller == s.controller and self:matches(s.player) and self:owns_banks()
end

function Item:prepare(player)
    if self.session and self.session.player == player then
        assert(self:owns_banks(), "CH9 Item banks changed ownership")
        return true
    end
    self.motion_info, self.weapon, self.pending = nil, nil, nil
    local controller = self.game:component(player, "app.PlayerMotionController")
    local motion = controller and controller:get_field("Motion")
    local manager = controller and controller:get_field("MotionManager")
    local inventory = self.game:component(player, "app.Inventory")
    if not motion or not manager or not inventory then return false end
    local fallback = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, 200)
    if not fallback or fallback:call("get_BankID") ~= 0 or fallback:call("get_BankType") ~= 200
        or not fallback:call("get_MotionList") then return false end
    local existing = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, BANK)
    assert(not existing or existing:call("get_BankType") ~= BANK, "CH9 Item bank type is already occupied")
    if not self.holder then
        local resource = assert(sdk.create_resource("via.motion.MotionListResource", MOTION)):add_ref()
        local ok, holder = pcall(function()
            return assert(resource:create_holder("via.motion.MotionListResourceHolder")):add_ref()
        end)
        resource:release()
        if not ok then error(holder) end
        assert(holder:call("get_ResourcePath"):lower() == MOTION:lower(), "Unexpected CH9 Item motion resource")
        self.holder = holder
    end
    local s = { player = player, controller = controller, motion = motion, manager = manager,
        inventory = inventory, banks = {} }
    self.session = s
    for _, holder in ipairs({ self.holder, fallback:call("get_MotionList") }) do
        local bank = sdk.create_instance("via.motion.DynamicMotionBank"):add_ref()
        bank:call("set_MotionList", holder)
        bank:call("set_OverwriteBankID", true); bank:call("set_BankID", 0)
        bank:call("set_OverwriteBankType", true); bank:call("set_BankType", BANK)
        local index = motion:call("getDynamicMotionBankCount")
        motion:call("setDynamicMotionBankCount", index + 1)
        motion:call("setDynamicMotionBank", index, bank)
        s.banks[#s.banks + 1] = { index = index, bank = bank }
    end
    return true
end

function Item:motion_ready()
    if self.session.ready then return true end
    if self.motion_info and not sdk.is_managed_object(self.motion_info) then self.motion_info = nil end
    self.motion_info = self.motion_info or sdk.create_instance("via.motion.MotionInfo"):add_ref()
    for _, clip in ipairs({ 2000, 2001, 2400 }) do
        if not self.session.motion:call("getMotionInfo(System.UInt32, System.Int32, System.UInt32, via.motion.MotionInfo)",
            0, BANK, clip, self.motion_info) or self.motion_info:call("get_MotionEndFrame") <= 0.0 then return false end
    end
    self.session.ready = true
    return true
end

function Item:reset()
    self.reset_pending = true
    self.motion_info, self.weapon, self.pending, self.waiting = nil, nil, nil, nil
    if self.session then self.session.ready = nil end
end

function Item:controllable()
    local s = self.session
    local gm = self.game:singleton("app.GameManager")
    if not s or not gm or gm:call("get_IsPause") or gm:call("get_IsSceneLoading") then return false end
    local owner = s.manager:get_field("OwnerTask")
    if not owner or s.manager:get_field("CurrentTask") ~= owner then return false end
    local damage = self.game:component(s.player, "app.PlayerDamageController")
    if not damage or damage:call("get_health") <= 0 then return false end
    local menu = self.game:singleton("app.MenuManager")
    return not menu or not menu:call("isOpenInventoryMenu")
end

function Item:equipped(player)
    local equip = self.game:component(player, "app.EquipManager")
    local weapon = equip and equip:call("get_equipWeaponRight")
    if weapon and weapon:call("get_Valid") and weapon:get_field("WeaponID") == 66 then return weapon end
end

function Item:update()
    local player = self.game:player()
    if self.reset_pending or not self.enabled() or self.error or not self:matches(player) then
        self.weapon, self.pending, self.waiting, self.motion_info = nil, nil, nil, nil
        self.reset_pending = nil
        return
    end
    local gm = self.game:singleton("app.GameManager")
    if not gm or gm:call("get_IsPause") or gm:call("get_IsSceneLoading") then
        self.waiting, self.pending, self.weapon = nil, nil, nil
        return
    end
    if not self:prepare(player) then
        self.waiting = self.waiting or os.clock()
        assert(os.clock() - self.waiting < 10.0, "CH9 Item player readiness timed out")
        return
    end
    self.waiting = nil
    if not self:controllable() then self.weapon, self.pending = nil, nil; return end
    local weapon = self:equipped(player)
    if not weapon then self.weapon, self.pending = nil, nil; return end
    assert(weapon:get_type_definition():get_full_name() == "app.CH9Weapon1900", "Unexpected CH9 Item weapon component")
    local inventory = weapon:get_field("Inventory")
    assert(not inventory or inventory == self.session.inventory, "Unexpected CH9 Item inventory owner")
    if self.weapon ~= weapon then
        self.weapon, self.pending = weapon, { since = os.clock() }
        self.session.controller:call("updateTargetBankType")
    end
    if self.pending then
        assert(os.clock() - self.pending.since < 10.0, "CH9 Item readiness timed out")
        if not inventory or not self:motion_ready() then return end
        self.pending = nil
        local s = self.session
        if s.manager:call("getCurrentMotionFsmStateName", 1, false) == "Item.ReadyStart"
            and s.motion:call("getLayer", 1):call("get_EndFrame") <= 0.0 then
            s.controller:call("updateTargetBankType")
            s.controller:call(REQUEST, "Item.ReadyIdle", 1, 0.0, 4.0, 0)
        end
    end
end

function Item:block_use(receiver)
    local player = self.game:player()
    if not receiver or not self:matches(player) or receiver ~= self.game:component(player, "app.PlayerItem") then return false end
    local weapon = receiver:get_field("WeaponItem")
    if not weapon or not weapon:call("get_Valid") or weapon:get_field("WeaponID") ~= 66 then return false end
    local s = self.session
    return not s or not self:scope(s.controller) or not s.ready or not self:controllable()
        or self:equipped(player) ~= weapon or weapon:get_field("Inventory") ~= s.inventory
        or not self.pool.ready or not self.pool:owned() or self.pool.player ~= player
        or self.pool.reset_pending or self.pool.destroy_pending
end

function Item:install()
    if self.installed then return end
    self.installed = true
    local bank_key = {}
    self.game:hook("app.PlayerMotionController", "getBankType(app.WeaponID)", function(args)
        thread.get_hook_storage()[bank_key] = sdk.to_int64(args[3]) == 66 and self:scope(self.game:object(args[2]))
    end, function(ret)
        local storage = thread.get_hook_storage(); local apply = storage[bank_key]; storage[bank_key] = nil
        return apply and sdk.to_ptr(BANK) or ret
    end)
    for _, method in ipairs({ "tryUse", "use" }) do
        local key = {}
        self.game:hook("app.PlayerItem", method, function(args)
            local blocked = self:block_use(self.game:object(args[2]))
            thread.get_hook_storage()[key] = blocked
            if blocked then return sdk.PreHookResult.SKIP_ORIGINAL end
        end, function(ret)
            local storage = thread.get_hook_storage(); local blocked = storage[key]; storage[key] = nil
            return blocked and sdk.to_ptr(0) or ret
        end)
    end
    self.game:hook("app.PlayerMotionController", "updateLArmMotion()", function(args)
        local controller = self.game:object(args[2])
        if not self:scope(controller) or controller:get_field("CurrentWeaponID") ~= 66 then return end
        local work = controller:get_field("MotionWorks"):get_element(1)
        local name = work:get_field("StateName")
        if not name or name:sub(1, 5) ~= "Item." then return end
        if work:get_field("IsSet") then
            controller:call("setMotionWorkCore", name, work:get_field("StartFrame"), work:get_field("InterpolationFrame"),
                work:get_field("InterpolationMode"), work:get_field("InterpolationCurve"), work:get_field("Priority"), 2)
            local left = controller:get_field("MotionWorks"):get_element(2)
            if left:get_field("IsSet") then
                controller:get_field("MotionFsm"):call("setCurrentStateFullName(System.String, System.UInt32, System.Single, System.Single, via.motion.InterpolationMode, via.motion.InterpolationCurve)",
                    left:get_field("StateName"), 2, left:get_field("StartFrame"), left:get_field("InterpolationFrame"), left:get_field("InterpolationMode"), left:get_field("InterpolationCurve"))
            end
        end
        controller:call("set_isBothHands", true)
        return sdk.PreHookResult.SKIP_ORIGINAL
    end, function(ret) return ret end)
end

return Item
