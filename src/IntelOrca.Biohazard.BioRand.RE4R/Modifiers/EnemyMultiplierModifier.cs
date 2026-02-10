using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyMultiplierModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var multiplier = randomizer.GetConfigOption<double>("enemy-multiplier", 1);
            if (multiplier == 1)
                return;

            var areaByChapter = randomizer.AreaService.Areas.GroupBy(x => x.Definition.Chapter);
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                logger.Push("Duplicating enemies");
                foreach (var chapterAreas in areaByChapter)
                {
                    logger.Push($"Chapter {chapterAreas.Key}");
                    foreach (var area in chapterAreas)
                    {
                        logger.Push(area.FileName);
                        DuplicateEnemies(randomizer, area.Enemies.ToImmutableArray(), multiplier);
                        logger.Pop();
                    }
                    logger.Pop();
                }
                logger.Pop();
            }
        }

        private static void DuplicateEnemies(ChainsawRandomizer randomizer, ImmutableArray<EnemySpawn> spawns, double multiplier)
        {
            var flagService = randomizer.GetService<FlagService>();

            var newList = spawns.ToBuilder();
            foreach (var g in spawns.GroupBy(x => x.StageID))
            {
                var stageSpawns = g.Where(x => !x.IsOrphan && !x.EnemyPlacement.HasTag(EnemyTags.NoDuplicate)).ToArray();
                var newEnemyCount = stageSpawns.Length * multiplier;
                var delta = (int)Math.Round(newEnemyCount - stageSpawns.Length);
                if (delta != 0)
                {
                    var rng = randomizer.GetRng("modifier/enemymultiplier/pick", g.Key);
                    var bag = new EndlessBag<EnemySpawn>(rng, stageSpawns);
                    while (delta > 0)
                    {
                        var enemyToDuplicate = bag.Next();
                        var newEnemy = enemyToDuplicate.Duplicate(flagService.AllocateContextId(0, 0));
                        var spawnController = enemyToDuplicate.SpawnController ?? throw new Exception("No spawn controller found");
                        spawnController.AddEnemy(newEnemy);
                        newList.Add(newEnemy);
                        delta--;
                    }
                }
            }
        }
    }
}
