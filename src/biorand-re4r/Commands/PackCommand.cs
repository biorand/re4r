using System.ComponentModel;
using REE;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class PackCommand : Command<PackCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Input files to pack")]
            [CommandArgument(0, "<input>")]
            public required string[] InputPaths { get; init; }

            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (settings.OutputPath == null)
            {
                return ValidationResult.Error($"Output path not specified");
            }
            foreach (var inputPath in settings.InputPaths)
            {
                if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
                {
                    return ValidationResult.Error($"{inputPath} not found");
                }
            }
            return base.Validate(context, settings);
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var builder = new PakFileBuilder();
            foreach (var inputPath in settings.InputPaths)
            {
                var fullInputPath = Path.GetFullPath(inputPath);
                var basePath = Path.GetDirectoryName(fullInputPath)!;
                if (Directory.Exists(fullInputPath))
                {
                    var files = Directory.GetFiles(fullInputPath, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var relativePath = Path.GetRelativePath(basePath, file);
                        builder.AddEntry(relativePath, File.ReadAllBytes(file));
                        AnsiConsole.WriteLine(relativePath);
                    }
                }
                else
                {
                    var relativePath = Path.GetFileName(fullInputPath);
                    builder.AddEntry(relativePath, File.ReadAllBytes(fullInputPath));
                    AnsiConsole.WriteLine(relativePath);
                }
            }
            builder.Save(settings.OutputPath!, PakFlags.ZSTD);
            return 0;
        }
    }
}
