using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerEnemyMultiplierBehaviorTests {
    private const string MiaPastVhsEnemyScenePath = "natives/stm/scenes/chapter/ff050/enemy_ff050.scn.20";

    private const string TestScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string ExternalGenerateScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/hard.scn.20";

    [Fact]
    public void CollectMultipliableSpawnSlots_FindsFsmGeneratedSpawnInfos() {
        using var result = RandomizerTest.RunState();
        var (_, scene, slots) = FindSceneWithSlots(result, minSlots: 2);

        Assert.NotEmpty(slots);
        Assert.All(slots, slot => {
            Assert.NotEqual(Guid.Empty, slot.SpawnInfoGuid);
            Assert.NotEqual(Guid.Empty, slot.GenerationGameObjectGuid);
            Assert.Contains(slot.SpawnInfoGuid,
                EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(slot.GenerationGameObject));
            Assert.NotNull(scene.FindGameObject(slot.SpawnInfoGuid));
            Assert.NotNull(scene.FindGameObject(slot.GenerationGameObjectGuid));
        });
    }

    [Fact]
    public void MoldedsScene_CollectsGenerationObjectsWithMultipleSpawnInfos() {
        using var result = RandomizerTest.RunState();
        var scene = result.ReadBeforeScene(TestScenePath);
        var groups = EnemyMultiplierModifier.CollectMultipliableSpawnGroups(scene);
        var slots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(scene);

        Assert.NotEmpty(groups);
        Assert.Contains(groups, group => group.SpawnSlots.Length > 1);
        Assert.Equal(groups.Sum(group => group.SpawnSlots.Length), slots.Length);
        Assert.All(groups, group => {
            var activeSpawnInfoRefs =
                EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(group.GenerationGameObject);
            Assert.All(group.SpawnSlots, slot => Assert.Contains(slot.SpawnInfoGuid, activeSpawnInfoRefs));
        });
    }

    [Fact]
    public void ProcessScene_MultiplierBelowOne_RemovesSpawnInfosAndDisablesGenerationActions() {
        using var result = RandomizerTest.RunState();
        var (scenePath, scene, beforeSlots) = FindSceneWithSlots(result, minSlots: 2);
        var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Length, 0.5);

        var afterScene = EnemyMultiplierModifier.ProcessScene(
            scene,
            result.Randomizer,
            new RandomizerLogger(),
            scenePath,
            0.5,
            new Rng(0x5150));
        var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(afterScene);

        Assert.True(targetCount < beforeSlots.Length);
        Assert.Equal(targetCount, afterSlots.Length);

        var afterSpawnGuids = afterSlots.Select(slot => slot.SpawnInfoGuid).ToHashSet();
        var removedSlots = beforeSlots.Where(slot => !afterSpawnGuids.Contains(slot.SpawnInfoGuid)).ToList();

        Assert.Equal(beforeSlots.Length - targetCount, removedSlots.Count);
        foreach (var removedSlot in removedSlots) {
            Assert.Null(afterScene.FindGameObject(removedSlot.SpawnInfoGuid));
            AssertNoActiveGenerationObjectReferences(afterScene, removedSlot.SpawnInfoGuid);
        }
    }

    [Fact]
    public void ProcessScene_MultiplierAboveOne_DuplicatesSpawnInfosGenerationObjectsAndPoolInstances() {
        using var result = RandomizerTest.RunState();
        var (scenePath, scene, beforeSlots) = FindSceneWithSlots(result, minSlots: 2);
        var beforeSpawnGuids = beforeSlots.Select(slot => slot.SpawnInfoGuid).ToHashSet();
        var beforeGenerationGuids = beforeSlots.Select(slot => slot.GenerationGameObjectGuid).ToHashSet();
        var beforePooledEnemyCount = CountPooledEnemyInstances(scene, beforeSlots);
        var beforeFsmInstanceGuids = beforeSlots
            .Select(slot => GetComponentGuid(slot.GenerationGameObject, "via.fsm.Fsm", "InstanceGuid"))
            .ToHashSet();
        var beforeTriggerSaveGuids = beforeSlots
            .Select(slot => GetComponentGuid(slot.GenerationGameObject, "app.TriggerInAction", "SaveGUID"))
            .ToHashSet();

        var afterScene = EnemyMultiplierModifier.ProcessScene(
            scene,
            result.Randomizer,
            new RandomizerLogger(),
            scenePath,
            2.0,
            new Rng(0x5151));
        var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(afterScene);
        var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Length, 2.0);
        var afterPooledEnemyCount = CountPooledEnemyInstances(afterScene, beforeSlots);

        Assert.Equal(targetCount, afterSlots.Length);
        Assert.Equal(afterSlots.Length, afterSlots.Select(slot => slot.SpawnInfoGuid).Distinct().Count());

        var newSlots = afterSlots
            .Where(slot => !beforeSpawnGuids.Contains(slot.SpawnInfoGuid))
            .ToList();

        Assert.Equal(beforeSlots.Length, newSlots.Count);
        Assert.Equal(beforePooledEnemyCount + newSlots.Count, afterPooledEnemyCount);
        var poolGuids = beforeSlots.Select(slot => slot.EnemyPoolGuid).Distinct();
        foreach (var poolGuid in poolGuids) {
            var pool = afterScene.FindGameObject(poolGuid)!;
            var copies = pool.Children.Where(child => child.FindComponent<app.EnemySave>() != null &&
                scene.FindGameObject(child.Guid) == null).ToArray();
            if (copies.Length == 0) continue;
            EnemyCloneIsolationTests.AssertIndependentState(copies);
            var originalIds = scene.FindGameObject(poolGuid)!.Children
                .Where(child => child.FindComponent<app.EnemySave>() != null)
                .SelectMany(EnemyCloneIsolationTests.GetStateIds).ToHashSet();
            foreach (var copy in copies) {
                copy.VisitComponents(component => {
                    foreach (var field in new[] { "SaveGUID", "InstanceGuid" }) {
                        var index = component.Type.FindFieldIndex(field);
                        if (index >= 0) {
                            var guid = component.Get<Guid>(field);
                            Assert.True(guid == Guid.Empty || !originalIds.Contains(guid),
                                $"{scenePath}: {copy.Name} {component.Type.Name}.{component.Type.Fields[index].Name} still shares {guid}");
                        }
                    }
                    return component;
                });
            }
        }
        foreach (var newSlot in newSlots) {
            Assert.DoesNotContain(newSlot.GenerationGameObjectGuid, beforeGenerationGuids);
            Assert.Contains("_BioRandMultiplier", newSlot.SpawnInfoGameObject.Name);
            Assert.Contains("_BioRandMultiplier", newSlot.GenerationGameObject.Name);
            Assert.Equal(
                [newSlot.SpawnInfoGuid],
                EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(newSlot.GenerationGameObject).Distinct());
            Assert.DoesNotContain(
                GetComponentGuid(newSlot.GenerationGameObject, "via.fsm.Fsm", "InstanceGuid"),
                beforeFsmInstanceGuids);
            Assert.DoesNotContain(
                GetComponentGuid(newSlot.GenerationGameObject, "app.TriggerInAction", "SaveGUID"),
                beforeTriggerSaveGuids);
        }
    }

    [Fact]
    public void ApplyMaxEnemyCount_BelowCurrentLimit_PreservesCurrentForNeutralOrIncrease() {
        Assert.Equal(7, EnemyMultiplierModifier.ApplyMaxEnemyCount(
            currentEnemyCount: 7,
            uncappedTargetCount: 7,
            maxEnemyCount: 0));

        Assert.Equal(7, EnemyMultiplierModifier.ApplyMaxEnemyCount(
            currentEnemyCount: 7,
            uncappedTargetCount: 14,
            maxEnemyCount: 0));
    }

    [Theory]
    [InlineData("Em5510")]
    [InlineData("Em5511")]
    [InlineData("Em5512")]
    [InlineData("Em5520")]
    public void ProcessScene_SwarmsAndHivesAreNeverDuplicatedEvenWithoutNoDupDirective(string alias) {
        using var result = RandomizerTest.RunState();
        var (scenePath, scene, slots) = FindSceneWithSlots(result, minSlots: 2);
        foreach (var slot in slots) {
            var gameObject = scene.FindGameObject(slot.SpawnInfoGuid)!;
            var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>()!;
            spawnInfo.UnitAlias = alias;
            scene = scene.UpdateGameObject(gameObject.AddOrUpdateComponent(spawnInfo));
        }

        var afterScene = EnemyMultiplierModifier.ProcessScene(scene, result.Randomizer,
            new RandomizerLogger(), scenePath, 3.0, new Rng(0x5151));

        Assert.Same(scene, afterScene);
        Assert.Equal(slots.Length, EnemyMultiplierModifier.CollectMultipliableSpawnSlots(afterScene).Length);
    }

    [Fact]
    public void ApplyMaxEnemyCount_CapsReducedTarget() {
        Assert.Equal(1, EnemyMultiplierModifier.ApplyMaxEnemyCount(
            currentEnemyCount: 7,
            uncappedTargetCount: 4,
            maxEnemyCount: 1));
    }

    [Fact]
    public void MoldedsScene_MultiplierAboveOne_DuplicatesFilteredGenerationClones() {
        using var result = RandomizerTest.RunState();
        var scene = result.ReadBeforeScene(TestScenePath);
        var beforeGroups = EnemyMultiplierModifier.CollectMultipliableSpawnGroups(scene);
        var beforeSlots = beforeGroups.SelectMany(group => group.SpawnSlots).ToList();
        var beforeSpawnGuids = beforeSlots.Select(slot => slot.SpawnInfoGuid).ToHashSet();

        Assert.Contains(beforeGroups, group => group.SpawnSlots.Length > 1);

        var afterScene = EnemyMultiplierModifier.ProcessScene(
            scene,
            result.Randomizer,
            new RandomizerLogger(),
            TestScenePath,
            2.0,
            new Rng(0x5152));
        var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(afterScene);
        var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Count, 2.0);
        var newSlots = afterSlots
            .Where(slot => !beforeSpawnGuids.Contains(slot.SpawnInfoGuid))
            .ToList();

        Assert.Equal(targetCount, afterSlots.Length);
        Assert.Equal(beforeSlots.Count, newSlots.Count);
        Assert.All(newSlots, slot => {
            var activeSpawnInfoRefs =
                EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(slot.GenerationGameObject);

            Assert.Equal([slot.SpawnInfoGuid], activeSpawnInfoRefs.Distinct());
            Assert.DoesNotContain(slot.SpawnInfoGuid, beforeSpawnGuids);
        });
    }

    [Fact]
    public void Randomizer_EnemyMultiplierAboveOne_UpdatesChangedSceneCounts() {
        const double multiplier = 1.5;

        using var result = RandomizerTest.RunState(config => { config["enemy-multiplier"] = multiplier; });

        var changedScenePaths = GetChangedScenePaths(result, multiplier);

        Assert.NotEmpty(changedScenePaths);
        foreach (var path in changedScenePaths) {
            var beforeSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(result.ReadBeforeScene(path));
            var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(result.ReadAfterScene(path));
            var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Length, multiplier);

            Assert.NotEqual(beforeSlots.Length, targetCount);
            Assert.Equal(targetCount, afterSlots.Length);
        }
    }

    [Fact]
    public void Randomizer_EnemyMultiplierBelowOne_UpdatesChangedSceneCounts() {
        const double multiplier = 0.5;

        using var result = RandomizerTest.RunState(config => { config["enemy-multiplier"] = multiplier; });

        var changedScenePaths = GetChangedScenePaths(result, multiplier);

        Assert.NotEmpty(changedScenePaths);
        foreach (var path in changedScenePaths) {
            var beforeSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(result.ReadBeforeScene(path));
            var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(result.ReadAfterScene(path));
            var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Length, multiplier);

            Assert.NotEqual(beforeSlots.Length, targetCount);
            Assert.Equal(targetCount, afterSlots.Length);
        }
    }

    [Fact]
    public void Randomizer_EnemyMultiplierPreservesScriptedFlashbackScene() {
        using var result = RandomizerTest.RunState(config => { config["enemy-multiplier"] = 1.5; });

        var beforeSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(
            result.ReadBeforeScene(MiaPastVhsEnemyScenePath));
        var afterSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(
            result.ReadAfterScene(MiaPastVhsEnemyScenePath));

        Assert.NotEmpty(beforeSlots);
        Assert.False(result.WasFileModified(MiaPastVhsEnemyScenePath));
        Assert.Equal(beforeSlots.Select(slot => slot.SpawnInfoGuid), afterSlots.Select(slot => slot.SpawnInfoGuid));
    }

    [Fact]
    public void Randomizer_EnemyLimitExternalSpawnInfoMapping_DoesNotDisableVanillaWithDefaultMultiplier() {
        using var result = RandomizerTest.RunState(
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(
                    DynamicDataName.EnemyLimits,
                    System.Text.Encoding.UTF8.GetBytes(BuildEnemyLimitCsv(
                        ExternalGenerateScenePath,
                        maxEnemies: 1)));
            });

        var beforeSlots = EnemyMultiplierModifier.CollectLimitableSpawnSlots(
            result.ReadBeforeScene(ExternalGenerateScenePath),
            result.Randomizer.EnemySceneLimitService);
        var afterSlots = EnemyMultiplierModifier.CollectLimitableSpawnSlots(
            result.ReadAfterScene(ExternalGenerateScenePath),
            result.Randomizer.EnemySceneLimitService);

        Assert.True(beforeSlots.Length > 1);
        Assert.False(result.WasFileModified(ExternalGenerateScenePath));
        Assert.Equal(beforeSlots.Length, afterSlots.Length);
    }

    [Fact]
    public void Randomizer_EnemyLimitExternalSpawnInfoMapping_DisablesExcessWhenMultiplierReduces() {
        using var result = RandomizerTest.RunState(
            config => { config["enemy-multiplier"] = 0.5; },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(
                    DynamicDataName.EnemyLimits,
                    System.Text.Encoding.UTF8.GetBytes(BuildEnemyLimitCsv(
                        ExternalGenerateScenePath,
                        maxEnemies: 1)));
            });

        var beforeSlots = EnemyMultiplierModifier.CollectLimitableSpawnSlots(
            result.ReadBeforeScene(ExternalGenerateScenePath),
            result.Randomizer.EnemySceneLimitService);
        var afterSlots = EnemyMultiplierModifier.CollectLimitableSpawnSlots(
            result.ReadAfterScene(ExternalGenerateScenePath),
            result.Randomizer.EnemySceneLimitService);

        Assert.True(beforeSlots.Length > 1);
        Assert.True(result.WasFileModified(ExternalGenerateScenePath));
        Assert.Single(afterSlots);
    }

    private static List<string> GetChangedScenePaths(RandomizerRunResult result, double multiplier)
        => result.ChangedFiles.Keys
            .Where(path => path.EndsWith(".scn.20", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.StartsWith("natives/stm/scenes/items/resources/skl")) // Birthday skills
            .Where(path => {
                var beforeSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(result.ReadBeforeScene(path));
                var targetCount = EnemyMultiplierModifier.GetTargetEnemyCount(beforeSlots.Length, multiplier);
                return beforeSlots.Length > 0 && beforeSlots.Length != targetCount;
            })
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    private static (string Path, RszScene Scene,
        System.Collections.Immutable.ImmutableArray<EnemyMultiplierModifier.EnemySpawnSlot> Slots) FindSceneWithSlots(
            RandomizerRunResult result,
            int minSlots) {
        foreach (var area in result.AreaService.Areas) {
            var slots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(area.Scene);
            if (slots.Length >= minSlots) {
                return (area.Path, area.Scene, slots);
            }
        }

        throw new InvalidOperationException(
            $"No test scene found with at least {minSlots} multipliable enemy spawn slots.");
    }

    private static int CountPooledEnemyInstances(
        RszScene scene,
        IEnumerable<EnemyMultiplierModifier.EnemySpawnSlot> slots)
        => slots
            .Select(slot => slot.EnemyPoolGuid)
            .Distinct()
            .Select(guid => scene.FindGameObject(guid))
            .Where(pool => pool != null)
            .SelectMany(pool => pool!.Children)
            .Count(IsEnemyInstance);

    private static bool IsEnemyInstance(RszGameObject gameObject) {
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

    private static Guid GetComponentGuid(RszGameObject gameObject, string componentType, string fieldName) {
        var component = gameObject.FindComponent(componentType);
        Assert.NotNull(component);
        return RszSerializer.Deserialize<Guid>(component![fieldName]);
    }

    private static void AssertNoActiveGenerationObjectReferences(RszScene scene, Guid guid) {
        scene.VisitGameObjects(gameObject => {
            if (gameObject.FindComponent("via.fsm.Fsm") == null ||
                gameObject.FindComponent("app.TriggerInAction") == null) {
                return;
            }

            Assert.DoesNotContain(guid, EnemyMultiplierModifier.GetEnabledEnemyGenerateSpawnInfoRefs(gameObject));
        });
    }

    private static string BuildEnemyLimitCsv(string sceneFile, int maxEnemies)
        => $"""
            SceneFile,MaxEnemies,Comment
            {sceneFile},{maxEnemies},Test limit
            """;
}
