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
    }

    [Fact]
    public void ManifestIsScopedAndVersioned() {
        var paths = DlcCampaignWeapons.RequiredAssetPaths;
        Assert.NotEmpty(paths);
        Assert.Equal(paths.Length, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(paths, p => {
            Assert.StartsWith("natives/stm/", p);
            Assert.Matches(@"\.\d+$", p);
            Assert.DoesNotContain("..", p);
            Assert.DoesNotContain("/ingame/", p);
        });
        Assert.Contains(paths, p => p.Contains("wp1330") && p.Contains(".mesh."));
        Assert.Contains(paths, p => p.Contains("wp1340") && p.Contains(".motlist."));
        Assert.Contains(paths, p => p.Contains("wp1390") && p.Contains(".rcol."));
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

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void CampaignRegistrationTemplatesAndStatsUseNativeIdentities() {
        using var state = RandomizerTest.RunState();
        var randomizer = state.Randomizer;
        var repository = randomizer.FileRepository;
        randomizer.Input.Configuration[DlcCampaignWeapons.ConfigKey] = true;
        // Render/animation blobs are opaque to generation. Tests never need a local game install.
        foreach (var path in DlcCampaignWeapons.RequiredAssetPaths)
            if (repository.GetFile(path) == null) repository.SetAdditionalOutputAssetFile(path, [1, 2, 3]);
        new DlcCampaignWeaponPatch(repository).Apply();
        var settings = repository.DeserializeUserFile<app.ItemSettings>("prefab/item/resourceitemsettings.user".UserFile());
        foreach (var source in DlcCampaignWeapons.Sources) {
            var item = Assert.Single(settings._Settings, i => i.ItemDataID == source.ItemId);
            Assert.Equal(source.CampaignPrefab, item.ItemPrefab.Path.ToString());
            Assert.True(item.CanStoreItembox);
            var template = randomizer.TemplateService.GetItemTemplate(source.ItemId);
            var ids = new List<string>();
            template.Visit(n => {
                if (n is RszObjectNode o && o.Type.Name == "app.Item") ids.Add(o.Get<string>("ItemDataID"));
                if (n is RszObjectNode a && a.Type.Name == "app.fsm.ItemAddTest") Assert.Equal(source.ItemId, a.Get<string>("_ItemDataID"));
                if (n is RszObjectNode w && w.Type.Name is "app.WeaponGun" or "app.Weapon") Assert.Equal(source.WeaponId, w.Get<int>("WeaponID"));
                return n;
            });
            Assert.NotEmpty(ids);
            Assert.All(ids, id => Assert.Equal(source.ItemId, id));
            Assert.Contains(template.Components, c => c.Type.Name == "via.render.Mesh");
            Assert.True(template.Children.SelectMany(c => c.Components).Any(c => c.Type.Name == "app.InteractWeapon"), source.ItemId);
            var donor = randomizer.TemplateService.GetItemTemplate("Handgun_M19");
            var preserved = randomizer.TemplateService.RebindDlcPickup(donor, "Handgun_M19", source.ItemId);
            Assert.Equal(donor.Guid, preserved.Guid);
            Assert.Equal(donor.Children.Select(c => c.Guid), preserved.Children.Select(c => c.Guid));
            preserved.Visit(n => {
                if (n is RszObjectNode w && w.Type.Name is "app.WeaponGun" or "app.Weapon")
                    Assert.Equal(source.WeaponId, w.Get<int>("WeaponID"));
                if (n is RszObjectNode a && a.Type.Name == "app.fsm.ItemAddTest")
                    Assert.Equal(source.ItemId, a.Get<string>("_ItemDataID"));
                return n;
            });
        }
        randomizer.ItemRandomizer.MarkItemPlaced("Shotgun_DB");
        Assert.True(randomizer.ItemRandomizer.IsItemPlaced("NumaItem072"));
        randomizer.Input.Configuration["weapon-mod-ammo-capacity"] = true;
        foreach (var definition in WeaponDefinitionRepository.Default.Guns) {
            var id = definition.WeaponId.ToString().ToLowerInvariant().Replace('_', '-');
            randomizer.Input.Configuration[$"weapon-ammo-capacity-min-{id}"] = 2.0;
            randomizer.Input.Configuration[$"weapon-ammo-capacity-max-{id}"] = 2.0;
        }
        new WeaponModifier(randomizer).Apply(new RandomizerLogger());
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
