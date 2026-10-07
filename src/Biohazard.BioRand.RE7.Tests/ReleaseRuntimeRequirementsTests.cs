using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.IO.Compression;

namespace Biohazard.BioRand.RE7.Tests;

public class ReleaseRuntimeRequirementsTests {
    [Theory]
    [InlineData("replace-weapons")]
    [InlineData("random-starting-inventory-ethan")]
    [InlineData("random-starting-inventory-additional-gun-ethan")]
    [InlineData("additional-items")]
    [InlineData("recipes-add-new")]
    [InlineData("disable-mia-opening-damage")]
    [InlineData("random-bird-cage-magnum")]
    [InlineData("extra-enemy-amount")]
    public void IndependentWeaponAndEnemyFeatures_IncludeRuntime(string option) {
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
            config["allow-dlc-items"] = false;
            config["debug-force-reframework"] = false;
        });
        var input = new RandomizerInput { Seed = 123, Configuration = configuration };
        using var randomizer = new Randomizer(input, "", new Reporter());
        Assert.False(randomizer.IsREFrameworkRequired());
        if (option == "extra-enemy-amount") configuration[option] = 0.5;
        else configuration[option] = true;
        if (option == "replace-weapons") configuration["random-items"] = true;
        Assert.True(randomizer.IsREFrameworkRequired());
        var output = new RandomizerOutput(input, new PakFileBuilder(), new PakFileBuilder(), [], 1,
            randomizer.IsREFrameworkRequired());
        using var zip = new ZipArchive(new MemoryStream(output.GetOutputZip()));
        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7/em8000_knee_down.lua"));
        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7/em3300_explosions.lua"));
        Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7/crafting.lua"));
        Assert.NotNull(zip.GetEntry("reframework/data/BioRand7/config.json"));
    }

    [Fact]
    public void PublicDefaults_UseBundledDataAndDoNotOfferChapterShuffle() {
        Assert.False(Randomizer.DefaultConfiguration.GetValueOrDefault<bool>("debug-download-data"));
        Assert.DoesNotContain(Randomizer.ConfigurationDefinition.AllItems, item => item.Id == "shuffle-chapters");
    }

    private sealed class Reporter : IRandomizerProgress {
        public void RunTask(string text, Action callback) => callback();
    }
}
