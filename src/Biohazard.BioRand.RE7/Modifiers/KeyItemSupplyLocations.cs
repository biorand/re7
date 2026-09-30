using Biohazard.BioRand.RE7.Items;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Modifiers;

// These loose pickup positions have been reviewed individually. Their original
// supplies live in difficulty-specific itemsets, so a selected key is instead
// materialized in the common room scene. Do not enable the difficulty itemsets.
internal static class KeyItemSupplyLocations {
    internal const string PlacementTag = "key_item_only";

    internal sealed record Supply(string Scene, Guid Guid);
    internal sealed record Location(Guid Guid, int Chapter, string RoomScene, Vector3 Position,
        string Description, ImmutableArray<Supply> Supplies);

    internal static readonly ImmutableArray<Location> All = [
        new(new("54c7a657-2d7e-448d-b767-c04fdf4ec3bc"), 3, "c03_mainhouse1fpantry",
            new(9.54421f, 0.4469466f, 10.36634f), "Pantry - plastic box above hatch",
            [new("chapter3/mainhouse_west/hard", new("54c7a657-2d7e-448d-b767-c04fdf4ec3bc"))]),
        new(new("323c9f2e-4660-4650-8f21-4fdecdecace0"), 3, "c03_mainhouse1fpantry",
            new(9.514553f, 1.323398f, 9.589417f), "Pantry - shelves above hatch",
            [new("chapter3/mainhouse_west/hard", new("323c9f2e-4660-4650-8f21-4fdecdecace0"))]),
        new(new("ee1ddf5f-6a3e-4a53-b27d-ca2ac20dbbaa"), 3, "c03_mainhouse1fldk",
            new(21.11776f, 0.5390167f, 6.891929f), "Kitchen - bin opposite fridge",
            [new("chapter3/mainhouse_west/normal", new("ee1ddf5f-6a3e-4a53-b27d-ca2ac20dbbaa"))]),
    ];

    internal static string ScenePath(Location location)
        => $"natives/stm/environment/scene/chapter{location.Chapter}/{location.RoomScene}.scn.20";

    internal static string SupplyScenePath(Supply supply)
        => $"natives/stm/leveldesign/itemset/{supply.Scene}.scn.20";

    internal static IEnumerable<ItemPlacement> CreatePlacements()
        => All.Select(location => new ItemPlacement {
            Guid = location.Guid, Id = "", IsExtra = true, Enabled = true,
            Tags = [PlacementTag], Chapter = location.Chapter,
            SceneFile = ScenePath(location), Comment = location.Description,
            PosX = location.Position.X, PosY = location.Position.Y, PosZ = location.Position.Z,
            RotW = 1, StackNum = 1, EasyNum = 1, HardNum = 1,
        });

    internal static bool IsReviewed(ItemPlacement placement)
        => placement.IsExtra && placement.Tags.Contains(PlacementTag) && All.Any(location =>
            location.Guid == placement.Guid &&
            ScenePath(location).Equals(placement.SceneFile, StringComparison.OrdinalIgnoreCase) &&
            Vector3.DistanceSquared(location.Position, placement.Position) < 0.000001f);

    internal static void RemoveOverlappingSupplies(Randomizer randomizer, IEnumerable<Guid> selectedGuids) {
        var selected = selectedGuids.ToHashSet();
        foreach (var group in All.Where(location => selected.Contains(location.Guid))
                     .SelectMany(location => location.Supplies).Distinct().GroupBy(SupplyScenePath)) {
            randomizer.FileRepository.ModifyScnFile(group.Key, scene => {
                foreach (var supply in group)
                    scene = scene.RemoveGameObject(supply.Guid);
                return scene;
            });
        }
    }
}
