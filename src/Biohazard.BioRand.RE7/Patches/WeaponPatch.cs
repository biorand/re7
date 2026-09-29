using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Patches;

internal class WeaponPatch(IPatchContext context) : IPatch {
    private const string ChainSawItemId = "ChainSaw";

    private readonly string _weaponPrefabPath = "prefab/item/resourceitemsettings.user".UserFile();

    private readonly string _chainSawDoorScenePath =
        "environment/scene/chapter3/c03_rightareab1ffreezer.scn".SceneFile();

    public void Apply() {
        var problematicWeapons = WeaponDefinitionRepository.Default.PlayerWeapons
            .Where(wp => ItemDefinitionRepository.Default.FromWeaponId(wp.WeaponId)!.IsStoryProgressionItem)
            .Select(wp => wp.WeaponId.ToString());

        context.ModifyUserFile<app.ItemSettings>(_weaponPrefabPath, root => {
            foreach (var setting in root._Settings) {
                if (!problematicWeapons.Contains(setting.ItemDataID))
                    continue;

                setting.MaxStackNum = 2;
            }

            return root;
        });

        if (context.GetConfigOption("keep-basement-chainsaw", true)) {
            KeepBasementChainsawAfterDoorCut();
        }
    }

    private void KeepBasementChainsawAfterDoorCut() {
        var patchedReductions = 0;
        context.ModifyScnFile(_chainSawDoorScenePath, scene => {
            return scene.Visit(node => {
                if (node is not RszObjectNode objectNode ||
                    objectNode.Type.Name != "app.fsm.ItemReduce" ||
                    !string.Equals(objectNode.Get<string>("ItemID"), ChainSawItemId, StringComparison.Ordinal) ||
                    objectNode.Get<int>("Num") <= 0) {
                    return node;
                }

                patchedReductions++;
                return objectNode.SetField("Num", 0);
            });
        });

        if (patchedReductions != 3) {
            throw new RandomizerUserException(
                $"Expected to patch three basement chainsaw reductions, patched {patchedReductions}.");
        }
    }
}
