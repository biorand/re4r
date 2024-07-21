using System;
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

        public async Task<UserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.EarlyAccess)
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
                return UserRoleKind.PendingEarlyAccess;

            var twitchModel = await twitchService.GetOrRefreshAsync(user.Id, TimeSpan.FromMinutes(1));
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
            var kofis = await db.GetKofiByUserAsync(user.Id);
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

        public async Task<bool> IsAuthorized(UserRoleKind minimumRole = UserRoleKind.EarlyAccess)
        {
            var user = await GetAuthorizedUserAsync(minimumRole);
            return user != null;
        }
    }
}
