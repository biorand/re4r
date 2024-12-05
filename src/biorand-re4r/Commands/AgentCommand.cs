using System.ComponentModel;
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
            var agent = new RandomizerAgent(settings.Host, settings.ApiKey, new RandomizerAgentHandler(settings.InputPath));
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                cts.Cancel();
            };
            await agent.RunAsync(cts.Token);
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
                return Task.FromResult(Randomizer.Randomize(input));
            }
        }
    }
}
