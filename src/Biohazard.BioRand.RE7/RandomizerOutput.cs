using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace Biohazard.BioRand.RE7;

public sealed class RandomizerOutput {
    private static readonly HttpClient Http = new();
    private byte[]? _zipFile;
    private byte[]? _modFile;

    public RandomizerInput Input { get; }
    public PakFileBuilder PakFile { get; }
    public PakFileBuilder AdditionalAssetPakFile { get; }
    public Dictionary<string, string> LogFiles { get; }
    public int PakVersion { get; }
    public bool IsWithREFramework { get; }
    public bool HasAdditionalAssets => AdditionalAssetPakFile.Entries.Count != 0;

    private static readonly string[] REFrameworkScriptPaths = [
        "BioRand7.lua",
        "BioRand7/config.lua",
        "BioRand7/context.lua",
        "BioRand7/data.lua",
        "BioRand7/em3300_explosions.lua",
        "BioRand7/em8000_knee_down.lua",
        "BioRand7/enemy_drops.lua",
        "BioRand7/game.lua",
        "BioRand7/inventory.lua",
        "BioRand7/logger.lua",
        "BioRand7/madhouse_saves.lua",
        "BioRand7/object_cache.lua",
        "BioRand7/random_events.lua",
        "BioRand7/reload_speed.lua",
        "BioRand7/rng.lua",
        "BioRand7/static_mia.lua",
        "BioRand7/ui.lua",
    ];

    private const string REFrameworkNightlyUrl =
        "https://github.com/praydog/REFramework-nightly/releases/latest/download/RE7.zip";

    internal RandomizerOutput(RandomizerInput input, PakFileBuilder pakFile, PakFileBuilder additionalAssetPakFile,
        Dictionary<string, string> logFiles, int pakVersion, bool isWithREFramework) {
        Input = input;
        PakFile = pakFile;
        AdditionalAssetPakFile = additionalAssetPakFile;
        LogFiles = logFiles;
        PakVersion = pakVersion;
        IsWithREFramework = isWithREFramework;
    }

    public byte[] GetOutputZip() {
        if (_zipFile != null)
            return _zipFile;

        var zipFile = BuildZipFile()
            .AddEntry($"re_chunk_000.pak.patch_{PakVersion:000}.pak", PakFile.ToByteArray());
        if (HasAdditionalAssets) {
            zipFile.AddEntry($"re_chunk_000.pak.patch_{PakVersion + 1:000}.pak", AdditionalAssetPakFile.ToByteArray());
        }
        _zipFile = zipFile.Build();
        return _zipFile;
    }

    public byte[] GetOutputMod() {
        if (_modFile != null)
            return _modFile;

        var zipFile = BuildZipFile();
        // Fluffy must install and uninstall the whole seed, including shared visual
        // assets. Omitting these leaves room loads dependent on a separate manual install.
        var seedPaths = PakFile.Entries.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in AdditionalAssetPakFile.Entries) {
            // Match FileRepository.GetFile: seed-specific files override shared assets.
            if (!seedPaths.Contains(entry.Key)) {
                zipFile.AddEntry(entry.Key, (byte[])entry.Value);
            }
        }

        foreach (var entry in PakFile.Entries) {
            zipFile.AddEntry(entry.Key, (byte[])entry.Value);
        }

        _modFile = zipFile
            .AddEntry("pic.jpg", EmbeddedData.GetFile("modimage.jpg"))
            .AddEntry("modinfo.ini", GetModInfo())
            .Build();
        return _modFile;
    }

    private ZipFileBuilder BuildZipFile(string logPrefix = "") {
        var builder = new ZipFileBuilder();
        var configBytes = Encoding.UTF8.GetBytes(Input.Configuration.ToJson());
        builder.AddEntry($"{logPrefix}config.json", configBytes);

        foreach (var logFile in LogFiles) {
            builder.AddEntry($"{logPrefix}{logFile.Key}", Encoding.UTF8.GetBytes(logFile.Value));
        }

        if (IsWithREFramework) {
            foreach (var scriptPath in REFrameworkScriptPaths) {
                var autorunPath = $"reframework/autorun/{scriptPath}";
                builder.AddEntry(autorunPath, EmbeddedData.GetFile(autorunPath));
            }

            builder.AddEntry("reframework/data/BioRand7/config.json", GetREFrameworkConfigBytes());
        }

        if (Input.Configuration.GetValueOrDefault<bool>("debug-download-reframework-nightly")) {
            var zipBytes = Http.GetByteArrayAsync(REFrameworkNightlyUrl)
                .GetAwaiter()
                .GetResult();

            using var ms = new MemoryStream(zipBytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            var entry = zip.GetEntry("dinput8.dll")!;

            using var entryStream = entry.Open();
            using var entryMs = new MemoryStream();
            entryStream.CopyTo(entryMs);
            builder.AddEntry("dinput8.dll", entryMs.ToArray());
        }

        return builder;
    }

    private byte[] GetREFrameworkConfigBytes() {
        var config = JsonNode.Parse(Input.Configuration.ToJson())?.AsObject() ?? [];
        config["biorand-seed"] = Input.Seed;
        return Encoding.UTF8.GetBytes(config.ToJsonString());
    }

    private byte[] GetModInfo() {
        var rf = RandomizerFactory.Default;

        var name = $"BioRand - {Sanitize(Input.ProfileName)} [{Input.Seed}]";
        var description = SanitizeParagraph(
            $"{Sanitize(Input.ProfileName)} by {Sanitize(Input.ProfileAuthor)} [{Input.Seed}]\n" +
            Input.ProfileDescription + "\n\n" +
            "Requires the Steam RT/DX12 version of RE7. Enable only one BioRand seed at a time. " +
            "Shared assets are included; do not also install the Patch ZIP." +
            (IsWithREFramework ? " Install REFramework for RE7 RT separately." : ""));
        var author = "BioRand 7 by IntelOrca, Descole & BioRand Team";
        var version = $"{rf.CurrentVersionNumber} ({rf.GitHash})";

        var lines = new List<string>{
            $"name={name}",
            $"version={version}",
            $"description={description}",
            "screenshot=pic.jpg",
            $"author={author}",
            "category=!Other > Misc"
        };

        if (IsWithREFramework) {
            lines.Add("requirement=RE Framework");
        }

        lines.Add("");

        var content = string.Join('\n', lines);
        return Encoding.UTF8.GetBytes(content);
    }

    private static string SanitizeParagraph(string? s) {
        return (s ?? "").Trim().ReplaceLineEndings("\\n");
    }

    private static string Sanitize(string? s) {
        return (s ?? "").Trim().ReplaceLineEndings(" ");
    }
}
