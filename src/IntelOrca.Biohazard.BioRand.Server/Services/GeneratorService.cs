using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class GeneratorService : IAsyncDisposable
    {
        private readonly static TimeSpan DefaultDownloadExpireTime = TimeSpan.FromHours(1);
        private readonly TimeSpan DefaultHeartbeatTimeout = TimeSpan.FromMinutes(5);

        private readonly DatabaseService _db;
        private readonly BioRandServerConfiguration _config;
        private readonly ILogger<GeneratorService> _logger;
        private readonly ConcurrentDictionary<Guid, RemoteGenerator> _generators = [];
        private readonly Dictionary<int, GenerateResult> _generatedRandos = [];
        private readonly Dictionary<int, DateTime> _processingRandos = [];
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private Task _cleanupTask;

        public GeneratorService(
            DatabaseService db,
            BioRandServerConfiguration config,
            ILogger<GeneratorService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
            _cleanupTask = RunCleanupTasks(_disposeCts.Token);
        }

        public async ValueTask DisposeAsync()
        {
            _disposeCts.Cancel();
            await _cleanupTask;
        }

        public Task<RemoteGenerator[]> GetAllAsync()
        {
            return Task.FromResult(_generators.Values.ToArray());
        }

        public async Task<GenerateResult[]> GetGeneratedResultsAsync()
        {

            await _mutex.WaitAsync();
            try
            {
                return [.. _generatedRandos.Values];
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<RandomizerConfigurationDefinition> GetConfigDefinitionAsync(int gameId)
        {
            var game = await _db.GetGameByIdAsync(gameId);
            if (game == null || game.ConfigurationDefinition == null)
                return new RandomizerConfigurationDefinition();

            return JsonSerializer.Deserialize<RandomizerConfigurationDefinition>(game.ConfigurationDefinition)!;
        }

        public async Task<RandomizerConfiguration> GetDefaultConfigAsync(int gameId)
        {
            var game = await _db.GetGameByIdAsync(gameId);
            if (game == null || game.DefaultConfiguration == null)
                return new RandomizerConfiguration();

            return RandomizerConfiguration.FromJson(game.DefaultConfiguration);
        }

        public async Task<GenerateResult?> GetResult(int randoId)
        {
            await _mutex.WaitAsync();
            try
            {
                _generatedRandos.TryGetValue(randoId, out var result);
                return result;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<RemoteGenerator> RegisterAsync(
            int gameId,
            RandomizerConfigurationDefinition definition,
            RandomizerConfiguration defaultConfig)
        {
            var game = await _db.GetGameByIdAsync(gameId) ?? throw new ArgumentException("Game not found", nameof(gameId));
            var generator = new RemoteGenerator(gameId);
            if (!_generators.TryAdd(generator.Id, generator))
                throw new Exception("Failed to register generator");

            game.ConfigurationDefinition = definition.ToJson(indented: false);
            game.DefaultConfiguration = defaultConfig.ToJson(indented: false);
            await _db.UpdateGameAsync(game);
            await CreateDefaultProfile(game.Id);
            return generator;
        }

        public Task<bool> UnregisterAsync(Guid id)
        {
            return Task.FromResult(_generators.TryRemove(id, out _));
        }

        public Task<bool> UpdateHeartbeatAsync(Guid id, string status)
        {
            var generator = GetGenerator(id);
            if (generator == null)
                return Task.FromResult(false);

            generator.LastHeartbeatTime = DateTime.UtcNow;
            generator.Status = status;
            return Task.FromResult(true);
        }

        private async Task RunCleanupTasks(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CleanUpGeneratorsAsync();
                    await FailTimedOutRandosAsync();
                    await ExpireOldRandosAsync();
                    await Task.Delay(5000, ct);
                }
                catch
                {
                }
            }
        }

        private Task CleanUpGeneratorsAsync()
        {
            var heartbeatTimeoutSeconds = _config.Generator?.HeartbeatTimeout ?? 0;
            var heartbeatTimeout = heartbeatTimeoutSeconds <= 0
                ? DefaultHeartbeatTimeout
                : TimeSpan.FromSeconds(heartbeatTimeoutSeconds);

            var generators = _generators.Values.ToArray();
            foreach (var g in generators)
            {
                var timeSinceLastHeartbeat = DateTime.UtcNow - g.LastHeartbeatTime;
                if (timeSinceLastHeartbeat > heartbeatTimeout)
                {
                    _generators.TryRemove(g.Id, out _);
                }
            }
            return Task.CompletedTask;
        }

        private async Task FailTimedOutRandosAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                foreach (var kvp in _processingRandos.ToArray())
                {
                    var duration = DateTime.UtcNow - kvp.Value;
                    if (duration > TimeSpan.FromMinutes(2))
                    {
                        await _db.SetRandoStatusAsync(kvp.Key, RandoStatus.Failed);
                        _processingRandos.Remove(kvp.Key);
                    }
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        private async Task ExpireOldRandosAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                var downloadExpireTimeSeconds = (_config.Generator?.RandoExpireTime ?? 0);
                var downloadExpireTime = downloadExpireTimeSeconds <= 0
                    ? DefaultDownloadExpireTime
                    : TimeSpan.FromSeconds(downloadExpireTimeSeconds);

                var result = new List<int>();
                var now = DateTime.UtcNow;
                foreach (var kvp in _generatedRandos.ToArray())
                {
                    var age = now - kvp.Value.CreatedAt;
                    if (age > downloadExpireTime)
                    {
                        _generatedRandos.Remove(kvp.Key);
                        result.Add(kvp.Key);
                    }
                }
                foreach (var id in result)
                {
                    await _db.SetRandoStatusAsync(id, RandoStatus.Expired);
                }
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<GeneratorQueueItem[]> GetQueueAsync()
        {
            var queryResult = await _db.GetRandosWithStatusAsync(RandoStatus.Unassigned);
            var results = queryResult.Results.Select(x => new GeneratorQueueItem()
            {
                Id = x.Id,
                GameId = x.GameId,
                Created = x.Created.ToUnixTimeSeconds(),
                UserId = x.UserId,
                Seed = x.Seed,
                ConfigId = x.ConfigId,
                Status = (int)x.Status,
                UserRole = (int)x.UserRole,
                UserName = x.UserName,
                UserTags = x.UserTags?.Split(',') ?? [],
                ProfileId = x.ProfileId,
                ProfileName = x.ProfileName,
                ProfileDescription = x.ProfileDescription,
                ProfileUserId = x.ProfileUserId,
                ProfileUserName = x.ProfileUserName,
                Config = x.Config,
            }).ToArray();
            return results;
        }

        public Task<bool> IsGeneratorValid(Guid id)
        {
            return Task.FromResult(GetGenerator(id) != null);
        }

        public async Task<bool> ProcessRando(Guid id, int randoId, string version)
        {
            var generator = GetGenerator(id);
            if (generator == null)
                return false;

            await _mutex.WaitAsync();
            try
            {
                var queue = await _db.GetRandosWithStatusAsync(RandoStatus.Unassigned);
                var rando = queue.Results.FirstOrDefault(x => x.Id == randoId);
                if (rando == null)
                    return false;

                if (rando.Status != RandoStatus.Unassigned)
                    return false;

                rando.Version = version;
                rando.Status = RandoStatus.Processing;
                _processingRandos.Add(randoId, DateTime.UtcNow);
                await _db.UpdateRandoAsync(rando);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start processing rando {RandoId}", randoId);
                return false;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> FinishRando(Guid id, int randoId, byte[] pakOutput, byte[] fluffyOutput)
        {
            var generator = GetGenerator(id);
            if (generator == null)
                return false;

            await _mutex.WaitAsync();
            try
            {
                var rando = await _db.GetRandoAsync(randoId);
                if (rando == null)
                    return false;

                if (rando.Status != RandoStatus.Processing)
                    return false;

                var game = await _db.GetGameByIdAsync(rando.GameId);
                if (game == null)
                    return false;

                _processingRandos.Remove(randoId);
                _generatedRandos.Add(randoId, new GenerateResult(randoId, rando.Seed, game.Moniker, pakOutput, fluffyOutput));
                await _db.SetRandoStatusAsync(rando.Id, RandoStatus.Completed);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize rando {RandoId}", randoId);
                return false;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<bool> FailRando(Guid id, int randoId, string reason)
        {
            var generator = GetGenerator(id);
            if (generator == null)
                return false;

            await _mutex.WaitAsync();
            try
            {
                var rando = await _db.GetRandoAsync(randoId);
                if (rando == null)
                    return false;

                if (rando.Status != RandoStatus.Processing)
                    return false;

                await _db.SetRandoStatusAsync(rando.Id, RandoStatus.Failed);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize rando {RandoId}", randoId);
                return false;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private RemoteGenerator? GetGenerator(Guid id)
        {
            _generators.TryGetValue(id, out var result);
            return result;
        }

        private async Task CreateDefaultProfile(int gameId)
        {
            var defaultConfig = await GetDefaultConfigAsync(gameId);
            var profile = await _db.GetDefaultProfile(gameId);
            if (profile == null)
            {
                var newProfile = new ProfileDbModel()
                {
                    UserId = _db.SystemUserId,
                    GameId = gameId,
                    Created = DateTime.UtcNow,
                    Name = "Default",
                    Description = "The default profile.",
                    Public = true,
                    Official = true
                };

                _logger.LogInformation("Creating profile {Name} for default config", newProfile.Name);
                await _db.CreateProfileAsync(newProfile, defaultConfig);
            }
            else
            {
                profile.Description = "The default profile.";
                profile.Public = true;
                profile.Official = true;

                _logger.LogInformation("Updating profile {Id} {Name} to default config", profile.Id, profile.Name);
                await _db.UpdateProfileAsync(profile);
                await _db.SetProfileConfigAsync(profile.Id, defaultConfig);
            }
        }
    }

    public class RemoteGenerator(int gameId)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime RegisterTime { get; } = DateTime.UtcNow;
        public DateTime LastHeartbeatTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Registered";
        public int GameId => gameId;
    }
}
