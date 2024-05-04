using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class RandoController : BaseController
    {
        private readonly RandomizerService _randomizer;

        public RandoController(DatabaseService db, RandomizerService randomizer) : base(db)
        {
            _randomizer = randomizer;
        }

        [Route(HttpVerbs.Post, "/generate")]
        public async Task<object> GenerateAsync([MyJsonData] GenerateRequest request)
        {
            var user = await GetAuthorizedUserAsync();
            if (user == null)
                return UnauthorizedResult();

            var result = await _randomizer.GenerateAsync(
                request.Seed,
                RandomizerConfigurationDefinition.ProcessConfig(request.Config));
            return new
            {
                result = "success",
                id = result.Id,
                seed = result.Seed,
                downloadUrl = CreateUrl($"rando/{result.Id}/download"),
                downloadUrlMod = CreateUrl($"rando/{result.Id}/download?mod=true")
            };
        }

        [Route(HttpVerbs.Get, "/{randoId}/download")]
        public async Task DownloadAsync(long randoId, [QueryField] bool mod)
        {
            var result = _randomizer.Find((ulong)randoId);
            if (result == null)
            {
                UnauthorizedResult();
                return;
            }

            string contentName;
            byte[] contentData;
            if (mod)
            {
                contentName = $"biorand-re4r-{result.Seed}-mod.zip";
                contentData = result.ModFile;
            }
            else
            {
                contentName = $"biorand-re4r-{result.Seed}.zip";
                contentData = result.ZipFile;
            }

            Response.ContentType = MimeType.Default;
            Response.ContentLength64 = result.ZipFile.LongLength;
            Response.ContentEncoding = null;
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentName}";
            await Response.OutputStream.WriteAsync(contentData);
        }

        public class GenerateRequest
        {
            public int Seed { get; set; }
            public string? Password { get; set; }
            public Dictionary<string, object>? Config { get; set; }
        }
    }
}
