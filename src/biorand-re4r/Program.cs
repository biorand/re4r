using System.Text;
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
            });
            return app.Run(args);
        }

        private static int DebugCode()
        {
            var paths = File.ReadAllLines(@"C:\Users\Ted\Downloads\RE4_PC_Release.list.txt");

            var sb = new StringBuilder();
            // var pak = new PatchedPakFile(@"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4\re_chunk_000.pak");
            var newPak = new PakFileBuilder();
            foreach (var path in paths)
            {
                if (!path.Contains("leveldesign"))
                    continue;
                if (!path.StartsWith("natives/stm/_chainsaw"))
                    continue;
                if (!path.EndsWith(".scn.20"))
                    continue;
                if (path.Contains("item"))
                    continue;
                if (path.Contains("appsystem"))
                    continue;
                if (path.Contains("conv"))
                    continue;
                if (path.Contains("think"))
                    continue;
                if (path.Contains("player"))
                    continue;
                if (path.Contains(".user."))
                    continue;

                // var file = pak.GetFileData(path);
                // newPak.AddEntry(path, file!);
                sb.AppendLine(path);
            }
            var s = sb.ToString();
            newPak.Save(@"G:\re4r\extract\custom.pak", PakFlags.ZSTD);
            return 0;
        }
    }
}
