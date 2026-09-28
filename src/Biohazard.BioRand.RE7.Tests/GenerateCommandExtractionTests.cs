using Biohazard.BioRand.RE7.Commands;
using System.IO.Compression;
using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Tests;

public class GenerateCommandExtractionTests {
    [Fact]
    public void UrlResponse_UsesGenerationSeedRatherThanRecordId() {
        var input = GenerateCommand.FromResponse(new GenerateCommand.RandoResponse {
            Id = 133558, Seed = 736361, ProfileName = "Shared profile",
            Config = new() { ["random-items"] = true },
        });
        Assert.Equal(736361, input.Seed);
        Assert.Equal("Shared profile", input.ProfileName);
        Assert.True(input.Configuration.GetValueOrDefault<bool>("random-items"));
    }

    [Theory]
    [InlineData("seed.pak")]
    [InlineData("seed.zip")]
    [InlineData("loose")]
    public void Output_IncludesRuntimeAndAdditionalAssetsAtRequestedDestination(string name) {
        var root = CreateTemporaryDirectory();
        try {
            var output = new IntelOrca.Biohazard.BioRand.RandomizerOutput([
                new("1-patch", "Patch", "", "seed-original.zip", ZipBytes(("patch.pak", "pak"))),
                new("2-fluffy", "Fluffy", "", "seed-mod.zip", ZipBytes(
                    ("natives/stm/test.user.2", "native"), ("reframework/autorun/BioRand7.lua", "lua"),
                    ("reframework/data/BioRand7/config.json", "{}"), ("process.log", "log"))),
                new("3-assets", "Assets", "", "assets.zip", ZipBytes(("patch_002.pak", "assets"))),
                new("4-key-hints", "Hints", "", "hints.html", "hints"u8.ToArray()),
            ], "");
            var target = Path.Combine(root, "destination", name);
            GenerateCommand.WriteOutput(output, target);
            var directory = name == "loose" ? target : Path.GetDirectoryName(target)!;
            Assert.Equal("hints", File.ReadAllText(Path.Combine(directory, "hints.html")));
            if (name == "seed.zip") {
                using var zip = ZipFile.OpenRead(target);
                Assert.NotNull(zip.GetEntry("reframework/autorun/BioRand7.lua"));
                Assert.True(File.Exists(Path.Combine(directory, "assets.zip")));
            } else {
                Assert.Equal("lua", File.ReadAllText(Path.Combine(directory, "reframework/autorun/BioRand7.lua")));
                Assert.Equal("{}", File.ReadAllText(Path.Combine(directory, "reframework/data/BioRand7/config.json")));
                Assert.Equal("assets", File.ReadAllText(Path.Combine(directory, "patch_002.pak")));
                Assert.Equal("log", File.ReadAllText(Path.Combine(directory, "process.log")));
                if (name == "seed.pak") Assert.Equal("pak", File.ReadAllText(target));
                else Assert.Equal("native", File.ReadAllText(Path.Combine(directory, "natives/stm/test.user.2")));
            }
            Assert.Empty(Directory.GetFiles(root));
        } finally { Directory.Delete(root, true); }
    }

    private static byte[] ZipBytes(params (string Name, string Content)[] entries) {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            foreach (var (name, content) in entries) {
                using var writer = new StreamWriter(zip.CreateEntry(name).Open());
                writer.Write(content);
            }
        }
        return stream.ToArray();
    }

    [Fact]
    public void EnsureParentDirectory_AllowsCurrentDirectoryOutputPath() {
        GenerateCommand.EnsureParentDirectory("biorand-re7-test-output.pak");
    }

    [Fact]
    public void EnsureParentDirectory_CreatesNestedOutputDirectory() {
        var outputRoot = CreateTemporaryDirectory();
        var outputPath = Path.Combine(outputRoot, "nested", "seed.pak");
        var outputDirectory = Path.GetDirectoryName(outputPath)!;

        try {
            GenerateCommand.EnsureParentDirectory(outputPath);

            Assert.True(Directory.Exists(outputDirectory));
        }
        finally {
            Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("seed.pak", ".pak")]
    [InlineData("seed.PAK", ".pak")]
    [InlineData("seed.ZiP", ".zip")]
    public void HasExtension_IsCaseInsensitive(string path, string extension) {
        Assert.True(GenerateCommand.HasExtension(path, extension));
    }

    [Fact]
    public void GetPakFile_ReadsCaseInsensitivePakEntry() {
        var expected = new byte[]{ 1, 2, 3, 4 };
        byte[] archiveBytes;
        using (var stream = new MemoryStream()) {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
                var entry = archive.CreateEntry("nested/seed.PAK");
                using var entryStream = entry.Open();
                entryStream.Write(expected);
            }
            archiveBytes = stream.ToArray();
        }

        Assert.Equal(expected, GenerateCommand.GetPakFile(archiveBytes));
    }

    [Fact]
    public void ExtractEntryToDirectory_ExtractsNestedEntry() {
        using var zip = CreateZip(("natives/stm/test.txt", "ok"));
        var outputPath = CreateTemporaryDirectory();

        try {
            GenerateCommand.ExtractEntryToDirectory(zip.Entries.Single(), outputPath);

            Assert.Equal(
                "ok",
                File.ReadAllText(Path.Combine(outputPath, "natives", "stm", "test.txt")));
        }
        finally {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("natives/../outside.txt")]
    [InlineData("natives/stm/../../outside.txt")]
    public void ExtractEntryToDirectory_RejectsTraversalEntry(string entryName) {
        using var zip = CreateZip((entryName, "bad"));
        var outputPath = CreateTemporaryDirectory();

        try {
            Assert.Throws<InvalidDataException>(() =>
                GenerateCommand.ExtractEntryToDirectory(zip.Entries.Single(), outputPath));

            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(outputPath)!, "outside.txt")));
        }
        finally {
            Directory.Delete(outputPath, recursive: true);
        }
    }

    private static ZipArchive CreateZip(params (string Name, string Content)[] entries) {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true)) {
            foreach (var (name, content) in entries) {
                var entry = zip.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(content);
            }
        }

        stream.Position = 0;
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    private static string CreateTemporaryDirectory() {
        var path = Path.Combine(Path.GetTempPath(), $"biorand-re7-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}
