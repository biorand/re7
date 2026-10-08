using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.IO.Compression;

namespace Biohazard.BioRand.RE7.Tests;

public class SerializationSafetyTests {
    [Fact]
    public void PatchArchive_SharedAssetsCannotOverrideSeedFiles() {
        const string path = "natives/stm/props/shared.mdf2.21";
        var seed = new PakFileBuilder();
        seed.AddEntry(path, "seed override"u8.ToArray());
        var assets = new PakFileBuilder();
        assets.AddEntry("natives/STM/Props/Shared.mdf2.21", "shared baseline"u8.ToArray());
        assets.AddEntry("natives/stm/props/shared.mesh.220128762", "mesh"u8.ToArray());
        var output = new RandomizerOutput(new RandomizerInput {
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(),
        }, seed, assets, [], 1, false);

        using var zip = new ZipArchive(new MemoryStream(output.GetOutputZip()));
        using var seedPak = new PakFile(ReadEntry(zip, "re_chunk_000.pak.patch_001.pak"));
        using var assetPak = new PakFile(ReadEntry(zip, "re_chunk_000.pak.patch_002.pak"));
        using var installed = new PakFileCollection([seedPak, assetPak]);
        Assert.Equal("seed override"u8.ToArray(), installed.GetEntryData(path));
        Assert.Equal("mesh"u8.ToArray(), installed.GetEntryData("natives/stm/props/shared.mesh.220128762"));
        Assert.Equal(2, assets.Entries.Count);
    }

    private static byte[] ReadEntry(ZipArchive zip, string path) {
        using var input = Assert.IsType<ZipArchiveEntry>(zip.GetEntry(path)).Open();
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
