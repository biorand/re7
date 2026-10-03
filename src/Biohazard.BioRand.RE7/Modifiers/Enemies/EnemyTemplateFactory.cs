using Biohazard.BioRand.RE7.Enemies;
using IntelOrca.Biohazard.REE.Rsz;
using System.Numerics;

namespace Biohazard.BioRand.RE7.Modifiers;

internal sealed class EnemyTemplateFactory(Randomizer randomizer) {
    internal const string ExplosiveEm3300Tag = "BioRandExplosiveEm3300";
    private readonly Dictionary<string, RszGameObject> _generatorTemplateCache = new();
    private readonly Dictionary<string, RszGameObject> _spawnInfoTemplateCache = new();

    internal RszGameObject GetOrCreateEnemyTemplate(
        string enemyId,
        GeneratedViaTransform transform,
        bool updateTransform,
        bool randomizeScale,
        ScaleOptions scaleOptions,
        Rng rng,
        IEnemyDefinition? definition = null) {
        if (!_generatorTemplateCache.TryGetValue(enemyId, out var baseTemplate)) {
            baseTemplate = randomizer.TemplateService
                .GetEnemyTemplate(enemyId)
                .WithName(enemyId);

            _generatorTemplateCache[enemyId] = baseTemplate;
        }

        var template = CloneGameObject(baseTemplate, rng);
        definition ??= EnemyDefinitions.Instance.FromId(enemyId)
                       ?? throw new InvalidOperationException($"Unknown enemy definition for '{enemyId}'.");
        template = definition.IndividualizeTemplate(rng, template);

        if (updateTransform || randomizeScale) {
            var templateTransform = updateTransform
                ? transform
                : template.FindComponent<GeneratedViaTransform>()!;

            if (randomizeScale) {
                RandomizeScale(templateTransform, scaleOptions, rng);
            }

            template = template.AddOrUpdateComponent(templateTransform);
        }

        // Cloning the GameObject GUIDs does not clone the enemy's save or FSM identities.
        // Use a separate stream so isolating those identities does not reshuffle enemy choices.
        template = RefreshRuntimeGuids(template, randomizer.GetRng("enemy-instance/state", template.Guid));
        return DisableEnemyStampSerialization(MarkExplosiveEm3300Template(enemyId, template.WithName(enemyId)));
    }

    internal RszGameObject GetOrCreateSpawnInfoTemplate(string enemyId, Rng rng) {
        if (!_spawnInfoTemplateCache.TryGetValue(enemyId, out var template)) {
            template = GetSpawnInfoTemplate(enemyId)
                .WithName(enemyId);
            template = NormalizeGenericSpawnInfoTemplate(enemyId, template);

            _spawnInfoTemplateCache[enemyId] = template;
        }

        return CloneGameObject(template, rng)
            .WithName($"ESI_{enemyId}");
    }

    private RszGameObject GetSpawnInfoTemplate(string enemyId) {
        var template = randomizer.TemplateService.TryGetEnemySpawnInfo(enemyId);
        if (template != null) {
            return template;
        }

        throw new Exception($"Object with name EnemySpawnInfo_{enemyId} not found in template scene!");
    }

    private static RszGameObject NormalizeGenericSpawnInfoTemplate(string enemyId, RszGameObject template) {
        if (!string.Equals(enemyId, EnemyID.Em4100.ToString(), StringComparison.Ordinal))
            return template;

        var option = template.FindComponent<app.EnemySpawnInfoOptionEm4100>();
        if (option?.ThinkSet?.AppearSet == null)
            return template;

        option.ThinkSet.AppearSet.AppearType = Enums.app.Em4100.ThinkAppearSet.Type.Default;
        return template.AddOrUpdateComponent(option);
    }

    internal static bool IsSpawnInfoOption(RszObjectNode component)
        => component.Type.Name.Contains("EnemySpawnInfoOption", StringComparison.Ordinal);

