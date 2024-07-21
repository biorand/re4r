using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("rando")]
    public class RandoController(
        AuthService authService,
        DatabaseService db,
        RandomizerService randomizerService,
        UrlService urlService,
        ILogger<RandoController> logger) : ControllerBase
    {
        [HttpPost("generate")]
        public async Task<object> GenerateAsync([FromBody] GenerateRequest request)
        {
            var user = await authService.GetAuthorizedUserAsync();
            if (user == null)
                return Unauthorized();

            var profile = await db.GetProfileAsync(request.ProfileId, user.Id);
            if (profile == null)
                return NotFound();

            var randomizer = randomizerService.GetRandomizer();
            var config = RandomizerConfiguration.FromDictionary(request.Config ?? []);
            var configJson = config.ToJson(indented: false);

            var randoConfig = await db.GetOrCreateRandoConfig(request.ProfileId, configJson);
            var rando = await db.CreateRando(new RandoDbModel()
            {
                Created = DateTime.UtcNow,
                Version = randomizer.BuildVersion,
                Seed = request.Seed,
                UserId = user.Id,
                ConfigId = randoConfig.Id
            });
            await db.UpdateSeedCount(request.ProfileId);

            logger.LogInformation("User [{UserId}]{UserName} generatating rando ProfileId = {ProfileId} ProfileName = {ProfileName} Seed = {Seed}",
                user.Id, user.Name, request.ProfileId, profile.Name, request.Seed);
            var result = await randomizerService.GenerateAsync(
                (ulong)rando.Id,
                profile.Name,
                profile.Description,
                profile.UserName,
                request.Seed,
                config);
            logger.LogInformation("User [{UserId}]{UserName} generated rando {RandoId} ProfileId = {ProfileId} Seed = {Seed}",
                user.Id, user.Name, result.Id, request.ProfileId, result.Seed);
            return new
            {
                result = "success",
                id = result.Id,
                seed = result.Seed,
                downloadUrl = urlService.GetApiUrl($"rando/{result.Id}/download"),
                downloadUrlMod = urlService.GetApiUrl($"rando/{result.Id}/download?mod=true")
            };
        }

        [HttpGet("history")]
        public async Task<object> GetHistoryAsync(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? user = null,
            [FromQuery] int page = 1)
        {
            var authorizedUser = await authService.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var viewerUserId = authorizedUser.Role < UserRoleKind.Administrator
                ? authorizedUser.Id
                : (int?)null;
            int? filterUserId = null;
            if (user != null)
            {
                var filterUser = await db.GetUserAsync(user);
                if (filterUser == null)
                    return NotFound();

                filterUserId = filterUser.Id;
            }

            if (page < 1)
                page = 1;

            if (sort == null)
            {
                sort = "Created";
                order = "desc";
            }

            var itemsPerPage = 25;
            var randos = await db.GetRandosAsync(
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
                Created = rando.Created.ToUnixTimeSeconds(),
                rando.Version,
                rando.ProfileId,
                rando.ProfileName,
                rando.ProfileUserId,
                rando.ProfileUserName,
                rando.Seed,
                rando.Config
            };
        }

        [HttpGet("stats")]
        public async Task<object> GetStatsAsync()
        {
            return new
            {
                RandoCount = await db.CountRandos(),
                ProfileCount = await db.CountProfiles(),
                UserCount = await db.CountUsers(),
            };
        }

        [HttpGet("{randoId}/download")]
        public object Download(long randoId, [FromQuery] bool mod)
        {
            var result = randomizerService.Find((ulong)randoId);
            if (result == null)
            {
                return Unauthorized();
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

            return File(contentData, MimeTypes.GetMimeType(contentName), contentName);
        }

        private static string GetAvatarUrl(string email)
        {
            var inputBytes = Encoding.ASCII.GetBytes(email.ToLower());
            var hashBytes = SHA256.HashData(inputBytes);
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"https://www.gravatar.com/avatar/{hashString}";
        }

        private object ResultListResult<TResult, TMapped>(
            int page,
            int itemsPerPage,
            LimitedResult<TResult> result,
            Func<TResult, TMapped> selector)
        {
            return new
            {
                Page = page,
                PageCount = (result.Total + itemsPerPage - 1) / itemsPerPage,
                TotalResults = result.Total,
                PageStart = result.From,
                PageEnd = result.To,
                PageResults = result.Results.Select(selector).ToArray()
            };
        }

        public class GenerateRequest
        {
            public int Seed { get; set; }
            public int ProfileId { get; set; }
            public Dictionary<string, object>? Config { get; set; }
        }
    }
}
