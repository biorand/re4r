using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyMultiplierModifier : Modifier
    {
        private int _contextId = 5000;
        private Dictionary<int, int> _stageEnemyCount = [];

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();
            var areaByChapter = randomizer.Areas.GroupBy(x => x.Definition.Chapter);
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                logger.Push("Duplicating enemies");
                foreach (var chapterAreas in areaByChapter)
                {
                    logger.Push($"Chapter {chapterAreas.Key}");
                    foreach (var area in chapterAreas)
                    {
                        logger.Push(area.FileName);
                        RandomizeArea(randomizer, area, rng);
                        logger.Pop();
                    }
                    _stageEnemyCount.Clear();
                    logger.Pop();
                }
                logger.Pop();
            }
        }

        private void RandomizeArea(ChainsawRandomizer randomizer, Area area, Rng rng)
        {
            // Duplicate enemy spawns
            var spawns = area.GetEnemySpawns();
            foreach (var spawn in spawns)
            {
                var stageId = spawn.Enemy.StageID;
                _stageEnemyCount.TryGetValue(stageId, out var count);
                _stageEnemyCount[stageId] = ++count;
            }
            DuplicateEnemies(randomizer, spawns, rng);
        }

        private void DuplicateEnemies(ChainsawRandomizer randomizer, ImmutableArray<EnemySpawn> spawns, Rng rng)
        {
            var multiplier = randomizer.GetConfigOption<double>("enemy-multiplier", 1);
            var maxPerStage = randomizer.GetConfigOption("debug-stage-enemy-limit-default", 25);
            var newList = spawns.ToBuilder();
            foreach (var g in spawns.GroupBy(x => x.StageID))
            {
                var enemyLimit = randomizer.GetConfigOption($"debug-stage-enemy-limit-{g.Key}", 0);
                if (enemyLimit == 0)
                {
                    enemyLimit = maxPerStage;
                }

                var stageSpawns = g.Where(x => !x.PreventDuplicate).ToArray();
                var newEnemyCount = Math.Min(enemyLimit, stageSpawns.Length * multiplier);
                var delta = (int)Math.Round(newEnemyCount - stageSpawns.Length);
                if (delta != 0)
                {
                    var bag = new EndlessBag<EnemySpawn>(rng, stageSpawns);
                    while (delta > 0)
                    {
                        var enemyToDuplicate = bag.Next();
                        var stageId = enemyToDuplicate.Enemy.StageID;
                        if (!_stageEnemyCount.TryGetValue(stageId, out var currentStageIdCount))
                            _stageEnemyCount[stageId] = 0;

                        if (currentStageIdCount < maxPerStage)
                        {
                            var newEnemy = enemyToDuplicate.Duplicate(GetNextContextId());
                            newList.Add(newEnemy);
                            _stageEnemyCount[stageId]++;
                            delta--;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        private int GetNextContextId()
        {
            return _contextId++;
        }
    }
}
