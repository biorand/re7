local Inventory = {}
Inventory.__index = Inventory

local SIZE_LEVELS = { ["12"] = 0, ["16"] = 1, ["20"] = 2 }
local KEY_ITEM = 4
local USABLE_KEY_ITEM = 9
local DISCARDABLE_KEY_ITEM = 10
local MAX_COMBINE_ROWS = 5
local STORY_WEAPONS = {
    handaxe = true, knife = true, chainsaw = true,
    candle = true, candle_lighted = true, handgun_albert = true,
}
local OPTIONAL_VIDEOTAPES = {
    foundfootage000 = true, foundfootage030 = true, foundfootage040 = true,
}

function Inventory.new(context)
    return setmetatable({ context = context }, Inventory)
end

function Inventory:desired_level()
    local manager = self.context.game:singleton("app.ObjectManager")
    local player = manager and manager:get_field("PlayerObj")
    if player == nil then
        return nil
    end

    local name = player:call("get_Name")
    if name == nil then
        return nil
    end

    if name:sub(1, 4) == "Pl00" then
        return SIZE_LEVELS[tostring(self.context.config:get("random-starting-inventory-size-ethan", "12"))]
    end
    if name:sub(1, 3) == "Pl2" then
        return SIZE_LEVELS[tostring(self.context.config:get("random-starting-inventory-size-mia", "12"))]
    end
    return nil
end

function Inventory:is_birthday_skill(item)
    if item == nil then
        return false
    end

    local data_id = item:get_field("ItemDataID")
    if data_id == nil then
        return false
    end

    local normalized = data_id:lower()
    return normalized:sub(1, 3) == "skl" and normalized:sub(-2) ~= "no"
end

function Inventory:install_discard_hook()
    local game = self.context.game
    game:hook("app.Item", "isCanDiscard()", function(args)
        local storage = thread.get_hook_storage()
        storage.biorand_discard_item = nil
        if self.context.config:get("inventory-unrestricted-management", true) then
            storage.biorand_discard_item = game:object(args[2])
        end
    end, function(retval)
        local storage = thread.get_hook_storage()
        local item = storage.biorand_discard_item
        storage.biorand_discard_item = nil
        if item == nil or sdk.to_int64(retval) % 256 ~= 0 then return retval end
        local item_data = item:call("get_ItemData")
        if item_data == nil then return retval end
        local data_id = item:get_field("ItemDataID") or item_data:get_field("ItemDataID")
        local normalized = data_id and data_id:lower()
        -- Native weapon and car-key restrictions can be progression gates too.
        -- Preserve them until the game itself says the item can be discarded.
        if normalized and STORY_WEAPONS[normalized] then return retval end
        local category = item_data:get_field("Category")
        if category ~= KEY_ITEM and category ~= USABLE_KEY_ITEM and category ~= DISCARDABLE_KEY_ITEM then
            return sdk.to_ptr(1)
        end
        -- Old Videotape (050) is mandatory for present-day ship progression.
        if normalized and OPTIONAL_VIDEOTAPES[normalized] then
            return sdk.to_ptr(1)
        end
        return retval
    end)
end

function Inventory:install_birthday_skill_hook()
    local game = self.context.game
    game:hook("app.PassiveSkillItem", "onInsertInventory(app.Inventory)", function(args)
        local skill_item = game:object(args[2])
        local item = skill_item:get_field("Item")
        if not self:is_birthday_skill(item) then
            return
        end

        local manager = game:singleton("app.ObjectManager")
        local player = manager and manager:get_field("PlayerObj")
        local player_order = game:component(player, "app.PlayerOrder")
        local passive_skill = skill_item:get_field("PassiveSkill")
        if passive_skill == nil or player_order == nil then
            self.context.log:warn("Unable to register Birthday passive skill " .. item:get_field("ItemDataID"))
            return sdk.PreHookResult.SKIP_ORIGINAL
        end
        skill_item:set_field("PlayerOrder", player_order)
        player_order:call("registerPassiveSkill(app.PlayerPassiveSkill)", passive_skill)
        return sdk.PreHookResult.SKIP_ORIGINAL
    end)
end

function Inventory:install_size_hooks()
    local game = self.context.game
    game:hook("app.Inventory", "setupItemSlotManager(app.Inventory.ExtendLvDef)", function(args)
        local desired = self:desired_level()
        if desired == nil then
            return
        end

        local inventory = game:object(args[2])
        if inventory:call("get_ExtendLv") < desired then
            inventory:set_field("_ExtendLv", desired)
        end
        if sdk.to_int64(args[3]) < desired then
            args[3] = sdk.to_ptr(desired)
        end
    end)

    game:hook("app.Inventory", "setExtendLv(app.Inventory.ExtendLvDef)", function(args)
        local desired = self:desired_level()
        if desired ~= nil and sdk.to_int64(args[3]) < desired then
            args[3] = sdk.to_ptr(desired)
        end
    end)
end

function Inventory:install_combine_hooks()
    local game = self.context.game
    local function configure_rows()
        if not self.context.config:get("recipes-add-new", false)
            and not self.context.config:get("debug-recipes-enabled", false) then return end
        local type_name = "app.InventoryMenu.DictionaryCombineUIController"
        if game:static_field(type_name, "RowNum") ~= MAX_COMBINE_ROWS then
            game:set_static_field(type_name, "RowNum", MAX_COMBINE_ROWS)
        end
    end
    -- Configure the first menu setup too: a Main House start bypasses the
    -- Guest House inventory, so it must not depend on an earlier deactivate.
    game:hook("app.InventoryMenu.DictionaryCombineUIController", "setup()", configure_rows)
    game:hook("app.InventoryMenu.DictionaryCombineUIController", "deactivate()", configure_rows)

    game:hook("app.InventoryMenu", "DictionaryCombine_UnlockedCombine(app.ItemCombineData.Data)", nil,
        function(retval)
            if self.context.config:get("debug-recipes-enabled", false)
                or not self.context.config:get("recipes-unlock-from-start", false) then return retval end
            if sdk.to_int64(retval) % 256 == 0 then
                return sdk.to_ptr(1)
            end
            return retval
        end)
end

function Inventory:install()
    self:install_discard_hook()
    self:install_birthday_skill_hook()
    self:install_size_hooks()
    self:install_combine_hooks()
end

return Inventory
