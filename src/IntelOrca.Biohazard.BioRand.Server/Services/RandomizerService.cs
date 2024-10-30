﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class RandomizerService
    {
        private readonly static TimeSpan DownloadExpireTime = TimeSpan.FromHours(1);

        private readonly Random _random = new Random();
        private readonly Dictionary<ulong, GenerateResult> _randos = new();
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1);

        private readonly ImmutableArray<IRandomizer> _randomizers = [
            new IntelOrca.Biohazard.BioRand.RE4R.Re4rRandomizer()
        ];

        private void ExpireOldRandos()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _randos.ToArray())
            {
                var age = now - kvp.Value.CreatedAt;
                if (age > DownloadExpireTime)
                {
                    _randos.Remove(kvp.Key);
                }
            }
        }

        public IRandomizer GetRandomizer()
        {
            const string className = "IntelOrca.Biohazard.BioRand.RE4R.Re4rRandomizer";
            var randomizer = _randomizers.First(x => x.GetType().FullName == className);
            return randomizer;
        }

        public async Task<GenerateResult> GenerateAsync(
            ulong id,
            string profileName,
            string profileDescription,
            string profileAuthor,
            int seed,
            RandomizerConfiguration config)
        {
            await _mutex.WaitAsync();
            try
            {
                ExpireOldRandos();

                var biorandConfig = BioRandServerConfiguration.GetDefault();
                var randomizer = GetRandomizer();
                var input = new RandomizerInput
                {
                    GamePath = biorandConfig.GamePath,
                    ProfileName = profileName,
                    ProfileDescription = profileDescription,
                    ProfileAuthor = profileAuthor,
                    Seed = seed,
                    Configuration = config
                };
                var output = randomizer.Randomize(input);
                var outputFile = output.PakOutput;
                var outputFileMod = output.FluffyOutput;
                var result = new GenerateResult(id, seed, outputFile, outputFileMod);
                _randos[id] = result;
                return result;
            }
            finally
            {
                _mutex.Release();
            }
        }

        public GenerateResult? Find(ulong id)
        {
            _randos.TryGetValue(id, out var result);
            return result;
        }
    }

}
