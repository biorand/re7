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
    public void FluffyArchive_ContainsSeedAndSharedAssetsInOneInstall(bool withRuntime, bool withAssets) {
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
        Assert.Equal(withRuntime, fluffy.GetEntry("reframework/data/BioRand7/config.json") != null);
        Assert.DoesNotContain(fluffy.Entries, entry => entry.FullName.EndsWith(".pak"));
        Assert.Equal(fluffy.Entries.Count, fluffy.Entries.Select(entry => entry.FullName)
            .Distinct(StringComparer.OrdinalIgnoreCase).Count());

        // Manual installation keeps the separate asset PAK contract.
        using var patch = new ZipArchive(new MemoryStream(output.GetOutputZip()));
        Assert.NotNull(patch.GetEntry("re_chunk_000.pak.patch_001.pak"));
        Assert.DoesNotContain(patch.Entries, entry => entry.FullName.StartsWith("natives/"));
        if (withAssets) {
            using var assetZip = new ZipArchive(new MemoryStream(output.GetAdditionalAssetsZip()));
            Assert.NotNull(assetZip.GetEntry("re_chunk_000.pak.patch_002.pak"));
        }
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

    private static RandomizerOutput CreateOutput(PakFileBuilder seed, PakFileBuilder assets, bool withRuntime)
        => new(new RandomizerInput {
            Seed = 895914,
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(),
        }, seed, assets, [], 1, withRuntime);

    private static string ReadText(ZipArchive zip, string path) {
        using var reader = new StreamReader(Assert.IsType<ZipArchiveEntry>(zip.GetEntry(path)).Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
