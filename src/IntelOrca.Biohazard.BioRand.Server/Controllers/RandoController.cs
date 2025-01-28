﻿using System;
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
        GeneratorService generatorService,
        UrlService urlService,
        ILogger<RandoController> logger) : ControllerBase
    {
        [HttpPost("generate")]
        public async Task<object> GenerateAsync([FromBody] GenerateRequest request)
        {
            var user = await authService.GetAuthorizedUserAsync();
            if (user == null)
                return Unauthorized();

            var game = await db.GetGameByIdAsync(request.GameId);
            if (game == null)
                return BadRequest();

            var profile = await db.GetProfileAsync(request.ProfileId, user.Id);
            if (profile == null)
                return NotFound();

            var config = RandomizerConfiguration.FromDictionary(request.Config ?? []);
            var configJson = config.ToJson(indented: false);

            var unassignedRandos = await db.GetRandosWithStatusAsync(RandoStatus.Unassigned);
            foreach (var r in unassignedRandos.Results)
            {
                if (r.UserId == user.Id)
                {
                    await db.SetRandoStatusAsync(r.Id, RandoStatus.Discarded);
                }
            }

            var randoConfig = await db.GetOrCreateRandoConfig(request.ProfileId, configJson);
            var rando = await db.CreateRando(new RandoDbModel()
            {
                GameId = request.GameId,
                Created = DateTime.UtcNow,
                Version = "",
                Seed = request.Seed,
                UserId = user.Id,
                ConfigId = randoConfig.Id,
                Status = RandoStatus.Unassigned
            });
            await db.UpdateSeedCount(request.ProfileId);
            logger.LogInformation("User [{UserId}]{UserName} requested generation of rando ProfileId = {ProfileId} ProfileName = {ProfileName} Seed = {Seed}",
                user.Id, user.Name, profile.Id, profile.Name, rando.Seed);
            return rando;
        }

        [HttpGet("history")]
        public async Task<object> GetHistoryAsync(
            [FromQuery] string? sort = null,
            [FromQuery] string? order = null,
            [FromQuery] string? user = null,
            [FromQuery] int? game = null,
            [FromQuery] int page = 1)
        {
            var authorizedUser = await authService.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var viewerUserId = authorizedUser.Role != UserRoleKind.Administrator
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
                game,
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
                rando.UserRole,
                rando.UserName,
                UserAvatarUrl = GetAvatarUrl(rando.UserEmail ?? ""),
                Created = rando.Created.ToUnixTimeSeconds(),
                rando.Version,
                rando.ProfileId,
                rando.ProfileName,
                rando.ProfileUserId,
                rando.ProfileUserName,
                rando.Seed,
                rando.Status,
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

        [HttpGet("{randoId}")]
        public async Task<object> GetAsync(int randoId)
        {
            var authorizedUser = await authService.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var rando = await db.GetRandoAsync(randoId);
            if (rando.UserId != authorizedUser.Id)
                return Unauthorized();

            var result = await generatorService.GetResult(rando.Id);
            var instructions = result?.Instructions ?? "";
            var failReason = result?.FailReason ?? "";
            var assets = result?.Assets ?? [];

            return new
            {
                rando.Id,
                rando.Seed,
                rando.Status,
                instructions,
                failReason,
                Assets = assets.Select(x => new
                {
                    x.Key,
                    x.Title,
                    x.Description,
                    x.FileName,
                    DownloadUrl = urlService.GetApiUrl($"rando/{rando.Id}/download/{x.Key}"),
                }).ToArray()
            };
        }

        [HttpGet("{randoId}/download/{key}")]
        public async Task<object> Download(int randoId, string key)
        {
            var result = await generatorService.GetResult(randoId);
            if (result == null)
                return Unauthorized();

            var asset = result.Assets.FirstOrDefault(x => x.Key == key);
            if (asset == null)
                return NotFound();

            var contentName = asset.FileName;
            var contentData = asset.Data;
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
            public int GameId { get; set; }
            public int Seed { get; set; }
            public int ProfileId { get; set; }
            public Dictionary<string, object>? Config { get; set; }
        }
    }
}
