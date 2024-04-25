using System.ComponentModel;
using System.Diagnostics;
using IntelOrca.Biohazard.BioRand.RE4R.Server;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class WebServerCommand : AsyncCommand<WebServerCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Port to host the web server on")]
            [CommandOption("-p|--port")]
            [DefaultValue(10285)]
            public int Port { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            return base.Validate(context, settings);
        }

        public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var url = $"http://*:{settings.Port}/";

            using var webServer = new BioRandWebServer();
            await webServer.RunAsync(url);

            var browser = new Process()
            {
                StartInfo = new ProcessStartInfo(url) { UseShellExecute = true }
            };
            browser.Start();
            Console.ReadKey(true);
            return 0;
        }
    }
}
