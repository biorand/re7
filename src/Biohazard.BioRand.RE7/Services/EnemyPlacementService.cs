using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Serialization;

namespace Biohazard.BioRand.RE7.Services;

internal sealed class EnemyPlacementService {
    private readonly Dictionary<(string Scene, Guid Guid), EnemyPlacementRule> _rules = [];
    private readonly Dictionary<Guid, EnemyPlacementRule> _spawnRules = [];
    private readonly Dictionary<string, HashSet<Guid>> _culls = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, HashSet<Guid>> Culls => _culls;
    public HashSet<Guid> CulledSpawnInfoGuids { get; } = [];
    public HashSet<string> AggroScenePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

    public EnemyPlacementService(Randomizer randomizer)
        : this(randomizer.DynamicData.GetData(DynamicDataName.Enemies)!) { }

    internal EnemyPlacementService(byte[] data) {
        foreach (var row in Csv.Deserialize<PlacementRow>(data)) {
            if (row.Guid == Guid.Empty || string.IsNullOrWhiteSpace(row.SceneFile))
                continue;
            EnemyPlacementRule rule;
            try {
                rule = new EnemyPlacementRule(row.Tags, row.Include, row.Exclude);
            } catch (InvalidDataException ex) {
                throw new InvalidDataException($"Enemy placement {row.Guid} in {row.SceneFile}: {ex.Message}", ex);
            }
            var scene = NormalizePath(row.SceneFile);
            if (rule.Aggro && !row.IsSpawnInfo)
                throw new InvalidDataException($"Enemy placement {row.Guid}: 'aggro' requires an EnemySpawnInfo row.");
            if ((rule.Cull || rule.Aggro) && !Modifiers.ScriptedSceneSafety.AllowsEnemyMutation(scene))
                throw new InvalidDataException($"Enemy placement {row.Guid}: scripted flashback scenes cannot use 'cull' or 'aggro'.");
            _rules[(scene, row.Guid)] = rule;
            if (row.IsSpawnInfo)
                _spawnRules[row.Guid] = rule;
            if (rule.Aggro)
                AggroScenePaths.Add(scene);
            if (rule.Cull) {
                if (!_culls.TryGetValue(scene, out var guids)) {
                    guids = [];
                    _culls.Add(scene, guids);
                }
                guids.Add(row.Guid);
                if (row.IsSpawnInfo)
                    CulledSpawnInfoGuids.Add(row.Guid);
            }
        }
    }

    public EnemyPlacementRule GetRule(string scenePath, Guid guid)
        => _rules.GetValueOrDefault((NormalizePath(scenePath), guid), EnemyPlacementRule.Default);

    // Generation actions may live in another scene from their spawn-info record.
    public EnemyPlacementRule GetSpawnRule(Guid guid)
        => _spawnRules.GetValueOrDefault(guid, EnemyPlacementRule.Default);

    private static string NormalizePath(string path) => path.Replace('\\', '/').ToLowerInvariant();

    private sealed class PlacementRow {
        public Guid Guid { get; set; }
        public string SceneFile { get; set; } = "";
        public bool IsSpawnInfo { get; set; }
        public string Tags { get; set; } = "";
        public string Include { get; set; } = "";
        public string Exclude { get; set; } = "";
    }
}
