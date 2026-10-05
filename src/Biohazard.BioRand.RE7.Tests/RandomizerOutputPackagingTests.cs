using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.IO.Compression;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

public class RandomizerOutputPackagingTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Archives_ContainSeedAndSharedAssetsInOneInstall(bool withRuntime, bool withAssets) {
        var seed = new PakFileBuilder();
        seed.AddEntry("natives/stm/leveldesign/test.scn.20", "scene"u8.ToArray());
        var assets = new PakFileBuilder();
        if (withAssets) {
            assets.AddEntry("natives/stm/props/test.mesh.220128762", "mesh"u8.ToArray());
            assets.AddEntry("natives/stm/streaming/props/test.tex.35", "texture"u8.ToArray());
        }
        var output = CreateOutput(seed, assets, withRuntime);

        using var fluffy = new ZipArchive(new MemoryStream(output.GetOutputMod()));
        Assert.Equal("scene", ReadText(fluffy, "natives/stm/leveldesign/test.scn.20"));
        if (withAssets) {
            Assert.Equal("mesh", ReadText(fluffy, "natives/stm/props/test.mesh.220128762"));
            Assert.Equal("texture", ReadText(fluffy, "natives/stm/streaming/props/test.tex.35"));
        }
        Assert.NotNull(fluffy.GetEntry("pic.jpg"));
        Assert.NotNull(fluffy.GetEntry("config.json"));
        Assert.Contains("Shared assets are included", ReadText(fluffy, "modinfo.ini"));
        Assert.Equal(withRuntime, fluffy.GetEntry("reframework/autorun/BioRand7.lua") != null);
        Assert.Equal(withRuntime, fluffy.GetEntry("reframework/autorun/BioRand7/crafting.lua") != null);
        Assert.Equal(withRuntime, fluffy.GetEntry("reframework/data/BioRand7/config.json") != null);
        Assert.DoesNotContain(fluffy.Entries, entry => entry.FullName.EndsWith(".pak"));
        Assert.Equal(fluffy.Entries.Count, fluffy.Entries.Select(entry => entry.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());

        using var patch = new ZipArchive(new MemoryStream(output.GetOutputZip()));
        using var seedPak = new PakFile(ReadBytes(patch, "re_chunk_000.pak.patch_001.pak"));
        Assert.Equal("scene"u8.ToArray(), seedPak.GetEntryData("natives/stm/leveldesign/test.scn.20"));
        Assert.DoesNotContain(patch.Entries, entry => entry.FullName.StartsWith("natives/"));
        Assert.Equal(withRuntime, patch.GetEntry("reframework/autorun/BioRand7.lua") != null);
        Assert.Equal(withRuntime, patch.GetEntry("reframework/autorun/BioRand7/crafting.lua") != null);
        Assert.Equal(withRuntime, patch.GetEntry("reframework/data/BioRand7/config.json") != null);
        Assert.Equal(withAssets ? 2 : 1, patch.Entries.Count(entry => entry.FullName.EndsWith(".pak")));
        if (withAssets) {
            using var assetPak = new PakFile(ReadBytes(patch, "re_chunk_000.pak.patch_002.pak"));
            Assert.Equal("mesh"u8.ToArray(), assetPak.GetEntryData("natives/stm/props/test.mesh.220128762"));
            Assert.Equal("texture"u8.ToArray(), assetPak.GetEntryData("natives/stm/streaming/props/test.tex.35"));
        }
        Assert.DoesNotContain(patch.Entries, entry => entry.FullName.EndsWith(".zip"));
        Assert.Equal(patch.Entries.Count, patch.Entries.Select(entry => entry.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void FluffyArchive_SeedOverrideWinsOverSharedAssetWithoutDuplicatePaths() {
        var seed = new PakFileBuilder();
        seed.AddEntry("natives/stm/props/shared.mdf2.21", "seed override"u8.ToArray());
        var assets = new PakFileBuilder();
        assets.AddEntry("natives/STM/Props/Shared.mdf2.21", "shared baseline"u8.ToArray());
        using var fluffy = new ZipArchive(new MemoryStream(CreateOutput(seed, assets, false).GetOutputMod()));

        var entry = Assert.Single(fluffy.Entries, entry => entry.FullName.Equals(
            "natives/stm/props/shared.mdf2.21", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("seed override", ReadText(fluffy, entry.FullName));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Archives_RoundTripBinaryAssetsAndEmptyFiles(bool fluffy) {
        const string binaryPath = "natives/stm/props/binary.tex.35";
        const string emptyPath = "natives/stm/props/empty.bin";
        // Cross compression-buffer boundaries and include every byte value.
        var binary = Enumerable.Range(0, 256 * 1024).Select(i => (byte)(i * 31 + i / 257)).ToArray();
        var seed = new PakFileBuilder();
        seed.AddEntry(emptyPath, []);
        var assets = new PakFileBuilder();
        assets.AddEntry(binaryPath, binary);
        var output = CreateOutput(seed, assets, false);

        using var zip = new ZipArchive(new MemoryStream(fluffy ? output.GetOutputMod() : output.GetOutputZip()));
        if (fluffy) {
            Assert.Equal(binary, ReadBytes(zip, binaryPath));
            Assert.Empty(ReadBytes(zip, emptyPath));
        } else {
            using var seedPak = new PakFile(ReadBytes(zip, "re_chunk_000.pak.patch_001.pak"));
            using var assetPak = new PakFile(ReadBytes(zip, "re_chunk_000.pak.patch_002.pak"));
            Assert.Equal(binary, assetPak.GetEntryData(binaryPath));
            var empty = seedPak.GetEntryData(emptyPath);
            Assert.NotNull(empty);
            Assert.Empty(empty);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RuntimeRecipeMode_IsDerivedAndDoesNotChangeInputProfile(bool active) {
        var config = RandomizerTest.CreateFeatureTestConfiguration();
        config["recipes-add-new"] = false;
        config["debug-recipes-enabled"] = !active;
        var output = new RandomizerOutput(new RandomizerInput { Configuration = config },
            new PakFileBuilder(), new PakFileBuilder(), [], 1, true, active);
        using var zip = new ZipArchive(new MemoryStream(output.GetOutputZip()));
        using var runtime = System.Text.Json.JsonDocument.Parse(ReadText(zip, "reframework/data/BioRand7/config.json"));
        using var input = System.Text.Json.JsonDocument.Parse(ReadText(zip, "config.json"));
        Assert.Equal(active, runtime.RootElement.GetProperty("debug-recipes-enabled").GetBoolean());
        Assert.False(runtime.RootElement.GetProperty("recipes-add-new").GetBoolean());
        Assert.Equal(!active, input.RootElement.GetProperty("debug-recipes-enabled").GetBoolean());
    }

    private static RandomizerOutput CreateOutput(PakFileBuilder seed, PakFileBuilder assets, bool withRuntime)
        => new(new RandomizerInput {
            Seed = 895914,
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(),
        }, seed, assets, [], 1, withRuntime);

    private static string ReadText(ZipArchive zip, string path) {
        using var reader = new StreamReader(Assert.IsType<ZipArchiveEntry>(zip.GetEntry(path)).Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static byte[] ReadBytes(ZipArchive zip, string path) {
        using var stream = Assert.IsType<ZipArchiveEntry>(zip.GetEntry(path)).Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }
}
