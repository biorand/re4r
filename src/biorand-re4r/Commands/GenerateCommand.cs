using System.ComponentModel;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using REE;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Seed to generate")]
            [CommandOption("-s|--seed")]
            public int Seed { get; init; }

            [Description("Configuration to use")]
            [CommandOption("-c|--config")]
            public string? ConfigPath { get; init; }

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


        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var biorandConfig = Re4rConfiguration.GetDefault();
            var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
            var randomizer = chainsawRandomizerFactory.Create();
            var input = new RandomizerInput();
            input.Seed = settings.Seed;
            input.GamePath = biorandConfig.GamePath;
            if (!string.IsNullOrEmpty(settings.ConfigPath))
            {
                var configJson = File.ReadAllText(settings.ConfigPath);
                input.Configuration = RandomizerConfigurationDefinition.ProcessConfig(configJson);
            }
            var output = randomizer.Randomize(input);

            // Create log files
            output.LogFiles.Input.WriteToFile("input.log");
            output.LogFiles.Process.WriteToFile("process.log");
            output.LogFiles.Output.WriteToFile("output.log");

            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                output.PakFile.WriteToFile(outputPath);
            }
            else
            {
                var pakList = chainsawRandomizerFactory.GetDefaultPakList();
                await new PakFile(output.PakFile).ExtractAllAsync(pakList, outputPath);
            }
            return 0;
        }
    }
}
