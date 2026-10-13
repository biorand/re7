using Enums.app;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Items;

internal class ItemDrops {
    private static readonly ImmutableDictionary<string, double> _defaultGenericDropRatios =
        new Dictionary<string, double>(StringComparer.Ordinal){
            ["HandgunBullet"] = 0.2,
            ["HandgunBulletL"] = 0.2,
            ["ShotgunBullet"] = 0.2,
            ["MachineGunBullet"] = 0.2,
            ["MagnumBullet"] = 0.05,
            ["BurnerBullet"] = 0.1,
            ["FlameBulletS"] = 0.08,
            ["AcidBulletS"] = 0.08,
            ["RemedyM"] = 0.05,
            ["RemedyL"] = 0.05,
            ["EyeDrops"] = 0.2,
            ["Stimulant"] = 0.01,
            ["Depressant"] = 0.01,
            ["Herb"] = 0.1,
            ["EasyBoots"] = 0.0,
            ["AlphaGrass"] = 0.01,
            ["ChemicalM"] = 0.1,
            ["ChemicalL"] = 0.2,
            ["ChemicalS"] = 0.2,
            ["Gunpowder"] = 0.35,
            ["LiquidBomb"] = 0.05,
            ["Grenadebomb"] = 0.03,
            ["Thermatebomb"] = 0.03,
            ["Stangrenadebomb"] = 0.03,
            ["CH9_WP003"] = 0.03,
            ["CH9_WP004"] = 0.03,
            ["CH9_WP005"] = 0.03,
            ["Coin"] = 0.1,
            ["Alcohol"] = 0.05,
            ["Flower"] = 0.05,
            ["AlloyClay"] = 0.05,
            ["Magnesium"] = 0.05,
            ["SyntheticDetergent"] = 0.05,
        }.ToImmutableDictionary(StringComparer.Ordinal);

    public static ImmutableList<string> GenericDrops { get; private set; } =[
        "EasyBoots",
        "AlphaGrass",
        "LiquidBomb",
        "HandgunBullet",
        "HandgunBulletL",
        "ShotgunBullet",
        "MachineGunBullet",
        "MagnumBullet",
        "BurnerBullet",
        "FlameBulletS",
        "AcidBulletS",
        "RemedyM",
        "RemedyL",
        "EyeDrops",
        "Stimulant",
        "Depressant",
        "Herb",
        "ChemicalM",
        "ChemicalL",
        "ChemicalS",
        "Gunpowder",
        "Coin",
        "Alcohol",
        "Flower",
        "AlloyClay",
        "Magnesium",
        "SyntheticDetergent",
        "Grenadebomb",
        "Thermatebomb",
        "Stangrenadebomb",
        "CH9_WP003",
        "CH9_WP004",
        "CH9_WP005"
    ];

    public static ImmutableHashSet<string> UnsupportedRuntimeDropIds { get; } =[
        "Stimulant",
        "Depressant",
    ];

    public static ImmutableList<string> GenericRuntimeDrops { get; } = GenericDrops
        .Where(id => !UnsupportedRuntimeDropIds.Contains(id))
        .ToImmutableList();

    public static ImmutableList<string> HighValueDrops { get; private set; } =[
        Weapon,
        DlcCoin,
        BirthdaySkill,
        LockPick,
        RepairKit,
    ];

    public static ImmutableArray<string> BirthdaySkillIds { get; } =
        new int[]{
                /* 1  Infinite Ammo */
                2 /* Health Regen */,
                /*3  Clairvoyance (Perma Psychostimulants) */
                /* 4, 5, 6, 7 (Time Bonuses) */
                8 /* Defense II */,
                9 /* Defense I */,
                10 /* Speed Up II */,
                11 /* Speed Up I */,
                12 /* Firepower Up II */,
                13 /* Firepower Up I */,
                14 /* Impact II */,
                15 /* Impact I */,
                16 /* Toughness II */,
                17 /* Toughness I */,
                18 /* Guard Up */,
                19 /* Quick Reload */,
                /* 20 (Masochist) */
                21 /* Vengeance */,
                22 /* Narrow Escape */,
                23 /* Brawler */,
            }.Select(index => $"skl{index:000}")
            .ToImmutableArray();

    public static ImmutableArray<(string Id, uint MinDropRate, uint MaxDropRate)> DlcCoinDrops { get; } =[
        ("GoodLuckCoinA_Buy", 3u, 5u), // Defense Coin
        ("GoodLuckCoinB_Buy", 3u, 5u), // Attack Coin
        ("GoodLuckCoinC_Buy", 5u, 10u), // Instinct Coin
        ("GoodLuckCoinD_Buy", 10u, 15u), // Reload Coin
        ("GoodLuckCoinE_Buy", 1u, 3u), // Universal Coin
    ];

