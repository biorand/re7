using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Weapons;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public sealed class DlcWeaponLabTests {
    [Fact]
    public void CatalogIsNotACampaignPool() {
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Length);
        Assert.Equal(14, DlcWeaponCatalog.Weapons.Select(w => w.ItemId).Distinct().Count());
        Assert.Equal(4, DlcWeaponCatalog.Weapons.Count(w => w.IsLabCandidate));
        Assert.False(DlcWeaponCatalog.Weapons.Single(w => w.ItemId == "CH9_WP006").IsLabCandidate);
        foreach (var weapon in DlcWeaponCatalog.Weapons)
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
            Assert.Contains(components, c => c.Type.Name == "via.render.Mesh");
            Assert.Contains(components, c => c.Type.Name == "via.motion.Motion");
            Assert.Contains(components, c => c.Type.Name == "via.motion.MotionFsm");
            Assert.Contains(first.GetPfbFile(path).Resources, p => p.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(components, c => c.Type.Name.StartsWith("app.CH8") || c.Type.Name.StartsWith("app.CH9"));
            Assert.DoesNotContain(components, c => c.Type.Name == "app.DisableSave");
            var native = Assert.Single(components, c => c.Type.Name == (weapon.Adapter == DlcWeaponAdapter.Gun ? "app.WeaponGun" : "app.Weapon"));
            Assert.Equal(weapon.WeaponId, native.Get<int>("WeaponID"));
            Assert.Equal(weapon.ItemId, Assert.Single(components, c => c.Type.Name == "app.Item").Get<string>("ItemDataID"));
            if (weapon.Adapter == DlcWeaponAdapter.Gun) {
                var bullets = native.Get<RszArrayNode>("BulletInfoList").Cast<RszObjectNode>().Select(x => x.Get<int>("BulletItemID")).ToArray();
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertHandgunBullet, bullets);
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertShotgunBullet, bullets);
                Assert.DoesNotContain((int)Enums.app.ItemID.AlbertHandgunBulletL, bullets);
                Assert.Contains((int)(weapon.ItemId == "Handgun_Albert_C" ? Enums.app.ItemID.HandgunBullet : Enums.app.ItemID.ShotgunBullet), bullets);
            }
        }
        Assert.DoesNotContain(first.Files.Keys, p => p.StartsWith("natives/stm/ch8/") || p.StartsWith("natives/stm/ch9/"));
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
        Assert.Equal(15, first.Files.Count);
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
            if (!weapon.IsLabCandidate)
                Assert.Throws<InvalidOperationException>(() => DlcWeaponLabPatch.AdaptPrefab(scene, context.TypeRepository, weapon));
        }
    }

    private sealed class LabContext(bool exporting) : IPatchContext, IDisposable {
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
            return Files.GetValueOrDefault(path) ?? (_pak ??= new PakFile(RandomizerTest.InputPakPath)).GetEntryData(path);
        }
        public void SetFile(string path, byte[] data) => Files[path] = data;
        public void Dispose() => _pak?.Dispose();
    }
}
