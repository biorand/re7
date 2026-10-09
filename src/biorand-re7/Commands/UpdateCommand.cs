using Biohazard.BioRand.RE7.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Biohazard.BioRand.RE7.Commands;

internal sealed class UpdateCommand : AsyncCommand<UpdateCommand.Settings> {
    public sealed class Settings : CommandSettings {
        // No arguments or options required
    }

    public override async Task<int>
        ExecuteAsync(CommandContext context, Settings settings, CancellationToken token) {
        var sourceDir = FindSourceDirectory();
        if (sourceDir == null) {
            AnsiConsole.MarkupLine("[red]Project source directory not found.[/]");
            return 1;
        }

        var dataDir = Path.Combine(sourceDir, "Biohazard.BioRand.RE7", "_Data");

        var dynamicData = new DynamicData(download: true);
        var failed = false;
        foreach (var dataName in Enum.GetValues<DynamicDataName>()) {
            token.ThrowIfCancellationRequested();
            var filename = DynamicData.GetFileName(dataName)!;

            var destinationPath = Path.Combine(dataDir, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            try {
                var fileBytes = dynamicData.GetData(dataName)!;

                if (fileBytes.Length == 0) {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]Skipped empty file {destinationPath} (0 bytes)[/]");
                    continue;
                }

                await File.WriteAllBytesAsync(destinationPath, fileBytes, token);
                AnsiConsole.MarkupLineInterpolated(
                    $"[green]Downloaded and overwrote: {destinationPath} ({fileBytes.Length} bytes)[/]");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                failed = true;
                AnsiConsole.MarkupLineInterpolated($"[red]Failed to update {filename}: {ex.Message}[/]");
            }
        }

        return failed ? 1 : 0;
    }

    internal static string? FindSourceDirectory(string? startDirectory = null) {
        var directory = new DirectoryInfo(startDirectory ?? Directory.GetCurrentDirectory());
        while (directory != null) {
            if (IsSourceDirectory(directory.FullName)) {
                return directory.FullName;
            }

            var nestedSourceDirectory = Path.Combine(directory.FullName, "src");
            if (IsSourceDirectory(nestedSourceDirectory)) {
                return Path.GetFullPath(nestedSourceDirectory);
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static bool IsSourceDirectory(string path)
        => Directory.Exists(Path.Combine(path, "Biohazard.BioRand.RE7", "_Data"));
}
