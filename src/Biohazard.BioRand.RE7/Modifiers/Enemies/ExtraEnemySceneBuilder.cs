using Biohazard.BioRand.RE7.Enemies;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal sealed class ExtraEnemySceneBuilder(
    Randomizer randomizer,
    EnemyTemplateFactory templateFactory) {
    internal const string GeneratorName = "BioRandExtraEnemyGenerator";
    internal const string PoolName = "BioRandExtraEnemyPool";
    internal const string SpawnPointsName = "BioRandExtraEnemySpawnPoints";
    internal const string SpawnInfoPrefix = "BioRandExtraEnemySpawnInfo";
    internal const string GeneratePrefix = "BioRandExtraEnemyGenerate";
    internal const string StaticPrefix = "BioRandExtraEnemyStatic";
    private const string EnemyGenerationFsmFolderName = "EnemyGenFsm";
    private const string GenerateFsmResource = "LevelDesign/Fsm/Template/TempFsm_TriggerInAction_EnemyGenerate5.fsm";

    private static readonly IReadOnlyDictionary<int, string> GeneratorSceneByChapter = new Dictionary<int, string>(){
        [1] = "natives/stm/scenes/chapter/chapter1/enemy_c01.scn.20",
        [3] = "natives/stm/scenes/chapter/chapter3/enemy_c03.scn.20",
        [4] = "natives/stm/scenes/chapter/chapter4/enemy_c04.scn.20",
    };

    private static readonly HashSet<string> MoldedIds = new(StringComparer.Ordinal){
        "Em4000",
        "Em4100",
        "Em4200",
    };

    private static readonly (string ScenePrefix, string MapName)[] MoldedAiMapByScenePrefix =[
        ("natives/stm/environment/scene/chapter3/c03_gh", "c03_AIMap"),
        ("natives/stm/environment/scene/chapter3/c03_oldhouse", "c03_AIMap"),
        ("natives/stm/environment/scene/chapter3/c03_cow", "c03_4_Lucus_Cowshed"),
        ("natives/stm/environment/scene/chapter3/c03_leftarea", "c03_4_AIMap"),
        ("natives/stm/environment/scene/chapter3/c03_boat", "c03_4_AIMap"),
        ("natives/stm/environment/scene/chapter3/", "c03_4_AIMap"),
        ("natives/stm/scenes/chapter/chapter3/chapter3_4/", "c03_4_AIMap"),
        ("natives/stm/scenes/chapter/chapter3/chapter3_3/", "c03_AIMap"),
        ("natives/stm/scenes/chapter/chapter3/", "c03_AIMap"),
        ("natives/stm/environment/scene/chapter1/", "c01_AIMap"),
        ("natives/stm/scenes/chapter/chapter1/", "c01_AIMap"),
        ("natives/stm/environment/scene/chapter4/c04_1", "c04_1_AIMap"),
        ("natives/stm/scenes/chapter/chapter4/chapter4_1/", "c04_1_AIMap"),
        ("natives/stm/environment/scene/chapter4/c04_2", "c04_2_AIMap"),
        ("natives/stm/scenes/chapter/chapter4/chapter4_2/", "c04_2_AIMap"),
    ];

    private static readonly uint[] GenerateActionUids =[
        2860522480,
    ];

    internal RszGameObject CreateSpawnInfo(
        RandomizerLogger logger,
        ResolvedExtraEnemyPlacement request,
        EnemyHealthResolver healthResolver,
        int index,
        Rng rng) {
        if (UsesStaticScenePlacement(request.Enemy)) {
            throw new InvalidOperationException(
                $"{request.Enemy.Name} must be placed as a direct scene object because it has no EnemySpawnInfoOption.");
        }

        var enemyId = request.Enemy.EnemyId.ToString();
        var spawnInfo = templateFactory.GetOrCreateSpawnInfoTemplate(enemyId, rng)
            .WithName(enemyId);

        var transform = spawnInfo.FindComponent<GeneratedViaTransform>()!;
        transform.Position = GetPlacementPosition(request.Placement);
        transform.Rotation = GetPlacementRotation(request.Placement);
        transform.Scale = Vector3.One;
        spawnInfo = spawnInfo.AddOrUpdateComponent(transform);

        var spawnInfoComponent = spawnInfo.FindComponent<app.EnemySpawnInfo>()!;
        var assignedHealth = healthResolver.GetHealth(request.Enemy);
        spawnInfoComponent.UnitAlias = enemyId;
        spawnInfoComponent.Comment = $"{SpawnInfoPrefix}_{enemyId}_{index:000}";
        spawnInfoComponent.HealthParameter.Health = assignedHealth;

        ConfigureMoldedAiMap(spawnInfoComponent, enemyId, request.Placement.SceneFile);
        spawnInfoComponent.MyGUID = rng.NextGuid();
        spawnInfo = spawnInfo.AddOrUpdateComponent(spawnInfoComponent);
        if (request.Enemy.Id is "Molded" or "MoldedBlade") {
            var option = spawnInfo.FindComponent<app.EnemySpawnInfoOptionEm4000>()!;
            option.IsUseBlade = request.Enemy.Id == "MoldedBlade";
            spawnInfo = spawnInfo.AddOrUpdateComponent(option);
        }
        spawnInfo = EnemyTemplateFactory.RefreshRuntimeGuids(spawnInfo, rng);

        logger.LogSpawnHealthAssignment(
            request.Enemy,
            assignedHealth,
            "extra enemy generator",
            spawnInfo.Name,
            spawnInfo.Guid);

        return spawnInfo;
    }

    internal RszGameObject CreateStaticInstance(
        ResolvedExtraEnemyPlacement request,
        EnemyRandomizerOptions options,
        int index,
        Rng rng) {
        var enemyId = request.Enemy.EnemyId.ToString();
        var transform = new GeneratedViaTransform(){
            Position = GetPlacementPosition(request.Placement),
            Rotation = GetPlacementRotation(request.Placement),
            Scale = Vector3.One,
        };

        var instance = EnemyTemplateFactory.RefreshRuntimeGuids(templateFactory.GetOrCreateEnemyTemplate(
                    enemyId,
                    transform,
                    updateTransform: true,
                    randomizeScale: true,
                    options.ScaleOptions,
                    rng,
                    request.Enemy)
                .WithName($"{StaticPrefix}_{enemyId}_{index:000}"),
            rng);
        // Static instances have no spawn encounter GUID, so their SpawnerGuid must be unique.
        // Sharing it makes one Mia's saved death (and runtime RNG) apply to every cloned Mia.
        return instance.WithComponents(instance.Components.Select(component =>
            component.Type.FindFieldIndex("SpawnerGuid") >= 0
                ? component.SetField("SpawnerGuid", instance.Guid)
                : component).ToImmutableArray());
    }

    internal List<RszGameObject> CreateInstances(
        ResolvedExtraEnemyPlacement request,
        EnemyRandomizerOptions options,
        Rng rng) {
        var enemyId = request.Enemy.EnemyId.ToString();
        var transform = new GeneratedViaTransform(){
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,
        };

        var instance = EnemyTemplateFactory.RefreshRuntimeGuids(templateFactory.GetOrCreateEnemyTemplate(
                enemyId,
                transform,
                updateTransform: false,
                randomizeScale: true,
                options.ScaleOptions,
                rng,
                request.Enemy)
            .WithName(enemyId), rng);
        var instances = new List<RszGameObject>(){
            instance,
        };
        instances.AddRange(templateFactory.CreatePoolInstancesForNestedSpawnInfos(instance, options.ScaleOptions, rng)
            .Select(nestedInstance => EnemyTemplateFactory.RefreshRuntimeGuids(nestedInstance, rng)));

        return instances
            .Select(PreparePoolInstance)
            .ToList();
    }

    internal RszGameObject CreateFsmGenerator(
        ResolvedExtraEnemyPlacement request,
        RszGameObject spawnInfo,
        int index,
        Rng rng) {
        var enemyId = request.Enemy.EnemyId.ToString();
        var fsmGenerator = EnemyTemplateFactory.CloneGameObject(randomizer.TemplateService.GetEnemyFsmGenerator(), rng)
            .WithName($"{GeneratePrefix}_{enemyId}_{index:000}");

        ValidateFsmGeneratorTemplate(fsmGenerator);
        ValidateFsmResource(fsmGenerator);
        fsmGenerator = ConfigureGenerateActions(fsmGenerator, spawnInfo.Guid);

        return EnemyTemplateFactory.RefreshRuntimeGuids(fsmGenerator, rng);
    }

    internal RszGameObject CreateGenerator(
        IReadOnlyList<RszGameObject> spawnInfos,
        IReadOnlyList<RszGameObject> instances,
        Rng rng) {
        var generator = EnemyTemplateFactory.CloneGameObject(randomizer.TemplateService.GetEnemyGenerator(), rng)
            .WithName(GeneratorName);

        var generatorComponent = generator.FindComponent<app.EnemyGenerator>()!;
        generatorComponent.Alias = GeneratorName;
        generator = generator.AddOrUpdateComponent(generatorComponent);

        var pool = generator.Children.Single(child => child.FindComponent<app.EnemyPool>() != null)
            .WithName(PoolName);
        var spawnPoints = pool.Children.Single(child => child.Name == "SpawnPoints")
            .WithName(SpawnPointsName)
            .WithChildren(spawnInfos.ToImmutableArray());

        var poolChildren = ImmutableArray.CreateBuilder<RszGameObject>();
        poolChildren.Add(spawnPoints);
        poolChildren.AddRange(instances);
        pool = pool.WithChildren(poolChildren.ToImmutable());

        var poolComponent = pool.FindComponent<app.EnemyPool>()!;
        poolComponent.ExternalInstancePoolRefs.Clear();
        pool = pool.AddOrUpdateComponent(poolComponent);

        return generator.WithChildren(generator.Children.Replace(
            generator.Children.Single(child => child.FindComponent<app.EnemyPool>() != null),
            pool));
    }

    internal static RszScene AddFsmGenerators(
        RszScene scene,
        IReadOnlyCollection<RszGameObject> fsmGenerators) {
        var dynamicParent = scene.FindGameObject(gameObject =>
            gameObject.Name.EndsWith("_dynamic", StringComparison.OrdinalIgnoreCase));
        if (dynamicParent != null) {
            var children = dynamicParent.Children
                .AddRange(fsmGenerators);
            return scene.UpdateGameObject(dynamicParent.WithChildren(children));
        }

        var fsmFolder = scene.Children
            .OfType<RszFolder>()
            .FirstOrDefault(folder => folder.Name == EnemyGenerationFsmFolderName);
        if (fsmFolder == null) {
            foreach (var fsmGenerator in fsmGenerators) {
                scene = scene.Add(fsmGenerator);
            }

            return scene;
        }

        var updatedFolder = fsmFolder.WithChildren(fsmFolder.Children.AddRange(fsmGenerators));
        return scene.WithChildren(scene.Children.Replace(fsmFolder, updatedFolder));
    }

    internal static RszScene AddSceneObjects(
        RszScene scene,
        IReadOnlyCollection<RszGameObject> gameObjects) {
        foreach (var gameObject in gameObjects) {
            scene = scene.Add(gameObject);
        }

        return scene;
    }

    internal static bool UsesStaticScenePlacement(IEnemyDefinition enemy)
        => enemy.UsesEnemyGenerator && enemy.SpawnOptionType == null;

    internal static string GetGeneratorScene(
        string requestScene,
        IReadOnlyCollection<ResolvedExtraEnemyPlacement> requests) {
        if (!IsEnvironmentScene(requestScene))
            return requestScene;

        var chapters = requests
            .Select(request => request.Placement.Chapter)
            .Distinct()
            .ToArray();
        if (chapters.Length != 1) {
            throw new InvalidOperationException(
                $"Extra enemy environment scene '{requestScene}' has placements for multiple chapters: {string.Join(", ", chapters)}.");
        }

        if (GeneratorSceneByChapter.TryGetValue(chapters[0], out var generatorScene)) {
            return generatorScene;
        }

        throw new InvalidOperationException(
            $"Extra enemy environment scene '{requestScene}' is in chapter {chapters[0]}, which has no configured generator scene.");
    }

    internal static string FormatSceneLog(
        string scene,
        int placementCount,
        int uncappedTargetEnemyCount,
        int targetEnemyCount,
        int? sceneLimit) {
        if (sceneLimit == null && placementCount == targetEnemyCount) {
            return scene;
        }

        var label = $"{scene} ({placementCount} => {targetEnemyCount}";
        if (sceneLimit != null && targetEnemyCount != uncappedTargetEnemyCount) {
            label += $", limit {sceneLimit}";
        }

        return label + ")";
    }

    private static RszGameObject PreparePoolInstance(RszGameObject instance) {
        return instance.WithSettings(instance.Settings.SetField("Draw", false));
    }

    private static bool IsEnvironmentScene(string scene)
        => scene.Replace('\\', '/').Contains("/environment/scene/", StringComparison.OrdinalIgnoreCase);

    private static void ConfigureMoldedAiMap(
        app.EnemySpawnInfo spawnInfo,
        string enemyId,
        string sceneFile) {
        if (!MoldedIds.Contains(enemyId))
            return;

        var mapName = ResolveMoldedAiMapName(sceneFile);
        if (mapName == null)
            return;

        spawnInfo.MapParameter ??= new app.EnemySpawnInfo.AIMapParameter();
        spawnInfo.MapParameter.IsUseCheck = true;
        spawnInfo.MapParameter.MapName = mapName;
        spawnInfo.MapParameter.VolumeSpaceMapName = "";
    }

    private static string? ResolveMoldedAiMapName(string sceneFile) {
        var normalizedSceneFile = sceneFile.Replace('\\', '/');
        foreach (var (scenePrefix, mapName) in MoldedAiMapByScenePrefix) {
            if (normalizedSceneFile.StartsWith(scenePrefix, StringComparison.OrdinalIgnoreCase)) {
                return mapName;
            }
        }

        return null;
    }

    private static Vector3 GetPlacementPosition(ExtraEnemyPlacement placement)
        => new(placement.PosX, placement.PosY, placement.PosZ);

    private static Quaternion GetPlacementRotation(ExtraEnemyPlacement placement)
        => new(placement.RotX, placement.RotY, placement.RotZ, placement.RotW);

    private static RszGameObject ConfigureGenerateActions(
        RszGameObject generationGameObject,
        Guid spawnInfoGuid) {
        var actionIndex = 0;
        var result = generationGameObject.Visit(node => {
            if (node is not RszObjectNode objectNode ||
                objectNode.Type.Name != "via.fsm.SceneFsmData") {
                return node;
            }

            var actions = (RszArrayNode)objectNode["v1_Actions"];
            var configuredActions = ImmutableArray.CreateBuilder<IRszNode>();
            foreach (var action in actions.Children.OfType<RszObjectNode>()) {
                if (action.Type.Name != "app.fsm.EnemyGenerate")
                    continue;

                if (actionIndex >= GenerateActionUids.Length) {
                    throw new InvalidOperationException(
                        $"Extra enemy generation template has more app.fsm.EnemyGenerate actions than {GenerateFsmResource} expects.");
                }

                configuredActions.Add(action
                    .SetField("v0_Enabled", true)
                    .SetField("v2_UID", GenerateActionUids[actionIndex++])
                    .SetField("SpawnInfo", spawnInfoGuid)
                    .SetField("Operation", Enums.app.EnemyGenerator.Operation.Spawn));
            }

            if (configuredActions.Count == 0)
                return node;

            var conditions = (RszArrayNode)objectNode["v2_Conditions"];
            return objectNode
                .SetField("v1_Actions", new RszArrayNode(actions.Type, configuredActions.ToImmutable()))
                .SetField("v2_Conditions", new RszArrayNode(conditions.Type, []));
        });

        if (actionIndex != GenerateActionUids.Length) {
            throw new InvalidOperationException(
                $"Extra enemy generation template has {actionIndex} app.fsm.EnemyGenerate actions, expected {GenerateActionUids.Length} for {GenerateFsmResource}.");
        }

        return result;
    }

    private static void ValidateFsmGeneratorTemplate(RszGameObject generationGameObject) {
        var componentNames = generationGameObject.Components
            .Select(component => component.Type.Name)
            .ToArray();
        var unexpectedComponents = componentNames
            .Where(componentName =>
                componentName is "app.GimmickActiveControl" or "via.physics.Colliders" or "app.TriggerInAction")
            .ToArray();
        if (unexpectedComponents.Length != 0) {
            throw new InvalidOperationException(
                $"Extra enemy generation template has unsupported trigger wrapper components: {string.Join(", ", unexpectedComponents)}.");
        }

        if (!componentNames.Contains("via.Transform", StringComparer.Ordinal) ||
            !componentNames.Contains("via.fsm.Fsm", StringComparer.Ordinal)) {
            throw new InvalidOperationException(
                $"Extra enemy generation template must be a plain GameObject with via.Transform and via.fsm.Fsm; found: {string.Join(", ", componentNames)}.");
        }
    }

    private static void ValidateFsmResource(RszGameObject generationGameObject) {
        var fsm = generationGameObject.FindComponent("via.fsm.Fsm")
                  ?? throw new InvalidOperationException("Extra enemy generation template is missing via.fsm.Fsm.");
        var resource = ((RszResourceNode)fsm["Resource"]).Value;
        if (!string.Equals(resource, GenerateFsmResource, StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"Extra enemy generation template uses '{resource}', expected '{GenerateFsmResource}'.");
        }
    }
}
