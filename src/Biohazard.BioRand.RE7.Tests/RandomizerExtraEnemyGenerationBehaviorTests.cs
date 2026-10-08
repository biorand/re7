using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerExtraEnemyGenerationBehaviorTests {
    private const string ExtraEnemyScenePath = "natives/stm/scenes/chapter/chapter1/enemy_c01.scn.20";

    private const string Chapter1EnvironmentExtraEnemyScenePath =
        "natives/stm/environment/scene/chapter1/c01_b1c.scn.20";

    private const string EnvironmentExtraEnemyScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fliving.scn.20";

    private const string SecondEnvironmentExtraEnemyScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fpantry.scn.20";

    private const string EnvironmentExtraEnemyGeneratorScenePath =
        "natives/stm/scenes/chapter/chapter3/enemy_c03.scn.20";

    private const string JackGarageHallwayScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fgaragehallway.scn.20";

    private const string JackHatchHallwayScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fhallway.scn.20";

    private const string RandomExtraEnemyScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string ExtraEnemyFsmResource = "LevelDesign/Fsm/Template/TempFsm_TriggerInAction_EnemyGenerate5.fsm";

    private static readonly uint[] ExtraEnemyGenerateActionUids =[
        2860522480,
    ];

    [Theory]
    [InlineData(0, 0.5, 0)]
    [InlineData(4, -1.0, 0)]
    [InlineData(4, 0.0, 0)]
    [InlineData(4, 0.25, 1)]
    [InlineData(3, 0.5, 2)]
    [InlineData(4, 1.0, 4)]
    [InlineData(4, 2.0, 4)]
    public void ExtraEnemies_Amount_MapsToExactSubsetCount(
        int placementCount,
        double percentage,
        int expectedCount) {
        Assert.Equal(expectedCount, ExtraEnemyPlanner.GetSubsetCount(placementCount, percentage));
    }

    [Theory]
    [InlineData(JackGarageHallwayScenePath)]
    [InlineData(JackHatchHallwayScenePath)]
    public void ExtraEnemies_DefaultData_DoesNotEnableOpeningJackSequenceScenes(string scenePath) {
        var enabledPlacements = Biohazard.BioRand.RE7.Serialization.Csv
            .Deserialize<ExtraEnemyPlacement>(EmbeddedData.GetFile("extra_enemies.csv"))
            .Where(extraEnemy => extraEnemy.Enabled &&
                                 extraEnemy.SceneFile.Equals(scenePath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Empty(enabledPlacements);
    }

    [Fact]
    public void ExtraEnemies_AddDynamicGeneratorSlots() {
        using var result = RunWithExtraEnemies();
        var beforeScene = result.ReadBeforeScene(ExtraEnemyScenePath);
        var afterScene = result.ReadAfterScene(ExtraEnemyScenePath);

        var newGameObjects = GetNewGameObjects(afterScene, beforeScene);
        var newRootGenerators = GetNewRootGameObjects(afterScene, beforeScene)
            .Where(gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName)
            .ToList();
        var newRootFsmGenerators = GetNewRootGameObjects(afterScene, beforeScene)
            .Where(IsFsmGenerationObject)
            .ToList();
        var extraSpawnInfos = GetNewExtraSpawnInfos(afterScene, beforeScene);
        var extraInstances = GetNewExtraEnemyInstances(afterScene, beforeScene);
        var extraPool = newRootGenerators.Single()
            .Children.Single(child => child.Name == EnemyModifier.ExtraEnemyPoolName);
        var extraPoolComponent = extraPool.FindComponent<app.EnemyPool>()!;

        Assert.True(result.WasFileModified(ExtraEnemyScenePath));
        Assert.Single(newRootGenerators);
        Assert.Equal(2, newRootFsmGenerators.Count);
        Assert.Equal(2, extraSpawnInfos.Count);
        Assert.Equal(2, extraInstances.Count);
        Assert.Empty(extraPoolComponent.ExternalInstancePoolRefs);
        Assert.Contains(newGameObjects, gameObject => gameObject.FindComponent<app.EnemyPool>() != null);
        Assert.DoesNotContain(newGameObjects,
            gameObject => gameObject.Name.EndsWith("_Extra", StringComparison.Ordinal));
        Assert.All(newRootFsmGenerators, AssertImmediateFsmGenerator);
        AssertEnemyGenerateRefs(extraSpawnInfos, newRootFsmGenerators);
        Assert.Equal(
            extraSpawnInfos.Select(GetSpawnInfo).Select(spawnInfo => spawnInfo.UnitAlias).Order().ToList(),
            extraInstances.Select(gameObject => gameObject.Name).Order().ToList());
        AssertPoolInstancesStayAtTemplatePosition(extraSpawnInfos, extraInstances);
        AssertEnemyStampSerializationDisabled(extraInstances);
        AssertPoolInstancesStartHidden(extraInstances);

        AssertExtraSpawnInfo(extraSpawnInfos, "Em4000", -49, 4.88f, 108, 3000);
        AssertExtraSpawnInfo(extraSpawnInfos, "Em4100", -47.92f, 4.99f, 100.86f, 900);
    }

    [Fact]
    public void ExtraEnemies_EnvironmentScene_AddsFsmUnderDynamicParentAndGeneratorToChapterScene() {
        using var result = RunWithExtraEnemies(BuildExtraEnemiesCsv(EnvironmentExtraEnemyScenePath, 3, "Em4000"));
        var beforeEnvironmentScene = result.ReadBeforeScene(EnvironmentExtraEnemyScenePath);
        var afterEnvironmentScene = result.ReadAfterScene(EnvironmentExtraEnemyScenePath);
        var beforeGeneratorScene = result.ReadBeforeScene(EnvironmentExtraEnemyGeneratorScenePath);
        var afterGeneratorScene = result.ReadAfterScene(EnvironmentExtraEnemyGeneratorScenePath);

        var dynamicParent = afterEnvironmentScene.FindGameObject(gameObject =>
            gameObject.Name.EndsWith("_dynamic", StringComparison.OrdinalIgnoreCase))!;
        var newDynamicChildren = dynamicParent.Children
            .Where(gameObject => beforeEnvironmentScene.FindGameObject(gameObject.Guid) == null)
            .ToList();
        var newEnvironmentGameObjects = GetNewGameObjects(afterEnvironmentScene, beforeEnvironmentScene);
        var newRootGenerators = GetNewRootGameObjects(afterGeneratorScene, beforeGeneratorScene)
            .Where(gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName)
            .ToList();
        var extraSpawnInfos = GetNewExtraSpawnInfos(afterGeneratorScene, beforeGeneratorScene);
        var fsmGenerator = Assert.Single(newDynamicChildren, IsFsmGenerationObject);

        Assert.True(result.WasFileModified(EnvironmentExtraEnemyScenePath));
        Assert.True(result.WasFileModified(EnvironmentExtraEnemyGeneratorScenePath));
        Assert.Single(newRootGenerators);
        Assert.DoesNotContain(newEnvironmentGameObjects,
            gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName);
        AssertImmediateFsmGenerator(fsmGenerator);
        AssertEnemyGenerateRefs(extraSpawnInfos, [fsmGenerator]);
        AssertExtraSpawnInfo(extraSpawnInfos, "Em4000", -50, 5, 100, 3000);
    }

    [Fact]
    public void ExtraEnemies_MultipleEnvironmentScenesInSameChapter_ShareChapterGenerator() {
        var extraEnemiesCsv = $"""
                               Enabled,Id,Comment,SceneFile,Chapter,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW
                               TRUE,Em4000,First environment extra,{EnvironmentExtraEnemyScenePath},3,-50,5,100,0,0,0,1
                               TRUE,Em4100,Second environment extra,{SecondEnvironmentExtraEnemyScenePath},3,-49,5,101,0,0,0,1
                               """;
        using var result = RunWithExtraEnemies(extraEnemiesCsv);
        var beforeFirstEnvironmentScene = result.ReadBeforeScene(EnvironmentExtraEnemyScenePath);
        var afterFirstEnvironmentScene = result.ReadAfterScene(EnvironmentExtraEnemyScenePath);
        var beforeSecondEnvironmentScene = result.ReadBeforeScene(SecondEnvironmentExtraEnemyScenePath);
        var afterSecondEnvironmentScene = result.ReadAfterScene(SecondEnvironmentExtraEnemyScenePath);
        var beforeGeneratorScene = result.ReadBeforeScene(EnvironmentExtraEnemyGeneratorScenePath);
        var afterGeneratorScene = result.ReadAfterScene(EnvironmentExtraEnemyGeneratorScenePath);

        var newRootGenerators = GetNewRootGameObjects(afterGeneratorScene, beforeGeneratorScene)
            .Where(gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName)
            .ToList();
        var extraSpawnInfos = GetNewExtraSpawnInfos(afterGeneratorScene, beforeGeneratorScene);
        var firstEnvironmentNewObjects = GetNewGameObjects(afterFirstEnvironmentScene, beforeFirstEnvironmentScene);
        var secondEnvironmentNewObjects = GetNewGameObjects(afterSecondEnvironmentScene, beforeSecondEnvironmentScene);
        var fsmGenerators = firstEnvironmentNewObjects
            .Concat(secondEnvironmentNewObjects)
            .Where(IsFsmGenerationObject)
            .ToList();

        Assert.True(result.WasFileModified(EnvironmentExtraEnemyScenePath));
        Assert.True(result.WasFileModified(SecondEnvironmentExtraEnemyScenePath));
        Assert.True(result.WasFileModified(EnvironmentExtraEnemyGeneratorScenePath));
        Assert.Single(newRootGenerators);
        Assert.Equal(2, extraSpawnInfos.Count);
        Assert.Single(firstEnvironmentNewObjects, IsFsmGenerationObject);
        Assert.Single(secondEnvironmentNewObjects, IsFsmGenerationObject);
        Assert.DoesNotContain(firstEnvironmentNewObjects,
            gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName);
        Assert.DoesNotContain(secondEnvironmentNewObjects,
            gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName);
        Assert.All(fsmGenerators, AssertImmediateFsmGenerator);
        AssertEnemyGenerateRefs(extraSpawnInfos, fsmGenerators);
    }

    [Fact]
    public void ExtraEnemies_Chapter1EnvironmentMoldeds_EnableSceneAiMap() {
        using var result =
            RunWithExtraEnemies(BuildExtraEnemiesCsv(Chapter1EnvironmentExtraEnemyScenePath, 1, "Em4100"));

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, ExtraEnemyScenePath);

        Assert.Single(extraSpawnInfos);
        AssertMoldedAiMap(extraSpawnInfos, "Em4100", "c01_AIMap");
    }

    [Fact]
    public void ExtraEnemies_Chapter3EnvironmentMoldeds_EnableSceneAiMap() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(EnvironmentExtraEnemyScenePath, 3, "Em4000", "Em4100", "Em4200"),
            enemyLimitsCsv: BuildEnemyLimitsCsv(EnvironmentExtraEnemyScenePath, 3));

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, EnvironmentExtraEnemyGeneratorScenePath);

        Assert.Equal(3, extraSpawnInfos.Count);
        AssertMoldedAiMap(extraSpawnInfos, "Em4000", "c03_4_AIMap");
        AssertMoldedAiMap(extraSpawnInfos, "Em4100", "c03_4_AIMap");
        AssertMoldedAiMap(extraSpawnInfos, "Em4200", "c03_4_AIMap");
    }

    [Fact]
    public void ExtraEnemies_Chapter3TestingAreaMoldeds_UseBarnAiMap() {
        const string cowshedScenePath = "natives/stm/environment/scene/chapter3/c03_cowshed01.scn.20";
        using var result = RunWithExtraEnemies(BuildExtraEnemiesCsv(cowshedScenePath, 3, "Em4000"));

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, EnvironmentExtraEnemyGeneratorScenePath);

        Assert.Single(extraSpawnInfos);
        AssertMoldedAiMap(extraSpawnInfos, "Em4000", "c03_4_Lucus_Cowshed");
    }

    [Fact]
    public void ExtraEnemies_ForceTargetingProbability_AppliesToEligibleSpawnOptions() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(ExtraEnemyScenePath, 1, "Em4000", "Em4100", "Em4200", "Em3001"),
            config => config[EnemyModifier.EnemyForceTargetingProbabilityConfigKey] = 1.0,
            enemyLimitsCsv: BuildEnemyLimitsCsv(ExtraEnemyScenePath, 4));

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, ExtraEnemyScenePath);
        var forceTargetingOptions = extraSpawnInfos
            .SelectMany(gameObject => gameObject.Components.Where(EnemySpawnInfoRules.SupportsForceTargetingOption))
            .ToList();

        Assert.Equal(4, extraSpawnInfos.Count);
        Assert.Equal(3, forceTargetingOptions.Count);
        Assert.All(forceTargetingOptions, component =>
            Assert.True(RszSerializer.Deserialize<bool>(component["IsForceTargetingToPlayer"])));
        Assert.Contains(extraSpawnInfos, gameObject =>
            GetSpawnInfo(gameObject).UnitAlias == "Em3001" &&
            !gameObject.Components.Any(EnemySpawnInfoRules.SupportsForceTargetingOption));
    }

    [Fact]
    public void ExtraEnemies_HivePlacement_AddsGeneratedInsectPoolInstances() {
        using var result = RunWithExtraEnemies(BuildExtraEnemiesCsv(ExtraEnemyScenePath, 1, "Em5510"));
        var beforeScene = result.ReadBeforeScene(ExtraEnemyScenePath);
        var afterScene = result.ReadAfterScene(ExtraEnemyScenePath);

        var extraSpawnInfos = GetNewExtraSpawnInfos(afterScene, beforeScene);
        var extraInstances = GetNewExtraEnemyInstances(afterScene, beforeScene);
        var instanceAliases = extraInstances
            .Select(gameObject => gameObject.Name)
            .ToList();
        var hive = Assert.Single(extraInstances, gameObject => gameObject.Name == "Em5510");

        Assert.Single(extraSpawnInfos);
        AssertExtraSpawnInfo(extraSpawnInfos, "Em5510", -50, 5, 100, 2400);
        Assert.Equal(3, instanceAliases.Count(alias => alias == "Em5400"));
        Assert.Equal(2, instanceAliases.Count(alias => alias == "Em5520"));
        AssertHiveTemplateUsesEm5510Assets(hive);
        AssertHiveNestedSpawnInfos(hive);
    }

    [Fact]
    public void ExtraEnemies_Em2000Placement_AddsStaticSceneInstance() {
        using var result = RunWithExtraEnemies(BuildExtraEnemiesCsv(ExtraEnemyScenePath, 1, "Em2000"));
        var beforeScene = result.ReadBeforeScene(ExtraEnemyScenePath);
        var afterScene = result.ReadAfterScene(ExtraEnemyScenePath);

        var newGameObjects = GetNewGameObjects(afterScene, beforeScene);
        var staticMia = Assert.Single(
            GetNewRootGameObjects(afterScene, beforeScene),
            gameObject => gameObject.Name.StartsWith(EnemyModifier.ExtraEnemyStaticPrefix, StringComparison.Ordinal));
        var transform = staticMia.FindComponent<GeneratedViaTransform>()!;

        Assert.True(result.WasFileModified(ExtraEnemyScenePath));
        Assert.Empty(GetNewExtraSpawnInfos(afterScene, beforeScene));
        Assert.DoesNotContain(newGameObjects, gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName);
        Assert.DoesNotContain(newGameObjects, IsFsmGenerationObject);
        Assert.Equal("Prefab/Character/Em2000/Em2000.pfb", staticMia.Prefab);
        Assert.Equal(-50f, transform.Position.X);
        Assert.Equal(5f, transform.Position.Y);
        Assert.Equal(100f, transform.Position.Z);
        Assert.True(RszSerializer.Deserialize<bool>(staticMia.Settings["Update"]));
        Assert.True(RszSerializer.Deserialize<bool>(staticMia.Settings["Draw"]));
        Assert.Contains("app.fsm.Em2000ThinkState", GetFsmActionTypes(staticMia));
        var controller = Assert.Single(staticMia.Components, component => component.Type.FindFieldIndex("SpawnerGuid") >= 0);
        Assert.Equal(staticMia.Guid, controller.Get<Guid>("SpawnerGuid"));
        Assert.NotEqual(Guid.Empty, staticMia.FindComponent<app.EnemySave>()!.SaveGUID);
    }

    [Theory]
    [InlineData("Em2000")]
    [InlineData("Em8001")]
    public void ExtraEnemies_RepeatedBosses_HaveIndependentSaveAndFsmState(string enemyId) {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(ExtraEnemyScenePath, 1, enemyId, enemyId, enemyId));
        var beforeScene = result.ReadBeforeScene(ExtraEnemyScenePath);
        var afterScene = result.ReadAfterScene(ExtraEnemyScenePath);
        var copies = enemyId == "Em2000"
            ? GetNewRootGameObjects(afterScene, beforeScene)
                .Where(gameObject => gameObject.Name.StartsWith(EnemyModifier.ExtraEnemyStaticPrefix, StringComparison.Ordinal))
                .ToList()
            : GetNewExtraEnemyInstances(afterScene, beforeScene);

        Assert.Equal(3, copies.Count);
        EnemyCloneIsolationTests.AssertIndependentState(copies);
        if (enemyId == "Em2000") {
            Assert.All(copies, copy => Assert.Equal(copy.Guid,
                Assert.Single(copy.Components, component => component.Type.FindFieldIndex("SpawnerGuid") >= 0)
                    .Get<Guid>("SpawnerGuid")));
        }
    }

    [Fact]
    public void ExtraEnemies_RandomId_UsesConfiguredEnemyRatios() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "random", "random", "random"),
            config => {
                config["enemy-variety"] = 1;
                config["enemy-pack-max-size"] = 1;
                ConfigureEnemyPool(config, "MoldedQuick");
                config["enemy-ratio-moldedquick"] = 1000.0;
            });

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.True(result.WasFileModified(RandomExtraEnemyScenePath));
        Assert.Equal(3, extraSpawnInfos.Count);
        Assert.All(extraSpawnInfos, gameObject => Assert.Equal("Em4100", GetSpawnInfo(gameObject).UnitAlias));
    }

    //[Fact]
    //public void ExtraEnemies_RandomId_ExcludesBossEnemies()
    //{
    //    using var result = RunWithExtraEnemies(
    //        BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "random", "random", "random"),
    //        config => ConfigureEnemyPool(config, "MargeMutated", "Molded"));

    //    var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

    //    Assert.Equal(3, extraSpawnInfos.Count);
    //    Assert.All(extraSpawnInfos, gameObject => Assert.Equal("Em4000", GetSpawnInfo(gameObject).UnitAlias));
    //}

    [Fact]
    public void ExtraEnemies_RandomId_RespectsEnemyVarietyLimit() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "random", "random", "random", "random"),
            config => {
                config["enemy-variety"] = 1;
                config["enemy-pack-max-size"] = 1;
                ConfigureEnemyPool(config, "Molded", "MoldedQuick", "MoldedFat");
            });

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.Equal(4, extraSpawnInfos.Count);
        Assert.Single(extraSpawnInfos.Select(gameObject => GetSpawnInfo(gameObject).UnitAlias)
            .Distinct(StringComparer.Ordinal));
    }

    //[Fact]
    //public void ExtraEnemies_RandomId_RespectsPackMaxSizeOne()
    //{
    //    using var result = RunWithExtraEnemies(
    //        BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "random", "random", "random", "random", "random", "random"),
    //        config =>
    //        {
    //            config["enemy-variety"] = 2;
    //            config["enemy-pack-max-size"] = 1;
    //            ConfigureEnemyPool(config, "Molded", "MoldedQuick");
    //            config["enemy-ratio-molded"] = 1000.0;
    //            config["enemy-ratio-moldedquick"] = 1000.0;
    //        });

    //    var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);
    //    var enemyAliases = extraSpawnInfos.Select(gameObject => GetSpawnInfo(gameObject).UnitAlias).ToList();
    //    var allowedEnemyAliases = new HashSet<string>(StringComparer.Ordinal)
    //    {
    //        "Em4000",
    //        "Em4100",
    //    };

    //    Assert.Equal(6, enemyAliases.Count);
    //    Assert.All(enemyAliases, alias => Assert.Contains(alias, allowedEnemyAliases));
    //    for (var i = 1; i < enemyAliases.Count; i++)
    //    {
    //        Assert.NotEqual(enemyAliases[i - 1], enemyAliases[i]);
    //    }
    //}

    [Fact]
    public void ExtraEnemies_Amount_SelectsExactRandomSubset() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(
                RandomExtraEnemyScenePath,
                "Em4000",
                "Em4000",
                "Em4000",
                "Em4000"),
            config => { config["extra-enemy-amount"] = 0.5; });

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.Equal(2, extraSpawnInfos.Count);
    }

    [Fact]
    public void ExtraEnemies_RandomId_SkipsWhenGeneratorEnemyPoolIsEmpty() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "random"),
            config => {
                config["enemy-variety"] = 1;
                config["enemy-pack-max-size"] = 1;
                ConfigureEnemyPool(config);
            },
            // Isolate extra placement from explicit aggro directives on vanilla enemies.
            enemyPlacementsCsv: "Guid,SceneFile,IsSpawnInfo,Tags,Include,Exclude\n");

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.Empty(extraSpawnInfos);
        Assert.False(result.WasFileModified(RandomExtraEnemyScenePath));
    }

    [Fact]
    public void ExtraEnemies_EnemyMultiplierAboveOne_RoundsAndDuplicatesExtraGameObjects() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "Em4000", "Em4000", "Em4000"),
            config => { config["enemy-multiplier"] = 1.5; });

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);
        var positions = GetPositions(extraSpawnInfos);

        Assert.Equal(5, extraSpawnInfos.Count);
        Assert.Equal(extraSpawnInfos.Count, extraSpawnInfos.Select(gameObject => gameObject.Guid).Distinct().Count());
        Assert.Equal(3, positions.Distinct().Count());
        Assert.Contains(positions.GroupBy(position => position), group => group.Count() > 1);
    }

    [Fact]
    public void ExtraEnemies_EnemyMultiplierBelowOne_RoundsAndTrimsExtraGameObjects() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "Em4000", "Em4000", "Em4000"),
            config => { config["enemy-multiplier"] = 0.5; });

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.Equal(2, extraSpawnInfos.Count);
    }

    [Fact]
    public void ExtraEnemies_EnemyLimitsCapScenePlacements() {
        using var result = RunWithExtraEnemies(
            BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "Em4000", "Em4000", "Em4000", "Em4000"),
            enemyLimitsCsv: BuildEnemyLimitsCsv(RandomExtraEnemyScenePath, 2));

        var extraSpawnInfos = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);

        Assert.Equal(2, extraSpawnInfos.Count);
    }

    [Fact]
    public void ExtraEnemies_PipeSeparatedId_WithUnknownEnemy_Throws() {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RunWithExtraEnemies(BuildExtraEnemiesCsv(RandomExtraEnemyScenePath, "BogusEnemy|")));

        Assert.Contains("Unknown extra enemy id 'BogusEnemy|'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("selected 'BogusEnemy'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ExtraEnemies_ExplicitIncludeWorksWithoutConfiguredRatios() {
        var csv = $"Enabled,Id,Include,Exclude,SceneFile,Chapter,PosX,PosY,PosZ,RotW\n" +
                  $"TRUE,,MoldedQuick MoldedFat,MoldedFat,{RandomExtraEnemyScenePath},4,-50,5,100,1\n";
        using var result = RunWithExtraEnemies(csv, config => ConfigureEnemyPool(config));
        Assert.Equal("Em4100", GetSpawnInfo(Assert.Single(GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath))).UnitAlias);
    }

    [Fact]
    public void ExtraEnemies_RandomFiltersFallbackBeyondAreaVarietyAndDoNotLeakBetweenRows() {
        var csv = $"Enabled,Id,Include,Exclude,SceneFile,Chapter,PosX,PosY,PosZ,RotW\n" +
                  $"TRUE,random,Molded*,MoldedFat,{RandomExtraEnemyScenePath},4,-50,5,100,1\n" +
                  $"TRUE,random,MoldedFat,,{RandomExtraEnemyScenePath},4,-49,5,101,1\n";
        using var result = RunWithExtraEnemies(csv, config => {
            ConfigureEnemyPool(config, "MoldedQuick", "MoldedFat");
            config["enemy-variety"] = 1;
        });
        var spawns = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath).Select(GetSpawnInfo).ToArray();
        Assert.Equal(2, spawns.Length);
        Assert.Equal(new[] { "Em4100", "Em4200" }, spawns.Select(x => x.UnitAlias).Order());
    }

    [Theory]
    [InlineData(1.0, 1)]
    [InlineData(2.0, 2)]
    public void ExtraEnemies_EmptyFilterDoesNotDuplicateOtherPlacements(double multiplier, int expected) {
        var csv = $"Enabled,Id,Include,Exclude,SceneFile,Chapter,PosX,PosY,PosZ,RotW\n" +
                  $"TRUE,random,MoldedQuick,Molded*,{RandomExtraEnemyScenePath},4,-50,5,100,1\n" +
                  $"TRUE,Em4100,,,{RandomExtraEnemyScenePath},4,-49,5,101,1\n";
        using var result = RunWithExtraEnemies(csv, config => {
            ConfigureEnemyPool(config, "MoldedQuick");
            config["enemy-multiplier"] = multiplier;
        });
        var spawns = GetNewExtraSpawnInfos(result, RandomExtraEnemyScenePath);
        Assert.Equal(expected, spawns.Count);
        Assert.All(GetPositions(spawns), position => Assert.Equal(-49, position.X));
    }

    private static RandomizerRunResult RunWithExtraEnemies() {
        var extraEnemiesCsv = $"""
                               Enabled,Id,Comment,SceneFile,Chapter,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW
                               TRUE,Em4000,Extra molded A,{ExtraEnemyScenePath},1,-49,4.88,108,0,0,0,1
                               TRUE,Em4100,Extra molded B,{ExtraEnemyScenePath},1,-47.92,4.99,100.86,0,0,0,1
                               """;

        return RunWithExtraEnemies(extraEnemiesCsv);
    }

    private static RandomizerRunResult RunWithExtraEnemies(
        string extraEnemiesCsv,
        Action<RandomizerConfiguration>? configure = null,
        string? enemyLimitsCsv = null,
        string? enemyPlacementsCsv = null)
        => RandomizerTest.RunState(
            config => {
                config["extra-enemy-amount"] = 1.0;
                configure?.Invoke(config);
            },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(
                    DynamicDataName.ExtraEnemies,
                    System.Text.Encoding.UTF8.GetBytes(extraEnemiesCsv));

                if (enemyLimitsCsv != null) {
                    randomizer.DynamicData.SetData(
                        DynamicDataName.EnemyLimits,
                        System.Text.Encoding.UTF8.GetBytes(enemyLimitsCsv));
                }
                if (enemyPlacementsCsv != null) {
                    randomizer.DynamicData.SetData(
                        DynamicDataName.Enemies,
                        System.Text.Encoding.UTF8.GetBytes(enemyPlacementsCsv));
                }
            });

    private static string BuildExtraEnemiesCsv(string scenePath, params string[] enemyIds)
        => BuildExtraEnemiesCsv(scenePath, 4, enemyIds);

    private static string BuildExtraEnemiesCsv(string scenePath, int chapter, params string[] enemyIds) {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Enabled,Id,Comment,SceneFile,Chapter,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW");
        for (var i = 0; i < enemyIds.Length; i++) {
            builder.AppendLine(
                $"TRUE,{enemyIds[i]},Random extra {i},{scenePath},{chapter},{-50 + i},5,{100 + i},0,0,0,1");
        }

        return builder.ToString();
    }

    private static string BuildEnemyLimitsCsv(string scenePath, int maxEnemies)
        => $"""
            SceneFile,MaxEnemies,Comment
            {scenePath},{maxEnemies},Test limit
            """;

    private static void ConfigureEnemyPool(RandomizerConfiguration configuration, params string[] enabledEnemyIds) {
        var enabledSet = enabledEnemyIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
            configuration[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enabledSet.Contains(enemy.Id) ? 1.0 : 0.0;
        }
    }

    private static List<RszGameObject> GetNewExtraSpawnInfos(RandomizerRunResult result, string scenePath) {
        var beforeScene = result.ReadBeforeScene(scenePath);
        var afterScene = result.ReadAfterScene(scenePath);

        return GetNewExtraSpawnInfos(afterScene, beforeScene);
    }

    private static List<RszGameObject> GetNewExtraSpawnInfos(RszScene afterScene, RszScene beforeScene)
        => GetNewGameObjects(afterScene, beforeScene)
            .Where(EnemySpawnInfoRules.IsExtraEnemySpawnInfo)
            .ToList();

    private static List<RszGameObject> GetNewExtraEnemyInstances(RszScene afterScene, RszScene beforeScene) {
        var generator = GetNewGameObjects(afterScene, beforeScene)
            .Single(gameObject => gameObject.Name == EnemyModifier.ExtraEnemyGeneratorName);
        var pool = generator.Children.Single(child => child.Name == EnemyModifier.ExtraEnemyPoolName);
        return pool.Children
            .Where(gameObject => gameObject.Name != EnemyModifier.ExtraEnemySpawnPointsName)
            .ToList();
    }

    private static List<(float X, float Y, float Z)> GetPositions(IEnumerable<RszGameObject> gameObjects)
        => gameObjects
            .Select(gameObject => {
                var position = gameObject.FindComponent<GeneratedViaTransform>()!.Position;
                return (position.X, position.Y, position.Z);
            })
            .ToList();

    private static List<RszGameObject> GetNewGameObjects(RszScene afterScene, RszScene beforeScene)
        => afterScene.GetGameObjects()
            .Where(gameObject => beforeScene.FindGameObject(gameObject.Guid) == null)
            .ToList();

    private static List<RszGameObject> GetNewRootGameObjects(RszScene afterScene, RszScene beforeScene) {
        var beforeRootGuids = beforeScene.Children
            .OfType<RszGameObject>()
            .Select(gameObject => gameObject.Guid)
            .ToHashSet();

        return afterScene.Children
            .OfType<RszGameObject>()
            .Where(gameObject => !beforeRootGuids.Contains(gameObject.Guid))
            .ToList();
    }

    private static bool IsFsmGenerationObject(RszGameObject gameObject)
        => gameObject.Name.StartsWith(EnemyModifier.ExtraEnemyGeneratePrefix, StringComparison.Ordinal)
           && gameObject.FindComponent("via.fsm.Fsm") != null;

    private static void AssertImmediateFsmGenerator(RszGameObject gameObject) {
        var fsm = gameObject.FindComponent("via.fsm.Fsm")!;

        Assert.Equal(
            ["via.Transform", "via.fsm.Fsm"],
            gameObject.Components.Select(component => component.Type.Name).ToArray());
        Assert.Null(gameObject.FindComponent("app.GimmickActiveControl"));
        Assert.Null(gameObject.FindComponent("via.physics.Colliders"));
        Assert.Null(gameObject.FindComponent("app.TriggerInAction"));
        Assert.Equal(ExtraEnemyFsmResource, ((RszResourceNode)fsm["Resource"]).Value);
        Assert.True(RszSerializer.Deserialize<bool>(fsm["UseExecuteOnScene"]));
        Assert.True(RszSerializer.Deserialize<bool>(fsm["ExecuteOnScene"]));
        Assert.Equal(["app.fsm.EnemyGenerate"], GetFsmActionTypes(gameObject));
        Assert.Equal(ExtraEnemyGenerateActionUids, GetEnemyGenerateActionUids(gameObject));
    }

    private static string[] GetFsmActionTypes(RszGameObject gameObject) {
        var actionTypes = new List<string>();
        gameObject.Visit(node => {
            if (node is not RszObjectNode objectNode || objectNode.Type.Name != "via.fsm.SceneFsmData")
                return;

            var actions = (RszArrayNode)objectNode["v1_Actions"];
            foreach (var action in actions.Children.OfType<RszObjectNode>()) {
                actionTypes.Add(action.Type.Name);
            }
        });

        return actionTypes.ToArray();
    }

    private static uint[] GetEnemyGenerateActionUids(RszGameObject gameObject) {
        var uids = new List<uint>();
        gameObject.Visit(node => {
            if (node is RszObjectNode objectNode && objectNode.Type.Name == "app.fsm.EnemyGenerate") {
                uids.Add(RszSerializer.Deserialize<uint>(objectNode["v2_UID"]));
            }
        });

        return uids.ToArray();
    }

    private static void AssertEnemyGenerateRefs(
        IReadOnlyCollection<RszGameObject> spawnInfos,
        IReadOnlyCollection<RszGameObject> fsmGenerators) {
        var expectedRefs = spawnInfos.Select(gameObject => gameObject.Guid).Order().ToList();
        var actualRefs = fsmGenerators
            .SelectMany(gameObject => EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(gameObject))
            .Order()
            .ToList();

        Assert.Equal(expectedRefs, actualRefs);
    }

    private static app.EnemySpawnInfo GetSpawnInfo(RszGameObject gameObject)
        => gameObject.FindComponent<app.EnemySpawnInfo>()!;

    private static void AssertPoolInstancesStayAtTemplatePosition(
        IReadOnlyCollection<RszGameObject> spawnInfos,
        IReadOnlyCollection<RszGameObject> instances) {
        foreach (var instance in instances) {
            var spawnInfo = spawnInfos.Single(gameObject => GetSpawnInfo(gameObject).UnitAlias == instance.Name);

            Assert.NotEqual(
                GetPosition(spawnInfo),
                GetPosition(instance));
        }
    }

    private static void AssertEnemyStampSerializationDisabled(IReadOnlyCollection<RszGameObject> instances) {
        var stampControllers = new List<RszObjectNode>();
        foreach (var instance in instances) {
            instance.VisitComponents(component => {
                if (component.Type.Name == "app.StampController") {
                    stampControllers.Add(component);
                }
            });
        }

        Assert.NotEmpty(stampControllers);
        Assert.All(stampControllers, component =>
            Assert.False(RszSerializer.Deserialize<bool>(component["IsSerializeTexture"])));
    }

    private static void AssertPoolInstancesStartHidden(IEnumerable<RszGameObject> instances) {
        foreach (var instance in instances) {
            Assert.False(RszSerializer.Deserialize<bool>(instance.Settings["Draw"]));
        }
    }

    private static (float X, float Y, float Z) GetPosition(RszGameObject gameObject) {
        var position = gameObject.FindComponent<GeneratedViaTransform>()!.Position;
        return (position.X, position.Y, position.Z);
    }

    private static void AssertExtraSpawnInfo(
        IReadOnlyCollection<RszGameObject> spawnInfos,
        string expectedAlias,
        float expectedX,
        float expectedY,
        float expectedZ,
        float expectedHealth) {
        var gameObject = Assert.Single(spawnInfos, gameObject => GetSpawnInfo(gameObject).UnitAlias == expectedAlias);
        var transform = gameObject.FindComponent<GeneratedViaTransform>()!;
        var spawnInfo = GetSpawnInfo(gameObject);
        var componentNames = string.Join(", ", gameObject.Components.Select(component => component.Type.Name));

        Assert.Equal(expectedAlias, gameObject.Name);
        Assert.StartsWith(EnemyModifier.ExtraEnemySpawnInfoPrefix, spawnInfo.Comment);
        Assert.True(Math.Abs(transform.Position.X - expectedX) <= 0.001f,
            $"{expectedAlias} X mismatch; components: {componentNames}");
        Assert.True(Math.Abs(transform.Position.Y - expectedY) <= 0.001f,
            $"{expectedAlias} Y mismatch; components: {componentNames}");
        Assert.True(Math.Abs(transform.Position.Z - expectedZ) <= 0.001f,
            $"{expectedAlias} Z mismatch; components: {componentNames}");
        Assert.Equal(expectedHealth, spawnInfo.HealthParameter.Health);
    }

    private static void AssertHiveTemplateUsesEm5510Assets(RszGameObject hive) {
        var think = hive.FindComponent("app.Em5510Think")!;
        var otherDirectives = (RszArrayNode)think["OtherDirectivesHolder"];

        Assert.Equal("Prefab/Character/Em5510/Em5510.pfb", hive.Prefab);
        Assert.Equal(
            "Prefab/Character/Em5510/Em5510DirectivesHolder.user",
            ((RszUserDataNode)think["DirectivesHolder"]).Path);
        Assert.Empty(otherDirectives.Children);
    }

    private static void AssertHiveNestedSpawnInfos(RszGameObject hive) {
        var aliasesByName = new Dictionary<string, List<string>>(StringComparer.Ordinal){
            ["Em5400SpawnInfo"] = [],
            ["Em5520SpawnInfo"] = [],
        };

        hive.VisitGameObjects(gameObject => {
            if (!aliasesByName.TryGetValue(gameObject.Name, out var aliases))
                return;

            var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
            if (spawnInfo != null) {
                aliases.Add(spawnInfo.UnitAlias);
            }
        });

        Assert.Equal(["Em5400", "Em5400", "Em5400"], aliasesByName["Em5400SpawnInfo"]);
        Assert.Equal(["Em5520", "Em5520"], aliasesByName["Em5520SpawnInfo"]);
    }

    private static void AssertMoldedAiMap(
        IReadOnlyCollection<RszGameObject> spawnInfos,
        string expectedAlias,
        string expectedMapName) {
        var gameObject = Assert.Single(spawnInfos, gameObject => GetSpawnInfo(gameObject).UnitAlias == expectedAlias);
        var spawnInfo = GetSpawnInfo(gameObject);

        Assert.True(spawnInfo.MapParameter.IsUseCheck);
        Assert.Equal(expectedMapName, spawnInfo.MapParameter.MapName);
        Assert.Equal("", spawnInfo.MapParameter.VolumeSpaceMapName);
    }
}
