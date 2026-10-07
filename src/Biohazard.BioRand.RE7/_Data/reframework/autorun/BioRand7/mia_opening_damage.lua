local MiaOpeningDamage = {}
MiaOpeningDamage.__index = MiaOpeningDamage

local OPENING_GRAPPLES = {
    Chapter1Battle1_ThrowStairs = true,
    Chapter1Battle1_Mount = true,
    Chapter1Battle1_Finish = true,
}

function MiaOpeningDamage.new(context)
    return setmetatable({ context = context }, MiaOpeningDamage)
end

function MiaOpeningDamage:should_protect(controller)
    if not self.context.config:get("disable-mia-opening-damage", false) or controller == nil then return false end
    -- DamageController is shared with enemies. Only protect the player whose
    -- active grapple belongs to the opening encounter, never later Mia fights.
    if controller:get_type_definition():get_full_name() ~= "app.PlayerDamageController" then return false end
    local game = self.context.game
    local player = controller:call("get_GameObject")
    if not game:valid(player) then return false end
    local grapple = game:component(player, "app.PlayerGrappleEm2000")
    if grapple == nil or not grapple:call("get_isGrapple") then return false end
    local param = grapple:call("get_grappleParam")
    return param ~= nil and OPENING_GRAPPLES[param:call("get_name")] == true
end

function MiaOpeningDamage:controller(pointer)
    if not self.context.config:get("disable-mia-opening-damage", false) then return nil end
    -- Native hook arguments are raw pointers; conversion throws for non-objects.
    if pointer == nil or not sdk.is_managed_object(pointer) then return nil end
    return self.context.game:object(pointer)
end

function MiaOpeningDamage:install()
    if self.hooked or not self.context.config:get("disable-mia-opening-damage", false) then return end
    local game = self.context.game
    -- Keep native damage records, animations, and progression running. Only
    -- intercept health changes; do not skip whole grapple or damage actions.
    game:hook("app.DamageController", "adjustHealth(app.Collision.HitController.DamageInfo)", function(args)
        if self:should_protect(self:controller(args[2])) then return sdk.PreHookResult.SKIP_ORIGINAL end
    end)
    -- canSubHealth is a shared constant-return native stub. Hooking it also
    -- intercepts unrelated functions, some without a managed receiver. Instead
    -- zero the player's calculated damage while preserving damage records.
    game:hook("app.PlayerDamageController", "calcDamage(app.Collision.HitController.DamageInfo)", function(args)
        local protect = self:should_protect(self:controller(args[2]))
        thread.get_hook_storage().biorand_mia_zero_damage = protect
        if protect then return sdk.PreHookResult.SKIP_ORIGINAL end
    end, function(retval)
        local storage = thread.get_hook_storage()
        local protect = storage.biorand_mia_zero_damage
        storage.biorand_mia_zero_damage = nil
        if protect then return sdk.float_to_ptr(0) end
        return retval
    end)
    game:hook("app.DamageController", "setHealth(System.Single, System.Single)", function(args)
        local controller = self:controller(args[2])
        if not self:should_protect(controller) then return end
        local current = controller:call("get_health")
        -- Scripted assignments can bypass ordinary damage. Preserve healing and
        -- the max-health argument, but clamp a requested HP reduction.
        if sdk.to_float(args[3]) < current then args[3] = sdk.float_to_ptr(current) end
    end)
    self.hooked = true
end

function MiaOpeningDamage:on_config_changed()
    self:install()
end

return MiaOpeningDamage
