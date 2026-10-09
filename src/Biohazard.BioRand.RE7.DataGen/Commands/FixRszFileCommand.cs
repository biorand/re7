using Spectre.Console;
using Spectre.Console.Cli;
using static Biohazard.BioRand.RE7.DataGen.Commands.FixRszFileCommand;

namespace Biohazard.BioRand.RE7.DataGen.Commands;

internal sealed class FixRszFileCommand : Command<Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandArgument(0, "<rszInputFile>")] public string RszInputFile { get; set; } = "";

        [CommandArgument(1, "<il2cppDumpFile>")]
        public string Il2CppJsonDumpFile { get; set; } = "";

        [CommandArgument(2, "<typeName>")] public string OutputFileName { get; set; } = "";
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken token) {
        try {
            // TODO
            //AnsiConsole.MarkupLine($"[green]Generated[/] {settings.OutputFileName}");
            return 0;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return -1;
        }
    }
}