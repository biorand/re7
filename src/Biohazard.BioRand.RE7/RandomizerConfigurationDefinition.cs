using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Enemies.Impl;
using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.BioRand;
using static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition;

namespace Biohazard.BioRand.RE7;

// TODO: Add extension methods like
// public static void AddDropdown(this Group group, GroupItem item) { ... }
internal static class RandomizerConfigurationDefinition {
    private static readonly ItemDefinitionRepository ItemDefinitions = ItemDefinitionRepository.Default;
    private static readonly WeaponDefinitionRepository WeaponDefinitions = WeaponDefinitionRepository.Default;

    private static GroupItem CreateValuableDropSwitch(string prefix, string drop, bool defaultValue)
        => new(){
            Id = $"{prefix}-valuable-{drop}",
            Label = ItemDrops.GetHighValueDropLabel(drop),
            Description = GetValuableDropDescription(drop),
            Type = "switch",
            Default = defaultValue
        };

    private static string? GetValuableDropDescription(string drop)
        => drop switch{
            ItemDrops.Weapon => "Adds all supported weapon types to this drop pool.",
            ItemDrops.DlcCoin => "Adds all five DLC coins to this drop pool.",
            ItemDrops.BirthdaySkill => "Adds Jack's 55th Birthday passive skills. Requires Allow DLC Items.",
            ItemDrops.LockPick => "Adds lock picks.",
            ItemDrops.RepairKit => "Adds repair kits.",
            _ => null
        };

    private static IEnumerable<IEnemyDefinition> GetEnemyDropProbabilityEnemies()
        => EnemyDefinitions.Instance.All
            .Where(enemy => enemy is not EvelineGrandmother and not MoldedBlade)
            .OrderBy(enemy => enemy.Name);

    private static string GetEnemyDropProbabilityLabel(IEnemyDefinition enemy)
        => enemy is Molded
            ? "Molded (Normal / Blade)"
            : enemy.Name;

    private static GroupItem CreateHealthRangeItem(string prefix, IEnemyDefinition enemy, EnemyHealthPart healthPart,
        bool isMin) {
        var labelPrefix = isMin ? "Min" : "Max";
        var defaultValue = Math.Round(healthPart.BaseHealth * (isMin ? 0.75 : 1.25));
        var maxValue = Math.Max(defaultValue, Math.Round(healthPart.BaseHealth * (enemy.IsBoss ? 3.0 : 5.0)));

        return new GroupItem(){
            Id = $"{prefix}-health-{(isMin ? "min" : "max")}-{healthPart.ConfigId.ToLowerInvariant()}",
            Label = $"{GetHealthPartLabel(enemy, healthPart)}: {labelPrefix}. HP",
            Type = "range",
            Min = 1,
            Max = maxValue,
            Step = 1,
            Default = defaultValue
        };
    }

    private static string GetHealthPartLabel(IEnemyDefinition enemy, EnemyHealthPart healthPart) {
        return string.Equals(healthPart.Label, enemy.Name, StringComparison.Ordinal)
            ? enemy.Name
            : $"{enemy.Name} {healthPart.Label}";
    }

