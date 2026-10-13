using Enums.app;

namespace Biohazard.BioRand.RE7.Inventory;

public enum StartingWeaponCategory {
    Bladed,
    CircularSaw,
    Handgun,
    MachineGun,
    Shotgun,
    Bomb,
    Burner,
    Magnum,
    GrenadeLauncher
};

internal static class StartingWeaponCategoryExtensions {
    extension(StartingWeaponCategory category) {
        public string GetLabel()
            => category switch{
                StartingWeaponCategory.Bladed => "Melee",
                StartingWeaponCategory.CircularSaw => "Circular Saw",
                StartingWeaponCategory.Handgun => "Handgun",
                StartingWeaponCategory.MachineGun => "P19 Machine Gun",
                StartingWeaponCategory.Shotgun => "Shotgun",
                StartingWeaponCategory.Bomb => "Explosives",
                StartingWeaponCategory.Burner => "Burner",
                StartingWeaponCategory.Magnum => "44 MAG",
                StartingWeaponCategory.GrenadeLauncher => "Grenade Launcher",
                _ => throw new ArgumentException("Invalid category")
            };

        public List<ItemID> GetItemIds(bool includeDlcWeapons = false) {
            var items = GetBaseItemIds(category);
            if (includeDlcWeapons) items.AddRange(category switch {
                StartingWeaponCategory.Bladed => [ItemID.CKnife, ItemID.CH9_WP002, ItemID.CH9_WP000, ItemID.CH9_WP001, ItemID.CH9_WP006],
                StartingWeaponCategory.Handgun => [ItemID.Handgun_Albert_C],
                StartingWeaponCategory.Shotgun => [ItemID.Shotgun_Albert, ItemID.NumaItem072],
                StartingWeaponCategory.Bomb => [ItemID.Grenadebomb, ItemID.Thermatebomb, ItemID.Stangrenadebomb, ItemID.CH9_WP005],
                _ => Array.Empty<ItemID>(),
            });
            return items;
        }
    }

    private static List<ItemID> GetBaseItemIds(StartingWeaponCategory category)
            => category switch{
                StartingWeaponCategory.Bladed =>[ /*ItemID.HandAxe, */ ItemID.Knife, ItemID.MiaKnife],
                StartingWeaponCategory.CircularSaw =>[ItemID.CircularSaw],
                StartingWeaponCategory.Handgun =>[
                    ItemID.Handgun_G17, ItemID.Handgun_M19, ItemID.Handgun_MPM,
                    ItemID.Handgun_Albert, ItemID.Handgun_Albert_Reward
                ],
                StartingWeaponCategory.MachineGun =>[ItemID.MachineGun],
                StartingWeaponCategory.Shotgun =>[ItemID.Shotgun_DB, ItemID.Shotgun_M37],
                StartingWeaponCategory.Bomb =>[ItemID.LiquidBomb],
                StartingWeaponCategory.Burner =>[ItemID.Burner],
                StartingWeaponCategory.Magnum =>[ItemID.Magnum],
                StartingWeaponCategory.GrenadeLauncher =>[ItemID.GrenadeLauncher],
                _ => throw new ArgumentException("Invalid category")
            };
}
