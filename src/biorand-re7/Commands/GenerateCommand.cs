using Biohazard.BioRand.RE7.Extensions;
using IntelOrca.Biohazard.BioRand;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Biohazard.BioRand.RE7.Commands;

internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings> {
    public sealed class Settings : CommandSettings {
        [Description("URL to generate")]
        [CommandOption("--url")]
        public string? Url { get; init; }

        [Description("Seed to generate")]
        [CommandOption("-s|--seed")]
        public required string Seed { get; init; }

        [Description("Configuration to use")]
        [CommandOption("-c|--config")]
        public string? ConfigPath { get; init; }

        [CommandOption("-i|--input")] public string? InputPath { get; init; }

        [CommandOption("-o|--output")] public string? OutputPath { get; init; }

        [CommandOption("-k|--kill")] public bool Kill { get; init; }
    }

    protected override ValidationResult Validate(CommandContext context, Settings settings) {
        if (settings.OutputPath == null) {
            return ValidationResult.Error($"Output path not specified");
        }

        return base.Validate(context, settings);
    }

    protected override async Task<int>
        ExecuteAsync(CommandContext context, Settings settings, CancellationToken token) {
        var reporter = new ConsoleReporter();
        if (settings.Kill) {
            reporter.RunTask("Killing re7.exe", KillRe7);
        }

        var randomizer = new RandomizerExecutor(settings.InputPath ?? "", reporter);
        RandomizerInput input;
        if (settings.Url is string url) {
            input = await FromUrl(new Uri(url));
        } else {
            input = new RandomizerInput{
                Seed = (settings.Seed == "random") ? Random.Shared.Next() : int.Parse(settings.Seed)
            };
            if (!string.IsNullOrEmpty(settings.ConfigPath)) {
                var configJson = File.ReadAllText(settings.ConfigPath);
                input.Configuration = RandomizerConfiguration.FromJson(configJson);
            } else {
                AnsiConsole.MarkupLine("[yellow]No configuration path provided. Using the default configuration.[/]");
                input.Configuration = RandomizerExecutor.DefaultConfiguration;
            }
        }

        AnsiConsole.MarkupLine($"Generating seed {input.Seed}...");

        try {
            var output = randomizer.Randomize(input);
            reporter.RunTask($"Writing {settings.OutputPath}", () => WriteOutput(output, settings.OutputPath!));
        }
        catch (Exception ex) {
            throw new RandomizerUserException("Randomization failed: " + ex);
        }

        return 0;
    }

    internal static void WriteOutput(IntelOrca.Biohazard.BioRand.RandomizerOutput output, string outputPath) {
        outputPath = Path.GetFullPath(outputPath);
        var isPak = HasExtension(outputPath, ".pak");
        var isZip = HasExtension(outputPath, ".zip");
        var directory = isPak || isZip ? Path.GetDirectoryName(outputPath)! : outputPath;
        Directory.CreateDirectory(directory);
        var patch = output.Assets.First(asset => asset.Key == "1-patch").Data;
        var fluffy = output.Assets.First(asset => asset.Key == "2-fluffy").Data;

        if (isPak) {
            GetPakFile(patch).WriteToFile(outputPath);
            ExtractLogFiles(fluffy, directory);
        } else if (isZip) {
            fluffy.WriteToFile(outputPath);
        } else {
            ExtractEntries(fluffy, directory, entry => entry.FullName != "modinfo.ini" && entry.FullName != "pic.jpg");
        }

        foreach (var asset in output.Assets.Where(asset => asset.Key != "1-patch" && asset.Key != "2-fluffy")) {
            // An archive output carries companion downloads next to it. Direct installations need
            // the shared asset PAK extracted, alongside the runtime and the generated game files.
            if (asset.Key == "3-assets" && !isZip) {
                ExtractEntries(asset.Data, directory, _ => true);
            } else {
                asset.Data.WriteToFile(Path.Combine(directory, Path.GetFileName(asset.FileName)));
            }
        }
    }

    internal static void EnsureParentDirectory(string path) {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }
    }

    internal static bool HasExtension(string path, string extension)
        => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase);

    private static void ExtractLogFiles(byte[] zipFile, string outputPath)
        => ExtractEntries(zipFile, outputPath, entry =>
            !entry.FullName.StartsWith("natives/", StringComparison.OrdinalIgnoreCase) &&
            entry.FullName != "modinfo.ini" &&
            entry.FullName != "pic.jpg");

    private static void ExtractEntries(byte[] zipFile, string outputPath, Func<ZipArchiveEntry, bool> includeEntry) {
        using var zip = new ZipArchive(new MemoryStream(zipFile));
        foreach (var entry in zip.Entries) {
            if (!includeEntry(entry))
                continue;

            ExtractEntryToDirectory(entry, outputPath);
        }
    }

    internal static void ExtractEntryToDirectory(ZipArchiveEntry entry, string outputPath) {
        if (string.IsNullOrEmpty(entry.Name)) {
            return;
        }

        if (HasParentDirectorySegment(entry.FullName)) {
            throw new InvalidDataException($"Archive entry '{entry.FullName}' contains a parent-directory segment.");
        }

        var destinationRoot = Path.GetFullPath(outputPath);
        var destinationPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
        var rootWithSeparator = Path.EndsInDirectorySeparator(destinationRoot)
            ? destinationRoot
            : destinationRoot + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!destinationPath.StartsWith(rootWithSeparator, comparison)) {
            throw new InvalidDataException($"Archive entry '{entry.FullName}' would extract outside '{outputPath}'.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        entry.ExtractToFile(destinationPath, overwrite: true);
    }

    private static bool HasParentDirectorySegment(string path) {
        var segments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment == "..");
    }

    internal static byte[] GetPakFile(byte[] zip) {
        using var input = new MemoryStream(zip, writable: false);
        using var archive = new ZipArchive(input, ZipArchiveMode.Read);
        var entry = archive.Entries.First(x => x.FullName.EndsWith(".pak", StringComparison.OrdinalIgnoreCase));
        using var output = new MemoryStream();
        using var entryStream = entry.Open();
        entryStream.CopyTo(output);
        return output.ToArray();
    }

    private static void KillRe7() {
        // Kill RE7 process if running / don't wait for him to close
        // There is only 1 process
        var process = System.Diagnostics.Process.GetProcessesByName("re7").FirstOrDefault();
        if (process != null) {
            try {
                process.Kill(entireProcessTree: false);
            }
            catch {
                // Ignore
            }
        }
    }

    private static async Task<RandomizerInput> FromUrl(Uri url) {
        var settings = LocalSettings.Default;

        var pathMatch = Regex.Match(url.LocalPath, @".*/(\d+)/?$");
        if (!pathMatch.Success)
            throw new Exception("Unexpected URL format");

        var hostMatch = Regex.Match(url.Host, @"(beta-)?.*\.biorand.net");
        if (!hostMatch.Success)
            throw new Exception("Unexpected URL format");

        var isBeta = hostMatch.Groups[1].Success;
        var apiUrl = isBeta ? "https://beta-api.biorand.net" : "https://api.biorand.net";
        var server = settings.Servers.FirstOrDefault(x => x.ApiUrl == apiUrl);
        if (server == null)
            throw new Exception($"No server defined for {apiUrl}");

        var id = int.Parse(pathMatch.Groups[1].Value);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"Bearer", server.AuthToken);
        client.BaseAddress = new Uri(apiUrl);
        var response = await client.GetFromJsonAsync<RandoResponse>($"/rando/{id}");
        if (response == null)
            throw new Exception("Invalid response from server");

        return FromResponse(response);
    }

    internal static RandomizerInput FromResponse(RandoResponse response) {
        var result = new RandomizerInput();
        result.Seed = response.Seed;
        result.UserName = response.UserName ?? "USERNAME";
        result.ProfileName = response.ProfileName;
        result.ProfileAuthor = response.ProfileUserName;
        result.ProfileDescription = response.ProfileDescription;
        result.Configuration = RandomizerConfiguration.FromDictionary(response.Config);
        return result;
    }

    internal class RandoResponse {
        public int Id { get; init; }
        public string UserName { get; init; } = "";
        public long Created { get; init; }
        public int GameId { get; init; }
        public string GameMoniker { get; init; } = "";
        public int ProfileId { get; init; }
        public string ProfileName { get; init; } = "";
        public string ProfileDescription { get; init; } = "";
        public string ProfileUserName { get; init; } = "";
        public int Seed { get; init; }
        public string Version { get; init; } = "";
        public int Status { get; init; }
        public Dictionary<string, object> Config { get; init; } = new();
        public string ShareUrl { get; init; } = "";
        public string Instructions { get; init; } = "";
        public string FailReason { get; init; } = "";
    }

    private class ConsoleReporter() : IRandomizerProgress {
        public void RunTask(string text, Action cb) {
            long timeInMs = 0;
            AnsiConsole
                .Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("teal"))
                .Start(text, ctx => {
                    var sw = new Stopwatch();
                    sw.Start();
                    cb();
                    sw.Stop();
                    timeInMs = sw.ElapsedMilliseconds;
                });
            AnsiConsole.MarkupLine($"[lime]:check_box_with_check:  {text} ({timeInMs} ms)[/]");
        }
    }

    private class LocalSettings {
        private static LocalSettings? _default;

        public ImmutableArray<Server> Servers { get; set; } = [];

        public static LocalSettings Default {
            get {
                if (_default == null) {
                    try {
                        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                        var settingsPath = Path.Combine(homePath, ".biorand", "local.json");
                        var settings = File.ReadAllText(settingsPath);
                        _default = JsonSerializer.Deserialize<LocalSettings>(settings, new JsonSerializerOptions(){
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })!;
                    }
                    catch {
                        _default = new LocalSettings();
                    }
                }

                return _default;
            }
        }
    }

    private class Server {
        public string Name { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public string AuthToken { get; set; } = "";
    }
}
