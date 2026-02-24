using System.Collections.Immutable;
using System.ComponentModel;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class GenerateCommand : AsyncCommand<GenerateCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("URL to generate")]
            [CommandOption("--url")]
            public string? Url { get; init; }

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


        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var reporter = new ConsoleReporter();
            if (settings.Kill)
            {
                reporter.RunTask("Killing re4.exe", () => KillRe4());
            }

            var randomizer = new Re4rRandomizer(settings.InputPath ?? "", true, reporter);
            RandomizerInput input;
            if (settings.Url is string url)
            {
                input = await FromUrl(new Uri(url));
            }
            else
            {
                input = new RandomizerInput
                {
                    Seed = settings.Seed
                };
                if (!string.IsNullOrEmpty(settings.ConfigPath))
                {
                    var configJson = File.ReadAllText(settings.ConfigPath);
                    input.Configuration = RandomizerConfiguration.FromJson(configJson);
                }
            }

            AnsiConsole.MarkupLine($"Generating seed {input.Seed}...");
            var output = randomizer.Randomize(input);

            foreach (var asset in output.Assets)
            {
                asset.Data.WriteToFile(asset.FileName);
            }

            // Find pak file
            var pakFile = GetPakFile(output.Assets.First(x => x.Key == "1-patch").Data);
            var zipFile = output.Assets.First(x => x.Key == "2-fluffy").Data;

            reporter.RunTask($"Extracting log files", () =>
            {
                ExtractLogFiles(zipFile, Environment.CurrentDirectory);
            });

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
            return 0;
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

        private static void ExtractLogFiles(byte[] zipFile, string outputPath)
        {
            using var zip = new ZipArchive(new MemoryStream(zipFile));
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.StartsWith("natives/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.FullName == "modinfo.ini")
                    continue;
                if (entry.FullName == "pic.jpg")
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

        private static async Task<RandomizerInput> FromUrl(Uri url)
        {
            var settings = LocalSettings.Default;

            var pathMatch = Regex.Match(url.LocalPath, @".*/(\d+)/?$");
            if (!pathMatch.Success)
                throw new Exception("Unexpected URL format");

            var hostMatch = Regex.Match(url.Host, @"(beta-)?.*\.biorand.net");
            if (!hostMatch.Success)
                throw new Exception("Unexpected URL format");

            var isBeta = hostMatch.Groups[1].Success;
            var apiUrl = isBeta ? "https://beta-api.biorand.net" : "https://api.biorand.net";
            var server = settings.Servers.FirstOrDefault(x => x.ApiUrl == apiUrl);
            if (server == null)
                throw new Exception($"No server defined for {apiUrl}");

            var seed = int.Parse(pathMatch.Groups[1].Value);
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue($"Bearer", server.AuthToken);
            client.BaseAddress = new Uri(apiUrl);
            var response = await client.GetFromJsonAsync<RandoResponse>($"/rando/{seed}");
            if (response == null)
                throw new Exception("Invalid response from server");

            var result = new RandomizerInput();
            result.Seed = response.Seed;
            result.UserName = response.UserName ?? "USERNAME";
            result.ProfileName = response.ProfileName;
            result.ProfileAuthor = response.ProfileUserName;
            result.ProfileDescription = response.ProfileDescription;
            result.Configuration = RandomizerConfiguration.FromDictionary(response.Config);
            return result;
        }

        private class RandoResponse
        {
            public int Id { get; init; }
            public string UserName { get; init; } = "";
            public long Created { get; init; }
            public int GameId { get; init; }
            public string GameMoniker { get; init; } = "";
            public int ProfileId { get; init; }
            public string ProfileName { get; init; } = "";
            public string ProfileDescription { get; init; } = "";
            public string ProfileUserName { get; init; } = "";
            public int Seed { get; init; }
            public string Version { get; init; } = "";
            public int Status { get; init; }
            public Dictionary<string, object> Config { get; init; } = new();
            public string ShareUrl { get; init; } = "";
            public string Instructions { get; init; } = "";
            public string FailReason { get; init; } = "";
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

        private class LocalSettings
        {
            private static LocalSettings? _default;

            public ImmutableArray<Server> Servers { get; set; } = [];

            public static LocalSettings Default
            {
                get
                {
                    if (_default == null)
                    {
                        try
                        {
                            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            var settingsPath = Path.Combine(homePath, ".biorand", "local.json");
                            var settings = File.ReadAllText(settingsPath);
                            _default = JsonSerializer.Deserialize<LocalSettings>(settings, new JsonSerializerOptions()
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            })!;
                        }
                        catch
                        {
                            _default = new LocalSettings();
                        }
                    }
                    return _default;
                }
            }
        }

        private class Server
        {
            public string Name { get; set; } = "";
            public string ApiUrl { get; set; } = "";
            public string AuthToken { get; set; } = "";
        }
    }
}
