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
                StartingWeaponCategory.Bladed => "Edged/Bladed",
                StartingWeaponCategory.CircularSaw => "Circular Saw",
                StartingWeaponCategory.Handgun => "Handgun",
                StartingWeaponCategory.MachineGun => "P19 Machine Gun",
                StartingWeaponCategory.Shotgun => "Shotgun",
                StartingWeaponCategory.Bomb => "Remote Bomb",
                StartingWeaponCategory.Burner => "Burner",
                StartingWeaponCategory.Magnum => "44 MAG",
                StartingWeaponCategory.GrenadeLauncher => "Grenade Launcher",
                _ => throw new ArgumentException("Invalid category")
            };

        public List<ItemID> GetItemIds(bool includeDlcWeapons = false) {
            var items = GetBaseItemIds(category);
            if (includeDlcWeapons) items.AddRange(category switch {
                StartingWeaponCategory.Bladed => [ItemID.CKnife],
                StartingWeaponCategory.Handgun => [ItemID.Handgun_Albert_C],
                StartingWeaponCategory.Shotgun => [ItemID.Shotgun_Albert, ItemID.NumaItem072],
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
