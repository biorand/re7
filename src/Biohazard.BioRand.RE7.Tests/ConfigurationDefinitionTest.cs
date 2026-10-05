using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition;

namespace Biohazard.BioRand.RE7.Tests;

public class ConfigurationDefinitionTest {
    private readonly IEnumerable<GroupItem> items = RandomizerExecutor.ConfigurationDefinition.AllItems;

    [Fact]
    public void Test_Id_Uniqueness() {
        var duplicates = items
            .Where(i => i.Id != null)
            .GroupBy(i => i.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.True(duplicates.Count == 0,
            $"Duplicate IDs found: {string.Join(", ", duplicates)}");
    }

    [Fact]
    public void Test_Id_Naming_Conventions() {
        foreach (var item in items.Where(item => item.Id != null)) {
            Assert.Matches("^[a-zA-Z0-9\\-]+$", item.Id!);

            var id = item.Id!;
            if (id.EndsWith("-min") || id.EndsWith("-max")) {
                Assert.Matches("range|percent", item.Type);
            }
            // else if(id...
        }
    }

    [Fact]
    public void Test_Numeric_Sanity() {
        foreach (var item in items) {
            if (item.Min != null) {
                Assert.NotNull(item.Max);
                Assert.True(item.Min <= item.Max!);
            }

            if (item.Step != null) {
                Assert.NotNull(item.Min);
                Assert.NotNull(item.Max);
                Assert.InRange(item.Step.Value, 0.01, item.Max!.Value);
            }
        }
    }

    [Fact]
    public void Test_Min_Max_Correspondence() {
        foreach (var item in items.Where(item => item.Id != null)) {
            if (item.Id!.EndsWith("-min")) {
                var correspondingMaxValue = item.Id.ReplaceLastOccurrence("-min", "-max");
                Assert.Contains(items, item => item.Id == correspondingMaxValue);
            }
        }
    }

    [Fact]
    public void Test_Max_Min_Correspondence() {
        foreach (var item in items.Where(item => item.Id != null)) {
            if (item.Id!.EndsWith("-max")) {
                var correspondingMinValue = item.Id.ReplaceLastOccurrence("-max", "-min");
                Assert.Contains(items, item => item.Id == correspondingMinValue);
            }
        }
    }

    [Fact]
    public void Test_Range_Items_Validity() {
        foreach (var item in items.Where(i => i.Type == "range")) {
            Assert.NotNull(item.Min);
            Assert.NotNull(item.Max);
            Assert.NotNull(item.Default);

            var defaultValue = Convert.ToDouble(item.Default);
            Assert.InRange(defaultValue, item.Min.Value, item.Max.Value);
        }
    }

    [Fact]
    public void Test_Dropdown_Items_Validity() {
        foreach (var item in items.Where(i => i.Type == "dropdown")) {
            Assert.NotNull(item.Options);
            Assert.NotEmpty(item.Options);
            Assert.Contains(item.Default, item.Options!);
        }
    }

    [Fact]
    public void Test_Switch_Items_Validity() {
        foreach (var item in items.Where(i => i.Type == "switch")) {
            Assert.IsType<bool>(item.Default);
        }
    }

    [Fact]
    public void Test_Stack_Limit_Items_Include_NonWeapon_Inventory_Items() {
        var ids = items
            .Where(item => item.Id != null)
            .Select(item => item.Id!)
            .ToHashSet(StringComparer.Ordinal);
        var itemDefinitions = ItemDefinitionRepository.Default;

        Assert.Contains(itemDefinitions.FromId("ChemicalS")!.StackLimitConfigId, ids);
        Assert.Contains(itemDefinitions.FromId("ChemicalM")!.StackLimitConfigId, ids);
        Assert.Contains(itemDefinitions.FromId("ChemicalL")!.StackLimitConfigId, ids);
        Assert.Contains(itemDefinitions.FromId("Stimulant")!.StackLimitConfigId, ids);
        Assert.Contains(itemDefinitions.FromId("Depressant")!.StackLimitConfigId, ids);
        Assert.Contains(itemDefinitions.FromId("LiquidBomb")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("Handgun_G17")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("BackDoorKey")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("PendulumClock")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("SpareKey")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("FuseCh4")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("EvelynRadar")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("EvelynRadar2")!.StackLimitConfigId, ids);
        Assert.DoesNotContain(itemDefinitions.FromId("EvelynRadar3")!.StackLimitConfigId, ids);
    }

    [Fact]
    public void Test_Stack_Limit_Items_Have_Readable_Unique_Labels() {
        var stackLimitItems = items
            .Where(item => item.Id?.StartsWith("inventory-stack-limit-", StringComparison.Ordinal) == true)
            .ToArray();
        var duplicateLabels = stackLimitItems
            .GroupBy(item => item.Label)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.All(stackLimitItems, item => Assert.False(string.IsNullOrWhiteSpace(item.Label), item.Id));
        Assert.True(duplicateLabels.Count == 0,
            $"Duplicate stack limit labels found: {string.Join(", ", duplicateLabels)}");
    }

    [Fact]
    public void Test_Type_Existence() {
        foreach (var item in items) {
            Assert.False(string.IsNullOrWhiteSpace(item.Type),
                $"Item '{item.Id}' has no type.");
        }
    }

    [Fact]
    public void Test_Pages_And_Groups_Have_Readable_Unique_Labels() {
        var pages = RandomizerExecutor.ConfigurationDefinition.Pages;
        var duplicatePageLabels = pages
            .GroupBy(page => page.Label)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.All(pages, page => Assert.False(string.IsNullOrWhiteSpace(page.Label)));
        Assert.Empty(duplicatePageLabels);

        foreach (var page in pages) {
            var duplicateGroupLabels = page.Groups
                .GroupBy(group => group.Label)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.All(page.Groups, group =>
                Assert.False(string.IsNullOrWhiteSpace(group.Label), $"Page '{page.Label}' has an unnamed group."));
            Assert.True(duplicateGroupLabels.Length == 0,
                $"Page '{page.Label}' has duplicate groups: {string.Join(", ", duplicateGroupLabels)}");
        }
    }

    [Fact]
    public void Test_Nonfunctional_Configuration_Items_Are_Not_Exposed() {
        var ids = items.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("enemy-health-progressive-difficulty", ids);

        foreach (var id in new[]{ "random-starting-inventory-size-ethan", "random-starting-inventory-size-mia" }) {
            var item = Assert.Single(items, item => item.Id == id);
            Assert.Equal(new[]{ "12", "16", "20" }, item.Options);
        }
    }

    [Fact]
    public void Test_Recipe_Mode_Description_Documents_Every_Choice() {
        var item = Assert.Single(items, item => item.Id == "recipes-randomization-mode");

        Assert.All(item.Options!, option => Assert.Contains($"{option}:", item.Description));
    }

    [Theory]
    [InlineData("Ethan")]
    [InlineData("Mia")]
    public void Test_Additional_Starting_Gun_Is_Opt_In(string character) {
        var page = Assert.Single(RandomizerExecutor.ConfigurationDefinition.Pages, page => page.Label == "Inventory");
        var group = Assert.Single(page.Groups, group => group.Label == "Starting Weapons");
        var item = Assert.Single(group.Items,
            item => item.Id == $"random-starting-inventory-additional-gun-{character.ToLowerInvariant()}");

        Assert.Equal($"{character}: Additional Random Gun", item.Label);
        Assert.Equal("switch", item.Type);
        Assert.False(Assert.IsType<bool>(item.Default));
    }

    [Fact]
    public void Test_General_Configuration_Items_Exist() {
        var definition = RandomizerExecutor.ConfigurationDefinition;
        var page = Assert.Single(definition.Pages, page => page.Label == "General");
        var ids = page.Groups.SelectMany(group => group.Items).Select(item => item.Id).ToHashSet();
        var startChapter = Assert.Single(page.Groups.SelectMany(group => group.Items),
            item => item.Id == ChapterJumpDataModifier.StartChapterConfigKey);

        Assert.Contains(ChapterJumpDataModifier.StartChapterConfigKey, ids);
        Assert.DoesNotContain("skip-guest-house", ids);
        Assert.Equal("dropdown", startChapter.Type);
        Assert.Equal(
            ChapterJumpDataModifier.StartChapterOptions.Select(option => option.Name),
            startChapter.Options);
        Assert.Equal(ChapterJumpDataModifier.NormalStartChapter, startChapter.Default);
    }

    [Fact]
    public void Test_EnemyDrop_Configuration_Items_Exist() {
        var ids = items
            .Where(item => item.Id != null)
            .Select(item => item.Id!)
            .ToHashSet();

        Assert.Contains("random-enemy-drops", ids);
        Assert.Contains("enemy-drop-probability", ids);
        Assert.Contains("enemy-drop-probability-flyingbug", ids);
        Assert.Contains("enemy-drop-probability-molded", ids);
        Assert.Contains("enemy-drop-probability-jackmutated", ids);
        Assert.DoesNotContain("enemy-drop-probability-moldedblade", ids);
        Assert.Contains("enemy-drop-respect-difficulty", ids);
        Assert.Contains("enemy-drop-ammo-only-available-weapons", ids);
        Assert.Contains("enemy-drop-ammo-min", ids);
        Assert.Contains("enemy-drop-ammo-max", ids);
        Assert.Contains("enemy-drop-ratio-herb", ids);
        Assert.Contains("enemy-drop-ratio-handgunbullet", ids);
        Assert.Contains("enemy-drop-ratio-liquidbomb", ids);
        Assert.DoesNotContain("enemy-drop-ratio-stimulant", ids);
        Assert.DoesNotContain("enemy-drop-ratio-depressant", ids);
        Assert.Contains("item-drop-ratio-stimulant", ids);
        Assert.Contains("item-drop-ratio-depressant", ids);
        Assert.Contains("enemy-drop-valuable-repair-kit", ids);
        Assert.Contains("enemy-drop-valuable-lock-pick", ids);
        Assert.Contains("enemy-drop-valuable-birthday-skill", ids);
    }

    [Theory]
    [InlineData("item-drop-ratio-flower")]
    [InlineData("enemy-drop-ratio-flower")]
    public void Test_Supplements_Are_Configurable_Materials_With_Enabled_Defaults(string configId) {
        var item = Assert.Single(items, item => item.Id == configId);

        Assert.Equal("Supplements", item.Label);
        Assert.Equal(ItemDrops.CategoryMaterial, item.Category!.Label);
        Assert.Equal("range", item.Type);
        Assert.Equal(0.05, Convert.ToDouble(item.Default));

        using var profile = System.Text.Json.JsonDocument.Parse(RandomizerFactory.GetDefaultProfile());
        Assert.Equal(0.05, profile.RootElement.GetProperty(configId).GetDouble());
    }

    [Fact]
    public void Test_RandomEvent_Configuration_Items_Exist() {
        var ids = items
            .Where(item => item.Id != null)
            .Select(item => item.Id!)
            .ToHashSet();

        Assert.Contains("random-events", ids);
        Assert.Contains("random-events-interval-min", ids);
        Assert.Contains("random-events-interval-max", ids);
        Assert.Contains("event-player-status-effects", ids);
        Assert.Contains("event-player-status-duration", ids);
        Assert.Contains("event-player-blindness", ids);
        Assert.Contains("event-player-freeze", ids);
        Assert.Contains("event-player-scale-min", ids);
        Assert.Contains("event-player-scale-max", ids);
        Assert.Contains("event-weapon-infinite-ammo", ids);
        Assert.Contains("event-weapon-neuro-ammo", ids);
        Assert.Contains("event-weapon-explosive-ammo", ids);
        Assert.Contains("event-enemy-speed", ids);
        Assert.Contains("event-enemy-invisible", ids);
        Assert.Contains("event-enemy-weak", ids);
        Assert.Contains("event-enemy-strong", ids);
        Assert.Contains("event-enemy-paused", ids);
        Assert.Contains("event-enemy-radius", ids);
        Assert.Contains("event-enemy-max-targets", ids);
    }

    [Fact]
    public void Test_Player_Configuration_Section_Exists() {
        var definition = RandomizerExecutor.ConfigurationDefinition;
        var page = Assert.Single(definition.Pages, page => page.Label == "Player");
        var group = Assert.Single(page.Groups, group => group.Label == "Health");
        var groupIds = group.Items.Select(item => item.Id).ToHashSet();
        var pageIds = page.Groups.SelectMany(group => group.Items).Select(item => item.Id).ToHashSet();

        Assert.Contains("player-random-max-health", groupIds);
        foreach (var healthLevel in PlayerModifier.MaxHealthLevels) {
            Assert.Contains(healthLevel.FromConfigId, pageIds);
            Assert.Contains(healthLevel.ToConfigId, pageIds);
        }

        group = Assert.Single(page.Groups, group => group.Label == "Base Reload Rate");
        groupIds = group.Items.Select(item => item.Id).ToHashSet();

        Assert.Contains("player-random-reload-speed", groupIds);
        foreach (var reloadSpeedLevel in PlayerModifier.ReloadSpeedLevels) {
            Assert.Contains(reloadSpeedLevel.FromConfigId, pageIds);
            Assert.Contains(reloadSpeedLevel.ToConfigId, pageIds);
        }

        group = Assert.Single(page.Groups, group => group.Label == "Psychostimulants");
        groupIds = group.Items.Select(item => item.Id).ToHashSet();

        Assert.Contains("player-random-psychostimulants", groupIds);
        Assert.Contains("player-psychostimulant-duration-min", pageIds);
        Assert.Contains("player-psychostimulant-duration-max", pageIds);
        Assert.Contains("player-psychostimulant-range-min", pageIds);
        Assert.Contains("player-psychostimulant-range-max", pageIds);
    }
}
