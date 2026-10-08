using IntelOrca.Biohazard.REE.Package;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Biohazard.BioRand.RE7.Commands;

internal sealed class SetupCommand : AsyncCommand<SetupCommand.Settings> {
    private const string BasePakFileName = "re_chunk_000.pak";

    public sealed class Settings : CommandSettings {
        [CommandOption("-i|--input")] public string? InputPath { get; init; }

        [CommandOption("-o|--output")] public string? OutputPath { get; init; }

        [CommandOption("--full")] public bool Full { get; init; }
    }

    public override ValidationResult Validate(CommandContext context, Settings settings) {
        if (settings.InputPath == null) {
            return ValidationResult.Error($"Input path not specified");
        }

        if (settings.OutputPath == null) {
            return ValidationResult.Error($"Output path not specified");
        }

        return base.Validate(context, settings);
    }

    public override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken token) {
        // --full remains accepted for compatibility. Both forms must produce a usable baseline.
        var patternList = FullPatterns;
        var gamePath = settings.InputPath!;
        using var pak = OpenGamePaks(gamePath);

        var outputPath = settings.OutputPath!;
        if (outputPath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) {
            var newPak = new PakFileBuilder();
            HarvestFiles(pak, patternList, (path, data) => { newPak.AddEntry(path, data); });
            GenerateCommand.EnsureParentDirectory(outputPath);
            newPak.Save(settings.OutputPath!, CompressionKind.Zstd);
        } else {
            HarvestFiles(pak, patternList, (path, data) => {
                var targetPath = Path.Combine(outputPath, path);
                var targetDir = Path.GetDirectoryName(targetPath)!;
                Directory.CreateDirectory(targetDir);
                File.WriteAllBytes(targetPath, data);
            });
        }

        return Task.FromResult(0);
    }

    private static IPakFile OpenGamePaks(string gamePath) {
        var basePakPath = Path.Combine(gamePath, BasePakFileName);
        if (!File.Exists(basePakPath))
            throw new FileNotFoundException($"Failed to find {BasePakFileName}.", basePakPath);

        var paks = new List<IPakFile>{
            new PatchedPakFile(basePakPath)
        };
        paks.AddRange(EnumerateAdditionalPakPaths(gamePath).Select(path => (IPakFile)new PakFile(path)));

        return new PakFileCollection(paks.ToImmutableArray());
    }

    private static IEnumerable<string> EnumerateAdditionalPakPaths(string gamePath)
        => Directory.EnumerateFiles(gamePath, "*.pak", SearchOption.AllDirectories)
            .Where(path => !IsBasePakFamily(gamePath, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

    private static bool IsBasePakFamily(string gamePath, string path) {
        var relativePath = Path.GetRelativePath(gamePath, path);
        if (relativePath.Contains(Path.DirectorySeparatorChar) ||
            relativePath.Contains(Path.AltDirectorySeparatorChar)) {
            return false;
        }

        var fileName = Path.GetFileName(path);
        return fileName.Equals(BasePakFileName, StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(
                   fileName,
                   $"^{Regex.Escape(BasePakFileName)}\\.patch_[0-9]{{3}}\\.pak$",
                   RegexOptions.IgnoreCase);
    }

    private static void HarvestFiles(
        IPakFile pak,
        ImmutableArray<string> patternList,
        Action<string, byte[]> cb
    ) {
        var pakList = RandomizerExecutor.GetDefaultPakList();
        var patternListRegex = patternList.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();
        foreach (var path in pakList.Entries) {
            if (!patternListRegex.Any(x => x.IsMatch(path)))
                continue;

            var file = pak.GetEntryData(path);
            if (file == null) {
                Console.WriteLine("X " + path);
            } else {
                cb(path, file);
                Console.WriteLine("* " + path);
            }
        }
    }

    private static readonly ImmutableArray<string> FullPatterns =[
        @"natives/.*\.gui\.\d+",
        @"natives/.*\.motfsm2\.\d+",
        @"natives/.*\.msg\.\d+",
        @"natives/.*\.pfb\.\d+",
        @"natives/.*\.scn\.\d+",
        @"natives/.*\.user\.\d+",
        @"natives/.*\.uvar\.\d+",
        @"natives/.*\.rcol\.\d+",
        @"natives/stm/animation/weapon/.*\.motlist\.\d+", // only weapons, otherwise the PAK file size explodes
        @"natives/stm/animation/weapon/.*\.motbank\.\d+"
    ];

}
