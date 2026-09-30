using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;

namespace Biohazard.BioRand.RE7.Tests;

public partial class RandomizerKeyItemLocationBehaviorTests {
    // Independently transcribed from the physical rooms/puzzles, not from the
    // production route graph. A scene-level classification alone is insufficient.
    private static readonly Dictionary<Guid, string[]> PhysicalPickupRequirements = new() {
        [new("ea6086fa-a847-4dd0-86d0-effd55fade49")] = ["SilhouettePazzlePiece"], // Washroom bathtub
        [new("48cfd500-3c51-44db-aa06-6fdcbfbe40b3")] = ["SilhouettePazzlePiece"], // Washroom bathtub
        [new("3f8bea82-d7c0-4868-babc-f3de7266e2e2")] = ["SilhouettePazzlePiece"], // Washroom shelf
        [new("f3ce63cd-3db3-4dfa-a9a9-1c4beecf6602")] = ["SilhouettePazzlePiece"], // Boiler Room trolley, before Dissection door
        [new("54c7a657-2d7e-448d-b767-c04fdf4ec3bc")] = [], // Pantry, on box above hatch
        [new("323c9f2e-4660-4650-8f21-4fdecdecace0")] = [], // Pantry, open shelves above hatch
        [new("ee1ddf5f-6a3e-4a53-b27d-ca2ac20dbbaa")] = [], // Kitchen bin, opposite fridge
        [new("665a86ed-7e9c-4b56-a889-4377fa1d3f47")] = [], // West hallway, hatch key table
        [new("d87bf384-39f3-d2ee-41e9-2f2124140a37")] = ["FloorDoorKey"], // Below pantry hatch
        [new("401dbfaa-3469-0702-1c9a-d74a7d185216")] = ["EntranceHallKey"], // Recreation-room book
        [new("7a0710fd-6939-02b3-1a5b-229ce8cf7e77")] = ["EntranceHallKey"], // Main Hall clock
        [new("71417782-8c02-4be1-88d2-735fc79e7940")] = ["EntranceHallKey"], // Bathroom tub
        [new("49d012b8-66cf-7c47-440e-6d41c40dd75c")] = ["EntranceHallKey"], // Main Hall table
        [new("0da28012-ad6a-0da5-1f0a-cacd2c677ed3")] = ["EntranceHallKey", "PendulumClock"], // Living-room clock
        [new("62f86528-44f6-0adf-0a70-bdf505111bb7")] = ["SilhouettePazzlePiece"], // Incinerator puzzle
        [new("5ed27b77-dc51-d228-4875-66abf42ecfb5")] = ["WorkroomKey"], // Jack 2 arena floor
        [new("c3af1930-0d59-a26c-458b-04d7a8ee0f0f")] = ["3CrestKeyA", "3CrestKeyB", "3CrestKeyC"], // Outside terrace
        [new("a0151085-587f-ac99-474c-30c12a8ce080")] = ["3CrestKeyA", "3CrestKeyB", "3CrestKeyC"], // Old House entrance
        [new("41a59cb8-7613-4d4b-a530-58aebfe0e1c8")] = ["3CrestKeyA", "3CrestKeyB", "3CrestKeyC"], // Mia's room
        [new("a3f59645-063b-41a5-a86a-6e5b8c507a88")] = ["SilhouettePazzlePieceOldHouse"], // Crank crawlspace
        [new("4415c5c8-4096-9536-4f5e-ddc2cdf657be")] = ["SilhouettePazzlePieceOldHouse"], // Same crawlspace
        [new("a5b09aa7-83dc-2764-4347-1ed98d4297d2")] = ["TalismanKey", "Lantern"], // Study reward area
        [new("f29dd369-30b8-52c6-4d65-3f8ebf01190e")] = ["TalismanKey"], // Greenhouse
        [new("15114d15-56af-468e-ab53-154e305e0ad1")] = ["SerumMaterialA", "SerumMaterialB"], // Captain's cabin, before hatch
        [new("f8cb1cae-ef77-2370-44b2-4d6da9affc26")] = ["SerumMaterialA", "SerumMaterialB"], // Same unlocked cabin
    };

    private static readonly Dictionary<string, string[]> VanillaPickupRequirements = new() {
        ["FloorDoorKey"] = [],
        ["EthanCarKey"] = ["FloorDoorKey"],
        ["EntranceHallKey"] = ["EthanCarKey"],
        ["PendulumClock"] = ["EntranceHallKey"],
        ["3CrestKeyA"] = ["EntranceHallKey"],
        ["3CrestKeyB"] = ["PendulumClock"],
        ["SilhouettePazzlePiece"] = ["EntranceHallKey"],
        ["MorgueKey"] = ["SilhouettePazzlePiece"],
        ["WorkroomKey"] = ["SilhouettePazzlePiece"],
        ["3CrestKeyC"] = ["WorkroomKey"],
        ["SilhouettePazzlePieceOldHouse"] = ["3CrestKeyA", "3CrestKeyB", "3CrestKeyC"],
        ["Crank"] = ["SilhouettePazzlePieceOldHouse"],
        ["TalismanKey"] = ["Crank"],
        ["Lantern"] = ["TalismanKey"],
        ["SerumMaterialA"] = ["Lantern"],
        ["MasterKey"] = ["SerumMaterialA"],
        ["LucasCardKey"] = ["MasterKey"],
        ["LucasCardKey2"] = ["MasterKey"],
        ["Battery"] = ["LucasCardKey", "LucasCardKey2"],
        ["SerumMaterialB"] = ["Battery"],
        ["EvOpener"] = ["SerumMaterialA", "SerumMaterialB"],
        ["SpareKey"] = ["EvOpener"],
        ["FuseCh4"] = ["EvOpener"],
        ["EvCable"] = ["EvOpener", "SpareKey"],
        ["SerumTypeE"] = ["EvCable", "FuseCh4"],
    };

