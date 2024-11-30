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

        public async Task<UserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.Standard)
        {
            var token = GetAuthToken();
            if (token != null)
            {
                var user = await db.GetUserByToken(token);
                if (user != null)
                {
                    CleanUpTokensIfTimeTo();
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

        private async Task CheckUserSubscriptionAsync(UserDbModel user)
        {
            var originalFlags = user.Flags;
            var originalRole = user.Role;
            if (originalRole is UserRoleKind.Pending
                             or UserRoleKind.Banned
                             or UserRoleKind.Tester
                             or UserRoleKind.Administrator
                             or UserRoleKind.System)
            {
                return;
            }

            var newRole = originalRole;
            var twitchRole = await GetRoleKindFromTwitchAsync(user);
            var kofiRole = await GetRoleKindFromKofiAsync(user);
            if (IsRoleBetterThan(twitchRole, newRole))
                newRole = twitchRole;
            if (IsRoleBetterThan(kofiRole, newRole))
                newRole = kofiRole;

            if (newRole != originalRole)
            {
                user.Role = newRole;
                await db.UpdateUserAsync(user);
                logger.LogInformation("Updated user {UserId}[{UserName}] role from {FromRole} to {ToRole}",
                    user.Id, user.Name, originalRole, user.Role);
            }
            else if (user.Flags != originalFlags)
            {
                await db.UpdateUserAsync(user);
                logger.LogInformation("Updated user {UserId}[{UserName}] flags", user.Id, user.Name);
            }
        }

        private async Task<UserRoleKind> GetRoleKindFromTwitchAsync(UserDbModel user)
        {
            if (!twitchService.IsAvailable)
                return UserRoleKind.Standard;

            var twitchModel = await twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1));
            if (twitchModel?.IsSubscribed == true)
            {
                user.TwitchSubscriber = true;
                return UserRoleKind.Patron;
            }
            else
            {
                user.TwitchSubscriber = false;
                return UserRoleKind.Standard;
            }
        }

        private async Task<UserRoleKind> GetRoleKindFromKofiAsync(UserDbModel user)
        {
            var kofis = await db.GetKofiByUserAsync(user.Id);
            var dt = DateTime.UtcNow - TimeSpan.FromDays(30);
            var role = UserRoleKind.Standard;
            foreach (var kofi in kofis)
            {
                if (kofi.Timestamp >= dt)
                {
                    if (string.Equals(kofi.TierName, "BioRand Patron", StringComparison.OrdinalIgnoreCase))
                    {
                        user.KofiMember = true;
                        return UserRoleKind.Patron;
                    }
                }
            }
            user.KofiMember = false;

            var totalDonated = kofis.Sum(x => x.Price);
            if (totalDonated >= 15)
            {
                return UserRoleKind.LongTermSupporter;
            }

            return role;
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
    }
}
