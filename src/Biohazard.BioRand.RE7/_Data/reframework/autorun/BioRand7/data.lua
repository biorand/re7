local Data = {}

Data.generic_drop_items = {
    "EasyBoots", "AlphaGrass", "LiquidBomb", "HandgunBullet", "HandgunBulletL", "ShotgunBullet",
    "MachineGunBullet", "MagnumBullet", "BurnerBullet", "FlameBulletS", "AcidBulletS", "RemedyM",
    "RemedyL", "EyeDrops", "Herb", "ChemicalM", "ChemicalL", "ChemicalS", "Gunpowder", "Coin", "Alcohol", "Flower",
}

Data.ammo = {
    HandgunBullet = true, HandgunBulletL = true, ShotgunBullet = true, MachineGunBullet = true,
    MagnumBullet = true, BurnerBullet = true, FlameBulletS = true, AcidBulletS = true,
}

Data.stack_limits = {
    HandgunBullet = 30, HandgunBulletL = 20, ShotgunBullet = 30, MachineGunBullet = 300,
    MagnumBullet = 20, BurnerBullet = 500, FlameBulletS = 5, AcidBulletS = 5, Coin = 999,
    CylinderKey = 20, EyeDrops = 5, Gunpowder = 10, Herb = 5, LiquidBomb = 20, RemedyL = 3,
    RemedyM = 3, Alcohol = 5, Flower = 5,
}

local handgun = { HandgunBullet = true, HandgunBulletL = true }
local shotgun = { HandgunBullet = true, HandgunBulletL = true, ShotgunBullet = true }
local advanced = {
    HandgunBullet = true, HandgunBulletL = true, ShotgunBullet = true, MagnumBullet = true,
    AcidBulletS = true, FlameBulletS = true,
}
local burner = {
    HandgunBullet = true, HandgunBulletL = true, ShotgunBullet = true, MagnumBullet = true,
    AcidBulletS = true, FlameBulletS = true, BurnerBullet = true,
}
local machine_gun = {
    HandgunBullet = true, HandgunBulletL = true, ShotgunBullet = true, MagnumBullet = true,
    AcidBulletS = true, FlameBulletS = true, BurnerBullet = true, MachineGunBullet = true,
}

Data.chapter_ammo = {
    [0] = handgun, [1] = handgun, [2] = handgun, [3] = shotgun, [4] = advanced,
    [5] = burner, [6] = burner, [7] = machine_gun, [8] = machine_gun,
    [9] = machine_gun, [13] = machine_gun,
}

Data.drop_config_ids = {
    Em3000 = "jackstalker", Em3001 = "jackstalker", Em3600 = "margemutated", Em4000 = "molded",
    Em4100 = "moldedquick", Em4200 = "moldedfat", Em5400 = "flyingbug", Em5510 = "insecthive",
    Em5511 = "insecthive", Em5512 = "insecthive", Em5520 = "insectswarm",
    Em8000 = "jackshears", Em8001 = "jackshears", Em8100 = "jackmutated",
}

Data.drop_multipliers = {
    Em4200 = 1.25, Em2000 = 1.35, Em3001 = 1.5, Em8000 = 1.75, Em8001 = 1.75, Em3600 = 2.0,
}

Data.bosses = { Em2000 = true, Em3001 = true, Em3600 = true, Em8000 = true, Em8001 = true, Em8100 = true }
Data.single_drop_per_spawn = { Em5510 = true, Em5511 = true, Em5512 = true }
Data.boss_drop_items = {
    LiquidBomb = true, HandgunBulletL = true, ShotgunBullet = true, MagnumBullet = true,
    FlameBulletS = true, AcidBulletS = true, RemedyL = true, ChemicalM = true, Coin = true,
}

Data.dlc_coin_weights = {
    { id = "GoodLuckCoinA_Buy", minimum = 3, maximum = 5 },
    { id = "GoodLuckCoinB_Buy", minimum = 3, maximum = 5 },
    { id = "GoodLuckCoinC_Buy", minimum = 5, maximum = 10 },
    { id = "GoodLuckCoinD_Buy", minimum = 10, maximum = 15 },
    { id = "GoodLuckCoinE_Buy", minimum = 1, maximum = 3 },
}

Data.birthday_skills = {
    "skl002", "skl008", "skl009", "skl010", "skl011", "skl012", "skl013", "skl014", "skl015",
    "skl016", "skl017", "skl018", "skl019", "skl021", "skl022", "skl023",
}

return Data