    internal List<RszGameObject> CreatePoolInstancesForNestedSpawnInfos(
        RszGameObject template,
        ScaleOptions scaleOptions,
        Rng rng) {
        var nestedSpawnAliases = new List<string>();
        template.VisitGameObjects(gameObject => {
            var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
            if (spawnInfo?.Enabled == true && !string.IsNullOrWhiteSpace(spawnInfo.UnitAlias)) {
                nestedSpawnAliases.Add(spawnInfo.UnitAlias);
            }
        });

        if (nestedSpawnAliases.Count == 0)
            return [];

        var instances = new List<RszGameObject>(nestedSpawnAliases.Count);
        var transform = new GeneratedViaTransform(){
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One,
        };

        foreach (var nestedSpawnAlias in nestedSpawnAliases) {
            var definition = EnemyDefinitions.Instance.FromId(nestedSpawnAlias)
                             ?? throw new InvalidOperationException(
                                 $"Enemy template '{template.Name}' contains a nested spawn info for unsupported enemy '{nestedSpawnAlias}'.");
            if (!definition.UsesEnemyGenerator) {
                throw new InvalidOperationException(
                    $"Enemy template '{template.Name}' contains a nested spawn info for non-generator enemy '{nestedSpawnAlias}'.");
            }

            instances.Add(GetOrCreateEnemyTemplate(
                nestedSpawnAlias,
                transform,
                updateTransform: false,
                randomizeScale: false,
                scaleOptions,
                rng,
                definition));
        }

        return instances;
    }

    internal static RszGameObject CloneGameObject(RszGameObject rootGameObject, Rng rng) {
        var guidMap = new Dictionary<Guid, Guid>();
        var root = rootGameObject.VisitGameObjects(gameObject => {
            var newGuid = rng.NextGuid();
            guidMap[gameObject.Guid] = newGuid;
            return gameObject.WithGuid(newGuid);
        });

        return ReplaceGameObjectRefs(root, guidMap);
    }

    internal static RszGameObject DisableEnemyStampSerialization(RszGameObject gameObject) {
        return gameObject.VisitComponents(component => {
            if (component.Type.Name == "app.StampController" &&
                component.Type.FindFieldIndex("IsSerializeTexture") != -1) {
                return component.SetField("IsSerializeTexture", false);
            }

            return component;
        });
    }

    private static RszGameObject MarkExplosiveEm3300Template(string enemyId, RszGameObject gameObject) {
        if (!string.Equals(enemyId, EnemyID.Em3300.ToString(), StringComparison.Ordinal))
            return gameObject;

        return gameObject.WithSettings(gameObject.Settings.Set("Tag", ExplosiveEm3300Tag));
    }

    internal static RszGameObject RefreshRuntimeGuids(RszGameObject gameObject, Rng rng) {
        return gameObject.VisitComponents(component => RefreshRuntimeGuids(component, rng));
    }

    private static RszObjectNode RefreshRuntimeGuids(RszObjectNode objectNode, Rng rng) {
        for (var i = 0; i < objectNode.Children.Length; i++) {
            var fieldName = objectNode.Type.Fields[i].Name;
            if (fieldName is "SaveGUID" or "InstanceGuid" or "MyGUID") {
                objectNode = objectNode.SetField(fieldName, rng.NextGuid());
            }
        }

        return objectNode;
    }

    private static void RandomizeScale(GeneratedViaTransform transform, ScaleOptions scaleOptions, Rng rng) {
        var unusualScaleChance = GetScaleProbabilityPercent(scaleOptions.Probability);
        if (!rng.NextProbability(unusualScaleChance)) {
            return;
        }

        var newScale = rng.NextFloat(scaleOptions.Min, scaleOptions.Max);
        transform.Scale = new Vector3(newScale, newScale, newScale);
    }

    private static int GetScaleProbabilityPercent(double probability)
        => (int)Math.Round(Math.Clamp(probability, 0.0, 1.0) * 100.0, MidpointRounding.AwayFromZero);

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
}
