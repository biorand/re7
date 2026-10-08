using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerEnemyModifierBehaviorTests {
    private const string OldHouseBugEnemyScenePath = "natives/stm/scenes/chapter/chapter3/enemy_c03_3.scn.20";
    private const string MiaPastVhsEnemyScenePath = "natives/stm/scenes/chapter/ff050/enemy_ff050.scn.20";

    private static readonly string[] GeneratorEnemyIds =[
        "Molded",
        "MoldedFat",
        "MoldedQuick"
    ];

    private static readonly Guid[] MargueritePitFightSpawnInfoGuids =[
        new("d484bae0-a8bf-4633-a917-d0aade800111"),
        new("28c36110-42dd-4a12-b6ed-389c1d97c779"),
        new("d3f157fa-68b6-0270-1678-e3ab4e066613"),
        new("21410999-80f4-02e8-2180-dc308b20b4e3"),
        new("a2143fb9-f0d0-034d-3e86-3e6f6056b159"),
        new("17e1a46c-c5a0-0db8-2359-659c65131060"),
        new("c927df77-f5ef-018d-0dbb-761b332d90bf"),
        new("44468ff6-b747-0f57-2472-38ce265840ea"),
        new("6aa86358-9661-0e1d-3a22-107860110dd9"),
        new("8ba82066-2552-0866-1b55-eb8aa5e7fa87"),
        new("478ac89b-7c37-083c-297f-74e790824f22"),
        new("73e69068-0827-0d3a-3612-324f64e7e264"),
        new("64af1c7e-05b4-085f-0abf-15b9c233779c"),
        new("3d24872f-0990-0e4f-2dbe-536696a000c3"),
        new("dc4a746c-4fba-0d5f-0754-6ffae69a1a28"),
    ];

    [Fact]
    public void SelectAreaEnemyPool_RespectsEnemyVarietyLimit() {
        var enemyPool = new[]{
            new EnemyTableEntry(new TestEnemyDefinition("A", EnemyID.Em4000), 1.0),
            new EnemyTableEntry(new TestEnemyDefinition("B", EnemyID.Em4100), 1.0),
            new EnemyTableEntry(new TestEnemyDefinition("C", EnemyID.Em4200), 1.0),
            new EnemyTableEntry(new TestEnemyDefinition("D", EnemyID.Em5400), 1.0)
        };

        var selectedEnemies = EnemyPoolSelector.SelectAreaEnemyPool(enemyPool, enemyVariety: 2, new Rng(0x42424242));

        Assert.Equal(2, selectedEnemies.Length);
        Assert.Equal(2, selectedEnemies.Select(x => x.Enemy.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(selectedEnemies,
            selected => Assert.Contains(enemyPool, entry => entry.Enemy.Id == selected.Enemy.Id));
    }

    [Fact]
    public void RandomizeEnemies_EnemyVarietyOne_UsesSingleEligibleAliasPerScene() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, GeneratorEnemyIds);
        }, prepareRandomizer: UseUnrestrictedEnemyPlacements);

        var changedScenePaths = GetChangedScenePaths(result);
        Assert.NotEmpty(changedScenePaths);

        var scenesWithEligibleSpawns = 0;
        foreach (var path in changedScenePaths) {
            var aliases = GetEligibleGeneratorAliases(result.ReadAfterScene(path))
                .SelectMany(x => x)
                .ToList();
            if (aliases.Count == 0)
                continue;

            scenesWithEligibleSpawns++;
            Assert.Single(aliases.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        Assert.True(scenesWithEligibleSpawns > 0);
    }

    [Fact]
    public void RandomizeEnemies_PackMaxSizeOne_AvoidsAdjacentDuplicateAliasesWithinGenerators() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["enemy-variety"] = GeneratorEnemyIds.Length;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, GeneratorEnemyIds);
        }, prepareRandomizer: UseUnrestrictedEnemyPlacements);

        var changedScenePaths = GetChangedScenePaths(result);
        Assert.NotEmpty(changedScenePaths);

        var generatorsWithMultipleEligibleSpawns = 0;
        foreach (var path in changedScenePaths) {
            foreach (var aliases in GetEligibleGeneratorAliases(result.ReadAfterScene(path))) {
                if (aliases.Count < 2)
                    continue;

                generatorsWithMultipleEligibleSpawns++;
                for (var i = 1; i < aliases.Count; i++) {
                    Assert.False(
                        string.Equals(aliases[i - 1], aliases[i], StringComparison.OrdinalIgnoreCase),
                        $"Found adjacent duplicate aliases in '{path}': {aliases[i - 1]} then {aliases[i]}.");
                }
            }
        }

        Assert.True(generatorsWithMultipleEligibleSpawns > 0);
    }

    [Fact]
    public void IsBalancedCompatibleReplacement_UsesChapterProgressionStrengthCaps() {
        var molded = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "Molded");
        var moldedQuick = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "MoldedQuick");
        var moldedFat = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "MoldedFat");
        var jackStalker = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "JackStalker");
        var margeMutated = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "MargeMutated");

        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            moldedQuick,
            chapter: 1,
            scenePath: "natives/stm/scenes/chapter/chapter1/enemy_c01.scn.20"));
        Assert.False(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            molded,
            chapter: 1,
            scenePath: "natives/stm/scenes/chapter/chapter1/enemy_c01.scn.20"));
        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            molded,
            chapter: 3,
            scenePath: "natives/stm/scenes/chapter/chapter3/chapter3_2/moldeads.scn.20"));
        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            moldedFat,
            chapter: 3,
            scenePath: "natives/stm/scenes/chapter/chapter3/chapter3_2/moldeads.scn.20"));
        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            moldedFat,
            chapter: 3,
            scenePath: OldHouseBugEnemyScenePath));
        Assert.False(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            jackStalker,
            chapter: 3,
            scenePath: OldHouseBugEnemyScenePath));
        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            jackStalker,
            chapter: 3,
            scenePath: "natives/stm/scenes/chapter/chapter3/enemy_c03_5.scn.20"));
        Assert.True(BalancedEnemyPoolSelector.IsCompatibleReplacement(
            margeMutated,
            chapter: 4,
            scenePath: "natives/stm/scenes/chapter/chapter4/enemy_c04_3.scn.20"));
    }

    [Theory]
    [InlineData("MoldedFat", 1, "natives/stm/environment/scene/chapter1/c01_3f.scn.20", false)]
    [InlineData("EvelineElderly", 1, "natives/stm/environment/scene/chapter1/c01_3f.scn.20", false)]
    [InlineData("MoldedFat", 3, "natives/stm/scenes/chapter/chapter3/chapter3_2/moldeads.scn.20", true)]
    [InlineData("EvelineElderly", 3, "natives/stm/scenes/chapter/chapter3/chapter3_2/moldeads.scn.20", true)]
    [InlineData("EvelineElderly", 3, "natives/stm/environment/scene/chapter3/c03_oldhouse1fkitchen01.scn.20", true)]
    [InlineData("MiaChainsaw", 1, "natives/stm/environment/scene/chapter1/c01_3f.scn.20", false)]
    [InlineData("JackShears", 3, "natives/stm/scenes/chapter/chapter3/chapter3_2/moldeads.scn.20", false)]
    [InlineData("JackShears", 3, "natives/stm/scenes/chapter/chapter3/chapter3_3/enemy.scn.20", true)]
    [InlineData("JackShears", 3, "natives/stm/environment/scene/chapter3/c03_oldhouse1fkitchen01.scn.20", true)]
    [InlineData("MiaChainsaw", 3, "natives/stm/environment/scene/chapter3/c03_greenhouse1f.scn.20", true)]
    [InlineData("MargeMutated", 3, "natives/stm/environment/scene/chapter3/c03_oldhouse1fkitchen01.scn.20", false)]
    [InlineData("MargeMutated", 3, "natives/stm/environment/scene/chapter3/c03_labo1f.scn.20", true)]
    [InlineData("MargeMutated", 3, "natives/stm/environment/scene/chapter3/c03_boat1fentrance01.scn.20", true)]
    public void BalancedEnemies_AllowBossesAsProgressionAdvances(string id, int chapter, string path, bool expected) {
        var enemy = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == id);
        Assert.Equal(expected, BalancedEnemyPoolSelector.IsCompatibleReplacement(enemy, chapter, path));
    }

    [Fact]
    public void RandomizeEnemies_Balanced_PreservesOldHouseInsectsWhenPoolHasNoCompatibleInsects() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-enemies"] = true;
                config["balanced-enemies"] = true;
                config["enemy-variety"] = 3;
                config["enemy-pack-max-size"] = 1;
                ConfigureGeneratorEnemyPool(config, ["MoldedFat", "JackStalker"]);
            },
            seed: 410980);

        var beforeAliases = GetGeneratorSpawnAliases(result.ReadBeforeScene(OldHouseBugEnemyScenePath), "Bug");
        var afterAliases = GetGeneratorSpawnAliases(result.ReadAfterScene(OldHouseBugEnemyScenePath), "Bug");

        Assert.NotEmpty(beforeAliases);
        Assert.All(beforeAliases, alias => Assert.True(EnemySpawnInfoRules.IsInsectSpawnAlias(alias)));
        Assert.NotEmpty(afterAliases);
        Assert.Equal(beforeAliases, afterAliases);
    }

    [Fact]
    public void
        RandomizeEnemies_Unbalanced_RestrictsOldHouseInsectsToInsectReplacements_AndDisablesStampSerialization() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-enemies"] = true;
                config["balanced-enemies"] = false;
                config["enemy-variety"] = 4;
                config["enemy-pack-max-size"] = 1;
                ConfigureGeneratorEnemyPool(config, ["FlyingBug", "Molded", "MoldedFat", "JackStalker"]);
            },
            seed: 410980);

        var beforeAliases = GetGeneratorSpawnAliases(result.ReadBeforeScene(OldHouseBugEnemyScenePath), "Bug");
        var afterScene = result.ReadAfterScene(OldHouseBugEnemyScenePath);
        var afterAliases = GetGeneratorSpawnAliases(afterScene, "Bug");
        var beforePoolGuids = GetGeneratorPoolInstances(result.ReadBeforeScene(OldHouseBugEnemyScenePath), "Bug")
            .Select(gameObject => gameObject.Guid)
            .ToHashSet();
        var replacementInstances = GetGeneratorPoolInstances(afterScene, "Bug")
            .Where(gameObject => !beforePoolGuids.Contains(gameObject.Guid))
            .ToList();

        Assert.NotEmpty(beforeAliases);
        Assert.All(beforeAliases, alias => Assert.True(EnemySpawnInfoRules.IsInsectSpawnAlias(alias)));
        Assert.NotEmpty(afterAliases);
        Assert.All(afterAliases, alias => Assert.Equal("Em5400", alias));
        Assert.NotEqual(beforeAliases, afterAliases);
        AssertStampSerializationDisabled(replacementInstances);
    }

    [Theory]
    [InlineData("InsectHive")]
    [InlineData("InsectSwarm")]
    public void RandomizeEnemies_LegacySwarmOnlyProfile_PreservesVanillaEncounters(string enemyId) {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-enemies"] = true;
                config["enemy-variety"] = 1;
                config["enemy-pack-max-size"] = 1;
                ConfigureGeneratorEnemyPool(config, []);
                config[$"enemy-ratio-{enemyId.ToLowerInvariant()}"] = 1.0;
            },
            seed: 410980);

        Assert.False(result.WasFileModified(OldHouseBugEnemyScenePath));
        Assert.Equal(
            GetGeneratorSpawnAliases(result.ReadBeforeScene(OldHouseBugEnemyScenePath), "Bug"),
            GetGeneratorSpawnAliases(result.ReadAfterScene(OldHouseBugEnemyScenePath), "Bug"));
    }

    [Fact]
    public void RandomizeEnemies_MargueriteInsectFallPitFightSpawns_AreNotRandomized() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-enemies"] = true;
                config["enemy-variety"] = 1;
                config["enemy-pack-max-size"] = 1;
                ConfigureGeneratorEnemyPool(config, ["MoldedFat"]);
            },
            seed: 410980);

        var beforeAliases = GetSpawnAliasesByGameObjectGuid(
            result.ReadBeforeScene(OldHouseBugEnemyScenePath),
            MargueritePitFightSpawnInfoGuids);
        var afterAliases = GetSpawnAliasesByGameObjectGuid(
            result.ReadAfterScene(OldHouseBugEnemyScenePath),
            MargueritePitFightSpawnInfoGuids);

        Assert.Equal(MargueritePitFightSpawnInfoGuids.Length, beforeAliases.Count);
        Assert.Equal(beforeAliases, afterAliases);
        Assert.Equal(8, afterAliases.Values.Count(alias => alias == "Em5400"));
        Assert.Equal(7, afterAliases.Values.Count(alias => alias == "Em5520"));
    }

    [Fact]
    public void RandomizeEnemies_MiaPastVhsEnemyScene_IsLoadedButPreserved() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["MoldedFat"]);
        });

        var area = Assert.Single(result.AreaService.Areas, area => area.Path == MiaPastVhsEnemyScenePath);
        Assert.NotEmpty(area.EnemyGenerators);

        var beforeAliases = GetEligibleGeneratorAliases(result.ReadBeforeScene(MiaPastVhsEnemyScenePath))
            .SelectMany(x => x)
            .ToList();
        var afterAliases = GetEligibleGeneratorAliases(result.ReadAfterScene(MiaPastVhsEnemyScenePath))
            .SelectMany(x => x)
            .ToList();

        Assert.False(result.WasFileModified(MiaPastVhsEnemyScenePath));
        Assert.NotEmpty(beforeAliases);
        Assert.Contains(beforeAliases, alias => alias != "Em4200");
        Assert.Equal(beforeAliases, afterAliases);
    }

    [Fact]
    public void RandomizeEnemies_GeneratorReplacementsUseTargetSpecifiedRankParameters() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["MoldedQuick"]);
        });

        var replacementsFromExplicitSpawnInfos = 0;
        foreach (var path in GetChangedScenePaths(result)
                     .Where(path =>
                         path.StartsWith("natives/stm/scenes/chapter/", StringComparison.OrdinalIgnoreCase))) {
            var beforeScene = result.ReadBeforeScene(path);
            var afterScene = result.ReadAfterScene(path);

            afterScene.VisitGameObjects(afterGameObject => {
                var afterSpawnInfo = afterGameObject.FindComponent<app.EnemySpawnInfo>();
                if (afterSpawnInfo?.UnitAlias != "Em4100")
                    return;

                var beforeSpawnInfo = beforeScene.FindGameObject(afterGameObject.Guid)
                    ?.FindComponent<app.EnemySpawnInfo>();
                if (beforeSpawnInfo == null || IsSpecifiedRankParameterEmpty(beforeSpawnInfo))
                    return;

                replacementsFromExplicitSpawnInfos++;
                Assert.Empty(afterSpawnInfo.specifiedRankParameter.SpecifiedDirectivesName);
                Assert.Empty(afterSpawnInfo.specifiedRankParameter.SpecifiedResistParameterName);
                Assert.Empty(afterSpawnInfo.specifiedRankParameter.SpecifiedSlipParameterName);
            });
        }

        Assert.True(replacementsFromExplicitSpawnInfos > 0);
    }

    [Fact]
    public void RandomizeEnemies_MoldedQuickReplacementsUseDefaultAppearMode() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["MoldedQuick"]);
        });

        var replacementAppearTypes = new List<Enums.app.Em4100.ThinkAppearSet.Type>();
        foreach (var path in GetChangedScenePaths(result)
                     .Where(path =>
                         path.StartsWith("natives/stm/scenes/chapter/", StringComparison.OrdinalIgnoreCase))) {
            var afterScene = result.ReadAfterScene(path);
            afterScene.VisitGameObjects(gameObject => {
                var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
                if (spawnInfo?.UnitAlias != "Em4100" ||
                    !gameObject.Name.EndsWith("_Now_Em4100", StringComparison.Ordinal)) {
                    return;
                }

                var appearType = GetMoldedQuickAppearType(gameObject);
                Assert.NotNull(appearType);
                replacementAppearTypes.Add(appearType.Value);
            });
        }

        Assert.NotEmpty(replacementAppearTypes);
        Assert.All(
            replacementAppearTypes,
            appearType => Assert.Equal(Enums.app.Em4100.ThinkAppearSet.Type.Default, appearType));
    }

    [Fact]
    public void RandomizeEnemies_NonDefaultMoldedQuickAppearSpawns_AreNotRandomized() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["MoldedFat"]);
        });

        var preservedSpawnGuids = new List<Guid>();
        foreach (var area in result.AreaService.Areas
                     .Where(area =>
                         area.Path.StartsWith("natives/stm/scenes/chapter/", StringComparison.OrdinalIgnoreCase))) {
            var beforeScene = result.ReadBeforeScene(area.Path);
            var afterScene = result.ReadAfterScene(area.Path);

            beforeScene.VisitGameObjects(beforeGameObject => {
                var beforeSpawnInfo = beforeGameObject.FindComponent<app.EnemySpawnInfo>();
                if (beforeSpawnInfo?.Enabled != true ||
                    !EnemySpawnInfoRules.HasNonDefaultMoldedQuickAppearMode(beforeGameObject)) {
                    return;
                }

                var afterGameObject = afterScene.FindGameObject(beforeGameObject.Guid);
                Assert.NotNull(afterGameObject);

                var afterSpawnInfo = afterGameObject!.FindComponent<app.EnemySpawnInfo>();
                Assert.NotNull(afterSpawnInfo);

                preservedSpawnGuids.Add(beforeGameObject.Guid);
                Assert.False(EnemySpawnInfoRules.ShouldReplaceSpawnInfo(beforeGameObject));
                Assert.Equal(beforeSpawnInfo.UnitAlias, afterSpawnInfo!.UnitAlias);
                Assert.Equal(GetMoldedQuickAppearType(beforeGameObject), GetMoldedQuickAppearType(afterGameObject));
            });
        }

        Assert.NotEmpty(preservedSpawnGuids);
    }

    [Fact]
    public void RandomizeEnemies_MiaChainsaw_IsNotUsedForGeneratorReplacements() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["MiaChainsaw"]);
        }, prepareRandomizer: UseUnrestrictedEnemyPlacements);

        Assert.DoesNotContain(
            GetChangedScenePaths(result),
            path => path.StartsWith("natives/stm/scenes/chapter/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RandomizeEnemies_Em3300ReplacementsAreMarkedForExplosiveBehavior() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            ConfigureGeneratorEnemyPool(config, ["EvelineElderly"]);
        }, prepareRandomizer: UseUnrestrictedEnemyPlacements);

        var replacements = new List<RszGameObject>();
        foreach (var path in GetChangedScenePaths(result)) {
            result.ReadAfterScene(path).VisitGameObjects(gameObject => {
                if (gameObject.Name == "Em3300_Static") {
                    replacements.Add(gameObject);
                }
            });
        }

        Assert.NotEmpty(replacements);
        Assert.All(
            replacements,
            gameObject => Assert.Equal(
                EnemyTemplateFactory.ExplosiveEm3300Tag,
                gameObject.Settings.Get<string>("Tag")));
    }

    [Fact]
    public void RandomizeEnemies_VanillaEm3300sDoNotHaveExplosionMarker() {
        using var result = RandomizerTest.RunState();
        var scene = result.ReadAfterScene("natives/stm/scenes/enemy/em3300.scn.20");
        var vanillaEm3300s = new List<RszGameObject>();

        scene.VisitGameObjects(gameObject => {
            if (string.Equals(gameObject.Name, "Em3300", StringComparison.OrdinalIgnoreCase)
                || gameObject.Name.StartsWith("Em3300_", StringComparison.OrdinalIgnoreCase)) {
                vanillaEm3300s.Add(gameObject);
            }
        });

        Assert.NotEmpty(vanillaEm3300s);
        Assert.DoesNotContain(
            vanillaEm3300s,
            gameObject => gameObject.Settings.Get<string>("Tag") == EnemyTemplateFactory.ExplosiveEm3300Tag);
    }

    [Fact]
    public void RandomizeEnemies_ForceTargetingProbability_AppliesToEligibleSpawnOptions() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["enemy-variety"] = 1;
            config["enemy-pack-max-size"] = 1;
            config[EnemyModifier.EnemyForceTargetingProbabilityConfigKey] = 1.0;
            ConfigureGeneratorEnemyPool(config, ["MoldedFat"]);
        });

        var forceTargetingOptions = new List<RszObjectNode>();
        foreach (var path in GetChangedScenePaths(result)) {
            result.ReadAfterScene(path).VisitGameObjects(gameObject => {
                var enemyGenerator = gameObject.FindComponent<app.EnemyGenerator>();
                if (enemyGenerator?.Enabled != true)
                    return;

                foreach (var child in GetGeneratorSpawnInfoGameObjects(gameObject)) {
                    if (!EnemySpawnInfoRules.ShouldReplaceSpawnInfo(child))
                        continue;

                    var spawnInfo = child.FindComponent<app.EnemySpawnInfo>();
                    if (spawnInfo?.UnitAlias != "Em4200")
                        continue;

                    forceTargetingOptions.AddRange(
                        child.Components.Where(EnemySpawnInfoRules.SupportsForceTargetingOption));
                }
            });
        }

        Assert.NotEmpty(forceTargetingOptions);
        Assert.All(forceTargetingOptions, component =>
            Assert.True(RszSerializer.Deserialize<bool>(component["IsForceTargetingToPlayer"])));
    }

    [Fact]
    public void ForceTargetingProbability_AppliesWithoutEnemyReplacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = false;
            config[EnemyModifier.EnemyForceTargetingProbabilityConfigKey] = 1.0;
        }, prepareRandomizer: UseUnrestrictedEnemyPlacements);

        var forceTargetingOptions = GetChangedScenePaths(result)
            .SelectMany(path => GetForceTargetingOptions(result.ReadAfterScene(path)))
            .ToList();

        Assert.NotEmpty(forceTargetingOptions);
        Assert.All(forceTargetingOptions, component =>
            Assert.True(RszSerializer.Deserialize<bool>(component["IsForceTargetingToPlayer"])));
    }

    private static void ConfigureGeneratorEnemyPool(RandomizerConfiguration configuration,
        IEnumerable<string> enabledEnemyIds) {
        var enabledSet = enabledEnemyIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
            configuration[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enabledSet.Contains(enemy.Id) ? 1.0 : 0.0;
        }
    }

    // Isolate pool/option mechanics from the curated per-placement sheet policies.
    // RandomizerEnemyPlacementRuleBehaviorTests exercises those policies separately.
    private static void UseUnrestrictedEnemyPlacements(Randomizer randomizer)
        => randomizer.DynamicData.SetData(Serialization.DynamicDataName.Enemies,
            System.Text.Encoding.UTF8.GetBytes("Guid,SceneFile,Tags,Include,Exclude\n"));

    private static List<string> GetChangedScenePaths(RandomizerRunResult result)
        => result.ChangedFiles.Keys
            .Where(path => path.EndsWith(".scn.20", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    private static List<List<string>> GetEligibleGeneratorAliases(RszScene scene) {
        var result = new List<List<string>>();

        scene.VisitGameObjects(gameObject => {
            var enemyGenerator = gameObject.FindComponent<app.EnemyGenerator>();
            if (enemyGenerator?.Enabled != true)
                return;

            var aliases = new List<string>();
            foreach (var child in GetGeneratorSpawnInfoGameObjects(gameObject)) {
                if (!EnemySpawnInfoRules.ShouldReplaceSpawnInfo(child))
                    continue;

                var spawnInfo = child.FindComponent<app.EnemySpawnInfo>();
                if (spawnInfo != null) {
                    aliases.Add(spawnInfo.UnitAlias);
                }
            }

            if (aliases.Count > 0) {
                result.Add(aliases);
            }
        });

        return result;
    }

    private static List<RszObjectNode> GetForceTargetingOptions(RszScene scene) {
        var result = new List<RszObjectNode>();
        scene.VisitComponents((gameObject, component) => {
            if (gameObject.FindComponent<app.EnemySpawnInfo>() != null &&
                EnemySpawnInfoRules.SupportsForceTargetingOption(component)) {
                result.Add(component);
            }
        });

        return result;
    }

    private static List<string> GetGeneratorSpawnAliases(RszScene scene, string generatorAlias) {
        var result = new List<string>();

        scene.VisitGameObjects(gameObject => {
            var enemyGenerator = gameObject.FindComponent<app.EnemyGenerator>();
            if (!string.Equals(enemyGenerator?.Alias, generatorAlias, StringComparison.Ordinal))
                return;

            foreach (var child in GetGeneratorSpawnInfoGameObjects(gameObject)) {
                if (!EnemySpawnInfoRules.ShouldReplaceSpawnInfo(child))
                    continue;

                var spawnInfo = child.FindComponent<app.EnemySpawnInfo>();
                if (spawnInfo != null) {
                    result.Add(spawnInfo.UnitAlias);
                }
            }
        });

        return result;
    }

    private static List<RszGameObject> GetGeneratorSpawnInfoGameObjects(RszGameObject generator) {
        var result = new List<RszGameObject>();
        foreach (var pool in generator.Children.Where(child => child.FindComponent<app.EnemyPool>() != null)) {
            foreach (var poolChild in pool.Children) {
                if (ContainsEnemyMesh(poolChild))
                    continue;

                poolChild.VisitGameObjects(gameObject => {
                    if (gameObject.FindComponent<app.EnemySpawnInfo>() != null) {
                        result.Add(gameObject);
                    }
                });
            }
        }

        return result;
    }

    private static List<RszGameObject> GetGeneratorPoolInstances(RszScene scene, string generatorAlias) {
        var result = new List<RszGameObject>();

        scene.VisitGameObjects(gameObject => {
            var enemyGenerator = gameObject.FindComponent<app.EnemyGenerator>();
            if (!string.Equals(enemyGenerator?.Alias, generatorAlias, StringComparison.Ordinal))
                return;

            var pool = gameObject.Children.Single(child => child.FindComponent<app.EnemyPool>() != null);
            result.AddRange(pool.Children.Where(ContainsEnemyMesh));
        });

        return result;
    }

    private static bool ContainsEnemyMesh(RszGameObject gameObject) {
        var result = false;
        gameObject.VisitGameObjects(child => {
            var mesh = child.FindComponent("via.render.Mesh");
            if (mesh != null &&
                mesh.Children.Length > 2 &&
                mesh.Children[2]?.ToString()
                    ?.StartsWith("Character/Enemy/", StringComparison.InvariantCultureIgnoreCase) == true) {
                result = true;
            }
        });

        return result;
    }

    private static void AssertStampSerializationDisabled(IReadOnlyCollection<RszGameObject> instances) {
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

    private static Dictionary<string, List<string>> GetSpawnAliasesByGameObjectNamePrefix(RszScene scene,
        params string[] prefixes) {
        var result = prefixes.ToDictionary(prefix => prefix, _ => new List<string>(), StringComparer.Ordinal);
        scene.VisitGameObjects(gameObject => {
            var matchingPrefix = prefixes.FirstOrDefault(prefix =>
                gameObject.Name.StartsWith(prefix, StringComparison.Ordinal));
            if (matchingPrefix == null)
                return;

            var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
            if (spawnInfo != null) {
                result[matchingPrefix].Add(spawnInfo.UnitAlias);
            }
        });

        return result;
    }

    private static Dictionary<Guid, string> GetSpawnAliasesByGameObjectGuid(RszScene scene,
        IReadOnlyCollection<Guid> guids) {
        var result = new Dictionary<Guid, string>();
        var guidSet = guids.ToHashSet();
        scene.VisitGameObjects(gameObject => {
            if (!guidSet.Contains(gameObject.Guid))
                return;

            var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
            if (spawnInfo != null) {
                result.Add(gameObject.Guid, spawnInfo.UnitAlias);
            }
        });

        return result;
    }

    private static bool IsSpecifiedRankParameterEmpty(app.EnemySpawnInfo spawnInfo) {
        var specified = spawnInfo.specifiedRankParameter;
        return string.IsNullOrEmpty(specified.SpecifiedDirectivesName) &&
               string.IsNullOrEmpty(specified.SpecifiedResistParameterName) &&
               string.IsNullOrEmpty(specified.SpecifiedSlipParameterName);
    }

    private static Enums.app.Em4100.ThinkAppearSet.Type? GetMoldedQuickAppearType(RszGameObject gameObject)
        => gameObject.FindComponent<app.EnemySpawnInfoOptionEm4100>()?.ThinkSet?.AppearSet?.AppearType;

    private sealed class TestEnemyDefinition(string id, EnemyID enemyId) : IEnemyDefinition {
        public string Id { get; } = id;
        public EnemyID EnemyId { get; } = enemyId;
        public EnemyCategory Category => EnemyCategory.Molded;
        public string Name => Id;
        public bool IsBoss => false;
        public int BaseHealth => 100;
        public string DirectivesHolderPath => string.Empty;
        public string ResistParamsHolderPath => string.Empty;
        public string OriginalPrefabPath => string.Empty;
        public bool UsesEnemyGenerator => true;
    }
}
