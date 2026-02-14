using System.Collections.Immutable;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.REE.Package;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class SetupCommand : AsyncCommand<SetupCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("-i|--input")]
            public string? InputPath { get; init; }

            [CommandOption("-o|--output")]
            public string? OutputPath { get; init; }

            [CommandOption("--full")]
            public bool Full { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (settings.InputPath == null)
            {
                return ValidationResult.Error($"Input path not specified");
            }
            if (settings.OutputPath == null)
            {
                return ValidationResult.Error($"Output path not specified");
            }
            return base.Validate(context, settings);
        }

        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var patternList = settings.Full ? FullPatterns : MiniPatterns;
            var gamePath = settings.InputPath!;
            var pak = new RePakCollection(gamePath);

            var outputPath = settings.OutputPath!;
            if (outputPath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            {
                var newPak = new PakFileBuilder();
                HarvestFiles(pak, patternList, (path, data) =>
                {
                    newPak.AddEntry(path, data);
                });
                newPak.Save(settings.OutputPath!, CompressionKind.Zstd);
            }
            else
            {
                HarvestFiles(pak, patternList, (path, data) =>
                {
                    var targetPath = Path.Combine(outputPath, path);
                    var targetDir = Path.GetDirectoryName(targetPath)!;
                    Directory.CreateDirectory(targetDir);
                    File.WriteAllBytes(targetPath, data);
                });
            }
            return Task.FromResult(0);
        }

        private static void HarvestFiles(IPakFile pak, ImmutableArray<string> patternList, Action<string, byte[]> cb)
        {
            var pakList = Re4rRandomizer.GetDefaultPakList();
            var patternListRegex = patternList.Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();
            foreach (var path in pakList.Entries)
            {
                if (!patternListRegex.Any(x => x.IsMatch(path)))
                    continue;

                var file = pak.GetEntryData(path);
                if (file == null)
                {
                    Console.WriteLine("X " + path);
                }
                else
                {
                    cb(path, file);
                    Console.WriteLine("* " + path);
                }
            }
        }

        private static readonly ImmutableArray<string> FullPatterns = [
            @"natives/stm/.*\.gui\.540034",
            @"natives/stm/.*\.motfsm2\.43",
            @"natives/stm/.*\.msg\.22",
            @"natives/stm/.*\.pfb\.17",
            @"natives/stm/.*\.scn\.20",
            @"natives/stm/.*\.user\.2",
            @"natives/stm/.*\.uvar\.3"
        ];

        private static readonly ImmutableArray<string> MiniPatterns = [
            "natives/stm/_anotherorder/appsystem/character/[0-9a-z]+/userdata/[0-9a-z_]+\\.user\\.2",
            "natives/stm/_anotherorder/appsystem/inventory/inventorycatalog/.*",
            "natives/stm/_anotherorder/appsystem/navigation/.*",
            "natives/stm/_anotherorder/appsystem/ui/.*",
            "natives/stm/_anotherorder/appsystem/weapon/.*",
            "natives/stm/_anotherorder/appsystem/weaponcustom/.*",
            "natives/stm/_anotherorder/environment/scene/gimmick/.*",
            "natives/stm/_anotherorder/leveldesign/chapter/.*",
            "natives/stm/_anotherorder/leveldesign/location/.*",
            "natives/stm/_anotherorder/message/mes_main_item/.*",
            "natives/stm/_anotherorder/message/mes_main_sys/.*",
            "natives/stm/_authoring/appsystem/globalvariables/.*",
            "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_110[12]/.*",
            "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_140[12]/.*",
            "natives/stm/_chainsaw/appsystem/character/[0-9a-z]+/userdata/[0-9a-z_]+\\.user\\.2",
            "natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/.*",
            "natives/stm/_chainsaw/appsystem/navigation/.*",
            "natives/stm/_chainsaw/appsystem/prefab/gui/.*",
            "natives/stm/_chainsaw/appsystem/shell/bullet/.*",
            "natives/stm/_chainsaw/appsystem/ui/.*",
            "natives/stm/_chainsaw/appsystem/weapon/.*",
            "natives/stm/_chainsaw/appsystem/weaponcustom/.*",
            "natives/stm/_chainsaw/environment/scene/gimmick/.*",
            "natives/stm/_chainsaw/leveldesign/chapter/.*",
            "natives/stm/_chainsaw/leveldesign/location/.*",
            "natives/stm/_chainsaw/leveldesign/scenario/.*",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_110[12]\\.msg\\.22",
            "natives/stm/_chainsaw/message/mes_main_charm/.*",
            "natives/stm/_chainsaw/message/mes_main_item/.*",
            "natives/stm/_chainsaw/message/mes_main_sys/.*",
            "natives/stm/_chainsaw/sound/resource/trigger/weapon/.*\\.user\\.2",
            "natives/stm/_chainsaw/vfx/provider/epv_character/.*\\.pfb\\.17",
            "natives/stm/_chainsaw/vfx/provider/epv_weapon/.*\\.pfb\\.17",
        ];
    }
}
