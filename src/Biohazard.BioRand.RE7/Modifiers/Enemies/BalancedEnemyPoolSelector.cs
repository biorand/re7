using Biohazard.BioRand.RE7.Enemies;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Modifiers;

internal static class BalancedEnemyPoolSelector {
    internal static bool IsCompatibleReplacement(
        IEnemyDefinition enemy,
        int? chapter,
        string scenePath) {
        var maxStrength = GetMaxEnemyStrength(chapter, scenePath);
        if (maxStrength == null)
            return true;

        return GetEnemyStrength(enemy) <= maxStrength.Value;
    }

    internal static ImmutableArray<EnemyTableEntry> Select(
        IReadOnlyList<EnemyTableEntry> enemyPool,
        int? chapter,
        string scenePath) {
        var maxStrength = GetMaxEnemyStrength(chapter, scenePath);
        if (maxStrength == null)
            return [.. enemyPool];

        return [.. enemyPool.Where(entry => GetEnemyStrength(entry.Enemy) <= maxStrength.Value)];
    }

    private static int GetEnemyStrength(IEnemyDefinition enemy) {
        // Encounter availability is distinct from health. Elder Eveline is an
        // invulnerable hazard, while fat Molded are suitable once Ethan reaches
        // the Main House. Neither belongs in the poorly equipped Guest House.
        if (enemy.Id is "MoldedFat" or "EvelineElderly")
            return 3_000;

        if (enemy.BaseHealth == int.MaxValue)
            return int.MaxValue;

        return enemy.IsBoss
            ? Math.Max(enemy.BaseHealth, 6_000)
            : enemy.BaseHealth;
    }

    private static int? GetMaxEnemyStrength(int? chapter, string scenePath) {
        if (chapter == null)
            return null;

        if (chapter <= 1)
            return 1_000;

        if (chapter == 3) {
            var progression = GetChapter3Progression(scenePath);
            return progression switch{
                <= 2 => 3_000,
                3 => 6_000,
                _ => 20_000,
            };
        }

        return null;
    }

    private static int GetChapter3Progression(string scenePath) {
        var normalizedScenePath = scenePath.Replace('\\', '/');
        if (normalizedScenePath.Contains("chapter3_5", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("enemy_c03_5", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("/c03_boat", StringComparison.OrdinalIgnoreCase)) {
            return 5;
        }

        if (normalizedScenePath.Contains("chapter3_4", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("enemy_c03_4", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("/c03_labo", StringComparison.OrdinalIgnoreCase)) {
            return 4;
        }

        if (normalizedScenePath.Contains("chapter3_3", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("enemy_c03_3", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("/c03_oldhouse", StringComparison.OrdinalIgnoreCase) ||
            normalizedScenePath.Contains("/c03_greenhouse", StringComparison.OrdinalIgnoreCase)) {
            return 3;
        }

        return 2;
    }
}
