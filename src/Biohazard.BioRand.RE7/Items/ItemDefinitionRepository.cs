using Biohazard.BioRand.RE7.Serialization;
using Enums.app;
using Biohazard.BioRand.RE7.Weapons;
using Enums.app.Item;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Items;

public sealed class ItemDefinitionRepository {
    private static ItemDefinitionRepository? _default;
    private static readonly object _defaultLock = new();
    public List<ItemDefinition> Items { get; private set; } = [];
    public ImmutableArray<ItemCategoryType> Kinds { get; private set; }

    public ImmutableDictionary<ItemCategoryType, ImmutableArray<ItemDefinition>> KindToItemMap { get; private set; } =
        [];

    public ImmutableDictionary<string, ItemDefinition> IdToItemMap { get; private set; } = [];
    public ImmutableDictionary<string, ItemDefinition> NameToItemMap { get; private set; } = [];
    public ImmutableDictionary<WeaponID, ItemDefinition> WeaponIdToItemMap { get; private set; } = [];
    private const string ItemDefinitionFileName = "item_definitions.json";

    public static ItemDefinitionRepository Default {
        get {
            if (_default == null) {
                lock (_defaultLock) {
                    if (_default == null) {
                        var repository = new ItemDefinitionRepository{
                            Items = EmbeddedData.GetFile(ItemDefinitionFileName).DeserializeJson<List<ItemDefinition>>()
                        };
                        repository.Initialize();
                        _default = repository;
                    }
                }
            }

            return _default;
        }
    }

    private void Initialize() {
        Items.AddRange(DlcCampaignWeapons.CreateItemDefinitions());
        var relevantItems = Items
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .ToArray();

        Kinds = relevantItems
            .Select(x => x.CategoryType)
            .Distinct()
            .ToImmutableArray();

        KindToItemMap = relevantItems
            .GroupBy(x => x.CategoryType)
            .ToImmutableDictionary(x => x.Key, x => x.ToImmutableArray());

        NameToItemMap = relevantItems
            .Where(x => !x.IsDlcItem || DlcCampaignWeapons.Contains(x.Id))
            .Where(x => !string.IsNullOrEmpty(x.Name))
            .Where(x => !new string[]{
                "CircularSawNo", "Candle_Lighted", "EvelynRadar",
                "EvelynRadar2", "EvelynRadar3", "Glasses_End",
                "ProposalBookFf", "SerumComplete", "SilhouettePazzlePieceChildroom"
            }.Contains(x.Id))
            .Where(x => x.Name != "Treasure Photo")
            .Where(x => x.CategoryType != ItemCategoryType.Map)
            .ToImmutableDictionary(x => x.Name!);

        IdToItemMap = Items.ToImmutableDictionary(x => x.Id);

        WeaponIdToItemMap = Items
            .Where(x => x.WeaponId != null)
            // Joe's M21 is an inventory alias, not a second native weapon/stat identity.
            .Where(x => x.Id != "NumaItem072")
            .ToImmutableDictionary(x => x.WeaponId!.Value);
    }

    public ItemDefinition? FromId(string id) {
        IdToItemMap.TryGetValue(id, out var item);
        return item;
    }

    public ItemDefinition? FromName(string name) {
        return IdToItemMap[GetIdByName(name)];
    }

    public string NameToId(string name) {
        return NameToItemMap.TryGetValue(name, out var item) ? item.Id : throw new Exception("Invalid name!");
    }

    public string GetName(string id) {
        return FromId(id)?.Name ?? id;
    }

    public string GetIdByName(string name) {
        return Items.First(item => item.Name == name).Id;
    }

    public ItemDefinition? FromWeaponId(WeaponID id) {
        WeaponIdToItemMap.TryGetValue(id, out var item);
        return item;
    }

    public ItemDefinition[] GetAll(ItemCategoryType kind) {
        var items = KindToItemMap[kind].ToArray();
        return items;
    }
}
