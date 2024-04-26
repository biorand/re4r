using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Net;
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
            var url = $"http://*:{settings.Port}/";
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
            EndPointManager.UseIpv6 = false;

            var version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            var randomizerService = new RandomizerService();
            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/api", SerializationCallback, m => m.WithController(() => new MainController(randomizerService)))
                .WithRouting("/", c =>
                {
                    c.OnGet("/", (c, _) => StringContent(c, MimeType.Html, GetString("index.html")));
                    c.OnGet("/re4rr.js", (c, _) => StringContent(c, "text/javascript", GetString("re4rr.js")));
                    c.OnGet("/download", (c, _) => OnDownloadRando(randomizerService, c));
                    c.OnGet("/favicon.ico", (c, _) => BinaryContent(c, "image/x-icon", Resources.favicon));
                    c.OnGet("/version", (c, _) => StringContent(c, MimeType.PlainText, version));
                })
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();
            return server;
        }

        public static async Task SerializationCallback(IHttpContext context, object? data)
        {
            var content = JsonSerializer.SerializeToUtf8Bytes(data,
                new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            context.Response.ContentEncoding = new UTF8Encoding(false);
            context.Response.ContentType = MimeType.Json;
            context.Response.ContentLength64 = content.LongLength;

            using var stream = context.OpenResponseStream(preferCompression: false);
            await stream.WriteAsync(content).ConfigureAwait(false);
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

            var isMod = "true".Equals(context.Request.QueryString["mod"], StringComparison.OrdinalIgnoreCase);
            if (isMod)
            {
                var contentName = $"biorand-re4r-{result.Seed}-mod.zip";
                context.Response.ContentType = "application/zip";
                context.Response.ContentLength64 = result.ModFile.LongLength;
                context.Response.ContentEncoding = null;
                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentName}";
                using var writer = context.OpenResponseStream(preferCompression: false);
                await writer.WriteAsync(result.ModFile);
            }
            else
            {
                var contentName = $"biorand-re4r-{result.Seed}-mod.zip";
                context.Response.ContentType = MimeType.Default;
                context.Response.ContentLength64 = result.ZipFile.LongLength;
                context.Response.ContentEncoding = null;
                context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentName}";
                using var writer = context.OpenResponseStream(preferCompression: false);
                await writer.WriteAsync(result.ZipFile);
            }
        }

        private async Task StringContent(IHttpContext context, string contentType, string content)
        {
            var encoding = new UTF8Encoding(false);
            var contentBytes = encoding.GetBytes(content);

            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = contentBytes.LongLength;
            using var writer = context.OpenResponseStream(preferCompression: false);
            await writer.WriteAsync(contentBytes);
        }

        private async Task BinaryContent(IHttpContext context, string contentType, byte[] content)
        {
            context.Response.ContentType = contentType;
            context.Response.ContentEncoding = null;
            context.Response.ContentLength64 = content.LongLength;
            using var writer = context.OpenResponseStream(preferCompression: false);
            await writer.WriteAsync(content);
        }

        private string GetString(string fileName)
        {
#if DEBUG
            var wwwroot = @"M:\git\re4rr-main\src\biorand-re4r\data\wwwroot";
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
            private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);

            private void ExpireOldRandos()
            {
                var now = DateTime.UtcNow;
                foreach (var kvp in _randos.ToArray())
                {
                    var age = now - kvp.Value.CreatedAt;
                    if (age.TotalHours > 6)
                    {
                        _randos.Remove(kvp.Key);
                    }
                }
            }

            private IChainsawRandomizer GetRandomizer()
            {
                var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
                var randomizer = chainsawRandomizerFactory.Create();
                return randomizer;
            }

            public Task<RandomizerConfigurationDefinition> GetConfigAsync()
            {
                var randomizer = GetRandomizer();
                var enemyClassFactory = randomizer.EnemyClassFactory;
                var configDefinition = RandomizerConfigurationDefinition.Create(enemyClassFactory);
                return Task.FromResult(configDefinition);
            }

            public async Task<GenerateResult> GenerateAsync(int seed, Dictionary<string, object> config)
            {
                await _mutex.WaitAsync();
                try
                {
                    ExpireOldRandos();

                    var biorandConfig = Re4rConfiguration.GetDefault();
                    var randomizer = GetRandomizer();
                    var input = new RandomizerInput
                    {
                        GamePath = biorandConfig.GamePath,
                        Seed = seed,
                        Configuration = config
                    };
                    var output = randomizer.Randomize(input);
                    var outputFile = output.GetOutputZip();
                    var outputFileMod = output.GetOutputMod();
                    var id = (ulong)_random.NextInt64();
                    var result = new GenerateResult(id, seed, outputFile, outputFileMod);
                    _randos[id] = result;
                    return result;
                }
                finally
                {
                    _mutex.Release();
                }
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
            public byte[] ZipFile { get; }
            public byte[] ModFile { get; }
            public DateTime CreatedAt { get; }

            public GenerateResult(ulong id, int seed, byte[] zipFile, byte[] modFile)
            {
                Id = id;
                Seed = seed;
                ZipFile = zipFile;
                ModFile = modFile;
                CreatedAt = DateTime.UtcNow;
            }
        }

        private class MainController : WebApiController
        {
            private readonly RandomizerService _randomizer;

            public MainController(RandomizerService randomizer)
            {
                _randomizer = randomizer;
            }

            [Route(HttpVerbs.Get, "/config")]
            public Task<RandomizerConfigurationDefinition> GetConfigAsync()
            {
                return _randomizer.GetConfigAsync();
            }

            [Route(HttpVerbs.Post, "/generate")]
            public async Task<object> GenerateAsync([MyJsonData] GenerateRequest request)
            {
                var isValidPassword = IsMatchingPassword(request.Password);
                var logLevel = isValidPassword ? LogLevel.Info : LogLevel.Warning;

                var ipAddress = Request.Headers["X-Forwarded-For"];
                if (string.IsNullOrEmpty(ipAddress))
                    ipAddress = Request.RemoteEndPoint.ToString();

                var configJson = JsonSerializer.Serialize(request.Config);
                Logger.Log(
                    $"Generate [{ipAddress}] [{request.Password}] Seed = {request.Seed} Config = {configJson}",
                    typeof(MainController),
                    logLevel);

                if (!isValidPassword)
                {
                    return new
                    {
                        result = "failure",
                        message = "Incorrect password"
                    };
                }

                var result = await _randomizer.GenerateAsync(
                    request.Seed,
                    RandomizerConfigurationDefinition.ProcessConfig(request.Config));
                return new
                {
                    result = "success",
                    seed = result.Seed,
                    downloadUrl = CreateUrl($"/download?id={result.Id}"),
                    downloadUrlMod = CreateUrl($"/download?id={result.Id}&mod=true")
                };
            }

            private bool IsMatchingPassword(string? password)
            {
                try
                {
                    var webConfig = Re4rConfiguration.GetDefault();
                    return webConfig.Passwords != null && webConfig.Passwords.Contains(password);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, typeof(MainController));
                    return false;
                }
            }

            private string CreateUrl(string path)
            {
                return path;
            }
        }
    }

    public class GenerateRequest
    {
        public int Seed { get; set; }
        public string? Password { get; set; }
        public Dictionary<string, object>? Config { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class MyJsonDataAttribute : Attribute, IRequestDataAttribute<WebApiController>
    {
        public async Task<object?> GetRequestDataAsync(WebApiController controller, Type type, string parameterName)
        {
            using var req = controller.HttpContext.OpenRequestText();
            var content = await req.ReadToEndAsync();
            return JsonSerializer.Deserialize(content, type, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
