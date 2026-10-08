using IntelOrca.Biohazard.BioRand;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Biohazard.BioRand.RE7.Commands;

internal sealed class AgentCommand : AsyncCommand<AgentCommand.Settings> {
    public sealed class Settings : CommandSettings {
        [Description("Host")]
        [CommandArgument(0, "<host>")]
        public required string Host { get; init; }

        [Description("Seed to generate")]
        [CommandOption("-k|--key")]
        public required string ApiKey { get; init; }

        [CommandOption("-i|--input")] public required string InputPath { get; init; }

        [CommandOption("-b|--beta")] public required bool Beta { get; init; }
    }

    public override async Task<int>
        ExecuteAsync(CommandContext context, Settings settings, CancellationToken token) {
        var gameId = await GetGameIdAsync(settings.Host, "re7")
                     ?? throw new Exception("re7 game moniker not found.");

        var agent = new RandomizerAgent(
            settings.Host,
            settings.ApiKey,
            gameId,
            new RandomizerAgentHandler(settings.InputPath, settings.Beta));
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        ConsoleCancelEventHandler cancelHandler = (_, e) => {
            e.Cancel = true;
            cts.Cancel();
        };
        Console.CancelKeyPress += cancelHandler;
        try {
            await agent.RunAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
        finally {
            Console.CancelKeyPress -= cancelHandler;
        }

        return 0;
    }

    private static async Task<int?> GetGameIdAsync(string uri, string moniker) {
        var client = new RandomizerClient(uri);
        var games = await client.GetGamesAsync();
        var game = games.FirstOrDefault(x => x.Moniker == moniker);
        return game?.Id;
    }

    private class RandomizerAgentHandler(string gameInputPath, bool beta) : IRandomizerAgentHandler {
        public string BuildVersion => RandomizerExecutor.BuildVersion;
        public RandomizerConfigurationDefinition ConfigurationDefinition => RandomizerExecutor.ConfigurationDefinition;
        public RandomizerConfiguration DefaultConfiguration => RandomizerExecutor.DefaultConfiguration;

        public Task<bool> CanGenerateAsync(RandomizerAgent.QueueResponseItem queueItem) {
            return Task.FromResult(true);
        }

        public Task<IntelOrca.Biohazard.BioRand.RandomizerOutput> GenerateAsync(
            RandomizerAgent.QueueResponseItem queueItem, RandomizerInput input) {
            var config = input.Configuration;

            // Special things for specific users
            if (beta) {
                if (!queueItem.UserTags.Contains("re7:tester")) {
                    throw new RandomizerUserException(
                        "The RE7 beta randomizer is currently only available to testers.");
                }
            }

            var specials = new List<string>();
            var userName = queueItem.UserName ?? "";
            if (userName.Equals("bawkbasoup", StringComparison.OrdinalIgnoreCase)) {
                specials.Add("bawk");
            }

            config["username"] = userName;
            config["tags"] = string.Join(",", queueItem.UserTags);
            config["special"] = string.Join(",", specials);

            var randomizer = new RandomizerExecutor(gameInputPath, new EmptyReporter());
            return Task.FromResult(randomizer.Randomize(input));
        }

        public void LogInfo(string message) =>
            AnsiConsole.MarkupLine($"[gray]{Timestamp} {Markup.Escape(message)}[/]");

        public void LogError(Exception ex, string message) =>
            AnsiConsole.MarkupLine(
                $"[red]{Timestamp} {Markup.Escape(message)} ({Markup.Escape(ex.Message)})[/]");

        private static string Timestamp => DateTime.Now.ToString("[[yyyy-MM-dd HH:mm]]");
    }
}
