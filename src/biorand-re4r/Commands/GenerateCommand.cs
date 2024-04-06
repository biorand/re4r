using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class GenerateCommand : Command<GenerateCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Seed to generate")]
            [CommandOption("-s|--seed")]
            public int Seed { get; init; }

            [Description("Configuration to use")]
            [CommandOption("-c|--config")]
            public string? Configuration { get; init; }

            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (settings.OutputPath == null)
            {
                return ValidationResult.Error($"Output path not specified");
            }
            return base.Validate(context, settings);
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var biorandConfig = Re4rConfiguration.GetDefault();
            var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
            var randomizer = chainsawRandomizerFactory.Create();
            var input = new RandomizerInput();
            input.GamePath = biorandConfig.GamePath;
            var output = randomizer.Randomize(input);

            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                output.WriteOutputPakFile(outputPath);
            }
            else
            {
                output.WriteOutputFolder(outputPath);
            }
            return 0;
        }
    }
}
