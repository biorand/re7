using Biohazard.BioRand.RE7.Patches;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerGuestHouseSafetyTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BathroomDrawer_GuaranteesStrongFirstAidWithItemRandomization(bool randomItems) {
        using var result = RandomizerTest.RunState(config => config["random-items"] = randomItems);
        var scene = result.ReadAfterScene(GuestHouseHealingPatch.ScenePath);
        var drawer = scene.FindGameObject(GuestHouseHealingPatch.DrawerGuid)!.FindComponent<app.InteractDrawer>()!;
        Assert.Equal("RemedyL", drawer.SetItemID);
        Assert.Equal(1, drawer.ChangeStackNum);
        Assert.False(drawer.IsHardNoItem);
        Assert.False(drawer.IsDirectGameObjectSet);
    }
}
