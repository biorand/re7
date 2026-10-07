using Biohazard.BioRand.RE7.Services;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class EnemyMultiplierModifier : Modifier {
    private readonly Randomizer _randomizer;

    public EnemyMultiplierModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/enemy-multiplier";

    internal sealed record EnemySpawnSlot(
        Guid SpawnInfoGuid,
        Guid SpawnInfoParentGuid,
        Guid EnemyPoolGuid,
        Guid GenerationGameObjectGuid,
        string UnitAlias,
        RszGameObject SpawnInfoGameObject,
        RszGameObject GenerationGameObject
    );

    internal sealed record EnemySpawnGroup(
        Guid GenerationGameObjectGuid,
        RszGameObject GenerationGameObject,
        ImmutableArray<EnemySpawnSlot> SpawnSlots
    );

    internal sealed record EnemyGenerateSlot(
        Guid SpawnInfoGuid,
        Guid GenerationGameObjectGuid,
        string UnitAlias,
        RszGameObject GenerationGameObject
    );

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var multiplier = randomizer.GetConfigOption("enemy-multiplier", 1.0);
        if (multiplier == 1.0) {
            logger.LogLine("Not running modifier with default modifier of 1.0.");
            return;
        }

        var enemyLimitService = randomizer.EnemySceneLimitService;
        var rng = randomizer.GetRng(RandomizerKey);
        foreach (var scenePath in GetCandidateScenePaths(randomizer)) {
            if (!ScriptedSceneSafety.AllowsEnemyMutation(scenePath)) {
                logger.LogLine($"Skipping enemy multiplier in scripted flashback scene {scenePath}.");
                continue;
            }

            var scnFile = randomizer.FileRepository.GetScnFile(scenePath)
                .ToBuilder(randomizer.FileRepository.TypeRepository);
            var updatedScene = ProcessScene(
                scnFile.Scene,
                randomizer,
                logger,
                scenePath,
                multiplier,
                rng,
                enemyLimitService.GetMaxEnemiesForScene(scenePath),
                enemyLimitService);
            if (!ReferenceEquals(updatedScene, scnFile.Scene)) {
                scnFile.Scene = updatedScene;
                randomizer.FileRepository.SetScnFile(scenePath, scnFile.AddMissingResources().Build());
            }
        }
    }

    private static IEnumerable<string> GetCandidateScenePaths(Randomizer randomizer) {
        var targetRepository = AreaSceneTargetRepository.Default;
        if (targetRepository.All.Count == 0) {
            return randomizer.AreaService.Areas.Select(area => area.Path);
        }

        var areaPaths = AreaDefinitionRepository.Default.All
            .Where(area => area.Dlc == null)
            .Select(area => area.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return targetRepository.All
            .Where(targets =>
                targets.GetEnemySpawnInfoGuids().Count != 0 ||
                targets.GetEnemyGenerateGuids().Count != 0)
            .Select(targets => targets.Path)
            .Where(areaPaths.Contains)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    internal static RszScene ProcessScene(
        RszScene scene,
        Randomizer randomizer,
        RandomizerLogger logger,
        string scenePath,
        double multiplier,
        Rng rng,
        int? maxEnemyCount = null,
        EnemySceneLimitService? enemyLimitService = null) {
        var allSlots = CollectMultipliableSpawnSlots(scene);
        var placements = randomizer.EnemyPlacementService;
        var slots = allSlots.Where(slot => {
            var rule = placements.GetRule(scenePath, slot.SpawnInfoGuid);
            return !rule.Preserve && !rule.Cull;
        }).ToImmutableArray();
        var allLimitableSlots = maxEnemyCount == null
            ? []
            : CollectLimitableSpawnSlots(scene, enemyLimitService);
        var limitableSlots = allLimitableSlots.Where(slot => {
            var rule = placements.GetSpawnRule(slot.SpawnInfoGuid);
            return !rule.Preserve && !rule.Cull;
        }).ToImmutableArray();
        var preservedCount = allLimitableSlots.IsDefaultOrEmpty
            ? allSlots.Count(slot => placements.GetRule(scenePath, slot.SpawnInfoGuid).Preserve)
            : allLimitableSlots.Count(slot => placements.GetSpawnRule(slot.SpawnInfoGuid).Preserve);
        var currentEnemyCount = limitableSlots.IsDefaultOrEmpty
            ? slots.Length
            : limitableSlots.Length;

        if (currentEnemyCount == 0)
            return scene;

        var uncappedTargetCount = GetTargetEnemyCount(currentEnemyCount, multiplier);
        var targetCount = maxEnemyCount == null
            ? uncappedTargetCount
            : ApplyMaxEnemyCount(currentEnemyCount, uncappedTargetCount, Math.Max(0, maxEnemyCount.Value - preservedCount));
        if (targetCount == currentEnemyCount)
            return scene;

        var limitLabel = maxEnemyCount != null && targetCount != uncappedTargetCount
            ? $", limit {Math.Max(0, maxEnemyCount.Value)}"
            : "";
        logger.Push($"{scenePath}: enemy multiplier {currentEnemyCount} => {targetCount}{limitLabel}");
        if (targetCount < currentEnemyCount) {
            scene = limitableSlots.IsDefaultOrEmpty
                ? RemoveSpawnSlots(scene, slots, currentEnemyCount - targetCount, logger, rng)
                : DisableGenerateSlots(scene, limitableSlots, currentEnemyCount - targetCount, logger, rng);
        } else if (slots.Length != 0) {
            var duplicableSlots = slots.Where(slot =>
                !placements.GetRule(scenePath, slot.SpawnInfoGuid).NoDuplicates).ToImmutableArray();
            if (!duplicableSlots.IsDefaultOrEmpty) {
                scene = AddSpawnSlots(scene, randomizer, duplicableSlots, targetCount - currentEnemyCount, logger, rng);
            }
        }

        logger.Pop();

        return scene;
    }

    internal static int GetTargetEnemyCount(int currentEnemyCount, double multiplier) {
        if (currentEnemyCount <= 0)
            return 0;

        var safeMultiplier = Math.Max(0.0, multiplier);
        return Math.Max(0, (int)Math.Round(currentEnemyCount * safeMultiplier, MidpointRounding.AwayFromZero));
    }

    internal static int ApplyMaxEnemyCount(int currentEnemyCount, int uncappedTargetCount, int maxEnemyCount) {
        var safeMaxEnemyCount = Math.Max(0, maxEnemyCount);
        if (uncappedTargetCount >= currentEnemyCount) {
            return Math.Max(currentEnemyCount, Math.Min(uncappedTargetCount, safeMaxEnemyCount));
        }

        return Math.Min(uncappedTargetCount, safeMaxEnemyCount);
    }

    internal static ImmutableArray<EnemySpawnSlot> CollectMultipliableSpawnSlots(RszScene scene)
        => CollectMultipliableSpawnGroups(scene)
            .SelectMany(group => group.SpawnSlots)
            .ToImmutableArray();

    internal static ImmutableArray<EnemyGenerateSlot> CollectLimitableSpawnSlots(
        RszScene scene,
        EnemySceneLimitService? enemyLimitService = null) {
        var spawnInfoAliases = new Dictionary<Guid, string>();
        scene.VisitGameObjects(gameObject => {
            if (EnemySpawnInfoRules.ShouldReplaceSpawnInfo(gameObject)) {
                var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>()!;
                spawnInfoAliases[gameObject.Guid] = spawnInfo.UnitAlias;
            }
        });

        var slots = ImmutableArray.CreateBuilder<EnemyGenerateSlot>();
        scene.VisitGameObjects(gameObject => {
            if (!IsGenerationGameObject(gameObject))
                return;

            foreach (var spawnInfoGuid in GetEnabledEnemyGenerateSpawnInfoRefs(gameObject).Distinct()) {
                if (!spawnInfoAliases.TryGetValue(spawnInfoGuid, out var unitAlias)) {
                    if (enemyLimitService?.TryGetVanillaSpawnInfo(spawnInfoGuid, out var placement) != true)
                        continue;

                    unitAlias = placement.UnitAlias;
                }

                slots.Add(new EnemyGenerateSlot(
                    spawnInfoGuid,
                    gameObject.Guid,
                    unitAlias,
                    gameObject));
            }
        });

        return slots.ToImmutable();
    }

    internal static ImmutableArray<EnemySpawnGroup> CollectMultipliableSpawnGroups(RszScene scene) {
        var spawnInfos = new Dictionary<Guid, RszGameObject>();
        scene.VisitGameObjects(gameObject => {
            if (EnemySpawnInfoRules.ShouldReplaceSpawnInfo(gameObject)) {
                spawnInfos[gameObject.Guid] = gameObject;
            }
        });

        if (spawnInfos.Count == 0)
            return [];

        var parentByChild = BuildParentMap(scene);
        var generationObjects = new List<(RszGameObject GameObject, ImmutableArray<Guid> SpawnInfoRefs)>();
        var generationObjectsBySpawnInfo = new Dictionary<Guid, List<RszGameObject>>();
        scene.VisitGameObjects(gameObject => {
            if (!IsGenerationGameObject(gameObject))
                return;

            var referencedSpawnInfos = GetEnabledEnemyGenerateSpawnInfoRefs(gameObject)
                .Where(spawnInfos.ContainsKey)
                .Distinct()
                .ToImmutableArray();

            if (referencedSpawnInfos.IsDefaultOrEmpty)
                return;

            generationObjects.Add((gameObject, referencedSpawnInfos));
            foreach (var spawnInfoGuid in referencedSpawnInfos) {
                if (!generationObjectsBySpawnInfo.TryGetValue(spawnInfoGuid, out var generationRefs)) {
                    generationRefs = [];
                    generationObjectsBySpawnInfo[spawnInfoGuid] = generationRefs;
                }

                generationRefs.Add(gameObject);
            }
        });

        var groups = ImmutableArray.CreateBuilder<EnemySpawnGroup>();
        foreach (var (generationGameObject, referencedSpawnInfos) in generationObjects) {
            var slots = ImmutableArray.CreateBuilder<EnemySpawnSlot>();
            foreach (var spawnInfoGuid in referencedSpawnInfos) {
                if (!spawnInfos.TryGetValue(spawnInfoGuid, out var spawnInfoGameObject) ||
                    !generationObjectsBySpawnInfo.TryGetValue(spawnInfoGuid, out var generationRefs) ||
                    generationRefs.Count != 1 ||
                    !parentByChild.TryGetValue(spawnInfoGuid, out var spawnInfoParentGuid)) {
                    continue;
                }

                var enemyPoolGuid = FindAncestorWithComponent(parentByChild, scene, spawnInfoGuid, "app.EnemyPool");
                if (enemyPoolGuid == null)
                    continue;

                var spawnInfo = spawnInfoGameObject.FindComponent<app.EnemySpawnInfo>()!;
                slots.Add(new EnemySpawnSlot(
                    spawnInfoGuid,
                    spawnInfoParentGuid,
                    enemyPoolGuid.Value,
                    generationGameObject.Guid,
                    spawnInfo.UnitAlias,
                    spawnInfoGameObject,
                    generationGameObject));
            }

            if (slots.Count != 0) {
                groups.Add(new EnemySpawnGroup(
                    generationGameObject.Guid,
                    generationGameObject,
                    slots.ToImmutable()));
            }
        }

        return groups.ToImmutable();
    }

    internal static ImmutableArray<Guid> GetEnabledEnemyGenerateSpawnInfoRefs(RszGameObject gameObject) {
        var result = ImmutableArray.CreateBuilder<Guid>();
        gameObject.Visit(node => {
            if (node is not RszObjectNode objectNode ||
                !IsEnemyGenerateAction(objectNode) ||
                !IsEnemyGenerateEnabled(objectNode)) {
                return;
            }

            var spawnInfoGuid = GetEnemyGenerateSpawnInfo(objectNode);
            if (spawnInfoGuid != Guid.Empty)
                result.Add(spawnInfoGuid);
        });
        return result.ToImmutable();
    }

    private static RszScene RemoveSpawnSlots(
        RszScene scene,
        ImmutableArray<EnemySpawnSlot> slots,
        int removeCount,
        RandomizerLogger logger,
        Rng rng) {
        var removedSlots = SelectRandomSlotsWithoutReplacement(slots, removeCount, rng);
        var removedSpawnInfosByGeneration = removedSlots
            .GroupBy(slot => slot.GenerationGameObjectGuid)
            .ToDictionary(
                group => group.Key,
                group => group.Select(slot => slot.SpawnInfoGuid).ToHashSet());

        foreach (var (generationGameObjectGuid, removedSpawnInfoGuids) in removedSpawnInfosByGeneration) {
            var generationGameObject = scene.FindGameObject(generationGameObjectGuid);
            if (generationGameObject != null) {
                scene = scene.UpdateGameObject(DisableEnemyGenerateActions(generationGameObject,
                    removedSpawnInfoGuids));
            }
        }

        foreach (var slot in removedSlots) {
            logger.LogLine($"Removing {slot.UnitAlias} ({slot.SpawnInfoGuid})");
            scene = scene.RemoveGameObject(slot.SpawnInfoGuid);
        }

        return scene;
    }

    private static RszScene DisableGenerateSlots(
        RszScene scene,
        ImmutableArray<EnemyGenerateSlot> slots,
        int removeCount,
        RandomizerLogger logger,
        Rng rng) {
        var removedSlots = SelectRandomSlotsWithoutReplacement(slots, removeCount, rng);
        var removedSpawnInfosByGeneration = removedSlots
            .GroupBy(slot => slot.GenerationGameObjectGuid)
            .ToDictionary(
                group => group.Key,
                group => group.Select(slot => slot.SpawnInfoGuid).ToHashSet());

        foreach (var (generationGameObjectGuid, removedSpawnInfoGuids) in removedSpawnInfosByGeneration) {
            var generationGameObject = scene.FindGameObject(generationGameObjectGuid);
            if (generationGameObject != null) {
                scene = scene.UpdateGameObject(DisableEnemyGenerateActions(generationGameObject,
                    removedSpawnInfoGuids));
            }
        }

        foreach (var slot in removedSlots) {
            logger.LogLine($"Disabling {slot.UnitAlias} ({slot.SpawnInfoGuid})");
        }

        return scene;
    }

    private static RszScene AddSpawnSlots(
        RszScene scene,
        Randomizer randomizer,
        ImmutableArray<EnemySpawnSlot> slots,
        int addCount,
        RandomizerLogger logger,
        Rng rng) {
        for (var i = 0; i < addCount; i++) {
            var sourceSlot = rng.Next(slots);
            scene = DuplicateSpawnSlot(scene, randomizer, sourceSlot, logger, rng);
        }

        return scene;
    }

    private static RszScene DuplicateSpawnSlot(
        RszScene scene,
        Randomizer randomizer,
        EnemySpawnSlot sourceSlot,
        RandomizerLogger logger,
        Rng rng) {
        var spawnInfoClone = CloneGameObject(sourceSlot.SpawnInfoGameObject, rng)
            .WithName(sourceSlot.SpawnInfoGameObject.Name + "_BioRandMultiplier");
        spawnInfoClone = EnemyTemplateFactory.RefreshRuntimeGuids(spawnInfoClone,
            randomizer.GetRng("enemy-multiplier/spawn-state", spawnInfoClone.Guid));

        var enemyInstanceClone = CreateEnemyInstanceClone(scene, randomizer, sourceSlot, rng);
        if (enemyInstanceClone == null) {
            // TODO External pool ref?
            // Maybe don't skip...
            logger.LogLine(
                $"Skipped multiplying {sourceSlot.UnitAlias}: unable to find or create a pooled enemy instance.");
            return scene;
        }

        var spawnInfoParent = scene.FindGameObject(sourceSlot.SpawnInfoParentGuid);
        if (spawnInfoParent == null) {
            logger.LogLine(
                $"Skipped multiplying {sourceSlot.UnitAlias}: missing spawn info parent {sourceSlot.SpawnInfoParentGuid}.");
            return scene;
        }

        spawnInfoParent = spawnInfoParent.WithChildren(spawnInfoParent.Children.Add(spawnInfoClone));
        scene = scene.UpdateGameObject(spawnInfoParent);

        var enemyPool = scene.FindGameObject(sourceSlot.EnemyPoolGuid);
        if (enemyPool == null) {
            logger.LogLine(
                $"Skipped multiplying {sourceSlot.UnitAlias}: missing enemy pool {sourceSlot.EnemyPoolGuid}.");
            return scene;
        }

        enemyPool = enemyPool.WithChildren(enemyPool.Children.Add(enemyInstanceClone));
        scene = scene.UpdateGameObject(enemyPool);

        var spawnInfoMap = new Dictionary<Guid, Guid>{
            [sourceSlot.SpawnInfoGuid] = spawnInfoClone.Guid
        };
        var generationClone = CloneGameObject(sourceSlot.GenerationGameObject, rng)
            .WithName(sourceSlot.GenerationGameObject.Name + "_BioRandMultiplier");

        generationClone = ConfigureEnemyGenerateActions(generationClone, spawnInfoMap);
        generationClone = ReplaceGameObjectRefs(generationClone, spawnInfoMap);
        generationClone = RefreshGenerationObjectInstanceIds(generationClone, rng);

        logger.LogLine($"Duplicating {sourceSlot.UnitAlias}: {sourceSlot.SpawnInfoGuid} => {spawnInfoClone.Guid}");
        return AddSiblingAfter(scene, sourceSlot.GenerationGameObjectGuid, generationClone);
    }

    private static RszGameObject? CreateEnemyInstanceClone(
        RszScene scene,
        Randomizer randomizer,
        EnemySpawnSlot sourceSlot,
        Rng rng) {
        var enemyPool = scene.FindGameObject(sourceSlot.EnemyPoolGuid);
        if (enemyPool == null)
            return null;

        var existingInstance = enemyPool.Children
            .Skip(1)
            .FirstOrDefault(child => IsEnemyInstance(child) && IsMatchingEnemyInstance(child, sourceSlot.UnitAlias));

        if (existingInstance != null) {
            var clone = CloneGameObject(existingInstance, rng).WithName(existingInstance.Name + "_BioRandMultiplier");
            return EnemyTemplateFactory.RefreshRuntimeGuids(clone,
                randomizer.GetRng("enemy-multiplier/instance-state", clone.Guid));
        }

        try {
            var clone = CloneGameObject(randomizer.TemplateService.GetEnemyTemplate(sourceSlot.UnitAlias), rng);
            return EnemyTemplateFactory.RefreshRuntimeGuids(clone,
                randomizer.GetRng("enemy-multiplier/instance-state", clone.Guid));
        }
        catch {
            return null;
        }
    }

    private static RszGameObject DisableEnemyGenerateActions(
        RszGameObject generationGameObject,
        HashSet<Guid> spawnInfoGuids) {
        return generationGameObject.Visit(node => {
            if (node is RszObjectNode objectNode &&
                IsEnemyGenerateAction(objectNode) &&
                spawnInfoGuids.Contains(GetEnemyGenerateSpawnInfo(objectNode))) {
                return DisableEnemyGenerateAction(objectNode);
            }

            return node;
        });
    }

    private static RszGameObject ConfigureEnemyGenerateActions(
        RszGameObject generationGameObject,
        Dictionary<Guid, Guid> spawnInfoMap) {
        return generationGameObject.Visit(node => {
            if (node is not RszObjectNode objectNode || !IsEnemyGenerateAction(objectNode))
                return node;

            var spawnInfoGuid = GetEnemyGenerateSpawnInfo(objectNode);
            if (spawnInfoMap.TryGetValue(spawnInfoGuid, out var newSpawnInfoGuid)) {
                return objectNode
                    .SetField("v0_Enabled", true)
                    .SetField("SpawnInfo", newSpawnInfoGuid);
            }

            return DisableEnemyGenerateAction(objectNode);
        });
    }

    private static RszObjectNode DisableEnemyGenerateAction(RszObjectNode objectNode)
        => objectNode
            .SetField("v0_Enabled", false)
            .SetField("SpawnInfo", Guid.Empty);

    private static bool IsEnemyGenerateAction(RszObjectNode objectNode)
        => objectNode.Type.Name == "app.fsm.EnemyGenerate";

    private static bool IsEnemyGenerateEnabled(RszObjectNode objectNode)
        => objectNode.Type.FindFieldIndex("v0_Enabled") == -1 ||
           RszSerializer.Deserialize<bool>(objectNode["v0_Enabled"]);

    private static Guid GetEnemyGenerateSpawnInfo(RszObjectNode objectNode)
        => objectNode.Type.FindFieldIndex("SpawnInfo") == -1
            ? Guid.Empty
            : RszSerializer.Deserialize<Guid>(objectNode["SpawnInfo"]);

    private static bool IsGenerationGameObject(RszGameObject gameObject)
        => gameObject.FindComponent("via.fsm.Fsm") != null &&
           gameObject.FindComponent("app.TriggerInAction") != null;

    private static bool IsEnemyInstance(RszGameObject gameObject) {
        var result = false;
        gameObject.VisitGameObjects(child => {
            var mesh = child.FindComponent("via.render.Mesh");
            if (mesh != null &&
                mesh.Children.Length > 2 &&
                mesh.Children[2]?.ToString()
                    ?.StartsWith("Character/Enemy/", StringComparison.InvariantCultureIgnoreCase) == true) {
                result = true;
            }
        });
        return result;
    }

    private static bool IsMatchingEnemyInstance(RszGameObject gameObject, string unitAlias)
        => gameObject.Name.Contains(unitAlias, StringComparison.OrdinalIgnoreCase);

    private static ImmutableArray<T> SelectRandomSlotsWithoutReplacement<T>(
        ImmutableArray<T> slots,
        int count,
        Rng rng) {
        var remainingSlots = slots.ToList();
        var selectedSlots = ImmutableArray.CreateBuilder<T>(Math.Min(count, slots.Length));
        while (selectedSlots.Count < count && remainingSlots.Count > 0) {
            var selectedSlot = rng.Next(remainingSlots);
            selectedSlots.Add(selectedSlot);
            remainingSlots.Remove(selectedSlot);
        }

        return selectedSlots.ToImmutable();
    }

    private static ImmutableDictionary<Guid, Guid> BuildParentMap(RszScene scene) {
        var result = ImmutableDictionary.CreateBuilder<Guid, Guid>();
        scene.VisitGameObjects(parent => {
            foreach (var child in parent.Children) {
                result[child.Guid] = parent.Guid;
            }
        });
        return result.ToImmutable();
    }

    private static Guid? FindAncestorWithComponent(
        ImmutableDictionary<Guid, Guid> parentByChild,
        RszScene scene,
        Guid childGuid,
        string componentName) {
        var currentGuid = childGuid;
        while (parentByChild.TryGetValue(currentGuid, out var parentGuid)) {
            var parent = scene.FindGameObject(parentGuid);
            if (parent?.FindComponent(componentName) != null)
                return parentGuid;

            currentGuid = parentGuid;
        }

        return null;
    }

    // RszGameObject.Clone() uses Guid.NewGuid()
    // This keeps multiplier output seed-deterministic instead
    private static RszGameObject CloneGameObject(RszGameObject rootGameObject, Rng rng) {
        var guidMap = new Dictionary<Guid, Guid>();
        var root = rootGameObject.VisitGameObjects(gameObject => {
            var newGuid = rng.NextGuid();
            guidMap[gameObject.Guid] = newGuid;
            return gameObject.WithGuid(newGuid);
        });

        return ReplaceGameObjectRefs(root, guidMap);
    }

    private static RszGameObject ReplaceGameObjectRefs(
        RszGameObject gameObject,
        Dictionary<Guid, Guid> guidMap) {
        return gameObject.Visit(node => {
            if (node is RszValueNode valueNode && valueNode.Type == RszFieldType.GameObjectRef) {
                var refGuid = RszSerializer.Deserialize<Guid>(valueNode);
                if (guidMap.TryGetValue(refGuid, out var newGuid)) {
                    return RszSerializer.Serialize(RszFieldType.GameObjectRef, newGuid);
                }
            }

            return node;
        });
    }

    private static RszGameObject RefreshGenerationObjectInstanceIds(RszGameObject gameObject, Rng rng) {
        return gameObject.VisitComponents(component => {
            if (component.Type.Name == "via.fsm.Fsm" && component.Type.FindFieldIndex("InstanceGuid") != -1)
                return component.SetField("InstanceGuid", rng.NextGuid());

            if (component.Type.Name == "app.TriggerInAction" && component.Type.FindFieldIndex("SaveGUID") != -1)
                return component.SetField("SaveGUID", rng.NextGuid());

            return component;
        });
    }

    private static T AddSiblingAfter<T>(
        T node,
        Guid existingSiblingGuid,
        RszGameObject newSibling)
        where T : IRszSceneNode {
        if (node.Children.IsDefaultOrEmpty)
            return node;

        var children = node.Children.ToBuilder();
        for (var i = 0; i < children.Count; i++) {
            if (children[i] is RszGameObject gameObject && gameObject.Guid == existingSiblingGuid) {
                children.Insert(i + 1, newSibling);
                return (T)node.WithChildren(children.ToImmutable());
            }

            children[i] = AddSiblingAfter(children[i], existingSiblingGuid, newSibling);
        }

        return (T)node.WithChildren(children.ToImmutable());
    }
}
