using System.ComponentModel;
using REE;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.Settings.ApplicationName = "biorand-re4r";
                config.AddCommand<PackCommand>("web-server")
                    .WithDescription("Runs a local web server for generating randos")
                    .WithExample("web-server", "-p", "8080");
                config.AddCommand<GenerateCommand>("generate")
                    .WithDescription("Generates a new rando")
                    .WithExample("generate", "-o", "re_chunk_000.pak.patch_004.pak", "--seed", "35825", "--config", "tough.json");
                config.AddCommand<PackCommand>("pack")
                    .WithDescription("Creates a .pak file from the given input files")
                    .WithExample("pack", "-o", "output.pak", "natives");
            });
            return app.Run(args);
        }
    }

    internal sealed class WebServerCommand : Command<WebServerCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Port to host the web server on")]
            [CommandOption("-p|--port")]
            [DefaultValue(10285)]
            public int Port { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            return base.Validate(context, settings);
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            return 0;
        }
    }

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
                var basePath = Path.GetDirectoryName(inputPath)!;
                if (Directory.Exists(inputPath))
                {
                    var files = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var relativePath = Path.GetRelativePath(basePath, file);
                        builder.AddEntry(relativePath, File.ReadAllBytes(file));
                        AnsiConsole.WriteLine(relativePath);
                    }
                }
                else
                {
                    var relativePath = Path.GetFileName(inputPath);
                    builder.AddEntry(relativePath, File.ReadAllBytes(inputPath));
                    AnsiConsole.WriteLine(relativePath);
                }
            }
            builder.Save(settings.OutputPath!, PakFlags.ZSTD);
            return 0;
        }
    }
}
