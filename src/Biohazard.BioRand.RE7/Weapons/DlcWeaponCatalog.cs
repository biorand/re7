using System.Collections.Immutable;
using Biohazard.BioRand.RE7.REEngine;

namespace Biohazard.BioRand.RE7.Weapons;

/// <summary>Source inventory, not a list of certified campaign weapons.</summary>
public static class DlcWeaponCatalog {
    public static ImmutableArray<DlcWeaponSource> Weapons { get; } = [
        new("CKnife", "Tactical Knife", 8, 48, "app.Weapon", DlcWeaponAdapter.Knife),
        new("Handgun_Albert_C", "Samurai Edge - AW Model-01", 8, 49, "app.CH8WeaponGun", DlcWeaponAdapter.Gun),
        new("Shotgun_Albert", "Thor's Hammer - AW Model-02", 8, 50, "app.CH8WeaponGun", DlcWeaponAdapter.Gun),
        new("Grenadebomb", "Grenade", 8, 58, "app.CH8WeaponThrowable", DlcWeaponAdapter.Grenade),
        new("Thermatebomb", "Incendiary Grenade", 8, 59, "app.CH8WeaponThrowable", DlcWeaponAdapter.Grenade),
        new("Stangrenadebomb", "Neuro-stun Grenade", 8, 60, "app.CH8WeaponThrowable", DlcWeaponAdapter.Grenade),
        new("CH9_WP000", "AMG-78a", 9, 61, "app.CH9Weapon1600", DlcWeaponAdapter.Gauntlet),
        new("CH9_WP001", "AMG-78", 9, 62, "app.CH9Weapon1600", DlcWeaponAdapter.Gauntlet),
        new("CH9_WP002", "Spirit Blade", 9, 63, "app.CH9Weapon1700", DlcWeaponAdapter.SpiritBlade),
        new("CH9_WP003", "Throwing Knife", 9, 64, "app.CH9Weapon1500", DlcWeaponAdapter.Ch9Throwable),
        new("CH9_WP004", "Throwing Spear", 9, 65, "app.CH9Weapon1800", DlcWeaponAdapter.Ch9Throwable),
        new("CH9_WP005", "Stake Bomb", 9, 66, "app.CH9Weapon1900", DlcWeaponAdapter.Ch9Item),
        new("CH9_WP006", "AMG-Dual", 9, 67, "app.CH9Weapon1600", DlcWeaponAdapter.Gauntlet),
        // This inventory ID deliberately shares Shotgun_DB's native WeaponID.
        new("NumaItem072", "Joe's M21", 9, 13, "app.CH9WeaponGun", DlcWeaponAdapter.Gun),
    ];

    public static string SettingsPath(int chapter) =>
        $"ch{chapter}/prefab/item/resourceitemsettings_chapter{chapter}.user".UserFile();
}

public sealed record DlcWeaponSource(
    string ItemId, string Name, int Chapter, int WeaponId, string ComponentType, DlcWeaponAdapter Adapter) {
    public bool IsLabCandidate => Adapter != DlcWeaponAdapter.None;
    public bool IsCampaignCandidate => Adapter is DlcWeaponAdapter.Knife or DlcWeaponAdapter.Gun or DlcWeaponAdapter.SpiritBlade or DlcWeaponAdapter.Gauntlet or DlcWeaponAdapter.Grenade or DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item;
    public bool IsStackWeapon => Adapter is DlcWeaponAdapter.Grenade or DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item;
    public string ResourceRoot { get; init; } = "BioRand/DlcWeaponLab";
    public string CampaignPrefab => $"{ResourceRoot}/{ItemId}/Item.pfb";
    public string ResourceScene => $"{ResourceRoot}/{ItemId}/Resource.scn";
    public string DetailPrefab => $"{ResourceRoot}/{ItemId}/Detail.pfb";
    public string ParameterPath => $"{ResourceRoot}/{ItemId}/Parameter.user";
    public string CollisionPath => $"{ResourceRoot}/{ItemId}/Attack.rcol";
    public string SourceResourceScene => ItemId switch {
        "CKnife" => "ch8/scenes/items/resources_chapter8/chrisknife.scn",
        "Handgun_Albert_C" => "ch8/scenes/items/resources_chapter8/chrishandgun.scn",
        "Shotgun_Albert" => "ch8/scenes/items/resources_chapter8/chrisshotgun.scn",
        "Grenadebomb" => "ch8/scenes/items/resources_chapter8/grenadebomb.scn",
        "Thermatebomb" => "ch8/scenes/items/resources_chapter8/thermatebomb.scn",
        "Stangrenadebomb" => "ch8/scenes/items/resources_chapter8/stangrenadebomb.scn",
        "NumaItem072" => "ch9/scenes/items/resource/numaitem072.scn",
        "CH9_WP002" => "ch9/scenes/items/resource/ch9_wp002.scn",
        "CH9_WP003" => "ch9/scenes/items/resource/ch9_wp003.scn",
        "CH9_WP004" => "ch9/scenes/items/resource/ch9_wp004.scn",
        "CH9_WP005" => "ch9/scenes/items/resource/ch9_wp005.scn",
        "CH9_WP000" => "ch9/scenes/items/resource/ch9_wp000.scn",
        "CH9_WP001" => "ch9/scenes/items/resource/ch9_wp001.scn",
        "CH9_WP006" => "ch9/scenes/items/resource/ch9_wp006.scn",
        _ => throw new InvalidOperationException($"No campaign resource adapter for {ItemId}."),
    };
}

public enum DlcWeaponAdapter { None, Knife, Gun, SpiritBlade, Gauntlet, Grenade, Ch9Throwable, Ch9Item }
