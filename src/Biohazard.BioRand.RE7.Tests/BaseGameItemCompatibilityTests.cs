using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public sealed class BaseGameItemCompatibilityTests {
    [Fact]
    public void BaseGameMode_RejectsConflictingDlcItemAndWeaponOptions() {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["allow-dlc-items"] = true;
            config["allow-bonus-items"] = true;
            config[DlcCampaignWeapons.ConfigKey] = true;
            config["item-drop-valuable-dlc-coin"] = true;
            config["item-drop-valuable-birthday-skill"] = true;
            config["enemy-drop-valuable-dlc-coin"] = true;
            config["enemy-drop-valuable-birthday-skill"] = true;
        });
        using var randomizer = new Randomizer(new RandomizerInput { Configuration = configuration },
            "unused", new EmptyReporter());

        Assert.False(DlcCampaignWeapons.IsEnabled(randomizer));
        foreach (var item in ItemDefinitionRepository.Default.Items.Where(item => item.Dlc != null)) {
            Assert.False(randomizer.ItemRandomizer.IsItemAllowed(item));
            Assert.False(randomizer.ItemRandomizer.IsGeneralDropAvailable(item.Id));
        }
        Assert.True(randomizer.ItemRandomizer.IsItemAllowed(ItemDefinitionRepository.Default.FromId("Herb")!));
        Assert.Empty(randomizer.ItemRandomizer.GetValuableDrops(randomizer.GetRng("item-drops"), "item-drop"));
        Assert.Empty(randomizer.ItemRandomizer.GetValuableDrops(randomizer.GetRng("enemy-drops"), "enemy-drop"));

        foreach (var id in ItemDrops.GenericDrops) {
            configuration[$"item-drop-ratio-{ItemDrops.GetConfigId(id)}"] = DlcCampaignWeapons.Contains(id) ? 1.0 : 0.0;
        }
        var bag = randomizer.ItemRandomizer.CreateGeneralItemPool(
            randomizer.StaticItemRandomizationService.RandomItemSettings, randomizer.GetRng("general-drops"));
        Assert.Equal("Herb", bag.Next());
    }

    [Fact]
    public void BaseGameMode_DlcPatchesDoNotReadOrWriteSourceAssets() {
        var context = new BaseGamePatchContext();
        new BirthdaySkillInventoryPatch(context).Apply();
        new InventoryManagementPatch(context).Apply();
        new DlcCampaignWeaponPatch(context).Apply();
    }

    [Fact]
    [Trait("Category", "RequiresPak")]
    public void BaseGameMode_ConflictingSkillsCagesAndWeaponStatsDoNotEmitDlcAssets() {
        using var result = RandomizerTest.RunState(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["allow-dlc-items"] = true;
            config[DlcCampaignWeapons.ConfigKey] = true;
            config["random-starting-inventory-skills-ethan"] = true;
            config["random-starting-inventory-skills-mia"] = true;
            config["random-bird-cage-magnum"] = true;
            config["random-bird-cage-drugs-coins"] = true;
            config["inventory-unrestricted-management"] = true;
            config["weapon-mod-ammo-capacity"] = true;
            config["weapon-mod-reload-speed"] = true;
            foreach (var stat in WeaponModifier.GunStatRandomizations) config[stat.ToggleConfigId] = true;
        });

        Assert.False(result.WasFileModified(RandomizerTestPaths.BirthdayResourceItemSettingsPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.KeyItemSettingsPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.ItemResourcesScenePath));
        foreach (var path in new[] { RandomizerTestPaths.EthanInventoryPath, RandomizerTestPaths.MiaInventoryPath }) {
            Assert.Equal(result.ReadBeforeBytes(path), result.ReadAfterBytes(path));
        }
        Assert.Equal(
            RandomizerTestHelpers.GetBirdCageStates(result.ReadBeforeScene(RandomizerTestPaths.BirdCageScenePath)),
            RandomizerTestHelpers.GetBirdCageStates(result.ReadAfterScene(RandomizerTestPaths.BirdCageScenePath)));

        foreach (var id in new[] { Enums.app.WeaponID.BlueBlaster, Enums.app.WeaponID.RedBlaster, Enums.app.WeaponID.HyperBlaster }) {
            var definition = WeaponDefinitionRepository.Default.FromWeaponId(id);
            Assert.NotNull(definition.UserParamsPath);
            Assert.False(result.WasFileModified(definition.UserParamsPath!));
            Assert.True(double.IsNaN(result.Randomizer.GetConfigOption(
                $"weapon-reload-speed-multiplier-{id.ToString().ToLowerInvariant()}", double.NaN)));
        }
        Assert.DoesNotContain(result.ChangedFiles.Keys, path => path.Contains("/prefab/skill/", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/biorand/dlcweapons/", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(result.AdditionalAssetFiles);
    }

    private sealed class BaseGamePatchContext : IPatchContext {
        public RszTypeRepository TypeRepository => throw new InvalidOperationException("Base-game mode must not load DLC types.");
        public DynamicData DynamicData => throw new InvalidOperationException("Base-game mode must not load DLC data.");
        public bool ExportingMod => false;
        public byte[]? GetFile(string path) => throw new InvalidOperationException($"Unexpected DLC read: {path}");
        public byte[]? GetSupplementFile(string path) => throw new InvalidOperationException($"Unexpected DLC supplement: {path}");
        public void SetFile(string path, byte[] data) => throw new InvalidOperationException($"Unexpected DLC write: {path}");
        public T? GetConfigOption<T>(string key, T? defaultValue = default)
            => typeof(T) == typeof(bool) ? (T)(object)true : defaultValue;
    }
}
