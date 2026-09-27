local Rng = {}
Rng.__index = Rng

local MAX_INT = 2147483647
local UINT_RANGE = 4294967296
local WORD_RANGE = 65536

local function signed_int(value)
    value = value % UINT_RANGE
    if value > MAX_INT then value = value - UINT_RANGE end
    return value
end

local function xor_word(left, right)
    local result, bit = 0, 1
    for _ = 1, 16 do
        if left % 2 ~= right % 2 then
            result = result + bit
        end
        left, right = math.floor(left / 2), math.floor(right / 2)
        bit = bit * 2
    end
    return result
end

local function words(value)
    local result = {}
    for index = 1, 4 do
        result[index] = value % WORD_RANGE
        value = math.floor(value / WORD_RANGE)
    end
    return result
end

local function mix(hash, value)
    -- Four 16-bit words keep the original ulong hash exact even with Lua doubles.
    local carry = 0
    for index = 1, 4 do
        local product = hash[index] * 16777619 + carry
        hash[index] = xor_word(product % WORD_RANGE, value[index])
        carry = math.floor(product / WORD_RANGE)
    end
end

local function fold(hash)
    return xor_word(hash[1], hash[3]) + xor_word(hash[2], hash[4]) * WORD_RANGE
end

function Rng.new(seed)
    seed = signed_int(math.floor(seed or 1))
    local magnitude = seed == -2147483648 and MAX_INT or math.abs(seed)

    -- Match the seeded System.Random subtractive generator used by the C# plugin.
    local state = {}
    local previous = 161803398 - magnitude
    local next_value = 1
    state[55] = previous
    for index = 1, 54 do
        local slot = (21 * index) % 55
        state[slot] = next_value
        next_value = signed_int(previous - next_value)
        if next_value < 0 then next_value = next_value + MAX_INT end
        previous = state[slot]
    end
    for _ = 1, 4 do
        for index = 1, 55 do
            state[index] = signed_int(state[index] - state[1 + (index + 30) % 55])
            if state[index] < 0 then state[index] = state[index] + MAX_INT end
        end
    end
    return setmetatable({ state = state, index = 0, paired_index = 21 }, Rng)
end

function Rng.for_enemy(seed, address, generation)
    local hash = words(seed % UINT_RANGE)
    mix(hash, words(address))
    mix(hash, words(generation % UINT_RANGE))
    return Rng.new(fold(hash))
end

function Rng.for_em3300(seed, address)
    local hash = words(seed % UINT_RANGE)
    mix(hash, { 0x3030, 0x3333, 0x456D, 0 })
    mix(hash, words(address))
    return Rng.new(fold(hash))
end

function Rng.for_events(seed)
    local hash = words(seed % UINT_RANGE)
    mix(hash, { 0x6437, 0x616E, 0x6F52, 0x4269 })
    mix(hash, { 0x7473, 0x656E, 0x6576, 0 })
    return Rng.new(fold(hash))
end

function Rng:next()
    self.index = self.index % 55 + 1
    self.paired_index = self.paired_index % 55 + 1
    local value = signed_int(self.state[self.index] - self.state[self.paired_index])
    if value == MAX_INT then value = value - 1 end
    if value < 0 then value = value + MAX_INT end
    self.state[self.index] = value
    return value
end

function Rng:float()
    return self:next() * (1 / MAX_INT)
end

function Rng:int(minimum, maximum)
    return minimum + math.floor(self:float() * (maximum - minimum + 1))
end

function Rng:chance(probability)
    return self:float() < probability
end

function Rng:weighted(entries)
    local total = 0
    for _, entry in ipairs(entries) do
        total = total + entry.weight
    end

    local roll = self:float() * total
    local cumulative = 0
    for _, entry in ipairs(entries) do
        cumulative = cumulative + entry.weight
        if roll < cumulative then
            return entry.value
        end
    end
    return entries[#entries].value
end

return Rng
