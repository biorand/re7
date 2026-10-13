using Biohazard.BioRand.RE7.REEngine;

namespace Biohazard.BioRand.RE7.Patches;

internal class InventoryManagementPatch(IPatchContext context) : IPatch {
    private static readonly HashSet<string> TransferableItemDataIds = new(StringComparer.Ordinal){
        "BlueBlaster",
        "RedBlaster",
    };

    private readonly string _birthdayResourceItemSettingsPath =
        "prefab/item/resourceitemsettings_birthday.user".UserFile();

    public void Apply() {
        if (!DlcContent.IsAllowed(context) || !context.GetConfigOption("inventory-unrestricted-management", true)) {
            return;
        }

        if (!context.Exists(_birthdayResourceItemSettingsPath)) {
            return;
        }

        var settings = context.DeserializeUserFile<app.ItemSettings>(_birthdayResourceItemSettingsPath);
        var changed = false;
        foreach (var setting in settings._Settings) {
            if (TransferableItemDataIds.Contains(setting.ItemDataID) && !setting.CanStoreItembox) {
                setting.CanStoreItembox = true;
                changed = true;
            }
        }

        if (changed) {
            context.SerializeUserFile(_birthdayResourceItemSettingsPath, settings);
        }
    }
}
