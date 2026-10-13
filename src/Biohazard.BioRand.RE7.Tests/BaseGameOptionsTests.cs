using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.IO.Compression;
using System.Text.Json;

namespace Biohazard.BioRand.RE7.Tests;

public class BaseGameOptionsTests {
    [Fact]
    public void DefaultAndLegacyProfiles_TargetTheBaseGame() {
        Assert.True(RandomizerExecutor.DefaultConfiguration.GetValueOrDefault<bool>(DlcContent.BaseGameOnlyConfigKey));
        using var legacy = new Randomizer(new RandomizerInput {
            Configuration = RandomizerConfiguration.FromJson("{\"allow-dlc-items\":true,\"dlc-campaign-weapons\":true}"),
        }, "unused", new EmptyReporter());
        Assert.False(DlcContent.IsAllowed(legacy));
        Assert.False(DlcContent.IsItemAllowed(legacy, "skl002"));
        Assert.False(DlcContent.IsItemAllowed(legacy, "GoodLuckCoinA_Buy"));
        Assert.True(DlcContent.IsItemAllowed(legacy, "Herb"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BothArchives_DescribeCompatibilityAndPersistItForRuntime(bool baseGameOnly) {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => config[DlcContent.BaseGameOnlyConfigKey] = baseGameOnly);
        var output = new RandomizerOutput(new RandomizerInput { Seed = 123, Configuration = configuration },
            new PakFileBuilder(), new PakFileBuilder(), [], 1, true);
        foreach (var bytes in new[] { output.GetOutputZip(), output.GetOutputMod() }) {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            using var reader = new StreamReader(zip.GetEntry("requirements.txt")!.Open());
            Assert.Contains(baseGameOnly ? "no DLC required" : "all DLC installed", reader.ReadToEnd());
            using var json = JsonDocument.Parse(zip.GetEntry("reframework/data/BioRand7/config.json")!.Open());
            Assert.Equal(baseGameOnly, json.RootElement.GetProperty(DlcContent.BaseGameOnlyConfigKey).GetBoolean());
        }
    }

    [Fact]
    public void OverriddenDlcOptions_DoNotRequireRuntimeOnTheirOwn() {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["allow-dlc-items"] = true;
            config["random-starting-inventory-skills-ethan"] = true;
            config["random-starting-inventory-skills-mia"] = true;
        });
        using var randomizer = new Randomizer(new RandomizerInput { Configuration = configuration }, "unused", new EmptyReporter());
        Assert.False(randomizer.IsREFrameworkRequired());
    }
}
