using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class AgentCommand : AsyncCommand<AgentCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Host")]
            [CommandArgument(0, "<host>")]
            public required string Host { get; init; }

            [Description("Seed to generate")]
            [CommandOption("-k|--key")]
            public required string ApiKey { get; init; }

            [CommandOption("-i|--input")]
            public required string InputPath { get; init; }

            [CommandOption("-b|--beta")]
            public bool Beta { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var agent = new RandomizerAgent(
                settings.Host,
                settings.ApiKey,
                1,
                new RandomizerAgentHandler(settings.InputPath, settings.Beta));
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            try
            {
                await agent.RunAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
            }
            return 0;
        }

        private class RandomizerAgentHandler(string gameInputPath, bool beta) : IRandomizerAgentHandler
        {
            public string BuildVersion => Re4rRandomizer.BuildVersion;
            public RandomizerConfigurationDefinition ConfigurationDefinition => Re4rRandomizer.ConfigurationDefinition;
            public RandomizerConfiguration DefaultConfiguration => Re4rRandomizer.DefaultConfiguration;

            public Task<bool> CanGenerateAsync(RandomizerAgent.QueueResponseItem queueItem)
            {
                return Task.FromResult(true);
            }

            public Task<RandomizerOutput> GenerateAsync(RandomizerAgent.QueueResponseItem queueItem, RandomizerInput input)
            {
                var config = input.Configuration;

                // Special things for specific users
                if (beta)
                {
                    if (!queueItem.UserTags.Contains("re4r:tester"))
                    {
                        throw new RandomizerUserException("The RE4R beta randomizer is currently only available to testers.");
                    }
                }

                var specials = new List<string>();
                var userName = queueItem.UserName ?? "";
                if (userName.Equals("bawkbasoup", StringComparison.OrdinalIgnoreCase))
                {
                    specials.Add("bawk");
                }
                // if (userName.Equals("doubleedger", StringComparison.OrdinalIgnoreCase))
                // {
                //     specials.Add("goldbar");
                // }
                config["username"] = userName;
                config["special"] = string.Join(",", specials);

                var randomizer = new Re4rRandomizer(gameInputPath, new EmptyReporter());
                return Task.FromResult(randomizer.Randomize(input));
            }

            public void LogInfo(string message) => AnsiConsole.MarkupLine($"[gray]{Timestamp} {message}[/]");
            public void LogError(Exception ex, string message) => AnsiConsole.MarkupLine($"[red]{Timestamp} {message} ({ex.Message})[/]");

            private static string Timestamp => DateTime.Now.ToString("[[yyyy-MM-dd HH:mm]]");
        }

        private class EmptyReporter : IProgressReporter
        {
            public void RunTask(string text, Action cb)
            {
                cb();
            }
        }
    }
}
