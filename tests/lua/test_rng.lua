return function()
    local Rng = require("BioRand7/rng")

    local function sequence(rng, expected)
        for index, value in ipairs(expected) do
            assert(rng:next() == value, "System.Random sequence differs at sample " .. index)
        end
    end

    -- Reference samples from System.Random and the original C# seed mixers.
    sequence(Rng.new(0), { 1559595546, 1755192844, 1649316166, 1198642031, 442452829, 1200195957 })
    sequence(Rng.new(1), { 534011718, 237820880, 1002897798, 1657007234, 1412011072, 929393559 })
    sequence(Rng.new(-1), { 534011718, 237820880, 1002897798, 1657007234, 1412011072, 929393559 })
    sequence(Rng.new(2147483647), { 1559595546, 1755192844, 1649316172, 1198642031, 442452829, 1200195955 })
    sequence(Rng.new(-2147483648), { 1559595546, 1755192844, 1649316172, 1198642031, 442452829, 1200195955 })
    sequence(Rng.new(305419896), { 1833211375, 1043988864, 268295676, 471379373, 1912720188, 1697597342 })

    for _, factory in ipairs({ Rng.for_enemy, Rng.for_em3300 }) do
        local first, reloaded = factory(35825, "enemy:one"), factory(35825, "enemy:one")
        for _ = 1, 20 do assert(first:next() == reloaded:next()) end
        assert(factory(35825, "enemy:one"):next() ~= factory(35825, "enemy:two"):next())
        assert(factory(35825, "enemy:one"):next() ~= factory(35826, "enemy:one"):next())
        assert(not pcall(factory, 35825, 1250999896491), "Memory addresses must not be accepted as identities")
    end
    sequence(Rng.for_events(305419896), {
        1745618172, 130394104, 2016358628, 1228675900, 981669908, 670291357,
    })

    local rng = Rng.new(1)
    assert(rng:float() == 534011718 * (1 / 2147483647))
    rng.float = function() return 0.5 end
    assert(rng:weighted({ { value = "first", weight = 1 }, { value = "second", weight = 1 } }) == "second")
    assert(rng:int(3, 7) == 5)
    assert(not rng:chance(0.5))
end
