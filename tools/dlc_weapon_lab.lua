-- Run manually in REFramework's ScriptRunner. Not installed by ordinary generation.
local Game = require("BioRand7/game")
local Lab = require("BioRand7/dlc_weapon_lab")
local lab = Lab.new(Game.new())
lab:install()
local selected = 1
local labels = {}
for _, entry in ipairs(Lab.candidates) do labels[#labels + 1] = entry.name end

re.on_application_entry("UpdateBehavior", function() lab:update() end)
re.on_script_reset(function() lab:reset() end)
re.on_draw_ui(function()
    if not imgui.tree_node("BioRand DLC Weapon Lab") then return end
    imgui.text("EXPERIMENTAL. Do not save a production playthrough.")
    imgui.text("Spirit Blade and AMG-Dual are experimental. Other AMG and throwable adapters are unavailable.")
    local changed
    changed, lab.armed = imgui.checkbox("Enable test commands", lab.armed)
    changed, selected = imgui.combo("Weapon", selected, labels)
    if imgui.button("Prepare prefab") then lab:queue("prepare", Lab.candidates[selected].id) end
    if imgui.button("Add to item box") then lab:queue("add", Lab.candidates[selected].id) end
    if imgui.button("Inspect equipped weapon") then lab:queue("inspect") end
    if imgui.button("Remove boxed test weapons") then lab:queue("cleanup") end
    imgui.text(lab.status)
    if lab.report then
        for _, key in ipairs({ "player", "type", "weapon_id", "load", "ammo_id", "bullet_type", "infinite_load" }) do
            if lab.report[key] ~= nil then imgui.text(key .. ": " .. tostring(lab.report[key])) end
        end
    end
    imgui.tree_pop()
end)
