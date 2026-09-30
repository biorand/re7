using Biohazard.BioRand.RE7.Items;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Modifiers;

internal static class KeyItemCarrierSafety {
    // A scene name is not proof of access: it can contain several locked rooms,
    // photo rewards, difficulty/rating variants, or damage-gated containers.
    // New CSV pickups must be reviewed before they enter the progression pool.
    private sealed record Carrier(string Scene, bool Extra, Vector3 Position = default);

    private static readonly Dictionary<Guid, Carrier> Carriers = new() {
        [new("401dbfaa-3469-0702-1c9a-d74a7d185216")] = new("leveldesign/itemset/chapter3/mainhouse_west/mainhouse_west", false), // Book
        [new("665a86ed-7e9c-4b56-a889-4377fa1d3f47")] = new("leveldesign/itemset/chapter3/mainhouse_west/mainhouse_west", false), // Hatch key
        [new("0da28012-ad6a-0da5-1f0a-cacd2c677ed3")] = new("environment/scene/chapter3/c03_mainhouse1fliving", false), // Clock reward
        [new("7a0710fd-6939-02b3-1a5b-229ce8cf7e77")] = new("environment/scene/chapter3/c03_mainhousehall", false), // Pendulum
        [new("71417782-8c02-4be1-88d2-735fc79e7940")] = new("environment/scene/chapter3/c03_mainhouse2fbath", false), // Bathtub
        [new("62f86528-44f6-0adf-0a70-bdf505111bb7")] = new("environment/scene/chapter3/c03_rightareab1fmorgue", false), // Incinerator tray
        [new("41a59cb8-7613-4d4b-a530-58aebfe0e1c8")] = new("leveldesign/itemset/chapter3/oldhouse/oldhouse", false), // Stone statuette
        [new("a3f59645-063b-41a5-a86a-6e5b8c507a88")] = new("environment/scene/chapter3/c03_oldhouse1funderfloor01", false), // Crank
        [new("15114d15-56af-468e-ab53-154e305e0ad1")] = new("leveldesign/itemset/chapter4/ship4f/ship4f", false), // Captain's cabin wrench
        [new("f8cb1cae-ef77-2370-44b2-4d6da9affc26")] = new("environment/scene/chapter4/c04_ship4fcabina", true, new(61.494f, 35.892f, 23.791f)),
        [new("a5b09aa7-83dc-2764-4347-1ed98d4297d2")] = new("environment/scene/chapter3/c03_oldhouse2fstudy01", true, new(-15.902f, 1.857f, 78.863f)),
        [new("f29dd369-30b8-52c6-4d65-3f8ebf01190e")] = new("environment/scene/chapter3/c03_gh1flowerroom01", true, new(38.607f, -2.361f, 83.115f)),
        [new("4415c5c8-4096-9536-4f5e-ddc2cdf657be")] = new("environment/scene/chapter3/c03_oldhouse1funderfloor01", true, new(2.186f, -4.376f, 87.344f)),
        [new("a0151085-587f-ac99-474c-30c12a8ce080")] = new("environment/scene/chapter3/c03_oldhouse1fentrance01", true, new(-19.923f, -1.745f, 74.867f)),
        [new("c3af1930-0d59-a26c-458b-04d7a8ee0f0f")] = new("environment/scene/chapter3/c03_mainhousoutsideterrace1f", true, new(21.86f, 0, 14.738f)),
        [new("5ed27b77-dc51-d228-4875-66abf42ecfb5")] = new("environment/scene/chapter3/c03_rightareab1ffreezer", true, new(-0.458f, -7.806f, 14.908f)),
        [new("49d012b8-66cf-7c47-440e-6d41c40dd75c")] = new("environment/scene/chapter3/c03_mainhousehall", true, new(-8.54f, 0.4f, 1.13f)),
        [new("d87bf384-39f3-d2ee-41e9-2f2124140a37")] = new("environment/scene/chapter3/c03_mainhouse1fpantry", true, new(10.2f, -1.2f, 11.05f)),
    };

    public static bool IsReviewed(ItemPlacement placement) {
        if (KeyItemSupplyLocations.IsReviewed(placement))
            return true;
        var guid = placement.IsExtra ? ExtraPlacementModifier.GetGeneratedItemGuid(placement) : placement.Guid;
        return Carriers.TryGetValue(guid, out var carrier) &&
               carrier.Extra == placement.IsExtra &&
               placement.SceneFile.Equals($"natives/stm/{carrier.Scene}.scn.20", StringComparison.OrdinalIgnoreCase) &&
               (!carrier.Extra || Vector3.DistanceSquared(placement.Position, carrier.Position) < 0.000001f);
    }
}
