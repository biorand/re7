using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.IO.Compression;
using System.Text.Json;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerInventoryPauseTests {
    private const string ConfigKey = "pause-inventory";
    private const string ScriptPath = "reframework/autorun/BioRand7/inventory_pause.lua";

    [Fact]
    public void InventoryPause_IsOptIn() {
        var definition = Assert.Single(Randomizer.ConfigurationDefinition.AllItems, item => item.Id == ConfigKey);
        Assert.Equal("switch", definition.Type);
        Assert.Equal(false, definition.Default);
        Assert.False(Randomizer.DefaultConfiguration.GetValueOrDefault<bool>(ConfigKey));
        using var profile = JsonDocument.Parse(EmbeddedData.GetFile("default-profile.json"));
        Assert.False(profile.RootElement.GetProperty(ConfigKey).GetBoolean());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InventoryPause_ControlsRuntimeAndBothArchiveFormats(bool enabled, bool forceRuntime) {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["allow-dlc-items"] = false;
            config["debug-force-reframework"] = forceRuntime;
            config[ConfigKey] = enabled;
        });
        var input = new RandomizerInput { Seed = 123, Configuration = configuration };
        using var randomizer = new Randomizer(input, "", new Reporter());
        var requiresRuntime = enabled || forceRuntime;
        Assert.Equal(requiresRuntime, randomizer.IsREFrameworkRequired());
        var output = new RandomizerOutput(input, new PakFileBuilder(), new PakFileBuilder(), [], 1,
            randomizer.IsREFrameworkRequired());

        foreach (var bytes in new[] { output.GetOutputZip(), output.GetOutputMod() }) {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var script = zip.GetEntry(ScriptPath);
            Assert.Equal(requiresRuntime, script != null);
            if (!requiresRuntime) {
                Assert.Null(zip.GetEntry("reframework/autorun/BioRand7.lua"));
                continue;
            }
            using var scriptStream = script!.Open();
            using var actual = new MemoryStream();
            scriptStream.CopyTo(actual);
            Assert.Equal(EmbeddedData.GetFile(ScriptPath), actual.ToArray());
            using var configStream = zip.GetEntry("reframework/data/BioRand7/config.json")!.Open();
            using var config = JsonDocument.Parse(configStream);
            Assert.Equal(enabled, config.RootElement.GetProperty(ConfigKey).GetBoolean());
        }
    }

    private sealed class Reporter : IRandomizerProgress {
        public void RunTask(string text, Action callback) => callback();
    }
}