    [Fact]
    public void KeyItemLocations_EveryEligibleCarrierHasIndependentPhysicalRequirements() {
        var eligible = KeyItemLocationModifier.GetRouteTargetsForTesting(_defaultRun.Result.Randomizer);
        Assert.Equal(PhysicalPickupRequirements.Keys.Order(), eligible.Select(target => target.TargetGuid).Order());
    }

    [Fact]
    public void KeyItemLocations_NewOrMovedCsvPickupsNeedReviewBeforeCarryingKeys() {
        var placement = new ItemPlacement {
            Id = "", IsExtra = true, Enabled = true, Chapter = 3,
            Guid = new("49d012b8-66cf-7c47-440e-6d41c40dd75c"),
            SceneFile = MainHouseHallScenePath, PosX = -8.54f, PosY = 0.4f, PosZ = 1.13f,
        };
        Assert.True(KeyItemCarrierSafety.IsReviewed(placement));
        placement.PosY = -10; // Same room/GUID, but underground.
        Assert.False(KeyItemCarrierSafety.IsReviewed(placement));
        placement.PosY = 0.4f;
        placement.SceneFile = ShipSickBayScenePath;
        Assert.False(KeyItemCarrierSafety.IsReviewed(placement));
        placement.SceneFile = MainHouseHallScenePath;
        placement.Guid = new("d37a3d66-9279-4d14-a055-0b275c28fa94");
        Assert.False(KeyItemCarrierSafety.IsReviewed(placement));
    }

    [Fact]
    public void KeyItemLocations_MainHallCubbyCannotHideProgressionInsideGeometry() {
        var eligible = KeyItemLocationModifier.GetRouteTargetsForTesting(_defaultRun.Result.Randomizer);
        Assert.DoesNotContain(eligible, target =>
            target.TargetGuid == new Guid("1ca1024c-bc24-0e14-4d97-693fd5d03651"));
    }

    [Theory]
    [InlineData(895914, false, "Normal")]
    [InlineData(895914, true, "Main House")]
    [InlineData(895914, true, "Wrecked Ship")]
    [InlineData(776198, false, "Main House")]
    [InlineData(703757, true, "Normal")]
    [InlineData(35825, true, "Main House")]
    [InlineData(16, false, "Normal")]
    [InlineData(100583, true, "Normal")]
    [InlineData(305655, false, "Normal")]
    [InlineData(736361, true, "Normal")]
    [InlineData(866537, true, "Normal")]
    [InlineData(999477, false, "Normal")]
    public void KeyItemLocations_SerializedSeedHasNoPhysicalAccessCycles(int seed, bool extras, string start) {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["additional-items"] = extras;
            config["additional-wooden-crates"] = extras;
            config["replace-weapons"] = true;
            config["start-chapter"] = start;
        }, seed);
        AssertPhysicalProgression(result);
    }

    internal static void AssertPhysicalProgression(RandomizerRunResult result) {
        var relocated = GetChangedPlacements(result)
            .Where(change => VanillaPickupRequirements.ContainsKey(change.AfterId))
            .ToDictionary(change => change.AfterId);
        var pending = new Dictionary<string, string[]>(VanillaPickupRequirements);
        foreach (var (id, change) in relocated) {
            var guid = GetTargetGuid(change.Placement);
            Assert.True(PhysicalPickupRequirements.ContainsKey(guid), $"Unreviewed carrier for {id}: {guid}");
            pending[id] = PhysicalPickupRequirements[guid];
            var item = GetItem(result.ReadAfterScene(change.Placement.SceneFile), guid);
            Assert.True(item.Enabled);
            Assert.True(item.ItemStackNum > 0);
            Assert.True(item._DifficultItemNumSetting.EasyNum > 0);
            Assert.True(item._DifficultItemNumSetting.HardNum > 0);
        }
        // A fallback is safe only if its original rewards really survive serialization.
        foreach (var placement in result.ItemPlacementService.MainGamePlacements.Where(placement =>
                     !placement.IsExtra && placement.Enabled && !string.IsNullOrEmpty(placement.Id) &&
                     pending.ContainsKey(placement.Id) &&
                     !relocated.ContainsKey(placement.Id))) {
            var before = GetItemOrNull(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
            if (before == null) continue;
            var after = GetItemOrNull(result.ReadAfterScene(placement.SceneFile), placement.Guid);
            Assert.NotNull(after);
            Assert.Equal(before.ItemDataID, after.ItemDataID);
            Assert.Equal(before.SaveGUID, after.SaveGUID);
        }
        var acquired = new HashSet<string>();
        while (pending.Count > 0) {
            var accessible = pending.Where(pair => pair.Value.All(acquired.Contains)).Select(pair => pair.Key).ToArray();
            Assert.True(accessible.Length > 0,
                "Unreachable progression items: " + string.Join("; ", pending.Select(pair =>
                    $"{pair.Key} needs {string.Join(", ", pair.Value.Where(key => !acquired.Contains(key)))}")));
            foreach (var key in accessible) {
                acquired.Add(key);
                pending.Remove(key);
            }
        }
    }
}
