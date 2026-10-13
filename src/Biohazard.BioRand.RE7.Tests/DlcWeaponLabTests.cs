using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Weapons;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public sealed class DlcWeaponLabTests {
    [Fact]
    [Trait("Category", "RequiresPak")]
    public void Ch9ProjectileExportPreservesNativePoolsAndIsolatesAttackDataDeterministically() {
        const string root = "BioRand/Research/CH9Projectiles";
        using var first = new LabContext(true);
        using var second = new LabContext(true);
        DlcCh9Projectiles.ExportShellManager(first, root);
        DlcCh9Projectiles.ExportShellManager(second, root);
        Assert.Equal(9, first.Files.Count);
        Assert.Equal(first.Files.Keys.Order(), second.Files.Keys.Order());
        foreach (var file in first.Files) {
            Assert.StartsWith(root.Of().ToLowerInvariant() + "/", file.Key);
            Assert.Equal(file.Value, second.Files[file.Key]);
        }
        var managerFile = first.GetPfbFile(DlcCh9Projectiles.ManagerPrefab(root).Of() + ".17");
        var manager = Assert.Single(managerFile.ReadScene(first.TypeRepository).GetGameObjects());
        Assert.Equal("BioRandCH9ShellManager", manager.Name);
        Assert.True(string.IsNullOrEmpty(manager.Prefab));
        Assert.Equal(new[] { "via.Transform", "app.CH9ShellManager" }, manager.Components.Select(c => c.Type.Name));
        foreach (var (name, field, component, collisionName) in DlcCh9Projectiles.Sources) {
            var shellPath = $"{root}/{name}/Shell.pfb";
            Assert.Equal(shellPath, manager.Components[1].Get<RszResourceNode>(field + ".Path").Value);
            Assert.True(manager.Components[1].Get<bool>(field + ".Standby"));
            Assert.Contains(shellPath, managerFile.Resources);
            var shell = first.GetPfbFile(shellPath.Of() + ".17");
            var collision = $"CH9/Collision/Collider/Weapon/{collisionName}.rcol";
            Assert.Contains($"{root}/{name}/Attack.rcol", shell.Resources);
            Assert.DoesNotContain(shell.Resources, p => string.Equals(p, collision, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(first.GetSourceFile(collision.RcolFile()), first.GetFile($"{root}/{name}/Attack.rcol".RcolFile()));
            var components = shell.ReadScene(first.TypeRepository).GetGameObjects().SelectMany(go => go.Components).ToArray();
            Assert.Single(components, c => c.Type.Name == component);
            Assert.Single(components, c => c.Type.Name == "app.Collision.HitController");
            Assert.Single(components, c => c.Type.Name == "via.physics.RequestSetCollider");
            if (name is "NailKnifeBulletS" or "HarpoonBulletS") {
                var weapon = Assert.Single(components, c => c.Type.Name == "app.CH9WeaponThrowable");
                Assert.False(weapon.Get<bool>("Enabled"));
                Assert.False(weapon.Get<bool>("IsInventoryWeapon"));
                Assert.Equal(1, weapon.Get<int>("UseType"));
                Assert.Equal(name == "NailKnifeBulletS" ? 64 : 65, weapon.Get<int>("WeaponID"));
            }
            if (name is "HarpoonBulletS" or "JoeLiquidbomb")
                Assert.Single(components, c => c.Type.Name == "app.CH9InteractWeapon");
        }
    }

    [Theory]
    [Trait("Category", "RequiresPak")]
    [InlineData("nailknifebullets")]
    [InlineData("harpoonbullets")]
    [InlineData("wp1900/wp1900")]
    [InlineData("knucklebullets")]
    public void Ch9ProjectileExportFailsWithoutPartialWrites(string collision) {
        using var context = new LabContext(true, $"ch9/collision/collider/weapon/{collision}.rcol".RcolFile());
        Assert.Throws<InvalidDataException>(() => DlcCh9Projectiles.ExportShellManager(context, "BioRand/Research/CH9Projectiles"));
        Assert.Empty(context.Files);
    }

    [Fact]
    public void Ch9ThrowableManifestIncludesAllNativePoolDependenciesAndItemResources() {
        var paths = DlcCh9Projectiles.RequiredAssetPaths;
        Assert.Equal(89, paths.Length);
        Assert.Equal(paths.Order(StringComparer.Ordinal), paths);
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var (_, _, _, collision) in DlcCh9Projectiles.Sources)
            Assert.Contains($"CH9/Collision/Collider/Weapon/{collision}.rcol".RcolFile().ToLowerInvariant(), paths);
        foreach (var id in new[] { "ch9_wp003", "ch9_wp004", "ch9_wp005" })
            Assert.Contains($"natives/stm/ch9/scenes/items/resource/{id}.scn.20", paths);
        foreach (var motion in new[] { "nailknife", "bangstick", "liquidbomb" })
            Assert.Contains(paths, p => p.Contains($"pl9000_{motion}.motlist."));
        Assert.All(paths, p => {
            Assert.StartsWith("natives/stm/", p);
            Assert.Matches(@"\.\d+(\.stm)?$", p);
            Assert.DoesNotContain("..", p);
        });
    }

    [Fact]
    public void GrenadeManifestIncludesNativePoolHandPosesAndSoundDependencies() {
        var paths = DlcGrenadeWeapons.RequiredAssetPaths;
        Assert.Equal(80, paths.Length);
        Assert.Equal(paths.Order(StringComparer.Ordinal), paths);
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var pose in new[] { "grenade", "grenadebomb", "thermatebomb", "stangrenadebomb" })
            Assert.Contains($"natives/stm/ch8/animation/player/pl1000/motlist/pl1000_{pose}.motlist.524", paths);
        Assert.Contains("natives/stm/ch8/prefab/weapon/defaultbullet.pfb.17", paths);
        foreach (var model in new[] { "wp3000", "wp3010", "wp3020" }) {
            foreach (var suffix in new[] { "", "_exp" }) {
                Assert.Contains($"natives/stm/sound/resource/snd_container/snd_container_chp8_{model}{suffix}.wcc.2", paths);
                Assert.Contains($"natives/stm/sound/resource/snd_eventlist_chp8_{model}{suffix}.wel.11", paths);
                Assert.Contains($"natives/stm/sound/wwise/chp8_{model}{suffix}.bnk.2.stm", paths);
            }
        }
        Assert.Contains("natives/stm/sound/resource/snd_chp8_system_id_intaract_exceptional.wel.11", paths);
        Assert.Contains("natives/stm/sound/wwise/chp8_system_id_intaract_exceptional.bnk.2.stm", paths);
        Assert.Contains("natives/stm/sound/resource/snd_rigidbody/snd_rigidbodylist_wp3000_shell.wcrb.5", paths);
        Assert.Contains("natives/stm/sound/resource/snd_eventlist_wp3000_shell.wel.11", paths);
        Assert.All(paths, p => {
            Assert.StartsWith("natives/stm/", p);
            Assert.Matches(@"\.\d+(\.stm)?$", p);
            Assert.DoesNotContain("..", p);
        });
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void GrenadeShellExportIsIsolatedAndDeterministic() {
        const string root = "BioRand/DlcWeapons";
        using var first = new LabContext(true);
        using var second = new LabContext(true);
        DlcGrenadeWeapons.ExportShellManager(first, root);
        DlcGrenadeWeapons.ExportShellManager(second, root);
        Assert.Equal(7, first.Files.Count);
        Assert.Equal(first.Files.Keys.Order(), second.Files.Keys.Order());
        foreach (var file in first.Files) {
            Assert.StartsWith(root.Of().ToLowerInvariant() + "/", file.Key);
            Assert.Equal(file.Value, second.Files[file.Key]);
        }
        var managerFile = first.GetPfbFile(DlcGrenadeWeapons.ManagerPrefab(root).Of() + ".17");
        var manager = Assert.Single(managerFile.ReadScene(first.TypeRepository).GetGameObjects());
        Assert.Equal("BioRandGrenadeShellManager", manager.Name);
        Assert.True(string.IsNullOrEmpty(manager.Prefab));
        Assert.Equal(new[] { "via.Transform", "app.CH8ShellManager" }, manager.Components.Select(c => c.Type.Name));
        var component = manager.Components[1];
        Assert.Equal("CH8/Prefab/Weapon/DefaultBullet.pfb", component.Get<RszResourceNode>("DefaultBulletPrefab.Path").Value,
            ignoreCase: true);
        foreach (var (id, model, field) in DlcGrenadeWeapons.Sources) {
            var shellPath = $"{root}/{id}/Shell.pfb";
            Assert.Equal(shellPath, component.Get<RszResourceNode>(field + ".Path").Value);
            Assert.True(component.Get<bool>(field + ".Standby"));
            Assert.Contains(shellPath, managerFile.Resources);
            var shell = first.GetPfbFile(shellPath.Of() + ".17");
            var collision = $"CH8/Collision/Collider/Weapon/{model[..6]}/{model}.rcol";
            Assert.Contains($"{root}/{id}/Attack.rcol", shell.Resources);
            Assert.DoesNotContain(shell.Resources, p => string.Equals(p, collision, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(first.GetSourceFile(collision.RcolFile()), first.GetFile($"{root}/{id}/Attack.rcol".RcolFile()));
            Assert.Contains(shell.ReadScene(first.TypeRepository).GetGameObjects().SelectMany(go => go.Components),
                c => c.Type.Name == "app.CH8Throwable");
        }
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void GrenadeShellExportFailsWithoutPartialWrites() {
        using var context = new LabContext(true, "ch8/collision/collider/weapon/wp3020/wp3020_stangrenadebomb.rcol".RcolFile());
        Assert.Throws<InvalidDataException>(() => DlcGrenadeWeapons.ExportShellManager(context, "BioRand/DlcWeapons"));
        Assert.Empty(context.Files);
    }

    [Fact]
    public void CatalogExcludesUnsupportedCampaignWeapons() {
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Length);
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Select(w => w.ItemId).Distinct().Count());
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Count(w => w.IsLabCandidate));
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Count(w => w.IsCampaignCandidate));
        var unsupported = DlcWeaponCatalog.Weapons[0] with { Adapter = DlcWeaponAdapter.None };
        Assert.False(unsupported.IsLabCandidate);
        Assert.False(unsupported.IsCampaignCandidate);
        Assert.False(unsupported.IsStackWeapon);
        Assert.Contains(DlcCampaignWeapons.Sources, w => w.ItemId == "CH9_WP002");
        Assert.True(DlcWeaponCatalog.Weapons.Single(w => w.ItemId == "CH9_WP006").IsLabCandidate);
        Assert.True(DlcWeaponCatalog.Weapons.Single(w => w.ItemId == "CH9_WP006").IsCampaignCandidate);
        foreach (var weapon in DlcWeaponCatalog.Weapons.Where(w => !w.IsCampaignCandidate))
            Assert.Null(ItemDefinitionRepository.Default.FromId(weapon.ItemId));
    }

    [Fact]
    public void OrdinaryGenerationDoesNotReadOrWriteLabAssets() {
        using var context = new LabContext(false);
        new DlcWeaponLabPatch(context).Apply();
        Assert.Empty(context.Files);
        Assert.Equal(0, context.Reads);
    }

    [Fact]
    public void LabManifestIncludesColdLoadGauntletDependencies() {
        var paths = DlcWeaponLabPatch.RequiredAssetPaths;
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(paths.Order(StringComparer.Ordinal), paths);
        Assert.All(paths, p => {
            Assert.StartsWith("natives/stm/", p);
            Assert.Matches(@"\.\d+(\.stm)?$", p);
            Assert.DoesNotContain("..", p);
        });
        foreach (var hand in new[] { "pl9010", "pl9020" }) {
            Assert.Contains($"natives/stm/ch9/character/player/pl9000/{hand}/{hand}.mesh.220128762", paths);
            Assert.Contains($"natives/stm/ch9/character/player/pl9000/{hand}/{hand}.mdf2.21", paths);
        }
        Assert.Contains(paths, p => p.Contains("/streaming/ch9/") && p.Contains("gauntlet"));
        Assert.Contains(paths, p => p.Contains("pl9000_gauntletw.motlist."));
        Assert.Contains(paths, p => p.Contains("pl9000_gauntlet.motlist."));
        Assert.Contains(paths, p => p.Contains("pl9000_gauntletr.motlist."));
        Assert.Contains(paths, p => p.Contains("pl9000_knuckle.motlist."));
        Assert.Contains(paths, p => p.Contains("pl9010_hand_recordsys_rtt.rtex."));
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void MissingLabAssetFailsBeforeWritingAnything() {
        var path = "natives/stm/ch9/character/player/pl9000/pl9010/pl9010.mesh.220128762";
        using var context = new LabContext(true, path);
        var error = Assert.Throws<RandomizerUserException>(() => new DlcWeaponLabPatch(context).Apply());
        Assert.Contains(path, error.Message);
        Assert.Empty(context.Files);
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void ExportPreservesSourcesAndRegistersOnlyCandidatesDeterministically() {
        using var first = new LabContext(true);
        using var second = new LabContext(true);
        new DlcWeaponLabPatch(first).Apply();
        new DlcWeaponLabPatch(second).Apply();
        Assert.Equal(first.Files.Keys.Order(), second.Files.Keys.Order());
        foreach (var file in first.Files) Assert.Equal(file.Value, second.Files[file.Key]);

        var campaign = first.DeserializeUserFile<app.ItemSettings>("prefab/item/resourceitemsettings.user".UserFile());
        var messages = first.GetMsgFile("message/ui_item_mes.msg".MessageFile());
        foreach (var weapon in DlcWeaponCatalog.Weapons) {
            var item = campaign._Settings.SingleOrDefault(x => x.ItemDataID == weapon.ItemId);
            if (!weapon.IsLabCandidate) {
                Assert.Null(item);
                continue;
            }
            Assert.NotNull(item);
            Assert.True(item.CanStoreItembox);
            Assert.NotNull(messages.FindMessage(item.NameMsg));
            if (item.ManualMsg != Guid.Empty) Assert.NotNull(messages.FindMessage(item.ManualMsg));
            Assert.Equal(weapon.CampaignPrefab, item.ItemPrefab.Path.ToString());
            var path = weapon.CampaignPrefab.Of() + ".17";
            var scene = first.GetPfbFile(path).ReadScene(first.TypeRepository);
            var components = scene.GetGameObjects().SelectMany(go => go.Components).ToArray();
            if (weapon.Adapter == DlcWeaponAdapter.Gauntlet) {
                Assert.Equal(2, components.Count(c => c.Type.Name == "via.render.Mesh"));
                foreach (var handName in new[] { "BioRandGauntletHandR", "BioRandGauntletHandL" }) {
                    var hand = Assert.Single(scene.GetGameObjects(), g => g.Name == handName);
                    Assert.False(hand.Settings.Get<bool>("Draw"));
                    Assert.Equal(new[] { "via.Transform", "via.render.Mesh" }, hand.Components.Select(c => c.Type.Name));
                    Assert.False(hand.Components.Single(c => c.Type.Name == "via.render.Mesh").Get<bool>("DrawDefault"));
                }
                Assert.Single(components, c => c.Type.Name == "app.Collision.HitController");
                Assert.Single(components, c => c.Type.Name == "via.physics.RequestSetCollider");
                Assert.Contains(weapon.CollisionPath, first.GetPfbFile(path).Resources);
                var resources = first.GetPfbFile(path).Resources;
                foreach (var hand in new[] { "pl9010", "pl9020" }) {
                    Assert.Contains($"CH9/Character/Player/pl9000/{hand}/{hand}.mesh", resources);
                    Assert.Contains($"CH9/Character/Player/pl9000/{hand}/{hand}.mdf2", resources);
                }
                var collision = first.GetRcolFile(weapon.CollisionPath.RcolFile()).ToBuilder(first.TypeRepository);
                Assert.Equal(Enumerable.Range(0, 7), collision.RequestSets.Select(r => r.Id));
                int[] damage = weapon.WeaponId switch {
                    67 => [300, 300, 300, 300, 500, 3000, 500],
                    61 => [100, 150, 150, 450, 150, 300, 500],
                    62 => [100, 150, 150, 450, 300, 700, 2000],
                    _ => throw new InvalidOperationException(),
                };
                Assert.Equal(damage, collision.RequestSets.Select(r => r.UserData!.Get<int>("Damage")));
                Assert.Equal(weapon.WeaponId == 67 ? "AttackBodyblowR_Double" : "AttackUppercutR", collision.RequestSets[2].Name);
                Assert.Equal(weapon.WeaponId == 67 ? "AttackStraightV2_Double" : weapon.WeaponId == 61
                    ? "Attack3ChargeLeftReword" : "Attack3ChargeLeft", collision.RequestSets[6].Name);
                if (weapon.WeaponId == 61)
                    Assert.Equal(new[] { 100, 200, 300 }, collision.RequestSets.Skip(4).Select(r => r.UserData!.Get<int>("Stun")));
                Assert.Contains(collision.Groups.SelectMany(g => g.Shapes), s => s.PrimaryJointName == "R_UpperArm");
                Assert.Contains(collision.Groups.SelectMany(g => g.Shapes), s => s.PrimaryJointName == "L_UpperArm");
                var weaponRoot = scene.GetGameObjects().Single(g => g.Components.Any(c => c.Type.Name == "app.Weapon"));
                Assert.True(weaponRoot.Components.Single(c => c.Type.Name == "via.Transform").Get<bool>("SameJointsContraint"));
                Assert.Empty(components.Single(c => c.Type.Name == "app.Weapon").Get<string>("EquipParam.JointName"));
            } else {
                Assert.Contains(components, c => c.Type.Name == "via.render.Mesh");
                if (weapon.Adapter is not (DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item)) {
                    Assert.Contains(components, c => c.Type.Name == "via.motion.Motion");
                    Assert.Contains(components, c => c.Type.Name == "via.motion.MotionFsm");
                }
                Assert.Contains(first.GetPfbFile(path).Resources, p => p.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase));
            }
            Assert.DoesNotContain(components, c => (c.Type.Name.StartsWith("app.CH8") || c.Type.Name.StartsWith("app.CH9"))
                && !(weapon.Adapter == DlcWeaponAdapter.Grenade && c.Type.Name == "app.CH8WeaponThrowable")
                && !(weapon.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item && c.Type.Name == weapon.ComponentType)
                && !(weapon.Adapter == DlcWeaponAdapter.Ch9Item && c.Type.Name == "app.CH9WeaponLiquidBombAppend"));
            Assert.DoesNotContain(components, c => c.Type.Name == "app.DisableSave");
            var native = Assert.Single(components, c => c.Type.Name == (weapon.Adapter switch {
                DlcWeaponAdapter.Gun => "app.WeaponGun",
                DlcWeaponAdapter.Grenade => "app.CH8WeaponThrowable",
                DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item => weapon.ComponentType,
                _ => "app.Weapon",
            }));
            Assert.Equal(weapon.WeaponId, native.Get<int>("WeaponID"));
            Assert.Equal(weapon.ItemId, Assert.Single(components, c => c.Type.Name == "app.Item").Get<string>("ItemDataID"));
            if (weapon.IsStackWeapon) {
                Assert.Equal(6, item.MaxStackNum);
                var folder = weapon.Adapter is DlcWeaponAdapter.Ch9Throwable or DlcWeaponAdapter.Ch9Item
                    ? DlcCh9Projectiles.ProjectileName(weapon.WeaponId) : weapon.ItemId;
                Assert.Contains($"{weapon.ResourceRoot}/{folder}/Shell.pfb".Of() + ".17", first.Files.Keys);
            }
            if (weapon.Adapter == DlcWeaponAdapter.Gun) {
                var bullets = native.Get<RszArrayNode>("BulletInfoList").Cast<RszObjectNode>().Select(x => x.Get<int>("BulletItemID")).ToArray();
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertHandgunBullet, bullets);
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertShotgunBullet, bullets);
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertHandgunBulletL, bullets);
                Assert.Contains((int)(weapon.ItemId == "Handgun_Albert_C" ? Enums.app.ItemID.HandgunBullet : Enums.app.ItemID.ShotgunBullet), bullets);
            }
        }
        // Only unchanged manifest assets may be emitted under the source DLC namespace.
        Assert.All(first.Files.Keys.Where(p => p.StartsWith("natives/stm/ch8/") || p.StartsWith("natives/stm/ch9/")),
            p => Assert.Contains(p, DlcWeaponLabPatch.RequiredAssetPaths));
        foreach (var path in DlcWeaponLabPatch.RequiredAssetPaths)
            Assert.Equal(first.GetSourceFile(path), first.Files[path]);
        var resourceIndex = first.GetScnFile("scenes/items/itemresources.scn".SceneFile()).ReadScene(first.TypeRepository);
        foreach (var weapon in DlcWeaponCatalog.Weapons.Where(w => w.IsLabCandidate)) {
            var folder = Assert.Single(resourceIndex.Children.OfType<RszFolder>(), f => f.Name == weapon.ItemId);
            Assert.Equal(weapon.ResourceScene, folder.Settings.Get<RszResourceNode>("ScenePath").Value);
            var resource = first.GetScnFile(weapon.ResourceScene.SceneFile()).ReadScene(first.TypeRepository);
            var component = Assert.Single(resource.GetGameObjects().SelectMany(go => go.Components), c => c.Type.Name == "app.ItemResource");
            Assert.Equal(weapon.ItemId, component.Get<string>("_ItemDataId"));
            Assert.Equal(weapon.DetailPrefab, component.Get<RszResourceNode>("_ResourcePrefab.Path").Value);
            Assert.True(component.Get<bool>("_ResourcePrefab.Standby"));
            Assert.NotEmpty(first.GetPfbFile(weapon.DetailPrefab.Of() + ".17").ReadScene(first.TypeRepository).GetGameObjects());
        }
        Assert.Equal(64 + DlcWeaponLabPatch.RequiredAssetPaths.Length, first.Files.Count);
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void SourceCatalogMatchesActualInventoryPrefabs() {
        using var context = new LabContext(true);
        foreach (var weapon in DlcWeaponCatalog.Weapons) {
            var source = context.DeserializeUserFile<app.ItemSettings>(DlcWeaponCatalog.SettingsPath(weapon.Chapter));
            var item = Assert.Single(source._Settings, x => x.ItemDataID == weapon.ItemId);
            var scene = context.GetPfbFile(item.ItemPrefab.Path.ToString()!.Of() + ".17").ReadScene(context.TypeRepository);
            var native = Assert.Single(scene.GetGameObjects().SelectMany(go => go.Components), c => c.Type.Name == weapon.ComponentType);
            Assert.Equal(weapon.WeaponId, native.Get<int>("WeaponID"));
            Assert.Throws<InvalidOperationException>(() => DlcWeaponLabPatch.AdaptPrefab(scene, context.TypeRepository,
                weapon with { Adapter = DlcWeaponAdapter.None }));
        }
    }

    private sealed class LabContext(bool exporting, string? missingAsset = null) : IPatchContext, IDisposable {
        private PakFile? _pak;
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int Reads { get; private set; }
        public RszTypeRepository TypeRepository => FileRepository.RszRepository;
        public DynamicData DynamicData { get; } = new(download: false);
        public bool ExportingMod => exporting;
        public T? GetConfigOption<T>(string key, T? defaultValue = default) => defaultValue;
        public byte[]? GetSupplementFile(string path) => throw new NotSupportedException();
        public byte[]? GetFile(string path) {
            Reads++;
            if (!exporting) throw new InvalidOperationException("The lab must not read assets during generation.");
            return Files.GetValueOrDefault(path) ?? GetSourceFile(path);
        }
        public byte[]? GetSourceFile(string path) {
            if (path == missingAsset) return null;
            // Opaque render assets are not required in the embedded test baseline.
            return (_pak ??= new PakFile(RandomizerTest.InputPakPath)).GetEntryData(path)
                ?? (DlcWeaponLabPatch.RequiredAssetPaths.Contains(path) ? [1, 2, 3] : null);
        }
        public void SetFile(string path, byte[] data) => Files[path] = data;
        public void Dispose() => _pak?.Dispose();
    }
}
