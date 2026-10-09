-- Opt-in research helper. BioRand7.lua deliberately does not load this module.
local Lab = {}
Lab.__index = Lab

Lab.candidates = {
    { id = "CKnife", name = "Tactical Knife" },
    { id = "Handgun_Albert_C", name = "Samurai Edge - AW Model-01" },
    { id = "Shotgun_Albert", name = "Thor's Hammer - AW Model-02" },
    { id = "NumaItem072", name = "Joe's M21" },
}

local function candidate(id)
    for _, entry in ipairs(Lab.candidates) do if entry.id == id then return entry end end
end

function Lab.new(game)
    return setmetatable({ game = game, armed = false, added = {}, status = "Disabled" }, Lab)
end

function Lab:reset()
    self.armed, self.pending, self.report, self.box_identity = false, nil, nil, nil
    self.added = {}
    self.status = "Session changed; test receipts cleared"
end

function Lab:install()
    self.game:hook("app.SaveDataManager", "newGameInit()", function() self:reset() end)
    self.game:hook("app.SaveDataManager", "loadLevelUsingLoadData()", function() self:reset() end)
end

function Lab:queue(command, id)
    if not self.armed then self.status = "Lab is disabled"; return false end
    if self.pending then self.status = "A command is pending"; return false end
    if command ~= "add" and command ~= "prepare" and command ~= "cleanup" and command ~= "inspect" then
        self.status = "Unknown command"; return false
    end
    if (command == "add" or command == "prepare") and not candidate(id) then
        self.status = "No campaign adapter for this weapon"; return false
    end
    self.pending = { command = command, id = id }
    return true
end

function Lab:session()
    local player = self.game:player()
    if not player or not (player:call("get_Name") or ""):match("^Pl00") then
        error("A loaded Ethan campaign is required")
    end
    local inventory = self.game:component(player, "app.Inventory")
    local box = inventory and inventory:call("get_ItemBoxData")
    if not box then error("Inventory/item box is not ready") end
    local identity = self.game:address(box)
    if identity ~= self.box_identity then
        -- Never remove pre-existing or reloaded items based on an older session's receipt.
        self.box_identity = identity
        self.added = {}
    end
    return player, inventory, box
end

function Lab:execute(request)
    local player, inventory, box = self:session()
    if request.command == "add" or request.command == "prepare" then
        local manager = self.game:singleton("app.ItemManager")
        local data = manager and manager:call("findItemData", request.id)
        local prefab = data and data:get_field("ItemPrefab")
        local path = prefab and prefab:call("get_Path")
        local expected = "BioRand/DlcWeaponLab/" .. request.id .. "/Item.pfb"
        if not path or path:lower() ~= expected:lower() then error("Matching DLC Weapon Lab patch is not loaded") end
        if request.command == "prepare" then
            if not prefab:call("get_Ready") then
                -- Imported settings can report Standby=true without a ready resource.
                -- Request loading once; never instantiate or force awake/start here.
                prefab:call("set_Standby", false)
                prefab:call("set_Path", path)
                prefab:call("set_Standby", true)
            end
            self.status = "Requested " .. request.id .. " prefab; wait before adding"
            return
        end
        if not prefab:call("get_Ready") then error("Prefab is not ready; prepare it before adding") end
        if inventory:call("hasItemIncludeItemBox", request.id, false) then
            self.status = request.id .. " already exists"
            return
        end
        if not box:call("addItem", request.id, 1, nil) then error("Item box refused the weapon") end
        self.added[request.id] = true
        self.status = "Added " .. request.id .. " to the item box"
    elseif request.command == "cleanup" then
        for _, entry in ipairs(Lab.candidates) do
            if self.added[entry.id] then
                if inventory:call("hasItem", entry.id, false) then
                    error("Return " .. entry.id .. " to the item box before cleanup")
                end
                if inventory:call("hasItemIncludeItemBox", entry.id, false)
                    and box:call("removeItem(System.String, System.Int32)", entry.id, 1) ~= 1 then
                    error("Item box could not remove " .. entry.id)
                end
                self.added[entry.id] = nil
            end
        end
        self.status = "Removed this session's boxed test weapons"
    else
        local equip = self.game:component(player, "app.EquipManager")
        local weapon = equip and equip:call("get_equipWeaponRight")
        local report = { player = player:call("get_Name"), equipped = weapon ~= nil }
        if weapon then
            report.type = weapon:get_type_definition():get_full_name()
            report.weapon_id = weapon:get_field("WeaponID")
            local go = weapon:call("get_GameObject")
            local gun = self.game:component(go, "app.WeaponGun")
            if gun then
                local bullet = gun:get_field("CurrentBulletInfo")
                report.load = bullet and bullet:get_field("LoadNum")
                report.ammo_id = bullet and bullet:get_field("BulletItemID")
                report.bullet_type = gun:call("getBulletType")
                report.infinite_load = gun:call("get_isLoadNumInfinity")
            end
        end
        self.report = report
        self.status = "Captured native equipped-weapon state"
    end
end

function Lab:update()
    local request = self.pending
    self.pending = nil
    if not request or not self.armed then return end
    local ok, message = xpcall(function() self:execute(request) end, debug.traceback)
    if not ok then
        self.armed = false
        self.status = tostring(message)
    end
end

return Lab
