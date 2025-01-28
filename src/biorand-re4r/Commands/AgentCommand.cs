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
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var randomizer = new Re4rRandomizer();
            var agent = new RandomizerAgent(
                settings.Host,
                settings.ApiKey,
                1,
                new RandomizerAgentHandler(settings.InputPath));
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

        private class RandomizerAgentHandler : IRandomizerAgentHandler
        {
            private readonly string _gamePath;

            public IRandomizer Randomizer { get; } = new Re4rRandomizer();

            public RandomizerAgentHandler(string gamePath)
            {
                _gamePath = gamePath;
            }

            public Task<bool> CanGenerateAsync(RandomizerAgent.QueueResponseItem queueItem)
            {
                return Task.FromResult(true);
            }

            public Task<RandomizerOutput> GenerateAsync(RandomizerAgent.QueueResponseItem queueItem, RandomizerInput input)
            {
                input.GamePath = _gamePath;

                var config = input.Configuration;

                // Special things for specific users
                if (config.GetValueOrDefault<bool>("separate-ways"))
                {
                    var userTags = queueItem.UserTags;
                    if (!userTags.Contains("re4r:tester") && !userTags.Any(x => x.StartsWith("re4r:patron/")))
                    {
                        throw new RandomizerUserException("Separate Ways is currently only available to patrons.");
                    }
                }

                // Special things for specific users
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
                config["special"] = string.Join(",", specials);

                return Task.FromResult(Randomizer.Randomize(input));
            }

            public void LogInfo(string message) => AnsiConsole.MarkupLine($"[gray]{Timestamp} {message}[/]");
            public void LogError(Exception ex, string message) => AnsiConsole.MarkupLine($"[red]{Timestamp} {message} ({ex.Message})[/]");

            private static string Timestamp => DateTime.Now.ToString("[[yyyy-MM-dd HH:mm]]");
        }
    }
}
