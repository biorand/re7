using Biohazard.BioRand.RE7.Commands;
using IntelOrca.Biohazard.REE.Package;
using Spectre.Console.Cli;

namespace Biohazard.BioRand.RE7.Tests;

public class SetupCommandTests {
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
