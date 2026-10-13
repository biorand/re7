using System.Text.RegularExpressions;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Enemies.Impl;
using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Weapons;

namespace Biohazard.BioRand.RE7.Tests;

public class ConfigurationIdUsageTest {
    private const string ConfigReadMethodPattern =
        "(?:Get(?:ConfigOption|ValueOrDefault)|Read(?:OrDefault|EnemyDropConfigOrDefault))";
    private const string LuaConfigReadMethodPattern =
        "(?:(?:[a-zA-Z_][a-zA-Z0-9_.]*\\.)?config:get|self:config)";

    private readonly HashSet<string> _definedIds =
        RandomizerExecutor.ConfigurationDefinition.AllItems
            .Where(i => i.Id != null)
            .Select(i => i.Id!)
            .ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> RuntimeConfigIds = new(StringComparer.Ordinal){
        "biorand-seed",
        "debug-recipes-enabled", // Derived from the authorized Debug Recipes snapshot when packaging the runtime.
        "username",
        "special",
        "tags",
        "shuffle-chapters", // Retired public option, still read to reject unsafe legacy profiles.
    };

    private static readonly HashSet<string> IndirectlyReferencedConfigIds = new(StringComparer.Ordinal){
        "debug-force-reframework",
        ChapterJumpDataModifier.StartChapterConfigKey,
        EnemyModifier.EnemyForceTargetingProbabilityConfigKey,
    };

    private static readonly Lazy<HashSet<string>> GeneratedConfigIds = new(CreateGeneratedConfigIds);

    [Fact]
    public void Test_All_StringId_References_Exist() {
        var projectRoot = GetProjectRoot();
        var invalidUsages = GetReferencedConfigIds(projectRoot)
            .Where(id => !IsValidId(id))
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(invalidUsages.Count == 0,
            $"Invalid config ID usages found: {string.Join(", ", invalidUsages)}");
    }

    [Fact]
    public void Test_All_ConfigIds_Are_Referenced() {
        var projectRoot = GetProjectRoot();
        var referencedIds = GetReferencedConfigIds(projectRoot);

        var unused = _definedIds
            .Where(id =>
                !referencedIds.Contains(id) &&
                !GeneratedConfigIds.Value.Contains(id) &&
                !IndirectlyReferencedConfigIds.Contains(id))
            .ToList();

        Assert.True(unused.Count == 0,
            $"Unused config IDs: {string.Join(", ", unused)}");
    }

