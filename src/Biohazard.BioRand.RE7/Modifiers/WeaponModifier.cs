using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Weapons;
using Enums.app;
using IntelOrca.Biohazard.REE.Rsz;
using System.Globalization;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class WeaponModifier : Modifier {
    private readonly Randomizer _randomizer;

    public WeaponModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/weapons";
    private const string WeaponItemMessagePath = "message/ui_item_mes.msg";
    private const string ItemSettingsDirectory = "natives/stm/prefab/item";
    private const string ReloadSpeedMultiplierConfigPrefix = "weapon-reload-speed-multiplier";
    private const string RollDescriptionPrefix = "BioRand:";
    private const double DefaultWeaponReloadSpeedMin = 0.3;
    private const double DefaultWeaponReloadSpeedMax = 1.8;

    public static IReadOnlyList<WeaponGunStatRandomization> GunStatRandomizations { get; } =[
        new(
            ConfigId: "range",
            GroupLabel: "Range",
            RollLabel: "Range",
            ToggleLabel: "Randomize Range",
            ToggleDescription: "Scales weapon range and damage falloff distances.",
            SliderLabel: "Range Multiplier",
            Min: 0.1,
            Max: 3,
            Step: 0.05,
            DefaultMin: 0.75,
            DefaultMax: 1.5),
        new(
            ConfigId: "radius",
            GroupLabel: "Hit Radius",
            RollLabel: "Hit radius",
            ToggleLabel: "Randomize Hit Radius",
            ToggleDescription: "Scales the collision radius used by gun projectiles.",
            SliderLabel: "Hit Radius Multiplier",
            Min: 0.1,
            Max: 3,
            Step: 0.05,
            DefaultMin: 0.75,
            DefaultMax: 1.5),
        new(
            ConfigId: "accuracy",
            GroupLabel: "Accuracy",
            RollLabel: "Spread",
            ToggleLabel: "Randomize Accuracy",
            ToggleDescription: "Scales hip-fire and aimed spread. Lower spread multipliers make guns more accurate.",
            SliderLabel: "Spread Multiplier",
            Min: 0,
            Max: 3,
            Step: 0.05,
            DefaultMin: 0.5,
            DefaultMax: 1.5),
        new(
            ConfigId: "recoil",
            GroupLabel: "Recoil",
            RollLabel: "Recoil",
            ToggleLabel: "Randomize Recoil",
            ToggleDescription: "Scales vertical and horizontal recoil angles.",
            SliderLabel: "Recoil Multiplier",
            Min: 0,
            Max: 3,
            Step: 0.05,
            DefaultMin: 0.5,
            DefaultMax: 1.5)
    ];

    private readonly WeaponDefinitionRepository _weaponDefinitions = WeaponDefinitionRepository.Default;
    private readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;

    public override void LogState(RandomizerLogger logger) {
        var randomizer = _randomizer;
        foreach (var definition in _weaponDefinitions.WeaponDefinitions) {
            if (string.IsNullOrEmpty(definition.UserParamsPath)) {
                continue;
            }

            var data = randomizer.FileRepository.DeserializeUserFile<app.WeaponGunParameter>(definition.UserParamsPath);
            var name = definition.Name ?? definition.WeaponId.ToString();
            logger.LogLine($"[{definition.UserParamsPath}] {name}: {data.Format()}");
        }
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var rng = randomizer.GetRng(RandomizerKey);
        var rolls = new Dictionary<WeaponID, WeaponStatRolls>();
        if (randomizer.GetConfigOption<bool>("weapon-mod-damage")) {
            RandomizeWeaponDamage(randomizer, logger, rng, rolls);
        }

        if (randomizer.GetConfigOption<bool>("weapon-mod-ammo-capacity")) {
            RandomizeAmmoCapacities(randomizer, logger, rng, rolls);
        }

        if (randomizer.GetConfigOption<bool>("weapon-mod-reload-speed")) {
            RecordReloadSpeedRolls(randomizer, rolls);
            LogReloadSpeedRuntimeHandling(logger);
        }

        RandomizeGunParameterStats(randomizer, logger, rolls);

        ApplyWeaponDescriptions(randomizer, logger, rolls);
    }

    private void ModifyDamageInRcol(
        string rcolPath,
        WeaponDefinition weapon,
        Randomizer randomizer,
        RandomizerLogger logger,
        double factor,
        bool randomizeStun,
        bool randomizePlayerDmg
    ) {
        int ScaleDamage(int value) => Math.Max(0, (int)Math.Round(value * factor));

        logger.Push(weapon.Name ?? weapon.WeaponId.ToString());
        randomizer.FileRepository.ModifyRcolFile(rcolPath, rcol => {
            foreach (var requestSet in rcol.RequestSets) {
                if (requestSet.UserData == null) {
                    logger.LogLine($"Skipping request set {requestSet.Name} in {rcolPath}, it has no user data?!");
                    continue;
                }

                var isDefaultBulletRcol = rcolPath.Contains("defaultbullet.rcol");
                if (isDefaultBulletRcol && !string.Equals(requestSet.Name, weapon.WeaponId.ToString(),
                        StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var isPlayerRequestSet = requestSet.Name.Contains("player", StringComparison.OrdinalIgnoreCase);
                if (isPlayerRequestSet && !randomizePlayerDmg) {
                    continue;
                }

                if (requestSet.UserData.Type.Name != "app.Collision.AttackUserData") {
                    continue;
                }

                var attackUserData = RszSerializer.Deserialize<app.Collision.AttackUserData>(requestSet.UserData)!;
                var prevDmg = attackUserData.Damage;
                var prevStun = attackUserData.Stun;

                attackUserData.Damage = ScaleDamage(attackUserData.Damage);
                if (randomizeStun) {
                    attackUserData.Stun = ScaleDamage(attackUserData.Stun);
                }

                if (prevDmg == attackUserData.Damage) {
                    logger.LogLine($"Damage of RequestSet {requestSet.Name} remains ({prevDmg})");
                } else {
                    logger.LogLine(
                        $"Damage of RequestSet {requestSet.Name} changes from {prevDmg} to {attackUserData.Damage}");
                }

                if (prevStun == attackUserData.Stun) {
                    logger.LogLine($"Stun of RequestSet {requestSet.Name} remains ({prevStun})");
                } else {
                    logger.LogLine(
                        $"Stun of RequestSet {requestSet.Name} changes from {prevStun} to {attackUserData.Stun}");
                }

                requestSet.UserData = (RszObjectNode)RszSerializer.Serialize(requestSet.UserData.Type, attackUserData);
            }
        });
        logger.Pop();
    }

    private void RandomizeWeaponDamage(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        Dictionary<WeaponID, WeaponStatRolls> rolls) {
        var randomizeStun = randomizer.GetConfigOption<bool>("weapon-mod-damage-include-stun");
        var randomizePlayerDmg = randomizer.GetConfigOption<bool>("weapon-mod-damage-include-player-damage");

        foreach (var definition in _weaponDefinitions.WeaponDefinitions) {
            var sanitizedId = definition.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");
            var min = randomizer.GetConfigOption($"weapon-damage-min-{sanitizedId}", -1d);
            var max = randomizer.GetConfigOption($"weapon-damage-max-{sanitizedId}", -1d);
            if ((min == -1d && max == -1d) || (min == 1.0d && max == 1.0d)) {
                continue;
            }

            var factor = Math.Round(rng.NextDouble(min, max), 1);
            GetOrCreateRolls(rolls, definition.WeaponId).DamageMultiplier = factor;
            foreach (var rcolPath in definition.RcolPaths) {
                ModifyDamageInRcol(rcolPath, definition, randomizer, logger, factor, randomizeStun, randomizePlayerDmg);
            }
        }
    }

    private void RandomizeAmmoCapacities(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        Dictionary<WeaponID, WeaponStatRolls> rolls) {
        var ensureAtLeastOneBullet = randomizer.GetConfigOption<bool>("weapon-mod-ammo-capacity-prevent-zero");
        var minCap = ensureAtLeastOneBullet ? 1 : 0;

        foreach (var definition in _weaponDefinitions.WeaponDefinitions) {
            if (definition.UserParamsPath == null || !definition.IsGun) {
                continue;
            }

            var name = definition.Name;
            var sanitizedId = definition.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");
            var min = randomizer.GetConfigOption<double>($"weapon-ammo-capacity-min-{sanitizedId}");
            var max = randomizer.GetConfigOption<double>($"weapon-ammo-capacity-max-{sanitizedId}");
            var factor = Math.Round(rng.NextDouble(min, max), 1);
            GetOrCreateRolls(rolls, definition.WeaponId).AmmoCapacityMultiplier = factor;

            var previousLoadNum = 0;
            var newLoadNum = 0;
            randomizer.FileRepository.ModifyUserFile<app.WeaponGunParameter>(definition.UserParamsPath, root => {
                previousLoadNum = root.MaxLoadNum;
                newLoadNum = Math.Max(minCap, (int)Math.Round(root.MaxLoadNum * factor));

                if (root.MaxLoadNum == newLoadNum) {
                    logger.LogLine($"Ammo capacity of {name} remains ({root.MaxLoadNum})");
                } else {
                    logger.LogLine($"Changing ammo capacity of {name} from {root.MaxLoadNum} to {newLoadNum}");
                }

                root.MaxLoadNum = newLoadNum;
                return root;
            });

            UpdateWeaponPrefabLoadNums(randomizer, logger, definition, previousLoadNum, newLoadNum);
        }
    }

    private static void UpdateWeaponPrefabLoadNums(
        Randomizer randomizer,
        RandomizerLogger logger,
        WeaponDefinition definition,
        int previousLoadNum,
        int newLoadNum) {
        if (previousLoadNum <= 0 || previousLoadNum == newLoadNum) {
            return;
        }

        foreach (var path in GetWeaponPrefabPaths(definition)) {
            if (!randomizer.FileRepository.Exists(path)) {
                continue;
            }

            var changedCount = 0;
            var pfbFile = randomizer.FileRepository.GetPfbFile(path).ToBuilder(randomizer.FileRepository.TypeRepository);
            pfbFile.Scene = pfbFile.Scene.VisitComponents((_, component) => {
                if (component.Type.Name != "app.WeaponGun") {
                    return component;
                }

                var gun = RszSerializer.Deserialize<app.WeaponGun>(component)!;
                if (!IsWeaponGunForDefinition(gun, definition)) {
                    return component;
                }

                var changed = false;
                foreach (var bulletInfo in gun.BulletInfoList) {
                    if (bulletInfo.LoadNum <= 0) {
                        continue;
                    }

                    var scaledLoadNum = ScaleLoadNum(bulletInfo.LoadNum, previousLoadNum, newLoadNum);
                    if (bulletInfo.LoadNum == scaledLoadNum) {
                        continue;
                    }

                    bulletInfo.LoadNum = scaledLoadNum;
                    changed = true;
                    changedCount++;
                }

                return changed
                    ? (RszObjectNode)RszSerializer.Serialize(component.Type, gun)
                    : component;
            });

            if (changedCount == 0) {
                continue;
            }

            randomizer.FileRepository.SetPfbFile(path, pfbFile.AddMissingResources().Build());
            logger.LogLine(
                $"Changing {changedCount} default loaded ammo value(s) in {path} to match {definition.Name ?? definition.WeaponId.ToString()} ammo capacity.");
        }
    }

    private static int ScaleLoadNum(int loadNum, int previousLoadNum, int newLoadNum) {
        if (newLoadNum <= 0) {
            return 0;
        }

        return Math.Max(1, (int)Math.Round(loadNum * (newLoadNum / (double)previousLoadNum)));
    }

    private static bool IsWeaponGunForDefinition(app.WeaponGun gun, WeaponDefinition definition)
        => gun.WeaponID == definition.WeaponId ||
           string.Equals(
               NormalizeUserDataPath(gun.WeaponGunParameter.Path),
               NormalizeUserDataPath(definition.UserParamsPath),
               StringComparison.OrdinalIgnoreCase);

    private static string NormalizeUserDataPath(string? path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return string.Empty;
        }

        var result = path.Trim().Replace('\\', '/');
        const string prefix = "natives/stm/";
        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
            result = result[prefix.Length..];
        }

        var versionSuffix = $".{FileVersions.UserFileVersion}";
        if (result.EndsWith(versionSuffix, StringComparison.OrdinalIgnoreCase)) {
            result = result[..^versionSuffix.Length];
        }

        return result;
    }

    private static IEnumerable<string> GetWeaponPrefabPaths(WeaponDefinition definition) {
        var result = new List<string>();
        void AddPath(string path) {
            if (!result.Contains(path, StringComparer.OrdinalIgnoreCase)) {
                result.Add(path);
            }
        }

        if (!string.IsNullOrWhiteSpace(definition.PrefabPath)) {
            AddPath(definition.PrefabPath);
        }

        if (string.IsNullOrWhiteSpace(definition.UserParamsPath)) {
            return result;
        }

        var parameterPath = definition.UserParamsPath.Replace('\\', '/');
        var separatorIndex = parameterPath.LastIndexOf('/');
        if (separatorIndex < 0) {
            return result;
        }

        var directory = parameterPath[..separatorIndex];
        var folderName = directory[(directory.LastIndexOf('/') + 1)..];
        var parameterFile = parameterPath[(separatorIndex + 1)..];
        if (parameterFile.Contains("_reward_parameter.", StringComparison.OrdinalIgnoreCase)) {
            AddPath($"{directory}/rewardinventory/{folderName}_rewardinventory.pfb.{FileVersions.PfbFileVersion}");
            AddPath($"{directory}/reward_item/{folderName}_reward_item.pfb.{FileVersions.PfbFileVersion}");
            return result;
        }

        AddPath($"{directory}/inventory/{folderName}_inventory.pfb.{FileVersions.PfbFileVersion}");
        AddPath($"{directory}/item/{folderName}_item.pfb.{FileVersions.PfbFileVersion}");
        AddPath($"{directory}/detailsearch/{folderName}_detailsearch.pfb.{FileVersions.PfbFileVersion}");
        return result;
    }

    private void RecordReloadSpeedRolls(Randomizer randomizer, Dictionary<WeaponID, WeaponStatRolls> rolls) {
        foreach (var definition in _weaponDefinitions.Guns.Where(x =>
                     x.UserType == Enums.app.CharacterDefine.Type.Player)) {
            if (!TryGetReloadSpeedRange(randomizer, definition, out var min, out var max)) {
                continue;
            }

            var rng = randomizer.GetRng(RandomizerKey, "reload-speed", definition.WeaponId);
            var factor = Math.Round(rng.NextDouble(min, max), 3);
            GetOrCreateRolls(rolls, definition.WeaponId).ReloadSpeedMultiplier = factor;
            randomizer.Input.Configuration[GetReloadSpeedMultiplierConfigId(definition.WeaponId)] = factor;
        }
    }

    private void RandomizeGunParameterStats(
        Randomizer randomizer,
        RandomizerLogger logger,
        Dictionary<WeaponID, WeaponStatRolls> rolls) {
        var activeStats = GunStatRandomizations
            .Where(stat => randomizer.GetConfigOption<bool>(stat.ToggleConfigId))
            .ToArray();
        if (activeStats.Length == 0) {
            return;
        }

        foreach (var definition in _weaponDefinitions.Guns.Where(x =>
                     x.UserType == Enums.app.CharacterDefine.Type.Player)) {
            if (definition.UserParamsPath == null) {
                continue;
            }

            var name = definition.Name ?? definition.WeaponId.ToString();
            var statRolls = new List<(WeaponGunStatRandomization Stat, double Multiplier)>();
            foreach (var stat in activeStats) {
                var min = randomizer.GetConfigOption<double>(stat.GetMinConfigId(definition.WeaponId));
                var max = randomizer.GetConfigOption<double>(stat.GetMaxConfigId(definition.WeaponId));
                if (max < min) {
                    (min, max) = (max, min);
                }

                var rng = randomizer.GetRng(RandomizerKey, stat.ConfigId, definition.WeaponId);
                var multiplier = Math.Round(rng.NextDouble(min, max), 3);
                statRolls.Add((stat, multiplier));
                GetOrCreateRolls(rolls, definition.WeaponId).GunStatMultipliers[stat.ConfigId] = multiplier;
            }

            randomizer.FileRepository.ModifyUserFile<app.WeaponGunParameter>(definition.UserParamsPath, root => {
                foreach (var (stat, multiplier) in statRolls) {
                    var before = DescribeGunStat(root, stat);
                    ApplyGunStat(root, stat, multiplier);
                    var after = DescribeGunStat(root, stat);
                    if (before == after) {
                        logger.LogLine($"{stat.GroupLabel} of {name} remains ({before})");
                    } else {
                        logger.LogLine(
                            $"Changing {stat.GroupLabel.ToLowerInvariant()} of {name} from {before} to {after}");
                    }
                }

                return root;
            });
        }
    }

    private void ApplyWeaponDescriptions(
        Randomizer randomizer,
        RandomizerLogger logger,
        IReadOnlyDictionary<WeaponID, WeaponStatRolls> rolls) {
        var activeRolls = rolls
            .Where(x => x.Value.HasAny)
            .ToDictionary(x => x.Key, x => x.Value);
        if (activeRolls.Count == 0) {
            return;
        }

        var itemDataByWeaponId = GetWeaponItemDataByWeaponId(randomizer, activeRolls.Keys);
        if (itemDataByWeaponId.Count == 0) {
            return;
        }

        randomizer.FileRepository.ModifyMsgFile(WeaponItemMessagePath.MessageFile(), messages => {
            foreach (var (weaponId, roll) in activeRolls.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal)) {
                if (!itemDataByWeaponId.TryGetValue(weaponId, out var itemData) || itemData.ManualMsg == Guid.Empty) {
                    continue;
                }

                var message = messages.FindMessage(itemData.ManualMsg);
                if (message == null) {
                    logger.LogLine($"Weapon description for {weaponId} not found in ui_item_mes.msg.");
                    continue;
                }

                messages.SetStringAll(itemData.ManualMsg, roll.Format());

                logger.LogLine($"Added weapon roll description for {weaponId}: {roll.Format()}");
            }
        });
    }

    private Dictionary<WeaponID, app.ItemData> GetWeaponItemDataByWeaponId(Randomizer randomizer,
        IEnumerable<WeaponID> weaponIds) {
        var result = new Dictionary<WeaponID, app.ItemData>();
        var pendingWeaponIds = weaponIds.ToHashSet();
        var itemDefinitionsBySource = pendingWeaponIds
            .Select(id => _itemDefinitions.FromWeaponId(id))
            .Where(item => item?.SourceUserFile != null)
            .GroupBy(item => item!.SourceUserFile!, StringComparer.OrdinalIgnoreCase);

        foreach (var group in itemDefinitionsBySource) {
            var path = $"{ItemSettingsDirectory}/{group.Key}".ToLowerInvariant();
            if (!randomizer.FileRepository.Exists(path)) {
                continue;
            }

            var settings = randomizer.FileRepository.DeserializeUserFile<app.ItemSettings>(path);
            var itemDataByItemId = settings._Settings.ToDictionary(x => x.ItemDataID, StringComparer.OrdinalIgnoreCase);
            foreach (var itemDefinition in group) {
                if (itemDefinition?.WeaponId == null ||
                    !itemDataByItemId.TryGetValue(itemDefinition.Id, out var itemData)) {
                    continue;
                }

                result[itemDefinition.WeaponId.Value] = itemData;
            }
        }

        return result;
    }

    private static bool TryGetReloadSpeedRange(Randomizer randomizer, WeaponDefinition definition, out double min,
        out double max) {
        var sanitizedId = SanitizeWeaponId(definition.WeaponId);
        min = randomizer.GetConfigOption($"weapon-reload-speed-min-{sanitizedId}", double.NaN);
        max = randomizer.GetConfigOption($"weapon-reload-speed-max-{sanitizedId}", double.NaN);

        if (double.IsNaN(min) && double.IsNaN(max)) {
            return false;
        }

        if (double.IsNaN(min)) {
            min = DefaultWeaponReloadSpeedMin;
        }

        if (double.IsNaN(max)) {
            max = DefaultWeaponReloadSpeedMax;
        }

        if (max < min) {
            (min, max) = (max, min);
        }

        return true;
    }

    private static WeaponStatRolls GetOrCreateRolls(Dictionary<WeaponID, WeaponStatRolls> rolls, WeaponID weaponId) {
        if (!rolls.TryGetValue(weaponId, out var roll)) {
            roll = new WeaponStatRolls();
            rolls[weaponId] = roll;
        }

        return roll;
    }

    private static string SanitizeWeaponId(WeaponID weaponId)
        => weaponId.ToString().ToLowerInvariant().Replace("_", "-");

    private static string GetReloadSpeedMultiplierConfigId(WeaponID weaponId)
        => $"{ReloadSpeedMultiplierConfigPrefix}-{SanitizeWeaponId(weaponId)}";

    private static string FormatMultiplier(double multiplier)
        => multiplier.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatFloat(float value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static float ScaleFloat(float value, double multiplier)
        => (float)Math.Round(value * multiplier, 3);

    private static float ScalePositiveFloat(float value, double multiplier) {
        if (value == 0) {
            return 0;
        }

        return Math.Max(0.001f, ScaleFloat(value, multiplier));
    }

    private static void ApplyGunStat(app.WeaponGunParameter root, WeaponGunStatRandomization stat, double multiplier) {
        switch (stat.ConfigId) {
            case "range":
                root.Range = ScalePositiveFloat(root.Range, multiplier);
                root.AttenuationStart = ScalePositiveFloat(root.AttenuationStart, multiplier);
                root.AttenuationEnd = ScalePositiveFloat(root.AttenuationEnd, multiplier);
                break;
            case "radius":
                root.Radius = ScalePositiveFloat(root.Radius, multiplier);
                break;
            case "accuracy":
                root.DiffusionRadius = ScaleFloat(root.DiffusionRadius, multiplier);
                root.AimDiffusionRadius = ScaleFloat(root.AimDiffusionRadius, multiplier);
                break;
            case "recoil":
                root.RecoilXAngle = ScaleFloat(root.RecoilXAngle, multiplier);
                root.RecoilYAngle = ScaleFloat(root.RecoilYAngle, multiplier);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stat), stat.ConfigId, null);
        }
    }

    private static string DescribeGunStat(app.WeaponGunParameter root, WeaponGunStatRandomization stat)
        => stat.ConfigId switch{
            "range" => $"range {FormatFloat(root.Range)}, falloff {FormatFloat(root.AttenuationStart)}-" +
                       $"{FormatFloat(root.AttenuationEnd)}",
            "radius" => $"radius {FormatFloat(root.Radius)}",
            "accuracy" => $"hip {FormatFloat(root.DiffusionRadius)}, aim {FormatFloat(root.AimDiffusionRadius)}",
            "recoil" => $"x {FormatFloat(root.RecoilXAngle)}, y {FormatFloat(root.RecoilYAngle)}",
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat.ConfigId, null)
        };

    private void LogReloadSpeedRuntimeHandling(RandomizerLogger logger) {
        logger.LogLine("Weapon reload speed randomization is applied by the REFramework plugin at runtime.");
    }

    public sealed record WeaponGunStatRandomization(
        string ConfigId,
        string GroupLabel,
        string RollLabel,
        string ToggleLabel,
        string ToggleDescription,
        string SliderLabel,
        double Min,
        double Max,
        double Step,
        double DefaultMin,
        double DefaultMax) {
        public string ToggleConfigId => $"weapon-mod-{ConfigId}";
        public string GetMinConfigId(WeaponID weaponId) => $"weapon-{ConfigId}-min-{SanitizeWeaponId(weaponId)}";
        public string GetMaxConfigId(WeaponID weaponId) => $"weapon-{ConfigId}-max-{SanitizeWeaponId(weaponId)}";
    }

    private sealed class WeaponStatRolls {
        public double? DamageMultiplier { get; set; }
        public double? AmmoCapacityMultiplier { get; set; }
        public double? ReloadSpeedMultiplier { get; set; }
        public Dictionary<string, double> GunStatMultipliers { get; } = [];

        public bool HasAny =>
            DamageMultiplier != null || AmmoCapacityMultiplier != null || ReloadSpeedMultiplier != null ||
            GunStatMultipliers.Count != 0;

        public string Format() {
            var parts = new List<string>();
            if (DamageMultiplier != null) {
                parts.Add($"Damage {FormatMultiplier(DamageMultiplier.Value)}x");
            }

            if (AmmoCapacityMultiplier != null) {
                parts.Add($"Ammo capacity {FormatMultiplier(AmmoCapacityMultiplier.Value)}x");
            }

            if (ReloadSpeedMultiplier != null) {
                parts.Add($"Reload speed {FormatMultiplier(ReloadSpeedMultiplier.Value)}x");
            }

            foreach (var stat in GunStatRandomizations) {
                if (GunStatMultipliers.TryGetValue(stat.ConfigId, out var multiplier)) {
                    parts.Add($"{stat.RollLabel} {FormatMultiplier(multiplier)}x");
                }
            }

            return $"{RollDescriptionPrefix} {string.Join(", ", parts)}";
        }
    }

    // TODO: Acid/Fire Bullets
    // Example file: em8100slipparameter.user.2
}
