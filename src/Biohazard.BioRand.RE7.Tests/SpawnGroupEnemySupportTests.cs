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

    [Theory, Trait("Category", "RequiresPak")]
    [InlineData("MiaChainsaw", "app.OtherObjectSave")]
    [InlineData("EvelineElderly", "app.Em3300.Em3300Save")]
    public void StaticExtrasStartHiddenAndHaveUniqueNativeGroupSaveRecords(string id, string saveType) {
        using var result = RandomizerTest.RunState(config => config["extra-enemy-amount"] = 1.0,
            prepareRandomizer: randomizer => {
                SetGroup(randomizer);
                randomizer.DynamicData.SetData(DynamicDataName.ExtraEnemies, Encoding.UTF8.GetBytes(
                    "Enabled,Include,SceneFile,Chapter,PosX,RotW,SpawnGroup\n" +
                    $"TRUE,{id},{Environment},3,20,1,encounter\n" +
                    $"TRUE,{id},{Environment},3,25,1,encounter\n"));
            });
        var members = Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members;
        Assert.Equal(2, members.Count);
        Assert.Equal(2, members.Select(member => member.RuntimeGuid).Distinct().Count());
        Assert.Equal(2, result.Randomizer.SpawnGroupService.Manifest.Version);
        foreach (var member in members) {
            Assert.Equal("static", member.Kind);
            Assert.Equal(Environment, member.Scene);
            var actor = result.ReadAfterScene(member.Scene).FindGameObject(member.Guid)!;
            Assert.False(actor.Settings.Get<bool>("Update"));
            Assert.False(actor.Settings.Get<bool>("Draw"));
            Assert.Equal(member.RuntimeGuid, actor.FindComponent(saveType)!.Get<Guid>("SaveGUID"));
            if (id == "MiaChainsaw") {
                Assert.True(actor.FindComponent(saveType)!.Get<bool>("IsNotSaveBasicData"));
                Assert.NotEqual(member.RuntimeGuid, actor.FindComponent("app.EnemySave")!.Get<Guid>("SaveGUID"));
            }
        }
    }

    [Fact, Trait("Category", "RequiresPak")]
    public void StaticReplacementRetiresOriginalGenerationRequestsAndTransfersMembership() {
        Guid sourceGuid = default;
        using var result = RandomizerTest.RunState(config => {
                config["random-enemies"] = true;
                foreach (var enemy in EnemyDefinitions.Instance.All)
                    config[$"enemy-ratio-{enemy.Id.ToLowerInvariant()}"] = enemy.Id == "EvelineElderly" ? 1.0 : 0.0;
            }, prepareRandomizer: randomizer => {
                SetGroup(randomizer);
                using var repository = new FileRepository(randomizer, RandomizerTest.InputPakPath, randomizer.DynamicData);
                var scene = repository.GetScnFile(Scene).ReadScene(repository.TypeRepository);
                sourceGuid = scene.FindGameObject(go => EnemySpawnInfoRules.ShouldReplaceSpawnInfo(go) &&
                    go.FindComponent<app.EnemySpawnInfo>()?.UnitAlias == "Em4000")!.Guid;
                randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                    "Guid,SceneFile,IsSpawnInfo,Include,SpawnGroup\n" +
                    $"{sourceGuid},{Scene},TRUE,EvelineElderly,encounter\n"));
            });
        var member = Assert.Single(Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members);
        Assert.Equal("static", member.Kind);
        Assert.Contains(sourceGuid, result.Randomizer.SpawnGroupService.RetiredSpawnInfos);
        Assert.NotEqual(sourceGuid, member.Guid);
        var after = result.ReadAfterScene(Scene);
        Assert.Null(after.FindGameObject(sourceGuid));
        var actor = after.FindGameObject(member.Guid)!;
        Assert.Equal("Em3300_Static", actor.Name);
        Assert.False(actor.Settings.Get<bool>("Update"));
        foreach (var path in new[] {Scene, "natives/stm/scenes/chapter/chapter4/chapter4_2/hard.scn.20"}) {
            result.ReadAfterScene(path).Visit(node => {
                if (node is RszObjectNode action && action.Type.Name == "app.fsm.EnemyGenerate")
                    Assert.NotEqual(sourceGuid, action.Get<Guid>("SpawnInfo"));
            });
        }
    }

    private static void SetGroup(Randomizer randomizer) => randomizer.DynamicData.SetData(
        DynamicDataName.SpawnGroups, Encoding.UTF8.GetBytes(
            "Name,Parameter,State,X,Y,Z,Radius,Time,Notes\nencounter,spawn,,,,,,5,\n"));

    [Fact, Trait("Category", "RequiresPak")]
    public void OneGroupCanMixEverySupportedSpawnEnemyWithoutIdentityCollisions() {
        using var result = RandomizerTest.RunState(config => config["extra-enemy-amount"] = 1.0,
            prepareRandomizer: randomizer => {
                SetGroup(randomizer);
                var rows = EnemyDefinitions.Instance.Randomizable.Select((enemy, i) =>
                    $"TRUE,{enemy.Id},{Environment},3,{20 + i * 5},1,encounter");
                randomizer.DynamicData.SetData(DynamicDataName.ExtraEnemies, Encoding.UTF8.GetBytes(
                    "Enabled,Include,SceneFile,Chapter,PosX,RotW,SpawnGroup\n" + string.Join('\n', rows)));
            });
        var members = Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members;
        Assert.Equal(EnemyDefinitions.Instance.Randomizable.Count, members.Count);
        Assert.Equal(members.Count, members.Select(member => member.RuntimeGuid).Distinct().Count());
        Assert.Equal(members.Count, members.Select(member => member.Guid).Distinct().Count());
        Assert.Equal(2, members.Count(member => member.Kind == "static"));
        Assert.All(members, member => Assert.NotNull(result.ReadAfterScene(member.Scene).FindGameObject(member.Guid)));
    }

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
