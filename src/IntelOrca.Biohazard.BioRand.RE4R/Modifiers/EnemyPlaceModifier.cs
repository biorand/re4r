using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();

            var extraEnemiesPercent = randomizer.GetConfigOption("extra-enemy-amount", 0.5);
            if (extraEnemiesPercent <= 0)
                return;

            var extraEnemiesToPlace = GetExtraEnemiesToPlace(randomizer, extraEnemiesPercent, rng)
                .GroupBy(x => FindBestAreaForEnemy(randomizer, x)!)
                .Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.ToArray());

            foreach (var area in randomizer.AreaService.Areas)
            {
                if (!extraEnemiesToPlace.TryGetValue(area, out var enemiesToPlace))
                    continue;

                logger.Push(area.FileName);
                var scn = area.ScnFile;
                foreach (var g in enemiesToPlace.GroupBy(x => (x.Stage, x.Condition, x.SkipCondition)))
                {
                    var firstEnemy = g.First();
                    var spawnController = RszFactory.CreateSpawnController("BioRandInitialSpawn");
                    spawnController = AddSpawnControllerConditions(spawnController, firstEnemy.Condition, firstEnemy.SkipCondition);

                    logger.Push($"CharacterSpawnController Condition = {firstEnemy.Condition} SkipCondition = {firstEnemy.SkipCondition}");

                    foreach (var enemyDef in g)
                    {
                        spawnController = AddEnemyToSpawnController(randomizer, spawnController, enemyDef, rng, logger);
                    }
                    logger.Pop();

                    area.AddSpawnController(spawnController);
                }
            }
            logger.Pop();
        }

        private static ImmutableArray<EnemyPlacement> GetExtraEnemiesToPlace(ChainsawRandomizer randomizer, double amount, Rng rng)
        {
            var allExtraEnemies = randomizer.EnemyService.EnemyPlacements
                .Where(x => x.IsExtra)
                .Where(x => x.Campaign == randomizer.Campaign)
                .Shuffle(rng);

            var count = (int)Math.Round(allExtraEnemies.Length * amount);
            return allExtraEnemies.Take(count).ToImmutableArray();
        }

        private static Area? FindBestAreaForEnemy(ChainsawRandomizer randomizer, EnemyPlacement placement)
        {
            var result = randomizer.AreaService.Areas
                .Where(x => IsAreaCompatible(x, placement))
                .OrderBy(x => GetOrder(x, placement))
                .FirstOrDefault();
            return result;

            static bool IsAreaCompatible(Area area, EnemyPlacement placement)
            {
                if (placement.HasTag(EnemyTags.AnyChapter))
                {
                    if (!area.Definition.ChapterOnly)
                    {
                        return area.Definition.Location == placement.Location;
                    }
                }
                else
                {
                    if (area.Definition.ChapterOnly)
                    {
                        return placement.Chapter == area.Definition.Chapter;
                    }
                }
                return false;
            }

            static int GetOrder(Area area, EnemyPlacement placement)
            {
                if (area.Enemies.Any())
                {
                    return area.Enemies.Min(x => Math.Abs(x.StageID - placement.Stage));
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }

        private static RszGameObject AddEnemyToSpawnController(ChainsawRandomizer randomizer, RszGameObject spawnController, EnemyPlacement e, Rng rng, RandomizerLogger logger)
        {
            var enemy = CreateEnemy(
                randomizer,
                "BioRandEnemy",
                e.Stage,
                e.Position,
                e.HasEmptyRotation ? RandomRotation(rng) : e.Rotation,
                e.HasTag(EnemyTags.Aggroed),
                rng,
                logger).WithGuid(e.GuidOrAuto);
            return spawnController.AddOrUpdateChild(enemy);
        }

        private static EulerAngles RandomRotation(Rng rng)
        {
            var angle = (float)rng.NextDouble(-180, 180);
            return new EulerAngles(angle, 0, 0);
        }

        private static RszGameObject AddSpawnControllerConditions(RszGameObject spawnController, string? condition, string? skipCondition)
        {
            var component = spawnController.Components[1];
            if (!string.IsNullOrEmpty(condition))
            {
                component = component.Set("_SpawnCondition", new FlagCondition()
                {
                    _CheckFlags =
                    [
                        new CheckFlagInfo()
                        {
                            _CheckFlag = new Guid(condition),
                            _CompareValue = true
                        }
                    ]
                });
            }
            if (!string.IsNullOrEmpty(skipCondition))
            {
                component = component.Set("_SpawnSkipCondition", new FlagConditionStrict()
                {
                    _CheckFlags =
                    [
                        new CheckFlagInfo()
                        {
                            _CheckFlag = new Guid(skipCondition),
                            _CompareValue = true
                        }
                    ]
                });
            }
            return spawnController.AddOrUpdateComponent(component);
        }

        private static RszGameObject CreateEnemy(ChainsawRandomizer randomizer, string name, int stageId, Vector3 position, EulerAngles rotation, bool findPlayer, Rng rng, RandomizerLogger logger)
        {
            var contextId = randomizer.EnemyService.GetNextContextId();
            logger.LogLine($"Enemy {contextId} Position = ({position.X}, {position.Y}, {position.Z})");

            var transform = RszFactory.CreateTransform(position, rotation.ToQuaternion());
            var spawnParam = FileRepository.RszRepository.Create("chainsaw.Ch1c0SpawnParamCommon")
                .Set("Enabled", true)
                .Set("_StageID", stageId)
                .Set("_SpawmRadius", 20.0f)
                .Set("_ContextID._Group", contextId.Group)
                .Set("_ContextID._Index", contextId.Index)
                .Set("_RoleType", 3)
                .Set("_IsEnableUnreachable", true)
                .Set("_RolePatternHash", 3152132219U)
                .Set("_SegmentID", 1)
                .Set("_FirstForceMoveEndTime", -1.0f)
                .Set("_FirstForceMoveEndRadius", 0.2f)
                .Set("_PreFirstForceMovePatternHash", 3152132219U)
                .Set("_RoleActionEndOnDamage", true)
                .Set("_CriticalResistRate", 0.25f)
                .Set("_MontageID", 1017464743U)
                .Set("_ForceFind", findPlayer);
            return RszFactory.CreateGameObject(name, "_Chainsaw/AppSystem/Prefab/ch1c0SpawnParam.pfb", [transform, spawnParam]);
        }
    }
}
