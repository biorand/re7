using Biohazard.BioRand.RE7.Enemies;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Modifiers;

// Runs after replacement/multiplication so the manifest contains the final spawn identities.
internal sealed class SpawnGroupModifier(Randomizer randomizer) : Modifier {
    public override void Apply(RandomizerLogger logger) {
        var service = randomizer.SpawnGroupService;
        if (service.Membership.Count == 0) return;
        var members = new Dictionary<string, List<SpawnGroupMember>>(StringComparer.OrdinalIgnoreCase);
        var controlled = new HashSet<Guid>();
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
                if (spawn == null || spawn.UnitAlias is not ("Em4000" or "Em4100" or "Em4200"))
                    throw new InvalidDataException($"SpawnGroup '{member.Group}' only supports ordinary Molded spawn slots: {guid}.");
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
        logger.LogLine($"SpawnGroups: {members.Count} groups controlling {members.Values.Sum(x => x.Count)} Molded spawn slots.");
    }
}
