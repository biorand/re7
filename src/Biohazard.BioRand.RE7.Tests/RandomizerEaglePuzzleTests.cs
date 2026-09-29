using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerEaglePuzzleTests {
    [Fact]
    public void EaglePuzzle_RemovesEnemyGateFromBothInteractionsOnly() {
        using var result = RandomizerTest.RunState();
        var before = result.ReadBeforeScene(EaglePuzzlePatch.ScenePath);
        var after = result.ReadAfterScene(EaglePuzzlePatch.ScenePath);
        foreach (var go in before.GetGameObjects()) {
            var original = EnemyChecks(go);
            var modified = EnemyChecks(after.FindGameObject(go.Guid)!);
            Assert.Equal(original.Count, modified.Count);
            for (var i = 0; i < original.Count; i++) {
                var isPuzzle = EaglePuzzlePatch.InteractionGuids.Contains(go.Guid);
                Assert.Equal(isPuzzle ? false : original[i].Get<bool>("IsEnemyTriggerInCheck"),
                    modified[i].Get<bool>("IsEnemyTriggerInCheck"));
            }
        }
        foreach (var guid in EaglePuzzlePatch.InteractionGuids) {
            Assert.True(Assert.Single(EnemyChecks(before.FindGameObject(guid)!)).Get<bool>("IsEnemyTriggerInCheck"));
        }
    }

    private static List<RszObjectNode> EnemyChecks(RszGameObject go) {
        var result = new List<RszObjectNode>();
        go.WithChildren([]).Visit(node => {
            if (node is RszObjectNode obj && obj.Type.Name == "app.InteractObjectBase.EnemyCheckParam") result.Add(obj);
        });
        return result;
    }
}
