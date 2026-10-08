using Biohazard.BioRand.RE7.DataGen.CodeGen;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using static Biohazard.BioRand.RE7.DataGen.Commands.RszToCsCommand;

namespace Biohazard.BioRand.RE7.DataGen.Commands;

internal sealed class RszToCsCommand : Command<Settings> {
    internal sealed class Settings : CommandSettings {
        [CommandArgument(0, "<typeName>")] public string TypeName { get; set; } = default!;

        [CommandOption("--with-enums")]
        [DefaultValue(false)]
        public bool WithEnums { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken token) {
        try {
            var output = RszCodeGenerator.Generate(settings.TypeName, settings.WithEnums);
            FileWriter.WriteOutput($"{settings.TypeName}.cs", output);

            AnsiConsole.MarkupLine($"[green]Generated[/] {settings.TypeName}.cs");
            return 0;
        }
        catch (Exception ex) {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return -1;
        }
    }
}