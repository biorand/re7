using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Services;
using Enums.app.Item;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class ItemModifier : Modifier {
    private readonly Randomizer _randomizer;

    public ItemModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private static readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;
    private static readonly AreaDefinitionRepository _areaDefinitions = AreaDefinitionRepository.Default;
    private static readonly HashSet<Guid> _birdCageGuids = [.. BirdCageModifier.Guids];
    private static readonly Guid _guestHouseAtticHandgunGuid = new("b09f6174-47f2-4851-acc3-3106d0902bfa");
    private static readonly Guid _guestHouseAtticHandgunInteractionGuid = new("abb411f5-de67-0b95-0a58-a794254c3417");

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        if (!randomizer.GetConfigOption<bool>("random-items"))
            return;

        var context = randomizer.StaticItemRandomizationService;
        var rng = context.Rng;
        var itemRandomizer = randomizer.ItemRandomizer;
        var itemPlacementService = randomizer.ItemPlacementService;
        var templateService = randomizer.TemplateService;
        var replaceMadhouseTapes = randomizer.GetConfigOption<bool>("replace-madhouse-tapes")
                                   || MadhouseSaveModifier.IsEnabled(randomizer);
        var replaceWeapons = randomizer.GetConfigOption<bool>("replace-weapons");
        var preserveItemModels = randomizer.GetConfigOption<bool>("preserve-item-models");
        var randomItemSettings = context.RandomItemSettings;
        var templateInstanceRng = randomizer.GetRng("modifier/static-items/template-instances");

        var candidates = GetIndexedItemPlacements(randomizer, itemPlacementService)
            .SelectMany(placement =>
                CreateCandidate(logger, itemRandomizer, placement, replaceMadhouseTapes, replaceWeapons))
            .DistinctBy(candidate => candidate.Key)
            .ToList();
        if (candidates.Count == 0)
            return;

        var replacements = CreateReplacementMap(candidates, itemRandomizer, rng, randomItemSettings);
        foreach (var areaGroup in candidates.GroupBy(candidate => candidate.AreaPath)) {
            var itemsToReplace = areaGroup
                .Where(candidate => replacements.ContainsKey(candidate.Key))
                .ToList();
            if (itemsToReplace.Count == 0)
                continue;

            logger.Push(FormatScenePath(areaGroup.Key));

            randomizer.FileRepository.ModifyScnFile(areaGroup.Key, scene => {
                var targetGuids = itemsToReplace
                    .Select(candidate => candidate.Placement.Guid)
                    .ToHashSet();
                var originalGameObjects = scene.FindGameObjectsByGuidWithFsmContext(targetGuids);
                var replacementGameObjects = new Dictionary<Guid, RszGameObject>();

                foreach (var candidate in itemsToReplace) {
                    var definition = candidate.Definition;
                    var placement = candidate.Placement;
                    var originalMatch = originalGameObjects[placement.Guid];
                    var originalGameObject = originalMatch.GameObject;
                    var originalTransform = originalGameObject.FindComponent<GeneratedViaTransform>();
                    var itemComponent = originalGameObject.FindComponent<app.Item>()!;
                    var drop = replacements[candidate.Key];

                    var replaceeName = _itemDefinitions.FromId(itemComponent.ItemDataID)?.Name ??
                                       definition.Name ?? itemComponent.ItemDataID;
                    var replacerName = _itemDefinitions.FromId(drop.Id)?.Name ?? drop.Id;
                    var quantity = itemComponent._IsOverwriteDifficultItemNumSetting
                        ? $"[{itemComponent._DifficultItemNumSetting.EasyNum}, {itemComponent.ItemStackNum}, {itemComponent._DifficultItemNumSetting.HardNum}]"
                        : itemComponent.ItemStackNum.ToString();
                    logger.LogLine($"Replacing {quantity}x {replaceeName} at {placement.Position} with " +
                                   $"[{drop.CountEasy}, {drop.CountNormal}, {drop.CountMadhouse}]x {replacerName}...");
                    logger.LogLine($"GUID: {originalGameObject.Guid}");
                    logger.LogLine($"Scene: {FormatScenePath(placement.SceneFile)}");

                    itemComponent.SaveGUID = rng.NextGuid(); // IMPORTANT!
                    itemComponent.ItemDataID = drop.Id;
                    itemComponent.ItemStackNum = drop.CountNormal;
                    itemComponent._IsOverwriteDifficultItemNumSetting = true;
                    itemComponent._DifficultItemNumSetting.EasyNum = drop.CountEasy;
                    itemComponent._DifficultItemNumSetting.HardNum = drop.CountMadhouse;
                    originalGameObject = originalGameObject.AddOrUpdateComponent(itemComponent);

                    var templateItemId = itemRandomizer.GetItemTemplateIdForDrop(drop.Id, rng, randomItemSettings);
                    var template = templateService.GetItemTemplate(templateItemId);

                    var replaceEventPickup = CanReplaceEventPickup(placement);
                    if (originalMatch.HasFsmInHierarchy && !replaceEventPickup) {
                        if (!preserveItemModels) {
                            originalGameObject = originalGameObject.ApplyVisualResourcesFromTemplate(template);
                        }

                        logger.LogLine(
                            "Preserving original pickup object shape because this placement is FSM-controlled.");
                        replacementGameObjects[originalGameObject.Guid] = originalGameObject;
                        continue;
                    }

                    Dictionary<Guid, Guid>? childGuidOverrides = null;
                    if (replaceEventPickup) {
                        var interaction = template.Children.Single(child =>
                            child.FindComponent("app.InteractDetailSearch") != null ||
                            child.FindComponent("app.InteractWeapon") != null);
                        childGuidOverrides = new() { [interaction.Guid] = _guestHouseAtticHandgunInteractionGuid };
                    }

                    var newGameObject = template.CloneWithNewGuids(templateInstanceRng, originalGameObject.Guid,
                        childGuidOverrides);
                    newGameObject = newGameObject.AddOrUpdateComponent(originalTransform);
                    newGameObject = newGameObject.AddOrUpdateComponent(itemComponent);

                    if (preserveItemModels) {
                        var mesh = originalGameObject.FindComponent("via.render.Mesh");
                        if (mesh != null) {
                            newGameObject = newGameObject.AddOrUpdateComponent(mesh);
                        }
                    }

                    newGameObject = newGameObject
                        .PreparePickupInteractionsForPlacement()
                        .PrepareWeaponPickupInteractionGameObjects();

                    newGameObject = newGameObject.WithSettings(
                        newGameObject.Settings
                            .Set("Update", originalGameObject.Settings.Get<bool>("Update"))
                            .Set("Draw", originalGameObject.Settings.Get<bool>("Draw"))
                    );

                    replacementGameObjects[originalGameObject.Guid] = newGameObject.WithGuid(originalGameObject.Guid);
                }

                return ReplaceGameObjects(scene, replacementGameObjects);
            });

            logger.Pop();
        }
    }

    private static bool CanReplaceEventPickup(ItemPlacement placement) {
        return placement.Guid == _guestHouseAtticHandgunGuid &&
               placement.SceneFile.Equals("natives/stm/environment/scene/chapter1/c01_3f.scn.20",
                   StringComparison.OrdinalIgnoreCase);
    }

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

    private static IEnumerable<ItemPlacement> GetIndexedItemPlacements(Randomizer randomizer,
        ItemPlacementService itemPlacementService) {
        var targetRepository = AreaSceneTargetRepository.Default;
        if (targetRepository.All.Count == 0) {
            foreach (var area in randomizer.AreaService.Areas) {
                foreach (var itemGameObject in area.Items) {
                    foreach (var placement in itemPlacementService.FromSceneGuid(area.Path, itemGameObject.Guid)) {
                        yield return placement;
                    }
                }
            }

            yield break;
        }

        var eligibleScenePaths = AreaDefinitionRepository.Default.All
            .Where(area => area.Dlc == null)
            .Select(area => area.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var targets in targetRepository.All) {
            if (!eligibleScenePaths.Contains(targets.Path))
                continue;

            foreach (var itemGuid in targets.GetItemGuids()) {
                if (!itemPlacementService.HasItem(itemGuid))
                    continue;

                foreach (var placement in itemPlacementService.FromSceneGuid(targets.Path, itemGuid)) {
                    yield return placement;
                }
            }
        }
    }

    private static IEnumerable<ItemReplacementCandidate> CreateCandidate(
        RandomizerLogger logger,
        ItemRandomizer itemRandomizer,
        ItemPlacement placement,
        bool replaceMadhouseTapes,
        bool replaceWeapons) {
        if (placement.Dlc != null
            || placement.IsExtra
            || !placement.Enabled
            || placement.Tags.Contains(ItemPlacement.ExcludeTag)
            || _birdCageGuids.Contains(placement.Guid)) {
            yield break;
        }

        var definition = _itemDefinitions.FromId(placement.Id);
        if (definition == null || !itemRandomizer.IsItemAllowed(definition))
            yield break;

        if (!replaceMadhouseTapes && definition.Id == "SaveTape") {
            logger.LogLine(
                $"NOT replacing Madhouse cassette tape at {placement.Position} in {FormatScenePath(placement.SceneFile)}");
            logger.LogLine($"GUID: {placement.Guid}");
            yield break;
        }

        if (!replaceWeapons && definition.IsWeapon) {
            logger.LogLine(
                $"NOT replacing weapon \"{definition.Name}\" at {placement.Position} in {FormatScenePath(placement.SceneFile)}");
            logger.LogLine($"GUID: {placement.Guid}");
            yield break;
        }

        yield return new ItemReplacementCandidate(placement.SceneFile, definition, placement);
    }

    private static string FormatScenePath(string path)
        => _areaDefinitions.FormatScenePath(path);

    private static Dictionary<ReplacementKey, Item> CreateReplacementMap(
        IReadOnlyList<ItemReplacementCandidate> candidates,
        ItemRandomizer itemRandomizer,
        Rng rng,
        RandomItemSettings randomItemSettings) {
        var result = new Dictionary<ReplacementKey, Item>();

        foreach (var chapterGroup in candidates.GroupBy(candidate => candidate.Placement.Chapter)
                     .OrderBy(group => group.Key)) {
            var availableCandidates = chapterGroup.ToList();
            foreach (var valuableDrop in itemRandomizer.GetValuableDrops(rng, "item-drop")) {
                var target = TakeRandomHighValueItem(availableCandidates, rng);
                if (target == null) {
                    break;
                }

                result[target.Key] = valuableDrop.Item;
            }

            foreach (var candidate in availableCandidates) {
                result[candidate.Key] = itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
            }
        }

        return result;
    }

    private static ItemReplacementCandidate?
        TakeRandomHighValueItem(List<ItemReplacementCandidate> candidates, Rng rng) {
        if (candidates.Count == 0) {
            return null;
        }

        var bestScore = candidates.Max(GetValuablePlacementScore);
        var bestCandidates = candidates
            .Where(candidate => GetValuablePlacementScore(candidate) == bestScore)
            .ToArray();
        var target = rng.Next(bestCandidates);
        candidates.Remove(target);
        return target;
    }

    private static int GetValuablePlacementScore(ItemReplacementCandidate candidate) {
        var definition = candidate.Definition;
        return definition.Id switch{
            "RepairKit" or "CylinderKey" => 100,
            "PowerUpCoin01A" or "PowerUpCoin01B" => 90,
            "Stimulant" or "Depressant" => 70,
            _ when definition.IsWeapon => 95,
            _ when ItemDrops.GetCategory(definition.Id) == ItemDrops.CategoryCoin => 90,
            _ when definition.CategoryType is ItemCategoryType.KeyItem
                or ItemCategoryType.UsableKeyItem
                or ItemCategoryType.DiscardableKeyItem => 85,
            _ when definition.CategoryType == ItemCategoryType.Shell => 20,
            _ => 50,
        };
    }

    private readonly record struct ReplacementKey(string AreaPath, Guid Guid);

    private sealed record ItemReplacementCandidate(
        string AreaPath,
        ItemDefinition Definition,
        ItemPlacement Placement) {
        public ReplacementKey Key => new(AreaPath, Placement.Guid);
    }
}
