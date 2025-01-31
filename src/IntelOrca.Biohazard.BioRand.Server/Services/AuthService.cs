using System;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class AuthService(
        IHttpContextAccessor httpContextAccessor,
        DatabaseService db,
        TwitchService twitchService,
        ILogger<AuthService> logger)
    {
        private DateTime _lastTokenCleanUp;

        private void CleanUpTokensIfTimeTo()
        {
            var now = DateTime.UtcNow;
            if (now - _lastTokenCleanUp >= TimeSpan.FromDays(1))
            {
                _lastTokenCleanUp = now;
                _ = db.DeleteExpiredTokens();
            }
        }

        public string? GetAuthToken()
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
                return null;

            var authorization = httpContext.Request.Headers["Authorization"];
            if (authorization.Count >= 1)
            {
                var parts = authorization[0]?.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts?.Length == 2)
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
            await db.UseTokenAsync(token);
        }

        public async Task<ExtendedUserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.Standard)
        {
            var token = GetAuthToken();
            if (token != null)
            {
                var user = await db.GetUserByToken(token);
                if (user != null)
                {
                    CleanUpTokensIfTimeTo();
                    await UpdateAutomatedTagsAsync(user);
                }
                if (user != null)
                {
                    if (minimumRole == UserRoleKind.Administrator && !user.IsAdmin)
                        return null;

                    if (minimumRole == UserRoleKind.Standard && user.IsPending)
                        return null;

                    await UseAuthToken(token);
                    return user;
                }
            }
            return null;
        }

        private async Task UpdateAutomatedTagsAsync(UserDbModel user)
        {
            var utm = await UserTagModifier.CreateAsync(db, user, logger);
            await UpdateTwitchLabelsAsync(utm);
            await UpdateKofiLabelsAsync(utm);
            await utm.ApplyAsync();
        }

        private async Task UpdateTwitchLabelsAsync(UserTagModifier utm)
        {
            if (!twitchService.IsAvailable)
            {
                utm.Remove("re2r:patron/twitch");
                utm.Remove("re4r:patron/twitch");
            }

            if (await twitchService.IsCurrentlyRateLimited(utm.UserId))
                return;

            if (await twitchService.IsSubscribed(utm.UserId, "124329822"))
                utm.Add("re2r:patron/twitch");
            else
                utm.Remove("re2r:patron/twitch");

            if (await twitchService.IsSubscribed(utm.UserId, "91981318"))
                utm.Add("re4r:patron/twitch");
            else
                utm.Remove("re4r:patron/twitch");
        }

        private async Task UpdateKofiLabelsAsync(UserTagModifier utm)
        {
            var dt = DateTime.UtcNow - TimeSpan.FromDays(30);
            var kofis = await db.GetKofiByUserAsync(utm.UserId);
            var games = await db.GetGamesAsync();
            foreach (var g in games)
            {
                var relevantKofis = kofis.Where(x => x.GameId == g.Id).ToArray();
                var totalDonated = kofis.Sum(x => x.Price);
                if (totalDonated >= 15)
                {
                    utm.Add("patron/long");
                }

                var kofiLabel = $"{g.Moniker}:patron/kofi";
                if (relevantKofis.Any(x => x.Timestamp >= dt))
                {
                    utm.Add(kofiLabel);
                }
                else
                {
                    utm.Remove(kofiLabel);
                }
            }
        }

        public async Task<bool> IsAuthorized(UserRoleKind minimumRole = UserRoleKind.Standard)
        {
            var user = await GetAuthorizedUserAsync(minimumRole);
            return user != null;
        }
    }
}
