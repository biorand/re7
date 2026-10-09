using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Extensions;

public static class RszExtensions {
    public readonly record struct GameObjectMatch(
        RszGameObject GameObject,
        bool HasFsmInHierarchy,
        bool HasDrawerContext = false,
        bool HasConditionalPickupContext = false);

    public static RszGameObject CloneWithNewGuids(
        this RszGameObject rootGameObject,
        Rng rng,
        Guid? rootGuid = null,
        IReadOnlyDictionary<Guid, Guid>? childGuidOverrides = null) {
        var guidMap = new Dictionary<Guid, Guid>();
        var isRoot = true;
        var root = rootGameObject.VisitGameObjects(gameObject => {
            var newGuid = rootGuid.HasValue && isRoot
                ? rootGuid.Value
                : !isRoot && childGuidOverrides != null && childGuidOverrides.TryGetValue(gameObject.Guid, out var childGuid)
                ? childGuid
                : rng.NextGuid();
            isRoot = false;
            guidMap[gameObject.Guid] = newGuid;
            return gameObject.WithGuid(newGuid);
        });

        return ReplaceGameObjectRefsAndSaveGuids(root, guidMap, new Dictionary<Guid, Guid>(), rng);
    }

    private static RszGameObject ReplaceGameObjectRefsAndSaveGuids(
        RszGameObject gameObject,
        IReadOnlyDictionary<Guid, Guid> guidMap,
        IDictionary<Guid, Guid> saveGuidMap,
        Rng rng) {
        return gameObject.Visit(node => {
            if (node is RszObjectNode objectNode) {
                return ReplacePersistentGuid(ReplacePersistentGuid(objectNode, "SaveGUID", saveGuidMap, rng),
                    "InstanceGuid", saveGuidMap, rng);
            }

            if (node is RszValueNode valueNode && valueNode.Type == RszFieldType.GameObjectRef) {
                var refGuid = RszSerializer.Deserialize<Guid>(valueNode);
                if (guidMap.TryGetValue(refGuid, out var newGuid)) {
                    return RszSerializer.Serialize(RszFieldType.GameObjectRef, newGuid);
                }
            } else if (node is RszValueNode guidValueNode && guidValueNode.Type == RszFieldType.Guid) {
                var guid = RszSerializer.Deserialize<Guid>(guidValueNode);
                if (guidMap.TryGetValue(guid, out var newGuid)) {
                    return RszSerializer.Serialize(RszFieldType.Guid, newGuid);
                }
            }

            return node;
        });
    }

    private static RszObjectNode ReplacePersistentGuid(
        RszObjectNode objectNode,
        string fieldName,
        IDictionary<Guid, Guid> saveGuidMap,
        Rng rng) {
        var saveGuidIndex = objectNode.Type.FindFieldIndex(fieldName);
        if (saveGuidIndex == -1 ||
            objectNode.Children[saveGuidIndex] is not RszValueNode saveGuidNode ||
            saveGuidNode.Type != RszFieldType.Guid) {
            return objectNode;
        }

        var saveGuid = RszSerializer.Deserialize<Guid>(saveGuidNode);
        if (saveGuid == Guid.Empty) {
            return objectNode;
        }

        if (!saveGuidMap.TryGetValue(saveGuid, out var newSaveGuid)) {
            newSaveGuid = rng.NextGuid();
            saveGuidMap[saveGuid] = newSaveGuid;
        }

        return objectNode.SetField(fieldName, newSaveGuid);
    }

