using System.Text;
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
            var pakList = Re4rRandomizer.GetDefaultPakList();

            var gamePath = settings.InputPath!;
            var corePath = Path.Combine(gamePath, "re_chunk_000.pak");
            var dlcPaths = Directory.GetFiles(Path.Combine(gamePath, "dlc"), "*.pak");

            var sb = new StringBuilder();
            var pak = new PatchedPakFile([.. dlcPaths, corePath]);
            var newPak = new PakFileBuilder();

            var includeList = new[] {
                "natives/stm/_anotherorder/appsystem/character/[0-9a-z]+/userdata/[0-9a-z_]+.user.2",
                "natives/stm/_anotherorder/appsystem/inventory/inventorycatalog/.*",
                "natives/stm/_anotherorder/appsystem/navigation/.*",
                "natives/stm/_anotherorder/appsystem/ui/.*",
                "natives/stm/_anotherorder/appsystem/weapon/.*",
                "natives/stm/_anotherorder/appsystem/weaponcustom/.*",
                "natives/stm/_anotherorder/environment/scene/gimmick/.*",
                "natives/stm/_anotherorder/leveldesign/chapter/.*",
                "natives/stm/_anotherorder/leveldesign/location/.*",
                "natives/stm/_anotherorder/message/mes_main_item/.*",
                "natives/stm/_authoring/appsystem/globalvariables/.*",
                "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_110[12]/.*",
                "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_140[12]/.*",
                "natives/stm/_chainsaw/appsystem/character/[0-9a-z]+/userdata/[0-9a-z_]+.user.2",
                "natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/.*",
                "natives/stm/_chainsaw/appsystem/navigation/.*",
                "natives/stm/_chainsaw/appsystem/shell/bullet/.*",
                "natives/stm/_chainsaw/appsystem/ui/.*",
                "natives/stm/_chainsaw/appsystem/weapon/.*",
                "natives/stm/_chainsaw/appsystem/weaponcustom/.*",
                "natives/stm/_chainsaw/environment/scene/gimmick/.*",
                "natives/stm/_chainsaw/leveldesign/chapter/.*",
                "natives/stm/_chainsaw/leveldesign/location/.*",
                "natives/stm/_chainsaw/leveldesign/scenario/.*",
                "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_110[12].msg.22",
                "natives/stm/_chainsaw/message/mes_main_charm/.*",
                "natives/stm/_chainsaw/message/mes_main_item/.*",
                "natives/stm/_chainsaw/message/mes_main_sys/.*"
            }.Select(x => new Regex(x, RegexOptions.IgnoreCase));

            foreach (var path in pakList.Entries)
            {
                if (!includeList.Any(x => x.IsMatch(path)))
                    continue;

                var file = pak.GetFileData(path);
                if (file == null)
                {
                    Console.WriteLine("X " + path);
                }
                else
                {
                    newPak.AddEntry(path, file);
                    Console.WriteLine("* " + path);
                    sb.AppendLine(path);
                }
            }
            var s = sb.ToString();
            newPak.Save(settings.OutputPath!, CompressionKind.Zstd);
            return Task.FromResult(0);
        }
    }
}
