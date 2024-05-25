using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class UserController : BaseController
    {
        private readonly DatabaseService _db;
        private readonly EmailService _emailService;
        private readonly TwitchService _twitchService;
        private readonly UrlService _urlService;
        private readonly ILogger _logger;

        public UserController(
            DatabaseService db,
            EmailService emailService,
            TwitchService twitchService,
            UrlService urlService) : base(db, twitchService)
        {
            _db = db;
            _emailService = emailService;
            _twitchService = twitchService;
            _urlService = urlService;
            _logger = Log.ForContext<UserController>();
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<object> GetUsersAsync([QueryField] string sort, [QueryField] string order, [QueryField] int page)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (page <= 0)
                page = 1;

            var itemsPerPage = 25;
            var descending = "desc".Equals(order, System.StringComparison.InvariantCultureIgnoreCase);
            var users = await _db.GetUsersAsync(sort, descending, LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult(page, itemsPerPage, users, GetUser);
        }

        [Route(HttpVerbs.Post, "/verify")]
        public async Task<object> VerifyAsync([MyJsonData] UserVerifyRequest request)
        {
            var user = await _db.GetUserByKofiEmailToken(request.Token);
            if (user == null)
                return NotFoundResult();

            if (user.KofiEmailTimestamp < DateTime.UtcNow - TimeSpan.FromMinutes(60))
                return NotFoundResult();

            user.KofiEmailTimestamp = null;
            user.KofiEmailVerification = null;
            await _db.UpdateUserAsync(user);
            _logger.Information("User {UserId}[{UserName}] verified Ko-fi email {Email}", user.Id, user.Name, user.KofiEmail);

            await _db.UpdateAllUnmatchedKofiMatchesAsync();

            return EmptyResult();
        }

        [Route(HttpVerbs.Get, "/{id}")]
        public async Task<object> GetUserAsync(string id)
        {
            var processedId = (object)id;
            if (int.TryParse(id, out var numericId))
            {
                processedId = numericId;
            }

            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (processedId is int userId)
            {
                if (authorizedUser.Role < UserRoleKind.Administrator && authorizedUser.Id != userId)
                    return UnauthorizedResult();

                var user = await _db.GetUserAsync(userId);
                if (user == null)
                    return NotFoundResult();

                var twitchModel = _twitchService.IsAvailable ? await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1)) : null;
                return GetUser(user, twitchModel);
            }
            else
            {
                if (authorizedUser.Role < UserRoleKind.Administrator && !string.Equals(authorizedUser.NameLowerCase, (string)processedId, StringComparison.OrdinalIgnoreCase))
                    return UnauthorizedResult();

                var user = await _db.GetUserAsync((string)processedId);
                if (user == null)
                    return NotFoundResult();

                var twitchModel = _twitchService.IsAvailable ? await _twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1)) : null;
                return GetUser(user, twitchModel);
            }
        }

        [Route(HttpVerbs.Put, "/{id}")]
        public async Task<object> UpdateUserAsync(int id, [MyJsonData] UserUpdateRequest request)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return UnauthorizedResult();

            var user = await _db.GetUserAsync(id);
            if (user == null)
                return NotFoundResult();

            if (authorizedUser.Role < UserRoleKind.Administrator && user.Id != authorizedUser.Id)
                return UnauthorizedResult();

            if (request.TwitchCode != null)
            {
                if (request.TwitchCode == "")
                {
                    await _twitchService.DisconnectAsync(user.Id);
                    return new
                    {
                        Success = true
                    };
                }
                else if (!_twitchService.IsAvailable)
                {
                    var validationResult = new Dictionary<string, string>();
                    validationResult["twitchCode"] = "Twitch functionality is not available.";
                    return new
                    {
                        Success = false,
                        Validation = validationResult
                    };
                }
                else
                {
                    try
                    {
                        await _twitchService.ConnectAsync(user.Id, request.TwitchCode);
                        return new
                        {
                            Success = true
                        };
                    }
                    catch
                    {
                        var validationResult = new Dictionary<string, string>();
                        validationResult["twitchCode"] = "Failed to connect twitch account.";
                        return new
                        {
                            Success = false,
                            Validation = validationResult
                        };
                    }
                }
            }

            var oldRole = user.Role;
            if (authorizedUser.Role >= UserRoleKind.Administrator)
            {
                user.Name = request.Name ?? user.Name;
                user.NameLowerCase = request.Name?.ToLowerInvariant() ?? user.NameLowerCase;
                user.Email = request.Email?.ToLowerInvariant() ?? user.Email;
                user.Role = request.Role ?? user.Role;
            }

            if (request.KofiEmail != null)
            {
                var newKofiEmail = request.KofiEmail.Trim().ToLowerInvariant();
                if (user.KofiEmail != newKofiEmail)
                {
                    if (newKofiEmail == "" || newKofiEmail == user.Email)
                    {
                        user.KofiEmail = null;
                        user.KofiEmailTimestamp = null;
                        user.KofiEmailVerification = null;
                    }
                    else
                    {
                        user.KofiEmail = newKofiEmail;
                        user.KofiEmailTimestamp = DateTime.UtcNow;
                        user.KofiEmailVerification = GetRandomEmailVerificationCode();
                        await SendKofiEmailVerification(user);
                    }
                }
            }

            user.ShareHistory = request.ShareHistory ?? user.ShareHistory;

            await _db.UpdateUserAsync(user);
            _logger.Information("User [{UserId}]{UserName} updated user {UserId}[{UserName}]",
                authorizedUser.Id, authorizedUser.Name, user.Id, user.Name);

            if (oldRole == UserRoleKind.PendingEarlyAccess &&
                user.Role == UserRoleKind.EarlyAccess)
            {
                await _emailService.SendEmailAsync(user.Name, user.Email,
                    "BioRand 4 - Early Access",
$@"Dear {user.Name},

We are pleased to inform you that your request for early access has been approved.

You should now be able to sign in and generate randomizers for Resident Evil 4 (2023).

Kind regards,
The BioRand Team");
            }

            return new
            {
                Success = true
            };
        }

        [Route(HttpVerbs.Post, "/{id}/reverifykofi")]
        public async Task<object> ReverifyKofiAsync(int id)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return UnauthorizedResult();

            var user = await _db.GetUserAsync(id);
            if (user == null)
                return NotFoundResult();

            if (user.Role < UserRoleKind.Administrator && user.Id != user.Id)
                return UnauthorizedResult();

            if (user.KofiEmailVerification == null)
            {
                _logger.Information("User {UserId}[{UserName}] attempted to verify already verified ko-fi email", user.Id, user.Name);
                return ErrorResult(System.Net.HttpStatusCode.BadRequest);
            }

            user.KofiEmailTimestamp = DateTime.UtcNow;
            user.KofiEmailVerification = GetRandomEmailVerificationCode();
            await _db.UpdateUserAsync(user);
            _logger.Information("User {UserId}[{UserName}] requested new ko-fi email verification", user.Id, user.Name);

            await SendKofiEmailVerification(user);
            return EmptyResult();
        }

        private async Task SendKofiEmailVerification(UserDbModel user)
        {
            var url = _urlService.GetWebUrl($"user?action=verifykofi&token={user.KofiEmailVerification}");
            await _emailService.SendEmailAsync(user.Name, user.KofiEmail!,
                    "BioRand 4 - Verify Email",
$@"Dear {user.Name},

Please verify your Ko-fi email address by navigating to this link:
{url}

Kind regards,
The BioRand Team");
        }

        private static string GetRandomEmailVerificationCode()
        {
            var randomBytes = new byte[16];
            RandomNumberGenerator.Fill(randomBytes);
            return BitConverter.ToString(randomBytes).Replace("-", "").ToLower();
        }

        public class UserUpdateRequest
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public UserRoleKind? Role { get; set; }
            public bool? ShareHistory { get; set; }
            public string? TwitchCode { get; set; }
            public string? KofiEmail { get; set; }
        }

        public class UserVerifyRequest
        {
            public string? Token { get; set; }
        }
    }
}
