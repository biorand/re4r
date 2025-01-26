using System;
using System.Collections.Generic;
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

        public async Task<UserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.Standard)
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
                if (user != null && IsRoleEqualOrBetterThan(user.Role, minimumRole))
                {
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
                if (kofis.Any(x => x.Timestamp >= dt))
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

        private static bool IsRoleBetterThan(UserRoleKind a, UserRoleKind b)
        {
            var rankA = Array.IndexOf(g_rolePriority, a);
            var rankB = Array.IndexOf(g_rolePriority, b);
            return rankA > rankB;
        }

        private static bool IsRoleEqualOrBetterThan(UserRoleKind a, UserRoleKind b)
        {
            return a == b || IsRoleBetterThan(a, b);
        }

        private static readonly UserRoleKind[] g_rolePriority = [
            UserRoleKind.Pending,
            UserRoleKind.PendingStandard,
            UserRoleKind.Banned,
            UserRoleKind.Standard,
            UserRoleKind.LongTermSupporter,
            UserRoleKind.Patron,
            UserRoleKind.Tester,
            UserRoleKind.Administrator,
            UserRoleKind.System,
        ];

        private class UserTagModifier
        {
            private readonly DatabaseService _db;
            private readonly Dictionary<string, UserTagDbModel> _cache;
            private readonly UserTagDbModel[] _orig;
            private readonly List<UserTagDbModel> _curr;
            private readonly ILogger _logger;

            public UserDbModel User { get; }
            public int UserId => User.Id;

            public static async Task<UserTagModifier> CreateAsync(DatabaseService db, UserDbModel user, ILogger logger)
            {
                var cache = (await db.GetUserTags()).ToDictionary(x => x.Label);
                var curr = await db.GetUserTagsForUser(user.Id);
                return new UserTagModifier(db, user, cache, curr, logger);
            }

            private UserTagModifier(
                DatabaseService db,
                UserDbModel user,
                Dictionary<string, UserTagDbModel> cache,
                IEnumerable<UserTagDbModel> curr,
                ILogger logger)
            {
                _db = db;
                User = user;
                _cache = cache;
                _orig = [.. curr];
                _curr = [.. curr];
                _logger = logger;
            }

            public async Task ApplyAsync()
            {
                var oldIds = _orig.Select(x => x.Id).Order().ToArray();
                var newIds = _curr.Select(x => x.Id).Order().ToArray();
                if (!oldIds.SequenceEqual(newIds))
                {
                    await _db.UpdateUserTagsForUser(UserId, _curr);

                    var oldTags = string.Join(",", _orig.Select(x => x.Label).Order());
                    var newTags = string.Join(",", _curr.Select(x => x.Label).Order());
                    _logger.LogInformation("Updated user {UserId}[{UserName}] tags from {OldTags} to {NewTags}", User.Id, User.Name, oldTags, newTags);
                }
            }

            public bool Contains(string label)
            {
                var tag = GetTag(label);
                if (tag == null)
                    return false;

                return _curr.Any(x => x.Id == tag.Id);
            }

            public void Add(string label)
            {
                var tag = GetTag(label);
                if (tag != null)
                {
                    if (!_curr.Any(x => x.Id == tag.Id))
                    {
                        _curr.Add(tag);
                    }
                }
            }

            public void Remove(string label)
            {
                var tag = GetTag(label);
                if (tag != null)
                {
                    _curr.RemoveAll(x => x.Id == tag.Id);
                }
            }

            private UserTagDbModel? GetTag(string label)
            {
                _cache.TryGetValue(label, out var result);
                return result;
            }
        }
    }
}
