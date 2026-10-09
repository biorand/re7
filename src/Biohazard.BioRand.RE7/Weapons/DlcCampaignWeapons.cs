using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Enums.app;
using Enums.app.Item;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Weapons;

public static class DlcCampaignWeapons {
    public const string ConfigKey = "dlc-campaign-weapons";
    public static ImmutableArray<DlcWeaponSource> Sources { get; } = [.. DlcWeaponCatalog.Weapons
        .Where(w => w.IsLabCandidate).Select(w => w with { ResourceRoot = "BioRand/DlcWeapons" })];
    public static ImmutableArray<string> RequiredAssetPaths { get; } = [.. System.Text.Encoding.UTF8
        .GetString(EmbeddedData.GetFile("dlc_weapon_assets.txt"))
        .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)];

    public static bool Contains(string id) => Sources.Any(w => w.ItemId == id);
    internal static bool IsEnabled(IPatchContext context)
        => context.GetConfigOption<bool>(ConfigKey) && context.GetConfigOption<bool>("allow-dlc-items");
    internal static bool IsEnabled(Randomizer randomizer)
        => randomizer.GetConfigOption<bool>(ConfigKey) && randomizer.GetConfigOption<bool>("allow-dlc-items");

    internal static IEnumerable<ItemDefinition> CreateItemDefinitions() => Sources.Select(w => new ItemDefinition {
        Id = w.ItemId, Name = w.Name, CategoryType = ItemCategoryType.Weapon,
        Size = w.ItemId is "Shotgun_Albert" or "NumaItem072" ? ItemSlotSize.Slot2 : ItemSlotSize.Slot1,
        Dlc = w.Chapter == 8 ? DlcType.NotAHero : DlcType.EndOfZoe,
        WeaponId = (WeaponID)w.WeaponId, MaxStack = 1, CanStoreInItemBox = true,
        SourceUserFile = "resourceitemsettings.user.2",
    });

    internal static IEnumerable<WeaponDefinition> CreateWeaponDefinitions() {
        foreach (var source in Sources.Where(w => w.Chapter == 8)) {
            var gun = source.Adapter == DlcWeaponAdapter.Gun;
            var family = source.ItemId switch {
                "CKnife" => "wp1390_ChrisKnife",
                "Handgun_Albert_C" => "wp1340_ChrisHandgun",
                _ => "wp1330_ChrisShotgun",
            };
            yield return new WeaponDefinition {
                WeaponId = (WeaponID)source.WeaponId, Id = family[..6], Name = source.Name,
                IsGun = gun, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
                MaxLoadNum = gun ? source.ItemId == "Handgun_Albert_C" ? 9 : 12 : 0,
                IsLoadNumInfinity = false, IsBulletStackNumInfinity = false, Range = gun ? 100 : null,
                BulletItemIDs = gun ? source.ItemId == "Handgun_Albert_C"
                    ? [ItemID.HandgunBullet, ItemID.HandgunBulletL] : [ItemID.ShotgunBullet] : [],
                Mesh = $"CH8/Weapon/{family}/{family}.mesh",
                Material = $"CH8/Weapon/{family}/{(source.ItemId == "Handgun_Albert_C" ? family : family[..6])}.mdf2",
                PrefabPath = source.CampaignPrefab.Of() + ".17",
                UserParamsPath = gun ? source.ParameterPath.UserFile() : null,
                RcolPaths = [gun ? "collision/collider/weapon/defaultbullet.rcol".RcolFile() : source.CollisionPath.RcolFile()],
                Damage = source.ItemId switch {
                    "CKnife" => new() {
                        ["Attack.rcol/AttackSmall"] = new() { Damage = 300, Stun = 50 },
                        ["Attack.rcol/AttackLarge"] = new() { Damage = 500, Stun = 50 },
                    },
                    "Handgun_Albert_C" => new() {
                        ["defaultbullet.rcol/Handgun_Albert_C"] = new() { Damage = 300, Stun = 300 },
                        ["defaultbullet.rcol/Handgun_Albert_C_L"] = new() { Damage = 1000, Stun = 1000 },
                    },
                    _ => new() { ["defaultbullet.rcol/Shotgun_Albert"] = new() { Damage = 60, Stun = 15 } },
                },
            };
        }
    }
}
