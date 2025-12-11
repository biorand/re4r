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

            [CommandOption("-k|--kill")]
            public bool Kill { get; init; }
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
            var reporter = new ConsoleReporter();
            if (settings.Kill)
            {
                reporter.RunTask("Killing re4.exe", () => KillRe4());
            }

            var randomizer = new Re4rRandomizer(reporter);
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
            var zipFile = output.Assets.First(x => x.Key == "2-fluffy").Data;

            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak"))
            {
                reporter.RunTask($"Writing {outputPath}", () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    pakFile.WriteToFile(outputPath);
                });
#if DEBUG
                reporter.RunTask($"Extracting files", () =>
                {
                    ExtractNatives(zipFile, Path.GetDirectoryName(outputPath)!);
                });
#endif
            }
            else if (outputPath.EndsWith(".zip"))
            {
                reporter.RunTask($"Writing {outputPath}", () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                    zipFile.WriteToFile(outputPath);
                });
            }
            else
            {
                reporter.RunTask($"Writing {outputPath}", () =>
                {
                    using var zip = new ZipArchive(new MemoryStream(zipFile));
                    foreach (var entry in zip.Entries)
                    {
                        if (!entry.FullName.StartsWith("natives/", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var destinationPath = Path.Combine(outputPath, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                });
            }
            return Task.FromResult(0);
        }

        private static void ExtractNatives(byte[] zipFile, string outputPath)
        {
            var nativesDirectory = Path.Combine(outputPath, "natives");
            if (Directory.Exists(nativesDirectory))
                Directory.Delete(nativesDirectory, true);

            using var zip = new ZipArchive(new MemoryStream(zipFile));
            foreach (var entry in zip.Entries)
            {
                if (!entry.FullName.StartsWith("natives/", StringComparison.OrdinalIgnoreCase))
                    continue;

                var destinationPath = Path.Combine(outputPath, entry.FullName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                entry.ExtractToFile(destinationPath, overwrite: true);
            }
        }

        private static byte[] GetPakFile(byte[] zip)
        {
            var archive = new ZipArchive(new MemoryStream(zip));
            var entry = archive.Entries.First(x => x.FullName.EndsWith(".pak"));
            var output = new MemoryStream();
            entry.Open().CopyTo(output);
            return output.ToArray();
        }

        private static void KillRe4()
        {
            // Kill RE4 process if running / don't wait for him to close
            // There is only 1 process
            var process = System.Diagnostics.Process.GetProcessesByName("re4").FirstOrDefault();
            if (process != null)
            {
                try
                {
                    process.Kill(entireProcessTree: false);
                }
                catch
                {
                    // Ignore
                }
            }
        }

        private class ConsoleReporter() : IProgressReporter
        {
            public void RunTask(string text, Action cb)
            {
                AnsiConsole
                    .Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("teal"))
                    .Start(text, ctx =>
                    {
                        cb();
                    });
                AnsiConsole.MarkupLine($"[lime]:check_box_with_check:  {text}[/]");
            }
        }
    }
}
