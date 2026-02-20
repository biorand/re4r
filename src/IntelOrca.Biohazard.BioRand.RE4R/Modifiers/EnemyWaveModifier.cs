using System;
using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyWaveModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-enemies"))
                return;

            var flagService = randomizer.FlagService;
            var minWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-min", 2), 2, 50);
            var maxWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-max", 2), minWaves, 50);
            var waveDistance = Math.Clamp(randomizer.GetConfigOption<float>("enemy-waves-distance", 10), 1, 100);

            var waveProbability = Math.Clamp(randomizer.GetConfigOption<float>("enemy-waves-probability", 1), 0, 1);
            var allSpawns = randomizer.AreaService.Areas
                .SelectMany(x => x.Enemies)
                .Shuffle(randomizer.GetRng("modifier/enemywave/pick"))
                .ToArray();

            var maxWavedEnemies = (int)(waveProbability * allSpawns.Length);
            var numWavedEnemies = 0;
            foreach (var oldSpawn in allSpawns)
            {
                if (numWavedEnemies >= maxWavedEnemies)
                    break;
                if (oldSpawn.IsOrphan || oldSpawn.EnemyPlacement.HasTag(EnemyTags.NoWave))
                    continue;
                if (!string.IsNullOrEmpty(oldSpawn.EnemyPlacement.MiniBoss))
                    continue;
                if (!oldSpawn.HasSimpleController)
                    continue;

                var area = oldSpawn.Area;
                var scn = oldSpawn.Area.ScnFile;
                var oldSpawnController = oldSpawn.SpawnController ?? throw new Exception("No spawn controller found");
                var lastSpawn = oldSpawn;
                var waveRng = randomizer.GetRng("modifier/enemywave/wave", oldSpawn.Guid);
                var numWaves = waveRng.Next(minWaves, maxWaves + 1);
                for (var i = 1; i < numWaves; i++)
                {
                    var guid = $"{oldSpawn.Guid}_wave_${i}".GetGuidHash();
                    var spawnControllerGameObject = randomizer.GetService<RszFactory>().CreateSpawnPointController(guid, $"BioRandOnDeathSpawn_{i}", waveDistance, [lastSpawn.Enemy]);
                    var spawnController = area.AddSpawnController(spawnControllerGameObject);

                    var deathFlag = flagService.AllocateFlag();
                    lastSpawn.Enemy.SetFieldValue("_DeathNotifyFlag", deathFlag);
                    spawnController.SpawnCondition = new chainsaw.FlagCondition()
                    {
                        _CheckFlags = new List<chainsaw.CheckFlagInfo>()
                            {
                                new chainsaw.CheckFlagInfo()
                                {
                                    _CheckFlag = deathFlag,
                                    _CompareValue = true
                                }
                            }
                    };
                    spawnController.SpawnSkipCondition = oldSpawnController.SpawnSkipCondition;

                    var newSpawn = lastSpawn.Duplicate(randomizer.FlagService.AllocateContextId(0, 0));
                    spawnController.AddEnemy(newSpawn);
                    newSpawn.Enemy.SetFieldValue("_ForceFind", true);
                    lastSpawn = newSpawn;
                }

                numWavedEnemies++;
            }
        }
    }
}
