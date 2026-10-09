using Biohazard.BioRand.RE7.Enemies;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Modifiers;

internal sealed class ExtraEnemyPlacement {
    public bool Enabled { get; init; }
    public string Id { get; init; } = "";
    public string Include { get; init; } = "";
    public string Exclude { get; init; } = "";
    public string SpawnGroup { get; init; } = "";
    public string Comment { get; init; } = "";
    public string SceneFile { get; init; } = "";
    public int Chapter { get; init; }
    public float PosX { get; init; }
    public float PosY { get; init; }
    public float PosZ { get; init; }
    public float RotX { get; init; }
    public float RotY { get; init; }
    public float RotZ { get; init; }
    public float RotW { get; init; }
}

internal sealed record ResolvedExtraEnemyPlacement(
    ExtraEnemyPlacement Placement,
    IEnemyDefinition Enemy
);

internal sealed class ExtraEnemyGeneratorBuild {
    public List<RszGameObject> SpawnInfos { get; } = [];
    public List<RszGameObject> Instances { get; } = [];
}

internal static class ExtraEnemyPlanner {
    internal static bool UsesConfiguredPool(ExtraEnemyPlacement placement)
        => placement.Id.Trim().Equals("random", StringComparison.OrdinalIgnoreCase) ||
           (string.IsNullOrWhiteSpace(placement.Id) && string.IsNullOrWhiteSpace(placement.Include));

    internal static ImmutableArray<IEnemyDefinition> GetExplicitCandidates(ExtraEnemyPlacement placement,
        EnemyPlacementRule rule) {
        IEnumerable<IEnemyDefinition> candidates;
        if (string.IsNullOrWhiteSpace(placement.Id)) {
            // An authored Include list replaces legacy fixed/pipe IDs, independently of profile ratios.
            candidates = EnemyDefinitions.Instance.Randomizable;
        } else {
            candidates = placement.Id.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(id => EnemyDefinitions.Instance.All.FirstOrDefault(enemy =>
                        enemy.Id.Equals(id, StringComparison.OrdinalIgnoreCase)) ??
                    EnemyDefinitions.Instance.FromId(id) ?? throw new InvalidOperationException(
                        $"Unknown extra enemy id '{placement.Id}' (selected '{id}')."));
        }
        return candidates.Where(enemy => enemy.SupportsRandomEnemyPlacement && rule.AllowsReplacement(enemy))
            .ToImmutableArray();
    }

    internal static int? GetSharedChapter(IEnumerable<ExtraEnemyPlacement> placements) {
        var chapters = placements
            .Select(extraEnemy => extraEnemy.Chapter)
            .Distinct()
            .ToArray();

        return chapters.Length == 1
            ? chapters[0]
            : null;
    }

    internal static ImmutableArray<ExtraEnemyPlacement> SelectRandomPlacementsWithoutReplacement(
        List<ExtraEnemyPlacement> placements,
        int count,
        Rng rng) {
        if (count <= 0)
            return [];

        if (count >= placements.Count)
            return [.. placements];

        var remainingPlacements = placements.ToList();
        var selectedPlacements = ImmutableArray.CreateBuilder<ExtraEnemyPlacement>(Math.Max(0, count));
        while (selectedPlacements.Count < count && remainingPlacements.Count > 0) {
            var selectedPlacement = rng.Next(remainingPlacements);
            selectedPlacements.Add(selectedPlacement);
            remainingPlacements.Remove(selectedPlacement);
        }

        return selectedPlacements.ToImmutable();
    }

    internal static int GetSubsetCount(int placementCount, double percentage) {
        if (placementCount <= 0)
            return 0;

        var safePercentage = Math.Clamp(percentage, 0.0, 1.0);
        return Math.Min(
            placementCount,
            Math.Max(0, (int)Math.Round(placementCount * safePercentage, MidpointRounding.AwayFromZero)));
    }

    internal static bool TryCreateRequest(
        RandomizerLogger logger,
        ExtraEnemyPlacement extraEnemy,
        IEnemyDefinition definition,
        out ResolvedExtraEnemyPlacement request) {
        if (!definition.SupportsRandomEnemyPlacement) {
            logger.LogLine(
                $"Skipping {definition.Name} at {extraEnemy.PosX}/{extraEnemy.PosY}/{extraEnemy.PosZ}: enemy is not supported for extra placement.");
            request = null!;
            return false;
        }

        logger.LogLine($"{definition.Name} at {extraEnemy.PosX}/{extraEnemy.PosY}/{extraEnemy.PosZ}");
        request = new ResolvedExtraEnemyPlacement(extraEnemy, definition);
        return true;
    }
}
