using Biohazard.BioRand.RE7.Commands;
using Biohazard.BioRand.RE7.Weapons;
using IntelOrca.Biohazard.REE.Package;
using Spectre.Console.Cli;

namespace Biohazard.BioRand.RE7.Tests;

public class SetupCommandTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Setup_DlcWeaponsRequiresAndHarvestsEntireManifest(bool missingAsset) {
        var root = Path.Combine(Path.GetTempPath(), $"biorand-setup-dlc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try {
            var builder = new PakFileBuilder();
            var bytes = new byte[] { 5, 6, 7, 8 };
            var paths = DlcCampaignWeapons.RequiredAssetPaths;
            foreach (var path in missingAsset ? paths.Skip(1) : paths)
                builder.AddEntry(path, bytes);
            builder.Save(Path.Combine(root, "re_chunk_000.pak"), CompressionKind.Zstd);
            var destination = Path.Combine(root, "output", "baseline.pak");
            var app = new CommandApp<SetupCommand>();
            var result = app.Run(["-i", root, "-o", destination, "--dlc-weapons"]);
            if (missingAsset) {
                Assert.NotEqual(0, result);
                Assert.False(File.Exists(destination));
            } else {
                Assert.Equal(0, result);
                using var pak = new PakFile(destination);
                Assert.All(paths, path => Assert.Equal(bytes, pak.GetEntryData(path)));
            }
        } finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Setup_ExtractsKnownBaselineFilesWithAndWithoutFull(bool full) {
        var root = Path.Combine(Path.GetTempPath(), $"biorand-setup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try {
            var builder = new PakFileBuilder();
            var bytes = new byte[] { 1, 2, 3, 4 };
            builder.AddEntry(RandomizerTestPaths.EthanInventoryPath, bytes);
            builder.Save(Path.Combine(root, "re_chunk_000.pak"), CompressionKind.Zstd);
            var destination = Path.Combine(root, "output", "baseline.pak");
            var arguments = new List<string> { "-i", root, "-o", destination };
            if (full) arguments.Add("--full");
            var app = new CommandApp<SetupCommand>();
            Assert.Equal(0, app.Run(arguments));
            using var pak = new PakFile(destination);
            Assert.Equal(bytes, pak.GetEntryData(RandomizerTestPaths.EthanInventoryPath));
        } finally { Directory.Delete(root, true); }
    }
}
