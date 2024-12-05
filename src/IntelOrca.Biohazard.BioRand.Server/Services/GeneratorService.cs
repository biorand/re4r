using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using Microsoft.Extensions.Logging;
using static IntelOrca.Biohazard.BioRand.Server.Services.DatabaseService;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class GeneratorService(DatabaseService db, ILogger<GeneratorService> logger)
    {
        private readonly static TimeSpan DownloadExpireTime = TimeSpan.FromHours(1);

        private readonly TimeSpan HeartbeatTimeout = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<Guid, RemoteGenerator> _generators = [];
        private readonly Dictionary<int, GenerateResult> _generatedRandos = [];
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private RandomizerConfigurationDefinition? _configDefinition;
        private RandomizerConfiguration? _defaultConfig;

        public async Task ExpireOldRandosAsync()
        {
            var result = new List<int>();
            var now = DateTime.UtcNow;
            foreach (var kvp in _generatedRandos.ToArray())
            {
                var age = now - kvp.Value.CreatedAt;
                if (age > DownloadExpireTime)
                {
                    _generatedRandos.Remove(kvp.Key);
                    result.Add(kvp.Key);
                }
            }
            foreach (var id in result)
            {
                await db.SetRandoStatusAsync(id, Models.RandoStatus.Expired);
            }
        }

        public Task<RemoteGenerator[]> GetAllAsync()
        {
            return Task.FromResult(_generators.Values.ToArray());
        }

        public Task<RandomizerConfigurationDefinition> GetConfigDefinitionAsync()
        {
            return Task.FromResult(_configDefinition ?? new RandomizerConfigurationDefinition());
        }

        public Task<RandomizerConfiguration> GetDefaultConfigAsync()
        {
            return Task.FromResult(_defaultConfig ?? new RandomizerConfiguration());
        }

        public Task<GenerateResult?> GetResult(int randoId)
        {
            _generatedRandos.TryGetValue(randoId, out var result);
            return Task.FromResult(result);
        }

        public Task<RemoteGenerator> RegisterAsync(
            RandomizerConfigurationDefinition definition,
            RandomizerConfiguration defaultConfig)
        {
            var generator = new RemoteGenerator();
            if (!_generators.TryAdd(generator.Id, generator))
                throw new Exception("Failed to register generator");

            _configDefinition = definition;
            _defaultConfig = defaultConfig;
            return Task.FromResult(generator);
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

        public Task CleanUpGeneratorsAsync()
        {
            var generators = _generators.Values.ToArray();
            foreach (var g in generators)
            {
                var timeSinceLastHeartbeat = DateTime.UtcNow - g.LastHeartbeatTime;
                if (timeSinceLastHeartbeat > HeartbeatTimeout)
                {
                    _generators.TryRemove(g.Id, out _);
                }
            }
            return Task.CompletedTask;
        }

        public async Task<ExtendedRandoDbModel[]> GetQueueAsync()
        {
            var queryResult = await db.GetUnassignedRandosAsync();
            var results = queryResult.Results;

            // Clear emails
            foreach (var r in results)
            {
                r.UserEmail = null;
            }

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
                var queue = await GetQueueAsync();
                var rando = queue.FirstOrDefault(x => x.Id == randoId);
                if (rando == null)
                    return false;

                if (rando.Status != RandoStatus.Unassigned)
                    return false;

                rando.Version = version;
                rando.Status = RandoStatus.Processing;
                await db.UpdateRandoAsync(rando);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start processing rando {RandoId}", randoId);
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
                var rando = await db.GetRandoAsync(randoId);
                if (rando == null)
                    return false;

                if (rando.Status != RandoStatus.Processing)
                    return false;

                _generatedRandos.Add(randoId, new GenerateResult((ulong)randoId, rando.Seed, pakOutput, fluffyOutput));
                await db.SetRandoStatusAsync(rando.Id, RandoStatus.Completed);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to finalize rando {RandoId}", randoId);
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
    }

    public class RemoteGenerator
    {
        public Guid Id { get; } = Guid.NewGuid();
        public DateTime RegisterTime { get; } = DateTime.UtcNow;
        public DateTime LastHeartbeatTime { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Registered";
    }
}
