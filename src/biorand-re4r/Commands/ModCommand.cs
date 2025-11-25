using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class ModCommand : AsyncCommand<ModCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("-m|--mod")]
            public string[] Mods { get; init; } = [];

            [CommandOption("-i|--input")]
            public string? InputPath { get; init; }

            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; }
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            if (settings.Mods == null || settings.Mods.Length == 0)
            {
                PrintMods();
                return 2;
            }

            var mods = ExportedMods.All;
            foreach (var mod in settings.Mods)
            {
                if (!mods.Any(x => x.Name.Equals(mod, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.Error.WriteLine($"{mod} not found");
                    return 1;
                }
            }

            if (string.IsNullOrEmpty(settings.InputPath))
            {
                Console.Error.WriteLine("Input not specified");
                return 1;
            }

            if (string.IsNullOrEmpty(settings.OutputPath))
            {
                Console.Error.WriteLine("Output location not specified");
                return 1;
            }


            foreach (var mod in settings.Mods)
            {
                var modAttribute = mods.First(x => x.Name.Equals(mod, StringComparison.OrdinalIgnoreCase));
                var modBuilder = ExportedMods.ExportMod(settings.InputPath, modAttribute.Name);
                modBuilder.SavePakFile(Path.Combine(settings.OutputPath, modAttribute.FileName + ".pak"));
                modBuilder.SaveFluffyZipFile(Path.Combine(settings.OutputPath, modAttribute.FileName + ".zip"));
            }

            return 0;
        }

        private void PrintMods()
        {
            Console.WriteLine("Available mods:");
            var exportedMods = ExportedMods.All;
            foreach (var mod in exportedMods)
            {
                Console.WriteLine($"  {mod.Name} {mod.Version} by {mod.Author}");
            }
        }
    }
}
