using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Patches;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerGuestHouseItemBoxTests {
    [Fact]
    public void GuestHouseBathroom_HasStorageEvenWithoutAdditionalItems() {
        using var result = RandomizerTest.RunState();
        var before = result.ReadBeforeScene(GuestHouseHealingPatch.ScenePath);
        var after = result.ReadAfterScene(GuestHouseHealingPatch.ScenePath);
        var existing = before.GetGameObjects().Select(go => go.Guid).ToHashSet();
        var box = Assert.Single(after.GetGameObjects(), go => !existing.Contains(go.Guid) && go.Name == "ItemBox");
        Assert.Contains(box.Children, child => child.FindComponent<app.InteractSendFsm>() is { Enabled: true, IsEnable: true });
        Assert.Contains(box.Components, component => component.Type.Name == "via.fsm.Fsm");
        Assert.Equal(new System.Numerics.Vector3(-1.6f, 0, -6.1f), box.FindComponent<GeneratedViaTransform>()!.Position);
    }
}
