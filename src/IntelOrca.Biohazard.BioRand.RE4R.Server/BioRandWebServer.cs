using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Net;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan.Logging;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server
{
    public class BioRandWebServer : IDisposable
    {
        public void Dispose()
        {
        }

        public async Task RunAsync(string url)
        {
            using var server = await CreateWebServer(url);
            await server.RunAsync();
        }

        private async Task<WebServer> CreateWebServer(string url)
        {
            EndPointManager.UseIpv6 = false;

            var version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            var randomizerService = new RandomizerService();
            var dbService = await DatabaseService.CreateDefault();
            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithCors()
                .WithWebApi("/api/auth", SerializationCallback, m => m.WithController(() => new AuthController(dbService)))
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
            var exeLocation = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            var solutionLocation = Path.GetFullPath(Path.Combine(exeLocation, "..", "..", "..", "..", ".."));
            var wwwroot = Path.Combine(solutionLocation, "src", "IntelOrca.Biohazard.BioRand.RE4R.Server", "wwwroot");
            var path = Path.Combine(wwwroot, fileName);
            return File.ReadAllText(path);
#else
            return Resources.ResourceManager.GetString(Path.GetFileNameWithoutExtension(fileName)) ?? "";
#endif
        }
    }
}
