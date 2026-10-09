local Weapons = {}
Weapons.__index = Weapons
local IDS = { "CKnife", "Handgun_Albert_C", "Shotgun_Albert", "NumaItem072" }

function Weapons.new(context)
    return setmetatable({ context = context, requested = {}, next_check = 0 }, Weapons)
end

function Weapons:install() end

function Weapons:reset()
    self.requested, self.next_check, self.finished = {}, 0, false
end

function Weapons:on_config_changed() self:reset() end

function Weapons:update()
    local context = self.context
    if self.finished or not context.config:get("dlc-campaign-weapons", false)
        or not context.config:get("allow-dlc-items", false) then return end
    local now = os.clock()
    if now < self.next_check then return end
    self.next_check = now + 1
    local manager = context.game:singleton("app.ItemManager")
    if not manager then return end
    local complete = true
    for _, id in ipairs(IDS) do
        if not self.requested[id] then
            local data = manager:call("findItemData", id)
            local prefab = data and data:get_field("ItemPrefab")
            local path = prefab and prefab:call("get_Path")
            -- Never touch source DLC prefabs or a foreign mod's item registrations.
            if path and path:lower() == ("BioRand/DlcWeapons/" .. id .. "/Item.pfb"):lower() then
                self.requested[id] = true
                local ok, message = pcall(function()
                    if not prefab:call("get_Ready") then
                        prefab:call("set_Standby", false)
                        prefab:call("set_Path", path)
                        prefab:call("set_Standby", true)
                    end
                end)
                if not ok then context.log:error("DLC weapon load request failed for " .. id .. ": " .. tostring(message)) end
            else
                complete = false
            end
        end
    end
    -- One request per registered item per session. No creation, grants, forced lifecycle or retry loop.
    self.finished = complete
end

return Weapons
