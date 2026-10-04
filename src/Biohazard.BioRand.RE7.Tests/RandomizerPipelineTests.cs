using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.Diagnostics;
using System.IO.Compression;
using Xunit.Abstractions;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerPipelineTests(ITestOutputHelper output) {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RunState_MatchesFilesInBothInstallationArchives(bool enableFeatures) {
        const int seed = 895914;
        void Configure(RandomizerConfiguration config) {
            config["allow-dlc-items"] = enableFeatures;
            config["random-items"] = enableFeatures;
            config["random-key-item-locations"] = enableFeatures;
            config["additional-items"] = enableFeatures;
            config["random-starting-inventory-ethan"] = enableFeatures;
            config["recipes-unlock-from-start"] = enableFeatures;
        }

        var timer = Stopwatch.StartNew();
        using var state = RandomizerTest.RunState(Configure, seed);
        output.WriteLine($"File generation: {timer.Elapsed.TotalSeconds:F2}s");

        timer.Restart();
        var configuration = RandomizerTest.CreateFeatureTestConfiguration(Configure);
        // Exercise the public executor with normal state logging, independently
        // of the faster behavior-test helper.
        var packaged = RandomizerTest.RunOutput(configuration.ToJson(), seed);
        output.WriteLine($"Generation with logs and both archives: {timer.Elapsed.TotalSeconds:F2}s");

        using var patch = packaged.Assets.Single(asset => asset.Key == "1-patch").Data.Unzip();
        using var fluffy = packaged.Assets.Single(asset => asset.Key == "2-fluffy").Data.Unzip();
        Assert.NotEmpty(state.ChangedFiles);
        AssertPakMatches(patch, "re_chunk_000.pak.patch_001.pak", state.ChangedFiles);
        if (state.AdditionalAssetFiles.Count > 0) {
            AssertPakMatches(patch, "re_chunk_000.pak.patch_002.pak", state.AdditionalAssetFiles);
        } else {
            Assert.Null(patch.GetEntry("re_chunk_000.pak.patch_002.pak"));
        }

        var expectedFiles = state.AdditionalAssetFiles.SetItems(state.ChangedFiles);
        Assert.Equal(expectedFiles.Keys.Order(StringComparer.Ordinal),
            fluffy.Entries.Where(entry => entry.FullName.StartsWith("natives/", StringComparison.Ordinal))
                .Select(entry => entry.FullName).Order(StringComparer.Ordinal));
        foreach (var (path, bytes) in expectedFiles) {
            Assert.True(bytes.AsSpan().SequenceEqual(ReadEntry(fluffy, path)), $"Fluffy file differs: {path}");
        }

        // Generation must still save pending flags before the helper returns.
        if (enableFeatures) {
            Assert.True(state.WasFileModified(RandomizerTestPaths.GlobalVariablesPath));
            Assert.Contains(packaged.Assets, asset => asset.Key == "4-key-hints");
        }
        Assert.NotNull(patch.GetEntry("input.log"));
        Assert.NotNull(patch.GetEntry("process.log"));
        Assert.NotNull(patch.GetEntry("output.log"));
    }

    private static void AssertPakMatches(ZipArchive archive, string name,
        IReadOnlyDictionary<string, byte[]> expectedFiles) {
        using var pak = new PakFile(ReadEntry(archive, name));
        Assert.Equal(expectedFiles.Count, pak.FileHashes.Count());
        foreach (var (path, bytes) in expectedFiles) {
            var actual = pak.GetEntryData(path);
            Assert.NotNull(actual);
            Assert.True(bytes.AsSpan().SequenceEqual(actual), $"PAK file differs: {path}");
        }
    }

    private static byte[] ReadEntry(ZipArchive archive, string path)
        => Assert.IsType<ZipArchiveEntry>(archive.GetEntry(path)).GetBytes();
}
