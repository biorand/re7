package.path = "src/Biohazard.BioRand.RE7/_Data/reframework/autorun/?.lua;" .. package.path
print("Runtime: " .. _VERSION)

local suites = {
    "test_em3300",
    "test_em3300_idle",
    "test_object_cache",
    "test_enemy_targets",
    "test_inventory",
    "test_inventory_pause",
    "test_crafting",
    "test_enemy_drops",
    "test_enemy_identity",
    "test_static_mia",
    "test_mia_opening_damage",
    "test_rng",
    "test_session_state",
    "test_random_events",
    "test_performance",
    "test_spawn_groups",
    "test_spawn_group_static",
}

local failures = 0
for _, name in ipairs(suites) do
    local globals = {}
    for key, value in pairs(_G) do globals[key] = value end
    local clock = os.clock
    for key in pairs(package.loaded) do
        if key:match("^BioRand7/") then package.loaded[key] = nil end
    end

    local ok, message = xpcall(function()
        dofile("tests/lua/" .. name .. ".lua")()
    end, debug.traceback)

    os.clock = clock
    for key in pairs(_G) do
        if globals[key] == nil then _G[key] = nil end
    end
    for key, value in pairs(globals) do _G[key] = value end

    if ok then
        print("PASS " .. name)
    else
        failures = failures + 1
        io.stderr:write("FAIL " .. name .. "\n" .. tostring(message) .. "\n")
    end
end

assert(failures == 0, failures .. " Lua suite(s) failed")
print(#suites .. " Lua suites passed")
