using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using Csv = Biohazard.BioRand.RE7.Serialization.Csv;

namespace Biohazard.BioRand.RE7.Enemies;

internal sealed record SpawnGroupCondition(string Parameter, string State, float? X, float? Y, float? Z,
    float? Radius, float Time);
internal sealed record SpawnGroupDefinition(string Name, List<SpawnGroupCondition> Conditions);
internal sealed record SpawnGroupMember(string Scene, Guid Guid, Guid RuntimeGuid, bool Aggro, string Kind = "generator");
internal sealed record SpawnGroupManifestEntry(string Name, List<SpawnGroupCondition> Conditions,
    List<SpawnGroupMember> Members);
internal sealed record SpawnGroupManifest(int Version, int Seed, List<SpawnGroupManifestEntry> Groups);

internal static class SpawnGroupTable {
    public static Dictionary<string, SpawnGroupDefinition> Parse(byte[] bytes) {
        var groups = new Dictionary<string, SpawnGroupDefinition>(StringComparer.OrdinalIgnoreCase);
        SpawnGroupDefinition? current = null;
        foreach (var row in Csv.Deserialize<Row>(bytes)) {
            if (string.IsNullOrWhiteSpace(row.Name) && string.IsNullOrWhiteSpace(row.Parameter) &&
                string.IsNullOrWhiteSpace(row.State) && row.X == null && row.Y == null && row.Z == null &&
                row.Radius == null && row.Time == null)
                continue; // Blank separators / notes do not reset the carried group name.
            var name = row.Name.Trim();
            if (name.Length != 0) {
                if (!groups.TryGetValue(name, out current)) {
                    current = new(name, []);
                    groups.Add(name, current);
                }
            }
            void Fail(string message) => throw new InvalidDataException($"SpawnGroups row {row.RowNumber}: {message}");
            if (current == null) Fail("a Name is required before continuation rows.");
            var parameter = row.Parameter.Trim().ToLowerInvariant();
            if (parameter == "waypoint") Fail("waypoint is not supported yet; ordinary Molded lifecycle control only.");
            if (parameter is not ("spawn" or "despawn" or "suspend" or "resume"))
                Fail($"unknown Parameter '{row.Parameter}'.");
            var hasPosition = row.X != null || row.Y != null || row.Z != null || row.Radius != null;
            if (hasPosition && (row.X == null || row.Y == null || row.Z == null || row.Radius == null || row.Radius <= 0))
                Fail("position triggers require X, Y, Z and a positive Radius.");
            if (new[] { row.X, row.Y, row.Z, row.Radius, row.Time }.Any(value => value.HasValue && !float.IsFinite(value.Value)))
                Fail("coordinates, radius and time must be finite.");
            if (row.Time < 0) Fail("Time must be zero or positive.");
            if (parameter != "spawn" && !hasPosition && string.IsNullOrWhiteSpace(row.State))
                Fail("despawn/suspend/resume require a State or position trigger.");
            var state = row.State.Trim();
            // Qualified state syntax is GUID|core|state; ordinary unique state names also work.
            if (state.Contains('|')) {
                var parts = state.Split('|');
                if (parts.Length != 3 || !Guid.TryParse(parts[0], out _) ||
                    !uint.TryParse(parts[1], out _) || string.IsNullOrWhiteSpace(parts[2]))
                    Fail("qualified State must be FsmGameObjectGuid|CoreId|StateName.");
                state = $"{Guid.Parse(parts[0]):D}|{uint.Parse(parts[1])}|{parts[2].Trim()}";
            }
            current!.Conditions.Add(new(parameter, state, row.X, row.Y, row.Z, row.Radius, row.Time ?? 0));
        }
        return groups;
    }

    private sealed class Row {
        [RowNumber] public int RowNumber { get; set; }
        public string Name { get; set; } = "";
        public string Parameter { get; set; } = "";
        public string State { get; set; } = "";
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Z { get; set; }
        public float? Radius { get; set; }
        public float? Time { get; set; }
    }
}
