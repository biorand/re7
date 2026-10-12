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
        .Where(w => w.IsCampaignCandidate).Select(w => w with { ResourceRoot = "BioRand/DlcWeapons" })];
    public static ImmutableArray<string> RequiredAssetPaths { get; } = [.. new[] { "dlc_weapon_assets.txt", "dlc_gauntlet_assets.txt", "dlc_grenade_assets.txt", "dlc_ch9_throwable_assets.txt" }
        .SelectMany(name => System.Text.Encoding.UTF8.GetString(EmbeddedData.GetFile(name))
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal)];

    public static bool Contains(string id) => Sources.Any(w => w.ItemId == id);
    internal static bool IsEnabled(IPatchContext context)
        => DlcContent.IsAllowed(context) && context.GetConfigOption<bool>(ConfigKey)
           && context.GetConfigOption<bool>("allow-dlc-items");
    internal static bool IsEnabled(Randomizer randomizer)
        => DlcContent.IsAllowed(randomizer) && randomizer.GetConfigOption<bool>(ConfigKey)
           && randomizer.GetConfigOption<bool>("allow-dlc-items");

    internal static IEnumerable<ItemDefinition> CreateItemDefinitions() => Sources.Select(w => new ItemDefinition {
        Id = w.ItemId, Name = w.Name, CategoryType = w.IsStackWeapon ? ItemCategoryType.StackWeapon : ItemCategoryType.Weapon,
        Size = w.ItemId is "Shotgun_Albert" or "NumaItem072" ? ItemSlotSize.Slot2 : ItemSlotSize.Slot1,
        Dlc = w.Chapter == 8 ? DlcType.NotAHero : DlcType.EndOfZoe,
        WeaponId = (WeaponID)w.WeaponId, MaxStack = w.IsStackWeapon ? 6 : 1, CanStoreInItemBox = true,
        SourceUserFile = "resourceitemsettings.user.2",
    });

    internal static IEnumerable<WeaponDefinition> CreateWeaponDefinitions() {
        foreach (var source in Sources.Where(w => w.Chapter == 8 && w.Adapter != DlcWeaponAdapter.Grenade)) {
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
        foreach (var source in Sources.Where(w => w.Adapter == DlcWeaponAdapter.Grenade)) {
            var model = DlcGrenadeWeapons.Sources.Single(w => w.ItemId == source.ItemId).Model;
            yield return new WeaponDefinition {
                WeaponId = (WeaponID)source.WeaponId, Id = model[..6], Name = source.Name,
                IsGun = false, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
                BulletItemIDs = [], UserParamsPath = null,
                Mesh = $"CH8/Weapon/{model}/{model}.mesh",
                Material = $"CH8/Weapon/{model}/{model}.mdf2",
                PrefabPath = source.CampaignPrefab.Of() + ".17",
                RcolPaths = [source.CollisionPath.RcolFile()],
                Damage = source.WeaponId == 58 ? new() {
                    ["Attack.rcol/ExplosionCToEnemy"] = new() { Damage = 500, Stun = 500 },
                    ["Attack.rcol/ExplosionToEnemy"] = new() { Damage = 1500, Stun = 500 },
                    ["Attack.rcol/ExplosionCToPlayer"] = new() { Damage = 400, Stun = 250 },
                    ["Attack.rcol/ExplosionToPlayer"] = new() { Damage = 300, Stun = 150 },
                } : CreateGrenadeDamage(source.WeaponId == 59),
            };
        }
        foreach (var source in Sources.Where(w => w.Adapter == DlcWeaponAdapter.Ch9Throwable)) {
            var knife = source.WeaponId == 64;
            var folder = knife ? "wp1500_nailknife" : "wp1800_harpoon";
            var model = knife ? "wp1500" : "wp1800_harpoon";
            yield return new WeaponDefinition {
                WeaponId = (WeaponID)source.WeaponId, Id = model[..6], Name = source.Name,
                IsGun = false, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
                BulletItemIDs = [], UserParamsPath = null,
                Mesh = $"CH9/Weapon/{folder}/{model}.mesh", Material = $"CH9/Weapon/{folder}/{model}.mdf2",
                PrefabPath = source.CampaignPrefab.Of() + ".17",
                RcolPaths = [DlcCh9Projectiles.WeaponCollisionPath(source).RcolFile()],
                Damage = new() {
                    ["Attack.rcol/Impact"] = new() { Damage = knife ? 200 : 100, Stun = knife ? 50 : 100 },
                    ["Attack.rcol/AttackToWeapon"] = new() { Damage = 0, Stun = 0 },
                },
            };
        }
        foreach (var source in Sources.Where(w => w.Adapter == DlcWeaponAdapter.Ch9Item)) {
            yield return new WeaponDefinition {
                WeaponId = (WeaponID)source.WeaponId, Id = "wp1900", Name = source.Name,
                IsGun = false, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
                BulletItemIDs = [], UserParamsPath = null,
                Mesh = "CH9/Weapon/wp1900_joeliquidbomb/wp1900.mesh",
                Material = "CH9/Weapon/wp1900_joeliquidbomb/wp1900.mdf2",
                PrefabPath = source.CampaignPrefab.Of() + ".17",
                RcolPaths = [DlcCh9Projectiles.WeaponCollisionPath(source).RcolFile()],
                Damage = new() {
                    ["Attack.rcol/ExplosionCToEnemy"] = new() { Damage = 1000, Stun = 500 },
                    ["Attack.rcol/ExplosionToEnemy"] = new() { Damage = 1000, Stun = 500 },
                    ["Attack.rcol/ExplosionCToPlayer"] = new() { Damage = 400, Stun = 250 },
                    ["Attack.rcol/ExplosionToPlayer"] = new() { Damage = 300, Stun = 150 },
                },
            };
        }
        var blade = Sources.Single(w => w.Adapter == DlcWeaponAdapter.SpiritBlade);
        yield return new WeaponDefinition {
            WeaponId = (WeaponID)blade.WeaponId, Id = "wp1700", Name = blade.Name,
            IsGun = false, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
            BulletItemIDs = [], UserParamsPath = null,
            Mesh = "CH9/Weapon/wp1700_hatchet/wp1700.mesh",
            Material = "CH9/Weapon/wp1700_hatchet/wp1700.mdf2",
            PrefabPath = blade.CampaignPrefab.Of() + ".17",
            RcolPaths = [blade.CollisionPath.RcolFile()],
            Damage = new() {
                ["Attack.rcol/AttackSmall"] = new() { Damage = 100, Stun = 10 },
                ["Attack.rcol/AttackLarge"] = new() { Damage = 150, Stun = 50 },
            },
        };
        foreach (var gauntlet in Sources.Where(w => w.Adapter == DlcWeaponAdapter.Gauntlet)) {
            var model = DlcGauntletWeapons.GetModel(gauntlet.WeaponId);
            yield return new WeaponDefinition {
                WeaponId = (WeaponID)gauntlet.WeaponId,
                Id = gauntlet.WeaponId == 61 ? "wp1610" : model, Name = gauntlet.Name,
                IsGun = false, IsInventoryWeapon = true, UserType = Enums.app.CharacterDefine.Type.Player,
                BulletItemIDs = [], UserParamsPath = null,
                Mesh = $"CH9/Weapon/{model}_Gauntlet/{model}.mesh",
                Material = $"CH9/Weapon/{model}_Gauntlet/{model}.mdf2",
                PrefabPath = gauntlet.CampaignPrefab.Of() + ".17",
                RcolPaths = [gauntlet.CollisionPath.RcolFile()],
                Damage = DlcGauntletWeapons.GetAttacks(gauntlet.WeaponId).ToDictionary(
                    attack => $"Attack.rcol/{attack.Name}",
                    attack => new WeaponDamageStats { Damage = attack.Damage, Stun = attack.Stun }),
            };
        }
    }

    private static Dictionary<string, WeaponDamageStats> CreateGrenadeDamage(bool incendiary) {
        var damage = new Dictionary<string, WeaponDamageStats> {
            ["Attack.rcol/AttackToWeapon"] = new() { Damage = 0, Stun = 100 },
            ["Attack.rcol/Impact"] = new() { Damage = 0, Stun = 0 },
            ["Attack.rcol/ExplosionToEnemy"] = new() { Damage = 500, Stun = 0 },
            ["Attack.rcol/ExplosionToPlayer"] = new() { Damage = 100, Stun = 100 },
        };
        if (incendiary) damage["Attack.rcol/Residual"] = new() { Damage = 500, Stun = 0 };
        return damage;
    }
}
