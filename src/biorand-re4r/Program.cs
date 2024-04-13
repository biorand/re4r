using System.Text;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.RE4R.Commands;
using REE;
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
                config.Settings.ApplicationName = "biorand-re4r";
                config.AddCommand<WebServerCommand>("web-server")
                    .WithDescription("Runs a local web server for generating randos")
                    .WithExample("web-server", "-p", "8080");
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
            });
            return app.Run(args);
        }

        private static int DebugCode()
        {
            var pakList = ChainsawRandomizerFactory.Default.GetDefaultPakList();
            var sb = new StringBuilder();
            var pak = new PatchedPakFile(@"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4\re_chunk_000.pak");
            var newPak = new PakFileBuilder();

            var includeList = new[] {
                @"natives/stm/_chainsaw/appsystem/ui/.*",
                @"natives/stm/_chainsaw/environment/scene/gimmick/.*",
                @"natives/stm/_chainsaw/leveldesign/chapter/.*\.scn\.20",
                @"natives/stm/_chainsaw/leveldesign/location/.*\.scn\.20",
                @"natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/.*"
            }.Select(x => new Regex(x));

            foreach (var path in pakList.Entries)
            {
                if (!includeList.Any(x => x.IsMatch(path)))
                    continue;

                // if (!(path.Contains("environment/scene/gimmick") || path.Contains("leveldesign")))
                //     continue;
                // if (!path.StartsWith("natives/stm/_chainsaw"))
                //     continue;
                // if (!path.EndsWith(".scn.20"))
                //     continue;
                // if (path.Contains("item"))
                //     continue;
                // if (path.Contains("appsystem"))
                //     continue;
                // if (path.Contains("conv"))
                //     continue;
                // if (path.Contains("think"))
                //     continue;
                // if (path.Contains("player"))
                //     continue;
                // if (path.Contains(".user."))
                //     continue;

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
            newPak.Save(@"G:\re4r\extract\custom.pak", PakFlags.ZSTD);
            return 0;
        }
    }
}
