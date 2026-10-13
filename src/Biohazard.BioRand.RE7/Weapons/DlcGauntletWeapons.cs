namespace Biohazard.BioRand.RE7.Weapons;

internal static class DlcGauntletWeapons {
    public static (string Name, int Damage, int Stun)[] GetAttacks(int weaponId) => weaponId switch {
        67 => [
            ("AttackHookR_Double", 300, 300), ("AttackHook_Double", 300, 300),
            ("AttackBodyblowR_Double", 300, 300), ("AttackUppercut_Double", 300, 300),
            ("Attack1ChargeBothHandsDouble", 500, 500), ("Attack2ChargeBothHandsDouble", 3000, 3000),
            ("AttackStraightV2_Double", 500, 500),
        ],
        61 => [
            ("AttackHookR", 100, 100), ("AttackHook_Reword", 150, 100),
            ("AttackUppercutR", 150, 150), ("AttackStraightV2_Reword", 450, 300),
            ("Attack1ChargeLeftReword", 150, 100), ("Attack2ChargeLeftReword", 300, 200),
            ("Attack3ChargeLeftReword", 500, 300),
        ],
        62 => [
            ("AttackHookR", 100, 100), ("AttackHook_Gauntlet", 150, 150),
            ("AttackUppercutR", 150, 150), ("AttackStraightV2_Gauntlet", 450, 450),
            ("Attack1ChargeLeft", 300, 300), ("Attack2ChargeLeft", 700, 700),
            ("Attack3ChargeLeft", 2000, 2000),
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(weaponId)),
    };

    public static string GetModel(int weaponId) => weaponId switch {
        61 or 62 => "wp1600",
        67 => "wp1620",
        _ => throw new ArgumentOutOfRangeException(nameof(weaponId)),
    };
}
