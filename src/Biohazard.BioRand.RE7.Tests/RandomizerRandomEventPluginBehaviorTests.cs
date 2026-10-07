using Biohazard.BioRand.RE7.Extensions;
using System.IO.Compression;
using System.Text.Json.Nodes;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerRandomEventPluginBehaviorTests {
    private static readonly string[] REFrameworkScriptPaths = [
        "BioRand7.lua",
        "BioRand7/config.lua",
        "BioRand7/context.lua",
        "BioRand7/data.lua",
        "BioRand7/em3300_explosions.lua",
        "BioRand7/em8000_knee_down.lua",
        "BioRand7/enemy_drops.lua",
        "BioRand7/game.lua",
        "BioRand7/inventory.lua",
        "BioRand7/inventory_pause.lua",
        "BioRand7/logger.lua",
        "BioRand7/madhouse_saves.lua",
        "BioRand7/mia_opening_damage.lua",
        "BioRand7/object_cache.lua",
        "BioRand7/random_events.lua",
        "BioRand7/reload_speed.lua",
        "BioRand7/rng.lua",
        "BioRand7/static_mia.lua",
        "BioRand7/ui.lua",
    ];

    [Fact]
    public void RandomEvents_EnableREFrameworkConfigAndInjectPluginSeedMetadata() {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["allow-dlc-items"] = false;
            config["random-events"] = true;
            config["random-events-interval-min"] = 10;
            config["random-events-interval-max"] = 20;
            config["event-player-blindness-duration"] = 3;
            config["event-enemy-max-targets"] = 4;
        });

        var output = RandomizerTest.RunOutput(configuration.ToJson(), seed: 0xE7E775);
        using var zip = output.Assets.Single(asset => asset.Key == "1-patch").Data.Unzip();
        using var fluffyMod = output.Assets.Single(asset => asset.Key == "2-fluffy").Data.Unzip();

        var rootConfigEntry = zip.GetEntry("config.json");
        var reframeworkConfigEntry = zip.GetEntry("reframework/data/BioRand7/config.json");

        Assert.NotNull(rootConfigEntry);
        AssertREFrameworkScripts(zip);
        AssertREFrameworkScripts(fluffyMod);
        Assert.NotNull(reframeworkConfigEntry);

        var rootConfig = JsonNode.Parse(rootConfigEntry!.GetBytes())!.AsObject();
        var reframeworkConfig = JsonNode.Parse(reframeworkConfigEntry!.GetBytes())!.AsObject();

        Assert.Null(rootConfig["biorand-seed"]);
        Assert.Equal(0xE7E775, reframeworkConfig["biorand-seed"]!.GetValue<int>());
        Assert.True(reframeworkConfig["random-events"]!.GetValue<bool>());
        Assert.Equal(10, reframeworkConfig["random-events-interval-min"]!.GetValue<int>());
        Assert.Equal(20, reframeworkConfig["random-events-interval-max"]!.GetValue<int>());
        Assert.Equal(3, reframeworkConfig["event-player-blindness-duration"]!.GetValue<int>());
        Assert.Equal(4, reframeworkConfig["event-enemy-max-targets"]!.GetValue<int>());
    }

    [Fact]
    public void RandomEvents_InfiniteAmmo_UsesPassiveSkillInfinityFlag() {
        var source = ReadScriptSource("random_events.lua");

        Assert.Contains("weapon_infinite_ammo", source);
        Assert.Contains("apply_passive(INFINITE_AMMO_DELTA)", source);
        Assert.Contains("set_loadNum(System.Int32)", source);
        Assert.Contains("sdk.PreHookResult.SKIP_ORIGINAL", source);
        Assert.Contains("INFINITE_AMMO_DELTA", source);
        Assert.Contains("BulletStackNumInfinityCount", source);
        Assert.Contains("app.PlayerOrder", source);
        Assert.Contains("app.PlayerStatus", source);
    }

    [Fact]
    public void RandomEvents_DebugUi_CanStartEverySupportedEffect() {
        var pluginSource = ReadScriptSource("../BioRand7.lua");
        var randomEventsSource = ReadScriptSource("random_events.lua");
        var uiSource = ReadScriptSource("ui.lua");

        Assert.Contains("started_from_ui", randomEventsSource);
        Assert.Contains("re.on_application_entry(\"UpdateBehavior\"", pluginSource);
        Assert.Contains("events:start(\"player_status\", true)", uiSource);
        Assert.Contains("events:start(\"player_status\", true, delta)", uiSource);
        Assert.Contains("events.status_deltas", uiSource);
        Assert.Contains("draw_overlay", uiSource);
        Assert.Contains("overlay_label", randomEventsSource);
        Assert.Contains("set_next_window_pos", uiSource);

        foreach (var kind in new[]{
                     "player_blindness",
                     "player_freeze",
                     "player_scale",
                     "weapon_infinite_ammo",
                     "weapon_neuro_ammo",
                     "weapon_explosive_ammo",
                     "enemy_speed",
                     "enemy_invisible",
                     "enemy_weak",
                     "enemy_strong",
                     "enemy_paused",
                 }) {
            Assert.Contains($"\"{kind}\"", randomEventsSource);
        }
    }

    [Fact]
    public void InventoryCombineHook_UsesStaticRowNumField() {
        var gameSource = ReadScriptSource("game.lua");
        var inventorySource = ReadScriptSource("inventory.lua");

        Assert.Contains("sdk.set_native_field(nil", gameSource);
        Assert.Contains("game:static_field(type_name, \"RowNum\")", inventorySource);
        Assert.Contains("game:set_static_field(type_name, \"RowNum\", MAX_COMBINE_ROWS)", inventorySource);
        Assert.DoesNotContain("get_RowNum", inventorySource);
        Assert.DoesNotContain("set_RowNum", inventorySource);
    }

    private static void AssertREFrameworkScripts(ZipArchive zip) {
        foreach (var scriptPath in REFrameworkScriptPaths) {
            var path = $"reframework/autorun/{scriptPath}";
            var entry = zip.GetEntry(path);
            Assert.NotNull(entry);
            Assert.Equal(Serialization.EmbeddedData.GetFile(path), entry!.GetBytes());
        }
        Assert.NotNull(zip.GetEntry("reframework/data/BioRand7/config.json"));
        Assert.Null(zip.GetEntry("reframework/plugins/managed/Biohazard.BioRand.RE7.REFrameworkPlugins.dll"));
    }

    private static string ReadScriptSource(string fileName) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "biorand-re7.sln"))) {
            directory = directory.Parent;
        }

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate repository root.");

        var path = Path.Combine(
            directory.FullName,
            "src",
            "Biohazard.BioRand.RE7",
            "_Data",
            "reframework",
            "autorun",
            "BioRand7",
            fileName);
        return File.ReadAllText(path);
    }
}
