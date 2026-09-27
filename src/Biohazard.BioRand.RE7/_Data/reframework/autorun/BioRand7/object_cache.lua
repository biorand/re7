local ObjectCache = {}
ObjectCache.__index = ObjectCache

local SCAN_BUDGET = 64
local SCAN_INTERVAL = 0.5

function ObjectCache.new(game, select)
    local self = setmetatable({ game = game, select = select }, ObjectCache)
    self:reset()
    return self
end

function ObjectCache:reset()
    self.values = {}
    self.manager = nil
    self.groups = nil
    self.seen = nil
    self.group_index = 0
    self.item_index = 0
    self.next_scan_at = 0
end

function ObjectCache:add(game_object)
    local address = self.game:address(game_object)
    local value = self.select(game_object, self.values[address])
    if value ~= nil then
        self.values[address] = value
        if self.seen ~= nil then self.seen[address] = true end
    end
end

function ObjectCache:refresh_scene()
    local manager = self.game:singleton("app.ObjectManager")
    local groups = manager and manager:get_field("ManagedObjects")
    if manager ~= self.manager or groups ~= self.groups then
        self:reset()
        self.manager, self.groups = manager, groups
    end
    return groups
end

function ObjectCache:register(game_object)
    self:refresh_scene()
    if self.game:valid(game_object) then self:add(game_object) end
end

function ObjectCache:update(now)
    local groups = self:refresh_scene()
    if groups == nil then return self.values end
    if now < self.next_scan_at then return self.values end
    if self.seen == nil then self.seen = {} end

    local group_items, group_count = self.game:list_storage(groups)
    local remaining = SCAN_BUDGET
    while self.group_index < group_count do
        local group = group_items:get_element(self.group_index)
        local items, count = self.game:list_storage(group)
        while self.item_index < count do
            if remaining == 0 then return self.values end
            local object = items:get_element(self.item_index)
            self.item_index = self.item_index + 1
            remaining = remaining - 1
            if self.game:valid(object) then self:add(object) end
        end
        self.group_index = self.group_index + 1
        self.item_index = 0
    end

    -- Retain previously discovered targets until the entire sweep has finished.
    for address in pairs(self.values) do
        if not self.seen[address] then self.values[address] = nil end
    end
    self.seen = nil
    self.group_index = 0
    self.next_scan_at = now + SCAN_INTERVAL
    return self.values
end

return ObjectCache
