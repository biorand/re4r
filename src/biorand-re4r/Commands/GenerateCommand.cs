using System.ComponentModel;
using System.IO.Compression;
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
                input.Configuration = RandomizerConfiguration.FromJson(configJson);
            }
            var output = randomizer.Randomize(input);

            // Create log files
            foreach (var log in output.Logs)
            {
                log.Value.WriteToFile(log.Key);
            }

            // Find pak file
            var pakFile = GetPakFile(output.PakOutput);
            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                pakFile.WriteToFile(outputPath);
            }
            else
            {
                var pakList = chainsawRandomizerFactory.GetDefaultPakList();
                await new PakFile(pakFile).ExtractAllAsync(pakList, outputPath);
            }
            return 0;
        }

        private static byte[] GetPakFile(byte[] zip)
        {
            var archive = new ZipArchive(new MemoryStream(zip));
            var entry = archive.Entries.First(x => x.FullName.EndsWith(".pak"));
            var output = new MemoryStream();
            entry.Open().CopyTo(output);
            return output.ToArray();
        }
    }
}
