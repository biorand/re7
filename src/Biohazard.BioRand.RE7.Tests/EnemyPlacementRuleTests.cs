using System.Text;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Services;

namespace Biohazard.BioRand.RE7.Tests;

public class EnemyPlacementRuleTests {
    [Theory]
    [InlineData("", "", "Molded", true)]
    [InlineData("Molded*", "MoldedFat", "MoldedBlade", true)]
    [InlineData("Molded*", "MoldedFat", "MoldedFat", false)]
    [InlineData("molded\tMOLDEDQUICK", "", "MoldedQuick", true)]
    [InlineData("Molded", "", "MoldedBlade", false)]
    [InlineData("Molded*", "*Blade", "MoldedBlade", false)]
    [InlineData("MissingEnemy", "", "Molded", false)]
    [InlineData("FlyingBug Insect*", "", "Molded", false)]
    public void ReplacementListsMatchDefinitionIdsAndExcludeWins(string include, string exclude, string id, bool allowed) {
        var enemy = EnemyDefinitions.Instance.All.Single(enemy => enemy.Id == id);
        Assert.Equal(allowed, new EnemyPlacementRule("", include, exclude).AllowsReplacement(enemy));
    }

    [Theory]
    [InlineData("preserve")]
    [InlineData("prefab exclude")]
    [InlineData("cull")]
    public void ProtectedOrCulledPlacementsCannotBeReplaced(string tags) {
        var rule = new EnemyPlacementRule(tags, "*", "");
        Assert.False(rule.AllowsReplacement(EnemyDefinitions.Instance.Randomizable[0]));
    }

    [Theory]
    [InlineData("cull preserve")]
    [InlineData("cull aggro")]
    [InlineData("preserv")]
    public void InvalidTagsFailClearly(string tags)
        => Assert.Throws<InvalidDataException>(() => new EnemyPlacementRule(tags, "", ""));

    [Fact]
    public void SceneRulesUseSceneAndGuidWhileExternalRequestsResolveSpawnGuid() {
        const string guid = "ea2ed40e-2caa-4abb-a508-edc1dbf220c9";
        var service = new EnemyPlacementService(Encoding.UTF8.GetBytes($"""
            EnemyID,Guid,SceneFile,IsSpawnInfo,Tags,Include,Exclude
            Em3000,{guid},natives/stm/one.scn.20,TRUE,aggro nodup,Molded*,MoldedFat
            Em8000,{guid},natives/stm/two.scn.20,FALSE,preserve,,
            """));
        Assert.True(service.GetRule("NATIVES\\STM\\ONE.SCN.20", Guid.Parse(guid)).Aggro);
        Assert.True(service.GetRule("natives/stm/two.scn.20", Guid.Parse(guid)).Preserve);
        Assert.True(service.GetSpawnRule(Guid.Parse(guid)).NoDuplicates);
        Assert.False(service.GetRule("natives/stm/missing.scn.20", Guid.Parse(guid)).Preserve);
    }

    [Fact]
    public void EmbeddedSheetLoadsWithoutParsingLegacyActorAliasesAsEnum() {
        var service = new EnemyPlacementService(EmbeddedData.GetFile("enemies.csv"));
        Assert.True(service.GetRule("natives/stm/scenes/enemy/em8100.scn.20",
            new Guid("2c90294a-df2c-491e-a555-74a96b82a588")).Preserve);
        Assert.True(service.GetSpawnRule(new Guid("ea2ed40e-2caa-4abb-a508-edc1dbf220c9")).Aggro);
        Assert.Empty(service.Culls);
    }
}
