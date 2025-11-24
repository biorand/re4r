using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            // No arguments or options required
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var solutionDir = FindSolutionDirectory();
            if (solutionDir == null)
            {
                AnsiConsole.MarkupLine("[red]Project directory not found.[/]");
                return 1;
            }

            var dataDir = Path.Combine(solutionDir, "src", "IntelOrca.Biohazard.BioRand.RE4R", "data");

            var dynamicData = new DynamicData(download: true);
            foreach (var dataName in Enum.GetValues<DynamicDataName>())
            {
                var filename = dynamicData.GetFileName(dataName)!;

                var destinationPath = Path.Combine(dataDir, filename);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                try
                {
                    var fileBytes = dynamicData.GetData(dataName)!;

                    await File.WriteAllBytesAsync(destinationPath, fileBytes);
                    AnsiConsole.MarkupLineInterpolated($"[green]Downloaded and overwrote: {destinationPath} ({fileBytes.Length} bytes)[/]");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Failed to update {filename}: {ex.Message}[/]");
                }
            }

            return 0;
        }

        private static string? FindSolutionDirectory()
        {
            var dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "biorand-re4r.sln")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
