using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyMultiplierModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var multiplier = Math.Clamp(randomizer.GetConfigOption<double>("enemy-multiplier", 1), 1, 5);
            if (multiplier == 1)
                return;

            var stageLimits = GetStageLimits(randomizer);

            var areaByChapter = randomizer.AreaService.Areas.GroupBy(x => x.Definition.Chapter);
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                logger.Push("Duplicating enemies");
                foreach (var chapterAreas in areaByChapter)
                {
                    if (!chapterAreas.Any())
                        continue;

                    logger.Push($"Chapter {chapterAreas.Key}");
                    var allEnemiesInChapter = chapterAreas
                        .SelectMany(x => x.Enemies)
                        .ToList();

                    var chapterStageLimits = stageLimits
                        .Where(x => x.Chapter == chapterAreas.Key)
                        .ToImmutableArray();
                    foreach (var group in chapterStageLimits)
                    {
                        logger.Push($"Stage group ({string.Join(", ", group.Stage)})");
                        var enemiesInGroup = TakeSpawns(group, allEnemiesInChapter);
                        DuplicateEnemies(randomizer, enemiesInGroup, multiplier * group.Damp, group.HardLimit, logger);
                        logger.Pop();
                    }

                    if (allEnemiesInChapter.Count > 0)
                    {
                        logger.Push($"Ungrouped enemies");
                        logger.LogLine($"Stages: ({string.Join(", ", allEnemiesInChapter.Select(x => x.StageID).Order().Distinct())})");
                        DuplicateEnemies(randomizer, allEnemiesInChapter, multiplier, 10_000, logger);
                        logger.Pop();
                    }

                    logger.Pop();
                }
                logger.Pop();
            }
        }

        private static void DuplicateEnemies(ChainsawRandomizer randomizer, IList<EnemySpawn> spawns, double multiplier, int hardLimit, RandomizerLogger logger)
        {
            var flagService = randomizer.GetService<FlagService>();
            var filteredSpawns = spawns.Where(x => !x.IsOrphan && !x.EnemyPlacement.HasTag(EnemyTags.NoDuplicate)).ToArray();
            var addedEnemies = 0;
            var newEnemyCount = Math.Min(hardLimit, filteredSpawns.Length * multiplier);
            var delta = (int)Math.Round(newEnemyCount - filteredSpawns.Length);
            if (delta >= 0)
            {
                var rng = randomizer.GetRng("modifier/enemymultiplier/pick");
                var bag = new EndlessBag<EnemySpawn>(rng, filteredSpawns);
                for (var i = 0; i < delta; i++)
                {
                    var enemyToDuplicate = bag.Next();
                    var newEnemy = enemyToDuplicate.Duplicate(flagService.AllocateContextId(0, 0));
                    var spawnController = enemyToDuplicate.SpawnController ?? throw new Exception("No spawn controller found");
                    spawnController.AddEnemy(newEnemy);
                    addedEnemies++;
                }
            }

            logger.LogLine($"Total = {spawns.Count}, Applicable = {filteredSpawns.Length} * {multiplier}, Max = {hardLimit}, New Total = {spawns.Count + addedEnemies}");
        }

        private static ImmutableArray<EnemySpawn> TakeSpawns(StageLimitRow stageLimitRow, List<EnemySpawn> spawns)
        {
            var stages = stageLimitRow.Stage.ToHashSet();
            var result = spawns.Where(x => stages.Contains(x.StageID)).ToImmutableArray();
            spawns.RemoveAll(x => stages.Contains(x.StageID));
            return result;
        }

        private static ImmutableArray<StageLimitRow> GetStageLimits(ChainsawRandomizer randomizer)
        {
            var result = new List<StageLimitRow>();
            var data = randomizer.DynamicData.GetData(DynamicDataName.StageLimits)!;
            var rows = Csv.Deserialize<StageLimitRow>(data);

            StageLimitRow? last = null;
            foreach (var row in rows)
            {
                if (row.Chapter == 0)
                {
                    if (last == null)
                        throw new RandomizerUserException("Stage limit sheet issue");

                    last.Stage = last.Stage.AddRange(row.Stage);
                }
                else
                {
                    if (last != null)
                    {
                        result.Add(last);
                    }
                    last = row;
                }
            }
            if (last != null)
            {
                result.Add(last);
            }

            return result
                .Where(x => x.Campaign == randomizer.Campaign)
                .ToImmutableArray();
        }

        private class StageLimitRow
        {
            public Campaign Campaign { get; set; }
            public int Chapter { get; set; }
            public ImmutableArray<int> Stage { get; set; }
            public double Damp { get; set; }
            public int HardLimit { get; set; }
        }
    }
}
