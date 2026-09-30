using Biohazard.BioRand.RE7.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerLucasItemBoxTests {
    [Fact]
    public void LucasPuzzle_HasStorageInsideEvenWithoutAdditionalItems() {
        const string path = "natives/stm/environment/scene/chapter3/c03_leftarea1fpuzzleroom1.scn.20";
        using var result = RandomizerTest.RunState();
        var before = result.ReadBeforeScene(path);
        var after = result.ReadAfterScene(path);
        var existing = before.GetGameObjects().Select(go => go.Guid).ToHashSet();
        var box = Assert.Single(after.GetGameObjects(), go => !existing.Contains(go.Guid) && go.Name == "ItemBox");
        Assert.Contains(box.Children, child => child.FindComponent<app.InteractSendFsm>() is { Enabled: true, IsEnable: true });
        Assert.Contains(box.Components, component => component.Type.Name == "via.fsm.Fsm");
        Assert.Equal(new System.Numerics.Vector3(51.1f, -0.6f, 22.4f), box.FindComponent<GeneratedViaTransform>()!.Position);
    }
}
