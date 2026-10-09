using Biohazard.BioRand.RE7.Items;
using Enums.app.Item;

namespace Biohazard.BioRand.RE7.Tests.Repositories;

public class ItemDefinitionRepositoryTests {
    private readonly ItemDefinitionRepository repository = ItemDefinitionRepository.Default;

    [Fact]
    public void Repository_Should_Not_Be_Empty() {
        Assert.NotEmpty(repository.Items);
    }

    [Fact]
    public void All_Items_Must_Have_Id() {
        var invalid = repository
            .Items
            .Where(i => string.IsNullOrWhiteSpace(i.Id))
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Items without valid ID: {string.Join(", ", invalid.Select(i => i.Name ?? "<null>"))}");
    }

    [Fact]
    public void Ids_Must_Be_Unique() {
        var duplicates = repository
            .Items
            .GroupBy(i => i.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate item IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void WeaponIds_Must_Be_UniqueExceptExplicitM21Alias() {
        var duplicates = repository
            .Items
            .Where(i => i.WeaponId != null)
            .GroupBy(i => i.WeaponId)
            .Where(g => g.Count() > 1)
            .ToList();

        var alias = Assert.Single(duplicates);
        Assert.Equal(Enums.app.WeaponID.Shotgun_DB, alias.Key);
        Assert.Equal(new[] { "NumaItem072", "Shotgun_DB" }, alias.Select(i => i.Id).Order().ToArray());
        Assert.Equal("Shotgun_DB", repository.FromWeaponId(Enums.app.WeaponID.Shotgun_DB)!.Id);
    }

    [Fact]
    public void Unlockables_Must_Not_Be_Stackable() {
        var invalid = repository
            .Items
            .Where(i => i.IsUnlockable && i.MaxStack > 1)
            .Select(i => i.Id)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Unlockables with MaxStack > 1: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void Weapons_Must_Have_MaxStack_Of_One() {
        var invalid = repository
            .Items
            .Where(i => i.CategoryType == ItemCategoryType.Weapon && i.MaxStack != 1)
            .Select(i => i.Id)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Weapons with invalid MaxStack: {string.Join(", ", invalid)}");
    }

    [Fact]
    public void Named_Items_Should_Not_Have_Empty_Names() {
        var invalid = repository
            .Items
            .Where(i => i.Name != null && string.IsNullOrWhiteSpace(i.Name))
            .Select(i => i.Id)
            .ToList();

        Assert.True(invalid.Count == 0,
            $"Items with empty Name string: {string.Join(", ", invalid)}");
    }
}
