using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Modifiers;
using IntelOrca.Biohazard.REE.Rsz;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Tests;

public partial class RandomizerKeyItemLocationBehaviorTests {
    [Fact]
    public void KeyItemLocations_SupplyAnchorsUsePlainNativePickupsAndCommonRoomCoordinates() {
        var result = _defaultRun.Result;
        foreach (var location in KeyItemSupplyLocations.All) {
            var room = result.ReadBeforeScene(KeyItemSupplyLocations.ScenePath(location));
            var parent = room.FindGameObject(go => go.Name.EndsWith("_dynamic", StringComparison.Ordinal));
            Assert.NotNull(parent);
            var parentTransform = parent.FindComponent<GeneratedViaTransform>()!;
            Assert.Equal(Vector3.Zero, (Vector3)parentTransform.Position);
            Assert.Equal(Quaternion.Identity, (Quaternion)parentTransform.Rotation);
            foreach (var supply in location.Supplies) {
                var source = result.ReadBeforeScene(KeyItemSupplyLocations.SupplyScenePath(supply));
                var match = source.FindGameObjectsByGuidWithFsmContext([supply.Guid])[supply.Guid];
                Assert.False(match.HasDrawerContext);
                Assert.False(match.HasConditionalPickupContext);
                Assert.False(match.HasFsmInHierarchy);
                var transform = match.GameObject.FindComponent<GeneratedViaTransform>()!;
                Assert.True(Vector3.Distance(location.Position, transform.Position) < 0.75f,
                    $"Supply {supply.Guid} no longer matches the reviewed location {location.Description}.");
            }
            Assert.Contains(location.Supplies, supply => supply.Guid == location.Guid);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KeyItemLocations_SelectedSupplySitesExistOnAllDifficultiesWithoutOverlappingLoot(bool extras) {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["additional-items"] = extras;
        }, 895914);
        var selected = GetChangedPlacements(result).Where(change =>
                change.Placement.Tags.Contains(KeyItemSupplyLocations.PlacementTag))
            .ToDictionary(change => change.Placement.Guid);
        Assert.NotEmpty(selected);
        foreach (var location in KeyItemSupplyLocations.All) {
            var after = result.ReadAfterScene(KeyItemSupplyLocations.ScenePath(location));
            if (!selected.ContainsKey(location.Guid)) {
                Assert.Null(after.FindGameObject(location.Guid));
                foreach (var supply in location.Supplies)
                    Assert.NotNull(result.ReadAfterScene(KeyItemSupplyLocations.SupplyScenePath(supply))
                        .FindGameObject(supply.Guid));
                continue;
            }
            var item = GetItem(after, location.Guid);
            Assert.True(item.Enabled);
            Assert.True(item.ItemStackNum > 0);
            Assert.True(item._DifficultItemNumSetting.EasyNum > 0);
            Assert.True(item._DifficultItemNumSetting.HardNum > 0);
            foreach (var supply in location.Supplies)
                Assert.Null(result.ReadAfterScene(KeyItemSupplyLocations.SupplyScenePath(supply))
                    .FindGameObject(supply.Guid));
        }
        AssertPhysicalProgression(result);
    }

    [Fact]
    public void KeyItemLocations_DisabledDoesNotCreateKeyOnlySitesOrRemoveTheirSupplies() {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = false;
            config["additional-items"] = true;
        });
        foreach (var location in KeyItemSupplyLocations.All) {
            Assert.Null(result.ReadAfterScene(KeyItemSupplyLocations.ScenePath(location)).FindGameObject(location.Guid));
            foreach (var supply in location.Supplies)
                Assert.NotNull(result.ReadAfterScene(KeyItemSupplyLocations.SupplyScenePath(supply))
                    .FindGameObject(supply.Guid));
        }
    }

    [Fact]
    public void KeyItemLocations_HatchHasSeveralCandidatesAboveItsOwnHatch() {
        var placements = KeyItemSupplyLocations.CreatePlacements().Where(placement =>
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(placement, "FloorDoorKey")).ToArray();
        Assert.True(placements.Length >= 3);
        Assert.All(placements, placement => Assert.True(placement.PosY >= 0));
    }
}
