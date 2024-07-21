using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    public class BaseController : ControllerBase
    {
        private readonly DatabaseService _db;
        private readonly TwitchService _twitchService;
        private readonly ILogger _logger = Log.ForContext<BaseController>();

        public BaseController(DatabaseService db, TwitchService twitchService)
        {
            _db = db;
            _twitchService = twitchService;
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
                Created = user.Created.ToUnixTimeSeconds(),
                user.Email,
                user.Role,
                AvatarUrl = twitchModel == null ? GetAvatarUrl(user.Email) : twitchModel.TwitchProfileImageUrl,
                user.ShareHistory,
                user.KofiEmail,
                KofiEmailVerified = user.KofiEmailVerification == null,
                user.KofiMember,
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
