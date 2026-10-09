using System.Text;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerEnemyPlacementRuleBehaviorTests {
    private const string ScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string ExternalScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/hard.scn.20";
    private const string GuidA = "ea2ed40e-2caa-4abb-a508-edc1dbf220c9";
    private const string GuidB = "591108aa-a595-4ae0-a3eb-07dddd6af697";

    [Fact]
    public void PreserveAndEmptyPoolKeepOriginalWhileAllowedReplacementIsAggroed() {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            foreach (var enemy in EnemyDefinitions.Instance.Randomizable)
                config[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enemy.Id == "MoldedQuick" ? 1.0 : 0.0;
        }, prepareRandomizer: randomizer => SetRows(randomizer,
            $"{GuidA},{ScenePath},TRUE,preserve,,",
            $"{GuidB},{ScenePath},TRUE,aggro,MoldedQuick,",
            $"b87a3dac-b714-4422-b8f1-93c52d5fe764,{ScenePath},TRUE,,MoldedQuick,Molded*"));

        var before = result.ReadBeforeScene(ScenePath);
        var after = result.ReadAfterScene(ScenePath);
        var quick = new EnemyTableEntry(EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "MoldedQuick"), 1);
        var fat = new EnemyTableEntry(EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == "MoldedFat"), 1);
        var fallback = EnemyModifier.SelectCompatibleEnemyPool(ScenePath, before.FindGameObject(new Guid(GuidB))!,
            [fat], [fat, quick], new EnemyPlacementRule("", "Molded*", "MoldedFat"));
        Assert.Equal(quick, Assert.Single(fallback));
        AssertOriginalSpawn(before, after, new Guid(GuidA));
        var replaced = after.FindGameObject(new Guid(GuidB))!;
        Assert.Equal("Em4100", replaced.FindComponent<app.EnemySpawnInfo>()!.UnitAlias);
        Assert.True(replaced.FindComponent<app.EnemySpawnInfo>()!.IsPlayerTargetingAtStart);
        Assert.True(replaced.FindComponent<app.EnemySpawnInfoOptionEm4100>()!.IsForceTargetingToPlayer);
        var emptyPoolGuid = new Guid("b87a3dac-b714-4422-b8f1-93c52d5fe764");
        AssertOriginalSpawn(before, after, emptyPoolGuid);
    }

    [Theory]
    [InlineData("Molded", false)]
    [InlineData("MoldedBlade", true)]
    public void DefinitionFilterDistinguishesMoldedBladeDespiteSharedAlias(string id, bool blade) {
        using var result = RandomizerTest.RunState(config => {
            config["random-enemies"] = true;
            config["balanced-enemies"] = false;
            foreach (var enemy in EnemyDefinitions.Instance.Randomizable)
                config[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enemy.Id is "Molded" or "MoldedBlade" ? 1.0 : 0.0;
        }, prepareRandomizer: randomizer => SetRows(randomizer, $"{GuidA},{ScenePath},TRUE,,{id},"));
        var option = result.ReadAfterScene(ScenePath).FindGameObject(new Guid(GuidA))!
            .FindComponent<app.EnemySpawnInfoOptionEm4000>();
        Assert.NotNull(option);
        Assert.Equal(blade, option.IsUseBlade);
    }

    [Fact]
    public void AggroWorksWithoutReplacementAndSurvivesMultiplication() {
        using var baseline = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer));
        var originalScene = baseline.ReadBeforeScene(ScenePath);
        var originalSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(originalScene);
        Assert.NotEmpty(originalSlots);
        var rows = originalSlots.Select(slot => $"{slot.SpawnInfoGuid},{ScenePath},TRUE,aggro,,").ToArray();
        using var result = RandomizerTest.RunState(config => config["enemy-multiplier"] = 2.0,
            prepareRandomizer: randomizer => SetRows(randomizer, rows));
        var after = result.ReadAfterScene(ScenePath);
        var multipliedSlots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(after);
        Assert.Equal(originalSlots.Length * 2, multipliedSlots.Length);
        foreach (var slot in multipliedSlots) {
            var spawn = after.FindGameObject(slot.SpawnInfoGuid)!;
            Assert.True(spawn.FindComponent<app.EnemySpawnInfo>()!.IsPlayerTargetingAtStart);
            var options = spawn.Components.Where(EnemySpawnInfoRules.SupportsForceTargetingOption).ToArray();
            Assert.NotEmpty(options);
            Assert.All(options, option => Assert.True(option.Get<bool>("IsForceTargetingToPlayer")));
        }
        foreach (var slot in originalSlots) {
            Assert.Equal(originalScene.FindGameObject(slot.SpawnInfoGuid)!.FindComponent<app.EnemySpawnInfo>()!.UnitAlias,
                after.FindGameObject(slot.SpawnInfoGuid)!.FindComponent<app.EnemySpawnInfo>()!.UnitAlias);
        }
    }

    [Fact]
    public void CullRemovesSpawnAndDisablesSameSceneAndExternalGenerationEvenWithoutRandomization() {
        using var baseline = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer));
        var originalScene = baseline.ReadBeforeScene(ScenePath);
        var localGuid = GetGenerateRefs(originalScene).First(guid => originalScene.FindGameObject(guid) != null);
        var externalGuid = GetGenerateRefs(baseline.ReadBeforeScene(ExternalScenePath))
            .First(guid => originalScene.FindGameObject(guid) != null);
        Assert.NotEqual(localGuid, externalGuid);
        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer,
            $"{localGuid},{ScenePath},TRUE,cull,,",
            $"{externalGuid},{ScenePath},TRUE,cull,,"));
        var guids = new[] { localGuid, externalGuid };
        Assert.All(guids, guid => Assert.Null(result.ReadAfterScene(ScenePath).FindGameObject(guid)));
        var beforeRefs = GetGenerateRefs(result.ReadBeforeScene(ScenePath))
            .Concat(GetGenerateRefs(result.ReadBeforeScene(ExternalScenePath))).ToHashSet();
        Assert.All(guids, guid => Assert.Contains(guid, beforeRefs));
        foreach (var path in new[] { ScenePath, ExternalScenePath }) {
            Assert.DoesNotContain(GetGenerateRefs(result.ReadAfterScene(path)), guid => guids.Contains(guid));
            result.ReadAfterScene(path).Visit(node => {
                if (node is RszObjectNode spawnUnit && spawnUnit.Type.Name == "app.CharacterExistZoneGroup.SpawnUnit")
                    Assert.DoesNotContain(spawnUnit.Get<Guid>("spawnInfo"), guids);
                return node;
            });
        }
    }

    [Fact]
    public void NodupPreventsCloningButAllowsRemovalAndPreservePreventsBoth() {
        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer));
        var scene = result.ReadBeforeScene(ScenePath);
        var slots = EnemyMultiplierModifier.CollectMultipliableSpawnSlots(scene);
        Assert.True(slots.Length > 1);
        // Use separate randomizers so the cached rules represent each supplied sheet.
        var protectedGuid = slots[0].SpawnInfoGuid;
        var rows = slots.Select(slot => $"{slot.SpawnInfoGuid},{ScenePath},TRUE,{(slot.SpawnInfoGuid == protectedGuid ? "preserve" : "nodup")},,").ToArray();
        using var withRules = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer, rows));
        var increased = EnemyMultiplierModifier.ProcessScene(scene, withRules.Randomizer, new RandomizerLogger(),
            ScenePath, 2.0, new Rng(987));
        Assert.Equal(slots.Length, EnemyMultiplierModifier.CollectMultipliableSpawnSlots(increased).Length);
        var reduced = EnemyMultiplierModifier.ProcessScene(scene, withRules.Randomizer, new RandomizerLogger(),
            ScenePath, 0.0, new Rng(987));
        Assert.Equal(protectedGuid, Assert.Single(EnemyMultiplierModifier.CollectMultipliableSpawnSlots(reduced)).SpawnInfoGuid);
    }

    [Fact]
    public void PreservedExternalSpawnCountsTowardSceneLimitAndIsNotDisabled() {
        using var baseline = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer));
        var originalScene = baseline.ReadBeforeScene(ScenePath);
        var externalGuids = GetGenerateRefs(baseline.ReadBeforeScene(ExternalScenePath))
            .Where(guid => originalScene.FindGameObject(guid)?.FindComponent<app.EnemySpawnInfo>() != null)
            .Distinct().ToArray();
        Assert.True(externalGuids.Length > 1);
        var preservedGuid = externalGuids[0];
        using var result = RandomizerTest.RunState(config => config["enemy-multiplier"] = 0.5,
            prepareRandomizer: randomizer => {
                var rows = externalGuids.Select(guid =>
                    $"{guid},{ScenePath},TRUE,TRUE,Em4200,{(guid == preservedGuid ? "preserve" : "")}");
                randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                    "Guid,SceneFile,IsSpawnInfo,Enabled,EnemyID,Tags\n" + string.Join('\n', rows) + "\n"));
                randomizer.DynamicData.SetData(DynamicDataName.EnemyLimits, Encoding.UTF8.GetBytes(
                    $"SceneFile,MaxEnemies\n{ExternalScenePath},1\n"));
            });
        var remaining = GetGenerateRefs(result.ReadAfterScene(ExternalScenePath)).Where(externalGuids.Contains).Distinct();
        Assert.Equal(preservedGuid, Assert.Single(remaining));
    }

    [Fact]
    public void CullingMeshChildRemovesActorRootInsteadOfLeavingInvisibleEnemy() {
        const string path = "natives/stm/scenes/chapter/chapter3/enemy_c03_1.scn.20";
        using var baseline = RandomizerTest.RunState(prepareRandomizer: randomizer => SetRows(randomizer));
        var before = baseline.ReadBeforeScene(path);
        var root = before.FindGameObject(go => go.FindComponent<app.EnemySave>() != null)!;
        Assert.NotNull(root);
        var meshChild = root.Children.OfType<RszGameObject>()
            .First(go => go.FindComponent("via.render.Mesh") != null);
        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer =>
            SetRows(randomizer, $"{meshChild.Guid},{path},FALSE,cull,,"));
        var after = result.ReadAfterScene(path);
        Assert.Null(after.FindGameObject(root.Guid));
        Assert.Null(after.FindGameObject(meshChild.Guid));
        Assert.NotEmpty(after.Children);
    }

    private static void SetRows(Randomizer randomizer, params string[] rows)
        => randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
            "Guid,SceneFile,IsSpawnInfo,Tags,Include,Exclude\n" + string.Join('\n', rows) + "\n"));

    private static List<Guid> GetGenerateRefs(RszScene scene) {
        var refs = new List<Guid>();
        scene.Visit(node => {
            if (node is RszObjectNode action && action.Type.Name == "app.fsm.EnemyGenerate" &&
                action.Get<bool>("v0_Enabled") && action.Get<Guid>("SpawnInfo") != Guid.Empty)
                refs.Add(action.Get<Guid>("SpawnInfo"));
        });
        return refs;
    }

    private static void AssertOriginalSpawn(RszScene before, RszScene after, Guid guid) {
        var original = before.FindGameObject(guid)!;
        var actual = after.FindGameObject(guid)!;
        Assert.Equal(original.Name, actual.Name);
        Assert.Equal(original.FindComponent<app.EnemySpawnInfo>()!.UnitAlias,
            actual.FindComponent<app.EnemySpawnInfo>()!.UnitAlias);
        Assert.Equal(original.FindComponent<app.EnemySpawnInfoOptionEm4000>()!.IsForceTargetingToPlayer,
            actual.FindComponent<app.EnemySpawnInfoOptionEm4000>()!.IsForceTargetingToPlayer);
    }
}
