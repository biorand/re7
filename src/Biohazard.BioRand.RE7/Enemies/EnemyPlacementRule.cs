using System.IO.Enumeration;

namespace Biohazard.BioRand.RE7.Enemies;

internal sealed class EnemyPlacementRule {
    public static EnemyPlacementRule Default { get; } = new("", "", "");

    public bool Preserve { get; }
    public bool Aggro { get; }
    public bool NoDuplicates { get; }
    public bool Cull { get; }
    private readonly string[] _include;
    private readonly string[] _exclude;

    public EnemyPlacementRule(string tags, string include, string exclude) {
        var tokens = Split(tags).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = tokens.FirstOrDefault(tag => tag.ToLowerInvariant() is not
            ("preserve" or "aggro" or "nodup" or "cull" or "prefab" or "exclude"));
        if (unknown != null)
            throw new InvalidDataException($"Unknown enemy placement tag '{unknown}'.");

        // Older downloaded sheets used prefab/exclude to protect original actors.
        Preserve = tokens.Overlaps(["preserve", "prefab", "exclude"]);
        Aggro = tokens.Contains("aggro");
        NoDuplicates = tokens.Contains("nodup");
        Cull = tokens.Contains("cull");
        if (Cull && (Preserve || Aggro))
            throw new InvalidDataException("Enemy placement 'cull' cannot be combined with 'preserve' or 'aggro'.");
        _include = Split(include);
        _exclude = Split(exclude);
    }

    public bool AllowsReplacement(IEnemyDefinition enemy)
        => !Preserve && !Cull &&
           (_include.Length == 0 || _include.Any(pattern => Matches(pattern, enemy.Id))) &&
           !_exclude.Any(pattern => Matches(pattern, enemy.Id));

    private static bool Matches(string pattern, string id)
        => FileSystemName.MatchesSimpleExpression(pattern, id, ignoreCase: true);

    private static string[] Split(string value)
        => value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
