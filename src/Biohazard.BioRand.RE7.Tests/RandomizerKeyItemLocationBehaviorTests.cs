using System.Numerics;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Enums.app.Item;
using IntelOrca.Biohazard.REE.Rsz;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerKeyItemLocationBehaviorTests : IClassFixture<DefaultRandomizerRunFixture> {
    private readonly DefaultRandomizerRunFixture _defaultRun;

    public RandomizerKeyItemLocationBehaviorTests(DefaultRandomizerRunFixture defaultRun) {
        _defaultRun = defaultRun;
    }

    private static readonly IReadOnlyDictionary<string, ExpectedKeyItemRule> ExpectedRules =
        new Dictionary<string, ExpectedKeyItemRule>(StringComparer.OrdinalIgnoreCase){
            ["FloorDoorKey"] = new(3, ExpectedScope.Chapter3Start),
            ["3CrestKeyB"] = new(3, ExpectedScope.BeforeDogDoor),
            ["3CrestKeyA"] = new(3, ExpectedScope.BeforeDogDoor),
            ["Battery"] = new(3, ExpectedScope.BeforeBarnBatterySocket),
            ["PendulumClock"] = new(3, ExpectedScope.AfterGarageBeforeShadowPuzzle),
            ["MorgueKey"] = new(3, ExpectedScope.BeforeShadowPuzzle),
            ["WorkroomKey"] = new(3, ExpectedScope.BeforeDissectionRoom),
            ["MasterKey"] = new(3, ExpectedScope.AfterLanternBeforeSnakeRooms),
            ["TalismanKey"] = new(3, ExpectedScope.AfterCrankBeforeCrowDoor),
            ["Crank"] = new(3, ExpectedScope.AfterStonePuzzleBeforeCrank),
            ["SilhouettePazzlePieceOldHouse"] = new(3, ExpectedScope.BeforeOldHouseShadowPuzzle),
            ["SerumMaterialA"] = new(3, ExpectedScope.AfterLanternBeforeSnakeRooms),
            ["SerumMaterialB"] = new(3, ExpectedScope.BeforeJack3),
            ["EthanCarKey"] = new(3, ExpectedScope.Chapter3Start),
            ["SilhouettePazzlePiece"] = new(3, ExpectedScope.BeforeShadowPuzzle),
            ["EvCable"] = new(4, ExpectedScope.AfterCorrosiveBeforeRepair),
            ["EvOpener"] = new(4, ExpectedScope.BeforeLugWrench),
            ["SpareKey"] = new(4, ExpectedScope.MiaPresentShip),
            ["SerumTypeE"] = new(4, ExpectedScope.BeforeNecrotoxinUse),
        };

    private static readonly HashSet<string> ExpectedPreservedKeyItemIds = new(StringComparer.OrdinalIgnoreCase){
        "ChainCutter",
        "EntranceHallKey",
        "Fuse",
        "FuseCh4",
        "3CrestKeyC",
        "HandAxe",
        "Lantern",
        "LucasCardKey",
        "LucasCardKey2",
        "SerumComplete",
        "Candle_Lighted",
    };

    private static readonly IReadOnlyDictionary<string, ExpectedPickupFlag> ExpectedPickupFlags =
        new Dictionary<string, ExpectedPickupFlag>(StringComparer.OrdinalIgnoreCase){
            ["FloorDoorKey"] = new("c03_1_Main_GetFloorDoorKey", new("024d7582-3a98-4587-9b4f-a4dc47cd2cb4"), true),
            ["MasterKey"] = new("c03_4_Main_SnakeKeyGet", new("3ceeead9-da86-48c2-89ed-73edeab7fdbf"), true),
            ["Crank"] = new("c03_3_Main_GetCrank", new("e4ef4f89-4d98-4d81-86a0-8ea640eac4dc"), true),
            ["TalismanKey"] = new("c03_3_Main_TalismanKeyGet", new("6ed99e11-2047-4236-84a0-6457c7a3b1c9"), true),
            ["SilhouettePazzlePiece"] =
                new("c03_2_Main_GetPazzleObject", new("e165ff7a-0829-4edc-8c34-68a01a1ff3b2"), true),
            ["SerumMaterialA"] = new("c03_3_Main_GetEvlineArm", new("e4b4b42e-ecfc-415e-a713-e1a3604af371"), true),
            ["SerumMaterialB"] = new("c03_objective_EvlineFace_Get", new("21411c8c-2b95-418e-8efa-8bf79bae4ae5"), true),
            ["EvCable"] = new("c04_objective_ElevatorCableGetInventory", new("8dc1c235-4ffc-4894-bd45-ae1cf2e5fba2"),
                true),
            ["SerumTypeE"] = new("c04_objective_EnecrotoxinGet", new("a787b236-b098-4b69-93b2-6df65c97bbe6"), true),
        };

    private const string MainHouseHallScenePath = "natives/stm/environment/scene/chapter3/c03_mainhousehall.scn.20";

    private const string MainHouseDiningKitchenScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fldk.scn.20";

    private const string MainHouseLivingRoomScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fliving.scn.20";

    private const string MainHousePantryScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fpantry.scn.20";

    private const string MainHouseRightParlorScenePath =
        "natives/stm/environment/scene/chapter3/c03_rightarea1fparlor.scn.20";

    private const string MainHouseWestItemSetScenePath =
        "natives/stm/leveldesign/itemset/chapter3/mainhouse_west/mainhouse_west.scn.20";

    private const string MainHouseGarageScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse1fgarage.scn.20";

    private const string Jack2ScenePath = "natives/stm/environment/scene/chapter3/c03_rightareab1ffreezer.scn.20";

    private const string RedKeycardWorkshopScenePath =
        "natives/stm/leveldesign/itemset/chapter3/mainhouse_east/mainhouse_east.scn.20";

    private const string BlueKeycardAtticScenePath =
        "natives/stm/environment/scene/chapter3/c03_mainhouse2fkids02.scn.20";

    private const string GreenhouseStairsScenePath = "natives/stm/environment/scene/chapter3/c03_gh2fhallway01.scn.20";

    private const string SnakeKeyBodyScenePath =
        "natives/stm/environment/scene/chapter3/c03_rightareab1fstoreroom.scn.20";

    private const string OldHouseItemSetScenePath = "natives/stm/leveldesign/itemset/chapter3/oldhouse/oldhouse.scn.20";

    private const string OldHouseCrankScenePath =
        "natives/stm/environment/scene/chapter3/c03_oldhouse1funderfloor01.scn.20";

    private const string OldHouseDSeriesArmScenePath =
        "natives/stm/environment/scene/chapter3/c03_oldhouse1fstairs01.scn.20";

    private const string TestingAreaMonitorRoomScenePath =
        "natives/stm/environment/scene/chapter3/c03_leftarea1fmonitorroom.scn.20";

    private const string BoatHousePostJack3ScenePath =
        "natives/stm/environment/scene/chapter3/c03_boat1fbridge02.scn.20";

    private const string ShipExitScenePath = "natives/stm/environment/scene/chapter4/c04_shipb2storage02.scn.20";

    private const string ShipCaptainCabinItemSetScenePath =
        "natives/stm/leveldesign/itemset/chapter4/ship4f/ship4f.scn.20";

    private const string Ship2FItemSetScenePath = "natives/stm/leveldesign/itemset/chapter4/ship2f/ship2f.scn.20";
    private const string ShipFlashbackBasementItemSetScenePath = "natives/stm/leveldesign/itemset/ff050/bf/bf.scn.20";
    private const string ShipSickBayScenePath = "natives/stm/environment/scene/chapter4/c04_ship3finfirmary.scn.20";
    private const string ShipLoungeScenePath = "natives/stm/environment/scene/chapter4/c04_ship2freceptionroom.scn.20";

    private const string ShipPastKitchenScenePath =
        "natives/stm/environment/scene/chapter4/c04_ship2fkitchenpast.scn.20";

    private const string LucasPuzzleRoomScenePath =
        "natives/stm/environment/scene/chapter3/c03_leftarea1fpuzzleroom1.scn.20";

    private const string SaltMineCultivationRoomScenePath =
        "natives/stm/environment/scene/chapter4/c04_cavepassage05.scn.20";

    private static readonly Guid MainHouseHallDrawerCoinGuid = new("ccd5a2ee-49f5-485b-97a8-42cf8282da07");
    private static readonly Guid GuestHouseFuseCabinetGuid = new("b116eb16-c4c5-4d43-8901-044ec9dccbcf");
    private static readonly Guid GuestHouseMiaDriversLicenseGuid = new("ee3242fe-55a4-450c-b8ca-0a8ab3c39546");
    private static readonly Guid MainHouseWestBlueDogHeadGuid = new("401dbfaa-3469-0702-1c9a-d74a7d185216");
    private static readonly Guid MainHouseHatchKeyGuid = new("665a86ed-7e9c-4b56-a889-4377fa1d3f47");
    private static readonly Guid MainHousePendulumGuid = new("7a0710fd-6939-02b3-1a5b-229ce8cf7e77");
    private static readonly Guid MainHouseRightParlorStimulantGuid = new("b1548f47-609a-0190-3976-50b2aeafd6b6");
    private static readonly Guid MainHouseGarageOxStatuetteGuid = new("3463277a-19ba-062b-2b8d-7d1f666496a2");
    private static readonly Guid MainHouseClockRewardGuid = new("0da28012-ad6a-0da5-1f0a-cacd2c677ed3");
    private static readonly Guid Jack2RedDogHeadGuid = new("301caf06-67b8-0645-11a1-faadce741e7d");
    private static readonly Guid MainHouseWestBlueKeycardGuid = new("896dd0bb-f3ee-41bf-b4a0-0b28e99da94c");
    private static readonly Guid RedKeycardWorkshopGuid = new("077f9206-19e7-4937-994b-cd13a80dabd4");
    private static readonly Guid BlueKeycardAtticGuid = new("ccf47d14-a937-43c4-9b87-f35b07d14034");
    private static readonly Guid BlueKeycardAtticVhsGuid = new("31aa60d9-4317-4139-973d-7c3d09fc426d");
    private static readonly Guid GreenhouseStairsItemGuid = new("af78cd5c-b090-4557-bd9c-2f6a0d74b0c0");
    private static readonly Guid OldHouseStoneStatuetteGuid = new("41a59cb8-7613-4d4b-a530-58aebfe0e1c8");
    private static readonly Guid OldHouseCrowKeyGuid = new("8b940901-8893-4091-a4ac-5a16b3de3a11");
    private static readonly Guid OldHouseCrankGuid = new("a3f59645-063b-41a5-a86a-6e5b8c507a88");
    private static readonly Guid OldHouseDSeriesArmGuid = new("1c01c49f-81fa-0d1c-2312-fd50eafe79a3");

    private static readonly Vector3 OldHouseShadowPuzzleProgressionFallbackPosition =
        new(-19.85224f, -2.963f, 92.8632f);

    private static readonly Vector3 OldHouseOriginalMiaCapturedRoomTriggerPosition =
        new(-51.50293f, -3.728563f, 76.21281f);

    private static readonly Guid TestingAreaDSeriesHeadGuid = new("89e0718a-9f23-0ba7-2ec6-0affca6c028b");
    private static readonly Guid TestingAreaCandleGuid = new("05606c7e-3669-497e-8196-561faefb95e5");
    private static readonly Guid ShipLugWrenchGuid = new("15114d15-56af-468e-ab53-154e305e0ad1");
    private static readonly Guid Ship2FItemSetCorrosiveGuid = new("284aa600-8e59-475a-82b4-d7cd353f70e9");
    private static readonly Guid ShipPowerCableGuid = new("ba174dbf-64e9-0e76-187a-801520648246");
    private static readonly Guid ShipLoungeFuseGuid = new("77468bde-b24f-0949-0741-de56355592c7");

    private static readonly Guid[] ExcludedShipFlashbackCorrosiveGuids =[
        new("dfa15efd-e270-4778-b0ed-192963e99a1e"),
        new("9c5de4b4-5e99-4bbc-bf21-bd904789401a"),
        new("802213a2-7a22-4d87-9b0f-af57c1bd4a71"),
        new("2c5c06d7-bcea-4df7-9a72-62657522dad6"),
    ];

    private static readonly Guid[] CultivationRoomDoorGuids =[
        new("3f4ca9a0-b4ff-432b-8784-1403fd1b687f"),
        new("55adadba-98ee-4086-bce7-3610a3bd9ecb"),
        new("03b4daed-2766-435e-96cb-1b4857b71f0a"),
        new("d7f7420e-e505-4772-a973-0342b1d58a85"),
    ];

    private static readonly Guid[] SnakeKeyBodyGuids =[
        new("96da0bd0-1a8b-4c35-bc02-695da693e8d4"),
        new("24512acb-965b-462c-941e-375f9d62bd5e"),
        new("751cff95-a933-48ad-8ffa-6f96e25f8959"),
    ];

    private const string GuestHouseMiaCellScenePath = "natives/stm/environment/scene/chapter1/c01_b1f.scn.20";

    private static readonly Guid[] MainHouseHallShotgunPuzzleGuids =[
        new("65a64069-d77d-0f46-31f9-f16439d0218e"),
        new("73fce2c5-6b93-047d-24be-ced1b2904df2"),
        new("a5594b78-dd67-47f9-b730-cc618d6d82c6"),
        new("46573881-af8d-045c-2145-946ddc5c01c9"),
    ];

    private const string ShipMaintenanceRoomScenePath =
        "natives/stm/environment/scene/chapter4/c04_ship1foffice.scn.20";

    private static readonly Guid ShipMaintenanceRoomDrawerHandgunGuid = new("23ffe0b9-43d3-4091-9588-bc45740c0b43");

    [Fact]
    public void KeyItemLocations_EmitsSeedSpecificHintAsset() {
        const int seed = 35825;
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["random-key-item-locations"] = true;
        });

        var output = RandomizerTest.RunOutput(configuration.ToJson(), seed);
        var hintsAsset = output.Assets.Single(asset => asset.Key == "4-key-hints");
        var html = Encoding.UTF8.GetString(hintsAsset.Data);

        Assert.Equal($"biorand-re7-{seed}-key-items.html", hintsAsset.FileName);
        Assert.Equal("Key Item Spoiler Maps", hintsAsset.Title);
        Assert.Contains($"Key Item Spoiler Maps (Seed {seed})", html);
        Assert.Contains("natives/stm/", html);
        Assert.Contains("data:image/gif;base64,", html);
        Assert.Contains("class=\"map-marker\"", html);
        Assert.DoesNotContain("<table", html);
        Assert.DoesNotContain("Other route locations", html);
    }

    [Fact]
    public void KeyItemLocations_RandomizesSupportedKeyItemsIntoRouteSafeNormalPlacements() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToList();

        Assert.NotEmpty(randomizedKeyItems);

        var unmappedTargets = KeyItemLocationModifier.GetRouteTargetsForTesting(result.Randomizer)
            .Where(target => !KeyItemMapRepository.TryGetLocation(target.TargetGuid, out _))
            .ToArray();
        Assert.True(unmappedTargets.Length == 0,
            $"Missing spoiler-map locations: {string.Join("; ", unmappedTargets.Select(target => $"{target.TargetGuid}: {target.Description}"))}");

        foreach (var change in randomizedKeyItems) {
            var rule = ExpectedRules[change.AfterId];
            Assert.Equal(rule.Chapter, change.Placement.Chapter);
            Assert.True(ScopeMatches(rule.Scope, change.Placement),
                $"{change.AfterId} was placed in unexpected scene {change.Placement.SceneFile}.");
        }

        var duplicateKeyItemLocation = randomizedKeyItems
            .GroupBy(change => GetCollisionKey(change.Placement))
            .FirstOrDefault(group => group.Count() > 1);
        Assert.Null(duplicateKeyItemLocation);

        Assert.DoesNotContain(randomizedKeyItems,
            change => change.AfterId == "3CrestKeyB" && change.Placement.Chapter == 4);
        Assert.DoesNotContain(randomizedKeyItems,
            change => change.AfterId == "MorgueKey" &&
                      !ScopeMatches(ExpectedScope.BeforeShadowPuzzle, change.Placement));
        Assert.DoesNotContain(randomizedKeyItems,
            change => change.AfterId == "SerumTypeE" &&
                      change.Placement.SceneFile.Contains("/chapter4/lastbattle", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void KeyItemLocations_BoundedSearchRegressionSeedKeepsSafePartialRouteWhenHatchKeyIsVanilla() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; }, seed: 16);

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .Select(change => change.AfterId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.NotEmpty(randomizedKeyItems);
        Assert.DoesNotContain("FloorDoorKey", randomizedKeyItems);
        Assert.DoesNotContain("Skipped full key item route", result.ProcessLog);
        Assert.Contains("Skipped key item Hatch Key: no route-safe candidate placement was found.", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_PreservesVanillaHatchKeyWhenNoRouteSafeTargetForSoftlockSeed() {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["replace-madhouse-tapes"] = true;
            config["replace-weapons"] = true;
            config["additional-items"] = true;
            config["additional-wooden-crates"] = true;
        }, seed: 736361);

        var change = GetChangedPlacements(result).SingleOrDefault(changed =>
            changed.Placement.Guid == MainHouseHatchKeyGuid &&
            changed.Placement.SceneFile.Equals(MainHouseWestItemSetScenePath, StringComparison.OrdinalIgnoreCase));
        var hatchKey = GetItem(result.ReadAfterScene(MainHouseWestItemSetScenePath), MainHouseHatchKeyGuid);

        Assert.Null(change);
        Assert.Equal("FloorDoorKey", hatchKey.ItemDataID);
        Assert.Contains("Skipped key item Hatch Key: no route-safe candidate placement was found.", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_DoesNotUseDuplicateBlueKeycardCarrierForKeyItems() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; }, seed: 2);

        var change = GetChangedPlacements(result).SingleOrDefault(changed =>
            changed.Placement.Guid == MainHouseWestBlueKeycardGuid &&
            changed.Placement.SceneFile.Equals(MainHouseWestItemSetScenePath, StringComparison.OrdinalIgnoreCase));
        var duplicateBlueKeycard =
            GetItem(result.ReadAfterScene(MainHouseWestItemSetScenePath), MainHouseWestBlueKeycardGuid);

        Assert.Null(change);
        Assert.Equal("LucasCardKey", duplicateBlueKeycard.ItemDataID);
    }

    [Fact]
    public void KeyItemLocations_DoesNotReplaceExcludedCorrosivesWithFiller() {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["replace-weapons"] = true;
            config["additional-items"] = true;
            config["additional-wooden-crates"] = true;
        }, seed: 999477);

        var changedExcludedCorrosives = GetChangedPlacements(result)
            .Where(change =>
                change.Placement.SceneFile.Equals(ShipFlashbackBasementItemSetScenePath,
                    StringComparison.OrdinalIgnoreCase) &&
                ExcludedShipFlashbackCorrosiveGuids.Contains(change.Placement.Guid))
            .ToList();

        Assert.Empty(changedExcludedCorrosives);
        foreach (var guid in ExcludedShipFlashbackCorrosiveGuids) {
            var placement = FindPlacement(result, ShipFlashbackBasementItemSetScenePath, guid);
            var corrosive = GetItem(result.ReadAfterScene(ShipFlashbackBasementItemSetScenePath), guid);

            Assert.Contains(ItemPlacement.ExcludeTag, placement.Tags);
            Assert.Equal("SpareKey", corrosive.ItemDataID);
        }
    }

    [Fact]
    public void KeyItemLocations_DoesNotTreatLateMainHouseWestKeyCarriersAsPreHatchTargets() {
        var result = _defaultRun.Result;
        var blueDogHead = FindPlacement(result, MainHouseWestItemSetScenePath, MainHouseWestBlueDogHeadGuid);
        var duplicateBlueKeycard = FindPlacement(result, MainHouseWestItemSetScenePath, MainHouseWestBlueKeycardGuid);

        foreach (var earlyKeyId in new[]{ "FloorDoorKey", "EthanCarKey" }) {
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueDogHead, earlyKeyId));
            Assert.False(
                KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(duplicateBlueKeycard, earlyKeyId));
        }

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueDogHead, "MasterKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(duplicateBlueKeycard, "MasterKey"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueDogHead, "MorgueKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(duplicateBlueKeycard, "MorgueKey"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueDogHead, "3CrestKeyB"));
        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(duplicateBlueKeycard, "LucasCardKey2"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotTreatDiningRoomExtraAsChapter3StartTarget() {
        var result = _defaultRun.Result;
        var diningTable = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Dinner Table" &&
            placement.SceneFile.Equals(MainHouseDiningKitchenScenePath, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, diningTable.Chapter);
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "FloorDoorKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "EthanCarKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "MorgueKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "MasterKey"));
    }

    [Fact]
    public void KeyItemLocations_RestrictsHatchKeyToReachablePreHatchRooms() {
        var result = _defaultRun.Result;
        var diningTable = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Dinner Table" &&
            placement.SceneFile.Equals(MainHouseDiningKitchenScenePath, StringComparison.OrdinalIgnoreCase));
        var pantryUnderHatch = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Pantry" &&
            placement.SceneFile.Equals(MainHousePantryScenePath, StringComparison.OrdinalIgnoreCase));
        var hatchKeyCarrier = FindPlacement(result, MainHouseWestItemSetScenePath, MainHouseHatchKeyGuid);
        var mainHallExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Main Hall" &&
            placement.SceneFile.Equals(MainHouseHallScenePath, StringComparison.OrdinalIgnoreCase));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "FloorDoorKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(pantryUnderHatch, "FloorDoorKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(hatchKeyCarrier, "FloorDoorKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(mainHallExtra, "FloorDoorKey"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotPlaceDissectionRoomKeyBeforeBasementEntry() {
        var result = _defaultRun.Result;
        var diningTable = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Dinner Table" &&
            placement.SceneFile.Equals(MainHouseDiningKitchenScenePath, StringComparison.OrdinalIgnoreCase));
        var mainHallExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Main Hall" &&
            placement.SceneFile.Equals(MainHouseHallScenePath, StringComparison.OrdinalIgnoreCase));
        var mainHallClock = FindPlacement(result, MainHouseHallScenePath, MainHousePendulumGuid);
        var rightParlorStimulant =
            FindPlacement(result, MainHouseRightParlorScenePath, MainHouseRightParlorStimulantGuid);

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(diningTable, "WorkroomKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(mainHallExtra, "WorkroomKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(mainHallClock, "WorkroomKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(rightParlorStimulant, "WorkroomKey"));
    }

    [Fact]
    public void KeyItemLocations_Softlock2SeedKeepsHatchKeyBeforeHatchAndPreservesOxStatuette() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["random-items"] = true;
                config["replace-madhouse-tapes"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
                config["additional-wooden-crates"] = true;
            },
            seed: 736361);

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToDictionary(change => change.AfterId, StringComparer.OrdinalIgnoreCase);
        var hatchKey = GetItem(result.ReadAfterScene(MainHouseWestItemSetScenePath), MainHouseHatchKeyGuid);
        var oxStatuette = GetItem(result.ReadAfterScene(MainHouseGarageScenePath), MainHouseGarageOxStatuetteGuid);

        Assert.False(randomizedKeyItems.ContainsKey("FloorDoorKey"));
        Assert.Equal("FloorDoorKey", hatchKey.ItemDataID);
        Assert.Equal("EntranceHallKey", oxStatuette.ItemDataID);
        Assert.Contains("Skipped key item Hatch Key: no route-safe candidate placement was found.", result.ProcessLog);
        Assert.DoesNotContain("Ox Statuette ->", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_ReplacesOriginalSupportedKeyItemPickupsWithFillers() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var randomizedIds = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .Select(change => change.AfterId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var placement in result.ItemPlacementService.MainGamePlacements
                     .Where(placement =>
                         !string.IsNullOrWhiteSpace(placement.Id) &&
                         randomizedIds.Contains(placement.Id) &&
                         placement.Enabled &&
                         !placement.Tags.Contains(ItemPlacement.ExcludeTag) &&
                         !placement.IsExtra &&
                         !IsFlashbackPath(placement.SceneFile))
                     .DistinctBy(placement => (placement.SceneFile, placement.Guid))) {
            var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);
            Assert.NotEqual(placement.Id, afterItem.ItemDataID);
        }
    }

    [Fact]
    public void KeyItemLocations_RandomizedKeyItemPickupsUseFreshInteractions() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToList();

        Assert.NotEmpty(randomizedKeyItems);

        foreach (var change in randomizedKeyItems) {
            var beforeScene = result.ReadBeforeScene(change.Placement.SceneFile);
            var afterScene = result.ReadAfterScene(change.Placement.SceneFile);
            var targetGuid = GetTargetGuid(change.Placement);
            var beforeGameObject = change.Placement.IsExtra ? null : beforeScene.FindGameObject(targetGuid);
            var gameObject = afterScene.FindGameObject(targetGuid);
            Assert.NotNull(gameObject);

            if (!change.Placement.IsExtra) {
                Assert.NotNull(beforeGameObject);
                Assert.False(
                    HasDrawerContext(beforeScene, targetGuid),
                    $"{change.AfterId} was placed in a drawer-backed pickup: {change.Placement.SceneFile} / {targetGuid}.");

                if (ShouldPreserveOriginalPickupCarrier(change.Placement, beforeScene, targetGuid)) {
                    AssertOriginalPickupShapePreserved(beforeGameObject!, gameObject!, change.AfterId);
                    AssertVisualResourcesMatch(result.Randomizer.TemplateService.GetItemTemplate(change.AfterId),
                        gameObject!);
                    continue;
                }
            }

            AssertPickupInteractionsAreReadyForFreshPlacement(gameObject!, change.AfterId);
        }
    }

    [Fact]
    public void KeyItemLocations_RandomizedPickupsSetRequiredAcquisitionFlags() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedPickupFlags.ContainsKey(change.AfterId))
            .ToDictionary(change => change.AfterId, StringComparer.OrdinalIgnoreCase);

        Assert.NotEmpty(randomizedKeyItems);

        foreach (var (itemId, change) in randomizedKeyItems) {
            var expectedFlag = ExpectedPickupFlags[itemId];
            var beforeScene = result.ReadBeforeScene(change!.Placement.SceneFile);
            var targetGuid = GetTargetGuid(change.Placement);
            var gameObject = result.ReadAfterScene(change.Placement.SceneFile)
                .FindGameObject(targetGuid);
            Assert.NotNull(gameObject);
            if (!change.Placement.IsExtra &&
                ShouldPreserveOriginalPickupCarrier(change.Placement, beforeScene, targetGuid) &&
                !HasPickupInteraction(gameObject!)) {
                continue;
            }

            AssertPickupSetsBoolFlag(
                gameObject!,
                itemId,
                expectedFlag.Name,
                expectedFlag.Guid,
                expectedFlag.Value);
        }

        Assert.DoesNotContain("Pickup side effect: sets c03_2_Main_OpenTrayInWorkshopKey", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_AddsOldHouseShadowPuzzleProgressionFallbackTriggerOnNaturalPuzzlePath() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        Assert.True(result.WasFileModified(KeyItemLocationModifier.OldHouseLevelFsmScenePath));
        var trigger = result.ReadAfterScene(KeyItemLocationModifier.OldHouseLevelFsmScenePath)
            .FindGameObject(KeyItemLocationModifier.OldHouseShadowPuzzleProgressionTriggerName);
        Assert.NotNull(trigger);

        var transform = trigger!.FindComponent<GeneratedViaTransform>();
        Assert.NotNull(transform);
        Assert.True(
            Vector3.Distance(OldHouseShadowPuzzleProgressionFallbackPosition, transform!.Position) < 0.001f,
            "Old House progression fallback should be on the shadow-puzzle path, not in the vanilla Stone Statuette room.");
        Assert.True(
            Vector3.Distance(OldHouseOriginalMiaCapturedRoomTriggerPosition, transform.Position) > 30f,
            "Old House progression fallback must not force a detour through the vanilla Stone Statuette room.");
        Assert.True(transform.Scale.X >= 5f);
        Assert.True(transform.Scale.Z >= 6f);

        var actions = GetSetBoolActions(trigger);
        Assert.Contains(actions, action =>
            action.Variable.Equals("c03_3_Main_EnterMiaCapturedRoom", StringComparison.Ordinal) &&
            action.Guid == new Guid("17e5af29-0cab-4e3c-a78d-71ee87798b6c") &&
            action.Status == 1);
        Assert.Contains(actions, action =>
            action.Variable.Equals("c03_3_Main_PassThroughFirePlaceAgain", StringComparison.Ordinal) &&
            action.Guid == new Guid("779d13a7-5296-4a4a-bf04-c92e8930cc90") &&
            action.Status == 1);
        Assert.Contains("Old House shadow puzzle progression fallback added", result.ProcessLog);
    }

    [Theory]
    [InlineData(424855)]
    [InlineData(702402)]
    public void KeyItemLocations_OldHouseStoneStatuetteDoesNotFakeMiaCapturedRoomTriggerForReportedSeeds(int seed) {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["replace-madhouse-tapes"] = true;
            config["replace-weapons"] = true;
            config["additional-items"] = true;
            config["additional-wooden-crates"] = true;
        }, seed);

        var change = GetChangedPlacements(result).SingleOrDefault(change =>
            change.AfterId.Equals("SilhouettePazzlePieceOldHouse", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(change);

        var targetGuid = GetTargetGuid(change!.Placement);
        var gameObject = result.ReadAfterScene(change.Placement.SceneFile).FindGameObject(targetGuid);
        Assert.NotNull(gameObject);

        AssertPickupDoesNotSetBoolFlag(
            gameObject!,
            "SilhouettePazzlePieceOldHouse",
            "c03_3_Main_EnterMiaCapturedRoom",
            new("17e5af29-0cab-4e3c-a78d-71ee87798b6c"));
        Assert.Contains("Skipped pickup side effects for Stone Statuette:", result.ProcessLog);
        Assert.DoesNotContain("Pickup side effect: sets c03_3_Main_EnterMiaCapturedRoom", result.ProcessLog);
        Assert.DoesNotContain("Skipped key item Stone Statuette:", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_PreservesVanillaUnsafeKeyItems() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var changes = GetChangedPlacements(result).ToList();

        Assert.DoesNotContain(
            changes,
            change =>
                ExpectedPreservedKeyItemIds.Contains(change.BeforeId) ||
                ExpectedPreservedKeyItemIds.Contains(change.AfterId));
        foreach (var itemId in ExpectedPreservedKeyItemIds) {
            Assert.Contains($"Skipped key item {ItemDefinitionRepository.Default.GetName(itemId)}:", result.ProcessLog);
        }
    }

    [Fact]
    public void KeyItemLocations_PatchesLucasPuzzleRoomInventoryGate() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var beforeChecks =
            GetLucasPuzzleInventoryChecks(result.ReadBeforeScene(LucasPuzzleInventoryModifier.ScenePath));
        var afterScene = result.ReadAfterScene(LucasPuzzleInventoryModifier.ScenePath);
        var afterChecks = GetLucasPuzzleInventoryChecks(afterScene);
        var afterActions = GetLucasPuzzleFsmActions(afterScene);

        Assert.Equal(9, beforeChecks.Count);
        Assert.Empty(afterChecks);
        Assert.All(beforeChecks, action => Assert.True(action.Get<bool>("v0_Enabled")));
        foreach (var beforeCheck in beforeChecks) {
            var uid = beforeCheck.Get<uint>("v2_UID");
            var listNo = beforeCheck.Get<byte>("v3_ListNo");
            var afterAction = Assert.Single(afterActions, action =>
                action.Get<uint>("v2_UID") == uid &&
                action.Get<byte>("v3_ListNo") == listNo);
            Assert.Equal("app.fsm.Wait", afterAction.Type.Name);
            Assert.True(afterAction.Get<bool>("v0_Enabled"));
            Assert.Equal(0f, afterAction.Get<float>("Time"));
            Assert.Equal(0f, afterAction.Get<float>("RandamMax"));
            Assert.Equal(0, afterAction.Get<int>("WaitType"));
            Assert.Equal(Guid.Empty, afterAction.Get<Guid>("SetFlag"));
        }

        Assert.Contains("Lucas puzzle room inventory gate: replaced 9 inventory-empty FSM checks with successful waits",
            result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_LeavesLucasPuzzleRoomInventoryGateVanillaWhenDisabled() {
        var result = _defaultRun.Result;
        var checks = GetLucasPuzzleInventoryChecks(result.ReadAfterScene(LucasPuzzleInventoryModifier.ScenePath));

        Assert.False(result.WasFileModified(LucasPuzzleInventoryModifier.ScenePath));
        Assert.Equal(9, checks.Count);
        Assert.All(checks, action => Assert.True(action.Get<bool>("v0_Enabled")));
    }

    [Fact]
    public void KeyItemLocations_DetectsFsmControlledPickupPlacements() {
        var result = _defaultRun.Result;
        var scene = result.ReadBeforeScene(MainHouseHallScenePath);

        Assert.True(HasFsmInHierarchy(scene, MainHouseHallDrawerCoinGuid));
        Assert.True(HasDrawerContext(scene, MainHouseHallDrawerCoinGuid));
    }

    [Fact]
    public void KeyItemLocations_DetectsInteractDrawerReferencedPickupPlacements() {
        var result = _defaultRun.Result;
        var scene = result.ReadBeforeScene(ShipMaintenanceRoomScenePath);

        Assert.True(HasFsmInHierarchy(scene, ShipMaintenanceRoomDrawerHandgunGuid));
        Assert.True(HasDrawerContext(scene, ShipMaintenanceRoomDrawerHandgunGuid));
    }

    [Fact]
    public void KeyItemLocations_DoesNotPlaceKeyItemsInShipMaintenanceRoomDrawerPickup() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
            },
            seed: 999477);

        var drawerItem = GetItem(result.ReadAfterScene(ShipMaintenanceRoomScenePath),
            ShipMaintenanceRoomDrawerHandgunGuid);
        Assert.DoesNotContain(drawerItem.ItemDataID, ExpectedRules.Keys);
    }

    [Fact]
    public void KeyItemLocations_DoesNotPlaceKeyItemsInMainHallShotgunPuzzlePickups() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
            },
            seed: 999477);

        var scene = result.ReadAfterScene(MainHouseHallScenePath);
        foreach (var guid in MainHouseHallShotgunPuzzleGuids) {
            var item = GetItem(scene, guid);
            Assert.DoesNotContain(item.ItemDataID, ExpectedRules.Keys);
        }
    }

    [Fact]
    public void KeyItemLocations_DoesNotTreatFuseCabinetAsPreBoltCuttersCandidate() {
        var result = _defaultRun.Result;
        var fuseCabinet = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.Guid == GuestHouseFuseCabinetGuid &&
            placement.SceneFile.Equals("natives/stm/environment/scene/chapter1/c01_corridor01.scn.20",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(fuseCabinet, "ChainCutter"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(fuseCabinet, "Fuse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(fuseCabinet, "HandAxe"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotUseMissableGuestHouseOutsidePlacements() {
        var result = _defaultRun.Result;
        var driversLicense = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.Guid == GuestHouseMiaDriversLicenseGuid &&
            placement.SceneFile.Equals("natives/stm/environment/scene/chapter1/c01_outside11.scn.20",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(driversLicense, "ChainCutter"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(driversLicense, "Fuse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(driversLicense, "HandAxe"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotTreatMiaCellAsPreBoltCuttersCandidate() {
        var result = _defaultRun.Result;
        var miaCell = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Mia's Cell" &&
            placement.SceneFile.Equals(GuestHouseMiaCellScenePath, StringComparison.OrdinalIgnoreCase));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(miaCell, "ChainCutter"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(miaCell, "Fuse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(miaCell, "HandAxe"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotTreatClockRewardAsPrePendulumCandidate() {
        var result = _defaultRun.Result;
        var clockReward = FindPlacement(result, MainHouseLivingRoomScenePath, MainHouseClockRewardGuid);

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(clockReward, "FloorDoorKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(clockReward, "EthanCarKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(clockReward, "EntranceHallKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(clockReward, "PendulumClock"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(clockReward, "3CrestKeyA"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotUseJack2RedDogHeadAsKeyItemTarget() {
        var result = _defaultRun.Result;
        var jack2RedDogHead = FindPlacement(result, Jack2ScenePath, Jack2RedDogHeadGuid);

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(jack2RedDogHead, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(jack2RedDogHead, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(jack2RedDogHead, "SerumMaterialB"));
    }

    [Fact]
    public void KeyItemLocations_PreservesKeycardsAndAvoidsAtticShadowPuzzleTargets() {
        var result = _defaultRun.Result;
        var blueKeycardAttic = FindPlacement(result, BlueKeycardAtticScenePath, BlueKeycardAtticGuid);
        var blueKeycardAtticVhs = FindPlacement(result, BlueKeycardAtticScenePath, BlueKeycardAtticVhsGuid);
        var blueKeycardDuplicate = FindPlacement(result, MainHouseWestItemSetScenePath, MainHouseWestBlueKeycardGuid);
        var redKeycardWorkshop = FindPlacement(result, RedKeycardWorkshopScenePath, RedKeycardWorkshopGuid);
        var greenhouseStairs = FindPlacement(result, GreenhouseStairsScenePath, GreenhouseStairsItemGuid);
        var snakeKeyBody = FindPlacement(result, SnakeKeyBodyScenePath, SnakeKeyBodyGuids[0]);

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueKeycardAttic, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueKeycardAttic, "LucasCardKey2"));
        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueKeycardAtticVhs, "SerumMaterialB"));
        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueKeycardDuplicate, "LucasCardKey"));
        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(blueKeycardDuplicate, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(redKeycardWorkshop, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(redKeycardWorkshop, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "Candle_Lighted"));
    }

    [Fact]
    public void KeyItemLocations_PreservesKeycardsForAtticShadowPuzzleSoftlockSeed() {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["replace-madhouse-tapes"] = true;
            config["replace-weapons"] = true;
            config["additional-items"] = true;
        }, seed: 866537);

        var changedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToList();
        var duplicateBlueKeycard =
            GetItem(result.ReadAfterScene(MainHouseWestItemSetScenePath), MainHouseWestBlueKeycardGuid);
        var atticBlueKeycard = GetItem(result.ReadAfterScene(BlueKeycardAtticScenePath), BlueKeycardAtticGuid);
        var redKeycard = GetItem(result.ReadAfterScene(RedKeycardWorkshopScenePath), RedKeycardWorkshopGuid);

        Assert.Equal("LucasCardKey", duplicateBlueKeycard.ItemDataID);
        Assert.Equal("LucasCardKey", atticBlueKeycard.ItemDataID);
        Assert.Equal("LucasCardKey2", redKeycard.ItemDataID);
        Assert.DoesNotContain(changedKeyItems, change =>
            change.Placement.SceneFile.Equals(BlueKeycardAtticScenePath, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(changedKeyItems, change => change.AfterId is "LucasCardKey" or "LucasCardKey2");
        Assert.Contains("Skipped key item Keycard (Blue):", result.ProcessLog);
        Assert.Contains("Skipped key item Keycard (Red):", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_SeparatesTestingAreaBatteryAndBirthdayPuzzlePhases() {
        var result = _defaultRun.Result;
        var barnPlacement = result.ItemPlacementService.MainGamePlacements.First(placement =>
            !placement.IsExtra &&
            placement.Id == "ShotgunBullet" &&
            placement.SceneFile.Contains("/leveldesign/itemset/chapter3/cowshed/normal.scn",
                StringComparison.OrdinalIgnoreCase));
        var monitorRoomExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Testing Area Safe Room" &&
            placement.SceneFile.Equals(TestingAreaMonitorRoomScenePath, StringComparison.OrdinalIgnoreCase));
        var dSeriesHead = FindPlacement(result, TestingAreaMonitorRoomScenePath, TestingAreaDSeriesHeadGuid);
        var puzzleCandle = FindPlacement(result, LucasPuzzleRoomScenePath, TestingAreaCandleGuid);

        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(barnPlacement, "Battery"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(barnPlacement, "Candle_Lighted"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(barnPlacement, "SerumMaterialA"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(barnPlacement, "SerumMaterialB"));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(monitorRoomExtra, "Battery"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(monitorRoomExtra, "Candle_Lighted"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(monitorRoomExtra, "SerumMaterialA"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(monitorRoomExtra, "SerumMaterialB"));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(dSeriesHead, "Battery"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(dSeriesHead, "Candle_Lighted"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(puzzleCandle, "SerumMaterialA"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(puzzleCandle, "SerumMaterialB"));
    }

    [Fact]
    public void KeyItemLocations_DoesNotRandomizeSerumComplete() {
        var result = _defaultRun.Result;
        var postJack3Bridge = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Post-Jack 3 Bridges" &&
            placement.SceneFile.Equals(BoatHousePostJack3ScenePath, StringComparison.OrdinalIgnoreCase));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(postJack3Bridge, "SerumComplete"));
    }

    [Fact]
    public void KeyItemLocations_RemovesCultivationRoomMineDoorsForEarlyNecrotoxinPickup() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var beforeScene = result.ReadBeforeScene(SaltMineCultivationRoomScenePath);
        var afterScene = result.ReadAfterScene(SaltMineCultivationRoomScenePath);
        foreach (var doorGuid in CultivationRoomDoorGuids) {
            Assert.NotNull(beforeScene.FindGameObject(doorGuid));
            Assert.Null(afterScene.FindGameObject(doorGuid));
        }

        Assert.Contains("Cultivation room mine doors removed:", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_TreatsSnakeKeyBodyAsAfterOldHouseNotProcessingArea() {
        var result = _defaultRun.Result;

        foreach (var guid in SnakeKeyBodyGuids) {
            var snakeKeyBody = FindPlacement(result, SnakeKeyBodyScenePath, guid);

            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "3CrestKeyB"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "3CrestKeyA"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "3CrestKeyC"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "WorkroomKey"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "TalismanKey"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "Crank"));
            Assert.False(
                KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody,
                    "SilhouettePazzlePieceOldHouse"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "Lantern"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "LucasCardKey"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "LucasCardKey2"));
            Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "Battery"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "SerumMaterialA"));
            Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(snakeKeyBody, "SerumMaterialB"));
        }
    }

    [Fact]
    public void KeyItemLocations_DoesNotPlaceOldHouseGateItemsBehindTheirOwnGates() {
        var result = _defaultRun.Result;
        var stoneStatuette = FindPlacement(result, OldHouseItemSetScenePath, OldHouseStoneStatuetteGuid);
        var crowKeyChest = FindPlacement(result, OldHouseItemSetScenePath, OldHouseCrowKeyGuid);
        var crankTunnel = FindPlacement(result, OldHouseCrankScenePath, OldHouseCrankGuid);
        var greenhouseStairs = FindPlacement(result, GreenhouseStairsScenePath, GreenhouseStairsItemGuid);
        var dSeriesArmAltar = FindPlacement(result, OldHouseDSeriesArmScenePath, OldHouseDSeriesArmGuid);
        var oldHouseStudyExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Old House Study Room" &&
            placement.SceneFile.Equals("natives/stm/environment/scene/chapter3/c03_oldhouse2fstudy01.scn.20",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(stoneStatuette,
                "SilhouettePazzlePieceOldHouse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(stoneStatuette, "MasterKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(stoneStatuette, "SerumMaterialA"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(stoneStatuette, "Crank"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(stoneStatuette, "TalismanKey"));

        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crankTunnel, "SilhouettePazzlePieceOldHouse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crankTunnel, "Crank"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crankTunnel, "MasterKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crankTunnel, "TalismanKey"));

        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crowKeyChest,
                "SilhouettePazzlePieceOldHouse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crowKeyChest, "Crank"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crowKeyChest, "TalismanKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(crowKeyChest, "Lantern"));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "TalismanKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "MasterKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "Crank"));
        Assert.False(
            KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs,
                "SilhouettePazzlePieceOldHouse"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "Lantern"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "LucasCardKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "LucasCardKey2"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(greenhouseStairs, "SerumMaterialA"));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(dSeriesArmAltar, "Lantern"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(dSeriesArmAltar, "MasterKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(dSeriesArmAltar, "SerumMaterialA"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(oldHouseStudyExtra, "Lantern"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(oldHouseStudyExtra, "SerumMaterialA"));
    }

    [Fact]
    public void KeyItemLocations_AllProgressionFlagCarriersRespectMinimumSafePhases() {
        var placements = _defaultRun.Result.ItemPlacementService.MainGamePlacements;
        var contracts = new[]{
            (ItemId: "Crank", Scope: ExpectedScope.AfterStonePuzzleBeforeCrank),
            (ItemId: "TalismanKey", Scope: ExpectedScope.AfterCrankBeforeCrowDoor),
            (ItemId: "EvCable", Scope: ExpectedScope.AfterCorrosiveBeforeRepair),
            (ItemId: "SerumMaterialB", Scope: ExpectedScope.BeforeJack3),
        };

        foreach (var contract in contracts) {
            var candidates = placements
                .Where(placement =>
                    KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(placement, contract.ItemId))
                .ToList();
            var vanillaPlacements = placements
                .Where(placement =>
                    string.Equals(placement.Id, contract.ItemId, StringComparison.OrdinalIgnoreCase) &&
                    !IsFlashbackPath(placement.SceneFile))
                .ToList();

            Assert.All(candidates, placement =>
                Assert.True(ScopeMatches(contract.Scope, placement),
                    $"{contract.ItemId} can use an unsafe carrier in {placement.SceneFile}."));
            Assert.Contains(vanillaPlacements, placement => ScopeMatches(contract.Scope, placement));
        }
    }

    [Fact]
    public void KeyItemLocations_SeparatesWreckedShipRepairPhases() {
        var result = _defaultRun.Result;
        var shipExitExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Wrecked Ship Exit" &&
            placement.SceneFile.Equals(ShipExitScenePath, StringComparison.OrdinalIgnoreCase));
        var captainCabinExtra = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Tags.Contains("random") &&
            placement.Comment == "Captain's Cabin" &&
            placement.SceneFile.Equals("natives/stm/environment/scene/chapter4/c04_ship4fcabina.scn.20",
                StringComparison.OrdinalIgnoreCase));
        var lugWrench = FindPlacement(result, ShipCaptainCabinItemSetScenePath, ShipLugWrenchGuid);
        var ship2FCorrosive = FindPlacement(result, Ship2FItemSetScenePath, Ship2FItemSetCorrosiveGuid);
        var powerCable = FindPlacement(result, ShipSickBayScenePath, ShipPowerCableGuid);
        var loungeFuse = FindPlacement(result, ShipLoungeScenePath, ShipLoungeFuseGuid);
        var shipPastKitchen = result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Ship Kitchen" &&
            placement.SceneFile.Equals(ShipPastKitchenScenePath, StringComparison.OrdinalIgnoreCase));

        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(captainCabinExtra, "EvOpener"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(captainCabinExtra, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(captainCabinExtra, "EvCable"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(lugWrench, "FuseCh4"));
        Assert.True(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(lugWrench, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(loungeFuse, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(loungeFuse, "EvOpener"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipPastKitchen, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipPastKitchen, "EvOpener"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(ship2FCorrosive, "EvOpener"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(ship2FCorrosive, "EvCable"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(ship2FCorrosive, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(powerCable, "EvOpener"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(powerCable, "SpareKey"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(powerCable, "FuseCh4"));

        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipExitExtra, "EvOpener"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipExitExtra, "EvCable"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipExitExtra, "FuseCh4"));
        Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(shipExitExtra, "SpareKey"));
    }

    [Fact]
    public void KeyItemLocations_RejectsPhotoRewardsAndKeysBehindTheirOwnDoors() {
        var result = _defaultRun.Result;
        var treasureGuids = new HashSet<Guid> {
            new("b1548f47-609a-0190-3976-50b2aeafd6b6"),
            new("abb03a3a-a10b-4dca-9385-6274ca2e004a"),
            new("5e6490f0-17f6-0ba5-1fdb-5d70e2ac3b2b"),
        };
        var rewards = result.ItemPlacementService.MainGamePlacements
            .Where(p => treasureGuids.Contains(p.Guid)).ToArray();
        Assert.Equal(3, rewards.Length);
        foreach (var reward in rewards) {
            foreach (var key in ExpectedRules.Keys)
                Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(reward, key));
        }

        foreach (var placement in result.ItemPlacementService.MainGamePlacements) {
            if (KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(placement, "EvOpener"))
                Assert.True(IsShipBeforeLugWrench(placement.SceneFile));
            if (placement.SceneFile.Contains("c03_mainhouse2fgrandma")) {
                Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(placement, "MorgueKey"));
                Assert.False(KeyItemLocationModifier.CanPlaceKeyItemInPlacementForTesting(placement, "SilhouettePazzlePiece"));
            }
        }
        var diagram = KeyItemLocationModifier.GenerateRouteGraphDiagram();
        var hatch = Assert.Single(diagram.Edges, e => e.SourceId == "ship" && e.TargetId == "ship-after-lug-wrench");
        Assert.Contains("Lug Wrench", hatch.Requirements);
    }

    [Theory]
    [InlineData(895914)]
    [InlineData(776198)]
    [InlineData(35825)]
    public void KeyItemLocations_ReleaseSeedsKeepKeysOutOfPhotoRewardsAndWrenchOn4F(int seed) {
        using var result = RandomizerTest.RunState(config => {
            config["random-key-item-locations"] = true;
            config["random-items"] = true;
            config["additional-items"] = true;
            config["start-chapter"] = "Main House";
        }, seed);
        var changes = GetChangedPlacements(result).Where(c => ExpectedRules.ContainsKey(c.AfterId)).ToArray();
        Assert.NotEmpty(changes);
        Assert.DoesNotContain(changes, c => c.Placement.Guid == new Guid("abb03a3a-a10b-4dca-9385-6274ca2e004a")
            || c.Placement.Guid == MainHouseRightParlorStimulantGuid
            || c.Placement.Guid == new Guid("5e6490f0-17f6-0ba5-1fdb-5d70e2ac3b2b"));
        Assert.All(changes.Where(c => c.AfterId == "EvOpener"), c => Assert.True(IsShipBeforeLugWrench(c.Placement.SceneFile)));
        // With no proven alternate carrier in the Processing Area, retain its native puzzle reward.
        Assert.DoesNotContain(changes, c => c.BeforeId == "WorkroomKey" || c.AfterId == "WorkroomKey");
    }

    [Theory]
    [InlineData(296769)]
    [InlineData(424855)]
    [InlineData(702402)]
    public void KeyItemLocations_ReportedSeedsRespectProgressionPickupPhaseFloors(int seed) {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["additional-items"] = true;
            },
            seed: seed);

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => change.AfterId is "Crank" or "TalismanKey" or "EvCable")
            .ToList();

        Assert.All(randomizedKeyItems, change => {
            var expectedScope = ExpectedRules[change.AfterId].Scope;
            Assert.True(ScopeMatches(expectedScope, change.Placement),
                $"{change.AfterId} violated its minimum pickup phase in {change.Placement.SceneFile}.");
        });

        foreach (var itemId in new[]{ "Crank", "TalismanKey", "EvCable" }) {
            Assert.True(
                randomizedKeyItems.Any(change => change.AfterId == itemId) ||
                result.ProcessLog.Contains(
                    $"Skipped key item {ItemDefinitionRepository.Default.GetName(itemId)}:",
                    StringComparison.Ordinal),
                $"{itemId} was neither assigned nor explicitly preserved for seed {seed}.");
        }
    }

    [Fact]
    public void KeyItemLocations_PreservesFlashbackKeyItemSources() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var flashbackCrank = result.ItemPlacementService.FromId("Crank")
            .First(placement => IsFlashbackPath(placement.SceneFile));
        var before = GetItem(result.ReadBeforeScene(flashbackCrank.SceneFile), flashbackCrank.Guid);
        var after = GetItem(result.ReadAfterScene(flashbackCrank.SceneFile), flashbackCrank.Guid);

        Assert.Equal("Crank", before.ItemDataID);
        Assert.Equal(before.ItemDataID, after.ItemDataID);
        Assert.Equal(before.SaveGUID, after.SaveGUID);
    }

    [Fact]
    public void KeyItemLocations_SoftlockSampleSeedKeepsGateItemsBeforeTheirUse() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["replace-madhouse-tapes"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
            },
            seed: 300214);

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToDictionary(change => change.AfterId, StringComparer.OrdinalIgnoreCase);

        foreach (var (itemId, change) in randomizedKeyItems) {
            var rule = ExpectedRules[itemId];
            Assert.Equal(rule.Chapter, change.Placement.Chapter);
            Assert.True(ScopeMatches(rule.Scope, change.Placement),
                $"{itemId} was placed in unexpected scene {change.Placement.SceneFile}.");
        }

        Assert.DoesNotContain(randomizedKeyItems.Values,
            change => GetTargetGuid(change.Placement) == Jack2RedDogHeadGuid);
    }

    [Fact]
    public void KeyItemLocations_LadderSoftlockSeedKeepsDSeriesArmAfterLanternDoorBeforeSnakeRooms() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["random-items"] = true;
                config["replace-madhouse-tapes"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
                config["additional-wooden-crates"] = true;
            },
            seed: 100583);

        var dSeriesArm = GetChangedPlacements(result)
            .Where(change => change.AfterId == "SerumMaterialA")
            .SingleOrDefault();

        if (dSeriesArm != null) {
            Assert.True(ScopeMatches(ExpectedScope.AfterLanternBeforeSnakeRooms, dSeriesArm.Placement),
                $"D-Series Arm was placed before the Lantern-door reward sphere: {dSeriesArm.Placement.SceneFile}.");
        }

        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House Stone Statuette pickup", result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House before Stone Statuette shadow puzzle",
            result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House cleared / Main House basement police body",
            result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Main House snake-key rooms", result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Testing Area", result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_StoneStatuettePodiumSoftlockSeedKeepsDSeriesArmAfterLanternDoor() {
        using var result = RandomizerTest.RunState(
            config => {
                config["random-key-item-locations"] = true;
                config["random-items"] = true;
                config["replace-madhouse-tapes"] = true;
                config["replace-weapons"] = true;
                config["additional-items"] = true;
                config["additional-wooden-crates"] = true;
            },
            seed: 305655);

        var dSeriesArm = GetChangedPlacements(result)
            .Where(change => change.AfterId == "SerumMaterialA")
            .SingleOrDefault();

        if (dSeriesArm != null) {
            Assert.True(ScopeMatches(ExpectedScope.AfterLanternBeforeSnakeRooms, dSeriesArm.Placement),
                $"D-Series Arm was placed before the Lantern-door reward sphere: {dSeriesArm.Placement.SceneFile}.");
        }

        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House Stone Statuette pickup", result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House before Stone Statuette shadow puzzle",
            result.ProcessLog);
        Assert.DoesNotContain("[KEY ITEM ROUTE] D-Series Arm -> Old House cleared / Main House basement police body",
            result.ProcessLog);
    }

    [Fact]
    public void KeyItemLocations_OnlyUsesPlainRandomItemsAsNonKeyTargets() {
        var definitions = ItemDefinitionRepository.Default;

        foreach (var id in new[]{
                     "BrokenShotgun_DB", "Shotgun_DB", "Shotgun_M37", "ToyShotgun", "PendulumClock", "Coin",
                     "TreasureMap01"
                 }) {
            var definition = definitions.FromId(id);
            Assert.NotNull(definition);
            Assert.False(KeyItemLocationModifier.IsPlainRandomItemTarget(definition),
                $"{id} should not be a key-item randomization target.");
        }

        foreach (var id in new[]
                     { "HandgunBullet", "ShotgunBullet", "ChemicalL", "ChemicalS", "SyntheticDetergent", "Herb" }) {
            var definition = definitions.FromId(id);
            Assert.NotNull(definition);
            Assert.True(KeyItemLocationModifier.IsPlainRandomItemTarget(definition),
                $"{id} should be a plain key-item randomization target.");
            Assert.Contains(definition.CategoryType,
                new[]{ ItemCategoryType.Shell, ItemCategoryType.Drug, ItemCategoryType.Material });
        }
    }

    [Fact]
    public void KeyItemLocations_DoesNotPlaceKeyItemsInDrawerPickups() {
        using var result = RandomizerTest.RunState(config => { config["random-key-item-locations"] = true; });

        var randomizedKeyItems = GetChangedPlacements(result)
            .Where(change => ExpectedRules.ContainsKey(change.AfterId))
            .ToList();

        foreach (var change in randomizedKeyItems) {
            var beforeScene = result.ReadBeforeScene(change.Placement.SceneFile);
            if (!change.Placement.IsExtra) {
                var targetGuid = GetTargetGuid(change.Placement);
                Assert.False(
                    HasDrawerContext(beforeScene, targetGuid),
                    $"{change.AfterId} was placed in a drawer-backed pickup: {change.Placement.SceneFile} / {targetGuid}.");
            }
        }

        var drawerItem = GetItem(result.ReadAfterScene(MainHouseHallScenePath), MainHouseHallDrawerCoinGuid);
        Assert.DoesNotContain(drawerItem.ItemDataID, ExpectedRules.Keys);
    }

    [Fact]
    public void KeyItemLocations_CanUsePlainExtraItemPlacementsWhenAdditionalItemsAreEnabled() {
        using var result = RandomizerTest.RunState(config => {
            config["additional-items"] = true;
            config["random-key-item-locations"] = true;
        });

        var extraKeyItem = GetChangedPlacements(result)
            .FirstOrDefault(change => change.Placement.IsExtra && ExpectedRules.ContainsKey(change.AfterId));

        Assert.NotNull(extraKeyItem);
        Assert.True(ExtraPlacementModifier.IsPlainExtraItemPlacement(extraKeyItem!.Placement));
        var gameObject = result.ReadAfterScene(extraKeyItem.Placement.SceneFile)
            .FindGameObject(GetTargetGuid(extraKeyItem.Placement));
        Assert.NotNull(gameObject);
        Assert.True(gameObject!.Settings.Get<bool>("Update"));
        Assert.True(gameObject.Settings.Get<bool>("Draw"));
    }

    [Fact]
    public void KeyItemLocations_PreservesExtraPickupCarrierInteractionsWhenAdditionalItemsAreEnabled() {
        using var extraOnly = RandomizerTest.RunState(config => { config["additional-items"] = true; });
        using var keyItems = RandomizerTest.RunState(config => {
            config["additional-items"] = true;
            config["random-key-item-locations"] = true;
        });

        var extraKeyItem = GetChangedPlacements(keyItems)
            .FirstOrDefault(change => change.Placement.IsExtra && ExpectedRules.ContainsKey(change.AfterId));

        Assert.NotNull(extraKeyItem);
        var targetGuid = GetTargetGuid(extraKeyItem!.Placement);
        var carrier = extraOnly.ReadAfterScene(extraKeyItem.Placement.SceneFile).FindGameObject(targetGuid);
        var replacement = keyItems.ReadAfterScene(extraKeyItem.Placement.SceneFile).FindGameObject(targetGuid);

        Assert.NotNull(carrier);
        Assert.NotNull(replacement);
        AssertOriginalPickupShapePreserved(carrier!, replacement!, extraKeyItem.AfterId);
        AssertVisualResourcesMatch(keyItems.Randomizer.TemplateService.GetItemTemplate(extraKeyItem.AfterId),
            replacement!);
    }

    [Fact]
    public void KeyItemLocations_CanMaterializePlainExtraItemPlacementsWhenAdditionalItemsAreDisabled() {
        using var result = RandomizerTest.RunState(config => {
            config["additional-items"] = false;
            config["random-key-item-locations"] = true;
        });

        var extraKeyItem = GetChangedPlacements(result)
            .FirstOrDefault(change => change.Placement.IsExtra && ExpectedRules.ContainsKey(change.AfterId));

        Assert.NotNull(extraKeyItem);
        Assert.True(ExtraPlacementModifier.IsPlainExtraItemPlacement(extraKeyItem!.Placement));
        var gameObject = result.ReadAfterScene(extraKeyItem.Placement.SceneFile)
            .FindGameObject(GetTargetGuid(extraKeyItem.Placement));
        Assert.NotNull(gameObject);
        Assert.True(gameObject!.Settings.Get<bool>("Update"));
        Assert.True(gameObject.Settings.Get<bool>("Draw"));
    }

    [Fact]
    public void KeyItemLocations_MainHallExtraPlacementUsesValidIdentityRotation() {
        using var result = RandomizerTest.RunState(config => { config["additional-items"] = true; });

        var placement = result.ItemPlacementService.ItemPlacements.Single(placement =>
            placement.IsExtra &&
            placement.Comment == "Main Hall" &&
            placement.SceneFile.Equals(MainHouseHallScenePath, StringComparison.OrdinalIgnoreCase));
        AssertQuaternionEquals(Quaternion.Identity, placement.Rotation);

        var scene = result.ReadAfterScene(MainHouseHallScenePath);
        var generatedGuid = ExtraPlacementModifier.GetGeneratedItemGuid(placement);
        var gameObject = scene.FindGameObject(generatedGuid);
        Assert.NotNull(gameObject);
        var transform = gameObject!.FindComponent<GeneratedViaTransform>();
        Assert.NotNull(transform);
        AssertQuaternionEquals(Quaternion.Identity, transform!.Rotation);
    }

    [Fact]
    public void KeyItemLocations_FsmControlledCoinPickup_CanUseBlueDogHeadVisuals() {
        var result = _defaultRun.Result;
        var scene = result.ReadBeforeScene(MainHouseHallScenePath);
        var coinGameObject = scene.FindGameObject(MainHouseHallDrawerCoinGuid);
        var blueDogHeadTemplate = result.Randomizer.TemplateService.GetItemTemplate("3CrestKeyA");

        Assert.NotNull(coinGameObject);
        var updated = coinGameObject!.ApplyVisualResourcesFromTemplate(blueDogHeadTemplate);

        AssertVisualResourcesMatch(blueDogHeadTemplate, updated);
        Assert.NotEqual(GetVisualResource(coinGameObject, "Mesh"), GetVisualResource(updated, "Mesh"));
        Assert.Equal(
            coinGameObject.Components.Select(component => component.Type.Name),
            updated.Components.Select(component => component.Type.Name));
        Assert.Equal(
            coinGameObject.Children.Select(child => child.Name),
            updated.Children.Select(child => child.Name));
    }

    private static IEnumerable<ChangedItemPlacement> GetChangedPlacements(RandomizerRunResult result) {
        foreach (var placement in result.ItemPlacementService.MainGamePlacements
                     .Where(placement =>
                         placement.Enabled &&
                         (!placement.IsExtra || ExtraPlacementModifier.IsPlainExtraItemPlacement(placement)))
                     .DistinctBy(placement => (placement.SceneFile, GetTargetGuid(placement)))) {
            if (!result.WasFileModified(placement.SceneFile))
                continue;

            var targetGuid = GetTargetGuid(placement);
            var beforeItem = placement.IsExtra
                ? null
                : GetItemOrNull(result.ReadBeforeScene(placement.SceneFile), targetGuid);
            var afterItem = GetItemOrNull(result.ReadAfterScene(placement.SceneFile), targetGuid);
            if (afterItem == null || beforeItem?.ItemDataID == afterItem.ItemDataID)
                continue;

            yield return new ChangedItemPlacement(placement, beforeItem?.ItemDataID ?? placement.Id,
                afterItem.ItemDataID);
        }
    }

    private static ItemPlacement FindPlacement(RandomizerRunResult result, string scenePath, Guid guid)
        => result.ItemPlacementService.MainGamePlacements.Single(placement =>
            placement.Guid == guid &&
            placement.SceneFile.Equals(scenePath, StringComparison.OrdinalIgnoreCase));

    private static Guid GetTargetGuid(ItemPlacement placement)
        => placement.IsExtra && ExtraPlacementModifier.IsPlainExtraItemPlacement(placement)
            ? ExtraPlacementModifier.GetGeneratedItemGuid(placement)
            : placement.Guid;

    private static (string SceneFile, int X, int Y, int Z) GetCollisionKey(ItemPlacement placement)
        => (
            placement.SceneFile,
            QuantizeLocation(placement.Position.X),
            QuantizeLocation(placement.Position.Y),
            QuantizeLocation(placement.Position.Z));

    private static int QuantizeLocation(float value)
        => (int)MathF.Round(value * 2, MidpointRounding.AwayFromZero);

    private static app.Item GetItem(RszScene scene, Guid guid) {
        var item = GetItemOrNull(scene, guid);
        Assert.NotNull(item);
        return item!;
    }

    private static app.Item? GetItemOrNull(RszScene scene, Guid guid)
        => scene.FindGameObject(guid)?.FindComponent<app.Item>();

    private static void AssertPickupInteractionsAreReadyForFreshPlacement(RszGameObject gameObject, string itemId) {
        var interactions = new List<app.InteractDetailSearch>();
        gameObject.VisitGameObjects(child => {
            var interact = child.FindComponent<app.InteractDetailSearch>();
            if (interact != null) {
                interactions.Add(interact);
            }
        });

        Assert.True(interactions.Count > 0, $"{itemId} replacement has no InteractDetailSearch pickup interaction.");
        Assert.All(interactions, interact => Assert.False(interact.IsCheckAngle));
        Assert.All(interactions, interact => Assert.False(interact.IsItemGet));
    }

    private static void AssertPickupSetsBoolFlag(
        RszGameObject gameObject,
        string itemId,
        string flagName,
        Guid flagGuid,
        bool value) {
        var interactions = new List<app.InteractDetailSearch>();
        gameObject.VisitGameObjects(child => {
            var interact = child.FindComponent<app.InteractDetailSearch>();
            if (interact != null) {
                interactions.Add(interact);
            }
        });

        Assert.True(interactions.Count > 0, $"{itemId} replacement has no InteractDetailSearch pickup interaction.");
        Assert.All(interactions, interact => {
            Assert.Equal(flagName, interact.SetFsmBoolFlag);
            Assert.Equal(flagGuid, interact.SetFsmBoolFlagId);
            Assert.Equal(value, interact.SetFsmBoolFlagValue);
        });
    }

    private static void AssertPickupDoesNotSetBoolFlag(
        RszGameObject gameObject,
        string itemId,
        string flagName,
        Guid flagGuid) {
        var interactions = new List<app.InteractDetailSearch>();
        gameObject.VisitGameObjects(child => {
            var interact = child.FindComponent<app.InteractDetailSearch>();
            if (interact != null) {
                interactions.Add(interact);
            }
        });

        Assert.True(interactions.Count > 0, $"{itemId} replacement has no InteractDetailSearch pickup interaction.");
        Assert.All(interactions, interact => {
            Assert.False(
                interact.SetFsmBoolFlag.Equals(flagName, StringComparison.OrdinalIgnoreCase) ||
                interact.SetFsmBoolFlagId == flagGuid,
                $"{itemId} pickup must not fake {flagName}; that flag is set by the native room-entry trigger.");
        });
    }

    private static List<(string Variable, Guid Guid, int Status)> GetSetBoolActions(RszGameObject gameObject) {
        var result = new List<(string Variable, Guid Guid, int Status)>();
        gameObject.Visit(node => {
            if (node is RszObjectNode objectNode &&
                objectNode.Type.Name == "via.fsm.action.SetBool") {
                result.Add((
                    objectNode.Get<string>("v4_Variable"),
                    objectNode.Get<Guid>("v5_Guid"),
                    objectNode.Get<int>("v6_Status")));
            }
        });

        return result;
    }

    private static void AssertOriginalPickupShapePreserved(RszGameObject before, RszGameObject after, string itemId) {
        Assert.Equal(itemId, after.FindComponent<app.Item>()!.ItemDataID);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(
            before.Components.Select(component => component.Type.Name),
            after.Components.Select(component => component.Type.Name));
        Assert.Equal(
            before.Children.Select(child => child.Name),
            after.Children.Select(child => child.Name));
    }

    private static void AssertVisualResourcesMatch(RszGameObject expected, RszGameObject actual) {
        Assert.Equal(GetVisualResource(expected, "Mesh"), GetVisualResource(actual, "Mesh"));
        Assert.Equal(GetVisualResource(expected, "Material"), GetVisualResource(actual, "Material"));
    }

    private static string GetVisualResource(RszGameObject gameObject, string fieldName) {
        var mesh = gameObject.FindComponent("via.render.Mesh");
        Assert.NotNull(mesh);
        return mesh![fieldName].ToString() ?? "";
    }

    private static bool HasFsmInHierarchy(RszScene scene, Guid guid)
        => scene.FindGameObjectsByGuidWithFsmContext([guid]).TryGetValue(guid, out var match) &&
           match.HasFsmInHierarchy;

    private static bool HasDrawerContext(RszScene scene, Guid guid)
        => scene.FindGameObjectsByGuidWithFsmContext([guid]).TryGetValue(guid, out var match) &&
           match.HasDrawerContext;

    private static bool HasPickupInteraction(RszGameObject gameObject) {
        var result = false;
        gameObject.VisitGameObjects(child => { result |= child.FindComponent<app.InteractDetailSearch>() != null; });
        return result;
    }

    private static List<RszObjectNode> GetLucasPuzzleInventoryChecks(RszScene scene)
        => GetLucasPuzzleFsmActions(scene)
            .Where(action => action.Type.Name == "app.fsm.CheckInventoryEmpty")
            .ToList();

    private static List<RszObjectNode> GetLucasPuzzleFsmActions(RszScene scene) {
        var result = new List<RszObjectNode>();

        foreach (var objectName in LucasPuzzleInventoryModifier.PatchedFsmGameObjectNames) {
            var gameObject = scene.FindGameObject(objectName);
            Assert.NotNull(gameObject);

            foreach (var component in gameObject!.Components.Where(component => component.Type.Name == "via.fsm.Fsm")) {
                component.Visit(node => {
                    if (node is RszObjectNode objectNode &&
                        objectNode.Type.FindFieldIndex("v2_UID") != -1 &&
                        objectNode.Type.FindFieldIndex("v3_ListNo") != -1) {
                        result.Add(objectNode);
                    }
                });
            }
        }

        return result;
    }

    private static bool ShouldPreserveOriginalPickupCarrier(ItemPlacement placement, RszScene beforeScene,
        Guid targetGuid) {
        if (HasFsmInHierarchy(beforeScene, targetGuid))
            return true;

        if (!string.IsNullOrWhiteSpace(placement.Id)
            && ExpectedRules.ContainsKey(placement.Id)) {
            return true;
        }

        if (string.IsNullOrWhiteSpace(placement.Id)) {
            return false;
        }

        var definition = ItemDefinitionRepository.Default.FromId(placement.Id);
        return definition != null && KeyItemLocationModifier.IsPlainRandomItemTarget(definition);
    }

    private static void AssertQuaternionEquals(Quaternion expected, Quaternion actual) {
        const float tolerance = 0.0001f;
        Assert.True(Math.Abs(expected.X - actual.X) <= tolerance, $"Expected X={expected.X}, actual X={actual.X}.");
        Assert.True(Math.Abs(expected.Y - actual.Y) <= tolerance, $"Expected Y={expected.Y}, actual Y={actual.Y}.");
        Assert.True(Math.Abs(expected.Z - actual.Z) <= tolerance, $"Expected Z={expected.Z}, actual Z={actual.Z}.");
        Assert.True(Math.Abs(expected.W - actual.W) <= tolerance, $"Expected W={expected.W}, actual W={actual.W}.");
    }

    private static bool ScopeMatches(ExpectedScope scope, ItemPlacement placement)
        => scope switch{
            ExpectedScope.GuestHouseBeforeBoltCutters => IsGuestHouseBeforeBoltCutters(placement.SceneFile),
            ExpectedScope.GuestHouseBeforeAxeFight => IsGuestHouseBeforeBoltCutters(placement.SceneFile)
                                                      || IsGuestHouseAfterBoltCutters(placement.SceneFile),
            ExpectedScope.GuestHouseAfterAxeFight => IsGuestHouseAfterAxeFight(placement.SceneFile),
            ExpectedScope.Chapter3Start => IsMainHouseBeforeGarage(placement),
            ExpectedScope.BeforeOxDoor => IsMainHouseBeforeGarage(placement)
                                          || IsGarage(placement.SceneFile),
            ExpectedScope.AfterGarageBeforeShadowPuzzle => IsMainHouseAfterGarageBeforeShadowPuzzle(placement),
            ExpectedScope.BeforeShadowPuzzle => IsMainHouseBeforeGarage(placement)
                                                || IsMainHouseAfterGarageBeforeShadowPuzzle(placement),
            ExpectedScope.BeforeDogDoor => IsMainHouseBeforeGarage(placement)
                                           || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                           || IsMainHouseEastOrBasement(placement.SceneFile),
            ExpectedScope.BeforeScorpionDoor => IsMainHouseBeforeGarage(placement)
                                                || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                                || IsMainHouseEastOrBasement(placement.SceneFile)
                                                || IsYardOrTrailer(placement.SceneFile),
            ExpectedScope.BeforeCrowDoor => IsMainHouseBeforeGarage(placement)
                                            || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                            || IsMainHouseEastOrBasement(placement.SceneFile)
                                            || IsYardOrTrailer(placement.SceneFile)
                                            || IsOldHouseBeforeCrowDoor(placement.SceneFile)
                                            || IsOldHouseStoneStatuettePickup(placement)
                                            || IsOldHouseCrowKeyPickup(placement)
                                            || IsOldHouseAfterStonePuzzleBeforeCrank(placement.SceneFile)
                                            || IsOldHouseAfterCrankBeforeCrowDoor(placement.SceneFile),
            ExpectedScope.AfterStonePuzzleBeforeCrank =>
                IsOldHouseAfterStonePuzzleBeforeCrank(placement.SceneFile),
            ExpectedScope.AfterCrankBeforeCrowDoor =>
                IsOldHouseCrowKeyPickup(placement) ||
                IsOldHouseAfterCrankBeforeCrowDoor(placement.SceneFile),
            ExpectedScope.AfterLanternBeforeSnakeRooms => IsOldHouseAfterLanternDoor(placement.SceneFile)
                                                          || IsSnakeKeyBody(placement.SceneFile),
            ExpectedScope.BeforeDissectionRoom => IsMainHouseBeforeGarage(placement)
                                                  || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                                  || IsMainHouseEastOrBasement(placement.SceneFile),
            ExpectedScope.BeforeOldHouseShadowPuzzle => IsMainHouseBeforeGarage(placement)
                                                        || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                                        || IsMainHouseEastOrBasement(placement.SceneFile)
                                                        || IsYardOrTrailer(placement.SceneFile)
                                                        || IsOldHouseBeforeCrowDoor(placement.SceneFile),
            ExpectedScope.OldHouseAfterCrowDoor => IsOldHouseAfterCrowDoorOrGreenHouse(placement.SceneFile)
                                                   && !IsOldHouseAfterLanternDoor(placement.SceneFile),
            ExpectedScope.BeforeSnakeRooms => IsBeforeSnakeRooms(placement),
            ExpectedScope.BeforeBarnBatterySocket => IsMainHouseBeforeGarage(placement)
                                                     || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                                     || IsMainHouseEastOrBasement(placement.SceneFile)
                                                     || IsYardOrTrailer(placement.SceneFile)
                                                     || IsOldHouseBeforeCrowDoor(placement.SceneFile)
                                                     || IsOldHouseStoneStatuettePickup(placement)
                                                     || IsOldHouseCrowKeyPickup(placement)
                                                     || IsOldHouseAfterStonePuzzleBeforeCrank(placement.SceneFile)
                                                     || IsOldHouseAfterCrankBeforeCrowDoor(placement.SceneFile)
                                                     || IsOldHouseAfterCrowDoorOrGreenHouse(placement.SceneFile)
                                                     || IsSnakeKeyBody(placement.SceneFile)
                                                     || IsMainHouseSnakeKeyRoom(placement.SceneFile)
                                                     || IsTestingAreaBeforeLucasPuzzle(placement.SceneFile)
                                                     || IsTestingAreaBeforeBarnFight(placement.SceneFile),
            ExpectedScope.BeforeTestingAreaGate => IsBeforeTestingAreaGate(placement),
            ExpectedScope.BeforeLucasPuzzle => IsBeforeTestingAreaGate(placement)
                                               || IsTestingAreaBeforeLucasPuzzle(placement.SceneFile)
                                               || IsTestingAreaBeforeBarnFight(placement.SceneFile),
            ExpectedScope.BeforeBoatHouse => IsMainHouseBeforeGarage(placement)
                                             || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
                                             || IsMainHouseEastOrBasement(placement.SceneFile)
                                             || IsYardOrTrailer(placement.SceneFile)
                                             || IsOldHouseBeforeCrowDoor(placement.SceneFile)
                                             || IsOldHouseStoneStatuettePickup(placement)
                                             || IsOldHouseCrowKeyPickup(placement)
                                             || IsOldHouseAfterStonePuzzleBeforeCrank(placement.SceneFile)
                                             || IsOldHouseAfterCrankBeforeCrowDoor(placement.SceneFile)
                                             || IsOldHouseAfterCrowDoorOrGreenHouse(placement.SceneFile)
                                             || IsSnakeKeyBody(placement.SceneFile)
                                             || IsMainHouseSnakeKeyRoom(placement.SceneFile)
                                             || IsTestingArea(placement.SceneFile)
                                             || IsTestingAreaBeforeBarnFight(placement.SceneFile),
            ExpectedScope.BeforeJack3 => IsTestingAreaAfterLucasPuzzle(placement.SceneFile)
                                         || IsBoatHouseRoute(placement.SceneFile),
            ExpectedScope.BoatHouse => IsBoatHouseRoute(placement.SceneFile),
            ExpectedScope.MiaPresentShip => IsMiaPresentShipRoute(placement.SceneFile),
            ExpectedScope.BeforeLugWrench => IsShipBeforeLugWrench(placement.SceneFile),
            ExpectedScope.AfterCorrosiveBeforeRepair =>
                IsShipAfterCorrosiveBeforeRepair(placement.SceneFile),
            ExpectedScope.BeforeNecrotoxinUse => IsSaltMineBeforeNecrotoxinUse(placement.SceneFile),
            _ => true,
        };

    private static bool PathContains(string path, string value)
        => path.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool IsGuestHouseBeforeBoltCutters(string path)
        => !IsFlashbackPath(path)
           && (PathContains(path, "c01_kitchen")
               || PathContains(path, "c01_living")
               || PathContains(path, "c01_corridor01")
               || PathContains(path, "c01_corridor02")
               || PathContains(path, "c01_b1a")
               || PathContains(path, "c01_b1b")
               || PathContains(path, "c01_b1c")
               || PathContains(path, "c01_b1d")
               || PathContains(path, "c01_b1e"));

    private static bool IsGuestHouseAfterBoltCutters(string path)
        => !IsFlashbackPath(path)
           && (PathContains(path, "c01_b1g")
               || PathContains(path, "c01_b1h")
               || PathContains(path, "c01_b1i")
               || PathContains(path, "c01_b1j")
               || PathContains(path, "c01_corridor03")
               || PathContains(path, "c01_storeroom"));

    private static bool IsGuestHouseAfterAxeFight(string path)
        => !IsFlashbackPath(path)
           && (PathContains(path, "c01_bathroom")
               || PathContains(path, "c01_2f")
               || PathContains(path, "c01_3f")
               || PathContains(path, "/leveldesign/itemset/chapter1/"));

    private static bool IsFlashbackPath(string path)
        => PathContains(path, "/environment/scene/ff")
           || PathContains(path, "/leveldesign/itemset/ff")
           || PathContains(path, "/scenes/chapter/ff")
           || PathContains(path, "past.scn");

    private static bool IsMainHouseBeforeGarage(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/mainhouse_west/")
           || PathContains(path, "c03_mainhouse1fgaragehallway")
           || PathContains(path, "c03_mainhouse1fhallway")
           || PathContains(path, "c03_mainhouse1fldk")
           || PathContains(path, "c03_mainhouse1fliving")
           || PathContains(path, "c03_mainhouse1fpantry")
           || PathContains(path, "c03_mainhouse1fwash");

    private static bool IsMainHouseBeforeGarage(ItemPlacement placement)
        => IsMainHouseBeforeGarage(placement.SceneFile)
           && !IsMainHouseWestBlueDogHead(placement)
           && !IsMainHouseWestBlueKeycard(placement);

    private static bool IsGarage(string path)
        => PathContains(path, "c03_mainhouse1fgarage.scn");

    private static bool IsMainHouseBeforeShadowPuzzle(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/mainhouse_hall/")
           || PathContains(path, "c03_mainhouse2fbath")
           || PathContains(path, "c03_mainhouse2fgrandma")
           || PathContains(path, "c03_mainhouse2fhallway")
           || PathContains(path, "c03_mainhouse2fplay")
           || PathContains(path, "c03_mainhouse2fstoreroom")
           || PathContains(path, "c03_mainhousehall")
           || PathContains(path, "c03_mainhousestair01");

    private static bool IsMainHouseAfterGarageBeforeShadowPuzzle(ItemPlacement placement)
        => IsMainHouseBeforeShadowPuzzle(placement.SceneFile)
           || IsMainHouseWestBlueDogHead(placement);

    private static bool IsMainHouseWestBlueDogHead(ItemPlacement placement)
        => placement.Guid == MainHouseWestBlueDogHeadGuid
           && placement.SceneFile.Equals(MainHouseWestItemSetScenePath, StringComparison.OrdinalIgnoreCase);

    private static bool IsMainHouseWestBlueKeycard(ItemPlacement placement)
        => placement.Guid == MainHouseWestBlueKeycardGuid
           && placement.SceneFile.Equals(MainHouseWestItemSetScenePath, StringComparison.OrdinalIgnoreCase);

    private static bool IsMainHouseEastOrBasement(string path)
        => !PathContains(path, "c03_rightareab1fstoreroom")
           && (PathContains(path, "/leveldesign/itemset/chapter3/mainhouse_east/")
               || PathContains(path, "c03_rightarea"));

    private static bool IsSnakeKeyBody(string path)
        => PathContains(path, "c03_rightareab1fstoreroom");

    private static bool IsMainHouseSnakeKeyRoom(string path)
        => PathContains(path, "c03_mainhouse2fbedroom")
           || PathContains(path, "c03_mainhouse2fkids")
           || PathContains(path, "c03_mainhousoutsideterrace2f3");

    private static bool IsMainHouseKeycardSetup(ItemPlacement placement)
        => placement.Guid == RedKeycardWorkshopGuid
           || IsMainHouseWestBlueKeycard(placement)
           || IsMainHouseSnakeKeyRoom(placement.SceneFile);

    private static bool IsBeforeTestingAreaGate(ItemPlacement placement)
        => IsMainHouseKeycardSetup(placement)
           || IsOldHouseAfterCrowDoorOrGreenHouse(placement.SceneFile)
           || IsOldHouseAfterLanternDoor(placement.SceneFile)
           || IsSnakeKeyBody(placement.SceneFile);

    private static bool IsBeforeSnakeRooms(ItemPlacement placement)
        => IsMainHouseBeforeGarage(placement)
           || IsMainHouseAfterGarageBeforeShadowPuzzle(placement)
           || IsMainHouseEastOrBasement(placement.SceneFile)
           || IsYardOrTrailer(placement.SceneFile)
           || IsOldHouseBeforeCrowDoor(placement.SceneFile)
           || IsOldHouseStoneStatuettePickup(placement)
           || IsOldHouseCrowKeyPickup(placement)
           || IsOldHouseAfterStonePuzzleBeforeCrank(placement.SceneFile)
           || IsOldHouseAfterCrankBeforeCrowDoor(placement.SceneFile)
           || IsOldHouseAfterCrowDoorOrGreenHouse(placement.SceneFile)
           || IsOldHouseAfterLanternDoor(placement.SceneFile)
           || IsSnakeKeyBody(placement.SceneFile);

    private static bool IsYardOrTrailer(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/gardenarea/")
           || PathContains(path, "c03_gardenarea")
           || PathContains(path, "c03_trailerhouse")
           || PathContains(path, "c03_mainhousoutside")
           || PathContains(path, "c03_mainhousoutsideterrace");

    private static bool IsOldHouseBeforeCrowDoor(string path)
        => PathContains(path, "c03_oldhouse1fbridge01")
           || PathContains(path, "c03_oldhouse1fentrance")
           || PathContains(path, "c03_oldhouse1fhallway")
           || PathContains(path, "c03_oldhouse1fhollway")
           || PathContains(path, "c03_oldhouse1fkitchen")
           || PathContains(path, "c03_oldhouse1fpuzzle")
           || PathContains(path, "c03_oldhouse1froom")
           || PathContains(path, "c03_oldhouse1fstorage")
           || PathContains(path, "c03_oldhouseoutside")
           || PathContains(path, "c03_oldhousesaferoom");

    private static bool IsOldHouseStoneStatuettePickup(ItemPlacement placement)
        => placement.SceneFile.Equals(OldHouseItemSetScenePath, StringComparison.OrdinalIgnoreCase)
           && placement.Guid == OldHouseStoneStatuetteGuid;

    private static bool IsOldHouseCrowKeyPickup(ItemPlacement placement)
        => placement.SceneFile.Equals(OldHouseItemSetScenePath, StringComparison.OrdinalIgnoreCase)
           && placement.Guid == OldHouseCrowKeyGuid;

    private static bool IsOldHouseAfterStonePuzzleBeforeCrank(string path)
        => PathContains(path, "c03_oldhouse1fhole")
           || PathContains(path, "c03_oldhouse1funderfloor")
           || PathContains(path, "c03_oldhouse1fwallinside");

    private static bool IsOldHouseAfterCrankBeforeCrowDoor(string path)
        => PathContains(path, "c03_oldhouse1fbridge02")
           || PathContains(path, "c03_oldhouse1fbridgestorag")
           || PathContains(path, "c03_oldhouse1fbridgewc");

    private static bool IsOldHouseAfterCrowDoorOrGreenHouse(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/greenhouse/")
           || PathContains(path, "c03_oldhouse1fstairs")
           || PathContains(path, "c03_oldhouse2f")
           || PathContains(path, "c03_oldhousecave")
           || PathContains(path, "c03_gh");

    private static bool IsOldHouseAfterLanternDoor(string path)
        => PathContains(path, "c03_oldhouse1fstairs")
           || PathContains(path, "c03_oldhouse1faltar")
           || PathContains(path, "c03_oldhouse2fbedroom")
           || PathContains(path, "c03_oldhouse2fhallway04")
           || PathContains(path, "c03_oldhouse2fkidsroom")
           || PathContains(path, "c03_oldhouse2fstudy");

    private static bool IsTestingAreaBeforeBarnFight(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/cowshed/")
           || PathContains(path, "c03_cowshed");

    private static bool IsTestingAreaBeforeLucasPuzzle(string path)
        => IsTestingArea(path) && !IsTestingAreaAfterLucasPuzzle(path);

    private static bool IsTestingAreaAfterLucasPuzzle(string path)
        => PathContains(path, "c03_leftarea1fmonitorroom")
           || PathContains(path, "c03_leftarea1fpuzzleroom");

    private static bool IsTestingArea(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/leftarea/")
           || PathContains(path, "c03_leftarea");

    private static bool IsBoatHouseRoute(string path)
        => !IsBoatHouseAfterSerumUse(path)
           && (PathContains(path, "/leveldesign/itemset/chapter3/boatshed/")
               || PathContains(path, "c03_boat")
               || PathContains(path, "c03_gardenareaboat"));

    private static bool IsBoatHouseAfterSerumUse(string path)
        => PathContains(path, "c03_boat1fbridge02");

    private static bool IsMiaPresentShipRoute(string path)
        => IsShipBeforeLugWrench(path)
           || IsShipAfterLugWrenchBeforeCorrosive(path)
           || IsShipAfterCorrosiveBeforeRepair(path);

    private static bool IsShipBeforeLugWrench(string path)
        => !PathContains(path, "past")
           && (PathContains(path, "c04_ship4f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship4f/")
               || PathContains(path, "c04_ship2f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship2f/"));

    private static bool IsShipAfterLugWrenchBeforeCorrosive(string path)
        => !PathContains(path, "past")
           && !IsShipAfterCorrosiveBeforeRepair(path)
           && (PathContains(path, "c04_ship1f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship1f/")
               || PathContains(path, "c04_ship3f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship3f/")
               || PathContains(path, "/scenes/chapter/chapter4/c04_shipelevator"));

    private static bool IsShipAfterCorrosiveBeforeRepair(string path)
        => !PathContains(path, "past")
           && (PathContains(path, "c04_ship3finfirmary")
               || PathContains(path, "c04_ship3fsecurityroom")
               || PathContains(path, "c04_ship3fshowerroom"));

    private static bool IsSaltMineBeforeNecrotoxinUse(string path)
        => !PathContains(path, "/chapter4/lastbattle/")
           && (PathContains(path, "/environment/scene/chapter4/c04_cottage")
               || PathContains(path, "/environment/scene/chapter4/c04_cave")
               || PathContains(path, "/leveldesign/itemset/chapter4/saltdome"));

    private enum ExpectedScope {
        Any,
        GuestHouseBeforeBoltCutters,
        GuestHouseBeforeAxeFight,
        GuestHouseAfterAxeFight,
        Chapter3Start,
        BeforeOxDoor,
        AfterGarageBeforeShadowPuzzle,
        BeforeShadowPuzzle,
        BeforeDogDoor,
        BeforeScorpionDoor,
        BeforeCrowDoor,
        AfterStonePuzzleBeforeCrank,
        AfterCrankBeforeCrowDoor,
        AfterLanternBeforeSnakeRooms,
        BeforeDissectionRoom,
        BeforeOldHouseShadowPuzzle,
        OldHouseAfterCrowDoor,
        BeforeSnakeRooms,
        BeforeTestingAreaGate,
        BeforeLucasPuzzle,
        BeforeBarnBatterySocket,
        BeforeBoatHouse,
        BeforeJack3,
        BoatHouse,
        MiaPresentShip,
        BeforeLugWrench,
        AfterCorrosiveBeforeRepair,
        BeforeNecrotoxinUse,
    }

    private sealed record ExpectedKeyItemRule(int Chapter, ExpectedScope Scope);

    private sealed record ExpectedPickupFlag(string Name, Guid Guid, bool Value);

    private sealed record ChangedItemPlacement(ItemPlacement Placement, string BeforeId, string AfterId);
}
