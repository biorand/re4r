using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.Net;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
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
            var emailService = new EmailService(Re4rConfiguration.GetDefault().Email);

            await CreateDefaultProfiles(randomizerService, dbService);

            var server = new WebServer(o => o
                .WithUrlPrefix(url)
                .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithCors()
                .WithWebApi("/auth", SerializationCallback, m => m.WithController(() => new AuthController(dbService, emailService)))
                .WithWebApi("/profile", SerializationCallback, m => m.WithController(() => new ProfileController(dbService)))
                .WithWebApi("/rando", SerializationCallback, m => m.WithController(() => new RandoController(dbService, randomizerService)))
                .WithWebApi("/user", SerializationCallback, m => m.WithController(() => new UserController(dbService)))
                .WithWebApi("/", SerializationCallback, m => m.WithController(() => new MainController(dbService, randomizerService)))
                .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Error" })));

            // Listen for state changes.
            server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();
            return server;
        }

        private async Task CreateDefaultProfiles(RandomizerService randomizerService, DatabaseService dbService)
        {
            // Default profile
            var randomizerConfig = await randomizerService.GetConfigAsync();
            var defaultConfig = randomizerConfig.GetDefault();

            var profile = await dbService.GetDefaultProfile();
            if (profile == null)
            {
                var newProfile = new ProfileDbModel()
                {
                    Created = DateTime.UtcNow,
                    Name = "Default",
                    Description = "The default profile.",
                    Public = true
                };
                await dbService.CreateProfileAsync(newProfile, defaultConfig);
            }
            else
            {
                profile.Description = "The default profile.";
                profile.Public = true;
                await dbService.UpdateProfileAsync(profile);
                await dbService.SetProfileConfigAsync(profile.Id, defaultConfig);
            }
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
