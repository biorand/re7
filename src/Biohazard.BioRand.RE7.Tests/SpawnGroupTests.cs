using System.Text;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class SpawnGroupTests {
    private const string Header = "Name,Parameter,State,X,Y,Z,Radius,Time,Notes\n";
    private const string ScenePath = "natives/stm/scenes/chapter/chapter4/chapter4_2/moldeads.scn.20";
    private const string GuidA = "ea2ed40e-2caa-4abb-a508-edc1dbf220c9";

    [Fact]
    public void ContinuationRowsMergeNamesAndPreserveConditionOrder() {
        var groups = SpawnGroupTable.Parse(Encoding.UTF8.GetBytes(Header +
            "Encounter,spawn,,,,,,0,\n,resume,Start,1,2,3,5,2,\n,,,,,,,,notes\nencounter,suspend,End,,,,,0,\n"));
        var group = Assert.Single(groups).Value;
        Assert.Equal("Encounter", group.Name);
        Assert.Equal(new[] { "spawn", "resume", "suspend" }, group.Conditions.Select(x => x.Parameter));
        Assert.Equal(2, group.Conditions[1].Time);
        Assert.Equal(5, group.Conditions[1].Radius);
    }

    [Theory]
    [InlineData(",spawn,,,,,,,", "Name")]
    [InlineData("A,waypoint,,1,2,3,5,,", "waypoint")]
    [InlineData("A,spawn,,1,2,,5,,", "X, Y, Z")]
    [InlineData("A,spawn,,,,,5,,", "X, Y, Z")]
    [InlineData("A,spawn,,1,2,3,0,,", "positive Radius")]
    [InlineData("A,spawn,,,,,,-1,", "Time")]
    [InlineData("A,spawn,,NaN,2,3,5,,", "finite")]
    [InlineData("A,resume,,,,,,,", "State or position")]
    [InlineData("A,spawn,wrong|core|state,,,,,,", "qualified State")]
    public void InvalidConditionsReportTheSheetRow(string row, string message) {
        var error = Assert.Throws<InvalidDataException>(() => SpawnGroupTable.Parse(Encoding.UTF8.GetBytes(Header + row)));
        Assert.Contains("row 2", error.Message);
        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void QualifiedStateNormalizesTheGuidCoreAndStateWhitespace() {
        var groups = SpawnGroupTable.Parse(Encoding.UTF8.GetBytes(Header +
            "A,spawn,{EA2ED40E-2CAA-4ABB-A508-EDC1DBF220C9}|00| Start ,,,,,,\n"));
        Assert.Equal($"{GuidA}|0|Start", Assert.Single(Assert.Single(groups).Value.Conditions).State);
    }

    [Fact, Trait("Category", "RequiresPak")]
    public void FinalManifestIncludesMultipliedMembersAndDisablesOnlyTheirVanillaRequests() {
        using var result = RandomizerTest.RunState(config => config["enemy-multiplier"] = 2.0,
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(DynamicDataName.SpawnGroups, Encoding.UTF8.GetBytes(Header + "test,spawn,,,,,,0,\n"));
                // Register every ordinary slot in one scene, including external difficulty requests.
                using var repository = new FileRepository(randomizer, RandomizerTest.InputPakPath, randomizer.DynamicData);
                var scene = repository.GetScnFile(ScenePath).ReadScene(repository.TypeRepository);
                var objects = new List<RszGameObject>();
                scene.VisitGameObjects(go => { objects.Add(go); });
                var rows = objects.Where(EnemySpawnInfoRules.ShouldReplaceSpawnInfo)
                    .Where(go => go.FindComponent<app.EnemySpawnInfo>()?.UnitAlias is "Em4000" or "Em4100" or "Em4200")
                    .Select(go => $"{go.Guid},{ScenePath},TRUE,aggro,test");
                randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                    "Guid,SceneFile,IsSpawnInfo,Tags,SpawnGroup\n" + string.Join('\n', rows) + "\n"));
            });
        var manifest = result.Randomizer.SpawnGroupService.Manifest;
        var group = Assert.Single(manifest.Groups);
        Assert.True(result.Randomizer.IsREFrameworkRequired());
        Assert.True(group.Members.Count > 1);
        Assert.Equal(group.Members.Count, group.Members.Select(x => x.RuntimeGuid).Distinct().Count());
        var before = result.ReadBeforeScene(ScenePath);
        Assert.Contains(group.Members, member => before.FindGameObject(member.Guid) == null);
        var after = result.ReadAfterScene(ScenePath);
        foreach (var member in group.Members) {
            Assert.True(member.Aggro);
            var spawn = after.FindGameObject(member.Guid)!.FindComponent<app.EnemySpawnInfo>()!;
            Assert.Equal(spawn.MyGUID, member.RuntimeGuid);
            Assert.False(spawn.autoSpawnParameter.IsAutoSpawnedAtLoad);
            Assert.False(spawn.suspendParameter.isUseSelfSuspend);
            Assert.True(spawn.suspendParameter.IsOnTheSpot);
            Assert.All(spawn.RespawnConditions, row => Assert.False(row.respawnCondition.IsUse));
        }
        var ids = group.Members.Select(x => x.Guid).ToHashSet();
        foreach (var path in new[] { ScenePath, "natives/stm/scenes/chapter/chapter4/chapter4_2/hard.scn.20" }) {
            result.ReadAfterScene(path).Visit(node => {
                if (node is RszObjectNode action && action.Type.Name == "app.fsm.EnemyGenerate")
                    Assert.False(action.Get<bool>("v0_Enabled") && ids.Contains(action.Get<Guid>("SpawnInfo")));
            });
        }
    }

    [Theory, Trait("Category", "RequiresPak")]
    [InlineData("preserve")]
    [InlineData("cull")]
    public void PreserveAndCullTakePrecedenceOverGroupControl(string tag) {
        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer => {
            randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                $"Guid,SceneFile,IsSpawnInfo,Tags,SpawnGroup\n{GuidA},{ScenePath},TRUE,{tag},ignored\n"));
        });
        Assert.Empty(result.Randomizer.SpawnGroupService.Manifest.Groups);
    }

    [Theory, Trait("Category", "RequiresPak")]
    [InlineData("Em4000")]
    [InlineData("random")]
    public void ExtraMembersUseTheirActualGeneratorSceneAndSurviveSerialization(string id) {
        const string environment = "natives/stm/environment/scene/chapter3/c03_mainhouse1fliving.scn.20";
        const string generator = "natives/stm/scenes/chapter/chapter3/enemy_c03.scn.20";
        using var result = RandomizerTest.RunState(config => {
                config["extra-enemy-amount"] = 1.0;
                config["enemy-ratio-molded"] = 1000.0;
            },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(DynamicDataName.SpawnGroups, Encoding.UTF8.GetBytes(Header + "extra,spawn,,,,,,,\n"));
                randomizer.DynamicData.SetData(DynamicDataName.ExtraEnemies, Encoding.UTF8.GetBytes(
                    $"Enabled,Id,SceneFile,Chapter,PosX,PosY,PosZ,RotW,SpawnGroup\nTRUE,{id},{environment},3,20,0,0,1,extra\n"));
            });
        var member = Assert.Single(Assert.Single(result.Randomizer.SpawnGroupService.Manifest.Groups).Members);
        Assert.Equal(generator, member.Scene);
        Assert.Equal(member.RuntimeGuid, result.ReadAfterScene(generator).FindGameObject(member.Guid)!
            .FindComponent<app.EnemySpawnInfo>()!.MyGUID);
    }

    [Theory, Trait("Category", "RequiresPak")]
    [InlineData(true)]
    [InlineData(false)]
    public void RemovedMembersAreSuppressedWithAndWithoutSceneLimits(bool withLimit) {
        var memberGuid = Guid.Empty;
        using var result = RandomizerTest.RunState(config => config["enemy-multiplier"] = 0.0,
            prepareRandomizer: randomizer => {
                using var repository = new FileRepository(randomizer, RandomizerTest.InputPakPath, randomizer.DynamicData);
                var scene = repository.GetScnFile(ScenePath).ReadScene(repository.TypeRepository);
                memberGuid = EnemyMultiplierModifier.CollectLimitableSpawnSlots(scene)
                    .First(slot => slot.UnitAlias == "Em4000").SpawnInfoGuid;
                randomizer.DynamicData.SetData(DynamicDataName.SpawnGroups, Encoding.UTF8.GetBytes(Header + "test,spawn,,,,,,,\n"));
                randomizer.DynamicData.SetData(DynamicDataName.Enemies, Encoding.UTF8.GetBytes(
                    $"Guid,SceneFile,IsSpawnInfo,Enabled,EnemyID,SpawnGroup\n{memberGuid},{ScenePath},TRUE,TRUE,Em4000,test\n"));
                randomizer.DynamicData.SetData(DynamicDataName.EnemyLimits, Encoding.UTF8.GetBytes(
                    "SceneFile,MaxEnemies\n" + (withLimit ? $"{ScenePath},0\n" : "")));
            });
        Assert.Contains(memberGuid, result.Randomizer.SpawnGroupService.Suppressed);
        Assert.Empty(result.Randomizer.SpawnGroupService.Manifest.Groups);
        var spawn = result.ReadAfterScene(ScenePath).FindGameObject(memberGuid);
        if (withLimit) {
            Assert.NotNull(spawn); // The limit path disables requests, without deleting the spawn slot.
            Assert.False(spawn.FindComponent<app.EnemySpawnInfo>()!.autoSpawnParameter.IsAutoSpawnedAtLoad);
        } else {
            Assert.Null(spawn);
        }
    }
}
