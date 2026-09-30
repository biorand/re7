using Enums.app;
using Enums.app.Item;
using System.Text.Json.Serialization;

namespace Biohazard.BioRand.RE7.Items;

/// <summary>
/// Represents the definition of an RE7 item.
/// Not to be confused with a concrete <see cref="Item"/>!
/// </summary>
public sealed class ItemDefinition {
    /// <summary>
    /// Unique identifier.
    /// Often but not always an <see cref="ItemID"/>.
    /// <para></para>
    /// Example: FoundFootage000
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Tags further specifying the item.
    /// <para></para>
    /// Example: story
    /// </summary>
    public string Tags { get; set; } = "";

    /// <summary>
    /// More readable name than the <see cref="Id"/> used in RE7's UI.
    /// <para></para>
    /// Example: Derelict House Footage
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Used to differentiate between different categories like ammo, health, weapon etc.
    /// </summary>
    public ItemCategoryType CategoryType { get; set; }

    /// <summary>
    /// Space used in the inventory.
    /// <para></para>
    /// Example: Slot1
    /// </summary>
    public ItemSlotSize Size { get; set; } = ItemSlotSize.Slot1;

    /// <summary>
    /// Whether the item is an unlockable extra.
    /// See <a href="https://steamcommunity.com/sharedfiles/filedetails?id=1761418830">here</a>
    /// </summary>
    public bool IsUnlockable { get; set; }

    /// <summary>
    /// Specifies the DLC the item exists in.
    /// <para></para>
    /// Example: NotAHero
    /// </summary>
    public DlcType? Dlc { get; set; }

    /// <summary>
    /// Maximum stack number of the item. Always 1 for weapons.
    /// </summary>
    public int MaxStack { get; set; }

    /// <summary>
    /// Identifier if the item is a weapon.
    /// Often the same as the <see cref="ItemID"/> if the <see cref="CategoryType"/> is <see cref="ItemCategoryType.Weapon"/>.
    /// </summary>
    public WeaponID? WeaponId { get; set; }

    /// <summary>
    /// Whether the item can be stored in the item box.
    /// </summary>
    public bool CanStoreInItemBox { get; set; }

    /// <summary>
    /// Japanese developer comment extracted from the game files.
    /// Sometimes gives helpful information if the item is special.
    /// Must be (de)serialized as UTF-8!
    /// </summary>
    public string? DeveloperComment { get; set; }

    /// <summary>
    /// Which user file the definition's data is coming from.
    /// Stored to be able to later re-write stuff.
    /// </summary>
    public string? SourceUserFile { get; set; }

    [JsonIgnore]
    public bool IsWeapon => Id is "ToyShotgun" or "DummyAxe" ||
                            CategoryType is ItemCategoryType.Weapon or ItemCategoryType.StackWeapon;

    [JsonIgnore] public bool IsStackable => MaxStack > 1;

    [JsonIgnore]
    public bool IsStackLimitConfigurable =>
        !IsDlcItem &&
        !IsStoryProgressionItem &&
        !IsStackLimitExcludedWeapon &&
        !string.IsNullOrWhiteSpace(SourceUserFile) &&
        !string.IsNullOrWhiteSpace(Name) &&
        (IsStackable || IsDrugOrMaterial);

    private bool IsStackLimitExcludedWeapon =>
        Id is "ToyShotgun" or "DummyAxe" || CategoryType is ItemCategoryType.Weapon;

    private bool IsDrugOrMaterial => CategoryType is ItemCategoryType.Drug or ItemCategoryType.Material;

    [JsonIgnore] public string StackLimitConfigId => $"inventory-stack-limit-{CreateStackLimitConfigIdSuffix(Id)}";

    [JsonIgnore] public bool IsDlcItem => Dlc != null;

    public ItemID? ItemId => EnumExtensions.ParseOrNull<ItemID>(Id);

    [JsonIgnore] public bool IsStoryProgressionItem => Tags.Contains(StoryProgressionTag);

    public override string ToString() => Name ?? Id;

    public string ToDetailedString() {
        var sb = new StringBuilder();

        sb.AppendLine("=== Item Definition ===");
        sb.AppendLine($"Id:                 {Id}");
        sb.AppendLine($"Name:               {Name ?? "<null>"}");
        sb.AppendLine($"Category:           {CategoryType}");
        sb.AppendLine($"Size:               {Size}");
        sb.AppendLine($"Max Stack:          {MaxStack}");
        sb.AppendLine($"Is Unlockable:      {IsUnlockable}");
        sb.AppendLine($"Can Store In Box:   {CanStoreInItemBox}");
        sb.AppendLine($"DLC:                {Dlc?.ToString() ?? "<Base Game>"}");
        sb.AppendLine($"Weapon Id:          {WeaponId?.ToString() ?? "<none>"}");

        if (!string.IsNullOrWhiteSpace(DeveloperComment)) {
            sb.AppendLine("Developer Comment:");
            sb.AppendLine($"    {DeveloperComment}");
        }

        sb.AppendLine("=======================");
        return sb.ToString();
    }

    // Tags
    public const string StoryProgressionTag = "story";

    private static string CreateStackLimitConfigIdSuffix(string id) {
        var sb = new StringBuilder(id.Length);
        var previousWasSeparator = false;

        foreach (var c in id) {
            var lower = char.ToLowerInvariant(c);
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9') {
                sb.Append(lower);
                previousWasSeparator = false;
            } else if (!previousWasSeparator && sb.Length > 0) {
                sb.Append('-');
                previousWasSeparator = true;
            }
        }

        if (sb.Length > 0 && sb[^1] == '-') {
            sb.Length--;
        }

        return sb.ToString();
    }
}