    extension(RszGameObject gameObject) {
        public RszGameObject PreparePickupInteractionsForPlacement() {
            return gameObject.VisitGameObjects(child => {
                var components = child.Components.ToBuilder();
                var changed = false;

                for (var i = 0; i < components.Count; i++) {
                    var component = components[i];
                    if (!IsPickupInteraction(component) &&
                        !IsWeaponPickupInteraction(component)) {
                        continue;
                    }

                    var updated = component;
                    updated = SetBoolFieldIfPresent(updated, "IsCheckAngle", false);
                    updated = SetBoolFieldIfPresent(updated, "IsItemGet", false);
                    updated = SetBoolFieldIfPresent(updated, "IsGetEventEnabled", false);
                    updated = SetBoolFieldIfPresent(updated, "IsForceEquip", false);
                    updated = SetBoolFieldIfPresent(updated, "UsePickupSE", true);

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

        public RszGameObject PrepareWeaponPickupInteractionGameObjects() {
            return gameObject.VisitGameObjects(child => {
                if (!child.Components.Any(IsWeaponPickupInteraction)) {
                    return child;
                }

                return child.WithSettings(
                    child.Settings
                        .Set("Update", true)
                        .Set("Draw", false));
            });
        }

        public RszGameObject ApplyVisualResourcesFromTemplate(RszGameObject template) {
            var templateMesh = template.FindComponent("via.render.Mesh");
            if (templateMesh == null) {
                return gameObject;
            }

            var mesh = gameObject.FindComponent("via.render.Mesh");
            if (mesh == null) {
                return gameObject.AddOrUpdateComponent(templateMesh);
            }

            mesh = CopyFieldIfPresent(mesh, templateMesh, "Mesh");
            mesh = CopyFieldIfPresent(mesh, templateMesh, "Material");
            return gameObject.AddOrUpdateComponent(mesh);
        }
    }

    public static Dictionary<Guid, GameObjectMatch> FindGameObjectsByGuidWithFsmContext(
        this RszScene scene,
        IEnumerable<Guid> targetGuids) {
        var remaining = targetGuids.ToHashSet();
        var result = new Dictionary<Guid, GameObjectMatch>();
        if (remaining.Count == 0) {
            return result;
        }

        foreach (var child in scene.Children) {
            VisitSceneNode(child, hasFsmInHierarchy: false, hasDrawerInHierarchy: false,
                hasConditionalPickupInHierarchy: false, remaining, result);
            if (remaining.Count == 0) {
                break;
            }
        }

        MarkDrawerReferencedTargets(scene, result);

        return result;
    }

    private static void VisitSceneNode(
        IRszSceneNode node,
        bool hasFsmInHierarchy,
        bool hasDrawerInHierarchy,
        bool hasConditionalPickupInHierarchy,
        ISet<Guid> remaining,
        IDictionary<Guid, GameObjectMatch> result) {
        switch (node) {
            case RszFolder folder:
                foreach (var child in folder.Children) {
                    VisitSceneNode(child, hasFsmInHierarchy, hasDrawerInHierarchy,
                        hasConditionalPickupInHierarchy, remaining, result);
                    if (remaining.Count == 0) {
                        break;
                    }
                }

                break;

            case RszGameObject gameObject:
                var hasFsmHere = hasFsmInHierarchy || HasFsmComponent(gameObject);
                var hasDrawerHere = hasDrawerInHierarchy || HasDrawerComponent(gameObject);
                var hasConditionalPickupHere = hasConditionalPickupInHierarchy ||
                    HasConditionalPickupComponent(gameObject);
                if (remaining.Remove(gameObject.Guid)) {
                    result[gameObject.Guid] = new GameObjectMatch(gameObject, hasFsmHere, hasDrawerHere,
                        hasConditionalPickupHere);
                }

                if (remaining.Count == 0) {
                    break;
                }

                foreach (var child in gameObject.Children) {
                    VisitSceneNode(child, hasFsmHere, hasDrawerHere, hasConditionalPickupHere, remaining, result);
                    if (remaining.Count == 0) {
                        break;
                    }
                }

                break;
        }
    }

    private static bool IsPickupInteraction(RszObjectNode component) {
        return component.Type.Name.Contains("InteractDetailSearch", StringComparison.Ordinal) &&
               component.Type.FindFieldIndex("IsCheckAngle") != -1;
    }

    private static bool IsWeaponPickupInteraction(RszObjectNode component) {
        return component.Type.Name.Contains("InteractWeapon", StringComparison.Ordinal) &&
               component.Type.FindFieldIndex("IsForceEquip") != -1;
    }

    private static bool HasFsmComponent(RszGameObject gameObject)
        => gameObject.Components.Any(component =>
            component.Type.Name.Contains("Fsm", StringComparison.Ordinal) ||
            component.Type.Name.Contains("FSM", StringComparison.Ordinal));

    private static bool HasDrawerComponent(RszGameObject gameObject)
        => gameObject.Name.Contains("Drawer", StringComparison.OrdinalIgnoreCase) ||
           gameObject.Components.Any(component =>
               component.Type.Name.Contains("InteractDrawer", StringComparison.Ordinal));

    private static bool HasConditionalPickupComponent(RszGameObject gameObject)
        // Clearing the spiders requires damage resources that randomized loadouts do not guarantee.
        => gameObject.Name.Contains("InsectLocker", StringComparison.OrdinalIgnoreCase)
           // The deputy's body has mutually exclusive censorship variants. A key in just
           // one variant would disappear on the other regional editions of the game.
           || gameObject.Components.Any(component =>
               component.Type.Name == "via.fsm.Fsm" &&
               component["Resource"].ToString()?.Contains("Fsm_RatingSelector", StringComparison.OrdinalIgnoreCase) == true);

    private static void MarkDrawerReferencedTargets(
        RszScene scene,
        IDictionary<Guid, GameObjectMatch> result) {
        var targetGuids = result.Keys.ToHashSet();
        if (targetGuids.Count == 0) {
            return;
        }

        scene.VisitGameObjects(gameObject => {
            foreach (var component in gameObject.Components) {
                if (!component.Type.Name.Contains("InteractDrawer", StringComparison.Ordinal)) {
                    continue;
                }

                foreach (var targetGuid in FindReferencedTargets(component, targetGuids)) {
                    var match = result[targetGuid];
                    result[targetGuid] = match with{ HasDrawerContext = true };
                }
            }
        });
    }

    private static IEnumerable<Guid> FindReferencedTargets(IRszNode node, IReadOnlySet<Guid> targetGuids) {
        switch (node) {
            case RszValueNode valueNode when valueNode.Type == RszFieldType.GameObjectRef:
                var referencedGuid = RszSerializer.Deserialize<Guid>(valueNode);
                if (targetGuids.Contains(referencedGuid)) {
                    yield return referencedGuid;
                }

                break;

            case RszObjectNode objectNode:
                foreach (var child in objectNode.Children) {
                    foreach (var matchedGuid in FindReferencedTargets(child, targetGuids)) {
                        yield return matchedGuid;
                    }
                }

                break;

            case RszArrayNode arrayNode:
                foreach (var child in arrayNode.Children) {
                    foreach (var matchedGuid in FindReferencedTargets(child, targetGuids)) {
                        yield return matchedGuid;
                    }
                }

                break;
        }
    }

    private static RszObjectNode SetBoolFieldIfPresent(RszObjectNode component, string fieldName, bool value) {
        var index = component.Type.FindFieldIndex(fieldName);
        if (index == -1 ||
            component.Children[index] is not RszValueNode valueNode ||
            valueNode.Type != RszFieldType.Bool ||
            RszSerializer.Deserialize<bool>(valueNode) == value) {
            return component;
        }

        return component.SetField(fieldName, value);
    }

    private static RszObjectNode CopyFieldIfPresent(RszObjectNode target, RszObjectNode source, string fieldName) {
        if (target.Type.FindFieldIndex(fieldName) == -1 ||
            source.Type.FindFieldIndex(fieldName) == -1) {
            return target;
        }

        return target.SetField(fieldName, source[fieldName]);
    }

#if false
    public static Dictionary<string, object> ToDictionary(this RszInstance instance)
    {
        var dict = new Dictionary<string, object>();
        for (var i = 0; i < instance.Fields.Length; i++)
        {
            var field = instance.Fields[i];
            if (instance.Values.Length <= i)
                continue;

            var value = instance.Values[i];
            if (value is RszInstance child)
            {
                value = ToDictionary(child);
            }
            else if (value is List<object> list)
            {
                var copy = list.ToList();
                for (var j = 0; j < copy.Count; j++)
                {
                    if (copy[j] is RszInstance el)
                    {
                        copy[j] = ToDictionary(el);
                    }
                }
                value = copy;
            }
            dict[field.name] = value;
        }
        return dict;
    }

    public static string ToSimpleJson(this RszInstance instance)
    {
        var dict = ToDictionary(instance);
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions()
        {
            IncludeFields = true,
            WriteIndented = true
        });
    }
#endif

    public static RszObjectNode Serialize<T>(this RszTypeRepository repo, T obj) {
        return (RszObjectNode)RszSerializer.Serialize(
            repo.FromName(obj!.GetType().FullName!)!,
            obj);
    }

    extension(RszScene scene) {
        public List<RszGameObject> GetGameObjects() {
            var result = new List<RszGameObject>();
            scene.VisitGameObjects(go => result.Add(go));
            return result;
        }
    }

    public static T ReplaceGameObject<T>(
        this T node,
        Guid targetGuid,
        RszGameObject replacement,
        bool keepChildren = true)
        where T : IRszSceneNode {
        if (node.Children.IsDefaultOrEmpty)
            return node;

        var children = node.Children.ToBuilder();

        for (var i = 0; i < children.Count; i++) {
            if (children[i] is RszGameObject oldGameObject && oldGameObject.Guid == targetGuid) {
                var newGameObject = replacement;

                if (keepChildren) {
                    newGameObject = newGameObject.WithChildren(oldGameObject.Children);
                }

                newGameObject = newGameObject.WithGuid(oldGameObject.Guid);

                children[i] = newGameObject;
            } else {
                children[i] = children[i].ReplaceGameObject(targetGuid, replacement, keepChildren);
            }
        }

        return (T)node.WithChildren(children.ToImmutable());
    }
}
