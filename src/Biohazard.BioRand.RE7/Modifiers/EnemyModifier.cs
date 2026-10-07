using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class EnemyModifier : Modifier {
    private readonly Randomizer _randomizer;

    public EnemyModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/enemies";
    internal const string EnemyForceTargetingProbabilityConfigKey = "enemy-force-targeting-probability";
    internal const string ExtraEnemyGeneratorName = ExtraEnemySceneBuilder.GeneratorName;
    internal const string ExtraEnemyPoolName = ExtraEnemySceneBuilder.PoolName;
    internal const string ExtraEnemySpawnPointsName = ExtraEnemySceneBuilder.SpawnPointsName;
    internal const string ExtraEnemySpawnInfoPrefix = ExtraEnemySceneBuilder.SpawnInfoPrefix;
    internal const string ExtraEnemyGeneratePrefix = ExtraEnemySceneBuilder.GeneratePrefix;
    internal const string ExtraEnemyStaticPrefix = ExtraEnemySceneBuilder.StaticPrefix;

    private static EnemyRandomizerOptions BuildOptions(Randomizer randomizer) {
        return new EnemyRandomizerOptions(
            EnemyVariety: randomizer.GetConfigOption<int>("enemy-variety"),
            MaxPackSize: randomizer.GetConfigOption<int>("enemy-pack-max-size"),
            DebugUniqueHp: randomizer.GetConfigOption<bool>("debug-unique-enemy-hp"),
            IsBalanced: randomizer.GetConfigOption<bool>("balanced-enemies"),
            ScaleOptions: new ScaleOptions(
                Probability: randomizer.GetConfigOption<double>("enemy-scale-probability", 0),
                Min: Math.Clamp(randomizer.GetConfigOption("enemy-scale-min", 0.25f), 0.1f, 10.0f),
                Max: Math.Clamp(randomizer.GetConfigOption("enemy-scale-max", 2.00f), 0.1f, 10.0f)
            ),
            ForceTargetingProbability: Math.Clamp(
                randomizer.GetConfigOption(EnemyForceTargetingProbabilityConfigKey, 0.0),
                0.0,
                1.0)
        );
    }

    private static RszObjectNode RandomizeForceTargetingOption(
        RszObjectNode component,
        EnemyRandomizerOptions options,
        Rng rng) {
        if (options.ForceTargetingProbability <= 0 ||
            !EnemySpawnInfoRules.SupportsForceTargetingOption(component)) {
            return component;
        }

        return component.SetField(
            "IsForceTargetingToPlayer",
            rng.NextProbability(options.ForceTargetingProbability));
    }

    private static void CopySpecifiedRankParameter(
        app.EnemySpawnInfo target,
        app.EnemySpawnInfo source) {
        if (target.specifiedRankParameter == null || source.specifiedRankParameter == null)
            return;

        target.specifiedRankParameter.SpecifiedDirectivesName = source.specifiedRankParameter.SpecifiedDirectivesName;
        target.specifiedRankParameter.SpecifiedResistParameterName =
            source.specifiedRankParameter.SpecifiedResistParameterName;
        target.specifiedRankParameter.SpecifiedSlipParameterName =
            source.specifiedRankParameter.SpecifiedSlipParameterName;
    }

    private static RszGameObject ReplaceSpawnInfoOptions(
        RszGameObject spawnInfoGameObject,
        RszObjectNode newSpawnOption,
        RszObjectNode? dlcSpawnOption) {
        var components = spawnInfoGameObject.Components
            .Where(component => !EnemyTemplateFactory.IsSpawnInfoOption(component))
            .ToImmutableArray()
            .Add(newSpawnOption);

        if (dlcSpawnOption != null) {
            components = components.Add(dlcSpawnOption);
        }

        return spawnInfoGameObject.WithComponents(components);
    }

    private static RszScene RandomizeForceTargetingOptions(
        RszScene scene,
        string scenePath,
        Services.EnemyPlacementService placements,
        EnemyRandomizerOptions options,
        Rng rng,
        out int changedCount) {
        var updatedGuids = new HashSet<Guid>();
        var updatedScene = scene.VisitComponents((gameObject, component) => {
            if (gameObject.FindComponent<app.EnemySpawnInfo>() == null) {
                return component;
            }

            var rule = placements.GetRule(scenePath, gameObject.Guid);
            if (rule.Cull || (!rule.Aggro && (rule.Preserve || options.ForceTargetingProbability <= 0)))
                return component;

            if (rule.Aggro && component.Type.FindFieldIndex("IsPlayerTargetingAtStart") != -1) {
                if (component.Get<bool>("IsPlayerTargetingAtStart"))
                    return component;
                updatedGuids.Add(gameObject.Guid);
                return component.SetField("IsPlayerTargetingAtStart", true);
            }
            if (!EnemySpawnInfoRules.SupportsForceTargetingOption(component))
                return component;

            var updated = rule.Aggro
                ? component.SetField("IsForceTargetingToPlayer", true)
                : RandomizeForceTargetingOption(component, options, rng);
            if (updated.Get<bool>("IsForceTargetingToPlayer") == component.Get<bool>("IsForceTargetingToPlayer"))
                return component;
            updatedGuids.Add(gameObject.Guid);
            return updated;
        });

        changedCount = updatedGuids.Count;
        return updatedScene;
    }

    private RszScene ProcessGeneratorScene(
        RszScene scene,
        RandomizerLogger logger,
        EnemyTemplateFactory templateFactory,
        EnemyGeneratorWrapper enemyGenerator,
        IEnumerable<(Guid spawnGuid, IEnemyDefinition enemy)> replacements,
        EnemyRandomizerOptions options,
        Rng rng,
        EnemyHealthResolver healthResolver) {
        var pooledObjects = new List<RszGameObject>();

        foreach (var (spawnGuid, newEnemy) in replacements) {
            var enemyId = newEnemy.EnemyId.ToString();

            var originalSpawnInfoGameObject = scene.FindGameObject(spawnGuid)!;
            var originalTransform = originalSpawnInfoGameObject.FindComponent<GeneratedViaTransform>()!;
            var originalSpawnInfoComponent = originalSpawnInfoGameObject.FindComponent<app.EnemySpawnInfo>()!;

            if (newEnemy.UsesEnemyGenerator) {
                // Enemy that uses generator pool: Replace UnitAlias and associated pool GameObject.
                if (newEnemy.SpawnOptionType == null) {
                    throw new InvalidOperationException(
                        $"{newEnemy.Name} cannot replace EnemyGenerator spawns because it has no EnemySpawnInfoOption.");
                }

                var spawnInfoTemplate = templateFactory.GetOrCreateSpawnInfoTemplate(enemyId, rng);
                var spawnInfoTemplateComponent = spawnInfoTemplate.FindComponent<app.EnemySpawnInfo>()!;
                var newSpawnOptions = spawnInfoTemplate.FindComponent(newEnemy.SpawnOptionType)
                                      ?? throw new InvalidOperationException(
                                          $"Spawn info template for '{enemyId}' does not contain '{newEnemy.SpawnOptionType}'.");
                var dlcSpawnOptions = spawnInfoTemplate.FindComponent("app.EnemySpawnInfoOptionDLC");

                // Both definitions use Em4000; the option determines the blade variant.
                if (newEnemy.Id is "Molded" or "MoldedBlade")
                    newSpawnOptions = newSpawnOptions.SetField("IsUseBlade", newEnemy.Id == "MoldedBlade");

                originalSpawnInfoGameObject = ReplaceSpawnInfoOptions(
                    originalSpawnInfoGameObject,
                    newSpawnOptions,
                    dlcSpawnOptions);
                CopySpecifiedRankParameter(originalSpawnInfoComponent, spawnInfoTemplateComponent);

                var oldUnitAlias = originalSpawnInfoComponent.UnitAlias;
                var assignedHealth = healthResolver.GetHealth(newEnemy);
                originalSpawnInfoComponent.HealthParameter.Health = assignedHealth;
                originalSpawnInfoComponent.UnitAlias = enemyId;
                originalSpawnInfoGameObject = originalSpawnInfoGameObject
                    .AddOrUpdateComponent(originalSpawnInfoComponent)
                    .WithName(originalSpawnInfoGameObject.Name + "_Now_" + enemyId);

                scene = scene.UpdateGameObject(originalSpawnInfoGameObject);
                logger.LogSpawnHealthAssignment(
                    newEnemy,
                    assignedHealth,
                    "generator replacement",
                    originalSpawnInfoGameObject.Name,
                    spawnGuid,
                    $"PreviousAlias={oldUnitAlias}");

                var template = templateFactory.GetOrCreateEnemyTemplate(
                    enemyId,
                    originalTransform,
                    updateTransform: false,
                    randomizeScale: true,
                    options.ScaleOptions,
                    rng
                );
                pooledObjects.Add(template);
                pooledObjects.AddRange(
                    templateFactory.CreatePoolInstancesForNestedSpawnInfos(template, options.ScaleOptions, rng));
            } else {
                // Static enemy: remove SpawnInfo and insert template
                var template = templateFactory.GetOrCreateEnemyTemplate(
                        enemyId,
                        originalTransform,
                        updateTransform: true,
                        randomizeScale: true,
                        options.ScaleOptions,
                        rng)
                    .WithName($"{enemyId}_Static");

                scene = scene.RemoveGameObject(spawnGuid);
                scene = scene.Add(template);
            }
        }

        var generator = scene.FindGameObject(enemyGenerator.GameObject.Guid)!;

        var poolObject = generator.Children
            .Select(child => new{ Child = child, Pool = child.FindComponent<app.EnemyPool>() })
            .Where(x => x.Pool != null)
            .Select(x => x.Child)
            .Single();

        var poolComponent = poolObject.FindComponent<app.EnemyPool>()!;
        //poolComponent.ExternalInstancePoolRefs.Clear();

        var newChildren = poolObject.Children.ToList();

        foreach (var pooled in pooledObjects) {
            if (!newChildren.Any(c => c.Guid == pooled.Guid)) {
                newChildren.Add(pooled);
            }
        }

        poolObject = poolObject.WithChildren(newChildren.ToImmutableArray());

        poolObject = poolObject.AddOrUpdateComponent(poolComponent);

        scene = scene.UpdateGameObject(poolObject);

        return scene;
    }

    private void ProcessArea(
        Area area,
        Randomizer randomizer,
        RandomizerLogger logger,
        EnemyTemplateFactory templateFactory,
        IReadOnlyList<EnemyTableEntry> enemyPool,
        EnemyRandomizerOptions options,
        Rng rng,
        EnemyHealthResolver healthResolver) {
        logger.Push(area.Path);

        var balancedEnemyPool = options.IsBalanced
            ? BalancedEnemyPoolSelector.Select(enemyPool, area.Definition.Chapter, area.Path)
            : enemyPool.ToImmutableArray();
        if (balancedEnemyPool.IsDefaultOrEmpty) {
            logger.LogLine("Balanced enemy pool is empty for this area. Skipping enemy replacement.");
            logger.Pop();
            return;
        }

        var areaEnemyPool = EnemyPoolSelector.SelectAreaEnemyPool(balancedEnemyPool, options.EnemyVariety, rng);
        logger.LogLine(
            $"Area enemy pool ({areaEnemyPool.Length}/{enemyPool.Count}): {string.Join(", ", areaEnemyPool.Select(entry => entry.Enemy.Name))}");

        var generatorChanges =
            new List<(EnemyGeneratorWrapper Generator, List<(Guid, IEnemyDefinition)> Replacements)>();
        foreach (var enemyGenerator in area.EnemyGenerators) {
            var spawnInfos = enemyGenerator.EnemySpawnInfos;

            if (spawnInfos.Length == 0)
                continue;

            logger.Push($"Generator '{enemyGenerator.Generator.Alias}' ({spawnInfos.Length} EnemySpawnInfos)");

            var packSelectors = new Dictionary<string, EnemyPackSelector>(StringComparer.Ordinal);
            var replacements = new List<(Guid, IEnemyDefinition)>();
            foreach (var spawnInfo in spawnInfos) {
                var rule = randomizer.EnemyPlacementService.GetRule(area.Path, spawnInfo.Guid);
                if (rule.Preserve || rule.Cull || !EnemySpawnInfoRules.ShouldReplaceSpawnInfo(spawnInfo))
                    continue;

                var component = spawnInfo.FindComponent<app.EnemySpawnInfo>()!;
                var compatibleEnemyPool =
                    SelectCompatibleEnemyPool(area.Path, spawnInfo, areaEnemyPool, balancedEnemyPool, rule);
                if (compatibleEnemyPool.IsDefaultOrEmpty) {
                    logger.LogLine($"Keeping {component.UnitAlias} ({spawnInfo.Name}): no compatible replacement.");
                    continue;
                }

                var packSelector = GetPackSelector(packSelectors, compatibleEnemyPool, options.MaxPackSize, rng);
                var replacement = packSelector.Next();

                logger.LogLine($"Replacing {component.UnitAlias} with {replacement.Name} ({spawnInfo.Name})");
                replacements.Add((spawnInfo.Guid, replacement));
            }

            if (replacements.Count > 0) {
                generatorChanges.Add((enemyGenerator, replacements));
            }

            logger.Pop();
        }

        if (generatorChanges.Count > 0) {
            var scene = area.Scene;
            foreach (var (generator, replacements) in generatorChanges) {
                scene = ProcessGeneratorScene(scene, logger, templateFactory, generator, replacements, options, rng,
                    healthResolver);
            }

            area.Scene = scene;
            randomizer.FileRepository.SetScnFile(area.Path, area.ScnFile.AddMissingResources().Build());
        }

        logger.Pop();
    }

    internal static ImmutableArray<EnemyTableEntry> SelectCompatibleEnemyPool(
        string scenePath,
        RszGameObject spawnInfo,
        ImmutableArray<EnemyTableEntry> areaEnemyPool,
        ImmutableArray<EnemyTableEntry> fallbackEnemyPool,
        EnemyPlacementRule rule) {
        var insectsOnly = EnemySpawnInfoRules.RequiresInsectReplacement(scenePath, spawnInfo);
        bool IsCompatible(EnemyTableEntry entry)
            => rule.AllowsReplacement(entry.Enemy) && (!insectsOnly || entry.Enemy.IsInsect) &&
               (!rule.Aggro || entry.Enemy.UsesEnemyGenerator);
        var candidates = areaEnemyPool.Where(IsCompatible).ToImmutableArray();
        return candidates.IsDefaultOrEmpty
            ? fallbackEnemyPool.Where(IsCompatible).ToImmutableArray()
            : candidates;
    }

    private static EnemyPackSelector GetPackSelector(
        Dictionary<string, EnemyPackSelector> packSelectors,
        ImmutableArray<EnemyTableEntry> enemyPool,
        int maxPackSize,
        Rng rng) {
        var key = string.Join("|", enemyPool.Select(entry => entry.Enemy.Id));
        if (!packSelectors.TryGetValue(key, out var packSelector)) {
            packSelector = new EnemyPackSelector(enemyPool, maxPackSize, rng);
            packSelectors.Add(key, packSelector);
        }

        return packSelector;
    }

    private ImmutableArray<EnemyTableEntry> CreateEnemyPool(Randomizer randomizer, bool includeBosses = true) {
        var enemyPool = ImmutableArray.CreateBuilder<EnemyTableEntry>();
        foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
            if (!includeBosses && enemy.IsBoss)
                continue;

            if (enemy.UsesEnemyGenerator && enemy.SpawnOptionType == null)
                continue;

            var ratio = randomizer.GetConfigOption<double>($"enemy-ratio-{enemy.Id.ToLowerInvariant()}");
            if (ratio != 0) {
                enemyPool.Add(new EnemyTableEntry(enemy, ratio));
            }
        }

        return enemyPool.ToImmutable();
    }

    private ImmutableArray<EnemyTableEntry> CreateExtraEnemyPool(Randomizer randomizer) {
        var enemyPool = ImmutableArray.CreateBuilder<EnemyTableEntry>();
        foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
            if (enemy.EnemyId is EnemyID.Em4000) // TODO: Fix stale Molded in idle animations
                continue;

            if (!enemy.UsesEnemyGenerator)
                continue;

            var ratio = randomizer.GetConfigOption<double>($"enemy-ratio-{enemy.Id.ToLowerInvariant()}");
            if (ratio != 0) {
                enemyPool.Add(new EnemyTableEntry(enemy, ratio));
            }
        }

        return enemyPool.ToImmutable();
    }

    private void RandomizeEnemies(
        Randomizer randomizer,
        RandomizerLogger logger,
        EnemyTemplateFactory templateFactory,
        EnemyRandomizerOptions options,
        EnemyHealthResolver healthResolver) {
        if (!randomizer.GetConfigOption<bool>("random-enemies"))
            return;

        var rng = randomizer.GetRng(RandomizerKey);
        var enemyPool = CreateEnemyPool(randomizer);

        if (enemyPool.IsDefaultOrEmpty) {
            logger.LogLine("Constructed an empty enemy table! Aborting...");
            return;
        } else {
            logger.LogLine($"Constructed an enemy table of size {enemyPool.Length}:");
            logger.LogLine(string.Join(", ", enemyPool.Select(entry => entry.Enemy.Name)));
        }

        foreach (var area in randomizer.AreaService.EnemyAreas) {
            if (!ScriptedSceneSafety.AllowsEnemyMutation(area.Path)) {
                logger.LogLine($"Skipping enemy replacement in scripted flashback scene {area.Path}.");
                continue;
            }

            ProcessArea(area, randomizer, logger, templateFactory, enemyPool, options, rng, healthResolver);
        }
    }

    private void RandomizeEnemyForceTargeting(
        Randomizer randomizer,
        RandomizerLogger logger,
        EnemyRandomizerOptions options) {
        var placements = randomizer.EnemyPlacementService;
        if (options.ForceTargetingProbability <= 0 && placements.AggroScenePaths.Count == 0)
            return;

        var rng = randomizer.GetRng("modifier/enemies/force-targeting");
        var updatedSceneCount = 0;
        var updatedSpawnInfoCount = 0;

        var paths = placements.AggroScenePaths.AsEnumerable();
        if (options.ForceTargetingProbability > 0) {
            paths = paths.Concat(AreaDefinitionRepository.Default.All
                .Where(area => area.Dlc == null).Select(area => area.Path));
        }
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.Ordinal)) {
            if (!ScriptedSceneSafety.AllowsEnemyMutation(path))
                continue;

            var scnFile = randomizer.FileRepository
                .GetScnFile(path)
                .ToBuilder(randomizer.FileRepository.TypeRepository);
            scnFile.Scene = RandomizeForceTargetingOptions(scnFile.Scene, path, placements, options, rng, out var changedCount);
            if (changedCount == 0)
                continue;

            updatedSceneCount++;
            updatedSpawnInfoCount += changedCount;
            randomizer.AreaService.UpdateCachedScene(path, scnFile.Scene);
            randomizer.FileRepository.SetScnFile(path, scnFile.AddMissingResources().Build());
        }

        logger.LogLine(
            $"Force targeting randomized for {updatedSpawnInfoCount} enemy spawn infos in {updatedSceneCount} scenes.");
    }

    private void PlaceExtraEnemies(
        Randomizer randomizer,
        RandomizerLogger logger,
        ExtraEnemySceneBuilder extraEnemySceneBuilder,
        EnemyRandomizerOptions options,
        EnemyHealthResolver healthResolver) {
        var extraEnemyPct = randomizer.GetConfigOption<double>("extra-enemy-amount");
        if (extraEnemyPct == 0)
            return;

        var rng = randomizer.GetRng("modifier/extra-enemies");
        var enemyMultiplier = randomizer.GetConfigOption("enemy-multiplier", 1.0);

        var enabledExtraEnemies = Csv
            .Deserialize<ExtraEnemyPlacement>(randomizer.DynamicData.GetData(DynamicDataName.ExtraEnemies)!)
            .Where(extraEnemy =>
                extraEnemy.Enabled && ScriptedSceneSafety.AllowsEnemyMutation(extraEnemy.SceneFile))
            .ToList();
        var subsetCount = ExtraEnemyPlanner.GetSubsetCount(enabledExtraEnemies.Count, extraEnemyPct);
        if (subsetCount == 0)
            return;

        var extraEnemies = ExtraEnemyPlanner
            .SelectRandomPlacementsWithoutReplacement(enabledExtraEnemies, subsetCount, rng)
            .GroupBy(extraEnemy => extraEnemy.SceneFile)
            .ToList();

        logger.Push("Additional enemies");
        var hasRandomExtraEnemies =
            extraEnemies.Any(group => group.Any(extraEnemy => ExtraEnemyPlanner.IsRandomEnemyId(extraEnemy.Id)));
        var randomEnemyPool = hasRandomExtraEnemies
            ? CreateExtraEnemyPool(randomizer)
            : [];
        if (hasRandomExtraEnemies && randomEnemyPool.IsDefaultOrEmpty) {
            logger.LogLine("Constructed an empty enemy table! Random extra enemies will be skipped.");
        }

        var generatorBuilds = new Dictionary<string, ExtraEnemyGeneratorBuild>(StringComparer.OrdinalIgnoreCase);
        var fsmGeneratorsByScene = new Dictionary<string, List<RszGameObject>>(StringComparer.OrdinalIgnoreCase);
        var directObjectsByScene = new Dictionary<string, List<RszGameObject>>(StringComparer.OrdinalIgnoreCase);

        foreach (var enemySceneGroup in extraEnemies) {
            var scene = enemySceneGroup.Key;
            var scenePlacements = enemySceneGroup.ToList();
            var sceneLimit = randomizer.EnemySceneLimitService.GetMaxEnemiesForExtraScene(scene);
            var uncappedTargetEnemyCount =
                EnemyMultiplierModifier.GetTargetEnemyCount(scenePlacements.Count, enemyMultiplier);
            var targetEnemyCount = sceneLimit == null
                ? uncappedTargetEnemyCount
                : Math.Min(uncappedTargetEnemyCount, sceneLimit.Value);
            if (targetEnemyCount == 0)
                continue;

            var selectedPlacements = ExtraEnemyPlanner.SelectRandomPlacementsWithoutReplacement(
                scenePlacements,
                Math.Min(targetEnemyCount, scenePlacements.Count),
                rng);
            var sceneHasRandomExtraEnemies =
                selectedPlacements.Any(extraEnemy => ExtraEnemyPlanner.IsRandomEnemyId(extraEnemy.Id));
            var sceneChapter = ExtraEnemyPlanner.GetSharedChapter(selectedPlacements);
            var sceneRandomEnemyPool = options.IsBalanced && sceneHasRandomExtraEnemies
                ? BalancedEnemyPoolSelector.Select(randomEnemyPool, sceneChapter, scene)
                : randomEnemyPool;

            logger.Push(ExtraEnemySceneBuilder.FormatSceneLog(scene, scenePlacements.Count, uncappedTargetEnemyCount,
                targetEnemyCount, sceneLimit));
            var extraEnemyRequests = new List<ResolvedExtraEnemyPlacement>(targetEnemyCount);
            var areaEnemyPool = !sceneHasRandomExtraEnemies || sceneRandomEnemyPool.IsDefaultOrEmpty
                ? []
                : EnemyPoolSelector.SelectAreaEnemyPool(sceneRandomEnemyPool, options.EnemyVariety, rng);
            var packSelector = areaEnemyPool.IsDefaultOrEmpty
                ? null
                : new EnemyPackSelector(areaEnemyPool, options.MaxPackSize, rng);

            foreach (var extraEnemy in selectedPlacements) {
                IEnemyDefinition definition;
                if (ExtraEnemyPlanner.IsRandomEnemyId(extraEnemy.Id)) {
                    if (packSelector == null) {
                        logger.LogLine(
                            $"Skipping random extra enemy at {extraEnemy.PosX}/{extraEnemy.PosY}/{extraEnemy.PosZ}: empty enemy table.");
                        continue;
                    }

                    definition = packSelector.Next();
                } else {
                    var possibleEnemies = extraEnemy.Id.Split('|',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    var selectedEnemyId = possibleEnemies.Length switch{
                        0 => extraEnemy.Id.Trim(),
                        1 => possibleEnemies[0],
                        _ => rng.Next(possibleEnemies),
                    };
                    definition = EnemyDefinitions.Instance.FromId(selectedEnemyId)
                                 ?? throw new InvalidOperationException(
                                     $"Unknown extra enemy id '{extraEnemy.Id}' (selected '{selectedEnemyId}').");
                }

                if (ExtraEnemyPlanner.TryCreateRequest(logger, extraEnemy, definition, out var request)) {
                    extraEnemyRequests.Add(request);
                }
            }

            while (extraEnemyRequests.Count < targetEnemyCount && extraEnemyRequests.Count != 0) {
                var source = rng.Next(extraEnemyRequests);
                logger.LogLine(
                    $"Duplicating {source.Enemy.Name} at {source.Placement.PosX}/{source.Placement.PosY}/{source.Placement.PosZ}");
                extraEnemyRequests.Add(source);
            }

            if (extraEnemyRequests.Count == 0) {
                logger.Pop();
                continue;
            }

            var fsmGenerators = new List<RszGameObject>(extraEnemyRequests.Count);
            ExtraEnemyGeneratorBuild? generatorBuild = null;

            for (var i = 0; i < extraEnemyRequests.Count; i++) {
                var request = extraEnemyRequests[i];
                if (ExtraEnemySceneBuilder.UsesStaticScenePlacement(request.Enemy)) {
                    if (!directObjectsByScene.TryGetValue(scene, out var directObjects)) {
                        directObjects = [];
                        directObjectsByScene.Add(scene, directObjects);
                    }

                    directObjects.Add(extraEnemySceneBuilder.CreateStaticInstance(
                        request,
                        options,
                        directObjects.Count,
                        rng));
                    continue;
                }

                if (generatorBuild == null) {
                    var generatorScene = ExtraEnemySceneBuilder.GetGeneratorScene(scene, extraEnemyRequests);
                    if (!generatorBuilds.TryGetValue(generatorScene, out generatorBuild)) {
                        generatorBuild = new ExtraEnemyGeneratorBuild();
                        generatorBuilds.Add(generatorScene, generatorBuild);
                    }
                }

                var generatorSpawnInfoIndex = generatorBuild.SpawnInfos.Count;
                var spawnInfo = extraEnemySceneBuilder.CreateSpawnInfo(
                    logger,
                    request,
                    healthResolver,
                    generatorSpawnInfoIndex,
                    rng);
                var requestInstances = extraEnemySceneBuilder.CreateInstances(request, options, rng);
                var fsmGenerator =
                    extraEnemySceneBuilder.CreateFsmGenerator(request, spawnInfo, generatorSpawnInfoIndex, rng);

                fsmGenerators.Add(fsmGenerator);
                generatorBuild.SpawnInfos.Add(spawnInfo);
                generatorBuild.Instances.AddRange(requestInstances);
            }

            if (fsmGenerators.Count != 0) {
                if (!fsmGeneratorsByScene.TryGetValue(scene, out var sceneFsmGenerators)) {
                    sceneFsmGenerators = [];
                    fsmGeneratorsByScene.Add(scene, sceneFsmGenerators);
                }

                sceneFsmGenerators.AddRange(fsmGenerators);
            }

            logger.Pop();
        }

        foreach (var (generatorScene, generatorBuild) in generatorBuilds.OrderBy(pair => pair.Key,
                     StringComparer.OrdinalIgnoreCase)) {
            var generator =
                extraEnemySceneBuilder.CreateGenerator(generatorBuild.SpawnInfos, generatorBuild.Instances, rng);
            randomizer.FileRepository.ModifyScnFile(generatorScene, root => {
                root = root.Add(generator);
                if (directObjectsByScene.Remove(generatorScene, out var directObjects)) {
                    root = ExtraEnemySceneBuilder.AddSceneObjects(root, directObjects);
                }

                if (fsmGeneratorsByScene.Remove(generatorScene, out var fsmGenerators)) {
                    root = ExtraEnemySceneBuilder.AddFsmGenerators(root, fsmGenerators);
                }

                return root;
            });
        }

        foreach (var (scene, fsmGenerators) in fsmGeneratorsByScene.OrderBy(pair => pair.Key,
                     StringComparer.OrdinalIgnoreCase)) {
            randomizer.FileRepository.ModifyScnFile(scene, root => {
                if (directObjectsByScene.Remove(scene, out var directObjects)) {
                    root = ExtraEnemySceneBuilder.AddSceneObjects(root, directObjects);
                }

                return ExtraEnemySceneBuilder.AddFsmGenerators(root, fsmGenerators);
            });
        }

        foreach (var (scene, directObjects) in directObjectsByScene.OrderBy(pair => pair.Key,
                     StringComparer.OrdinalIgnoreCase)) {
            randomizer.FileRepository.ModifyScnFile(scene,
                root => ExtraEnemySceneBuilder.AddSceneObjects(root, directObjects));
        }

        logger.Pop();
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var options = BuildOptions(randomizer);
        if (options.DebugUniqueHp) {
            logger.LogUniqueSpawnHpHelp();
        }

        var healthResolver = new EnemyHealthResolver(randomizer, options, randomizer.GetRng("modifier/enemy-health"));
        var templateFactory = new EnemyTemplateFactory(randomizer);
        var extraEnemySceneBuilder = new ExtraEnemySceneBuilder(randomizer, templateFactory);
        RandomizeEnemies(randomizer, logger, templateFactory, options, healthResolver);
        PlaceExtraEnemies(randomizer, logger, extraEnemySceneBuilder, options, healthResolver);
        RandomizeEnemyForceTargeting(randomizer, logger, options);
        ApplyCulling(randomizer, logger);
    }

    private static void ApplyCulling(Randomizer randomizer, RandomizerLogger logger) {
        var placements = randomizer.EnemyPlacementService;
        if (placements.Culls.Count == 0)
            return;

        // A spawn's generation FSM can live outside its own scene. Scan the
        // campaign scene catalogue so no active request retains a removed GUID.
        var paths = AreaDefinitionRepository.Default.All.Where(area => area.Dlc == null)
            .Select(area => area.Path).Concat(placements.Culls.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path, StringComparer.Ordinal);
        foreach (var path in paths) {
            var builder = randomizer.FileRepository.GetScnFile(path)
                .ToBuilder(randomizer.FileRepository.TypeRepository);
            var scene = builder.Scene;
            var changed = false;
            scene = scene.Visit(node => {
                if (node is RszObjectNode action && action.Type.Name == "app.fsm.EnemyGenerate" &&
                    placements.CulledSpawnInfoGuids.Contains(action.Get<Guid>("SpawnInfo"))) {
                    changed = true;
                    return action.SetField("v0_Enabled", false).SetField("SpawnInfo", Guid.Empty);
                }
                return node;
            });
            if (placements.Culls.TryGetValue(path, out var guids)) {
                foreach (var guid in GetCullTargets(scene, guids).Order()) {
                    if (scene.FindGameObject(guid) == null)
                        continue;
                    scene = scene.RemoveGameObject(guid);
                    changed = true;
                    logger.LogLine($"Culling enemy {guid} in {path}.");
                }
            }
            if (!changed)
                continue;
            builder.Scene = scene;
            randomizer.AreaService.UpdateCachedScene(path, scene);
            randomizer.FileRepository.SetScnFile(path, builder.AddMissingResources().Build());
        }
    }

    private static HashSet<Guid> GetCullTargets(RszScene scene, HashSet<Guid> requested) {
        var targets = new HashSet<Guid>();
        Visit(scene, null);
        return targets;

        void Visit(IRszSceneNode node, Guid? enemyRoot) {
            if (node is RszGameObject gameObject) {
                if (gameObject.FindComponent<app.EnemySave>() != null)
                    enemyRoot = gameObject.Guid;
                if (requested.Contains(gameObject.Guid)) {
                    // Mesh rows can be the head/body of a saved enemy. Remove its
                    // actor root, not just its visible mesh. Spawn slots stay local.
                    targets.Add(gameObject.FindComponent<app.EnemySpawnInfo>() != null
                        ? gameObject.Guid : enemyRoot ?? gameObject.Guid);
                }
            }
            foreach (var child in node.Children)
                Visit(child, enemyRoot);
        }
    }
}
