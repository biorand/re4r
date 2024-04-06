using System.ComponentModel;
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
            var randomizerService = new RandomizerService();
            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/api", m => m.WithController(() => new MainController(randomizerService)))
                .WithRouting("/", c =>
                {
                    c.OnGet("/", (c, _) => StringContent(c, MimeType.Html, GetString("index.html")));
                    c.OnGet("/download", (c, _) => OnDownloadRando(randomizerService, c));
                    c.OnGet("/favicon.ico", (c, _) => BinaryContent(c, "image/x-icon", Resources.favicon));
                    c.OnGet("/version", (c, _) => StringContent(c, MimeType.PlainText, version));
                })
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();
            return server;
        }

        private async Task OnDownloadRando(RandomizerService randomizerService, IHttpContext context)
        {
            ulong.TryParse(context.Request.QueryString["id"], out var id);
            var result = randomizerService.Find(id);
            if (result == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var contentName = "re_chunk_000.pak.patch_004.pak";
            context.Response.ContentType = MimeType.Default;
            context.Response.ContentEncoding = null;
            context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentName}";
            using var writer = context.OpenResponseStream();
            await writer.WriteAsync(result.PakFile);
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

        private class RandomizerService
        {
            private readonly Random _random = new Random();
            private readonly Dictionary<ulong, GenerateResult> _randos = new();

            public Task<GenerateResult> GenerateAsync(int seed)
            {
                var biorandConfig = Re4rConfiguration.GetDefault();
                var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
                var randomizer = chainsawRandomizerFactory.Create();
                var input = new RandomizerInput();
                input.GamePath = biorandConfig.GamePath;
                var output = randomizer.Randomize(input);
                var outputFile = output.GetOutputPakFile();
                var id = (ulong)_random.NextInt64();
                var result = new GenerateResult(id, seed, outputFile);
                _randos[id] = result;
                return Task.FromResult(result);
            }

            public GenerateResult? Find(ulong id)
            {
                _randos.TryGetValue(id, out var result);
                return result;
            }
        }

        private class GenerateResult
        {
            public ulong Id { get; }
            public int Seed { get; }
            public byte[] PakFile { get; }

            public GenerateResult(ulong id, int seed, byte[] pakFile)
            {
                Id = id;
                PakFile = pakFile;
            }
        }

        private class MainController : WebApiController
        {
            private readonly RandomizerService _randomizer;

            public MainController(RandomizerService randomizer)
            {
                _randomizer = randomizer;
            }

            [Route(HttpVerbs.Get, "/generate")]
            public async Task<object> GenerateAsync([QueryField] int seed)
            {
                var result = await _randomizer.GenerateAsync(seed);
                return new
                {
                    result = "success",
                    seed = result.Seed,
                    downloadUrl = CreateUrl($"/download?id={result.Id}"),
                    downloadUrlMod = CreateUrl($"/download?id={result.Id}&mod=true")
                };
            }

            private string CreateUrl(string path)
            {
                var authority = Request.Url.GetLeftPart(UriPartial.Authority) ?? "/";
                return new Uri(new Uri(authority), path).AbsoluteUri;
            }
        }
    }
}
