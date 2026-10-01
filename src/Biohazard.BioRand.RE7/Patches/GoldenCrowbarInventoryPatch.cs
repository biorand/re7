using Biohazard.BioRand.RE7.REEngine;
using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Patches;

internal class GoldenCrowbarInventoryPatch(IPatchContext context) : IPatch {
    internal const string ItemId = "GoldenBar";
    internal const string MeshPath = "Weapon/wp1370_GoldenBar/wp1370_GoldenBar.mesh";
    internal const string MaterialPath = "Weapon/wp1370_GoldenBar/wp1370.mdf2";

    private static readonly string[] DlcDependencies = [
        "natives/stm/prefab/weapon/wp1370_goldenbar/item/wp1370_goldenbar_item.pfb.17",
        "natives/stm/collision/collider/weapon/wp1370/wp1370_goldenbar.rcol.20",
        "natives/stm/vfx/vfx_provider/vfx_epv_weapon_id/epv_wp1370/epvc_wp1370.pfb.17",
    ];

    public void Apply() {
        // The campaign does not register the Birthday weapon table. Both AddItem
        // and cage pickups need this definition before they can create the weapon.
        // Register support without granting it or importing the locked GoldenBarNo entry.
        var goldenCrowbar = context.DeserializeUserFile<app.ItemSettings>(
                "prefab/item/resourceitemsettings_birthday.user".UserFile())
            ._Settings.Single(item => item.ItemDataID == ItemId);
        context.ModifyUserFile<app.ItemSettings>("prefab/item/resourceitemsettings.user".UserFile(), settings => {
            settings._Settings.RemoveAll(item => item.ItemDataID == ItemId);
            settings._Settings.Add(goldenCrowbar);
            return settings;
        });

        // The model, textures, detail-search prefab and item-resource registration
        // are already in the base game. These three files exist only in the DLC PAK.
        foreach (var path in DlcDependencies) {
            var data = context.GetFile(path) ?? throw new RandomizerUserException(
                $"Unable to read Golden Crowbar dependency '{path}'.");
            context.SetFile(path, data);
        }

        var birthdayMessages = context.GetMsgFile("message/ui_birthday_mes.msg".MessageFile());
        context.ModifyMsgFile("message/ui_item_mes.msg".MessageFile(), messages => {
            BirthdaySkillInventoryPatch.CopyMessageIfMissing(messages, birthdayMessages, goldenCrowbar.NameMsg);
            BirthdaySkillInventoryPatch.CopyMessageIfMissing(messages, birthdayMessages, goldenCrowbar.ManualMsg);
        });
    }
}
