using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Weapons;

namespace Biohazard.BioRand.RE7.Tests.Repositories;

public class WeaponDefinitionRepositoryTests {
    private readonly WeaponDefinitionRepository repository = WeaponDefinitionRepository.Default;

    [Fact]
    public void Repository_Should_Not_Be_Empty() {
        Assert.NotEmpty(repository.WeaponDefinitions);
    }

    [Fact]
    public void All_Weapons_Must_Have_Damage() {
        var invalid = repository
            .WeaponDefinitions
            .Where(w => w.Damage == null || w.Damage.Count == 0)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Weapons without damage: {string.Join(", ", invalid.Select(w => w.WeaponId))}");
    }

    [Fact]
    public void All_Weapons_Must_Have_Sane_Damage_Stats() {
        var invalid = repository
            .WeaponDefinitions
            .Where(w => w.Damage.Any(entry => {
                var stats = entry.Value;
                if (stats.Damage < 0 || stats.Stun < 0) return true;
                // These source shell impact requests detect contact without applying damage.
                var contactOnly = (w.WeaponId is Enums.app.WeaponID.Thermatebomb or Enums.app.WeaponID.Stangrenadebomb
                    && entry.Key == "Attack.rcol/Impact")
                    || (w.WeaponId is Enums.app.WeaponID.CH9_WP003 or Enums.app.WeaponID.CH9_WP004
                    && entry.Key == "Attack.rcol/AttackToWeapon");
                return stats.Damage == 0 && stats.Stun == 0 && !contactOnly;
            }))
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Weapons with invalid damage: {string.Join(", ", invalid.Select(w => w.WeaponId))}");
    }

    [Fact]
    public void WeaponIds_Must_Be_Unique() {
        var duplicates = repository
            .WeaponDefinitions
            .GroupBy(w => w.WeaponId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate WeaponIDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Named_Weapons_Should_Not_Have_Empty_Names() {
        var invalid = repository
            .WeaponDefinitions
            .Where(w => w.Name != null && string.IsNullOrWhiteSpace(w.Name))
            .Select(w => w.WeaponId)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Weapons with empty Name string: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void Guns_Must_Be_Flagged_Correctly() {
        var invalid = repository
            .Guns
            .Where(w => w.BulletItemIDs == null || w.BulletItemIDs.Count == 0)
            .Select(w => w.WeaponId)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Guns without BulletItemIDs: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void Melee_Must_Not_Have_Bullets() {
        var invalid = repository
            .MeleeWeapons
            .Where(w => w.BulletItemIDs != null && w.BulletItemIDs.Count > 0)
            .Select(w => w.WeaponId)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Non-guns with BulletItemIDs: {string.Join(", ", invalid)}");
    }


    [Fact]
    public void Player_Weapons_Should_Be_Resolvable_In_ItemRepository() {
        var invalid = repository
            .WeaponDefinitions
            .Where(w => w.UserType == Enums.app.CharacterDefine.Type.Player)
            .Where(w => ItemDefinitionRepository.Default.FromWeaponId(w.WeaponId) == null)
            .Select(w => w.WeaponId)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Player weapons missing ItemDefinition: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void FromWeaponId_String_And_Enum_Should_Be_Consistent() {
        var mismatches = repository
            .WeaponDefinitions
            .Where(w => {
                var fromEnum = repository.FromWeaponId(w.WeaponId);
                var fromString = repository.FromWeaponId(w.WeaponId.ToString());
                return !ReferenceEquals(fromEnum, fromString);
            })
            .Select(w => w.WeaponId)
            .ToList();

        Assert.True(mismatches.Count == 0,
            $"Mismatch between string and enum lookup: {string.Join(", ", mismatches)}");
    }
}