    private static HashSet<string> GetReferencedConfigIds(string projectRoot) {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configRegex = new Regex(
            $"{ConfigReadMethodPattern}(?:<[^>]+>)?\\s*\\(\\s*\"([a-zA-Z0-9\\-]+)\"",
            RegexOptions.Compiled);
        var luaConfigRegex = new Regex(
            $"{LuaConfigReadMethodPattern}\\s*\\(\\s*\"([a-zA-Z0-9\\-]+)\"\\s*[,)]",
            RegexOptions.Compiled);
        var luaConfigDefinitionRegex = new Regex(
            "(?:enabled|duration)\\s*=\\s*\"([a-zA-Z0-9\\-]+)\"",
            RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)) {
            foreach (Match match in configRegex.Matches(File.ReadAllText(file))) {
                result.Add(match.Groups[1].Value);
            }
        }

        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.lua", SearchOption.AllDirectories)) {
            var content = File.ReadAllText(file);
            foreach (Match match in luaConfigRegex.Matches(content)) {
                result.Add(match.Groups[1].Value);
            }
            foreach (Match match in luaConfigDefinitionRegex.Matches(content)) {
                result.Add(match.Groups[1].Value);
            }
        }

        return result;
    }

    private bool IsValidId(string value) {
        if (_definedIds.Contains(value))
            return true;

        return RuntimeConfigIds.Contains(value) ||
               GeneratedConfigIds.Value.Contains(value);
    }

    private static HashSet<string> CreateGeneratedConfigIds() {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        AddDropIds(ids, "enemy-drop", ItemDrops.GenericRuntimeDrops);
        AddDropIds(ids, "item-drop", ItemDrops.GenericDrops);
        AddEnemyIds(ids);
        AddPlayerIds(ids);
        AddInventoryIds(ids);
        AddWeaponIds(ids);

        return ids;
    }

    private static void AddDropIds(HashSet<string> ids, string configPrefix, IEnumerable<string> genericDrops) {
        foreach (var drop in genericDrops) {
            ids.Add($"{configPrefix}-ratio-{ItemDrops.GetConfigId(drop)}");
        }

        foreach (var drop in ItemDrops.HighValueDrops) {
            ids.Add($"{configPrefix}-valuable-{drop}");
        }
    }

    private static void AddEnemyIds(HashSet<string> ids) {
        foreach (var enemy in EnemyDefinitions.Instance.Randomizable) {
            var id = enemy.Id.ToLowerInvariant();
            ids.Add($"enemy-ratio-{id}");
        }

        // Vanilla-only enemies still read their speed settings through directive modifiers.
        foreach (var enemy in EnemyDefinitions.Instance.All.Where(enemy => enemy.SupportsSpeedRandomization)) {
            var id = enemy.SpeedConfigId.ToLowerInvariant();
            ids.Add($"enemy-speed-min-{id}");
            ids.Add($"enemy-speed-max-{id}");
        }

        foreach (var enemy in EnemyDefinitions.Instance.All) {
            if (enemy is EvelineGrandmother or MoldedBlade)
                continue;

            ids.Add($"enemy-drop-probability-{enemy.Id.ToLowerInvariant()}");
        }

        foreach (var enemy in EnemyDefinitions.Instance.All) {
            if (enemy is MargeStalker or MoldedBlade or EvelineGrandmother)
                continue;

            var prefix = enemy.IsBoss ? "boss" : "enemy";
            foreach (var healthPart in enemy.HealthParts) {
                var id = healthPart.ConfigId.ToLowerInvariant();
                ids.Add($"{prefix}-health-min-{id}");
                ids.Add($"{prefix}-health-max-{id}");
            }
        }
    }

    private static void AddPlayerIds(HashSet<string> ids) {
        foreach (var healthLevel in PlayerModifier.MaxHealthLevels) {
            ids.Add(healthLevel.FromConfigId);
            ids.Add(healthLevel.ToConfigId);
        }

        foreach (var reloadSpeedLevel in PlayerModifier.ReloadSpeedLevels) {
            ids.Add(reloadSpeedLevel.FromConfigId);
            ids.Add(reloadSpeedLevel.ToConfigId);
        }
    }

    private static void AddInventoryIds(HashSet<string> ids) {
        foreach (var character in new[]{ "ethan", "mia" }) {
            foreach (var category in Enum.GetValues<StartingWeaponCategory>()) {
                ids.Add($"inventory-weapon-{category.ToString().ToLowerInvariant()}-{character}");
            }
        }

        foreach (var item in ItemDefinitionRepository.Default.Items.Where(item => item.IsStackLimitConfigurable)) {
            ids.Add(item.StackLimitConfigId);
        }
    }

    private static void AddWeaponIds(HashSet<string> ids) {
        var weaponDefinitions = WeaponDefinitionRepository.Default;
        var weapons = weaponDefinitions.PlayerWeapons
            .Where(wp => !wp.WeaponId.ToString().Contains("blaster", StringComparison.InvariantCultureIgnoreCase));
        foreach (var weapon in weapons) {
            var id = SanitizeWeaponId(weapon);
            ids.Add($"weapon-damage-min-{id}");
            ids.Add($"weapon-damage-max-{id}");
        }

        foreach (var gun in weaponDefinitions.Guns.Where(gun =>
                     gun.UserType == Enums.app.CharacterDefine.Type.Player)) {
            var id = SanitizeWeaponId(gun);
            ids.Add($"weapon-ammo-capacity-min-{id}");
            ids.Add($"weapon-ammo-capacity-max-{id}");
            ids.Add($"weapon-reload-speed-min-{id}");
            ids.Add($"weapon-reload-speed-max-{id}");
            foreach (var stat in WeaponModifier.GunStatRandomizations) {
                ids.Add(stat.ToggleConfigId);
                ids.Add(stat.GetMinConfigId(gun.WeaponId));
                ids.Add(stat.GetMaxConfigId(gun.WeaponId));
            }
        }
    }

    private static string SanitizeWeaponId(WeaponDefinition weapon)
        => weapon.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");

    private string GetProjectRoot() {
        var dir = AppContext.BaseDirectory;

        while (dir != null && Directory.GetFiles(dir, "*.sln").Length == 0) {
            dir = Directory.GetParent(dir)?.FullName;
        }

        if (dir == null)
            throw new InvalidOperationException("Could not locate BioRand 7 solution root!");

        return dir;
    }
}
