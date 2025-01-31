using System;
using System.Net;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class TwitchService
    {
        private readonly DatabaseService _databaseService;
        private readonly TwitchConfig? _twitchConfig;
        private readonly ILogger _logger;

        public TwitchService(DatabaseService databaseService, BioRandServerConfiguration config, ILogger<TwitchService> logger)
        {
            _databaseService = databaseService;
            _twitchConfig = config.Twitch;
            _logger = logger;
        }

        private TwitchConfig GetConfig()
        {
            if (_twitchConfig == null)
                throw new InvalidOperationException("Twitch credentials not set up.");
            return _twitchConfig;
        }

        public bool IsAvailable => _twitchConfig != null;

        public async Task ConnectAsync(int userId, string code, string redirectUri)
        {
            var config = GetConfig();
            try
            {
                var api = GetApi();
                var response = await api.Auth.GetAccessTokenFromCodeAsync(code, config.ClientSecret, redirectUri);
                var twitchModel = await RefreshAsync(userId, response.AccessToken, response.RefreshToken);
                _logger.LogInformation("Connected twitch for user {UserId} to {TwitchId}", userId, twitchModel.TwitchDisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect twitch for user {UserId}", userId);
                throw;
            }
        }

        public async Task DisconnectAsync(int userId)
        {
            await _databaseService.DeleteUserTwitchAsync(userId);
            _logger.LogInformation("Disconnected twitch for user {UserId}", userId);
        }

        public async Task<bool> IsCurrentlyRateLimited(int userId, TimeSpan? limit = null)
        {
            var twitchModel = await _databaseService.GetUserTwitchAsync(userId);
            if (twitchModel == null)
                return false;

            limit ??= TimeSpan.FromMinutes(1);
            if (DateTime.UtcNow - twitchModel.LastUpdated < limit)
                return true;

            return false;
        }

        public async Task<bool> IsSubscribed(int userId, string broadcasterId)
        {
            var twitchModel = await GetOrRefreshAsync(userId, TimeSpan.FromMinutes(1));
            if (twitchModel == null)
                return false;

            return await IsSubscribedAsync(twitchModel.AccessToken, twitchModel.TwitchId, broadcasterId);
        }

        public async Task<TwitchDbModel?> GetOrRefreshAsync(int userId, TimeSpan? refresh)
        {
            var twitchModel = await _databaseService.GetUserTwitchAsync(userId);
            if (twitchModel == null)
                return null;

            if (refresh.HasValue)
            {
                if (twitchModel.LastUpdated < DateTime.UtcNow - refresh)
                {
                    try
                    {
                        return await RefreshAsync(userId, twitchModel.AccessToken, twitchModel.RefreshToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to refresh Twitch information for user {UserId}", userId);
                        if (twitchModel.LastUpdated < DateTime.UtcNow - TimeSpan.FromDays(5))
                        {
                            await DisconnectAsync(userId);
                            _logger.LogWarning("Twitch information more than 5 days out of date. Disconnecting Twitch for User {UserId}", userId);
                        }
                    }
                }
            }
            return twitchModel;
        }

        private async Task<TwitchDbModel> RefreshAsync(int userId, string accessToken, string refreshToken)
        {
            var config = GetConfig();
            var api = GetApi();
            ValidateAccessTokenResponse? validation = null;
            try
            {
                validation = await api.Auth.ValidateAccessTokenAsync(accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate access token for user {UserId}", userId);
            }
            if (validation == null)
            {
                try
                {
                    var refreshResponse = await api.Auth.RefreshAuthTokenAsync(refreshToken, config.ClientSecret, config.ClientId);
                    accessToken = refreshResponse.AccessToken;
                    refreshToken = refreshResponse.RefreshToken;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to refresh access token for user {UserId}", userId);
                    throw;
                }
            }

            var twitchUser = await GetUserInfoAsync(accessToken);
            var twitchModel = await _databaseService.AddOrUpdateUserTwitchAsync(userId, new TwitchDbModel()
            {
                LastUpdated = DateTime.UtcNow,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TwitchId = twitchUser.Id,
                TwitchDisplayName = twitchUser.DisplayName,
                TwitchProfileImageUrl = twitchUser.ProfileImageUrl
            });
            _logger.LogInformation("Refreshed twitch info for user {UserId} under twitch name {TwitchDisplayName}", userId, twitchModel.TwitchDisplayName);
            return twitchModel;
        }

        private async Task<User> GetUserInfoAsync(string accessToken)
        {
            var api = GetApi(accessToken);
            var twitchUsers = await api.Helix.Users.GetUsersAsync();
            var twitchUser = twitchUsers!.Users[0];
            return twitchUser;
        }

        private async Task<bool> IsSubscribedAsync(string accessToken, string userId, string broadcasterId)
        {
            try
            {
                var config = GetConfig();
                var api = GetApi(accessToken);
                var response = await api.Helix.Subscriptions.CheckUserSubscriptionAsync(broadcasterId, userId);
                return response.Data.Length != 0;
            }
            catch (BadResourceException ex) when (ex.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check subscription for twitch user {UserId}, broardcaster {BroadcasterId}", userId, broadcasterId);
                return false;
            }
        }

        private TwitchAPI GetApi(string? accessToken = null)
        {
            var config = GetConfig();
            var api = new TwitchAPI();
            api.Settings.ClientId = config.ClientId;
            api.Settings.Secret = config.ClientSecret;
            api.Settings.AccessToken = accessToken;
            return api;
        }
    }
}
