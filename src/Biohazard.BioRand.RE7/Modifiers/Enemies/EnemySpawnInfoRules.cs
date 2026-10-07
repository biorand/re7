using IntelOrca.Biohazard.REE.Rsz;
using Biohazard.BioRand.RE7.Enemies;

namespace Biohazard.BioRand.RE7.Modifiers;

internal static class EnemySpawnInfoRules {
    private const string OldHouseBugEnemyScenePath = "natives/stm/scenes/chapter/chapter3/enemy_c03_3.scn.20";

    private static readonly HashSet<Guid> BarnFightMoldeds =[
        new("3d39aa00-a4f6-48ab-87f5-8f04dbfc13a5"),
        new("7ae3d438-f9cb-49da-9a60-00435b946a59"),
    ];

    private static readonly HashSet<Guid> MargueritePitFightSpawns =[
        new("d484bae0-a8bf-4633-a917-d0aade800111"),
        new("28c36110-42dd-4a12-b6ed-389c1d97c779"),
        new("d3f157fa-68b6-0270-1678-e3ab4e066613"),
        new("21410999-80f4-02e8-2180-dc308b20b4e3"),
        new("a2143fb9-f0d0-034d-3e86-3e6f6056b159"),
        new("17e1a46c-c5a0-0db8-2359-659c65131060"),
        new("c927df77-f5ef-018d-0dbb-761b332d90bf"),
        new("44468ff6-b747-0f57-2472-38ce265840ea"),
        new("6aa86358-9661-0e1d-3a22-107860110dd9"),
        new("8ba82066-2552-0866-1b55-eb8aa5e7fa87"),
        new("478ac89b-7c37-083c-297f-74e790824f22"),
        new("73e69068-0827-0d3a-3612-324f64e7e264"),
        new("64af1c7e-05b4-085f-0abf-15b9c233779c"),
        new("3d24872f-0990-0e4f-2dbe-536696a000c3"),
        new("dc4a746c-4fba-0d5f-0754-6ffae69a1a28"),
    ];

    private static readonly HashSet<string> InsectSpawnAliases = new(StringComparer.OrdinalIgnoreCase){
        "Em5400",
        "Em5510",
        "Em5511",
        "Em5512",
        "Em5520",
    };

    internal static bool ShouldReplaceSpawnInfo(RszGameObject spawnInfoGameObject) {
        var component = spawnInfoGameObject.FindComponent<app.EnemySpawnInfo>();
        return component?.Enabled == true
               && !BarnFightMoldeds.Contains(spawnInfoGameObject.Guid)
               && !MargueritePitFightSpawns.Contains(spawnInfoGameObject.Guid)
               && !HasNonDefaultMoldedQuickAppearMode(spawnInfoGameObject)
               && !IsExtraEnemySpawnInfo(spawnInfoGameObject);
    }

    internal static bool HasNonDefaultMoldedQuickAppearMode(RszGameObject spawnInfoGameObject) {
        var option = spawnInfoGameObject.FindComponent<app.EnemySpawnInfoOptionEm4100>();
        var appearType = option?.ThinkSet?.AppearSet?.AppearType;
        return appearType is { } value && value != Enums.app.Em4100.ThinkAppearSet.Type.Default;
    }

    internal static bool IsExtraEnemySpawnInfo(RszGameObject gameObject) {
        var spawnInfo = gameObject.FindComponent<app.EnemySpawnInfo>();
        return spawnInfo?.Comment.StartsWith(ExtraEnemySceneBuilder.SpawnInfoPrefix, StringComparison.Ordinal) == true;
    }

    internal static bool IsInsectSpawnAlias(string unitAlias)
        => InsectSpawnAliases.Contains(unitAlias);

    internal static bool RequiresInsectReplacement(string scenePath, RszGameObject spawnInfoGameObject) {
        if (!string.Equals(NormalizePath(scenePath), OldHouseBugEnemyScenePath, StringComparison.OrdinalIgnoreCase))
            return false;

        var spawnInfo = spawnInfoGameObject.FindComponent<app.EnemySpawnInfo>();
        return spawnInfo != null && IsInsectSpawnAlias(spawnInfo.UnitAlias);
    }

    internal static bool SupportsForceTargetingOption(RszObjectNode component)
        => component.Type.Name.Contains("EnemySpawnInfoOption", StringComparison.Ordinal)
           && component.Type.FindFieldIndex("IsForceTargetingToPlayer") != -1;

    internal static (string Tags, string Include, string Exclude) GetDefaultPlacementDirectives(
        string path, RszGameObject gameObject, IEnemyDefinition definition, bool isSpawnInfo, bool enabled) {
        if (!enabled || !isSpawnInfo || definition.IsBoss ||
            NormalizePath(path).StartsWith("natives/stm/scenes/enemy/", StringComparison.OrdinalIgnoreCase) ||
            !ScriptedSceneSafety.AllowsEnemyMutation(path) || !ShouldReplaceSpawnInfo(gameObject)) {
            return ("preserve nodup", "", "");
        }

        var tags = new List<string>();
        if (gameObject.Components.Any(component => SupportsForceTargetingOption(component) &&
                component.Get<bool>("IsForceTargetingToPlayer")))
            tags.Add("aggro");
        var specialAppearance = definition.EnemyId switch {
            EnemyID.Em4000 => gameObject.FindComponent<app.EnemySpawnInfoOptionEm4000>()?.ThinkSet?.AppearSet?.AppearType
                is { } appear && appear != Enums.app.Em4000.ThinkAppearSet.Type.Default,
            EnemyID.Em4100 => HasNonDefaultMoldedQuickAppearMode(gameObject),
            _ => false,
        };
        if (definition.IsInsect || definition.EnemyId == EnemyID.Em4200 || specialAppearance)
            tags.Add("nodup");

        if (definition.IsInsect)
            return (string.Join(' ', tags), "FlyingBug InsectHive InsectSwarm", "Molded* Mia* Jack* Marge* Eveline*");
        if (definition.IsMolded) {
            var fat = definition.EnemyId == EnemyID.Em4200;
            return (string.Join(' ', tags),
                "Molded MoldedBlade MoldedQuick" + (fat ? " MoldedFat" : ""),
                (fat ? "" : "MoldedFat ") + "Mia* Jack* Marge* Eveline* FlyingBug Insect*");
        }
        return ("preserve nodup", "", "");
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');
}
