local Engine = require("BioRand7/spawn_group_engine")

local SpawnGroups = {}
SpawnGroups.__index = SpawnGroups

function SpawnGroups.new(context, engine)
    return setmetatable({ context = context, engine = engine or Engine.new(context), groups = {}, clock = 0 }, SpawnGroups)
end

function SpawnGroups:install()
    self:load(json.load_file("BioRand7/spawn_groups.json"))
end

function SpawnGroups:load(manifest)
    self.manifest = nil
    if manifest ~= nil then
        if manifest.version ~= 1 or manifest.seed ~= self.context.config:get("biorand-seed", 0) then
            self.context.log:error("SpawnGroups manifest version/seed does not match this seed; controller disabled")
        else
            self.manifest = manifest
        end
    end
    self:reset()
end

function SpawnGroups:on_config_changed()
    self:install()
end

function SpawnGroups:reset()
    self.clock, self.next_update, self.groups = 0, 0, {}
    self.engine:reset()
    for _, definition in ipairs(self.manifest and self.manifest.groups or {}) do
        local group = { definition = definition, rules = {}, spawned = true, resumed = true, terminal = false }
        for _, condition in ipairs(definition.conditions) do
            group.rules[#group.rules + 1] = { condition = condition, inside = false, done = false }
            if condition.parameter == "spawn" then group.spawned = false end
            if condition.parameter == "resume" then group.resumed = false end
        end
        self.groups[#self.groups + 1] = group
    end
end

local function matches(engine, condition, position)
    if condition.state ~= "" and not engine:state_matches(condition.state) then return false end
    if condition.x ~= nil then
        local dx, dy, dz = position.x - condition.x, position.y - condition.y, position.z - condition.z
        if dx * dx + dy * dy + dz * dz > condition.radius * condition.radius then return false end
    end
    return true
end

function SpawnGroups:update()
    if #self.groups == 0 then return end
    local delta, position = self.engine:frame()
    if delta == nil or position == nil then return end -- Paused, loading, or no player.
    self.clock = self.clock + delta
    if self.clock < self.next_update then return end
    self.next_update = self.clock + 0.1
    self.engine:refresh(self.clock, self.manifest.groups)
    for _, group in ipairs(self.groups) do
        -- Native spawned/suspended records survive save reloads. Never reset IsCompleted.
        if not group.restored then
            local present, started, active = self.engine:restore(group.definition.members)
            if present then
                group.restored = true
                if started then
                    group.spawned = true
                    group.resumed = active
                    for _, rule in ipairs(group.rules) do
                        if rule.condition.parameter == "spawn" then rule.done = true end
                    end
                end
            end
        end
        if group.restored and not group.terminal then
            local due = {}
            for index, rule in ipairs(group.rules) do
                if not rule.done then
                    local inside = matches(self.engine, rule.condition, position)
                    -- A trigger latches its delay even after leaving the trigger volume/state.
                    if inside and not rule.inside and rule.deadline == nil then
                        rule.deadline = self.clock + rule.condition.time
                    end
                    rule.inside = inside
                    if rule.deadline ~= nil and self.clock >= rule.deadline then
                        due[#due + 1] = { rule = rule, index = index, deadline = rule.deadline }
                    end
                end
            end
            table.sort(due, function(a, b)
                if a.deadline ~= b.deadline then return a.deadline < b.deadline end
                return a.index < b.index
            end)
            for _, event in ipairs(due) do
                local rule, parameter = event.rule, event.rule.condition.parameter
                rule.deadline = nil
                if parameter == "spawn" then group.spawned, rule.done = true, true
                elseif parameter == "despawn" then group.terminal, rule.done = true, true
                elseif parameter == "suspend" then group.resumed = false
                elseif parameter == "resume" then group.resumed = true end
                self.context.log:info("SpawnGroup " .. group.definition.name .. ": " .. parameter, true)
            end
        end
        local desired = group.terminal and "despawn" or (group.spawned and group.resumed and "active" or "suspend")
        for _, member in ipairs(group.definition.members) do
            self.engine:apply(member, desired, self.clock)
        end
    end
end

return SpawnGroups
