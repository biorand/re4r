﻿using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
            using var text = context.OpenResponseText(new UTF8Encoding(false));
            await text.WriteAsync(JsonSerializer.Serialize(data,
                new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })).ConfigureAwait(false);
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
                    var outputFile = output.GetOutputPakFile();
                    var id = (ulong)_random.NextInt64();
                    var result = new GenerateResult(id, seed, outputFile);
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
            public byte[] PakFile { get; }
            public DateTime CreatedAt { get; }

            public GenerateResult(ulong id, int seed, byte[] pakFile)
            {
                Id = id;
                PakFile = pakFile;
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
                if (request.Password != "test")
                {
                    return new
                    {
                        result = "failure",
                        message = "Incorrect password"
                    };
                }

                var result = await _randomizer.GenerateAsync(request.Seed, ProcessConfig(request.Config));
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

            private Dictionary<string, object> ProcessConfig(Dictionary<string, object>? config)
            {
                var result = new Dictionary<string, object>();
                if (config != null)
                {
                    foreach (var kvp in config)
                    {
                        var value = ProcessConfigValue(kvp.Value);
                        if (value is not null)
                            result[kvp.Key] = value;
                    }
                }
                return result;
            }

            private object? ProcessConfigValue(object? value)
            {
                if (value is JsonElement element)
                {
                    return element.ValueKind switch
                    {
                        JsonValueKind.Null => null,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Number => ProcessNumber(element.GetDouble()),
                        JsonValueKind.String => element.GetString(),
                        _ => null
                    };
                }
                return value;
            }

            private object? ProcessNumber(double d)
            {
                var l = (long)d;
                if (l == d)
                {
                    int i = (int)l;
                    return i == l ? i : (object)l;
                }
                return d;
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
