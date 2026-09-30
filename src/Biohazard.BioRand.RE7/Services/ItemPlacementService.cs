using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Modifiers;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Services;

internal class ItemPlacementService {
    public ImmutableList<ItemPlacement> ItemPlacements { get; private set; }
    public ImmutableDictionary<ItemPlacement, ItemDefinition> PlacementToItemMap { get; private set; } = [];
    public ImmutableDictionary<string, List<ItemPlacement>> IdToItemsMap { get; private set; } = [];
    public ImmutableDictionary<Guid, List<ItemPlacement>> GuidToItemsMap { get; private set; } = [];
    public ImmutableList<ItemPlacement> MainGamePlacements { get; private set; } = [];

    public ItemPlacementService(Randomizer randomizer) {
        var csv = randomizer.DynamicData.GetData(DynamicDataName.ItemPlacements) ??
                  throw new Exception("Unable to get item data");
        ItemPlacements = Csv.Deserialize<ItemPlacement>(csv)
            .Concat(KeyItemSupplyLocations.CreatePlacements()).ToImmutableList();
        PlacementToItemMap = ItemPlacements.Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .ToImmutableDictionary(x => x, x => ItemDefinitionRepository.Default.FromId(x.Id)!);
        IdToItemsMap = ItemPlacements.Where(x => !string.IsNullOrWhiteSpace(x.Id)).GroupBy(x => x.Id)
            .ToImmutableDictionary(g => g.Key, g => g.ToList());
        GuidToItemsMap = ItemPlacements.GroupBy(x => x.Guid).ToImmutableDictionary(g => g.Key, g => g.ToList());
        MainGamePlacements = ItemPlacements.Where(x => x.Dlc == null).ToImmutableList();
    }

    public List<ItemPlacement> FromId(string id) {
        if (!IdToItemsMap.TryGetValue(id, out var item)) {
            return new List<ItemPlacement>();
        }

        return item;
    }

    public List<ItemPlacement> FromSceneGuid(string sceneFile, Guid guid)
        => FromGuid(guid)
            .Where(x => string.Equals(x.SceneFile, sceneFile, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public List<ItemPlacement> FromSceneGuid(string sceneFile, string guid)
        => FromSceneGuid(sceneFile, new Guid(guid));

    public bool HasItem(Guid guid) => GuidToItemsMap.ContainsKey(guid);

    public List<ItemPlacement> FromGuid(Guid guid) => GuidToItemsMap[guid];

    public List<ItemPlacement> FromGuid(string guid) => GuidToItemsMap[new Guid(guid)];
}