    public static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition Create() {
        var configDefinition = new IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition();

        #region General

        var page = configDefinition.CreatePage("General");
        var group = page.CreateGroup("Game Progression");
        group.Items.Add(new GroupItem(){
            Id = ChapterJumpDataModifier.StartChapterConfigKey,
            Label = "Start Chapter",
            Description =
                "Choose where a new game starts.",
            Type = "dropdown",
            Options = [.. ChapterJumpDataModifier.StartChapterOptions.Select(option => option.Name)],
            Default = ChapterJumpDataModifier.NormalStartChapter
        });

        group.Items.Add(new GroupItem(){
            Id = "keep-basement-chainsaw",
            Label = "Keep Chainsaw After Jack 2",
            Description = "Keep the chainsaw after cutting open the basement exit. Disable to discard it as in the original game.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Presentation");

        group.Items.Add(new GroupItem(){
            Id = "randomized-messages",
            Label = "Randomize Flavor Text",
            Description = "Replace selected UI messages with BioRand jokes and alternate text.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "main-menu-biorand-touch",
            Label = "Use BioRand Main Menu Theme",
            Description =
                "Show the BioRand logo and customized New Game text on the main menu.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Quality of Life");

        group.Items.Add(new GroupItem(){
            Id = "madhouse-normal-saves",
            Label = "Use Normal Saving on Madhouse",
            Description =
                "Use the Easy/Normal autosave and manual save behavior on Madhouse, without requiring cassette tapes.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "inventory-unrestricted-management",
            Label = "Flexible Inventory Management",
            Description =
                "Allow Birthday blasters to be moved to item boxes, and allow non-key items and found footage tapes to be discarded.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Content Pools");

        group.Items.Add(new GroupItem(){
            Id = $"allow-bonus-items",
            Label = "Allow Unlockable Items",
            Description =
                "Include unlockable rewards such as the Albert-01R, Infinite Ammo, and defense scrolls in randomized item pools.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"allow-dlc-items",
            Label = "Allow DLC Items",
            Description = "Include supported DLC items and weapons in randomized item pools.",
            Type = "switch",
            Default = true
        });

        #endregion General

        #region Player

        page = configDefinition.CreatePage("Player");
        group = page.CreateGroup("Health");

        group.Items.Add(new GroupItem(){
            Id = "player-random-max-health",
            Label = "Randomize Max Health",
            Description = "Randomize Ethan's maximum health using the ranges below.",
            Type = "switch",
            Default = false
        });

        group = page.CreateGroup("Health Ranges");

        foreach (var healthLevel in PlayerModifier.MaxHealthLevels) {
            group.Items.Add(new GroupItem(){
                Id = healthLevel.FromConfigId,
                Label = $"{healthLevel.Label}: Min. HP",
                Type = "range",
                Min = 1,
                Max = 9999,
                Step = 1,
                Default = healthLevel.DefaultFromHealth
            });

            group.Items.Add(new GroupItem(){
                Id = healthLevel.ToConfigId,
                Label = $"{healthLevel.Label}: Max. HP",
                Type = "range",
                Min = 1,
                Max = 9999,
                Step = 1,
                Default = healthLevel.DefaultToHealth
            });
        }

        group = page.CreateGroup("Base Reload Rate");

        group.Items.Add(new GroupItem(){
            Id = "player-random-reload-speed",
            Label = "Randomize Base Reload Rate",
            Description = "Randomize the player's base reload rate and stabilizer upgrades using the ranges below.",
            Type = "switch",
            Default = false
        });

        group = page.CreateGroup("Reload Rate Ranges");

        foreach (var reloadSpeedLevel in PlayerModifier.ReloadSpeedLevels) {
            group.Items.Add(new GroupItem(){
                Id = reloadSpeedLevel.FromConfigId,
                Label = $"{reloadSpeedLevel.Label}: Min. Reload Rate",
                Type = "range",
                Min = 0.1,
                Max = 5,
                Step = 0.05,
                Default = reloadSpeedLevel.DefaultFromRate
            });

            group.Items.Add(new GroupItem(){
                Id = reloadSpeedLevel.ToConfigId,
                Label = $"{reloadSpeedLevel.Label}: Max. Reload Rate",
                Type = "range",
                Min = 0.1,
                Max = 5,
                Step = 0.05,
                Default = reloadSpeedLevel.DefaultToRate
            });
        }

        group = page.CreateGroup("Psychostimulants");

        group.Items.Add(new GroupItem(){
            Id = "player-random-psychostimulants",
            Label = "Randomize Psychostimulants",
            Description = "Randomize psychostimulant effect duration and detection range.",
            Type = "switch",
            Default = false
        });

        group = page.CreateGroup("Psychostimulant Ranges");

        group.Items.Add(new GroupItem(){
            Id = "player-psychostimulant-duration-min",
            Label = "Min. Duration Multiplier",
            Type = "range",
            Min = 0.1,
            Max = 5,
            Step = 0.05,
            Default = 0.75
        });

        group.Items.Add(new GroupItem(){
            Id = "player-psychostimulant-duration-max",
            Label = "Max. Duration Multiplier",
            Type = "range",
            Min = 0.1,
            Max = 5,
            Step = 0.05,
            Default = 1.5
        });

        group.Items.Add(new GroupItem(){
            Id = "player-psychostimulant-range-min",
            Label = "Min. Range Multiplier",
            Type = "range",
            Min = 0.1,
            Max = 5,
            Step = 0.05,
            Default = 0.75
        });

        group.Items.Add(new GroupItem(){
            Id = "player-psychostimulant-range-max",
            Label = "Max. Range Multiplier",
            Type = "range",
            Min = 0.1,
            Max = 5,
            Step = 0.05,
            Default = 1.5
        });

        #endregion Player

        #region Enemies

        var allEnemies = EnemyDefinitions.Instance.Randomizable.OrderBy(enemy => enemy.Name);
        var bosses = EnemyDefinitions.Instance.Bosses.OrderBy(boss => boss.Name);
        var nonBosses = EnemyDefinitions.Instance.NonBosses.OrderBy(nonBoss => nonBoss.Name);
        var speedConfigurableEnemies = allEnemies.Where(enemy => enemy.SupportsSpeedRandomization);

        page = configDefinition.CreatePage("Enemies");
        group = page.CreateGroup("Randomization");

        group.Items.Add(new GroupItem(){
            Id = $"random-enemies",
            Label = "Randomize Enemies",
            Description = "Replace supported enemies with enemies selected from the configured pool.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = $"extra-enemy-amount",
            Label = "Extra Enemy Placements",
            Description =
                "Select this percentage of configured extra spawn locations, including some peaceful areas and boss arenas.",
            Type = "percent",
            Min = 0,
            Max = 1,
            Step = 0.01,
            Default = 0.25
        });

        group = page.CreateGroup("Population");
        group.Items.Add(new GroupItem(){
            Id = $"enemy-multiplier",
            Label = "Enemy Multiplier",
            Description =
                "Scale the number of existing enemies. Values below 1 remove enemies; values above 1 duplicate them. " +
                "High values may cause stability or performance problems.",
            Type = "range",
            Min = 0.25,
            Max = 5,
            Step = 0.05,
            Default = 1
        });

        var enemyCount = allEnemies.Count();
        group.Items.Add(new GroupItem(){
            Id = $"enemy-variety",
            Label = "Max. Enemy Types per Area",
            Description = "Limit how many different enemy types may be selected for one area.",
            Type = "range",
            Min = 1,
            Max = enemyCount,
            Step = 1,
            Default = enemyCount,
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-pack-max-size",
            Label = "Max. Enemy Pack Size",
            Description = "Allow up to this many adjacent enemies to use the same replacement type.",
            Type = "range",
            Min = 1,
            Max = 10,
            Step = 1,
            Default = 1,
        });

        group.Items.Add(new GroupItem(){
            Id = EnemyModifier.EnemyForceTargetingProbabilityConfigKey,
            Label = "Forced Player Targeting Chance",
            Description = "Chance that supported enemies are forced to target the player after spawning.",
            Type = "percent",
            Min = 0,
            Max = 1,
            Step = 0.01,
            Default = 0.0,
        });

        group = page.CreateGroup("Size");
        group.Items.Add(new GroupItem(){
            Id = $"enemy-scale-probability",
            Label = "Random Scale Chance",
            Description = "Chance that an enemy receives a scale multiplier from the range below.",
            Type = "percent",
            Min = 0.0,
            Max = 1,
            Step = 0.01,
            Default = 0.05
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-scale-min",
            Label = "Min. Enemy Scale",
            Description = "The minimum scale multiplier of enemies.",
            Type = "range",
            Min = 0.25,
            Max = 4.00,
            Step = 0.05,
            Default = 0.25
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-scale-max",
            Label = "Max. Enemy Scale",
            Description = "The maximum scale multiplier of enemies.",
            Type = "range",
            Min = 0.25,
            Max = 4.00,
            Step = 0.05,
            Default = 2
        });

        group = page.CreateGroup("Speed");
        group.Items.Add(new GroupItem(){
            Id = $"random-enemy-speed",
            Label = "Randomize Enemy Speed",
            Description = "Randomize supported enemy speeds using the per-enemy ranges below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-speed-probability",
            Label = "Random Speed Chance",
            Description = "Chance that a supported enemy receives a randomized speed multiplier.",
            Type = "percent",
            Min = 0,
            Max = 1,
            Step = 0.01,
            Default = 1.0
        });

        group = page.CreateGroup("Speed Ranges");
        group.Warning =
            "Enable Randomize Enemy Speed for these ranges to take effect.";
        foreach (var enemy in speedConfigurableEnemies) {
            var speedConfigId = enemy.SpeedConfigId.ToLowerInvariant();
            group.Items.Add(new GroupItem(){
                Id = $"enemy-speed-min-{speedConfigId}",
                Label = $"Min. {enemy.Name} Speed Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 2.00,
                Step = 0.05,
                Default = 0.5
            });

            group.Items.Add(new GroupItem(){
                Id = $"enemy-speed-max-{speedConfigId}",
                Label = $"Max. {enemy.Name} Speed Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 2.00,
                Step = 0.05,
                Default = 2.00
            });
        }

        group = page.CreateGroup("Damage");
        group.Items.Add(new GroupItem(){
            Id = $"random-enemy-damage",
            Label = "Randomize Enemy Damage",
            Description = "Randomize damage from all enemies using the multiplier range below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-insta-death",
            Label = "One-Hit Enemy Damage",
            Description = "Any enemy damage kills the player immediately.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-damage-min",
            Label = "Min. Enemy Damage",
            Description = "The minimum damage multiplier for enemies.",
            Type = "range",
            Min = 0.1,
            Max = 3.00,
            Step = 0.1,
            Default = 0.8
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-damage-max",
            Label = "Max. Enemy Damage",
            Description = "The maximum damage multiplier for enemies.",
            Type = "range",
            Min = 0.1,
            Max = 3.00,
            Step = 0.1,
            Default = 1.2
        });

        group = page.CreateGroup("Progression Balance");
        group.Items.Add(new GroupItem(){
            Id = $"balanced-enemies",
            Label = "Balanced Enemies",
            Description =
                "Keep very strong enemies out of earlier chapters so enemy strength ramps up with game progression. " +
                "Good for permadeath runs but may reduce chaos.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Special Cases");
        group.Items.Add(new GroupItem(){
            Id = $"enemy-evelineelderly-explosive-behavior",
            Label = "Explosive Eveline Elderly",
            Description = "Make Eveline Elderly detonate after the player gets close, then despawn.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Enemy Pool Weights");
        foreach (var enemy in allEnemies) {
            group.Items.Add(new GroupItem(){
                Id = $"enemy-ratio-{enemy.Id.ToLowerInvariant()}",
                Label = enemy.Name,
                Category = new GroupItemCategory(enemy.Category.ToConfigCategory()),
                Type = "range",
                Description = enemy.EnemyId == EnemyID.Em2000
                    ? "Relative selection weight. Set to 0 to exclude it. Only applies to extra enemy placements."
                    : "Relative selection weight. Set to 0 to exclude this enemy from randomization.",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
        }

        var genericEnemyDrops =
            ItemDrops.GenericRuntimeDrops.OrderBy(drop => ItemDefinitions.FromId(drop.ToString())!.CategoryType);
        var genericItemDrops =
            ItemDrops.GenericDrops.OrderBy(drop => ItemDefinitions.FromId(drop.ToString())!.CategoryType);

        group = page.CreateGroup("Enemy Drops");
        group.Warning = "This feature requires RE Framework.";
        group.Items.Add(new GroupItem(){
            Id = $"random-enemy-drops",
            Label = "Enable Enemy Drops",
            Description = "Allow defeated enemies to drop randomized items through RE Framework.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-drop-probability",
            Label = "Default Enemy Drop Chance",
            Description =
                "The fallback probability that a defeated enemy drops an item. Per-enemy probabilities below override this value.",
            Type = "percent",
            Min = 0,
            Max = 1,
            Step = 0.01,
            Default = 0.5
        });

        group = page.CreateGroup("Per-Enemy Drop Chances");
        foreach (var enemy in GetEnemyDropProbabilityEnemies()) {
            group.Items.Add(new GroupItem(){
                Id = $"enemy-drop-probability-{enemy.Id.ToLowerInvariant()}",
                Label = GetEnemyDropProbabilityLabel(enemy),
                Category = new GroupItemCategory(enemy.Category.ToConfigCategory()),
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
        }

        group = page.CreateGroup("Drop Settings");
        group.Items.Add(new GroupItem(){
            Id = $"enemy-drop-respect-difficulty",
            Label = "Scale Ammo Quantities by Difficulty",
            Description = "Drop more ammo on Easy/Normal and less on Madhouse. Disable to use one quantity on every difficulty.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-drop-ammo-only-available-weapons",
            Label = "Only Drop Ammo for Available Weapons",
            Description = "Only drop ammo for weapons that are available before or in the chapter with the drop.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-drop-ammo-min",
            Label = "Min. Ammo Quantity",
            Description = "The minimum percentage of an ammo stack to drop.",
            Type = "percent",
            Min = 0.1,
            Max = 1,
            Step = 0.1,
            Default = 0.1
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-drop-ammo-max",
            Label = "Max. Ammo Quantity",
            Description = "The maximum percentage of an ammo stack to drop.",
            Type = "percent",
            Min = 0.1,
            Max = 10,
            Step = 0.1,
            Default = 0.4
        });

        group = page.CreateGroup("Enemy Drop Weights");

        foreach (var drop in genericEnemyDrops) {
            var category = ItemDrops.GetCategory(drop);
            var (bgColor, textColor) = ItemDrops.GetColor(category);
            group.Items.Add(new GroupItem(){
                Id = $"enemy-drop-ratio-{drop.ToLowerInvariant()}",
                Label = ItemDefinitions.FromId(drop)!.Name,
                Description = "Relative drop weight. Set to 0 to exclude this item from enemy drops.",
                Category = new GroupItemCategory(){
                    Label = category,
                    BackgroundColor = bgColor,
                    TextColor = textColor,
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = ItemDrops.GetDefaultGenericDropRatio(drop)
            });
        }

        group = page.CreateGroup("Valuable Drops");
        group.Advanced = true;
        foreach (var drop in ItemDrops.HighValueDrops) {
            group.Items.Add(CreateValuableDropSwitch("enemy-drop", drop, defaultValue: false));
        }

        #endregion

        #region Enemy health

        page = configDefinition.CreatePage("Enemy Health");
        group = page.CreateGroup("General");

        group.Items.Add(new GroupItem(){
            Id = $"boss-random-health",
            Label = "Randomize Boss Health",
            Description = "Randomize boss health using the absolute HP ranges below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"enemy-random-health",
            Label = "Randomize Regular Enemy Health",
            Description = "Randomize regular enemy health using the absolute HP ranges below.",
            Type = "switch",
            Default = false
        });

        group = page.CreateGroup("Regular Enemies");
        group.Warning = "Enable Randomize Regular Enemy Health for these values to take effect.";
        foreach (var enemy in nonBosses) {
            if (enemy is MargeStalker or MoldedBlade or EvelineGrandmother)
                continue;

            foreach (var healthPart in enemy.HealthParts) {
                group.Items.Add(CreateHealthRangeItem("enemy", enemy, healthPart, isMin: true));
                group.Items.Add(CreateHealthRangeItem("enemy", enemy, healthPart, isMin: false));
            }
        }

        group = page.CreateGroup("Bosses");
        group.Warning = "Enable Randomize Boss Health for these values to take effect.";
        foreach (var boss in bosses) {
            foreach (var healthPart in boss.HealthParts) {
                group.Items.Add(CreateHealthRangeItem("boss", boss, healthPart, isMin: true));
                group.Items.Add(CreateHealthRangeItem("boss", boss, healthPart, isMin: false));
            }
        }

        #endregion

        #region Items

        page = configDefinition.CreatePage("Items");
        group = page.CreateGroup("Item Randomization");

        group.Items.Add(new GroupItem(){
            Id = "random-items",
            Label = "Randomize Items",
            Description =
                "Randomize most static item pickups. Key items, model shotguns, and other unsafe placements are excluded.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "random-key-item-locations",
            Label = "Randomize Key Item Locations",
            Description = "Place supported key items in route-safe normal item locations.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "replace-madhouse-tapes",
            Label = "Include Madhouse Cassette Tapes",
            Description = "Allow Madhouse cassette-tape pickups to be replaced during item randomization.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "replace-weapons",
            Label = "Include Placed Weapons",
            Description =
                "Allow placed weapons, such as the garage G17, to be replaced. Some weapons may then be unavailable in a seed.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"random-bird-cage-magnum",
            Label = "Randomize Bird Cage 44 MAG",
            Description = "Replace the bird-cage 44 MAG with an appropriate guaranteed reward.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"random-bird-cage-drugs-coins",
            Label = "Randomize Bird Cage Drugs and Coins",
            Description =
                "Replace bird-cage steroids, stabilizers, and coins with appropriate guaranteed rewards.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"preserve-item-models",
            Label = "Preserve In-World Item Models",
            Description = "Keep each pickup's original in-world model even when its item behavior is randomized.",
            Type = "switch",
            Default = false,
        });

        group = page.CreateGroup("Drop Settings");

        group.Items.Add(new GroupItem(){
            Id = $"item-drop-respect-difficulty",
            Label = "Scale Ammo Quantities by Difficulty",
            Description = "Drop more ammo on Easy/Normal and less on Madhouse. Disable to use one quantity on every difficulty.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = $"item-drop-ammo-only-available-weapons",
            Label = "Only Drop Ammo for Available Weapons",
            Description = "Only drop ammo for weapons that are available before or in the chapter with the drop. " +
                          "This currently applies to item crates; static ammo replacements remain unrestricted.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"item-drop-ammo-min",
            Label = "Min. Ammo Quantity",
            Description = "The minimum percentage of an ammo stack to drop.",
            Type = "percent",
            Min = 0.1,
            Max = 1,
            Step = 0.1,
            Default = 0.1
        });

        group.Items.Add(new GroupItem(){
            Id = $"item-drop-ammo-max",
            Label = "Max. Ammo Quantity",
            Description = "The maximum percentage of an ammo stack to drop.",
            Type = "percent",
            Min = 0.1,
            Max = 10,
            Step = 0.1,
            Default = 0.4
        });

        group = page.CreateGroup("Additional Items");
        group.Items.Add(new GroupItem(){
            Id = $"additional-items",
            Label = "Add Extra Items",
            Description = "Spawn randomized items at additional preselected locations.",
            Type = "switch",
            Default = true,
        });

        group.Items.Add(new GroupItem(){
            Id = $"additional-items-prefer-healing",
            Label = "Prefer Healing Items and Stat Upgrades",
            Description = "Give extra placements more herbs, first aid medicine, steroids, and stabilizers.",
            Type = "switch",
            Default = false,
        });

        group.Items.Add(new GroupItem(){
            Id = $"additional-wooden-crates",
            Label = "Add Wooden Item Crates",
            Description = "Spawn wooden crates containing randomized items at additional locations.",
            Type = "switch",
            Default = true,
        });

        group.Items.Add(new GroupItem(){
            Id = $"additional-wooden-crates-fakes",
            Label = "Allow Unmarked Explosive Crates",
            Description =
                "Some added crates become explosive traps like those in Ethan Must Die and End of Zoe. " +
                "They are not identified by a different model or ticking sound.",
            Type = "switch",
            Default = true,
        });

        group.Items.Add(new GroupItem(){
            Id = $"additional-wooden-crates-fakes-pct-min",
            Label = "Min. Fake Crate Probability",
            Description = "Minimum chance that an added wooden crate becomes an explosive trap.",
            Type = "percent",
            Min = 0.1,
            Max = 1,
            Step = 0.1,
            Default = 0.3
        });

        group.Items.Add(new GroupItem(){
            Id = $"additional-wooden-crates-fakes-pct-max",
            Label = "Max. Fake Crate Probability",
            Description = "Maximum chance that an added wooden crate becomes an explosive trap.",
            Type = "percent",
            Min = 0.1,
            Max = 1,
            Step = 0.1,
            Default = 0.5
        });

        group = page.CreateGroup("Item Drop Weights");
        foreach (var drop in genericItemDrops) {
            var category = ItemDrops.GetCategory(drop);
            var (bgColor, textColor) = ItemDrops.GetColor(category);
            group.Items.Add(new GroupItem(){
                Id = $"item-drop-ratio-{drop.ToLowerInvariant()}",
                Label = ItemDefinitions.FromId(drop)!.Name,
                Description = "Relative drop weight. Set to 0 to exclude this item from randomized item drops.",
                Category = new GroupItemCategory(){
                    Label = category,
                    BackgroundColor = bgColor,
                    TextColor = textColor,
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = ItemDrops.GetDefaultGenericDropRatio(drop)
            });
        }

        group = page.CreateGroup("Valuable Drops");
        group.Advanced = true;
        foreach (var drop in ItemDrops.HighValueDrops) {
            group.Items.Add(CreateValuableDropSwitch(
                "item-drop",
                drop,
                defaultValue: ItemDrops.GetEnabledValuableDrops().Contains(drop)));
        }

        #endregion Items

        #region Inventory

        page = configDefinition.CreatePage("Inventory");

        group = page.CreateGroup("Starting Weapons");
        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-ethan",
            Label = "Ethan: Random Starting Weapons",
            Description = "Give Ethan one random gun and one random bladed weapon at the start.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-mia",
            Label = "Mia: Random Starting Weapons",
            Description = "Give Mia one random gun and one random bladed weapon at the start.",
            Type = "switch",
            Default = true
        });

        foreach (var character in new[]{ "Ethan", "Mia" }) {
            group.Items.Add(new GroupItem(){
                Id = $"random-starting-inventory-additional-gun-{character.ToLowerInvariant()}",
                Label = $"{character}: Additional Random Gun",
                Description = $"Add one extra gun from {character}'s allowed gun categories, preferring a gun not already in the inventory. " +
                              "Works independently of Random Starting Weapons. Requires at least one allowed gun category.",
                Type = "switch",
                Default = false
            });
        }

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-give-ammo",
            Label = "Provide Starter Ammo",
            Description = "Provide an appropriate starter ammo loadout for each selected starting gun, including the additional gun.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-vhs",
            Label = "Randomize VHS Inventories",
            Description = "Apply starting-inventory randomization to supported Clancy and Mia VHS sections.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Starting Skills and Inventory Size");

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-skills-ethan",
            Label = "Ethan: Random Starting Skill",
            Description =
                "Give Ethan a random level-one passive skill from Jack's 55th Birthday. Requires RE Framework.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-skills-mia",
            Label = "Mia: Random Starting Skill",
            Description =
                "Give Mia a random level-one passive skill from Jack's 55th Birthday. Requires RE Framework.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-size-ethan",
            Label = "Ethan: Inventory Size",
            Description =
                "Set Ethan's inventory capacity. Values above 12 require RE Framework.",
            Type = "dropdown",
            Options = ["12", "16", "20"],
            Default = "12"
        });

        group.Items.Add(new GroupItem(){
            Id = "random-starting-inventory-size-mia",
            Label = "Mia: Inventory Size",
            Description =
                "Set Mia's inventory capacity. Values above 12 require RE Framework.",
            Type = "dropdown",
            Options = ["12", "16", "20"],
            Default = "12"
        });

        var categories = Enum.GetValues<StartingWeaponCategory>();
        foreach (var character in new[]{ "Ethan", "Mia" }) {
            group = page.CreateGroup($"{character}: Allowed Weapon Categories");
            foreach (var category in categories) {
                group.Items.Add(new GroupItem(){
                    Id = $"inventory-weapon-{category.ToString().ToLowerInvariant()}-{character.ToLowerInvariant()}",
                    Description = (category == StartingWeaponCategory.Bladed ? "Knives and Axe" : null),
                    Label = category.GetLabel(),
                    Type = "switch",
                    Default = category is StartingWeaponCategory.Bladed or StartingWeaponCategory.Handgun
                });
            }
        }

        group = page.CreateGroup("Crafting");
        group.Warning = "Adding recipes requires RE Framework to expand the crafting menu from 8 to 20 slots.";
        group.Items.Add(new GroupItem(){
            Id = "recipes-add-new",
            Label = "Add Random Recipes",
            Description = "Add randomized recipes alongside the original recipe data.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "recipes-randomization-mode",
            Label = "Recipe Mode",
            Description = "Controls how ingredients and results are selected.\n" +
                          "Easy: Useful recipes only.\n" +
                          "Balanced: Ingredients and results stay within compatible item categories.\n" +
                          "Chaos: Recipes tend to require more resources for smaller results.\n" +
                          "Crazy: Deliberately nonsensical recipes.\n" +
                          "No crafting: Disables every crafting recipe.",
            Type = "dropdown",
            Options = ["Easy", "Balanced", "Chaos", "Crazy", "No crafting"],
            Default = "Balanced"
        });

        group.Items.Add(new GroupItem(){
            Id = "recipes-unlock-from-start",
            Label = "Unlock Crafting from the Start",
            Description = "Make the combine menu available immediately in a new game.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Recipe Generation");

        group.Items.Add(new GroupItem(){
            Id = "recipes-allow-stabilizers-and-steroids",
            Label = "Allow Stat Upgrades as Results",
            Description = "Allow randomized recipes to produce stabilizers or steroids.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "recipes-random-item-quantities",
            Label = "Randomize Item Quantities",
            Description = "Randomize ingredient and result quantities.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"recipes-count-min",
            Label = "Min. Item Quantity Multiplier",
            Description = "Minimum quantity multiplier when Randomize Item Quantities is enabled. " +
                          "Every ingredient still requires at least one item.",
            Type = "range",
            Min = 0.5,
            Max = 3,
            Step = 0.1,
            Default = 1
        });

        group.Items.Add(new GroupItem(){
            Id = $"recipes-count-max",
            Label = "Max. Item Quantity Multiplier",
            Description = "Maximum quantity multiplier when Randomize Item Quantities is enabled.",
            Type = "range",
            Min = 1,
            Max = 3,
            Step = 0.1,
            Default = 2
        });

        group.Items.Add(new GroupItem(){
            Id = $"recipes-new-min",
            Label = "Min. Number of New Recipes",
            Description = "Minimum number of recipes to add when Add Random Recipes is enabled.",
            Type = "range",
            Min = 1,
            Max = RecipeModifier.MaxRecipeCount,
            Step = 1,
            Default = 4
        });

        group.Items.Add(new GroupItem(){
            Id = $"recipes-new-max",
            Label = "Max. Number of New Recipes",
            Description = "Maximum number of recipes to add when Add Random Recipes is enabled.",
            Type = "range",
            Min = 1,
            Max = RecipeModifier.MaxRecipeCount,
            Step = 1,
            Default = 12
        });

        group = page.CreateGroup("Stack Limits");
        group.Advanced = true;

        var items = from item in ItemDefinitions.Items
            where item.IsStackLimitConfigurable
            select (item.StackLimitConfigId, item.Name, item.MaxStack);

        foreach ((string id, string? name, int maxStack) in items) {
            group.Items.Add(new GroupItem(){
                Id = id,
                Label = name ?? id,
                Type = "range",
                Min = 0,
                Max = 999,
                Step = 1,
                Default = maxStack
            });
        }

        #endregion Inventory

        #region Weapons

        page = configDefinition.CreatePage("Weapons");
        group = page.CreateGroup("Damage");

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-damage",
            Label = "Randomize Damage",
            Description = "Randomize weapon damage using the per-weapon multiplier ranges below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-damage-include-stun",
            Label = "Include Stun",
            Description = "Apply the damage multipliers to stun values as well.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-damage-include-player-damage",
            Label = "Include Self-Damage",
            Description = "Apply the multipliers to attacks that can damage the player, such as remote bombs.",
            Type = "switch",
            Default = true
        });

        group = page.CreateGroup("Damage Ranges");
        group.Warning = "Enable Randomize Damage for these ranges to take effect.";

        var weapons = WeaponDefinitions.PlayerWeapons
            .Where(wp => !wp.WeaponId.ToString().Contains("blaster", StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(gun => gun.Name ?? gun.WeaponId.ToString());
        foreach (var definition in weapons) {
            var sanitizedId = definition.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");
            var name = ItemDefinitions.FromId(definition.WeaponId.ToString())!.Name;
            group.Items.Add(new GroupItem(){
                Id = $"weapon-damage-min-{sanitizedId}",
                Label = $"{name}: Min. Damage Multiplier",
                Type = "range",
                Min = 0,
                Max = 2,
                Step = 0.1,
                Default = 0.8
            });

            group.Items.Add(new GroupItem(){
                Id = $"weapon-damage-max-{sanitizedId}",
                Label = $"{name}: Max. Damage Multiplier",
                Type = "range",
                Min = 0,
                Max = 2,
                Step = 0.1,
                Default = 1.2
            });
        }

        group = page.CreateGroup("Ammo Capacity");
        group.Warning = "Ammo-capacity changes only take effect in a new game.";

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-ammo-capacity",
            Label = "Randomize Ammo Capacity",
            Description = "Randomize gun capacities using the per-weapon multiplier ranges below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-ammo-capacity-prevent-zero",
            Label = "Prevent Zero Capacity",
            Description = "Clamp randomized gun capacities to at least one round.",
            Type = "switch",
            Default = true
        });

        var guns = WeaponDefinitions.Guns
            .Where(gun => gun.UserType == Enums.app.CharacterDefine.Type.Player)
            .OrderBy(gun => gun.Name ?? gun.WeaponId.ToString());
        foreach (var definition in guns) {
            var sanitizedId = definition.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");
            var name = ItemDefinitions.FromId(definition.WeaponId.ToString())!.Name;
            group.Items.Add(new GroupItem(){
                Id = $"weapon-ammo-capacity-min-{sanitizedId}",
                Label = $"{name}: Min. Capacity Multiplier",
                Type = "range",
                Min = 0,
                Max = 2,
                Step = 0.1,
                Default = 0.8
            });

            group.Items.Add(new GroupItem(){
                Id = $"weapon-ammo-capacity-max-{sanitizedId}",
                Label = $"{name}: Max. Capacity Multiplier",
                Type = "range",
                Min = 0,
                Max = 2,
                Step = 0.1,
                Default = 1.2
            });
        }

        group = page.CreateGroup("Reload Speed");
        group.Warning = "This feature requires RE Framework.";
        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-reload-speed",
            Label = "Randomize Reload Speed",
            Description = "Randomize gun reload speeds using the per-weapon ranges below.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "weapon-mod-reload-speed-include-stabilizers",
            Label = "Include Stabilizers",
            Description = "Also apply randomized reload speeds after stabilizer upgrades.",
            Type = "switch",
            Default = true
        });

        foreach (var definition in guns) {
            var sanitizedId = definition.WeaponId.ToString().ToLowerInvariant().Replace("_", "-");
            var name = ItemDefinitions.FromId(definition.WeaponId.ToString())!.Name;
            group.Items.Add(new GroupItem(){
                Id = $"weapon-reload-speed-min-{sanitizedId}",
                Label = $"{name}: Min. Reload Speed Multiplier",
                Type = "range",
                Min = 0.1,
                Max = 2,
                Step = 0.1,
                Default = 0.3
            });

            group.Items.Add(new GroupItem(){
                Id = $"weapon-reload-speed-max-{sanitizedId}",
                Label = $"{name}: Max. Reload Speed Multiplier",
                Type = "range",
                Min = 0.1,
                Max = 2,
                Step = 0.1,
                Default = 1.8
            });
        }

        foreach (var stat in WeaponModifier.GunStatRandomizations) {
            group = page.CreateGroup(stat.GroupLabel);
            group.Items.Add(new GroupItem(){
                Id = stat.ToggleConfigId,
                Label = stat.ToggleLabel,
                Description = stat.ToggleDescription,
                Type = "switch",
                Default = false
            });

            foreach (var definition in guns) {
                var name = ItemDefinitions.FromId(definition.WeaponId.ToString())!.Name;
                group.Items.Add(new GroupItem(){
                    Id = stat.GetMinConfigId(definition.WeaponId),
                    Label = $"{name}: Min. {stat.SliderLabel}",
                    Type = "range",
                    Min = stat.Min,
                    Max = stat.Max,
                    Step = stat.Step,
                    Default = stat.DefaultMin
                });

                group.Items.Add(new GroupItem(){
                    Id = stat.GetMaxConfigId(definition.WeaponId),
                    Label = $"{name}: Max. {stat.SliderLabel}",
                    Type = "range",
                    Min = stat.Min,
                    Max = stat.Max,
                    Step = stat.Step,
                    Default = stat.DefaultMax
                });
            }
        }

        #endregion

        #region Events

        page = configDefinition.CreatePage("Runtime Events");
        group = page.CreateGroup("Timing");
        group.Warning = "Runtime events are experimental and require RE Framework.";

        group.Items.Add(new GroupItem(){
            Id = "random-events",
            Label = "Enable Runtime Events",
            Description = "Trigger temporary gameplay effects at randomized intervals.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "random-events-interval-min",
            Label = "Min. Event Interval (Seconds)",
            Description = "The minimum number of seconds between random events.",
            Type = "range",
            Min = 15,
            Max = 600,
            Step = 5,
            Default = 90
        });

        group.Items.Add(new GroupItem(){
            Id = "random-events-interval-max",
            Label = "Max. Event Interval (Seconds)",
            Description = "The maximum number of seconds between random events.",
            Type = "range",
            Min = 15,
            Max = 600,
            Step = 5,
            Default = 210
        });

        group = page.CreateGroup("Player Events");
        group.Items.Add(new GroupItem(){
            Id = "event-player-status-effects",
            Label = "Random Status Effect",
            Description = "Temporarily applies one random Jack's 55th Birthday-style passive skill effect or drawback.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-status-duration",
            Label = "Status Effect Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 30
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-blindness",
            Label = "Brief Blindness",
            Description = "Temporarily fades the screen to black through the game's blackout manager.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-blindness-duration",
            Label = "Blindness Duration (Seconds)",
            Type = "range",
            Min = 1,
            Max = 30,
            Step = 1,
            Default = 4
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-freeze",
            Label = "Movement Lock",
            Description = "Temporarily prevents player movement without disabling camera control.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-freeze-duration",
            Label = "Movement Lock Duration (Seconds)",
            Type = "range",
            Min = 1,
            Max = 30,
            Step = 1,
            Default = 5
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-scale",
            Label = "Player Scale Change",
            Description = "Temporarily makes the active player smaller or larger.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-scale-duration",
            Label = "Player Scale Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-scale-min",
            Label = "Min. Player Scale",
            Type = "range",
            Min = 0.3,
            Max = 2.5,
            Step = 0.05,
            Default = 0.65
        });

        group.Items.Add(new GroupItem(){
            Id = "event-player-scale-max",
            Label = "Max. Player Scale",
            Type = "range",
            Min = 0.3,
            Max = 2.5,
            Step = 0.05,
            Default = 1.55
        });

        group = page.CreateGroup("Weapon Events");
        group.Items.Add(new GroupItem(){
            Id = "event-weapon-infinite-ammo",
            Label = "Infinite Ammo",
            Description = "Temporarily prevents guns from consuming loaded ammo.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-weapon-infinite-ammo-duration",
            Label = "Infinite Ammo Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-weapon-neuro-ammo",
            Label = "Neuro Rounds",
            Description = "Temporarily converts fired gun bullets into neuro grenade rounds when possible.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-weapon-neuro-ammo-duration",
            Label = "Neuro Round Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 20
        });

        group.Items.Add(new GroupItem(){
            Id = "event-weapon-explosive-ammo",
            Label = "Explosive Ammo",
            Description = "Temporarily adds a small bomb detonation to gunshots.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-weapon-explosive-ammo-duration",
            Label = "Explosive Ammo Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 20
        });

        group = page.CreateGroup("Enemy Events");
        group.Items.Add(new GroupItem(){
            Id = "event-enemy-speed",
            Label = "Enemy Speed Change",
            Description = "Temporarily makes nearby enemies much faster or slower.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-speed-duration",
            Label = "Enemy Speed Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-speed-min",
            Label = "Min. Event Enemy Speed",
            Type = "range",
            Min = 0.1,
            Max = 4,
            Step = 0.05,
            Default = 0.4
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-speed-max",
            Label = "Max. Event Enemy Speed",
            Type = "range",
            Min = 0.1,
            Max = 4,
            Step = 0.05,
            Default = 2.5
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-invisible",
            Label = "Invisible Enemies",
            Description = "Temporarily hides nearby enemies while leaving them active.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-invisible-duration",
            Label = "Invisible Enemy Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 15
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-weak",
            Label = "Weak Enemies",
            Description = "Temporarily lowers max health for nearby enemies.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-weak-duration",
            Label = "Weak Enemy Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-strong",
            Label = "Strong Enemies",
            Description = "Temporarily raises max health for nearby enemies.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-strong-duration",
            Label = "Strong Enemy Duration (Seconds)",
            Type = "range",
            Min = 5,
            Max = 120,
            Step = 1,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-paused",
            Label = "Paused Enemies",
            Description = "Temporarily freezes nearby enemies in place.",
            Type = "switch",
            Default = true
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-paused-duration",
            Label = "Paused Enemy Duration (Seconds)",
            Type = "range",
            Min = 1,
            Max = 60,
            Step = 1,
            Default = 8
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-radius",
            Label = "Enemy Event Radius (Meters)",
            Description = "Only enemies within this many meters of the player are affected.",
            Type = "range",
            Min = 5,
            Max = 60,
            Step = 0.5,
            Default = 25
        });

        group.Items.Add(new GroupItem(){
            Id = "event-enemy-max-targets",
            Label = "Max. Enemy Event Targets",
            Description = "The maximum number of nearby enemies affected by a single enemy event.",
            Type = "range",
            Min = 1,
            Max = 20,
            Step = 1,
            Default = 8
        });

        #endregion Events

        #region Debug

        page = configDefinition.CreatePage("Debug");
        page.Advanced = true;
        group = page.CreateGroup("Developer Tools");
        group.Warning = "These options are intended for development and troubleshooting.";
        group.Items.Add(new GroupItem(){
            Id = "debug-download-data",
            Label = "Refresh Spreadsheet Data",
            Description = "Development only: download current spreadsheet data instead of the version shipped with this build. This can change a seed's results.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "debug-force-reframework",
            Label = "Force RE Framework Artifacts",
            Description = "Install RE Framework artifacts even when no enabled feature requires them.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "debug-download-reframework-nightly",
            Label = "Download RE Framework Nightly",
            Description = "Download and install the latest RE Framework nightly build from praydog's GitHub repository.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = "verbose-reframework-plugin-logging",
            Label = "Verbose RE Framework Logging",
            Description = "Write additional BioRand runtime diagnostics to the RE Framework log.",
            Type = "switch",
            Default = false
        });

        group.Items.Add(new GroupItem(){
            Id = $"enable-special",
            Label = "Enable Personal Touch",
            Description = "Enables a personal touch or meme for the current user.",
            Type = "switch",
            Default = true
        });
        group.Items.Add(new GroupItem(){
            Id = $"debug-unique-enemy-hp",
            Label = "Unique Enemy HP",
            Description = "Assign a distinct HP value to each enemy to help identify it in game data.",
            Type = "switch",
            Default = false
        });

        #endregion Debug

        var defaultProfileBytes = RandomizerFactory.GetDefaultProfile();
        var defaultProfileJson = Encoding.UTF8.GetString(defaultProfileBytes);
        var defaultProfile = RandomizerConfiguration.FromJson(defaultProfileJson);
        foreach (var item in configDefinition.AllItems) {
            if (defaultProfile.TryGetValue(item.Id!, out var defaultOverride)) {
                item.Default = defaultOverride;
            }
        }

        return configDefinition;
    }
}
