return function()
    local Player = require("BioRand7/dlc_weapon_player")
    local function object(fields, calls)
        return { get_field = function(_, key) return fields[key] end,
            call = function(_, method, ...)
                assert(calls[method], "Unexpected call: " .. method)
                return calls[method](...)
            end }
    end
    local enabled, health, enemy_health, heals = true, 500, 1000, 0
    local strong, registered, fail = false, 0, false
    local name, path = "Pl0000", "BioRand/DlcWeaponLab/CH9_WP002/Item.pfb"
    local player = object({}, { get_Name = function() return name end })
    local current_player = player
    local weapon_object, enemy_object, foreign_object = {}, {}, {}
    local weapon_fields = { WeaponID = 63, IsAttacking = false }
    local weapon = object(weapon_fields, { get_GameObject = function() return weapon_object end })
    local current_weapon = weapon
    local equip = object({}, { get_equipWeaponRight = function() return current_weapon end })
    local collider = object({}, { get_NumRegisteredRequestSetIds = function() return registered end })
    local player_damage = object({}, {
        get_GameObject = function() return player end,
        get_health = function() return health end,
        recoveryHealth = function(amount)
            assert(math.type(amount) == "float", "Native System.Single arguments require Lua floats")
            heals = heals + 1
            if fail then error("native recovery failed") end
            health = math.min(1000, health + amount)
        end,
    })
    local enemy_damage = object({}, {
        get_GameObject = function() return enemy_object end,
        get_health = function() return enemy_health end,
    })
    local melee = object({}, { get_isAimAttack = function() return strong end })
    local prefab = object({}, { get_Path = function() return path end })
    local data = object({ ItemPrefab = prefab }, {})
    local manager = object({}, { findItemData = function() return data end })
    local components = {
        [player] = { ["app.EquipManager"] = equip, ["app.PlayerDamageController"] = player_damage, ["app.PlayerMelee"] = melee },
        [weapon_object] = { ["via.physics.RequestSetCollider"] = collider },
        [enemy_object] = { ["app.EnemyDamageController"] = enemy_damage },
    }
    local game = {
        player = function() return current_player end,
        component = function(_, owner, kind) return components[owner] and components[owner][kind] end,
        address = function(_, value) return value end,
        singleton = function() return manager end,
    }
    local attacker = weapon_object
    local hit = object({}, { get_AttackGameObject = function() return attacker end })
    local record_fields = { AddedDamage = 60 }
    local record = object(record_fields, {})
    local adapter = Player.new(game, function() return enabled end, "BioRand/DlcWeaponLab")
    local function begin_swing()
        registered, weapon_fields.IsAttacking = 0, false
        adapter:update()
        registered, weapon_fields.IsAttacking = 3, true
        adapter:update()
    end
    local function damage(amount)
        local pending = adapter:begin_damage(enemy_damage, hit)
        enemy_health = enemy_health - amount
        adapter:finish_damage(pending, record)
    end
    begin_swing()
    damage(60)
    assert(health == 500 and heals == 0, "Native hooks must not mutate player health")
    damage(60)
    adapter:update()
    assert(health == 600 and heals == 1, "Heal once for a multi-contact swing")
    adapter:update(); damage(60); adapter:update()
    assert(health == 600 and heals == 1)
    begin_swing(); adapter:update()
    assert(health == 600, "Missed swings must not heal")
    strong = true
    damage(60); adapter:update()
    assert(health == 750 and heals == 2)
    begin_swing(); damage(0); adapter:update()
    assert(health == 750, "A contact without enemy health loss must not heal")
    record_fields.AddedDamage = 0
    damage(60); adapter:update()
    assert(health == 750, "Rejected native damage must not heal")
    record_fields.AddedDamage = 60
    attacker = foreign_object
    assert(adapter:begin_damage(enemy_damage, hit) == nil)
    attacker = weapon_object
    assert(adapter:begin_damage(player_damage, hit) == nil, "Only enemies qualify")
    begin_swing(); damage(60)
    current_weapon = nil
    adapter:update()
    assert(health == 750 and not adapter.window and not adapter.pending_recovery)
    current_weapon = weapon
    begin_swing(); damage(60); adapter:reset(); adapter:update()
    assert(health == 750, "Loads discard queued recovery")
    begin_swing(); damage(60); enabled = false; adapter:update()
    assert(health == 750 and not adapter.window)
    enabled = true
    for _, foreign_name in ipairs({ "Pl9000", "Pl8000", "Pl1000" }) do
        name = foreign_name; adapter:update(); assert(not adapter.window)
    end
    name = "Pl0000"
    path = "CH9/Original.pfb"; adapter:update(); assert(not adapter.window)
    path = "BioRand/DlcWeaponLab/CH9_WP002/Item.pfb"
    current_player = nil; adapter:update(); assert(not adapter.window)
    current_player = player
    begin_swing(); damage(60); health = 0; adapter:update()
    assert(health == 0 and heals == 2, "Never revive a dead player")
    health = 990
    begin_swing(); damage(60); adapter:update()
    assert(health == 1000 and heals == 3)
    begin_swing(); damage(60); fail = true
    assert(not pcall(function() adapter:update() end))
    adapter:update()
    assert(heals == 4, "Do not retry a failed health mutation")
end
