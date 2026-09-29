using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Patches;

internal class EaglePuzzlePatch(IPatchContext context) : IPatch {
    internal static readonly string ScenePath = "environment/scene/chapter3/c03_mainhousehall.scn".SceneFile();
    internal static readonly Guid[] InteractionGuids = [
        new("2b4c9402-af3c-4a36-a67d-0eec6a63b783"),
        new("eb6bf976-5420-46e3-8062-43daf4c8932a")
    ];

    public void Apply() {
        context.ModifyScnFile(ScenePath, scene => {
            foreach (var guid in InteractionGuids) {
                var interaction = scene.FindGameObject(guid)!;
                interaction = interaction.Visit(node => node is RszObjectNode obj &&
                    obj.Type.Name == "app.InteractObjectBase.EnemyCheckParam"
                        ? obj.SetField("IsEnemyTriggerInCheck", false).SetField("IsCheckNotDiscovery", false)
                        : node);
                scene = scene.UpdateGameObject(interaction);
            }
            return scene;
        });
    }
}
