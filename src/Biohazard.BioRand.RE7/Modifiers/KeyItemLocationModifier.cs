using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Services;
using Enums.app.Item;
using IntelOrca.Biohazard.BioRand.Routing;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class KeyItemLocationModifier : Modifier {
    private readonly Randomizer _randomizer;

    public KeyItemLocationModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/key-item-locations";
    private const string TemplateInstanceKey = $"{RandomizerKey}/template-instances";
    private const string ExtraKeyItemCarrierTemplateId = "HandgunBullet";

    internal const string OldHouseLevelFsmScenePath =
        "natives/stm/leveldesign/fsm/chapter3/chapter3_3/levelfsm_c03_3.scn.20";

    internal const string OldHouseShadowPuzzleProgressionTriggerName =
        "150_Main_EnterMiaCapturedRoom_ShadowPuzzleFallback";

    private const int MaxRouteSeedAttempts = 64;
    private const int MaxRouteDeadEndsPerAttempt = 1024;
    private const int MaxPreferredRuleSubsetAttempts = 8192;
    private const int RouteDepthPadding = 8;
    private static readonly Guid _guestHouseFuseCabinetGuid = new("b116eb16-c4c5-4d43-8901-044ec9dccbcf");

    private static readonly Guid _oldHouseEnterMiaCapturedRoomTriggerGuid =
        new("83eb1968-eba3-4925-8e1e-ddac00495a92");

    private static readonly Guid _oldHousePassThroughFireplaceFlagGuid =
        new("779d13a7-5296-4a4a-bf04-c92e8930cc90");

    private static readonly Vector3 _oldHouseShadowPuzzleProgressionTriggerPosition =
        new(-19.85224f, -2.963f, 92.8632f);

    private static readonly Vector3 _oldHouseShadowPuzzleProgressionTriggerScale =
        new(5f, 2.5f, 6f);

    private const uint OldHouseShadowPuzzleProgressionPassThroughFireplaceActionUid = 0xB107A200;

    private static readonly HashSet<string> _preservedVanillaKeyItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "ChainCutter",
        "EntranceHallKey",
        "HandAxe",
        "Fuse",
        "FuseCh4",
        "3CrestKeyC",
        "Lantern",
        "LucasCardKey",
        "LucasCardKey2",
        "SerumComplete",
        "Candle_Lighted",
        // These rewards advance scripted story branches, including regional body
        // variants and the mine laboratory sequence. Keep their native events intact.
        "MasterKey",
        "SerumMaterialA",
        "SerumMaterialB",
        "SerumTypeE",
    };

    private static readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;
    private static readonly AreaDefinitionRepository _areaDefinitions = AreaDefinitionRepository.Default;
    private static readonly HashSet<Guid> _birdCageGuids = [.. BirdCageModifier.Guids];

    private static readonly IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>>
        _levelFsmAcquisitionFlagsByItemId =
            new Dictionary<string, ImmutableArray<KeyItemAcquisitionFlag>>(StringComparer.OrdinalIgnoreCase){
                ["ChainCutter"] = Flags(new KeyItemAcquisitionFlag("PL_ChainCutterGet",
                    new("889fd052-4339-4fb4-920c-a6ac99eb6fd7"), true)),
                ["Fuse"] = Flags(new KeyItemAcquisitionFlag("c01_Main_FuseGet",
                    new("f7486e9b-8924-494c-9738-c26fa3c2e055"), true)),
                ["FloorDoorKey"] = Flags(new KeyItemAcquisitionFlag("c03_1_Main_GetFloorDoorKey",
                    new("024d7582-3a98-4587-9b4f-a4dc47cd2cb4"), true)),
                ["3CrestKeyC"] = Flags(new KeyItemAcquisitionFlag("c03_2_Main_GetCrestInFreezerRoom",
                    new("ed2860cf-2569-4045-96c8-ba01e0fcfed8"), true)),
                ["MasterKey"] = Flags(new KeyItemAcquisitionFlag("c03_4_Main_SnakeKeyGet",
                    new("3ceeead9-da86-48c2-89ed-73edeab7fdbf"), true)),
                ["Crank"] = Flags(new KeyItemAcquisitionFlag("c03_3_Main_GetCrank",
                    new("e4ef4f89-4d98-4d81-86a0-8ea640eac4dc"), true)),
                ["TalismanKey"] = Flags(new KeyItemAcquisitionFlag("c03_3_Main_TalismanKeyGet",
                    new("6ed99e11-2047-4236-84a0-6457c7a3b1c9"), true)),
                ["SilhouettePazzlePiece"] = Flags(new KeyItemAcquisitionFlag("c03_2_Main_GetPazzleObject",
                    new("e165ff7a-0829-4edc-8c34-68a01a1ff3b2"), true)),
                ["SilhouettePazzlePieceOldHouse"] = Flags(new KeyItemAcquisitionFlag("c03_3_Main_EnterMiaCapturedRoom",
                    new("17e5af29-0cab-4e3c-a78d-71ee87798b6c"), true,
                    KeyItemAcquisitionFlagSource.NativeTriggerOnly)),
                ["SerumMaterialA"] = Flags(new KeyItemAcquisitionFlag("c03_3_Main_GetEvlineArm",
                    new("e4b4b42e-ecfc-415e-a713-e1a3604af371"), true)),
                ["Lantern"] = Flags(new KeyItemAcquisitionFlag("c03_3_Main_GetLantern",
                    new("acb7e0bd-e123-4a57-8a0b-1bf77087e856"), true)),
                ["LucasCardKey"] = Flags(new KeyItemAcquisitionFlag("c03_4A_Main_LucasCardKeyGet_InLoft",
                    new("b5532ea8-facb-428d-bf61-ea3e66d373dd"), true)),
                ["LucasCardKey2"] = Flags(new KeyItemAcquisitionFlag("c03_4B_Main_LucasCardKeyGet_InWorkRoom",
                    new("b9f9b409-c6b8-4142-9697-c70f24a7c15b"), true)),
                ["SerumMaterialB"] = Flags(new KeyItemAcquisitionFlag("c03_objective_EvlineFace_Get",
                    new("21411c8c-2b95-418e-8efa-8bf79bae4ae5"), true)),
                ["Candle_Lighted"] = Flags(new KeyItemAcquisitionFlag("c03_4_Main_PazzleRoom_CandleOn",
                    new("e8f18a82-943a-41dd-977b-f1d729499dee"), true)),
                ["SerumComplete"] = Flags(new KeyItemAcquisitionFlag("c03_5_Main_GetKesseiEventEnd",
                    new("2f3f5ec1-c595-4078-a686-118a5a8d1a8f"), true)),
                ["EvCable"] = Flags(new KeyItemAcquisitionFlag("c04_objective_ElevatorCableGetInventory",
                    new("8dc1c235-4ffc-4894-bd45-ae1cf2e5fba2"), true)),
                ["FuseCh4"] = Flags(new KeyItemAcquisitionFlag("c04_objective_ElevatorFuseGetInventory",
                    new("c7004b40-85bc-4d0a-a274-05d771d581ab"), true)),
                ["SerumTypeE"] = Flags(new KeyItemAcquisitionFlag("c04_objective_EnecrotoxinGet",
                    new("a787b236-b098-4b69-93b2-6df65c97bbe6"), true)),
            };

    private const int WhiteDogHeadMask = 1 << 0;
    private const int BlueDogHeadMask = 1 << 1;
    private const int BatteryMask = 1 << 2;
    private const int ScorpionKeyMask = 1 << 3;
    private const int SnakeKeyMask = 1 << 4;
    private const int CrowKeyMask = 1 << 5;
    private const int PowerCableMask = 1 << 6;
    private const int ShipFuseMask = 1 << 7;
    private const int LugWrenchMask = 1 << 8;
    private const int CorrosiveMask = 1 << 9;
    private const int NecrotoxinMask = 1 << 10;
    private const int CarKeyMask = 1 << 11;
    private const int WoodenStatuetteMask = 1 << 12;
    private const int BoltCuttersMask = 1 << 13;
    private const int GuestFuseMask = 1 << 14;
    private const int AxeMask = 1 << 15;
    private const int FloorDoorKeyMask = 1 << 16;
    private const int OxStatuetteMask = 1 << 17;
    private const int PendulumMask = 1 << 18;
    private const int RedDogHeadMask = 1 << 19;
    private const int DissectionRoomKeyMask = 1 << 20;
    private const int CrankMask = 1 << 21;
    private const int StoneStatuetteMask = 1 << 22;
    private const int DSeriesArmMask = 1 << 23;
    private const int LanternMask = 1 << 24;
    private const int BlueKeycardMask = 1 << 25;
    private const int RedKeycardMask = 1 << 26;
    private const int DSeriesHeadMask = 1 << 27;
    private const int SerumMask = 1 << 28;
    private const int CandleMask = 1 << 29;
    private const int DogHeadMasks = WhiteDogHeadMask | BlueDogHeadMask;
    private const int AllDogHeadMasks = DogHeadMasks | RedDogHeadMask;
    private const int MainHouseCarryMasks = DogHeadMasks | BatteryMask;

    private const int MainHouseBeforeHatchCarryMasks =
        MainHouseCarryMasks | ScorpionKeyMask | CarKeyMask | WoodenStatuetteMask | FloorDoorKeyMask |
        OxStatuetteMask | RedDogHeadMask |
        StoneStatuetteMask | DSeriesHeadMask;

    private const int MainHouseAfterGarageCarryMasks =
        (MainHouseBeforeHatchCarryMasks | PendulumMask) &
        ~FloorDoorKeyMask & ~CarKeyMask & ~OxStatuetteMask;

    private const int MainHouseEastCarryMasks =
        MainHouseCarryMasks | RedDogHeadMask | DissectionRoomKeyMask |
        StoneStatuetteMask | DSeriesHeadMask;

    private const int DissectionRoomCarryMasks =
        AllDogHeadMasks | BatteryMask | StoneStatuetteMask | DSeriesHeadMask;

    private const int OldHouseBeforeCrowCarryMasks =
        StoneStatuetteMask | BatteryMask | DSeriesHeadMask;

    private const int OldHouseAfterStoneCarryMasks =
        CrankMask | BatteryMask | DSeriesHeadMask;

    private const int OldHouseAfterCrankCarryMasks =
        CrowKeyMask | BatteryMask | DSeriesHeadMask;

    private const int OldHouseAfterCrowCarryMasks =
        LanternMask | BatteryMask | DSeriesHeadMask;

    private const int OldHouseAfterLanternCarryMasks =
        DSeriesArmMask | SnakeKeyMask | BatteryMask | DSeriesHeadMask;

    private const int SnakeKeyRewardCarryMasks =
        SnakeKeyMask | BatteryMask | DSeriesHeadMask;

    private const int KeycardSetupCarryMasks =
        BatteryMask | DSeriesHeadMask | BlueKeycardMask | RedKeycardMask | CandleMask;

    private const int LucasBeforePuzzleCarryMasks = BatteryMask | DSeriesHeadMask | CandleMask;
    private const int LucasAfterPuzzleCarryMasks = DSeriesHeadMask;
    private const int ShipBeforeWrenchMasks = ShipFuseMask | LugWrenchMask | CorrosiveMask;
    private const int ShipAfterWrenchMasks = ShipFuseMask | CorrosiveMask;
    private const int ShipAfterCorrosiveMasks = PowerCableMask | ShipFuseMask;
    // These rewards have photo/FSM prerequisites that are not represented by their room.
    // Never use them as progression carriers, even after ordinary item randomization.
    private static readonly HashSet<Guid> _treasureRewardGuids = [
        new("b1548f47-609a-0190-3976-50b2aeafd6b6"), // Fireplace
        new("abb03a3a-a10b-4dca-9385-6274ca2e004a"), // Trailer toilet
        new("5e6490f0-17f6-0ba5-1fdb-5d70e2ac3b2b"), // Testing Area mannequin
    ];
    private static readonly Guid _mainHouseWestBlueDogHeadGuid = new("401dbfaa-3469-0702-1c9a-d74a7d185216");
    private static readonly Guid _mainHouseWestBlueKeycardGuid = new("896dd0bb-f3ee-41bf-b4a0-0b28e99da94c");
    private static readonly Guid _mainHouseClockRewardGuid = new("0da28012-ad6a-0da5-1f0a-cacd2c677ed3");
    private static readonly Guid _jack2RedDogHeadGuid = new("301caf06-67b8-0645-11a1-faadce741e7d");
    private static readonly Guid _redKeycardWorkshopGuid = new("077f9206-19e7-4937-994b-cd13a80dabd4");
    private static readonly Guid _blueKeycardAtticGuid = new("ccf47d14-a937-43c4-9b87-f35b07d14034");
    private static readonly Guid _oldHouseStoneStatuetteGuid = new("41a59cb8-7613-4d4b-a530-58aebfe0e1c8");
    private static readonly Guid _oldHouseCrowKeyGuid = new("8b940901-8893-4091-a4ac-5a16b3de3a11");
    private static readonly Guid _lucasPuzzleCandleGuid = new("05606c7e-3669-497e-8196-561faefb95e5");

    private static readonly HashSet<Guid> _snakeKeyRewardGuids =[
        new("96da0bd0-1a8b-4c35-bc02-695da693e8d4"),
        new("24512acb-965b-462c-941e-375f9d62bd5e"),
        new("751cff95-a933-48ad-8ffa-6f96e25f8959"),
    ];

    private static readonly ImmutableArray<KeyItemRule> _supportedKeyItems =[
        new("ChainCutter", 1, BoltCuttersMask), // Bolt Cutters
        new("Fuse", 1, GuestFuseMask), // Fuse
        new("HandAxe", 1, AxeMask, Priority: 20), // Axe
        new("FloorDoorKey", 3, FloorDoorKeyMask), // Hatch Key
        new("3CrestKeyB", 3, WhiteDogHeadMask, Priority: 90), // White Dog's Head
        new("3CrestKeyA", 3, BlueDogHeadMask, Priority: 110), // Blue Dog's Head
        new("3CrestKeyC", 3, RedDogHeadMask), // Red Dog's Head
        new("Battery", 3, BatteryMask),
        new("EntranceHallKey", 3, OxStatuetteMask), // Ox Statuette
        new("PendulumClock", 3, PendulumMask), // Clock Pendulum
        new("MorgueKey", 3, ScorpionKeyMask, Priority: 130), // Scorpion Key
        new("WorkroomKey", 3, DissectionRoomKeyMask,
            EarliestSafePhase: KeyItemRoutePhase.MainHouseEast), // Dissection Room Key
        new("MasterKey", 3, SnakeKeyMask,
            EarliestSafePhase: KeyItemRoutePhase.OldHouseAfterLantern), // Snake Key
        new("TalismanKey", 3, CrowKeyMask,
            EarliestSafePhase: KeyItemRoutePhase.OldHouseAfterCrank), // Crow Key
        new("Crank", 3, CrankMask,
            EarliestSafePhase: KeyItemRoutePhase.OldHouseAfterStonePuzzle),
        new("SilhouettePazzlePieceOldHouse", 3, StoneStatuetteMask), // Stone Statuette
        new("SerumMaterialA", 3, DSeriesArmMask,
            EarliestSafePhase: KeyItemRoutePhase.OldHouseAfterLantern), // D-Series Arm
        new("Lantern", 3, LanternMask),
        new("LucasCardKey", 3, BlueKeycardMask), // Blue Keycard
        new("LucasCardKey2", 3, RedKeycardMask), // Red Keycard
        // Its acquisition flag completes the Lucas/keycard objective branches.
        // Do not let an early pickup advance them before the puzzle-door event.
        new("SerumMaterialB", 3, DSeriesHeadMask,
            EarliestSafePhase: KeyItemRoutePhase.LucasPuzzle), // D-Series Head
        new("SerumComplete", 3, SerumMask, Count: 2), // Serum
        new("Candle_Lighted", 3, CandleMask, Priority: 20), // Candle
        new("EvCable", 4, PowerCableMask,
            EarliestSafePhase: KeyItemRoutePhase.ShipAfterCorrosive), // Power Cable
        new("FuseCh4", 4, ShipFuseMask), // General Purpose Fuse
        new("EvOpener", 4, LugWrenchMask), // Lug Wrench
        // Cover all five present-day locks, so spending on optional rooms/cabinets
        // cannot exhaust the only randomized supply. VHS supplies remain separate.
        new("SpareKey", 4, CorrosiveMask, Count: 5), // Corrosive
        new("SerumTypeE", 4, NecrotoxinMask), // E-Necrotoxin
        new("EthanCarKey", 3, CarKeyMask, Priority: 80), // Car Key
        new("SilhouettePazzlePiece", 3, WoodenStatuetteMask, Priority: 80), // Wooden Statuette
    ];

    public override void LogState(RandomizerLogger logger) {
        var randomizer = _randomizer;
        foreach (var rule in _supportedKeyItems) {
            var item = _itemDefinitions.FromId(rule.Id)!;
            var placements = randomizer.ItemPlacementService.FromId(rule.Id);
            foreach (var placement in placements.Where(x => x.Enabled && !x.IsExtra && x.Dlc == null)) {
                logger.LogLine(
                    $"{item.Name} in {FormatScenePath(placement.SceneFile)}, X={placement.Position.X}, Y={placement.Position.Y}, Z={placement.Position.Z}");
                logger.LogLine($"GUID: {placement.Guid}");
            }
        }
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        if (!randomizer.GetConfigOption<bool>("random-key-item-locations"))
            return;

        var rng = randomizer.GetRng(RandomizerKey);
        var itemPlacementService = randomizer.ItemPlacementService;
        var itemRandomizer = randomizer.ItemRandomizer;
        var randomItemSettings = randomizer.StaticItemRandomizationService.RandomItemSettings;
        var preserveItemModels = randomizer.GetConfigOption<bool>("preserve-item-models");
        AddOldHouseShadowPuzzleProgressionTrigger(randomizer, logger);
        var availableTargets = GetEligibleTargetPlacements(randomizer, itemPlacementService)
            .OrderBy(target => target.Placement.SceneFile, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => target.LocationKey.X)
            .ThenBy(target => target.LocationKey.Y)
            .ThenBy(target => target.LocationKey.Z)
            .ThenBy(target => target.TargetGuid.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .DistinctBy(target => target.LocationKey)
            .ToList();
        var replacementPlanSet = CreateKeyItemReplacementPlans(logger, rng, availableTargets);
        if (replacementPlanSet == null)
            return;
        KeyItemSupplyLocations.RemoveOverlappingSupplies(randomizer,
            replacementPlanSet.Assignments.Select(assignment => assignment.Target.TargetGuid));
        AddKeyItemHintsOutput(randomizer, replacementPlanSet);
        var replacementPlans = replacementPlanSet.Plans;
        var acquisitionFlagsByItemId = GetAcquisitionFlagsByItemId(
            randomizer,
            itemPlacementService,
            replacementPlanSet.ActiveRules,
            logger);

        foreach (var placement in GetOriginalSupportedKeyItemPlacements(itemPlacementService,
                     replacementPlanSet.ActiveRules)) {
            var key = new ReplacementKey(placement.SceneFile, placement.Guid);
            if (replacementPlans.ContainsKey(key))
                continue;

            replacementPlans[key] = ReplacementPlan.Filler(
                placement,
                placement.Guid,
                itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings));
        }

        foreach (var sceneGroup in replacementPlans.Values.GroupBy(plan => plan.Placement.SceneFile,
                     StringComparer.OrdinalIgnoreCase)) {
            logger.Push(FormatScenePath(sceneGroup.Key));
            randomizer.FileRepository.ModifyScnFile(sceneGroup.Key, scene => {
                var plans = sceneGroup.ToList();
                var targetGuids = plans
                    .Select(plan => plan.TargetGuid)
                    .ToHashSet();
                var originalGameObjects = scene.FindGameObjectsByGuidWithFsmContext(targetGuids);
                var replacementGameObjects = new Dictionary<Guid, RszGameObject>();
                var extraParentGameObject = scene.FindGameObject(gameObject =>
                    gameObject.Name.EndsWith("_dynamic", StringComparison.Ordinal));
                var extraParentChanged = false;

                foreach (var plan in plans) {
                    if (!originalGameObjects.TryGetValue(plan.TargetGuid, out var originalMatch)) {
                        if (plan.Placement.IsExtra &&
                            ExtraPlacementModifier.IsPlainExtraItemPlacement(plan.Placement) &&
                            extraParentGameObject != null) {
                            var extraKeyItem = CreateExtraKeyItemGameObject(
                                randomizer,
                                logger,
                                rng,
                                randomItemSettings,
                                plan,
                                acquisitionFlagsByItemId);
                            extraParentGameObject = extraParentGameObject.AddOrUpdateChild(extraKeyItem);
                            extraParentChanged = true;
                            continue;
                        }

                        if (plan.Kind == ReplacementKind.KeyItem) {
                            throw new InvalidOperationException(
                                $"Cannot place progression item {plan.Drop.Id}: pickup {plan.TargetGuid} " +
                                $"and its extra-item parent are missing from {plan.Placement.SceneFile}.");
                        }
                        logger.LogLine(
                            $"Skipped replacing {plan.Placement.Id} in {FormatScenePath(plan.Placement.SceneFile)}: GameObject {plan.TargetGuid} was not found.");
                        continue;
                    }

                    var replacement = CreateReplacementGameObject(
                        randomizer,
                        logger,
                        rng,
                        randomItemSettings,
                        plan,
                        originalMatch.GameObject,
                        preserveItemModels,
                        GetPreserveObjectShapeReason(plan.Placement, originalMatch),
                        acquisitionFlagsByItemId);

                    replacementGameObjects[plan.TargetGuid] = replacement;
                }

                if (extraParentChanged) {
                    scene = scene.UpdateGameObject(extraParentGameObject!);
                }

                return ReplaceGameObjects(scene, replacementGameObjects);
            });
            logger.Pop();
        }
    }

    private static void AddOldHouseShadowPuzzleProgressionTrigger(Randomizer randomizer, RandomizerLogger logger) {
        var added = false;
        var sourceMissing = false;
        randomizer.FileRepository.ModifyScnFile(OldHouseLevelFsmScenePath, scene => {
            if (scene.FindGameObject(OldHouseShadowPuzzleProgressionTriggerName) != null)
                return scene;

            var sourceTrigger = scene.FindGameObject(_oldHouseEnterMiaCapturedRoomTriggerGuid);
            if (sourceTrigger == null) {
                sourceMissing = true;
                return scene;
            }

            var trigger = sourceTrigger
                .CloneWithNewGuids(randomizer.GetRng($"{RandomizerKey}/old-house-shadow-puzzle-progress-trigger"))
                .WithName(OldHouseShadowPuzzleProgressionTriggerName);
            trigger = MoveOldHouseShadowPuzzleProgressionTrigger(trigger);
            trigger = AddSetBoolAction(
                trigger,
                "c03_3_Main_PassThroughFirePlaceAgain",
                _oldHousePassThroughFireplaceFlagGuid,
                true,
                OldHouseShadowPuzzleProgressionPassThroughFireplaceActionUid);

            added = true;
            return scene.Add(randomizer.FileRepository.TypeRepository,
                "MainFlow_Advanced_gimmick/c03_3_Main_150/" + OldHouseShadowPuzzleProgressionTriggerName,
                trigger);
        });

        if (added) {
            logger.LogLine(
                $"Old House shadow puzzle progression fallback added near the puzzle path in {FormatScenePath(OldHouseLevelFsmScenePath)}.");
        } else if (sourceMissing) {
            logger.LogLine(
                $"Old House shadow puzzle progression fallback skipped: source trigger {_oldHouseEnterMiaCapturedRoomTriggerGuid} was not found.");
        }
    }

    private static RszGameObject MoveOldHouseShadowPuzzleProgressionTrigger(RszGameObject trigger) {
        var transform = trigger.FindComponent<GeneratedViaTransform>()
                        ?? throw new Exception($"{OldHouseShadowPuzzleProgressionTriggerName} has no transform.");
        transform.Position = _oldHouseShadowPuzzleProgressionTriggerPosition;
        transform.Rotation = Quaternion.Identity;
        transform.Scale = _oldHouseShadowPuzzleProgressionTriggerScale;
        return trigger.AddOrUpdateComponent(transform);
    }

    private static RszGameObject AddSetBoolAction(
        RszGameObject gameObject,
        string flagName,
        Guid flagGuid,
        bool value,
        uint uid) {
        var added = false;
        return gameObject.Visit(node => {
            if (added ||
                node is not RszObjectNode objectNode ||
                objectNode.Type.Name != "via.fsm.SceneFsmData" ||
                objectNode["v1_Actions"] is not RszArrayNode actions) {
                return node;
            }

            if (actions.Children
                .OfType<RszObjectNode>()
                .Any(action =>
                    action.Type.Name == "via.fsm.action.SetBool" &&
                    action.Get<Guid>("v5_Guid") == flagGuid)) {
                added = true;
                return node;
            }

            var sourceAction = actions.Children
                .OfType<RszObjectNode>()
                .FirstOrDefault(action => action.Type.Name == "via.fsm.action.SetBool");
            if (sourceAction == null)
                return node;

            var extraAction = sourceAction
                .SetField("v2_UID", uid)
                .SetField("v4_Variable", flagName)
                .SetField("v5_Guid", flagGuid)
                .SetField("v6_Status", value ? 1 : 0)
                .SetField("v7_ActionEnd", false);
            added = true;
            return objectNode.SetField("v1_Actions", actions.Add(extraAction));
        });
    }

    private static IEnumerable<ItemReplacementTarget> GetEligibleTargetPlacements(
        Randomizer randomizer,
        ItemPlacementService itemPlacementService) {
        var replaceMadhouseTapes = randomizer.GetConfigOption<bool>("replace-madhouse-tapes")
                                   || MadhouseSaveModifier.IsEnabled(randomizer);
        var eligibleScenePaths = AreaDefinitionRepository.Default.All
            .Where(area => area.Dlc == null)
            .Select(area => area.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var supportedIds = _supportedKeyItems
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var metadataEligibleTargets = new List<ItemReplacementTarget>();
        foreach (var placement in itemPlacementService.MainGamePlacements) {
            var isSupportedKeyPlacement = supportedIds.Contains(placement.Id);
            if (!eligibleScenePaths.Contains(placement.SceneFile)
                || !placement.Enabled
                || placement.Difficulty != null
                || placement.Tags.Contains(ItemPlacement.ExcludeTag)
                || _birdCageGuids.Contains(placement.Guid)
                || !KeyItemCarrierSafety.IsReviewed(placement)) {
                continue;
            }

            if (isSupportedKeyPlacement && _preservedVanillaKeyItemIds.Contains(placement.Id))
                continue;

            if (placement.IsExtra) {
                if (!ExtraPlacementModifier.IsPlainExtraItemPlacement(placement))
                    continue;

                var extraDefinition = string.IsNullOrWhiteSpace(placement.Id)
                    ? null
                    : _itemDefinitions.FromId(placement.Id);
                metadataEligibleTargets.Add(new ItemReplacementTarget(
                    placement,
                    extraDefinition,
                    ExtraPlacementModifier.GetGeneratedItemGuid(placement),
                    extraDefinition?.Name ?? "Extra item"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(placement.Id))
                continue;

            var definition = _itemDefinitions.FromId(placement.Id);
            if (definition == null
                || (!isSupportedKeyPlacement && !randomizer.ItemRandomizer.IsItemAllowed(definition))) {
                continue;
            }

            if (!replaceMadhouseTapes && definition.Id == "SaveTape")
                continue;

            if (!isSupportedKeyPlacement && !IsPlainRandomItemTarget(definition))
                continue;

            metadataEligibleTargets.Add(new ItemReplacementTarget(placement, definition, placement.Guid,
                definition.Name ?? definition.Id));
        }

        foreach (var target in ExcludeDrawerKeyItemTargets(randomizer, metadataEligibleTargets)) {
            yield return target;
        }
    }

    internal static bool IsPlainRandomItemTarget(ItemDefinition definition)
        => definition.CategoryType is ItemCategoryType.Shell
            or ItemCategoryType.Drug
            or ItemCategoryType.Material
            or ItemCategoryType.OtherItem;

    internal static bool CanPlaceKeyItemInPlacementForTesting(ItemPlacement placement, string keyItemId) {
        if (_preservedVanillaKeyItemIds.Contains(keyItemId))
            return false;

        if (!placement.IsExtra
            && !string.IsNullOrWhiteSpace(placement.Id)
            && _preservedVanillaKeyItemIds.Contains(placement.Id)) {
            return false;
        }

        var rule = _supportedKeyItems.Single(supported =>
            supported.Id.Equals(keyItemId, StringComparison.OrdinalIgnoreCase));
        var definition = string.IsNullOrWhiteSpace(placement.Id)
            ? null
            : _itemDefinitions.FromId(placement.Id);
        var target = new ItemReplacementTarget(
            placement,
            definition,
            placement.IsExtra ? ExtraPlacementModifier.GetGeneratedItemGuid(placement) : placement.Guid,
            definition?.Name ?? placement.Id ?? "Extra item");
        var routeTarget = new KeyItemRouteGraph(_supportedKeyItems).GetRouteTarget(target);
        return routeTarget != null && (routeTarget.GroupMask & rule.RouteMask) == rule.RouteMask;
    }

    internal static ImmutableArray<(Guid TargetGuid, string Description)> GetRouteTargetsForTesting(
        Randomizer randomizer) {
        return GetEligibleTargetPlacements(randomizer, randomizer.ItemPlacementService)
            .Where(target => _supportedKeyItems.Any(rule =>
                CanPlaceKeyItemInPlacementForTesting(target.Placement, rule.Id)))
            .Select(target => (
                target.TargetGuid,
                $"{FormatScenePath(target.Placement.SceneFile)} / {target.Label} / {target.Placement.Position}"))
            .DistinctBy(target => target.TargetGuid)
            .OrderBy(target => target.TargetGuid)
            .ToImmutableArray();
    }

    private static bool IsOriginalSupportedKeyItemPlacement(ItemPlacement placement)
        => !placement.IsExtra
           && !string.IsNullOrWhiteSpace(placement.Id)
           && _supportedKeyItems.Any(rule => rule.Id.Equals(placement.Id, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<ItemReplacementTarget> ExcludeDrawerKeyItemTargets(
        Randomizer randomizer,
        IEnumerable<ItemReplacementTarget> targets) {
        foreach (var sceneGroup in targets.GroupBy(target => target.Placement.SceneFile,
                     StringComparer.OrdinalIgnoreCase)) {
            var sceneTargets = sceneGroup.ToList();
            var scene = randomizer.FileRepository
                .GetScnFile(sceneGroup.Key)
                .ReadScene(randomizer.FileRepository.TypeRepository);
            var targetMatches = scene.FindGameObjectsByGuidWithFsmContext(
                sceneTargets.Select(target => target.TargetGuid).ToHashSet());
            var hasExtraParent = scene.FindGameObject(gameObject =>
                gameObject.Name.EndsWith("_dynamic", StringComparison.Ordinal)) != null;

            foreach (var target in sceneTargets) {
                if (!targetMatches.TryGetValue(target.TargetGuid, out var match)) {
                    if (hasExtraParent && target.Placement.IsExtra &&
                        ExtraPlacementModifier.IsPlainExtraItemPlacement(target.Placement)) {
                        yield return target;
                    }

                    continue;
                }

                if (match.HasDrawerContext || match.HasConditionalPickupContext)
                    continue;

                yield return target;
            }
        }
    }

    private static IEnumerable<ItemPlacement> GetOriginalSupportedKeyItemPlacements(
        ItemPlacementService itemPlacementService,
        IEnumerable<KeyItemRule> activeRules) {
        var supportedIds = activeRules
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return itemPlacementService.MainGamePlacements
            .Where(placement =>
                supportedIds.Contains(placement.Id) &&
                placement.Enabled &&
                !placement.Tags.Contains(ItemPlacement.ExcludeTag) &&
                !placement.IsExtra &&
                !IsFlashbackPath(placement.SceneFile))
            .DistinctBy(placement => new ReplacementKey(placement.SceneFile, placement.Guid));
    }

    private static IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>> GetAcquisitionFlagsByItemId(
        Randomizer randomizer,
        ItemPlacementService itemPlacementService,
        IEnumerable<KeyItemRule> activeRules,
        RandomizerLogger logger) {
        var supportedIds = activeRules
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, ImmutableArray<KeyItemAcquisitionFlag>>(StringComparer.OrdinalIgnoreCase);

        foreach (var placementGroup in itemPlacementService.MainGamePlacements
                     .Where(placement =>
                         supportedIds.Contains(placement.Id) &&
                         placement.Enabled &&
                         !placement.IsExtra &&
                         placement.Dlc == null &&
                         !IsFlashbackPath(placement.SceneFile))
                     .GroupBy(placement => placement.Id, StringComparer.OrdinalIgnoreCase)) {
            var flags = placementGroup
                .SelectMany(placement => GetPickupAcquisitionFlags(randomizer, placement))
                .Concat(GetLevelFsmAcquisitionFlags(placementGroup.Key))
                .Distinct()
                .ToImmutableArray();
            if (flags.Length == 0)
                continue;

            var triggerOnlyFlags = flags
                .Where(flag => flag.Source == KeyItemAcquisitionFlagSource.NativeTriggerOnly)
                .ToImmutableArray();
            if (!triggerOnlyFlags.IsDefaultOrEmpty) {
                logger.LogLine(
                    $"Skipped pickup side effects for {_itemDefinitions.GetName(placementGroup.Key)}: " +
                    $"{string.Join(", ", triggerOnlyFlags.Select(flag => flag.Name))} is driven by a native trigger and must not be faked on relocated pickups.");
                flags = flags
                    .Where(flag => flag.Source != KeyItemAcquisitionFlagSource.NativeTriggerOnly)
                    .ToImmutableArray();
                if (flags.Length == 0)
                    continue;
            }

            if (HasUnsafeRelocatedPickupSideEffect(placementGroup.Key, flags)) {
                logger.LogLine(
                    $"Skipped pickup side effects for {_itemDefinitions.GetName(placementGroup.Key)}: the vanilla workshop tray flag is tied to the morgue FSM and is unsafe when relocated.");
                continue;
            }

            if (flags.Length > 1) {
                logger.LogLine(
                    $"Skipped pickup side effects for {_itemDefinitions.GetName(placementGroup.Key)}: multiple distinct vanilla pickup flags were found.");
                continue;
            }

            result[placementGroup.Key] = flags;
        }

        return result;
    }

    private static ImmutableArray<KeyItemAcquisitionFlag> Flags(params KeyItemAcquisitionFlag[] flags)
        => [.. flags];

    private static IEnumerable<KeyItemAcquisitionFlag> GetLevelFsmAcquisitionFlags(string itemId)
        => _levelFsmAcquisitionFlagsByItemId.TryGetValue(itemId, out var flags)
            ? flags
            : [];

    private static bool HasUnsafeRelocatedPickupSideEffect(
        string itemId,
        ImmutableArray<KeyItemAcquisitionFlag> flags)
        => itemId.Equals("WorkroomKey", StringComparison.OrdinalIgnoreCase) &&
           flags.Any(flag =>
               flag.Name.Equals("c03_2_Main_OpenTrayInWorkshopKey", StringComparison.OrdinalIgnoreCase)
               || flag.Guid == new Guid("b3096800-d600-4015-b934-63d671b597a9"));

    private static IEnumerable<KeyItemAcquisitionFlag> GetPickupAcquisitionFlags(
        Randomizer randomizer,
        ItemPlacement placement) {
        var scene = randomizer.FileRepository
            .GetScnFile(placement.SceneFile)
            .ReadScene(randomizer.FileRepository.TypeRepository);
        var gameObject = scene.FindGameObject(placement.Guid);
        if (gameObject == null)
            yield break;

        foreach (var component in GetPickupInteractionComponents(gameObject)) {
            if (TryGetAcquisitionFlag(component) is{ } flag) {
                yield return flag;
            }
        }
    }

    private static IEnumerable<RszObjectNode> GetPickupInteractionComponents(RszGameObject gameObject) {
        var result = new List<RszObjectNode>();
        gameObject.VisitGameObjects(child => { result.AddRange(child.Components.Where(IsPickupInteraction)); });

        return result;
    }

    private static KeyItemAcquisitionFlag? TryGetAcquisitionFlag(RszObjectNode component) {
        if (component.Type.FindFieldIndex("SetFsmBoolFlag") == -1 ||
            component.Type.FindFieldIndex("SetFsmBoolFlagId") == -1 ||
            component.Type.FindFieldIndex("SetFsmBoolFlagValue") == -1) {
            return null;
        }

        var flagName = component.Get<string>("SetFsmBoolFlag");
        if (string.IsNullOrWhiteSpace(flagName) ||
            flagName.Equals("none", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }

        return new(
            flagName,
            component.Get<Guid>("SetFsmBoolFlagId"),
            component.Get<bool>("SetFsmBoolFlagValue"));
    }

    private static KeyItemReplacementPlanSet? CreateKeyItemReplacementPlans(
        RandomizerLogger logger,
        Rng rng,
        IReadOnlyCollection<ItemReplacementTarget> availableTargets) {
        var supportedIds = _supportedKeyItems
            .Select(rule => rule.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var randomizableRules = _supportedKeyItems
            .Where(rule => !_preservedVanillaKeyItemIds.Contains(rule.Id))
            .ToImmutableArray();
        var routeSeed = rng.Next();
        foreach (var preservedRule in _supportedKeyItems.Where(rule => _preservedVanillaKeyItemIds.Contains(rule.Id))) {
            logger.LogLine(
                $"Skipped key item {_itemDefinitions.GetName(preservedRule.Id)}: vanilla placement is preserved until exact access-sphere data is available.");
        }

        // Some vanilla-safe keys only have drawer-backed candidate locations.
        // Keep those keys vanilla instead of forcing the whole feature into unsafe targets.
        foreach (var activeRules in EnumerateRuleSubsets(randomizableRules)) {
            var activeIds = activeRules
                .Select(rule => rule.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var activeTargets = availableTargets
                .Where(target => !supportedIds.Contains(target.Placement.Id) || activeIds.Contains(target.Placement.Id))
                .ToList();
            var planSet = TryCreateKeyItemReplacementPlans(routeSeed, activeRules, activeTargets,
                randomizableRules.Length, logger);
            if (planSet == null)
                continue;

            foreach (var skippedRule in randomizableRules.Where(rule => !activeIds.Contains(rule.Id))) {
                logger.LogLine(
                    $"Skipped key item {_itemDefinitions.GetName(skippedRule.Id)}: no complete safe route was found after excluding drawer-backed pickups; vanilla placement is preserved.");
            }

            return planSet;
        }

        logger.LogLine(
            "Skipped key item randomization: no supported key item could be placed on a complete safe route " +
            $"after {MaxPreferredRuleSubsetAttempts} preferred fallback attempts and a deterministic greedy fallback.");
        return null;
    }

    private static KeyItemReplacementPlanSet? TryCreateKeyItemReplacementPlans(
        int routeSeed,
        ImmutableArray<KeyItemRule> activeRules,
        IReadOnlyCollection<ItemReplacementTarget> availableTargets,
        int fullRuleCount,
        RandomizerLogger logger) {
        var routeGraph = new KeyItemRouteGraph(activeRules);
        foreach (var target in availableTargets
                     .OrderBy(target => target.Placement.SceneFile, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(target => target.TargetGuid)) {
            routeGraph.TryAddTarget(target);
        }

        foreach (var rule in activeRules) {
            if (!routeGraph.HasCandidate(rule)) {
                if (activeRules.Length == fullRuleCount) {
                    logger.LogLine(
                        $"Skipped key item {_itemDefinitions.GetName(rule.Id)}: no route-safe candidate placement was found.");
                }

                return null;
            }
        }

        if (!routeGraph.TryGenerateAssignments(routeSeed, out var assignments, out var routeFailureLog)) {
            if (activeRules.Length == fullRuleCount) {
                logger.LogLine(
                    $"Skipped full key item route: route graph could not assign and validate every progression key after {MaxRouteSeedAttempts} bounded attempts.");
                if (!string.IsNullOrWhiteSpace(routeFailureLog)) {
                    logger.LogLine(routeFailureLog);
                }
            }

            return null;
        }

        if (assignments.Length != activeRules.Length) {
            return null;
        }

        var result = new Dictionary<ReplacementKey, ReplacementPlan>();
        foreach (var assignment in assignments) {
            if (result.ContainsKey(assignment.Target.Key)) {
                logger.LogLine(
                    $"Skipped key item randomization: route graph assigned multiple key items to {FormatScenePath(assignment.Target.Placement.SceneFile)}.");
                return null;
            }

            result[assignment.Target.Key] = ReplacementPlan.KeyItem(
                assignment.Target.Placement,
                assignment.Target.TargetGuid,
                assignment.Rule);
            logger.LogLine($"[KEY ITEM ROUTE] {_itemDefinitions.GetName(assignment.Rule.Id)} " +
                           $"-> {assignment.RegionName}: {FormatScenePath(assignment.Target.Placement.SceneFile)} " +
                           $"[{assignment.Target.Placement.Position}]");
            logger.LogLine($"GUID: {assignment.Target.TargetGuid}");
        }

        return new KeyItemReplacementPlanSet(result, activeRules, assignments);
    }

    private static IEnumerable<ImmutableArray<KeyItemRule>> EnumerateRuleSubsets(ImmutableArray<KeyItemRule> rules) {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var subset in EnumeratePreferredRuleSubsets(rules).Take(MaxPreferredRuleSubsetAttempts)) {
            if (seen.Add(GetRuleSubsetSignature(subset)))
                yield return subset;
        }

        // Always retain a cheap last-resort path to a smaller safe randomization.
        // This is deliberately appended after the higher-quality one/two-key omissions above.
        var activeRules = rules;
        while (activeRules.Length > 0) {
            if (seen.Add(GetRuleSubsetSignature(activeRules)))
                yield return activeRules;

            var dropRule = activeRules
                .OrderBy(rule => rule.Priority)
                .ThenBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase)
                .First();
            activeRules = activeRules.Remove(dropRule);
        }
    }

    private static IEnumerable<ImmutableArray<KeyItemRule>> EnumeratePreferredRuleSubsets(
        ImmutableArray<KeyItemRule> rules) {
        for (var omittedCount = 0; omittedCount < rules.Length; omittedCount++) {
            var candidates = EnumerateIndexCombinations(rules.Length, omittedCount)
                .Select(omittedIndices => new{
                    OmittedIndices = omittedIndices,
                    OmittedPriority = omittedIndices.Sum(index => rules[index].Priority),
                    OmittedMask = omittedIndices.Aggregate(
                        BigInteger.Zero,
                        (mask, index) => mask | (BigInteger.One << index)),
                })
                .OrderBy(candidate => candidate.OmittedPriority)
                // The previous active-mask search used ascending masks. For equal priority,
                // that is equivalent to descending omitted masks; preserve seed behavior.
                .ThenByDescending(candidate => candidate.OmittedMask);

            foreach (var candidate in candidates) {
                var omitted = candidate.OmittedIndices.ToHashSet();
                yield return rules
                    .Where((_, index) => !omitted.Contains(index))
                    .ToImmutableArray();
            }
        }
    }

    private static IEnumerable<ImmutableArray<int>> EnumerateIndexCombinations(int itemCount, int selectionCount) {
        if (selectionCount == 0) {
            yield return [];
            yield break;
        }

        var selected = new int[selectionCount];
        foreach (var result in Enumerate(0, 0))
            yield return result;

        IEnumerable<ImmutableArray<int>> Enumerate(int depth, int startIndex) {
            if (depth == selectionCount) {
                yield return [.. selected];
                yield break;
            }

            var remaining = selectionCount - depth;
            for (var index = startIndex; index <= itemCount - remaining; index++) {
                selected[depth] = index;
                foreach (var result in Enumerate(depth + 1, index + 1))
                    yield return result;
            }
        }
    }

    private static string GetRuleSubsetSignature(ImmutableArray<KeyItemRule> rules)
        => string.Join('\u001F', rules.Select(rule => rule.Id));

    private static void AddKeyItemHintsOutput(Randomizer randomizer, KeyItemReplacementPlanSet replacementPlanSet) {
        if (replacementPlanSet.Assignments.IsDefaultOrEmpty)
            return;

        var hints = replacementPlanSet.Assignments.Select(assignment => {
            var placement = assignment.Target.Placement;
            var pickupName = placement.IsExtra && !string.IsNullOrWhiteSpace(placement.Comment)
                ? placement.Comment
                : assignment.Target.Label;
            return new KeyItemHint(
                assignment.RouteOrder,
                _itemDefinitions.GetName(assignment.Rule.Id),
                assignment.Rule.Id,
                assignment.Rule.Count,
                pickupName,
                assignment.RegionName,
                placement.SceneFile,
                assignment.Target.TargetGuid,
                placement.Position.X,
                placement.Position.Y,
                placement.Position.Z);
        });
        var html = KeyItemHintsGenerator.RenderHtml(hints, randomizer.Seed);
        randomizer.AddOutputAsset(new IntelOrca.Biohazard.BioRand.RandomizerOutputAsset(
            "4-key-hints",
            "Key Item Spoiler Maps",
            "Floor-plan maps showing where progression items were placed in this seed.",
            $"biorand-re7-{randomizer.Seed}-key-items.html",
            Encoding.UTF8.GetBytes(html)));
    }

    internal static string GenerateRouteGraphMermaid(bool includeItems = false)
        => new KeyItemRouteGraph(_supportedKeyItems).ToMermaid(includeItems);

    internal static KeyItemRouteGraphDiagram GenerateRouteGraphDiagram()
        => new KeyItemRouteGraph(_supportedKeyItems).ToDiagram();

    private static RszGameObject CreateReplacementGameObject(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        RandomItemSettings randomItemSettings,
        ReplacementPlan plan,
        RszGameObject originalGameObject,
        bool preserveItemModels,
        string? preserveObjectShapeReason,
        IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>> acquisitionFlagsByItemId) {
        var originalItem = originalGameObject.FindComponent<app.Item>()
                           ?? throw new Exception(
                               $"Item placement {plan.TargetGuid} in {plan.Placement.SceneFile} does not have an app.Item component.");
        var originalTransform = originalGameObject.FindComponent<GeneratedViaTransform>();
        var drop = plan.Drop;
        var templateItemId = randomizer.ItemRandomizer.GetItemTemplateIdForDrop(drop.Id, rng, randomItemSettings);
        var template = TryGetItemTemplate(randomizer, logger, templateItemId, originalGameObject);

        if (preserveObjectShapeReason != null) {
            logger.LogLine($"Preserving original pickup object shape because {preserveObjectShapeReason}.");
            LogReplacement(logger, plan, originalItem, drop);
            ApplyDropToItem(originalItem, rng, drop);
            var preservedGameObject = originalGameObject.AddOrUpdateComponent(originalItem);
            var preservedReplacement = preserveItemModels
                ? preservedGameObject
                : preservedGameObject.ApplyVisualResourcesFromTemplate(template);
            preservedReplacement = ApplyAcquisitionFlags(preservedReplacement,
                GetAcquisitionFlags(acquisitionFlagsByItemId, drop.Id));
            LogAcquisitionFlags(logger, acquisitionFlagsByItemId, drop.Id);
            return preservedReplacement;
        }

        var carrierTemplate = template;
        var visualTemplate = template;
        if (!HasPickupInteraction(template)) {
            logger.LogLine(
                $"Template {templateItemId} has no pickup interaction; using {ExtraKeyItemCarrierTemplateId} as the pickup carrier.");
            carrierTemplate = randomizer.TemplateService.GetItemTemplate(ExtraKeyItemCarrierTemplateId);
        }

        var replacement = carrierTemplate.CloneWithNewGuids(
            randomizer.GetRng(TemplateInstanceKey, plan.Placement.SceneFile, plan.TargetGuid, templateItemId,
                carrierTemplate.Guid),
            originalGameObject.Guid);
        var item = replacement.FindComponent<app.Item>() ?? originalItem;

        ApplyDropToItem(item, rng, drop);
        item.Enabled = true;
        replacement = replacement.AddOrUpdateComponent(item);

        if (originalTransform != null) {
            replacement = replacement.AddOrUpdateComponent(originalTransform);
        }

        if (preserveItemModels) {
            var mesh = originalGameObject.FindComponent("via.render.Mesh");
            if (mesh != null) {
                replacement = replacement.AddOrUpdateComponent(mesh);
            }
        } else if (!ReferenceEquals(carrierTemplate, visualTemplate)) {
            replacement = replacement.ApplyVisualResourcesFromTemplate(visualTemplate);
        }

        replacement = ApplyAcquisitionFlags(
            replacement
                .PreparePickupInteractionsForPlacement()
                .PrepareWeaponPickupInteractionGameObjects(),
            GetAcquisitionFlags(acquisitionFlagsByItemId, drop.Id));

        replacement = replacement.WithSettings(
            replacement.Settings
                .Set("Update", originalGameObject.Settings.Get<bool>("Update"))
                .Set("Draw", originalGameObject.Settings.Get<bool>("Draw")));

        LogReplacement(logger, plan, originalItem, drop);
        LogAcquisitionFlags(logger, acquisitionFlagsByItemId, drop.Id);
        return replacement.WithGuid(originalGameObject.Guid);
    }

    private static RszGameObject CreateExtraKeyItemGameObject(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        RandomItemSettings randomItemSettings,
        ReplacementPlan plan,
        IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>> acquisitionFlagsByItemId) {
        var drop = plan.Drop;
        var templateItemId = randomizer.ItemRandomizer.GetItemTemplateIdForDrop(drop.Id, rng, randomItemSettings);
        var template = randomizer.TemplateService.GetItemTemplate(ExtraKeyItemCarrierTemplateId);
        var visualTemplate = randomizer.TemplateService.GetItemTemplate(templateItemId);
        var replacement = template.CloneWithNewGuids(
            randomizer.GetRng(TemplateInstanceKey, plan.Placement.SceneFile, plan.TargetGuid,
                ExtraKeyItemCarrierTemplateId, templateItemId),
            plan.TargetGuid);
        var item = replacement.FindComponent<app.Item>()
                   ?? throw new Exception(
                       $"Item template {ExtraKeyItemCarrierTemplateId} does not have an app.Item component.");

        ApplyDropToItem(item, rng, drop);
        if (plan.Placement.SaveGuid != Guid.Empty) {
            item.SaveGUID = plan.Placement.SaveGuid;
        }

        item.RoomId = 0;
        replacement = replacement.AddOrUpdateComponent(item);

        if (replacement.FindComponent<GeneratedViaTransform>() is{ } transform) {
            transform.Position = plan.Placement.Position;
            transform.Rotation = plan.Placement.Rotation;
            transform.Scale = Vector3.One;
            replacement = replacement.AddOrUpdateComponent(transform);
        }

        replacement = ApplyAcquisitionFlags(
            replacement
                .ApplyVisualResourcesFromTemplate(visualTemplate)
                .PreparePickupInteractionsForPlacement()
                .PrepareWeaponPickupInteractionGameObjects(),
            GetAcquisitionFlags(acquisitionFlagsByItemId, drop.Id));
        replacement = replacement.WithSettings(
            replacement.Settings
                .Set("Update", true)
                .Set("Draw", true));

        logger.LogLine($"[KEY ITEM] Placing [{drop.CountEasy}, {drop.CountNormal}, {drop.CountMadhouse}]x " +
                       $"{_itemDefinitions.GetName(drop.Id)} at {plan.Placement.Position}.");
        logger.LogLine($"GUID: {plan.TargetGuid}");
        LogAcquisitionFlags(logger, acquisitionFlagsByItemId, drop.Id);
        return replacement.WithGuid(plan.TargetGuid);
    }

    private static ImmutableArray<KeyItemAcquisitionFlag> GetAcquisitionFlags(
        IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>> acquisitionFlagsByItemId,
        string itemId)
        => acquisitionFlagsByItemId.TryGetValue(itemId, out var flags)
            ? flags
            : [];

    private static RszGameObject ApplyAcquisitionFlags(
        RszGameObject gameObject,
        ImmutableArray<KeyItemAcquisitionFlag> flags) {
        if (flags.IsDefaultOrEmpty)
            return gameObject;

        var flag = flags[0];
        return gameObject.VisitGameObjects(child => {
            var components = child.Components.ToBuilder();
            var changed = false;

            for (var i = 0; i < components.Count; i++) {
                var component = components[i];
                if (!IsPickupInteraction(component))
                    continue;

                var updated = SetFieldIfPresent(component, "SetFsmBoolFlag", flag.Name);
                updated = SetFieldIfPresent(updated, "SetFsmBoolFlagId", flag.Guid);
                updated = SetFieldIfPresent(updated, "SetFsmBoolFlagValue", flag.Value);

                if (!ReferenceEquals(updated, component)) {
                    components[i] = updated;
                    changed = true;
                }
            }

            return changed
                ? child.WithComponents(components.ToImmutable())
                : child;
        });
    }

    private static bool IsPickupInteraction(RszObjectNode component)
        => component.Type.Name.Contains("InteractDetailSearch", StringComparison.Ordinal) &&
           component.Type.FindFieldIndex("SetFsmBoolFlag") != -1;

    private static bool HasPickupInteraction(RszGameObject gameObject) {
        var result = false;
        gameObject.VisitGameObjects(child => { result |= child.Components.Any(IsPickupInteraction); });

        return result;
    }

    private static RszObjectNode SetFieldIfPresent(RszObjectNode component, string fieldName, object value)
        => component.Type.FindFieldIndex(fieldName) == -1
            ? component
            : component.SetField(fieldName, value);

    private static void LogAcquisitionFlags(
        RandomizerLogger logger,
        IReadOnlyDictionary<string, ImmutableArray<KeyItemAcquisitionFlag>> acquisitionFlagsByItemId,
        string itemId) {
        foreach (var flag in GetAcquisitionFlags(acquisitionFlagsByItemId, itemId)) {
            logger.LogLine($"Pickup side effect: sets {flag.Name}={flag.Value} ({flag.Guid}).");
        }
    }

    private static string? GetPreserveObjectShapeReason(
        ItemPlacement placement,
        Biohazard.BioRand.RE7.Extensions.RszExtensions.GameObjectMatch originalMatch) {
        if (originalMatch.HasFsmInHierarchy)
            return "this placement is FSM-controlled";

        if (placement.IsExtra && ExtraPlacementModifier.IsPlainExtraItemPlacement(placement))
            return "this key item is using a generated extra pickup carrier";

        if (IsOriginalSupportedKeyItemPlacement(placement))
            return "this placement is an original key item carrier";

        if (!string.IsNullOrWhiteSpace(placement.Id)
            && !_supportedKeyItems.Any(rule => rule.Id.Equals(placement.Id, StringComparison.OrdinalIgnoreCase))
            && _itemDefinitions.FromId(placement.Id) is{ } definition
            && IsPlainRandomItemTarget(definition)) {
            return "this key item is using an existing random-item pickup carrier";
        }

        return null;
    }

    private static void ApplyDropToItem(app.Item item, Rng rng, Item drop) {
        item.SaveGUID = rng.NextGuid();
        item.ItemDataID = drop.Id;
        item.ItemStackNum = drop.CountNormal;
        item._IsOverwriteDifficultItemNumSetting = true;
        item._DifficultItemNumSetting.EasyNum = drop.CountEasy;
        item._DifficultItemNumSetting.HardNum = drop.CountMadhouse;
        item.Enabled = true;
    }

    private static RszGameObject TryGetItemTemplate(
        Randomizer randomizer,
        RandomizerLogger logger,
        string templateItemId,
        RszGameObject originalGameObject) {
        try {
            return randomizer.TemplateService.GetItemTemplate(templateItemId);
        }
        catch (Exception ex) {
            logger.LogLine(
                $"Template {templateItemId} was not found; preserving original pickup object shape. {ex.Message}");
            return originalGameObject;
        }
    }

    private static void LogReplacement(
        RandomizerLogger logger,
        ReplacementPlan plan,
        app.Item originalItem,
        Item drop) {
        var replaceeName = _itemDefinitions.GetName(originalItem.ItemDataID);
        var replacerName = _itemDefinitions.GetName(drop.Id);
        var prefix = plan.Kind == ReplacementKind.KeyItem ? "[KEY ITEM]" : "[KEY ITEM FILLER]";
        logger.LogLine($"{prefix} Replacing {replaceeName} at {plan.Placement.Position} with " +
                       $"[{drop.CountEasy}, {drop.CountNormal}, {drop.CountMadhouse}]x {replacerName}.");
        logger.LogLine($"GUID: {plan.TargetGuid}");
    }

    private static string FormatScenePath(string path)
        => _areaDefinitions.FormatScenePath(path);

    private static T ReplaceGameObjects<T>(T node, IReadOnlyDictionary<Guid, RszGameObject> replacements)
        where T : IRszSceneNode {
        if (node.Children.IsDefaultOrEmpty)
            return node;

        var children = node.Children.ToBuilder();
        for (var i = 0; i < children.Count; i++) {
            if (children[i] is RszGameObject oldGameObject &&
                replacements.TryGetValue(oldGameObject.Guid, out var replacement)) {
                children[i] = replacement.WithGuid(oldGameObject.Guid);
            } else {
                children[i] = ReplaceGameObjects(children[i], replacements);
            }
        }

        return (T)node.WithChildren(children.ToImmutable());
    }

    private static bool PathContains(string path, string value)
        => path.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool IsFlashbackPath(string path)
        => ScriptedSceneSafety.IsFlashbackPath(path);

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

    private static bool IsMainHouseBeforeHatch(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/mainhouse_west/")
           || PathContains(path, "c03_mainhouse1fgaragehallway")
           || PathContains(path, "c03_mainhouse1fhallway")
           || PathContains(path, "c03_mainhouse1fldk")
           || PathContains(path, "c03_mainhouse1fliving")
           || PathContains(path, "c03_mainhouse1fpantry")
           || PathContains(path, "c03_mainhouse1fwash");

    private static bool IsMainHouseBeforeGarage(string path)
        => IsMainHouseBeforeHatch(path)
           || PathContains(path, "c03_mainhouse1fgaragehallway")
           || PathContains(path, "c03_mainhouse1fgarageoutside");

    private static bool IsMainHouseClockReward(ItemPlacement placement)
        => placement.Guid == _mainHouseClockRewardGuid
           && PathContains(placement.SceneFile, "c03_mainhouse1fliving");

    private static bool IsHatchKeySafeTarget(ItemPlacement placement)
        => PathContains(placement.SceneFile, "c03_mainhouse1fldk")
           || (PathContains(placement.SceneFile, "c03_mainhouse1fliving")
               && !IsMainHouseClockReward(placement))
           || (PathContains(placement.SceneFile, "c03_mainhouse1fpantry")
               && !IsUnderHatchPantryTarget(placement))
           || PathContains(placement.SceneFile, "c03_mainhouse1fhallway")
           || PathContains(placement.SceneFile, "c03_mainhouse1fgaragehallway");

    private static bool IsUnderHatchPantryTarget(ItemPlacement placement)
        => placement.IsExtra
           && string.Equals(placement.Comment, "Pantry", StringComparison.OrdinalIgnoreCase)
           && PathContains(placement.SceneFile, "c03_mainhouse1fpantry")
           && placement.Position.Y < -0.5f;

    private static bool IsMainHouseWestBlueDogHead(ItemPlacement placement)
        => placement.Guid == _mainHouseWestBlueDogHeadGuid
           && placement.Id.Equals("3CrestKeyA", StringComparison.OrdinalIgnoreCase)
           && PathContains(placement.SceneFile, "/leveldesign/itemset/chapter3/mainhouse_west/mainhouse_west.scn");

    private static bool IsMainHouseWestBlueKeycard(ItemPlacement placement)
        => placement.Guid == _mainHouseWestBlueKeycardGuid
           && placement.Id.Equals("LucasCardKey", StringComparison.OrdinalIgnoreCase)
           && PathContains(placement.SceneFile, "/leveldesign/itemset/chapter3/mainhouse_west/mainhouse_west.scn");

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

    private static bool IsMainHouseEastOrBasement(string path)
        => !PathContains(path, "c03_rightareab1fstoreroom")
           && (PathContains(path, "/leveldesign/itemset/chapter3/mainhouse_east/")
               || PathContains(path, "c03_rightarea"));

    private static bool IsDissectionRoomRoute(string path)
        => PathContains(path, "c03_rightareab1ffreezer")
           || PathContains(path, "c03_rightareab1fmorgue");

    private static bool IsMainHouseSnakeKeyRoom(string path)
        => PathContains(path, "c03_mainhouse2fbedroom")
           || PathContains(path, "c03_mainhouse2fkids")
           || PathContains(path, "c03_mainhousoutsideterrace2f3");

    private static bool IsMainHouseKeycardSetup(ItemPlacement placement)
        => placement.Guid == _redKeycardWorkshopGuid
           || IsMainHouseWestBlueKeycard(placement)
           || IsMainHouseSnakeKeyRoom(placement.SceneFile);

    private static bool IsMainHouseAtticShadowPuzzleArea(string path)
        => PathContains(path, "c03_mainhouse2fkids02");

    private static bool IsSnakeKeyRewardTarget(ItemPlacement placement)
        => PathContains(placement.SceneFile, "c03_rightareab1fstoreroom")
           && _snakeKeyRewardGuids.Contains(placement.Guid);

    private static bool IsOldHouseStoneStatuetteTarget(ItemPlacement placement)
        => PathContains(placement.SceneFile, "/leveldesign/itemset/chapter3/oldhouse/")
           && placement.Guid == _oldHouseStoneStatuetteGuid;

    private static bool IsOldHouseCrowKeyTarget(ItemPlacement placement)
        => PathContains(placement.SceneFile, "/leveldesign/itemset/chapter3/oldhouse/")
           && placement.Guid == _oldHouseCrowKeyGuid;

    private static bool IsYardOrTrailer(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/gardenarea/")
           || PathContains(path, "c03_gardenarea")
           || PathContains(path, "c03_trailerhouse")
           || PathContains(path, "c03_mainhousoutside")
           || PathContains(path, "c03_mainhousoutsideterrace");

    private static bool IsOldHouseBeforeStonePuzzle(string path)
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

    private static bool IsOldHouseBeforeCrowDoor(string path)
        => IsOldHouseBeforeStonePuzzle(path);

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

    private static bool IsTestingArea(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/leftarea/")
           || PathContains(path, "c03_leftarea");

    private static bool IsTestingAreaBeforeBarnFight(string path)
        => PathContains(path, "/leveldesign/itemset/chapter3/cowshed/")
           || PathContains(path, "c03_cowshed");

    private static bool IsTestingAreaAfterLucasPuzzle(string path)
        => PathContains(path, "c03_leftarea1fmonitorroom")
           || PathContains(path, "c03_leftarea1fpuzzleroom");

    private static bool IsBoatHouseAfterSerumUse(string path)
        => PathContains(path, "c03_boat1fbridge02");

    private static bool IsBoatHouseRoute(string path)
        => !IsBoatHouseAfterSerumUse(path)
           && (PathContains(path, "/leveldesign/itemset/chapter3/boatshed/")
               || PathContains(path, "c03_boat")
               || PathContains(path, "c03_gardenareaboat"));

    private static bool IsShipBeforeLugWrench(string path)
        => !PathContains(path, "past")
           && (PathContains(path, "c04_ship4f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship4f/"));

    private static bool IsShipAfterLugWrenchBeforeCorrosive(string path)
        => !PathContains(path, "past")
           && !IsShipAfterCorrosiveBeforeRepair(path)
           && (PathContains(path, "c04_ship1f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship1f/")
               || PathContains(path, "c04_ship2f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship2f/")
               || PathContains(path, "c04_ship3f")
               || PathContains(path, "/leveldesign/itemset/chapter4/ship3f/")
               || PathContains(path, "/scenes/chapter/chapter4/c04_shipelevator"));

    private static bool IsShipAfterCorrosiveBeforeRepair(string path)
        => !PathContains(path, "past")
           && (PathContains(path, "c04_ship3finfirmary")
               || PathContains(path, "c04_ship3fsecurityroom")
               || PathContains(path, "c04_ship3fshowerroom"));

    private static bool IsShipAfterElevatorRepairOrExit(string path)
        => !PathContains(path, "past")
           && (PathContains(path, "c04_shipb1")
               || PathContains(path, "c04_shipb2")
               || PathContains(path, "c04_shipstairs"));

    private static bool IsMiaPresentShipRoute(string path)
        => IsShipBeforeLugWrench(path)
           || IsShipAfterLugWrenchBeforeCorrosive(path)
           || IsShipAfterCorrosiveBeforeRepair(path);

    private static bool IsSaltMineBeforeNecrotoxinUse(string path)
        => !PathContains(path, "/chapter4/lastbattle/")
           && (PathContains(path, "/environment/scene/chapter4/c04_cottage")
               || PathContains(path, "/environment/scene/chapter4/c04_cave")
               || PathContains(path, "/leveldesign/itemset/chapter4/saltdome"));

    private sealed class KeyItemRouteGraph {
        private readonly GraphBuilder _builder = new();
        private readonly ImmutableArray<KeyItemRule> _activeRules;
        private readonly Dictionary<string, Key> _routeKeys;
        private readonly Dictionary<string, KeyItemRule> _activeRulesById;
        private readonly Dictionary<Node, ItemReplacementTarget> _targetsByNode = [];
        private readonly Dictionary<Node, string> _regionByNode = [];
        private readonly Dictionary<Node, int> _routeOrderByTargetNode = [];
        private readonly Dictionary<Node, int> _routeOrderByNode = [];
        private readonly Dictionary<Node, string> _diagramNodeIds = [];
        private readonly List<KeyItemRouteGraphNode> _diagramNodes = [];
        private readonly List<KeyItemRouteGraphEdge> _diagramEdges = [];
        private readonly Node _guestHouseBeforeBoltCutters;
        private readonly Node _guestHouseAfterBoltCutters;
        private readonly Node _guestHouseAfterAxeFight;
        private readonly Node _guestHouseAttic;
        private readonly Node _mainHouseBeforeHatch;
        private readonly Node _mainHouseBeforeGarage;
        private readonly Node _garage;
        private readonly Node _mainHouseBeforeShadowPuzzle;
        private readonly Node _mainHouseClockReward;
        private readonly Node _mainHouseEast;
        private readonly Node _scorpionRooms;
        private readonly Node _dissectionRoom;
        private readonly Node _yard;
        private readonly Node _oldHouseBeforeStonePuzzle;
        private readonly Node _oldHouseAfterStonePuzzle;
        private readonly Node _oldHouseAfterCrank;
        private readonly Node _oldHouseAfterCrow;
        private readonly Node _oldHouseAfterLantern;
        private readonly Node _snakeRooms;
        private readonly Node _testingArea;
        private readonly Node _barn;
        private readonly Node _lucasPuzzle;
        private readonly Node _boatHouse;
        private readonly Node _ship;
        private readonly Node _shipAfterLugWrench;
        private readonly Node _shipAfterCorrosive;
        private readonly Node _shipExit;
        private readonly Node _saltMine;
        private readonly Node _finale;

        public KeyItemRouteGraph(ImmutableArray<KeyItemRule> activeRules) {
            _activeRules = activeRules;
            _routeKeys = activeRules.ToDictionary(
                rule => rule.Id,
                rule => _builder.Key(_itemDefinitions.GetName(rule.Id), rule.RouteMask),
                StringComparer.OrdinalIgnoreCase);
            _activeRulesById = activeRules.ToDictionary(rule => rule.Id, StringComparer.OrdinalIgnoreCase);

            _guestHouseBeforeBoltCutters =
                Room("guest-house-before-bolt-cutters", "Guest House before Mia cell chain", 0, 0);
            _guestHouseAfterBoltCutters =
                Room("guest-house-after-bolt-cutters", "Guest House after Bolt Cutters", 1, 0);
            _guestHouseAfterAxeFight = Room("guest-house-after-axe-fight", "Guest House after Mia axe fight", 2, 0);
            _guestHouseAttic = Room("guest-house-attic", "Guest House attic route", 3, 0);
            _mainHouseBeforeHatch = Room("main-house-before-hatch", "Main House west side before hatch", 4, 0);
            _mainHouseBeforeGarage = Room("main-house-before-garage", "Main House west side before garage", 5, 0);
            _garage = Room("garage", "Garage car fight", 6, 0);
            _mainHouseBeforeShadowPuzzle = Room("main-house-before-shadow-puzzle",
                "Main House after garage before shadow puzzle", 7, 0);
            _mainHouseClockReward = Room("main-house-clock-reward", "Main House clock-pendulum reward", 7, 1);
            _mainHouseEast = Room("main-house-east", "Main House east side and processing area", 8, 0);
            _scorpionRooms = Room("scorpion-rooms", "Main House scorpion-key rooms", 6, 2);
            _dissectionRoom = Room("dissection-room", "Main House dissection room route", 9, 1);
            _yard = Room("yard", "Yard and trailer", 9, 0);
            _oldHouseBeforeStonePuzzle = Room("old-house-before-stone-puzzle",
                "Old House before Stone Statuette shadow puzzle", 10, 0);
            _oldHouseAfterStonePuzzle = Room("old-house-after-stone-puzzle",
                "Old House after Stone Statuette shadow puzzle", 11, 0);
            _oldHouseAfterCrank = Room("old-house-after-crank", "Old House after Crank bridges", 12, 0);
            _oldHouseAfterCrow = Room("old-house-after-crow", "Old House after Crow Key door and Green House", 13, 0);
            _oldHouseAfterLantern = Room("old-house-after-lantern", "Old House after Lantern door", 14, 0);
            _snakeRooms = Room("snake-rooms", "Snake-key rooms and keycard setup", 15, 0);
            _testingArea = Room("testing-area", "Testing Area before barn", 16, 0);
            _barn = Room("barn", "Testing Area barn before battery socket", 17, 0);
            _lucasPuzzle = Room("lucas-puzzle", "Testing Area Lucas puzzle and control room", 18, 0);
            _boatHouse = Room("boat-house", "Boat House before serum use", 19, 0);
            _ship = Room("ship", "Wrecked Ship before elevator hatch", 20, 0);
            _shipAfterLugWrench = Room("ship-after-lug-wrench", "Wrecked Ship after Lug Wrench", 21, 0);
            _shipAfterCorrosive = Room("ship-after-corrosive", "Wrecked Ship after Corrosive access", 22, 0);
            _shipExit = Room("ship-exit", "Wrecked Ship elevator repaired", 23, 0);
            _saltMine = Room("salt-mine", "Swamp and Salt Mine before E-Necrotoxin", 24, 0);
            _finale = Room("finale", "Final E-Necrotoxin use", 25, 0);

            Door(_guestHouseBeforeBoltCutters, _guestHouseAfterBoltCutters, RouteKeys("ChainCutter"));
            Door(_guestHouseAfterBoltCutters, _guestHouseAfterAxeFight, RouteKeys("HandAxe"));
            Door(_guestHouseAfterAxeFight, _guestHouseAttic, RouteKeys("Fuse"));
            NoReturn(_guestHouseAttic, _mainHouseBeforeHatch);
            Door(_mainHouseBeforeHatch, _mainHouseBeforeGarage, RouteKeys("FloorDoorKey"));
            Door(_mainHouseBeforeGarage, _garage, RouteKeys("EthanCarKey"));
            Door(_mainHouseBeforeShadowPuzzle, _scorpionRooms, RouteKeys("MorgueKey"));
            Door(_garage, _mainHouseBeforeShadowPuzzle, RouteKeys("EntranceHallKey"));
            Door(_mainHouseBeforeShadowPuzzle, _mainHouseClockReward, RouteKeys("PendulumClock"));
            Door(_mainHouseBeforeShadowPuzzle, _mainHouseEast, RouteKeys("SilhouettePazzlePiece"));
            Door(_mainHouseEast, _dissectionRoom, RouteKeys("WorkroomKey"));
            Door(_mainHouseEast, _yard, RouteKeys("3CrestKeyB", "3CrestKeyA", "3CrestKeyC"));
            Door(_yard, _oldHouseBeforeStonePuzzle);
            Door(_oldHouseBeforeStonePuzzle, _oldHouseAfterStonePuzzle, RouteKeys("SilhouettePazzlePieceOldHouse"));
            Door(_oldHouseAfterStonePuzzle, _oldHouseAfterCrank, RouteKeys("Crank"));
            Door(_oldHouseAfterCrank, _oldHouseAfterCrow, RouteKeys("TalismanKey"));
            Door(_oldHouseAfterCrow, _oldHouseAfterLantern, RouteKeys("Lantern"));
            Door(_oldHouseAfterLantern, _snakeRooms, RouteKeys("MasterKey", "SerumMaterialA"));
            Door(_snakeRooms, _testingArea, RouteKeys("LucasCardKey", "LucasCardKey2"));
            Door(_testingArea, _barn);
            Door(_barn, _lucasPuzzle, RouteKeys("Battery"));
            Door(_lucasPuzzle, _boatHouse, RouteKeys("Candle_Lighted"));
            NoReturn(_boatHouse, _ship, RouteKeys("SerumComplete", "SerumMaterialA", "SerumMaterialB"));
            Door(_ship, _shipAfterLugWrench, RouteKeys("EvOpener"));
            Door(_shipAfterLugWrench, _shipAfterCorrosive, RouteKeys("SpareKey"));
            Door(_shipAfterCorrosive, _shipExit, RouteKeys("EvCable", "EvOpener", "FuseCh4"));
            NoReturn(_shipExit, _saltMine);
            Door(_saltMine, _finale, RouteKeys("SerumTypeE"));
        }

        public void TryAddTarget(ItemReplacementTarget target) {
            var routeTarget = GetRouteTarget(target);
            if (routeTarget == null) return;

            AddTargetNode(target, routeTarget, routeTarget.GroupMask);
        }

        public bool TryAddFixedTarget(ItemReplacementTarget target, KeyItemRule rule) {
            var routeTarget = GetRouteTarget(target);
            if (routeTarget == null || (routeTarget.GroupMask & rule.RouteMask) != rule.RouteMask)
                return false;

            AddTargetNode(target, routeTarget, rule.RouteMask);
            return true;
        }

        public RouteTarget? GetRouteTarget(ItemReplacementTarget target) {
            var routeTarget = ClassifyTarget(target);
            if (routeTarget == null)
                return null;

            var groupMask = routeTarget.GroupMask & ~GetTargetAccessRequirementMask(target);
            var routeOrder = _routeOrderByNode[routeTarget.Room];
            foreach (var rule in _activeRules) {
                if (routeOrder < (int)rule.EarliestSafePhase) {
                    groupMask &= ~rule.RouteMask;
                }
            }

            if (!string.IsNullOrWhiteSpace(target.Placement.Id) &&
                _activeRulesById.TryGetValue(target.Placement.Id, out var sourceKeyRule)) {
                groupMask &= ~sourceKeyRule.RouteMask;
            }

            return groupMask == 0
                ? null
                : routeTarget with{ GroupMask = groupMask };
        }

        private void AddTargetNode(ItemReplacementTarget target, RouteTarget routeTarget, int groupMask) {
            var node = _builder.Item(
                $"{target.Label} @ {FormatScenePath(target.Placement.SceneFile)}",
                groupMask,
                routeTarget.Room);
            _targetsByNode[node] = target;
            _regionByNode[node] = routeTarget.RegionName;
            _routeOrderByTargetNode[node] = _routeOrderByNode[routeTarget.Room];
        }

        public bool HasCandidate(KeyItemRule rule)
            => _targetsByNode.Keys.Any(node => (node.Group & rule.RouteMask) == rule.RouteMask);

        public bool TryGenerateAssignments(
            int seed,
            out ImmutableArray<KeyItemRouteAssignment> assignments,
            out string? failureLog) {
            failureLog = null;
            assignments = [];
            for (var attempt = 0; attempt < MaxRouteSeedAttempts; attempt++) {
                if (!TryCreateMatchingAssignments(unchecked(seed + attempt), out var candidateAssignments,
                        out failureLog)) {
                    continue;
                }

                if (TryValidateAssignments(candidateAssignments, unchecked(seed + attempt), out failureLog)) {
                    assignments = candidateAssignments;
                    return true;
                }
            }

            return false;
        }

        public bool HasAssignmentsForAllRules(Route route)
            => _activeRules.All(rule => {
                var routeKey = RouteKey(rule.Id);
                return route.GetItemsContainingKey(routeKey)
                    .Where(_targetsByNode.ContainsKey)
                    .Take(2)
                    .Count() == 1;
            });

        public bool TryGenerateRoute(int seed, out Route route, out string? failureLog) {
            var deadEnds = 0;
            try {
                route = _builder.ToGraph().GenerateRoute(seed, new RouteFinderOptions{
                    DebugDepthLimit = _activeRules.Length + RouteDepthPadding,
                    DebugDeadendCallback = _ => {
                        deadEnds++;
                        if (deadEnds > MaxRouteDeadEndsPerAttempt) {
                            throw new RouteFinderException(
                                $"Route dead-end budget exceeded ({MaxRouteDeadEndsPerAttempt}).", _);
                        }
                    },
                });
                failureLog = null;
                return true;
            }
            catch (RouteFinderException ex) {
                route = null!;
                failureLog = $"{ex.Message} Seed={seed}, dead ends={deadEnds}.";
                return false;
            }
        }

        private bool TryCreateMatchingAssignments(
            int seed,
            out ImmutableArray<KeyItemRouteAssignment> assignments,
            out string? failureLog) {
            assignments = [];
            failureLog = null;
            var rng = new Rng(seed);
            var candidatesByRule = new Dictionary<KeyItemRule, List<Node>>();
            foreach (var rule in _activeRules) {
                var candidates = _targetsByNode.Keys
                    .Where(node => (node.Group & rule.RouteMask) == rule.RouteMask)
                    .OrderBy(node => _targetsByNode[node].Placement.SceneFile, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(node => _targetsByNode[node].TargetGuid.ToString("D"), StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (candidates.Count == 0) {
                    failureLog =
                        $"No route-safe candidate placement was found for {_itemDefinitions.GetName(rule.Id)}.";
                    return false;
                }

                Shuffle(candidates, rng);
                candidatesByRule[rule] = candidates;
            }

            var targetAssignments = new Dictionary<Node, KeyItemRule>();
            var ruleAssignments = new Dictionary<KeyItemRule, Node>();
            foreach (var rule in _activeRules
                         .OrderBy(rule => candidatesByRule[rule].Count)
                         .ThenByDescending(rule => rule.Priority)
                         .ThenBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase)) {
                if (!TryAssign(rule, [])) {
                    failureLog = $"No complete route-safe key item matching was found. " +
                                 $"Could not reserve a unique target for {_itemDefinitions.GetName(rule.Id)} " +
                                 $"from {candidatesByRule[rule].Count} candidate placements. " +
                                 $"Candidates: {DescribeCandidates(rule)}. " +
                                 "Candidate counts: " +
                                 string.Join(", ", _activeRules
                                     .OrderBy(candidateRule => candidateRule.Id, StringComparer.OrdinalIgnoreCase)
                                     .Select(candidateRule =>
                                         $"{_itemDefinitions.GetName(candidateRule.Id)}={candidatesByRule[candidateRule].Count}"));
                    return false;
                }
            }

            assignments = _activeRules
                .Select(rule => {
                    var node = ruleAssignments[rule];
                    return new KeyItemRouteAssignment(
                        rule,
                        _targetsByNode[node],
                        _regionByNode[node],
                        _routeOrderByTargetNode[node]);
                })
                .ToImmutableArray();

            var duplicateLocation = assignments
                .GroupBy(assignment => assignment.Target.LocationKey)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateLocation != null) {
                failureLog = "No complete route-safe key item matching was found. " +
                             $"Multiple key items were assigned to the same physical location in {duplicateLocation.Key.SceneFile}.";
                return false;
            }

            return true;

            bool TryAssign(KeyItemRule rule, HashSet<Node> visitedTargets) {
                foreach (var candidate in candidatesByRule[rule]) {
                    if (!visitedTargets.Add(candidate))
                        continue;

                    if (targetAssignments.TryGetValue(candidate, out var currentRule) &&
                        !TryAssign(currentRule, visitedTargets)) {
                        continue;
                    }

                    targetAssignments[candidate] = rule;
                    ruleAssignments[rule] = candidate;
                    return true;
                }

                return false;
            }

            string DescribeCandidates(KeyItemRule rule)
                => string.Join("; ", candidatesByRule[rule]
                    .OrderBy(node => _targetsByNode[node].Placement.SceneFile, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(node => _targetsByNode[node].TargetGuid.ToString("D"), StringComparer.OrdinalIgnoreCase)
                    .Select(node =>
                        $"{_regionByNode[node]} / {FormatScenePath(_targetsByNode[node].Placement.SceneFile)} / {_targetsByNode[node].TargetGuid}"));
        }

        private bool TryValidateAssignments(
            ImmutableArray<KeyItemRouteAssignment> assignments,
            int seed,
            out string? failureLog) {
            var validationGraph = new KeyItemRouteGraph(_activeRules);
            foreach (var assignment in assignments) {
                if (!validationGraph.TryAddFixedTarget(assignment.Target, assignment.Rule)) {
                    failureLog =
                        $"Route-safe matching assigned {_itemDefinitions.GetName(assignment.Rule.Id)} to an invalid target.";
                    return false;
                }
            }

            failureLog = null;
            for (var attempt = 0; attempt < MaxRouteSeedAttempts; attempt++) {
                if (!validationGraph.TryGenerateRoute(unchecked(seed + attempt), out var route,
                        out var candidateFailureLog)) {
                    failureLog = candidateFailureLog ?? failureLog;
                    continue;
                }

                if (route.AllNodesVisited && validationGraph.HasAssignmentsForAllRules(route))
                    return true;

                failureLog = route.Log;
            }

            return false;
        }

        private static void Shuffle<T>(IList<T> items, Rng rng) {
            for (var i = 0; i < items.Count; i++) {
                var j = rng.Next(0, i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        public string ToMermaid(bool includeItems)
            => _builder.ToGraph().ToMermaid(useLabels: true, includeItems);

        public KeyItemRouteGraphDiagram ToDiagram()
            => new([.. _diagramNodes], [.. _diagramEdges]);

        public IEnumerable<KeyItemRouteAssignment> GetAssignments(Route route, RandomizerLogger logger) {
            foreach (var rule in _activeRules) {
                var routeKey = RouteKey(rule.Id);
                var nodes = route.GetItemsContainingKey(routeKey)
                    .Where(_targetsByNode.ContainsKey)
                    .OrderBy(node => node)
                    .ToArray();

                if (nodes.Length == 0) {
                    logger.LogLine(
                        $"Skipped key item {_itemDefinitions.GetName(rule.Id)}: route did not assign a placement.");
                    continue;
                }

                if (nodes.Length > 1) {
                    logger.LogLine(
                        $"Skipped key item {_itemDefinitions.GetName(rule.Id)}: route assigned multiple placements.");
                    continue;
                }

                var node = nodes[0];
                yield return new KeyItemRouteAssignment(
                    rule,
                    _targetsByNode[node],
                    _regionByNode[node],
                    _routeOrderByTargetNode[node]);
            }
        }

        private Key RouteKey(string itemId)
            => _routeKeys[itemId];

        private Key[] RouteKeys(params string[] itemIds)
            =>[
                .. itemIds
                    .Where(_routeKeys.ContainsKey)
                    .Select(RouteKey)
            ];

        private Node Room(string id, string label, int row, int column) {
            var node = _builder.Room(label);
            _routeOrderByNode[node] = row;
            _diagramNodeIds[node] = id;
            _diagramNodes.Add(new(id, label, row, column));
            return node;
        }

        private void Door(Node source, Node target, params Key[] keys) {
            _builder.Door(source, target, [.. keys.Select(key => (Requirement)key)]);
            AddDiagramEdge(source, target, keys, isNoReturn: false);
        }

        private void NoReturn(Node source, Node target, params Key[] keys) {
            _builder.NoReturn(source, target, [.. keys.Select(key => (Requirement)key)]);
            AddDiagramEdge(source, target, keys, isNoReturn: true);
        }

        private void AddDiagramEdge(Node source, Node target, Key[] keys, bool isNoReturn) {
            var labels = keys
                .Select(key => key.Label ?? key.ToString())
                .ToImmutableArray();
            _diagramEdges.Add(new(
                _diagramNodeIds[source],
                _diagramNodeIds[target],
                labels,
                isNoReturn));
        }

        private RouteTarget? ClassifyTarget(ItemReplacementTarget target) {
            var placement = target.Placement;
            var path = placement.SceneFile;
            if (IsFlashbackPath(path))
                return null;

            if (IsUnsafeKeyItemTarget(placement))
                return null;

            if (placement.Chapter == 1) {
                if (IsGuestHouseAfterAxeFight(path))
                    return new(_guestHouseAfterAxeFight, GuestFuseMask, "Guest House after Mia axe fight");
                if (IsGuestHouseAfterBoltCutters(path))
                    return new(_guestHouseAfterBoltCutters, AxeMask, "Guest House after Bolt Cutters");
                if (IsGuestHouseBeforeBoltCutters(path))
                    return new(_guestHouseBeforeBoltCutters, BoltCuttersMask | AxeMask,
                        "Guest House before Mia cell chain");
            } else if (placement.Chapter == 3) {
                if (IsBoatHouseAfterSerumUse(path))
                    return null;
                if (IsBoatHouseRoute(path))
                    return new(_boatHouse, SerumMask | DSeriesArmMask | DSeriesHeadMask, "Boat House before serum use");
                if (IsTestingAreaAfterLucasPuzzle(path))
                    return new(_lucasPuzzle, LucasAfterPuzzleCarryMasks, "Testing Area Lucas puzzle and control room");
                if (IsTestingAreaBeforeBarnFight(path))
                    return new(_barn, LucasBeforePuzzleCarryMasks, "Testing Area barn before battery socket");
                if (IsTestingArea(path))
                    return new(_testingArea, LucasBeforePuzzleCarryMasks, "Testing Area before barn");
                if (IsMainHouseKeycardSetup(placement))
                    return new(_snakeRooms, KeycardSetupCarryMasks, "Main House snake-key rooms and keycard setup");
                if (PathContains(path, "c03_mainhouse2fgrandma"))
                    return new(_scorpionRooms, MainHouseCarryMasks | StoneStatuetteMask | DSeriesHeadMask,
                        "Main House scorpion-key rooms");
                if (IsSnakeKeyRewardTarget(placement))
                    return new(_oldHouseAfterLantern, SnakeKeyRewardCarryMasks,
                        "Old House cleared / Main House basement police body");
                if (IsOldHouseStoneStatuetteTarget(placement))
                    return new(_oldHouseBeforeStonePuzzle, OldHouseBeforeCrowCarryMasks,
                        "Old House Stone Statuette pickup");
                if (IsOldHouseCrowKeyTarget(placement))
                    return new(_oldHouseAfterCrank, OldHouseAfterCrankCarryMasks, "Old House Crow Key chest");
                if (IsOldHouseAfterLanternDoor(path))
                    return new(_oldHouseAfterLantern, OldHouseAfterLanternCarryMasks, "Old House after Lantern door");
                if (IsOldHouseAfterCrowDoorOrGreenHouse(path))
                    return new(_oldHouseAfterCrow, OldHouseAfterCrowCarryMasks,
                        "Old House after Crow Key door and Green House");
                if (IsOldHouseAfterCrankBeforeCrowDoor(path))
                    return new(_oldHouseAfterCrank, OldHouseAfterCrankCarryMasks, "Old House after Crank bridges");
                if (IsOldHouseAfterStonePuzzleBeforeCrank(path))
                    return new(_oldHouseAfterStonePuzzle, OldHouseAfterStoneCarryMasks,
                        "Old House after Stone Statuette shadow puzzle");
                if (IsOldHouseBeforeCrowDoor(path))
                    return new(_oldHouseBeforeStonePuzzle, OldHouseBeforeCrowCarryMasks,
                        "Old House before Stone Statuette shadow puzzle");
                if (IsYardOrTrailer(path))
                    return new(_yard, OldHouseBeforeCrowCarryMasks, "Yard and trailer");
                if (IsDissectionRoomRoute(path))
                    return new(_dissectionRoom, DissectionRoomCarryMasks, "Main House dissection room route");
                if (IsMainHouseEastOrBasement(path))
                    return new(_mainHouseEast, MainHouseEastCarryMasks, "Main House east side and processing area");
                if (IsMainHouseClockReward(placement))
                    return new(_mainHouseClockReward, MainHouseAfterGarageCarryMasks & ~PendulumMask,
                        "Main House clock-pendulum reward");
                if (IsMainHouseWestBlueDogHead(placement))
                    return new(_mainHouseBeforeShadowPuzzle, MainHouseAfterGarageCarryMasks,
                        "Main House after garage before shadow puzzle");
                if (IsMainHouseBeforeShadowPuzzle(path))
                    return new(_mainHouseBeforeShadowPuzzle, MainHouseAfterGarageCarryMasks,
                        "Main House after garage before shadow puzzle");
                if (IsGarage(path))
                    return new(_garage, OxStatuetteMask, "Garage car fight");
                if (IsUnderHatchPantryTarget(placement))
                    return new(_mainHouseBeforeGarage, MainHouseBeforeHatchCarryMasks & ~FloorDoorKeyMask,
                        "Main House crawlspace after hatch");
                if (IsMainHouseBeforeHatch(path))
                    return new(_mainHouseBeforeHatch, MainHouseBeforeHatchCarryMasks,
                        "Main House west side before hatch");
                if (IsMainHouseBeforeGarage(path))
                    return new(_mainHouseBeforeGarage, MainHouseBeforeHatchCarryMasks & ~FloorDoorKeyMask,
                        "Main House west side before garage");
            } else if (placement.Chapter == 4) {
                if (IsShipAfterElevatorRepairOrExit(path))
                    return null;
                if (IsShipAfterCorrosiveBeforeRepair(path))
                    return new(_shipAfterCorrosive, ShipAfterCorrosiveMasks, "Wrecked Ship after Corrosive access");
                if (IsShipAfterLugWrenchBeforeCorrosive(path))
                    return new(_shipAfterLugWrench, ShipAfterWrenchMasks, "Wrecked Ship after Lug Wrench");
                if (IsShipBeforeLugWrench(path))
                    return new(_ship, ShipBeforeWrenchMasks, "Wrecked Ship before elevator hatch");
                if (IsSaltMineBeforeNecrotoxinUse(path))
                    return new(_saltMine, NecrotoxinMask, "Swamp and Salt Mine before E-Necrotoxin");
            }

            return null;
        }

        private static int GetTargetAccessRequirementMask(ItemReplacementTarget target) {
            var mask = 0;
            if (target.Placement.Guid == _guestHouseFuseCabinetGuid &&
                PathContains(target.Placement.SceneFile, "/chapter1/c01_corridor01.scn")) {
                mask |= BoltCuttersMask | AxeMask;
            }

            if (IsMainHouseClockReward(target.Placement)) {
                mask |= FloorDoorKeyMask | CarKeyMask | OxStatuetteMask | PendulumMask;
            }

            if (!IsHatchKeySafeTarget(target.Placement)) {
                mask |= FloorDoorKeyMask;
            }

            if (IsMainHouseWestBlueKeycard(target.Placement)) {
                mask |= BlueKeycardMask | RedKeycardMask;
            }

            return mask;
        }

        private static bool IsUnsafeKeyItemTarget(ItemPlacement placement)
            => _treasureRewardGuids.Contains(placement.Guid)
               || (placement.Guid == _jack2RedDogHeadGuid
                && PathContains(placement.SceneFile, "c03_rightareab1ffreezer"))
               || (placement.Guid == _lucasPuzzleCandleGuid
                   && PathContains(placement.SceneFile, "c03_leftarea1fpuzzleroom1"))
               || IsMainHouseAtticShadowPuzzleArea(placement.SceneFile);
    }

    private enum ReplacementKind {
        KeyItem,
        Filler,
    }

    private enum KeyItemRoutePhase {
        Any = 0,
        MainHouseEast = 8,
        OldHouseAfterStonePuzzle = 11,
        OldHouseAfterCrank = 12,
        OldHouseAfterLantern = 14,
        LucasPuzzle = 18,
        ShipAfterCorrosive = 22,
    }

    private sealed record KeyItemRule(
        string Id,
        int Chapter,
        int RouteMask,
        int Count = 1,
        int Priority = 100,
        KeyItemRoutePhase EarliestSafePhase = KeyItemRoutePhase.Any);

    private enum KeyItemAcquisitionFlagSource {
        RelocatedPickup,
        NativeTriggerOnly,
    }

    private sealed record KeyItemAcquisitionFlag(
        string Name,
        Guid Guid,
        bool Value,
        KeyItemAcquisitionFlagSource Source = KeyItemAcquisitionFlagSource.RelocatedPickup);

    private sealed record KeyItemReplacementPlanSet(
        Dictionary<ReplacementKey, ReplacementPlan> Plans,
        ImmutableArray<KeyItemRule> ActiveRules,
        ImmutableArray<KeyItemRouteAssignment> Assignments);

    private sealed record RouteTarget(Node Room, int GroupMask, string RegionName);

    private sealed record KeyItemRouteAssignment(
        KeyItemRule Rule,
        ItemReplacementTarget Target,
        string RegionName,
        int RouteOrder);

    internal sealed record KeyItemRouteGraphDiagram(
        ImmutableArray<KeyItemRouteGraphNode> Nodes,
        ImmutableArray<KeyItemRouteGraphEdge> Edges);

    internal sealed record KeyItemRouteGraphNode(
        string Id,
        string Label,
        int Row,
        int Column);

    internal sealed record KeyItemRouteGraphEdge(
        string SourceId,
        string TargetId,
        ImmutableArray<string> Requirements,
        bool IsNoReturn);

    private readonly record struct ReplacementKey(string SceneFile, Guid Guid);

    private readonly record struct ReplacementLocationKey(string SceneFile, int X, int Y, int Z);

    private sealed record ItemReplacementTarget(
        ItemPlacement Placement,
        ItemDefinition? Definition,
        Guid TargetGuid,
        string Label) {
        public ReplacementKey Key => new(Placement.SceneFile, TargetGuid);

        public ReplacementLocationKey LocationKey => new(
            Placement.SceneFile,
            QuantizeLocation(Placement.Position.X),
            QuantizeLocation(Placement.Position.Y),
            QuantizeLocation(Placement.Position.Z));
    }

    private static int QuantizeLocation(float value)
        => (int)MathF.Round(value * 2, MidpointRounding.AwayFromZero);

    private sealed record ReplacementPlan(
        ReplacementKind Kind,
        ItemPlacement Placement,
        Guid TargetGuid,
        Item Drop) {
        public static ReplacementPlan KeyItem(ItemPlacement placement, Guid targetGuid, KeyItemRule rule)
            => new(ReplacementKind.KeyItem, placement, targetGuid, new Item(rule.Id, rule.Count));

        public static ReplacementPlan Filler(ItemPlacement placement, Guid targetGuid, Item drop)
            => new(ReplacementKind.Filler, placement, targetGuid, drop);
    }
}
