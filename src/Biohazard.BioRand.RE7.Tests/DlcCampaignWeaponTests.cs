using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Services;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using Enums.app;

namespace Biohazard.BioRand.RE7.Tests;

public sealed class DlcCampaignWeaponTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PoolRequiresBothPermissions(bool allowDlc, bool enableWeapons) {
        using var randomizer = new Randomizer(new RandomizerInput {
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(c => {
                c["allow-dlc-items"] = allowDlc;
                c[DlcCampaignWeapons.ConfigKey] = enableWeapons;
            }),
        }, "unused", new EmptyReporter());
        foreach (var source in DlcCampaignWeapons.Sources)
            Assert.Equal(allowDlc && enableWeapons, randomizer.ItemRandomizer.IsItemAllowed(
                ItemDefinitionRepository.Default.FromId(source.ItemId)!));
        var stackWeapons = DlcCampaignWeapons.Sources.Where(w => w.IsStackWeapon).Select(w => w.ItemId).ToArray();
        foreach (var id in ItemDrops.GenericDrops)
            randomizer.Input.Configuration[$"item-drop-ratio-{ItemDrops.GetConfigId(id)}"] = stackWeapons.Contains(id) ? 1.0 : 0.0;
        var settings = randomizer.StaticItemRandomizationService.RandomItemSettings;
        foreach (var id in stackWeapons) Assert.Equal(1.0, settings.ItemRatioKeyFunc!(id));
        var bag = randomizer.ItemRandomizer.CreateGeneralItemPool(settings, randomizer.GetRng("dlc-grenade-drops"));
        var drops = Enumerable.Range(0, stackWeapons.Length).Select(_ => bag.Next()).ToArray();
        if (allowDlc && enableWeapons) Assert.Equal(stackWeapons.Order(), drops.Order());
        else Assert.All(drops, id => Assert.Equal("Herb", id));
        if (allowDlc && enableWeapons) Assert.True(randomizer.IsREFrameworkRequired());
        Assert.False(RandomizerExecutor.DefaultConfiguration.GetValueOrDefault<bool>(DlcCampaignWeapons.ConfigKey));
    }

    [Fact]
    public void NativeAliasAndAmmoAreExplicit() {
        Assert.Equal(WeaponID.Shotgun_DB, ItemDefinitionRepository.Default.FromId("NumaItem072")!.WeaponId);
        Assert.Equal("Shotgun_DB", ItemDefinitionRepository.Default.FromWeaponId(WeaponID.Shotgun_DB)!.Id);
        Assert.Single(WeaponDefinitionRepository.Default.WeaponDefinitions, w => w.WeaponId == WeaponID.Shotgun_DB);
        Assert.Equal([ItemID.HandgunBullet, ItemID.HandgunBulletL], WeaponDefinitionRepository.Default.GetAmmoTypes(WeaponID.Handgun_Albert_C));
        Assert.Equal([ItemID.ShotgunBullet], WeaponDefinitionRepository.Default.GetAmmoTypes(WeaponID.Shotgun_Albert));
        Assert.DoesNotContain(ItemID.Handgun_Albert_C, StartingWeaponCategory.Handgun.GetItemIds());
        Assert.Contains(ItemID.Handgun_Albert_C, StartingWeaponCategory.Handgun.GetItemIds(true));
        Assert.DoesNotContain(ItemID.CH9_WP002, StartingWeaponCategory.Bladed.GetItemIds());
        Assert.Contains(ItemID.CH9_WP002, StartingWeaponCategory.Bladed.GetItemIds(true));
        var blade = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)63);
        Assert.False(blade.IsGun);
        Assert.Equal("Spirit Blade", blade.Name);
        Assert.NotNull(blade.BulletItemIDs);
        Assert.Empty(blade.BulletItemIDs);
        foreach (var id in new[] { ItemID.CH9_WP000, ItemID.CH9_WP001, ItemID.CH9_WP006 }) {
            Assert.DoesNotContain(id, StartingWeaponCategory.Bladed.GetItemIds());
            Assert.Contains(id, StartingWeaponCategory.Bladed.GetItemIds(true));
            var definition = WeaponDefinitionRepository.Default.FromWeaponId(ItemDefinitionRepository.Default.FromId(id.ToString())!.WeaponId!.Value);
            Assert.False(definition.IsGun);
            Assert.Null(definition.UserParamsPath);
            Assert.Empty(definition.BulletItemIDs!);
            Assert.Equal(7, definition.Damage.Count);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("Category", "RequiresPak")]
    public void StackWeaponReliefDropsRequireBothPermissions(bool allowDlc, bool enableWeapons) {
        using var state = RandomizerTest.RunState();
        var randomizer = state.Randomizer;
        randomizer.Input.Configuration["allow-dlc-items"] = allowDlc;
        randomizer.Input.Configuration[DlcCampaignWeapons.ConfigKey] = enableWeapons;
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.IsStackWeapon))
            randomizer.Input.Configuration[$"item-drop-ratio-{ItemDrops.GetConfigId(source.ItemId)}"] = 0.03;
        new ItemDropTableModifier(randomizer).Apply(new RandomizerLogger());
        var table = randomizer.FileRepository.DeserializeUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.IsStackWeapon)) {
            var entries = table.DataList.Where(d => d.ItemID == source.ItemId).ToArray();
            if (allowDlc && enableWeapons) {
                var drop = Assert.Single(entries);
                Assert.Equal((3u, 3u, 3u), (drop.EasyDropRate, drop.NormalDropRate, drop.HardDropRate));
                Assert.Equal((1u, 1u, 1u), (drop.ReliefNum, drop.NormalDropNum, drop.ReliefDropNum));
            } else Assert.Empty(entries);
        }
    }

    [Fact]
    public void ManifestIsScopedAndVersioned() {
        var paths = DlcCampaignWeapons.RequiredAssetPaths;
        Assert.NotEmpty(paths);
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(paths, p => {
            Assert.StartsWith("natives/stm/", p);
            Assert.Matches(@"\.\d+(\.stm)?$", p);
            Assert.DoesNotContain("..", p);
            Assert.DoesNotContain("/ingame/", p);
        });
        Assert.Contains(paths, p => p.Contains("wp1330") && p.Contains(".mesh."));
        Assert.Contains(paths, p => p.Contains("wp1340") && p.Contains(".motlist."));
        Assert.Contains(paths, p => p.Contains("wp1390") && p.Contains(".rcol."));
        Assert.Contains(paths, p => p.Contains("wp1700") && p.Contains(".rcol."));
        Assert.Contains(paths, p => p.Contains("wp1600_gauntlet") && p.Contains(".mesh."));
        Assert.Contains(paths, p => p.Contains("wp1620_gauntlet") && p.Contains(".mesh."));
        Assert.Contains(paths, p => p.Contains("pl9000_gauntletw.motlist."));
        Assert.All(DlcGrenadeWeapons.RequiredAssetPaths, path => Assert.Contains(path, paths));
        Assert.All(DlcCh9Projectiles.RequiredAssetPaths, path => Assert.Contains(path, paths));
    }

    [Fact]
    public void GrenadesKeepNativeStackCategoryAndIsolatedProjectileStats() {
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter == DlcWeaponAdapter.Grenade)) {
            var item = ItemDefinitionRepository.Default.FromId(source.ItemId)!;
            Assert.Equal(Enums.app.Item.ItemCategoryType.StackWeapon, item.CategoryType);
            Assert.True(item.IsWeapon);
            Assert.Equal(6, item.MaxStack);
            var itemId = Enum.Parse<ItemID>(source.ItemId);
            Assert.DoesNotContain(itemId, StartingWeaponCategory.Bomb.GetItemIds());
            Assert.Contains(itemId, StartingWeaponCategory.Bomb.GetItemIds(true));
            Assert.Contains(source.ItemId, ItemDrops.GenericDrops);
            Assert.Contains(source.ItemId, ItemDrops.GenericRuntimeDrops);
            Assert.Equal(ItemDrops.CategoryExplosive, ItemDrops.GetCategory(source.ItemId));
            var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
            Assert.False(definition.IsGun);
            Assert.Null(definition.UserParamsPath);
            Assert.Empty(definition.BulletItemIDs!);
            Assert.Equal(source.CollisionPath.RcolFile(), Assert.Single(definition.RcolPaths));
            Assert.Contains(definition.Mesh.Of() + ".220128762", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(definition.Material.Of() + ".21", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Ch9ThrowablesKeepStackCategoryAndIsolatedProjectileStats() {
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter == DlcWeaponAdapter.Ch9Throwable)) {
            var item = ItemDefinitionRepository.Default.FromId(source.ItemId)!;
            Assert.Equal(Enums.app.Item.ItemCategoryType.StackWeapon, item.CategoryType);
            Assert.True(item.IsWeapon);
            Assert.Equal(6, item.MaxStack);
            Assert.DoesNotContain(Enum.Parse<ItemID>(source.ItemId), StartingWeaponCategory.Bladed.GetItemIds(true));
            Assert.Contains(source.ItemId, ItemDrops.GenericDrops);
            Assert.Contains(source.ItemId, ItemDrops.GenericRuntimeDrops);
            Assert.Equal(ItemDrops.CategoryAmmo, ItemDrops.GetCategory(source.ItemId));
            var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
            Assert.False(definition.IsGun);
            Assert.Null(definition.UserParamsPath);
            Assert.Empty(definition.BulletItemIDs!);
            Assert.Equal(DlcCh9Projectiles.WeaponCollisionPath(source).RcolFile(), Assert.Single(definition.RcolPaths));
            Assert.Contains(definition.Mesh.Of() + ".220128762", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
            Assert.Contains(definition.Material.Of() + ".21", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void StakeBombKeepsExplosiveStackAndNativeInstallationDamage() {
        var source = DlcCampaignWeapons.Sources.Single(w => w.ItemId == "CH9_WP005");
        Assert.Equal(DlcWeaponAdapter.Ch9Item, source.Adapter);
        var item = ItemDefinitionRepository.Default.FromId(source.ItemId)!;
        Assert.Equal(Enums.app.Item.ItemCategoryType.StackWeapon, item.CategoryType);
        Assert.True(item.IsWeapon);
        Assert.Equal(6, item.MaxStack);
        Assert.DoesNotContain(ItemID.CH9_WP005, StartingWeaponCategory.Bomb.GetItemIds());
        Assert.Contains(ItemID.CH9_WP005, StartingWeaponCategory.Bomb.GetItemIds(true));
        Assert.Contains(source.ItemId, ItemDrops.GenericDrops);
        Assert.Contains(source.ItemId, ItemDrops.GenericRuntimeDrops);
        Assert.Equal(ItemDrops.CategoryExplosive, ItemDrops.GetCategory(source.ItemId));
        var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
        Assert.False(definition.IsGun);
        Assert.Null(definition.UserParamsPath);
        Assert.Empty(definition.BulletItemIDs!);
        Assert.Equal(DlcCh9Projectiles.WeaponCollisionPath(source).RcolFile(), Assert.Single(definition.RcolPaths));
        Assert.Equal(4, definition.Damage.Count);
        Assert.Contains(definition.Mesh.Of() + ".220128762", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(definition.Material.Of() + ".21", DlcCampaignWeapons.RequiredAssetPaths, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void MissingAssetsFailBeforeRegisteringWeapons() {
        using var state = RandomizerTest.RunState();
        var randomizer = state.Randomizer;
        randomizer.Input.Configuration[DlcCampaignWeapons.ConfigKey] = true;
        // Use a known missing resource regardless of whether the developer's baseline was upgraded.
        using var missing = new MissingAssetContext(randomizer.FileRepository);
        var exception = Assert.Throws<RandomizerUserException>(() => new DlcCampaignWeaponPatch(missing).Apply());
        Assert.Contains("setup --dlc-weapons", exception.Message);
        Assert.Equal(0, missing.Writes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "RequiresPak")]
    public void CampaignRegistrationTemplatesAndStatsUseNativeIdentities(bool includeSelfDamage) {
        using var state = RandomizerTest.RunState();
        var randomizer = state.Randomizer;
        var repository = randomizer.FileRepository;
        randomizer.Input.Configuration[DlcCampaignWeapons.ConfigKey] = true;
        // Render/animation blobs are opaque to generation. Tests never need a local game install.
        foreach (var path in DlcCampaignWeapons.RequiredAssetPaths)
            if (repository.GetFile(path) == null) repository.SetAdditionalOutputAssetFile(path, [1, 2, 3]);
        new DlcCampaignWeaponPatch(repository).Apply();
        var blade = DlcCampaignWeapons.Sources.Single(w => w.ItemId == "CH9_WP002");
        var originalBladeCollision = "ch9/collision/collider/weapon/wp1700/wp1700_hatchet.rcol".RcolFile();
        var sourceCollision = repository.GetFile(originalBladeCollision)!;
        var originalGauntletCollision = "ch9/collision/collider/player/pl9000/pl9000.rcol".RcolFile();
        var sourceGauntletCollision = repository.GetFile(originalGauntletCollision)!;
        var gauntletDamage = new Dictionary<string, int[]>();
        var grenadeCollisions = new Dictionary<string, byte[]>();
        var ch9Collisions = new Dictionary<string, byte[]>();
        Assert.Equal(sourceCollision, repository.GetFile(blade.CollisionPath.RcolFile()));
        var bladePrefab = repository.GetPfbFile(blade.CampaignPrefab.Of() + ".17");
        Assert.Contains(blade.CollisionPath, bladePrefab.Resources);
        Assert.DoesNotContain(bladePrefab.Resources, p => p.Contains("wp1700_hatchet.rcol", StringComparison.OrdinalIgnoreCase));
        var settings = repository.DeserializeUserFile<app.ItemSettings>("prefab/item/resourceitemsettings.user".UserFile());
        foreach (var source in DlcCampaignWeapons.Sources) {
            var item = Assert.Single(settings._Settings, i => i.ItemDataID == source.ItemId);
            Assert.Equal(source.CampaignPrefab, item.ItemPrefab.Path.ToString());
            Assert.True(item.CanStoreItembox);
            if (source.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item) {
                Assert.Equal(6, item.MaxStackNum);
                var name = DlcCh9Projectiles.ProjectileName(source.WeaponId);
                var collisionName = DlcCh9Projectiles.Sources.Single(s => s.Name == name).Collision;
                var original = $"CH9/Collision/Collider/Weapon/{collisionName}.rcol".RcolFile();
                ch9Collisions[original] = repository.GetFile(original)!;
                var collisionPath = DlcCh9Projectiles.WeaponCollisionPath(source);
                Assert.Equal(ch9Collisions[original], repository.GetFile(collisionPath.RcolFile()));
                var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
                var rcol = repository.GetRcolFile(collisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
                foreach (var request in rcol.RequestSets.Where(r => r.UserData?.Type.Name == "app.Collision.AttackUserData")) {
                    var stats = definition.Damage[$"Attack.rcol/{request.Name}"];
                    Assert.Equal(stats.Damage, request.UserData!.Get<int>("Damage"));
                    Assert.Equal(stats.Stun, request.UserData!.Get<int>("Stun"));
                }
                var shell = repository.GetPfbFile($"{source.ResourceRoot}/{name}/Shell.pfb".Of() + ".17");
                Assert.Contains(collisionPath, shell.Resources);
                var inventory = repository.GetPfbFile(source.CampaignPrefab.Of() + ".17").ReadScene(repository.TypeRepository);
                Assert.Contains(inventory.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == source.ComponentType);
                Assert.DoesNotContain(inventory.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == "app.DisableSave");
                if (source.Adapter == DlcWeaponAdapter.Ch9Item)
                    Assert.Single(inventory.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == "app.CH9WeaponLiquidBombAppend");
            }
            if (source.Adapter == DlcWeaponAdapter.Grenade) {
                Assert.Equal(6, item.MaxStackNum);
                var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
                var model = DlcGrenadeWeapons.Sources.Single(w => w.ItemId == source.ItemId).Model;
                var original = $"CH8/Collision/Collider/Weapon/{model[..6]}/{model}.rcol".RcolFile();
                grenadeCollisions[original] = repository.GetFile(original)!;
                Assert.Equal(grenadeCollisions[original], repository.GetFile(source.CollisionPath.RcolFile()));
                var rcol = repository.GetRcolFile(source.CollisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
                foreach (var request in rcol.RequestSets.Where(r => r.UserData?.Type.Name == "app.Collision.AttackUserData")) {
                    var stats = definition.Damage[$"Attack.rcol/{request.Name}"];
                    Assert.Equal(stats.Damage, request.UserData!.Get<int>("Damage"));
                    Assert.Equal(stats.Stun, request.UserData!.Get<int>("Stun"));
                }
                var inventory = repository.GetPfbFile(source.CampaignPrefab.Of() + ".17").ReadScene(repository.TypeRepository);
                Assert.Contains(inventory.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == "app.CH8WeaponThrowable");
                Assert.DoesNotContain(inventory.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == "app.DisableSave");
            }
            if (source.Adapter == DlcWeaponAdapter.Gauntlet) {
                var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
                var rcol = repository.GetRcolFile(source.CollisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
                gauntletDamage[source.ItemId] = rcol.RequestSets.Select(r => r.UserData!.Get<int>("Damage")).ToArray();
                Assert.Equal(Enumerable.Range(0, 7), rcol.RequestSets.Select(r => r.Id));
                foreach (var request in rcol.RequestSets) {
                    var stats = definition.Damage[$"Attack.rcol/{request.Name}"];
                    Assert.Equal(stats.Damage, request.UserData!.Get<int>("Damage"));
                    Assert.Equal(stats.Stun, request.UserData!.Get<int>("Stun"));
                }
                var inventory = repository.GetPfbFile(source.CampaignPrefab.Of() + ".17");
                Assert.Contains(source.CollisionPath, inventory.Resources);
                Assert.DoesNotContain(inventory.ReadScene(repository.TypeRepository).GetGameObjects().SelectMany(g => g.Components),
                    c => c.Type.Name == "app.WeaponMotionController");
            }
            var template = randomizer.TemplateService.GetItemTemplate(source.ItemId);
            var ids = new List<string>();
            template.Visit(n => {
                if (n is RszObjectNode o && o.Type.Name == "app.Item") ids.Add(o.Get<string>("ItemDataID"));
                if (n is RszObjectNode a && a.Type.Name == "app.fsm.ItemAddTest") Assert.Equal(source.ItemId, a.Get<string>("_ItemDataID"));
                if (n is RszObjectNode w && w.Type.Name is "app.WeaponGun" or "app.Weapon" or "app.CH8WeaponThrowable" or "app.CH9Weapon1500" or "app.CH9Weapon1800" or "app.CH9Weapon1900") Assert.Equal(source.WeaponId, w.Get<int>("WeaponID"));
                return n;
            });
            Assert.NotEmpty(ids);
            Assert.All(ids, id => Assert.Equal(source.ItemId, id));
            Assert.Contains(template.Components, c => c.Type.Name == "via.render.Mesh");
            var interaction = source.IsStackWeapon ? "app.InteractDetailSearch" : "app.InteractWeapon";
            Assert.True(template.Children.SelectMany(c => c.Components).Any(c => c.Type.Name == interaction), source.ItemId);
            var donor = randomizer.TemplateService.GetItemTemplate("Handgun_M19");
            var preserved = randomizer.TemplateService.RebindDlcPickup(donor, "Handgun_M19", source.ItemId);
            if (source.Adapter == DlcWeaponAdapter.Grenade)
                Assert.Contains(preserved.Components, c => c.Type.Name == "app.CH8WeaponThrowable");
            if (source.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item)
                Assert.Contains(preserved.Components, c => c.Type.Name == source.ComponentType);
            Assert.Equal(donor.Guid, preserved.Guid);
            Assert.Equal(donor.Children.Select(c => c.Guid), preserved.Children.Select(c => c.Guid));
            preserved.Visit(n => {
                if (n is RszObjectNode w && w.Type.Name is "app.WeaponGun" or "app.Weapon" or "app.CH8WeaponThrowable" or "app.CH9Weapon1500" or "app.CH9Weapon1800" or "app.CH9Weapon1900")
                    Assert.Equal(source.WeaponId, w.Get<int>("WeaponID"));
                if (n is RszObjectNode a && a.Type.Name == "app.fsm.ItemAddTest")
                    Assert.Equal(source.ItemId, a.Get<string>("_ItemDataID"));
                return n;
            });
        }
        randomizer.ItemRandomizer.MarkItemPlaced("Shotgun_DB");
        Assert.True(randomizer.ItemRandomizer.IsItemPlaced("NumaItem072"));
        randomizer.Input.Configuration["weapon-mod-ammo-capacity"] = true;
        randomizer.Input.Configuration["weapon-mod-damage"] = true;
        randomizer.Input.Configuration["weapon-mod-damage-include-player-damage"] = includeSelfDamage;
        randomizer.Input.Configuration["weapon-damage-min-ch9-wp002"] = 2.0;
        randomizer.Input.Configuration["weapon-damage-max-ch9-wp002"] = 2.0;
        foreach (var id in gauntletDamage.Keys) {
            var key = id.ToLowerInvariant().Replace('_', '-');
            randomizer.Input.Configuration[$"weapon-damage-min-{key}"] = 2.0;
            randomizer.Input.Configuration[$"weapon-damage-max-{key}"] = 2.0;
        }
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter == DlcWeaponAdapter.Grenade)) {
            var key = ((WeaponID)source.WeaponId).ToString().ToLowerInvariant();
            randomizer.Input.Configuration[$"weapon-damage-min-{key}"] = 2.0;
            randomizer.Input.Configuration[$"weapon-damage-max-{key}"] = 2.0;
        }
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item)) {
            var key = ((WeaponID)source.WeaponId).ToString().ToLowerInvariant().Replace('_', '-');
            randomizer.Input.Configuration[$"weapon-damage-min-{key}"] = 2.0;
            randomizer.Input.Configuration[$"weapon-damage-max-{key}"] = 2.0;
        }
        foreach (var definition in WeaponDefinitionRepository.Default.Guns) {
            var id = definition.WeaponId.ToString().ToLowerInvariant().Replace('_', '-');
            randomizer.Input.Configuration[$"weapon-ammo-capacity-min-{id}"] = 2.0;
            randomizer.Input.Configuration[$"weapon-ammo-capacity-max-{id}"] = 2.0;
        }
        new WeaponModifier(randomizer).Apply(new RandomizerLogger());
        Assert.Equal(sourceCollision, repository.GetFile(originalBladeCollision));
        Assert.Equal(sourceGauntletCollision, repository.GetFile(originalGauntletCollision));
        foreach (var (path, original) in grenadeCollisions) Assert.Equal(original, repository.GetFile(path));
        foreach (var (path, original) in ch9Collisions) Assert.Equal(original, repository.GetFile(path));
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item)) {
            var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
            var rcol = repository.GetRcolFile(DlcCh9Projectiles.WeaponCollisionPath(source).RcolFile()).ToBuilder(repository.TypeRepository);
            foreach (var request in rcol.RequestSets.Where(r => r.UserData?.Type.Name == "app.Collision.AttackUserData")) {
                var original = definition.Damage[$"Attack.rcol/{request.Name}"].Damage;
                Assert.Equal(request.Name.Contains("Player") && !includeSelfDamage ? original : original * 2,
                    request.UserData!.Get<int>("Damage"));
            }
        }
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter == DlcWeaponAdapter.Grenade)) {
            var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
            var rcol = repository.GetRcolFile(source.CollisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
            foreach (var request in rcol.RequestSets.Where(r => r.UserData?.Type.Name == "app.Collision.AttackUserData")) {
                var original = definition.Damage[$"Attack.rcol/{request.Name}"].Damage;
                Assert.Equal(request.Name.Contains("Player") && !includeSelfDamage ? original : original * 2,
                    request.UserData!.Get<int>("Damage"));
            }
        }
        foreach (var source in DlcCampaignWeapons.Sources.Where(w => w.Adapter == DlcWeaponAdapter.Gauntlet)) {
            var rcol = repository.GetRcolFile(source.CollisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
            Assert.Equal(gauntletDamage[source.ItemId].Select(d => d * 2), rcol.RequestSets.Select(r => r.UserData!.Get<int>("Damage")));
        }
        var collision = repository.GetRcolFile(blade.CollisionPath.RcolFile()).ToBuilder(repository.TypeRepository);
        Assert.Equal(200, collision.RequestSets.Single(r => r.Name == "AttackSmall").UserData!.Get<int>("Damage"));
        Assert.Equal(300, collision.RequestSets.Single(r => r.Name == "AttackLarge").UserData!.Get<int>("Damage"));
        foreach (var id in new[] { "Handgun_Albert_C", "Shotgun_Albert" }) {
            var source = DlcCampaignWeapons.Sources.Single(w => w.ItemId == id);
            var definition = WeaponDefinitionRepository.Default.FromWeaponId((WeaponID)source.WeaponId);
            Assert.Equal(definition.MaxLoadNum * 2, repository.DeserializeUserFile<app.WeaponGunParameter>(definition.UserParamsPath!).MaxLoadNum);
            var pfb = repository.GetPfbFile(source.CampaignPrefab.Of() + ".17").ReadScene(repository.TypeRepository);
            var gun = Assert.Single(pfb.GetGameObjects().SelectMany(g => g.Components), c => c.Type.Name == "app.WeaponGun");
            Assert.Equal(source.ParameterPath, gun.Get<RszUserDataNode>("WeaponGunParameter").Path);
            Assert.Equal(definition.MaxLoadNum * 2, gun.Get<RszArrayNode>("BulletInfoList").Cast<RszObjectNode>().First().Get<int>("LoadNum"));
        }
    }

    private sealed class MissingAssetContext(IPatchContext source) : IPatchContext, IDisposable {
        public int Writes { get; private set; }
        public RszTypeRepository TypeRepository => source.TypeRepository;
        public DynamicData DynamicData => source.DynamicData;
        public bool ExportingMod => false;
        public T? GetConfigOption<T>(string key, T? defaultValue = default) => source.GetConfigOption(key, defaultValue);
        public byte[]? GetSupplementFile(string path) => source.GetSupplementFile(path);
        public byte[]? GetFile(string path) => path == DlcCampaignWeapons.RequiredAssetPaths[0] ? null : source.GetFile(path);
        public void SetFile(string path, byte[] data) => Writes++;
        public void Dispose() { }
    }
}
