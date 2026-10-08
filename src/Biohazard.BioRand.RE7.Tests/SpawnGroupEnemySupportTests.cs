using System.Text;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class SpawnGroupEnemySupportTests {
    private const string Scene = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string Environment = "natives/stm/environment/scene/chapter3/c03_mainhouse1fliving.scn.20";
    private const string Generator = "natives/stm/scenes/chapter/chapter3/enemy_c03.scn.20";

    public static IEnumerable<object[]> GeneratorEnemies => EnemyDefinitions.Instance.All
        .Where(enemy => enemy.SpawnOptionType != null).Select(enemy => new object[] { enemy.Id });

    [Theory, MemberData(nameof(GeneratorEnemies)), Trait("Category", "RequiresPak")]
    public void ExplicitExtraFiltersSupportEveryRegisteredGeneratorEnemy(string id) {
        using var result = RandomizerTest.RunState(config => config["extra-enemy-amount"] = 1.0,
            prepareRandomizer: randomizer => {
                SetGroup(randomizer);
                randomizer.DynamicData.SetData(DynamicDataName.ExtraEnemies, Encoding.UTF8.GetBytes(
                    "Enabled,Id,Include,Exclude,SceneFile,Chapter,PosX,RotW,SpawnGroup\n" +
                    $"TRUE,,{id},,{Environment},3,20,1,encounter\n" +
                    $"TRUE,,{id},{id},{Environment},3,25,1,encounter\n"));
            });
        var member = Assert.Single(Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members);
        Assert.Equal(Generator, member.Scene);
        AssertGeneratorMember(result.ReadAfterScene(member.Scene), member, id);
    }

    [Theory, MemberData(nameof(GeneratorEnemies)), Trait("Category", "RequiresPak")]
    public void ReplacementFiltersAllowEveryRegisteredGeneratorEnemyInGroups(string id) {
        Guid sourceGuid = default;
        using var result = RandomizerTest.RunState(config => {
                config["random-enemies"] = true;
                foreach (var enemy in EnemyDefinitions.Instance.All)
                    config[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enemy.Id == id ? 1.0 : 0.0;
            }, prepareRandomizer: randomizer => {
                SetGroup(randomizer);
                using var repository = new FileRepository(randomizer, RandomizerTest.InputPakPath, randomizer.DynamicData);
                var scene = repository.GetScnFile(Scene).ReadScene(repository.TypeRepository);
                sourceGuid = scene.FindGameObject(go => EnemySpawnInfoRules.ShouldReplaceSpawnInfo(go) &&
                    go.FindComponent<app.EnemySpawnInfo>()?.UnitAlias == "Em4000")!.Guid;
                randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                    "Guid,SceneFile,IsSpawnInfo,Tags,Include,SpawnGroup\n" +
                    $"{sourceGuid},{Scene},TRUE,aggro,{id},encounter\n"));
            });
        var member = Assert.Single(Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members);
        Assert.Equal(sourceGuid, member.Guid);
        Assert.True(member.Aggro);
        AssertGeneratorMember(result.ReadAfterScene(Scene), member, id);
    }

    private static void SetGroup(Randomizer randomizer) => randomizer.DynamicData.SetData(
        DynamicDataName.SpawnGroups, Encoding.UTF8.GetBytes(
            "Name,Parameter,State,X,Y,Z,Radius,Time,Notes\nencounter,spawn,,,,,,5,\n"));

    private static void AssertGeneratorMember(RszScene scene, SpawnGroupMember member, string id) {
        var enemy = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == id);
        var go = scene.FindGameObject(member.Guid)!;
        var spawn = go.FindComponent<app.EnemySpawnInfo>()!;
        Assert.Equal(enemy.EnemyId.ToString(), spawn.UnitAlias);
        Assert.Equal(spawn.MyGUID, member.RuntimeGuid);
        Assert.NotEqual(Guid.Empty, member.RuntimeGuid);
        Assert.NotNull(go.FindComponent(enemy.SpawnOptionType!));
        Assert.False(spawn.autoSpawnParameter.IsAutoSpawnedAtLoad);
        Assert.False(spawn.suspendParameter.isUseSelfSuspend);
        Assert.False(spawn.suspendParameter.IsForgetBackup);
        Assert.True(spawn.suspendParameter.IsOnTheSpot);
        Assert.All(spawn.RespawnConditions, row => Assert.False(row.respawnCondition.IsUse));
        if (id is "Molded" or "MoldedBlade")
            Assert.Equal(id == "MoldedBlade", go.FindComponent<app.EnemySpawnInfoOptionEm4000>()!.IsUseBlade);
    }
}
