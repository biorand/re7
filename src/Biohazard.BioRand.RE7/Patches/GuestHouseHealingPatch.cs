using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Patches;

internal class GuestHouseHealingPatch(IPatchContext context) : IPatch {
    internal static readonly string ScenePath = "environment/scene/chapter1/c01_bathroom01.scn".SceneFile();
    internal static readonly Guid DrawerGuid = new("be9e9bb7-e25d-00f8-2788-7d5afcdfb432");

    public void Apply() {
        context.ModifyScnFile(ScenePath, scene => {
            var gameObject = scene.FindGameObject(DrawerGuid)!;
            var drawer = gameObject.FindComponent<app.InteractDrawer>()!;
            // The first Mia encounter includes unavoidable damage. Keep a full heal
            // available even when the player's starting inventory is randomized.
            drawer.SetItemID = "RemedyL";
            drawer.ChangeStackNum = 1;
            drawer.IsHardNoItem = false;
            return scene.UpdateGameObject(gameObject.AddOrUpdateComponent(drawer));
        });
    }
}
