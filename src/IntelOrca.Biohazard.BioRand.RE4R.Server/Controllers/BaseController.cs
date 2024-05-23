﻿using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class BaseController : WebApiController
    {
        private readonly DatabaseService _db;
        private readonly TwitchService _twitchService;

        public BaseController(DatabaseService db, TwitchService twitchService)
        {
            _db = db;
            _twitchService = twitchService;
        }

        protected string? GetAuthToken()
        {
            var authorization = HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization))
            {
                var parts = authorization.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var type = parts[0];
                    var token = parts[1];
                    if (type == "Bearer")
                    {
                        return token;
                    }
                }
            }
            return null;
        }

        private async Task UseAuthToken(string token)
        {
            await _db.UseTokenAsync(token);
        }

        protected async Task<UserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.EarlyAccess)
        {
            var token = GetAuthToken();
            if (token != null)
            {
                var user = await _db.GetUserByToken(token);
                if (user != null)
                {
                    await CheckUserSubscriptionAsync(user);
                }
                if (user != null && user.Role >= minimumRole)
                {
                    await UseAuthToken(token);
                    return user;
                }
            }
            return null;
        }

        protected async Task CheckUserSubscriptionAsync(UserDbModel user)
        {
            if (!_twitchService.IsAvailable)
                return;

            if (user.TwitchId == null)
            {
                if (user.Role == UserRoleKind.Standard)
                {
                    user.Role = UserRoleKind.EarlyAccess;
                    await _db.UpdateUserAsync(user);
                }
            }
            else if (user.Role == UserRoleKind.PendingEarlyAccess)
            {
                var twitchModel = await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1));
                if (twitchModel?.IsSubscribed == true)
                {
                    user.Role = UserRoleKind.Standard;
                    await _db.UpdateUserAsync(user);
                }
            }
            else if (user.Role == UserRoleKind.EarlyAccess)
            {
                var twitchModel = await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(5));
                if (twitchModel?.IsSubscribed == true)
                {
                    user.Role = UserRoleKind.Standard;
                    await _db.UpdateUserAsync(user);
                }
            }
            else if (user.Role == UserRoleKind.Standard)
            {
                var twitchModel = await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromDays(7));
                if (twitchModel?.IsSubscribed != true)
                {
                    user.Role = UserRoleKind.EarlyAccess;
                    await _db.UpdateUserAsync(user);
                }
            }
        }

        protected object EmptyResult()
        {
            return new object { };
        }

        protected object ErrorResult(HttpStatusCode code)
        {
            Response.StatusCode = (int)code;
            return EmptyResult();
        }

        protected object NotFoundResult() => ErrorResult(HttpStatusCode.NotFound);
        protected object UnauthorizedResult() => ErrorResult(HttpStatusCode.Unauthorized);
        protected object ForbiddenResult() => ErrorResult(HttpStatusCode.Forbidden);

        protected object Failure(HttpStatusCode statusCode, string message)
        {
            Response.StatusCode = (int)statusCode;
            return new
            {
                success = false,
                message
            };
        }

        protected object GetUser(UserDbModel user) => GetUser(user, null);
        protected object GetUser(UserDbModel user, TwitchDbModel? twitchModel)
        {
            return new
            {
                user.Id,
                user.Name,
                Created = user.Created.ToUnixEpochDate(),
                user.Email,
                user.Role,
                AvatarUrl = twitchModel == null ? GetAvatarUrl(user.Email) : twitchModel.TwitchProfileImageUrl,
                user.ShareHistory,
                twitch = twitchModel == null ? null : new
                {
                    DisplayName = twitchModel.TwitchDisplayName,
                    ProfileImageUrl = twitchModel.TwitchProfileImageUrl,
                    IsSubscribed = twitchModel.IsSubscribed,
                }
            };
        }

        protected static string GetAvatarUrl(string email)
        {
            var inputBytes = Encoding.ASCII.GetBytes(email.ToLower());
            var hashBytes = SHA256.HashData(inputBytes);
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"https://www.gravatar.com/avatar/{hashString}";
        }

        protected object ResultListResult<TResult, TMapped>(
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
    }
}
