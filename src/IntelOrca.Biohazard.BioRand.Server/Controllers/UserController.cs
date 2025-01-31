using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("user")]
    public class UserController(
        AuthService auth,
        DatabaseService db,
        EmailService emailService,
        TwitchService twitchService,
        UrlService urlService,
        UserService userService,
        ILogger<UserController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetUsersAsync([FromQuery] string? sort, [FromQuery] string? order, [FromQuery] int page = 1)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (authorizedUser == null)
                return Unauthorized();

            var itemsPerPage = 25;
            var users = await db.GetUsersAsync(
                SortOptions.FromQuery(sort, order, ["name", "created", "role"]),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult.Map(page, itemsPerPage, users, x => userService.GetUser(x));
        }

        [HttpPost("verify")]
        public async Task<object> VerifyAsync([FromBody] UserVerifyRequest request)
        {
            var user = await db.GetUserByKofiEmailToken(request.Token);
            if (user == null)
                return NotFound();

            if (user.KofiEmailTimestamp < DateTime.UtcNow - TimeSpan.FromMinutes(60))
                return NotFound();

            user.KofiEmailTimestamp = null;
            user.KofiEmailVerification = null;
            await db.UpdateUserAsync(user);
            logger.LogInformation("User {UserId}[{UserName}] verified Ko-fi email {Email}", user.Id, user.Name, user.KofiEmail);

            await db.UpdateAllUnmatchedKofiMatchesAsync();

            return Empty;
        }

        [HttpGet("{id}")]
        public async Task<object> GetUserAsync(string id)
        {
            var processedId = (object)id;
            if (int.TryParse(id, out var numericId))
            {
                processedId = numericId;
            }

            var authorizedUser = await auth.GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return Unauthorized();

            if (processedId is int userId)
            {
                if (!authorizedUser.IsAdmin && authorizedUser.Id != userId)
                    return Unauthorized();

                var user = await db.GetUserById(userId);
                if (user == null)
                    return NotFound();

                var twitchModel = twitchService.IsAvailable ? await twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1)) : null;
                return userService.GetUser(user, twitchModel);
            }
            else
            {
                if (!authorizedUser.IsAdmin && !string.Equals(authorizedUser.NameLowerCase, (string)processedId, StringComparison.OrdinalIgnoreCase))
                    return Unauthorized();

                var user = await db.GetUserByName((string)processedId);
                if (user == null)
                    return NotFound();

                var twitchModel = twitchService.IsAvailable ? await twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1)) : null;
                return userService.GetUser(user, twitchModel);
            }
        }

        [HttpPut("{id}")]
        public async Task<object> UpdateUserAsync(int id, [FromBody] UserUpdateRequest request)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return Unauthorized();

            var user = await db.GetUserById(id);
            if (user == null)
                return NotFound();

            if (!authorizedUser.IsAdmin && user.Id != authorizedUser.Id)
                return Unauthorized();

            if (request.TwitchCode != null)
            {
                if (request.TwitchCode == "")
                {
                    await twitchService.DisconnectAsync(user.Id);
                    return new
                    {
                        Success = true
                    };
                }
                else if (!twitchService.IsAvailable)
                {
                    var validationResult = new Dictionary<string, string>();
                    validationResult["twitchCode"] = "Twitch functionality is not available.";
                    return new
                    {
                        Success = false,
                        Validation = validationResult
                    };
                }
                else if (string.IsNullOrEmpty(request.TwitchRedirectUri))
                {
                    return BadRequest();
                }
                else
                {
                    try
                    {
                        await twitchService.ConnectAsync(user.Id, request.TwitchCode, request.TwitchRedirectUri);
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

            if (authorizedUser.IsAdmin)
            {
                user.Name = request.Name ?? user.Name;
                user.NameLowerCase = request.Name?.ToLowerInvariant() ?? user.NameLowerCase;
                user.Email = request.Email?.ToLowerInvariant() ?? user.Email;

                if (request.Tags != null)
                {
                    var utm = await UserTagModifier.CreateAsync(db, user, logger);

                    var tags = request.Tags.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    var unknownTags = tags
                        .Where(x => !utm.IsValid(x))
                        .ToArray();
                    if (unknownTags.Length != 0)
                    {
                        return new
                        {
                            Success = false,
                            Validation = new Dictionary<string, string>
                            {
                                ["tags"] = $"Unknown tag/s: {string.Join(",", unknownTags)}"
                            }
                        };
                    }

                    utm.Set(tags);
                    await utm.ApplyAsync();
                }
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

            await db.UpdateUserAsync(user);
            logger.LogInformation("User [{UserId}]{UserName} updated user {UserId}[{UserName}]",
                authorizedUser.Id, authorizedUser.Name, user.Id, user.Name);

            return new
            {
                Success = true
            };
        }

        [HttpPost("{id}/reverifykofi")]
        public async Task<object> ReverifyKofiAsync(int id)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return Unauthorized();

            var user = await db.GetUserById(id);
            if (user == null)
                return NotFound();

            if (!user.IsAdmin && user.Id != user.Id)
                return Unauthorized();

            if (user.KofiEmailVerification == null)
            {
                logger.LogInformation("User {UserId}[{UserName}] attempted to verify already verified ko-fi email", user.Id, user.Name);
                return BadRequest();
            }

            user.KofiEmailTimestamp = DateTime.UtcNow;
            user.KofiEmailVerification = GetRandomEmailVerificationCode();
            await db.UpdateUserAsync(user);
            logger.LogInformation("User {UserId}[{UserName}] requested new ko-fi email verification", user.Id, user.Name);

            await SendKofiEmailVerification(user);
            return Empty;
        }

        private async Task SendKofiEmailVerification(UserDbModel user)
        {
            var url = urlService.GetWebUrl($"user?action=verifykofi&token={user.KofiEmailVerification}");
            await emailService.SendEmailAsync(user.Name, user.KofiEmail!,
                    "BioRand - Verify Email",
$@"Dear {user.Name},

Please verify your Ko-fi email address by navigating to this link:
{url}

Kind regards,
The BioRand Team");
        }

        private async Task SendAccessGrantedEmailAsync(UserDbModel user)
        {
            await emailService.SendEmailAsync(user.Name, user.Email,
                                "BioRand",
            $@"Dear {user.Name},

We are pleased to inform you that you now have access to BioRand.

You should now be able to sign in and generate randomizers for Resident Evil.

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
            public string[]? Tags { get; set; }
            public bool? ShareHistory { get; set; }
            public string? TwitchCode { get; set; }
            public string? TwitchRedirectUri { get; set; }
            public string? KofiEmail { get; set; }
        }

        public class UserVerifyRequest
        {
            public string? Token { get; set; }
        }
    }
}
