using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;
using Swan;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class BaseController : WebApiController
    {
        private readonly DatabaseService _db;
        private readonly TwitchService _twitchService;
        private readonly ILogger _logger = Log.ForContext<BaseController>();

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
            var originalFlags = user.Flags;
            var originalRole = user.Role;
            if (originalRole is UserRoleKind.Pending
                             or UserRoleKind.Banned
                             or UserRoleKind.Administrator
                             or UserRoleKind.System)
            {
                return;
            }

            var newRole = originalRole;
            var twitchRole = await GetRoleKindFromTwitchAsync(user);
            var kofiRole = await GetRoleKindFromKofiAsync(user);
            if (twitchRole > newRole)
                newRole = twitchRole;
            if (kofiRole > newRole)
                newRole = kofiRole;

            // Don't downgrade role to no access
            if (originalRole != UserRoleKind.PendingEarlyAccess && newRole == UserRoleKind.PendingEarlyAccess)
                newRole = UserRoleKind.EarlyAccess;

            if (newRole != originalRole)
            {
                user.Role = newRole;
                await _db.UpdateUserAsync(user);
                _logger.Information("Updated user {UserId}[{UserName}] role from {FromRole} to {ToRole}",
                    user.Id, user.Name, originalRole, user.Role);
            }
            else if (user.Flags != originalFlags)
            {
                await _db.UpdateUserAsync(user);
                _logger.Information("Updated user {UserId}[{UserName}] flags", user.Id);
            }
        }

        private async Task<UserRoleKind> GetRoleKindFromTwitchAsync(UserDbModel user)
        {
            if (!_twitchService.IsAvailable)
                return UserRoleKind.PendingEarlyAccess;

            var twitchModel = await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1));
            if (twitchModel?.IsSubscribed == true)
            {
                user.TwitchSubscriber = true;
                return UserRoleKind.Standard;
            }
            else
            {
                user.TwitchSubscriber = false;
                return UserRoleKind.PendingEarlyAccess;
            }
        }

        private async Task<UserRoleKind> GetRoleKindFromKofiAsync(UserDbModel user)
        {
            var kofis = await _db.GetKofiByUserAsync(user.Id);
            var dt = DateTime.UtcNow - TimeSpan.FromDays(30);
            var role = UserRoleKind.PendingEarlyAccess;
            foreach (var kofi in kofis)
            {
                if (kofi.Timestamp >= dt)
                {
                    if (string.Equals(kofi.TierName, "BioRand Patron", StringComparison.OrdinalIgnoreCase))
                    {
                        user.KofiMember = true;
                        return UserRoleKind.Standard;
                    }
                }
                role = UserRoleKind.EarlyAccess;
            }
            user.KofiMember = false;
            return role;
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
