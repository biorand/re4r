using System.ComponentModel;
using System.IO.Compression;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
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

            [CommandOption("-i|--input")]
            public string? InputPath { get; init; }

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


        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var randomizer = GetRandomizer();
            var input = new RandomizerInput();
            input.Seed = settings.Seed;
            input.GamePath = settings.InputPath;
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

            foreach (var asset in output.Assets)
            {
                asset.Data.WriteToFile(asset.FileName);
            }

            // Find pak file
            var pakFile = GetPakFile(output.Assets.First(x => x.Key == "1-patch").Data);
            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak"))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                pakFile.WriteToFile(outputPath);
            }
            else
            {
                using var zip = new ZipArchive(new MemoryStream(output.Assets.First(x => x.Key == "2-fluffy").Data));
                foreach (var entry in zip.Entries)
                {
                    if (!entry.FullName.StartsWith("natives/", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var destinationPath = Path.Combine(outputPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
            return Task.FromResult(0);
        }

        private static byte[] GetPakFile(byte[] zip)
        {
            var archive = new ZipArchive(new MemoryStream(zip));
            var entry = archive.Entries.First(x => x.FullName.EndsWith(".pak"));
            var output = new MemoryStream();
            entry.Open().CopyTo(output);
            return output.ToArray();
        }

        private IRandomizer GetRandomizer()
        {
            return new Re4rRandomizer();
        }
    }
}
