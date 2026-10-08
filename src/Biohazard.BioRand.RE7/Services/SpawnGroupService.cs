using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;

namespace Biohazard.BioRand.RE7.Services;

internal sealed class SpawnGroupService {
    public Dictionary<string, SpawnGroupDefinition> Definitions { get; }
    public Dictionary<Guid, (string Scene, string Group, bool Aggro)> Membership { get; } = [];
    public HashSet<Guid> Suppressed { get; } = [];
    public HashSet<Guid> RetiredSpawnInfos { get; } = [];
    public SpawnGroupManifest Manifest { get; private set; }

    public SpawnGroupService(Randomizer randomizer) {
        Definitions = SpawnGroupTable.Parse(randomizer.DynamicData.GetData(DynamicDataName.SpawnGroups)!);
        Manifest = new(2, randomizer.Seed, []);
        foreach (var row in Csv.Deserialize<PlacementRow>(randomizer.DynamicData.GetData(DynamicDataName.Enemies)!)) {
            if (string.IsNullOrWhiteSpace(row.SpawnGroup)) continue;
            var rule = randomizer.EnemyPlacementService.GetRule(row.SceneFile, row.Guid);
            if (rule.Preserve || rule.Cull) continue;
            if (!string.IsNullOrWhiteSpace(row.Dlc))
                throw new InvalidDataException($"SpawnGroup '{row.SpawnGroup}' does not support DLC encounters.");
            if (!row.IsSpawnInfo || row.Guid == Guid.Empty)
                throw new InvalidDataException($"SpawnGroup '{row.SpawnGroup}' requires a valid EnemySpawnInfo: {row.Guid}.");
            Register(row.SceneFile, row.Guid, row.SpawnGroup, rule.Aggro);
        }
    }

    public void Register(string scene, Guid guid, string name, bool aggro = false) {
        name = name.Trim();
        if (name.Length == 0) return;
        if (!Definitions.TryGetValue(name, out var definition))
            throw new InvalidDataException($"Unknown SpawnGroup '{name}' for {guid} in {scene}.");
        if (string.IsNullOrWhiteSpace(scene) || !ScriptedSceneSafety.AllowsEnemyMutation(scene))
            throw new InvalidDataException($"SpawnGroup '{name}' cannot control scripted flashback placements.");
        var membership = (scene.Replace('\\', '/').ToLowerInvariant(), definition.Name, aggro);
        if (Membership.TryGetValue(guid, out var existing) && existing != membership)
            throw new InvalidDataException($"Conflicting SpawnGroups for enemy {guid}.");
        Membership[guid] = membership;
    }

    public void ReplaceWithStatic(Guid source, Guid actor) {
        if (!Membership.Remove(source, out var member)) return;
        RetiredSpawnInfos.Add(source);
        Register(member.Scene, actor, member.Group, member.Aggro);
    }

    public void RegisterDuplicate(Guid source, Guid clone) {
        if (Membership.TryGetValue(source, out var member)) {
            Register(member.Scene, clone, member.Group, member.Aggro);
            if (Suppressed.Contains(source)) Suppressed.Add(clone);
        }
    }

    public void Suppress(Guid guid) {
        if (Membership.ContainsKey(guid)) Suppressed.Add(guid);
    }

    public void SetManifest(List<SpawnGroupManifestEntry> groups) => Manifest = Manifest with { Groups = groups };

    private sealed class PlacementRow {
        public Guid Guid { get; set; }
        public string SceneFile { get; set; } = "";
        public bool IsSpawnInfo { get; set; }
        public string SpawnGroup { get; set; } = "";
        public string Dlc { get; set; } = "";
    }
}
