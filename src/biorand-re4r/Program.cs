using IntelOrca.Biohazard.BioRand.RE4R.Commands;
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
                config.AddCommand<WebServerCommand>("web-server")
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
}
