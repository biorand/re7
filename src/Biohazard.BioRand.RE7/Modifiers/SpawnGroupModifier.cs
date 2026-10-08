using Biohazard.BioRand.RE7.Enemies;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Modifiers;

// Runs after replacement/multiplication so the manifest contains the final spawn identities.
internal sealed class SpawnGroupModifier(Randomizer randomizer) : Modifier {
    public override void Apply(RandomizerLogger logger) {
        var service = randomizer.SpawnGroupService;
        if (service.Membership.Count == 0) return;
        var members = new Dictionary<string, List<SpawnGroupMember>>(StringComparer.OrdinalIgnoreCase);
        var controlled = new HashSet<Guid>(service.RetiredSpawnInfos);
        var runtimeGuids = new HashSet<Guid>();
        foreach (var sceneGroup in service.Membership.OrderBy(x => x.Key).GroupBy(x => x.Value.Scene)) {
            var builder = randomizer.FileRepository.GetScnFile(sceneGroup.Key)
                .ToBuilder(randomizer.FileRepository.TypeRepository);
            var scene = builder.Scene;
            foreach (var (guid, member) in sceneGroup) {
                var gameObject = scene.FindGameObject(guid);
                if (gameObject == null) {
                    if (service.Suppressed.Contains(guid)) {
                        controlled.Add(guid);
                        continue;
                    }
                    throw new InvalidDataException($"SpawnGroup '{member.Group}' cannot find enemy {guid} in {member.Scene}.");
                }
                var spawn = gameObject.FindComponent<app.EnemySpawnInfo>();
                if (spawn == null) {
                    var (actor, saveGuid) = PrepareStaticActor(gameObject, randomizer);
                    if (saveGuid == Guid.Empty || !runtimeGuids.Add(saveGuid))
                        throw new InvalidDataException($"SpawnGroup member {guid} has an empty or duplicate runtime save GUID.");
                    scene = scene.UpdateGameObject(actor);
                    if (!service.Suppressed.Contains(guid)) {
                        if (!members.TryGetValue(member.Group, out var actors)) members[member.Group] = actors = [];
                        actors.Add(new(member.Scene, guid, saveGuid, member.Aggro, "static"));
                    }
                    continue;
                }
                if (!SupportsGeneratorAlias(spawn.UnitAlias))
                    throw new InvalidDataException($"SpawnGroup '{member.Group}' has an unsupported enemy spawn slot: {guid}.");
                if (!EnemySpawnInfoRules.IsExtraEnemySpawnInfo(gameObject) &&
                    !EnemySpawnInfoRules.ShouldReplaceSpawnInfo(gameObject))
                    throw new InvalidDataException($"SpawnGroup '{member.Group}' targets a protected native encounter: {guid}.");
                if (spawn.MyGUID == Guid.Empty || !runtimeGuids.Add(spawn.MyGUID))
                    throw new InvalidDataException($"SpawnGroup member {guid} has an empty or duplicate runtime save GUID.");
                // Turn off autonomous activation. The controller owns lifecycle requests.
                spawn.autoSpawnParameter.IsAutoSpawnedAtLoad = false;
                spawn.autoSpawnParameter.AutoSpawnedXZRange = 0;
                spawn.autoSpawnParameter.AutoSpawnedYRange = 0;
                foreach (var condition in spawn.RespawnConditions)
                    condition.respawnCondition.IsUse = false;
                // A requested suspension must complete in place and retain health/limbs.
                // Native self-suspension would otherwise fight the group's desired state.
                spawn.suspendParameter.IsOnTheSpot = true;
                spawn.suspendParameter.IsForgetBackup = false;
                spawn.suspendParameter.isUseSelfSuspend = false;
                spawn.suspendParameter.inCameraCondition.IsUseCheck = false;
                spawn.resumeParameter.inCameraCondition.IsUseCheck = false;
                scene = scene.UpdateGameObject(gameObject.AddOrUpdateComponent(spawn));
                controlled.Add(guid);
                if (service.Suppressed.Contains(guid)) continue;
                if (!members.TryGetValue(member.Group, out var list)) members[member.Group] = list = [];
                list.Add(new(member.Scene, guid, spawn.MyGUID, member.Aggro));
            }
            builder.Scene = scene;
            randomizer.AreaService.UpdateCachedScene(sceneGroup.Key, scene);
            randomizer.FileRepository.SetScnFile(sceneGroup.Key, builder.AddMissingResources().Build());
        }
        // References can live in separate difficulty/level FSM scenes. Keep FSM UIDs and
        // unrelated progression actions intact; only disable this controller's requests.
        var paths = AreaDefinitionRepository.Default.All.Where(x => x.Dlc == null).Select(x => x.Path)
            .Concat(service.Membership.Values.Select(x => x.Scene))
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal);
        foreach (var path in paths) {
            var builder = randomizer.FileRepository.GetScnFile(path).ToBuilder(randomizer.FileRepository.TypeRepository);
            var changed = false;
            builder.Scene = builder.Scene.Visit(node => {
                if (node is RszObjectNode action && action.Type.Name == "app.fsm.EnemyGenerate" &&
                    controlled.Contains(action.Get<Guid>("SpawnInfo"))) {
                    changed = true;
                    return action.SetField("v0_Enabled", false).SetField("SpawnInfo", Guid.Empty);
                }
                return node;
            });
            if (!changed) continue;
            randomizer.AreaService.UpdateCachedScene(path, builder.Scene);
            randomizer.FileRepository.SetScnFile(path, builder.AddMissingResources().Build());
        }
        service.SetManifest(members.OrderBy(x => x.Key, StringComparer.Ordinal).Select(pair =>
            new SpawnGroupManifestEntry(pair.Key, service.Definitions[pair.Key].Conditions, pair.Value)).ToList());
        logger.LogLine($"SpawnGroups: {members.Count} groups controlling {members.Values.Sum(x => x.Count)} enemy spawn slots.");
    }

    private static (RszGameObject Actor, Guid SaveGuid) PrepareStaticActor(RszGameObject actor, Randomizer randomizer) {
        var save = actor.FindComponent("app.Em3300.Em3300Save");
        if (save == null && actor.Name.StartsWith($"{ExtraEnemySceneBuilder.StaticPrefix}_Em2000_", StringComparison.Ordinal)) {
            // Mia's EnemySave owns health. A separate native OtherObjectSave stores group
            // activity in its extensible OtherInt data, without changing her AI save fields.
            save = randomizer.FileRepository.TypeRepository.Create("app.OtherObjectSave")
                .SetField("Enabled", true)
                .SetField("SaveGUID", randomizer.GetRng("spawn-groups/static-state", actor.Guid).NextGuid())
                .SetField("IsNotSaveBasicData", true);
            actor = actor.AddOrUpdateComponent(save);
        }
        if (save == null || (actor.Name != "Em3300_Static" &&
            !actor.Name.StartsWith(ExtraEnemySceneBuilder.StaticPrefix + "_", StringComparison.Ordinal)))
            throw new InvalidDataException($"SpawnGroups cannot control an unsupported or original scripted static actor: {actor.Guid}.");
        // Awake/save registration still runs; rendering and gameplay wait for the controller.
        actor = actor.WithSettings(actor.Settings.SetField("Update", false).SetField("Draw", false));
        return (actor, save.Get<Guid>("SaveGUID"));
    }

    private static bool SupportsGeneratorAlias(string alias) {
        // Hive variants share the registered Em5510 component/option stack.
        if (alias is "Em5511" or "Em5512") alias = "Em5510";
        return EnemyDefinitions.Instance.FromId(alias)?.SpawnOptionType != null;
    }

}
