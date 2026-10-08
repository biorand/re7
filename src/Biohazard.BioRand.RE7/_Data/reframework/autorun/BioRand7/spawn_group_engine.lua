local Static = require("BioRand7/spawn_group_static")
local Engine = {}
Engine.__index = Engine
local unpack_args = table.unpack or unpack

function Engine.new(context)
    local self = setmetatable({ context = context, game = context.game, static = Static.new(context) }, Engine)
    self:reset()
    return self
end

function Engine:reset(groups)
    self.static:reset(groups)
    self.members, self.states, self.requests, self.warned = {}, {}, {}, {}
    self.next_discovery, self.next_states = 0, 0
    self.manager = nil
end

function Engine:frame()
    if self.game:method("via.SceneManager", "get_Loading()"):call(nil) then return nil end
    local manager = self.game:singleton("app.GameManager")
    if manager == nil or manager:call("get_IsPause") then return nil end
    local player = self.game:player()
    if player == nil then return nil end
    local transform = player:call("get_Transform")
    if transform == nil then return nil end
    -- RE7 DeltaTime is scaled to a 60 Hz frame. ElapsedSecond is seconds per frame.
    local delta = self.game:method("via.Application", "get_ElapsedSecond()"):call(nil)
    return math.max(0, math.min(delta, 0.25)), transform:call("get_Position")
end

function Engine:valid(component)
    return component ~= nil and self.game:valid(component:call("get_GameObject"))
end

