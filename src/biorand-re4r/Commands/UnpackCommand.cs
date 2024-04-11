using System.ComponentModel;
using REE;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class UnpackCommand : AsyncCommand<UnpackCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Input pak file")]
            [CommandArgument(0, "<input>")]
            public required string InputPath { get; init; }

            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (!File.Exists(settings.InputPath))
            {
                return ValidationResult.Error($"{settings.InputPath} not found");
            }
            return base.Validate(context, settings);
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var pakFile = new PakFile(settings.InputPath);
            var outputPath = settings.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Environment.CurrentDirectory;
            }

            var crf = ChainsawRandomizerFactory.Default;
            var pakList = crf.GetDefaultPakList();

            await pakFile.ExtractAllAsync(pakList, outputPath);
            return 0;
        }
    }
}