    public const string BirthdaySkillVisualTemplateFallback = "Herb";

    public static string GetHighValueDropLabel(string highValueDrop) => highValueDrop switch{
        DlcCoin => "DLC Coin",
        BirthdaySkill => "Jack's 55th Birthday Skill",
        _ => highValueDrop.Replace("-", " ").ToTitleCase()
    };

    public static List<string> GetEnabledValuableDrops() =>[
        RepairKit, LockPick
    ];

    public static double GetDefaultGenericDropRatio(string id)
        => _defaultGenericDropRatios.GetValueOrDefault(id, 0.5);

    public static string GetConfigId(string id) => id.ToLowerInvariant().Replace('_', '-');

    // Categories
    public const string None = "None";
    public const string CategoryAmmo = "Ammo";
    public const string CategoryHealth = "Health";
    public const string CategoryExplosive = "Explosive";
    public const string CategoryMaterial = "Material";
    public const string CategoryCoin = "Coin";
    public const string CategoryOther = "Other";
    public const string CategoryNone = "None";

    // High value drops
    public const string Weapon = "weapon";
    public const string DlcCoin = "dlc-coin";
    public const string BirthdaySkill = "birthday-skill";
    public const string LockPick = "lock-pick";
    public const string RepairKit = "repair-kit";

    public static bool IsBirthdaySkill(string id)
        => id.StartsWith("skl", StringComparison.OrdinalIgnoreCase)
           && !id.EndsWith("no", StringComparison.OrdinalIgnoreCase);

    public static uint GetValuableDropRate(string highValueDrop) => highValueDrop switch{
        Weapon => 1u,
        _ => 3u
    };

    public static int GetValuableDropCount(string highValueDrop) => 1;

    public static string ToItemID(string highValueDrop) => highValueDrop switch{
        LockPick => ItemID.CylinderKey.ToString(),
        RepairKit => "RepairKit",
        _ => ItemID.NoName.ToString()
    };

    public static string GetCategory(string id) => id switch{
        "NoName" => CategoryNone,
        "CH9_WP003" or "CH9_WP004" => CategoryAmmo,
        "LiquidBomb" or "Grenadebomb" or "Thermatebomb" or "Stangrenadebomb" or "CH9_WP005" => CategoryExplosive,
        "HandgunBullet" => CategoryAmmo,
        "HandgunBulletL" => CategoryAmmo,
        "ShotgunBullet" => CategoryAmmo,
        "MachineGunBullet" => CategoryAmmo,
        "MagnumBullet" => CategoryAmmo,
        "BurnerBullet" => CategoryAmmo,
        "FlameBulletS" => CategoryAmmo,
        "AcidBulletS" => CategoryAmmo,
        "RemedyM" => CategoryHealth,
        "RemedyL" => CategoryHealth,
        "EyeDrops" => CategoryHealth,
        "Stimulant" => CategoryHealth,
        "Depressant" => CategoryHealth,
        "Herb" => CategoryHealth,
        "ChemicalM" => CategoryMaterial,
        "ChemicalL" => CategoryMaterial,
        "ChemicalS" => CategoryMaterial,
        "Gunpowder" => CategoryMaterial,
        "Alcohol" => CategoryMaterial,
        "Flower" => CategoryMaterial,
        "AlloyClay" => CategoryMaterial,
        "Magnesium" => CategoryMaterial,
        "SyntheticDetergent" => CategoryMaterial,
        "GoodLuckCoinA" => CategoryCoin,
        "GoodLuckCoinB" => CategoryCoin,
        "GoodLuckCoinC" => CategoryCoin,
        "GoodLuckCoinD" => CategoryCoin,
        "GoodLuckCoinE" => CategoryCoin,
        "PowerUpCoin01A" => CategoryCoin,
        "PowerUpCoin01B" => CategoryCoin,
        "Coin" => CategoryCoin,
        _ => CategoryOther
    };

    public static (string BackgroundColor, string TextColor) GetColor(string category) {
        return category switch{
            CategoryAmmo => ("#66f", "#fff"),
            CategoryHealth => ("#696", "#fff"),
            CategoryExplosive => ("#833", "#fff"),
            CategoryMaterial => ("#866", "#000"),
            CategoryCoin => ("#ff0", "#000"),
            CategoryNone => ("#333", "#fff"),
            _ => ("#ddd", "#000"),
        };
    }
}