function Engine:refresh(now, groups)
    self.current_states = {} -- Read each FSM/core once per update, shared by all conditions.
    if now >= self.next_discovery then
        self.next_discovery = now + 1
        self.members = {}
        self.static:refresh()
        self.manager = self.game:singleton("app.EnemyGeneratorManager")
        local wanted = {}
        for _, group in ipairs(groups) do
            for _, member in ipairs(group.members) do
                if member.kind ~= "static" then wanted[member.runtimeGuid] = true end
            end
        end
        if self.manager ~= nil then
            for generator in self.game:list(self.manager:get_field("generators")) do
                if self:valid(generator) then
                    local pool = generator:call("get_poolInstance")
                    if pool ~= nil then
                        for _, field in ipairs({ "SpawnInfos", "ForceSpawnInfos", "ExternalSpawnInfos", "ExternalForceSpawnInfos" }) do
                            for spawn in self.game:list(pool:get_field(field)) do
                                if self:valid(spawn) then
                                    local guid = self.game:guid_string(spawn:get_field("MyGUID"))
                                    if wanted[guid] then self.members[guid] = spawn end
                                end
                            end
                        end
                    end
                end
            end
        end
    end
    if now < self.next_states then return end
    self.next_states = now + 2
    self.states = {}
    local wanted = {}
    for _, group in ipairs(groups) do
        for _, condition in ipairs(group.conditions) do
            if condition.state ~= "" then wanted[condition.state] = true end
        end
    end
    if next(wanted) == nil then return end
    local scene = self.game:method("via.SceneManager", "get_CurrentScene()"):call(nil)
    if scene == nil then return end
    self.fsm_type = self.fsm_type or sdk.typeof("via.fsm.Fsm")
    local fsms = scene:call("findComponents(System.Type)", self.fsm_type)
    if fsms == nil then return end
    for query in pairs(wanted) do
        local guid, core, state = query:match("^([^|]+)|(%d+)|(.+)$")
        core, state = tonumber(core) or 0, state or query
        local matches = {}
        for index = 0, fsms:get_size() - 1 do
            local fsm = fsms:get_element(index)
            if self:valid(fsm) then
                local qualified = guid == nil or self.game:guid_string(scene:call("createRefId(via.GameObject)", fsm:call("get_GameObject"))) == guid:lower()
                if qualified then
                    local full = fsm:call("hasStateFullName(System.String, System.UInt32)", state, core)
                    if full or fsm:call("hasState(System.String, System.UInt32)", state, core) then
                        local method = full and "queryStateIDFullName(System.String, System.UInt32)" or "queryStateID(System.String, System.UInt32)"
                        matches[#matches + 1] = { fsm = fsm, core = core, id = fsm:call(method, state, core) }
                    end
                end
            end
        end
        if #matches == 1 then
            self.states[query] = matches[1]
        elseif #matches > 1 and not self.warned[query] then
            self.warned[query] = true
            self.context.log:warn("Ambiguous SpawnGroup State '" .. query .. "'; use FsmGameObjectGuid|CoreId|StateName")
        end -- Unloaded FSMs are expected and retried on the next discovery pass.
    end
end

function Engine:state_matches(query)
    local state = self.states[query]
    if state == nil or not self:valid(state.fsm) then return false end
    local key = tostring(self.game:address(state.fsm)) .. ":" .. state.core
    local current = self.current_states[key]
    if current == nil then
        current = state.fsm:call("getCurrentStateID(System.UInt32)", state.core)
        self.current_states[key] = current
    end
    return current == state.id
end

function Engine:restore(members)
    local present, started, active = false, false, false
    for _, member in ipairs(members) do
        local spawn = self.members[member.runtimeGuid]
        if member.kind == "static" then
            local state = self.static:state(member)
            present = present or state ~= nil
            started = started or (state ~= nil and state ~= 0)
            active = active or state == 1
        elseif self:valid(spawn) then
            present = true
            local spawned = spawn:get_field("IsSpawned")
            local suspended = spawn:get_field("suspendType") ~= 0
            started = started or spawned or suspended or spawn:get_field("IsCompleted")
            active = active or (spawned and not suspended and not spawn:get_field("IsCompleted"))
        end
    end
    return present, started, active
end

function Engine:apply(member, desired, now)
    if member.kind == "static" then
        local ok, err = pcall(self.static.apply, self.static, member, desired)
        local key = member.runtimeGuid .. ":static"
        if not ok and not self.warned[key] then
            self.warned[key] = true
            self.context.log:warn("SpawnGroup static request failed for " .. member.runtimeGuid .. ": " .. tostring(err))
        elseif ok then
            self.warned[key] = nil
        end
        return
    end
    local spawn = self.members[member.runtimeGuid]
    if self.manager == nil or not self:valid(spawn) or spawn:get_field("IsCompleted") then return end
    if spawn:get_field("RequestedOperation") ~= 0 then return end
    local previous = self.requests[member.runtimeGuid]
    if previous ~= nil and previous.desired == desired and now < previous.time + 1 then return end
    local spawned, suspended = spawn:get_field("IsSpawned"), spawn:get_field("suspendType") ~= 0
    local method, args
    if desired == "despawn" then
        method, args = "requestKill(app.EnemySpawnInfo)", { spawn }
    elseif desired == "suspend" then
        if not spawned or suspended then return end
        method, args = "requestSuspend(app.EnemySpawnInfo)", { spawn }
    else
        if spawned and not suspended then return end
        if member.aggro then
            spawn:set_field("IsPlayerTargetingAtStart", true)
            local option = spawn:call("get_option")
            if option ~= nil and option:get_type_definition():get_field("IsForceTargetingToPlayer") ~= nil then
                option:set_field("IsForceTargetingToPlayer", true)
            end
        end
        if suspended then
            method, args = "requestResume(app.EnemySpawnInfo, System.Boolean, System.Boolean, System.Int32)", { spawn, false, false, 0 }
        else
            method, args = "requestSpawn(app.EnemySpawnInfo, System.Int32)", { spawn, 0 }
        end
    end
    self.requests[member.runtimeGuid] = { desired = desired, time = now }
    local ok, err = pcall(self.manager.call, self.manager, method, unpack_args(args))
    local error_key = member.runtimeGuid .. ":" .. desired
    if not ok and not self.warned[error_key] then
        self.warned[error_key] = true
        self.context.log:warn("SpawnGroup request failed for " .. member.runtimeGuid .. " (" .. desired .. "): " .. tostring(err))
    elseif ok then
        self.warned[error_key] = nil
    end
end

return Engine
