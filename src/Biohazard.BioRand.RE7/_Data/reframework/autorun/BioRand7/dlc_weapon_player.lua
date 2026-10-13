local Player = {}
Player.__index = Player

local SPIRIT_BLADE = 63
local HAND_AXE_BANK = 10
local KNIFE_BANK = 30
local CAMPAIGN_PLAYERS = { Pl0000 = true, Pl0000_Chapter1 = true, Pl2000 = true, Pl2100 = true, Pl3000 = true }

function Player.new(game, enabled, resource_root)
    return setmetatable({ game = game, enabled = enabled, resource_root = resource_root }, Player)
end

function Player:enabled_for(owner)
    if not self.enabled() or not owner or owner ~= self.game:player() then return false end
    local name = owner:call("get_Name")
    if not CAMPAIGN_PLAYERS[name] then return false end
    local manager = self.game:singleton("app.ItemManager")
    local data = manager and manager:call("findItemData", "CH9_WP002")
    local prefab = data and data:get_field("ItemPrefab")
    local path = prefab and prefab:call("get_Path")
    return path ~= nil and path:lower() == (self.resource_root .. "/CH9_WP002/Item.pfb"):lower()
end

function Player:bank_for(controller, weapon_id)
    if weapon_id == SPIRIT_BLADE and self:enabled_for(controller:call("get_GameObject")) then
        local motion = controller:get_field("Motion")
        if not motion then return nil end
        for _, bank in ipairs({ HAND_AXE_BANK, KNIFE_BANK }) do
            local found = motion:call("findMotionBank(System.UInt32, System.UInt32)", 0, bank)
            -- RE7 returns its default bank when the requested type is absent.
            if found and found:call("get_BankID") == 0 and found:call("get_BankType") == bank then return bank end
        end
    end
end

function Player:reset()
    self.window, self.pending_recovery = nil, nil
end

function Player:update()
    if not self.enabled() then self:reset(); return end
    local game = self.game
    local player = game:player()
    local equip = game:component(player, "app.EquipManager")
    local weapon = equip and equip:call("get_equipWeaponRight")
    if not weapon or weapon:get_field("WeaponID") ~= SPIRIT_BLADE or not self:enabled_for(player) then
        self:reset(); return
    end
    local object = weapon:call("get_GameObject")
    local player_id, weapon_id = game:address(player), game:address(object)
    local window = self.window
    if not window or window.player ~= player_id or window.weapon ~= weapon_id then
        self:reset()
        window = { player = player_id, weapon = weapon_id, healed = false }
        self.window = window
    end

    -- Only scalar identities and the recovery amount survive the native hook.
    local recovery = self.pending_recovery
    self.pending_recovery = nil
    if recovery and recovery.window == window then
        local damage = game:component(player, "app.PlayerDamageController")
        if damage and damage:call("get_health") > 0 then damage:call("recoveryHealth", recovery.amount) end
    end
    local collider = game:component(object, "via.physics.RequestSetCollider")
    if not weapon:get_field("IsAttacking") and collider and collider:call("get_NumRegisteredRequestSetIds") == 0 then
        window.healed = false
    end
end

function Player:begin_damage(controller, hit)
    local window = self.window
    if not self.enabled() or not window or window.healed or not hit then return nil end
    local attacker = hit:call("get_AttackGameObject")
    if not attacker or self.game:address(attacker) ~= window.weapon then return nil end
    local target = controller:call("get_GameObject")
    if self.game:component(target, "app.EnemyDamageController") ~= controller then return nil end
    local player = self.game:player()
    if not player or self.game:address(player) ~= window.player then return nil end
    local melee = self.game:component(player, "app.PlayerMelee")
    local health = controller:call("get_health")
    if not melee or health <= 0 then return nil end
    -- CH9ThrowWepParam.Wp1700RecoveryValueS/L; independent of randomized damage.
    return { window = window, controller = controller, health = health,
        amount = melee:call("get_isAimAttack") and 150.0 or 100.0 }
end

function Player:finish_damage(pending, record)
    if not pending or not record or not self.enabled() or self.window ~= pending.window
        or pending.window.healed then return end
    if record:get_field("AddedDamage") <= 0 or pending.controller:call("get_health") >= pending.health then return end
    pending.window.healed = true
    self.pending_recovery = { window = pending.window, amount = pending.amount }
end

function Player:install()
    if self.installed then return end
    self.installed = true
    local bank_key, recovery_key = {}, {}
    -- Install before equip: changing the bank after entering an empty ReadyStart
    -- animation does not restart that animation or finish the weapon-change task.
    self.game:hook("app.PlayerMotionController", "getBankType(app.WeaponID)", function(args)
        local storage = thread.get_hook_storage()
        storage[bank_key] = nil
        local id = sdk.to_int64(args[3])
        if id == SPIRIT_BLADE and self.enabled() then
            storage[bank_key] = self:bank_for(self.game:object(args[2]), id)
        end
    end, function(retval)
        local storage = thread.get_hook_storage()
        local bank = storage[bank_key]
        storage[bank_key] = nil
        return bank and sdk.to_ptr(bank) or retval
    end)
    self.game:hook("app.DamageController", "addDamageCore(app.Collision.HitController.DamageInfo, app.DamageController.DamageRecord.DamageType)", function(args)
        local storage = thread.get_hook_storage()
        storage[recovery_key] = nil
        if self.enabled() and self.window and not self.window.healed then
            storage[recovery_key] = self:begin_damage(self.game:object(args[2]), self.game:object(args[3]))
        end
    end, function(retval)
        local storage = thread.get_hook_storage()
        local pending = storage[recovery_key]
        storage[recovery_key] = nil
        if pending then self:finish_damage(pending, self.game:object(retval)) end
        return retval
    end)
end

return Player
