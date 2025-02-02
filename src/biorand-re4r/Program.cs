using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.RE4R.Commands;
using IntelOrca.Biohazard.REE.Package;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            // return DebugCode();
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.Settings.ApplicationName = "biorand-re4r";
                config.Settings.ApplicationVersion = GetVersion();
                config.AddCommand<AgentCommand>("agent")
                    .WithDescription("Runs a remote generator agent for generating randos")
                    .WithExample("agent", "localhost:8080", "-k", "nCF6UaetQJJ053QLwhXqUGR68U85Rcia", "-i", "input.pak");
                config.AddCommand<GenerateCommand>("generate")
                    .WithDescription("Generates a new rando")
                    .WithExample("generate", "-o", "re_chunk_000.pak.patch_004.pak", "--seed", "35825", "--config", "tough.json");
                config.AddCommand<PackCommand>("pack")
                    .WithDescription("Creates a .pak file from the given input files")
                    .WithExample("pack", "-o", "output.pak", "natives");
                config.AddCommand<UnpackCommand>("unpack")
                    .WithDescription("Extracts a .pak file to a directory")
                    .WithExample("unpack", "-o", "output", "input.pak");
                config.AddCommand<DumpCommand>("dump")
                    .WithDescription("Dumps an SCN file to JSON")
                    .WithExample("dump", "input.scn");
                config.AddCommand<MsgCommand>("msg")
                    .WithDescription("Lists strings in an MSG file")
                    .WithExample("msg", "input.msg.22");
            });
            return app.Run(args);
        }

        private static int DebugCode()
        {
            var pakList = Re4rRandomizer.GetDefaultPakList();

            var gamePath = "G:\\re4r\\vanilla";
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
            newPak.Save(@"G:\re4r\extract\custom.pak", CompressionKind.Zstd);
            return 0;
        }

        private static string GetVersion()
        {
            return GetGitHash();
        }

        private static string GetGitHash()
        {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
                return string.Empty;

            var attribute = assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            if (attribute == null)
                return string.Empty;

            var rev = attribute.InformationalVersion;
            var plusIndex = rev.IndexOf('+');
            if (plusIndex != -1)
            {
                return rev.Substring(plusIndex + 1);
            }
            return rev;
        }
    }
}
