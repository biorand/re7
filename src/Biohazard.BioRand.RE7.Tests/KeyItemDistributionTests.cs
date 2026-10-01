namespace Biohazard.BioRand.RE7.Tests;

public partial class RandomizerKeyItemLocationBehaviorTests {
    [Theory]
    [InlineData(895914, false, "Normal")]
    [InlineData(895914, true, "Main House")]
    [InlineData(776198, true, "Normal")]
    [InlineData(736361, false, "Main House")]
    public void KeyItemLocations_SpreadsChapter3KeysBeyondKitchenAndPantry(int seed, bool extras, string start) {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["additional-items"] = extras;
            config["additional-wooden-crates"] = extras;
            config["start-chapter"] = start;
        }, seed);

        var keys = GetChangedPlacements(result).Where(change =>
            change.Placement.Chapter == 3 && ExpectedRules.ContainsKey(change.AfterId)).ToArray();
        // Distribution must not improve by silently leaving more keys vanilla.
        Assert.Equal(ExpectedRules.Count(rule => rule.Value.Chapter == 3), keys.Length);
        var kitchenKeys = keys.Where(change =>
            change.Placement.SceneFile == MainHouseDiningKitchenScenePath ||
            change.Placement.SceneFile == MainHousePantryScenePath).ToArray();
        Assert.Contains(kitchenKeys, change => change.AfterId == "EthanCarKey");
        Assert.InRange(kitchenKeys.Length, 1, 2);
        Assert.True(keys.Select(change => change.Placement.SceneFile).Distinct().Count() >= 8);
        Assert.All(keys.GroupBy(change => change.Placement.SceneFile), room => Assert.InRange(room.Count(), 1, 2));
        AssertHatchKeyRemainsVanilla(result);
        AssertPhysicalProgression(result);
    }

    [Fact]
    public void KeyItemLocations_RoomDistributionRemainsDeterministic() {
        static void Configure(IntelOrca.Biohazard.BioRand.RandomizerConfiguration config) {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["additional-items"] = true;
        }
        using var first = RandomizerTest.RunState(Configure, 895914);
        using var second = RandomizerTest.RunState(Configure, 895914);
        Assert.Equal(first.ChangedFiles.Keys.Order(StringComparer.OrdinalIgnoreCase),
            second.ChangedFiles.Keys.Order(StringComparer.OrdinalIgnoreCase));
        foreach (var path in first.ChangedFiles.Keys)
            Assert.Equal(first.ChangedFiles[path], second.ChangedFiles[path]);
    }
}
