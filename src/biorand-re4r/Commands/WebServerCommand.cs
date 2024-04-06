﻿using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Spectre.Console;
using Spectre.Console.Cli;
using Swan.Logging;

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
            var url = $"http://localhost:{settings.Port}/";
            using var server = CreateWebServer(url);
            await server.RunAsync();

            var browser = new Process()
            {
                StartInfo = new ProcessStartInfo(url) { UseShellExecute = true }
            };
            browser.Start();
            // Wait for any key to be pressed before disposing of our web server.
            // In a service, we'd manage the lifecycle of our web server using
            // something like a BackgroundWorker or a ManualResetEvent.
            Console.ReadKey(true);
            return 0;
        }

        private WebServer CreateWebServer(string url)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithRouting("/", c =>
                {
                    c.OnGet("/", (c, _) => StringContent(c, MimeType.Html, GetString("index.html")));
                    c.OnGet("/favicon.ico", (c, _) => BinaryContent(c, "image/x-icon", Resources.favicon));
                    c.OnGet("/version", (c, _) => StringContent(c, MimeType.PlainText, version));
                })
                // .WithWebApi("/api", m => m.WithController<MainController>())
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }

        private async Task StringContent(IHttpContext context, string contentType, string content)
        {
            context.Response.ContentType = contentType;
            using var writer = context.OpenResponseText();
            await writer.WriteAsync(content);
        }

        private async Task BinaryContent(IHttpContext context, string contentType, byte[] content)
        {
            context.Response.ContentType = contentType;
            context.Response.ContentEncoding = null;
            using var writer = context.OpenResponseStream();
            await writer.WriteAsync(content);
        }

        private string GetString(string fileName)
        {
#if DEBUG
            var wwwroot = @"M:\git\re4rr\src\biorand-re4r\data\wwwroot";
            var path = Path.Combine(wwwroot, fileName);
            return File.ReadAllText(path);
#else
            return Resources.ResourceManager.GetString(Path.GetFileNameWithoutExtension(fileName)) ?? "";
#endif
        }

        private class MainController : WebApiController
        {
            [Route(HttpVerbs.Get, "/ping")]
            public async Task<string> TableTennisAsync()
            {
                await Task.Delay(500);
                return "pong";
            }
        }
    }
}
