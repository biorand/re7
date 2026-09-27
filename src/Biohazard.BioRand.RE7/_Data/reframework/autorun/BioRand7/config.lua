local Config = {}
Config.__index = Config

local CONFIG_PATH = "BioRand7/config.json"

function Config.new()
    local self = setmetatable({}, Config)
    self:reload()
    return self
end

function Config:reload()
    self.values = json.load_file(CONFIG_PATH) or {}
    self.sorted_entries = nil
end

function Config:get(key, default)
    local value = self.values[key]
    if value == nil then
        return default
    end
    return value
end

function Config:entries()
    if self.sorted_entries ~= nil then return self.sorted_entries end
    local entries = {}
    for key, value in pairs(self.values) do
        entries[#entries + 1] = { key = key, value = value, sort_key = key:lower() }
    end
    table.sort(entries, function(left, right)
        if left.sort_key == right.sort_key then return left.key < right.key end
        return left.sort_key < right.sort_key
    end)
    self.sorted_entries = entries
    return entries
end

return Config
