using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using System.IO.Compression;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerBirdCageBehaviorTests {
    private static readonly string[] BirdCageScenePaths =[
        RandomizerTestPaths.BirdCageScenePath,
        "leveldesign/itemset/chapter3/mainhouse_hall/hard.scn".SceneFile(),
        "environment/scene/chapter4/c04_cottage.scn".SceneFile(),
        "leveldesign/itemset/chapter4/shipoutside/hard.scn".SceneFile(),
    ];

    [Fact]
    public void BirdCageModifier_MagnumOption_ChangesMagnumBirdCageRewards() {
        using var result = RandomizerTest.RunState(config => { config["random-bird-cage-magnum"] = true; });

        var before = RandomizerTestHelpers
            .GetBirdCageStates(result.ReadBeforeScene(RandomizerTestPaths.BirdCageScenePath))
            .Where(x => x.ItemId == "Magnum")
            .ToArray();
        var after = RandomizerTestHelpers
            .GetBirdCageStates(result.ReadAfterScene(RandomizerTestPaths.BirdCageScenePath))
            .Where(x => before.Select(b => b.ContainerGuid).Contains(x.ContainerGuid))
            .ToArray();

        Assert.True(result.WasFileModified(RandomizerTestPaths.BirdCageScenePath));
        Assert.NotEmpty(before);
        Assert.All(after, entry => {
            var original = before.Single(x => x.ContainerGuid == entry.ContainerGuid);
            Assert.NotEqual(original.ItemId, entry.ItemId);
        });
    }

    [Fact]
    public void BirdCageModifier_BothOptions_DoesNotDuplicateChangedRewards() {
        using var result = RandomizerTest.RunState(config => {
            config["random-bird-cage-magnum"] = true;
            config["random-bird-cage-drugs-coins"] = true;
        });

        var changed = GetChangedBirdCageStates(result);

        Assert.NotEmpty(changed);
        Assert.Equal(
            changed.Count,
            changed.Select(state => state.ItemId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void BirdCageModifier_SkipsAlreadyPlacedWeaponRewardsWhenAlternativesExist() {
        using var result = RandomizerTest.RunState(
            config => { config["random-bird-cage-drugs-coins"] = true; },
            prepareRandomizer: randomizer => {
                randomizer.ItemRandomizer.MarkItemPlaced("MachineGun");
                randomizer.DynamicData.SetData(
                    DynamicDataName.BirdCages,
                    System.Text.Encoding.UTF8.GetBytes("""
                                                           Enabled,ItemId,MinAmount,MaxAmount,CoinsMin,CoinsMax,InputItemIds
                                                           true,MachineGun,1,1,7,7,Coin CoinOld
                                                           true,MiaKnife,1,1,3,3,Coin CoinOld
                                                           true,Herb,3,6,2,2,Coin CoinOld
                                                           true,RemedyM,2,4,2,2,Coin CoinOld
                                                           true,RemedyL,1,3,3,3,Coin CoinOld
                                                           true,Gunpowder,3,6,2,2,Coin CoinOld
                                                           true,ChemicalS,2,4,2,2,Coin CoinOld
                                                           true,ShotgunBullet,8,12,3,3,Coin CoinOld
                                                           true,HandgunBullet,15,25,2,2,Coin CoinOld
                                                           true,HandgunBulletL,10,15,2,2,Coin CoinOld
                                                           true,MagnumBullet,4,6,7,7,Coin CoinOld
                                                       """));
            });

        var changed = GetChangedBirdCageStates(result);

        Assert.NotEmpty(changed);
        Assert.DoesNotContain(changed, state => state.ItemId == "MachineGun");
    }

    [Fact]
    public void BirdCageModifier_UsesConfiguredCoinRange() {
        using var result = RandomizerTest.RunState(
            config => { config["random-bird-cage-magnum"] = true; },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(
                    DynamicDataName.BirdCages,
                    System.Text.Encoding.UTF8.GetBytes("""
                                                       Enabled,ItemId,MinAmount,MaxAmount,CoinsMin,CoinsMax,InputItemIds
                                                       true,Herb,1,1,4,6,Coin CoinOld
                                                       """));
            });

        var changed = GetChangedBirdCageStates(result);

        Assert.NotEmpty(changed);
        Assert.All(changed, state => {
            Assert.Equal("Herb", state.ItemId);
            Assert.InRange(state.CoinCount, 4, 6);
        });
    }

    [Fact]
    public void BirdCageModifier_BirthdaySkillRewards_UseOverlayVisuals() {
        var scenePath = "leveldesign/itemset/chapter3/mainhouse_hall/hard.scn".SceneFile();
        using var result = RandomizerTest.RunState(
            config => { config["random-bird-cage-drugs-coins"] = true; },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(
                    DynamicDataName.BirdCages,
                    System.Text.Encoding.UTF8.GetBytes("""
                                                       Enabled,ItemId,MinAmount,MaxAmount,CoinsMin,CoinsMax,InputItemIds
                                                       true,skl002,1,1,3,3,Coin CoinOld
                                                       """));
            });

        var before = RandomizerTestHelpers.GetBirdCageStates(result.ReadBeforeScene(scenePath))
            .ToDictionary(state => state.ContainerGuid);
        var beforeScene = result.ReadBeforeScene(scenePath);
        var afterScene = result.ReadAfterScene(scenePath);
        var changed = RandomizerTestHelpers.GetBirdCageStates(afterScene)
            .Where(state =>
                before.TryGetValue(state.ContainerGuid, out var beforeState) &&
                beforeState.ItemId != state.ItemId)
            .ToArray();
        Assert.True(BirthdaySkillVisuals.TryGetResources("skl002", out var visuals));

        Assert.NotEmpty(changed);
        Assert.All(changed, state => {
            Assert.Equal("skl002", state.ItemId);
            var mesh = GetBirdCageItemMesh(afterScene, state.ContainerGuid);
            var beforeTransform = GetBirdCageItemHolder(beforeScene, state.ContainerGuid)
                .FindComponent<GeneratedViaTransform>();
            var afterTransform = GetBirdCageItemHolder(afterScene, state.ContainerGuid)
                .FindComponent<GeneratedViaTransform>();

            Assert.Equal(visuals.Mesh, ((RszResourceNode)mesh["Mesh"]).Value);
            Assert.Equal(visuals.Material, ((RszResourceNode)mesh["Material"]).Value);
            Assert.NotNull(beforeTransform);
            Assert.NotNull(afterTransform);
            AssertQuaternionEquals(
                BirthdaySkillVisuals.CorrectRotation(beforeTransform!.Rotation),
                afterTransform!.Rotation);
        });
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/sm9959_skillpatch02.mesh.220128762",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/skl002/skl002.mdf2.21",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/skl002/skl002_ALBM.tex.35",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/skl002/skl002_ATOS.tex.35",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/skl002/skl002_NRMR.tex.35",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/ui/ui0100/tex/ui0105_iam.tex.35", result.AdditionalAssetFiles.Keys);
        Assert.DoesNotContain(result.ChangedFiles.Keys, IsBirthdaySkillOverlayAsset);
        Assert.Contains("natives/stm/props/sm9959_skillpatch02/skl008/skl008.mdf2.21",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9958_skillpatch01/skl001/skl001.mdf2.21",
            result.AdditionalAssetFiles.Keys);
        Assert.Contains("natives/stm/props/sm9960_skillpatch03/skl003/skl003.mdf2.21",
            result.AdditionalAssetFiles.Keys);
        Assert.Equal(
            GetBirthdayOverlayEntryPaths(),
            result.AdditionalAssetFiles.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void BirdCageModifier_BirthdaySkillRewards_IncludeAssetsInFluffyAndSeparatePatchDownload() {
        using var randomizer = CreateBirthdaySkillBirdCageRandomizer();
        var output = randomizer.Randomize();
        var additionalAsset = output.Assets.SingleOrDefault(asset => asset.Key == "3-assets");
        var overlayPath = "natives/stm/props/sm9959_skillpatch02/skl002/skl002.mdf2.21";
        var unusedOverlayPath = "natives/stm/props/sm9960_skillpatch03/skl003/skl003.mdf2.21";

        Assert.NotNull(additionalAsset);
        Assert.Contains("assets", additionalAsset!.FileName);

        using var patchZip = output.Assets.Single(asset => asset.Key == "1-patch").Data.Unzip();
        using var fluffyZip = output.Assets.Single(asset => asset.Key == "2-fluffy").Data.Unzip();
        using var additionalZip = additionalAsset.Data.Unzip();
        using var patchPak = new PakFile(patchZip.Entries.Single(entry => entry.Name.EndsWith(".pak")).GetBytes());
        var additionalPakEntry = additionalZip.Entries.Single(entry => entry.Name == "re_chunk_000.pak.patch_002.pak");
        using var additionalPak = new PakFile(additionalPakEntry.GetBytes());

        Assert.Null(patchPak.GetEntryData(overlayPath));
        Assert.NotNull(additionalPak.GetEntryData(overlayPath));
        Assert.NotNull(additionalPak.GetEntryData(unusedOverlayPath));

        foreach (var path in GetBirthdayOverlayEntryPaths()) {
            var entry = fluffyZip.GetEntry(path);
            Assert.NotNull(entry);
            Assert.Equal(additionalPak.GetEntryData(path), entry.GetBytes());
        }
        foreach (var (path, bytes) in randomizer.FileRepository.GetOutputFilesSnapshot()) {
            Assert.Equal(bytes, Assert.IsType<ZipArchiveEntry>(fluffyZip.GetEntry(path)).GetBytes());
        }
        foreach (var entry in patchZip.Entries.Where(entry => entry.FullName.StartsWith("reframework/"))) {
            Assert.Equal(entry.GetBytes(), Assert.IsType<ZipArchiveEntry>(fluffyZip.GetEntry(entry.FullName)).GetBytes());
        }
    }

    [Fact]
    public void BirdCageModifier_BirthdaySkillRewards_EmitAdditionalAssetsWhenPreservingItemModels() {
        using var randomizer =
            CreateBirthdaySkillBirdCageRandomizer(config => { config["preserve-item-models"] = true; });
        var output = randomizer.Randomize();
        var additionalAsset = output.Assets.SingleOrDefault(asset => asset.Key == "3-assets");

        Assert.NotNull(additionalAsset);
    }

    private static List<BirdCageState> GetChangedBirdCageStates(RandomizerRunResult result) {
        var changed = new List<BirdCageState>();
        foreach (var scenePath in BirdCageScenePaths) {
            var before = RandomizerTestHelpers.GetBirdCageStates(result.ReadBeforeScene(scenePath))
                .ToDictionary(state => state.ContainerGuid);
            var after = RandomizerTestHelpers.GetBirdCageStates(result.ReadAfterScene(scenePath));

            changed.AddRange(after.Where(state =>
                before.TryGetValue(state.ContainerGuid, out var beforeState) &&
                (beforeState.ItemId != state.ItemId ||
                 beforeState.ItemCount != state.ItemCount ||
                 beforeState.CoinCount != state.CoinCount)));
        }

        return changed;
    }

    private static Randomizer CreateBirthdaySkillBirdCageRandomizer(
        Action<RandomizerConfiguration>? configure = null) {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["random-bird-cage-drugs-coins"] = true;
            configure?.Invoke(config);
        });
        var input = new RandomizerInput{
            Seed = 4342338,
            UserName = "behavior-tests",
            ProfileName = "Behavior Tests",
            ProfileAuthor = "xUnit",
            ProfileDescription = "Randomizer behavior test profile.",
            Configuration = configuration
        };
        var randomizer = new Randomizer(input, RandomizerTest.InputPakPath, new EmptyReporter());
        randomizer.CaptureStateLogs = false;
        randomizer.DynamicData.SetData(
            DynamicDataName.BirdCages,
            System.Text.Encoding.UTF8.GetBytes("""
                                               Enabled,ItemId,MinAmount,MaxAmount,CoinsMin,CoinsMax,InputItemIds
                                               true,skl002,1,1,3,3,Coin CoinOld
                                               """));
        randomizer.DynamicData.SetData(
            DynamicDataName.EnemyLimits,
            System.Text.Encoding.UTF8.GetBytes("SceneFile,MaxEnemies,Comment\r\n"));
        return randomizer;
    }

    private static RszObjectNode GetBirdCageItemMesh(RszScene scene, Guid containerGuid) {
        var itemHolder = GetBirdCageItemHolder(scene, containerGuid);
        var mesh = itemHolder.FindComponent("via.render.Mesh");
        Assert.NotNull(mesh);
        return mesh!;
    }

    private static RszGameObject GetBirdCageItemHolder(RszScene scene, Guid containerGuid) {
        var container = scene.FindGameObject(containerGuid);
        Assert.NotNull(container);
        return container!.Children.Single(child => child.FindComponent<app.Item>() != null);
    }

    private static bool IsBirthdaySkillOverlayAsset(string path)
        => path.StartsWith("natives/stm/props/sm995", StringComparison.OrdinalIgnoreCase) ||
           path.Equals("natives/stm/ui/ui0100/tex/ui0105_iam.tex.35", StringComparison.OrdinalIgnoreCase);

    private static string[] GetBirthdayOverlayEntryPaths() {
        using var zip = new ZipArchive(
            new MemoryStream(EmbeddedData.GetFile("silver_birthday_patches.zip")),
            ZipArchiveMode.Read);
        return zip.Entries
            .Where(entry => entry.Length != 0)
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AssertQuaternionEquals(Quaternion expected, Quaternion actual) {
        if (Quaternion.Dot(expected, actual) < 0) {
            actual = new Quaternion(-actual.X, -actual.Y, -actual.Z, -actual.W);
        }

        Assert.InRange(MathF.Abs(actual.X - expected.X), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.Y - expected.Y), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.Z - expected.Z), 0, 0.0001f);
        Assert.InRange(MathF.Abs(actual.W - expected.W), 0, 0.0001f);
    }
}
