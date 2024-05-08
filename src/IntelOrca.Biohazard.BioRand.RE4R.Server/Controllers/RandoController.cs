using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;
using Swan;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class RandoController : BaseController
    {
        private readonly DatabaseService _db;
        private readonly RandomizerService _randomizer;
        private readonly UrlService _urlService;
        private readonly ILogger _logger;

        public RandoController(DatabaseService db, RandomizerService randomizer, UrlService urlService) : base(db)
        {
            _db = db;
            _randomizer = randomizer;
            _urlService = urlService;
            _logger = Log.ForContext<RandoController>();
        }

        [Route(HttpVerbs.Post, "/generate")]
        public async Task<object> GenerateAsync([MyJsonData] GenerateRequest request)
        {
            var user = await GetAuthorizedUserAsync();
            if (user == null)
                return UnauthorizedResult();

            var config = RandomizerConfigurationDefinition.ProcessConfig(request.Config);
            var configJson = config.ToJson(indented: false);

            var randoConfig = await _db.GetOrCreateRandoConfig(request.ProfileId, configJson);
            var rando = await _db.CreateRando(new RandoDbModel()
            {
                Created = DateTime.UtcNow,
                Version = ChainsawRandomizerFactory.Default.GitHash,
                Seed = request.Seed,
                UserId = user.Id,
                ConfigId = randoConfig.Id
            });
            await _db.UpdateSeedCount(request.ProfileId);

            _logger.Information("User [{UserId}]{UserName} generatating rando ProfileId = {ProfileId} Seed = {Seed}",
                user.Id, user.Name, request.ProfileId, request.Seed);
            var result = await _randomizer.GenerateAsync((ulong)rando.Id, request.Seed, config);
            _logger.Information("User [{UserId}]{UserName} generated rando {RandoId} ProfileId = {ProfileId} Seed = {Seed}",
                user.Id, user.Name, result.Id, request.ProfileId, result.Seed);
            return new
            {
                result = "success",
                id = result.Id,
                seed = result.Seed,
                downloadUrl = _urlService.GetApiUrl($"rando/{result.Id}/download"),
                downloadUrlMod = _urlService.GetApiUrl($"rando/{result.Id}/download?mod=true")
            };
        }

        [Route(HttpVerbs.Get, "/history")]
        public async Task<object> GetHistoryAsync(
            [QueryField] string? sort = null,
            [QueryField] string? order = null,
            [QueryField] string? user = null,
            [QueryField] int page = 1)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var viewerUserId = authorizedUser.Role < UserRoleKind.Administrator
                ? authorizedUser.Id
                : (int?)null;
            int? filterUserId = null;
            if (user != null)
            {
                var filterUser = await _db.GetUserAsync(user);
                if (filterUser == null)
                    return NotFoundResult();

                filterUserId = filterUser.Id;
            }

            if (page < 1)
                page = 1;

            var itemsPerPage = 25;
            var randos = await _db.GetRandosAsync(
                filterUserId,
                viewerUserId,
                SortOptions.FromQuery(sort, order, "Created"),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult(page, itemsPerPage, randos, GetRando);
        }

        private object GetRando(DatabaseService.ExtendedRandoDbModel rando)
        {
            return new
            {
                rando.Id,
                rando.UserId,
                rando.UserName,
                UserAvatarUrl = GetAvatarUrl(rando.UserEmail ?? ""),
                Created = rando.Created.ToUnixEpochDate(),
                rando.Version,
                rando.ProfileId,
                rando.ProfileName,
                rando.ProfileUserId,
                rando.ProfileUserName,
                rando.Seed,
                rando.Config
            };
        }

        [Route(HttpVerbs.Get, "/stats")]
        public async Task<object> GetStatsAsync()
        {
            return new
            {
                RandoCount = await _db.CountRandos(),
                ProfileCount = await _db.CountProfiles(),
                UserCount = await _db.CountUsers(),
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
            Response.ContentLength64 = contentData.LongLength;
            Response.ContentEncoding = null;
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{contentName}";
            await Response.OutputStream.WriteAsync(contentData);
        }

        public class GenerateRequest
        {
            public int Seed { get; set; }
            public int ProfileId { get; set; }
            public Dictionary<string, object>? Config { get; set; }
        }
    }
}
