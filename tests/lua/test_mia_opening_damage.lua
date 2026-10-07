return function()
    local MiaOpeningDamage = require("BioRand7/mia_opening_damage")
    local hooks, storage, hook_count, reads = {}, {}, 0, 0
    local enabled = false
    local state = {
        type_name = "app.PlayerDamageController", valid = true, active = true,
        name = "Chapter1Battle1_Finish", health = 1000,
    }
    local function read() reads = reads + 1 end
    local param = { call = function(_, method)
        read()
        assert(method == "get_name")
        return state.name
    end }
    local grapple = { call = function(_, method)
        read()
        if method == "get_isGrapple" then return state.active end
        assert(method == "get_grappleParam")
        if not state.missing_param then return param end
    end }
    local player = {}
    local controller = {
        get_type_definition = function()
            read()
            return { get_full_name = function() return state.type_name end }
        end,
        call = function(_, method)
            read()
            if method == "get_health" then return state.health end
            assert(method == "get_GameObject")
            if not state.missing_player then return player end
        end,
    }
    local game = {
        object = function(_, value)
            assert(value == controller, "Non-managed hook arguments must not reach object conversion")
            return value
        end,
        valid = function(_, value) read(); return value ~= nil and state.valid end,
        component = function(_, value, name)
            read()
            assert(value == player and name == "app.PlayerGrappleEm2000")
            if not state.missing_grapple then return grapple end
        end,
        hook = function(_, type_name, signature, before, after)
            assert(hooks[signature] == nil)
            assert(signature ~= "canSubHealth()", "Never hook the shared constant-return native stub")
            assert(type_name == (signature:match("^calcDamage") and "app.PlayerDamageController" or "app.DamageController"))
            hook_count = hook_count + 1
            hooks[signature] = { before = before, after = after }
        end,
    }
    -- Distinguish pointer conversions from Lua numbers to catch float/int ABI mistakes.
    sdk = {
        PreHookResult = { SKIP_ORIGINAL = "skip" },
        is_managed_object = function(value) read(); return value == controller end,
        float_to_ptr = function(value) return { float = value } end,
        to_float = function(value) return assert(value.float) end,
    }
    thread = { get_hook_storage = function() return storage end }
    local feature = MiaOpeningDamage.new({ game = game, config = {
        get = function(_, key, default)
            assert(key == "disable-mia-opening-damage" and default == false)
            return enabled
        end,
    } })

    feature:install()
    assert(hook_count == 0, "Disabled profiles must not install damage hooks")
    enabled = true
    feature:on_config_changed()
    feature:install()
    feature:on_config_changed()
    assert(hook_count == 3, "Reloading configuration must not duplicate hooks")
    local adjust = hooks["adjustHealth(app.Collision.HitController.DamageInfo)"]
    local calculate = hooks["calcDamage(app.Collision.HitController.DamageInfo)"]
    local assign = hooks["setHealth(System.Single, System.Single)"]

    local function assert_protected()
        assert(adjust.before({ nil, controller }) == "skip")
        assert(calculate.before({ nil, controller }) == "skip")
        assert(calculate.after({ uninitialized = true }).float == 0,
            "A skipped damage calculation must return a native float zero")
        assert(next(storage) == nil, "Hook state must be released after each call")
        local max_health = sdk.float_to_ptr(1200)
        local args = { nil, controller, sdk.float_to_ptr(999), max_health }
        assert(assign.before(args) == nil, "Keep the setter running for its other effects")
        assert(args[3].float == 1000 and args[4] == max_health,
            "Block scripted health loss without changing maximum health")
    end
    for _, name in ipairs({ "Chapter1Battle1_ThrowStairs", "Chapter1Battle1_Mount", "Chapter1Battle1_Finish" }) do
        state.name = name
        assert_protected()
    end
    for _, health in ipairs({ 1000, 1100 }) do
        local requested = sdk.float_to_ptr(health)
        local args = { nil, controller, requested, sdk.float_to_ptr(1200) }
        assign.before(args)
        assert(args[3] == requested, "Equal health and healing must pass through")
    end

    local function assert_unprotected(target)
        assert(adjust.before({ nil, target }) == nil)
        assert(calculate.before({ nil, target }) == nil)
        for _, native_result in ipairs({ 0, 123.5 }) do
            local result = sdk.float_to_ptr(native_result)
            assert(calculate.after(result) == result, "Preserve native damage outside the opening grapple")
        end
        local requested = sdk.float_to_ptr(100)
        local args = { nil, target, requested, sdk.float_to_ptr(1000) }
        assign.before(args)
        assert(args[3] == requested, "Other encounters must retain damage and scripted HP changes")
    end
    for _, name in ipairs({ "Chapter1Battle2_KnifeRush", "Chapter1Battle4_Mount", "Chapter4Battle", "Chapter1Battle1_FinishExtra", "" }) do
        state.name = name
        assert_unprotected(controller)
    end
    state.name = "Chapter1Battle1_Finish"
    state.type_name = "app.Em2000.Em2000DamageController"
    assert_unprotected(controller)
    state.type_name = "app.PlayerDamageController"
    state.active = false
    assert_unprotected(controller) -- stale grapple parameters after the opening ends
    state.active = true
    state.valid = false
    assert_unprotected(controller)
    state.valid = true
    for _, missing in ipairs({ "missing_player", "missing_grapple", "missing_param" }) do
        state[missing] = true
        assert_unprotected(controller)
        state[missing] = false
    end
    assert_unprotected(nil)
    assert_unprotected(0)
    assert_unprotected(8627681984) -- non-object receiver reported by the live shared-stub hook
    assert_unprotected({})

    -- Separate native hook invocations (including nested calls) have separate storage.
    local outer = {}
    storage = outer
    assert(calculate.before({ nil, controller }) == "skip")
    storage = {}
    state.active = false
    assert_unprotected(controller)
    state.active = true
    storage = outer
    assert(calculate.after({ uninitialized = true }).float == 0)

    enabled = false
    feature:on_config_changed()
    reads = 0
    assert_unprotected(controller)
    assert(reads == 0, "Disabling the feature must avoid engine traversal")
    enabled = true
    feature:on_config_changed()
    assert(hook_count == 3)
    assert_protected()
end
