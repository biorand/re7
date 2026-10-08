using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;

namespace Biohazard.BioRand.RE7.Tests;

public class ExtraEnemyFilterTests {
    [Fact]
    public void SwarmBanRemovesPlacementWeightsButKeepsVanillaTuningAndValidVarietyDefault() {
        var definition = RandomizerExecutor.ConfigurationDefinition;
        foreach (var id in new[] { "insecthive", "insectswarm" }) {
            Assert.DoesNotContain(definition.AllItems, item => item.Id == $"enemy-ratio-{id}");
            Assert.Contains(definition.AllItems, item => item.Id == $"enemy-health-min-{id}");
            Assert.Contains(definition.AllItems, item => item.Id == $"enemy-speed-min-{id}");
        }
        var variety = Assert.Single(definition.AllItems, item => item.Id == "enemy-variety");
        Assert.Equal(EnemyDefinitions.Instance.Randomizable.Count, Convert.ToInt32(variety.Max));
        Assert.InRange(Convert.ToInt32(variety.Default), 1, EnemyDefinitions.Instance.Randomizable.Count);
    }

    [Theory]
    [InlineData("", "Molded*", "MoldedFat", "Molded MoldedBlade MoldedQuick")]
    [InlineData("", "molded\tMOLDEDBLADE", "*Blade", "Molded")]
    [InlineData("", "MoldedBlade", "", "MoldedBlade")]
    [InlineData("", "MoldedQuick", "Molded*", "")]
    [InlineData("", "Em4100", "", "")]
    [InlineData("Em4000|Em4100|Em4200", "Molded*", "MoldedFat", "Molded MoldedQuick")]
    [InlineData("Em4100", "", "MoldedQuick", "")]
    [InlineData("MoldedBlade", "", "", "MoldedBlade")]
    [InlineData("", "Insect*", "", "")]
    [InlineData("Em5510|Em5520|Em5400", "", "", "FlyingBug")]
    [InlineData("InsectHive|InsectSwarm", "", "", "")]
    public void ExplicitFiltersUseDefinitionIdsAndExcludeWins(string id, string include, string exclude, string expected) {
        var placement = new ExtraEnemyPlacement { Id = id, Include = include, Exclude = exclude };
        var candidates = ExtraEnemyPlanner.GetExplicitCandidates(placement, new EnemyPlacementRule("", include, exclude));
        Assert.Equal(expected.Split(' ', StringSplitOptions.RemoveEmptyEntries), candidates.Select(x => x.Id));
    }

    [Theory]
    [InlineData("", "", true)]
    [InlineData(" random ", "Molded*", true)]
    [InlineData("", "Molded*", false)]
    [InlineData("Em4100", "", false)]
    public void ExplicitIncludeCanReplaceLegacyIds(string id, string include, bool configured) {
        Assert.Equal(configured, ExtraEnemyPlanner.UsesConfiguredPool(new() { Id = id, Include = include }));
    }

    [Fact]
    public void EmbeddedRowsUseFiltersInsteadOfLegacyExplicitIds() {
        var rows = Csv.Deserialize<ExtraEnemyPlacement>(EmbeddedData.GetFile("extra_enemies.csv")).ToArray();
        Assert.NotEmpty(rows);
        Assert.All(rows, row => {
            Assert.True(row.Id is "" or "random");
            if (row.Id == "")
                Assert.NotEmpty(ExtraEnemyPlanner.GetExplicitCandidates(row, new EnemyPlacementRule("", row.Include, row.Exclude)));
        });
    }
}
