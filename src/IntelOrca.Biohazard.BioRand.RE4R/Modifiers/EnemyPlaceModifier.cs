using System;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var extraEnemiesPercent = randomizer.GetConfigOption("extra-enemy-amount", 0.5);
            var extraEnemiesToPlace = GetExtraEnemiesToPlace(randomizer, extraEnemiesPercent)
                .GroupBy(x => FindBestAreaForEnemy(randomizer, x)!)
                .Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.ToArray());

            if (extraEnemiesToPlace.Count == 0)
                return;

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
                        spawnController = AddEnemyToSpawnController(randomizer, spawnController, enemyDef, logger);
                    }
                    logger.Pop();

                    area.AddSpawnController(spawnController);
                }
                logger.Pop();
            }
        }

        private static ImmutableArray<EnemyPlacement> GetExtraEnemiesToPlace(ChainsawRandomizer randomizer, double amount)
        {
            var allExtraEnemies = randomizer.EnemyService.EnemyPlacements
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => x.IsExtra)
                .ToArray();

            var mustPlace = allExtraEnemies
                .Where(x => x.HasTag(EnemyTags.Always))
                .ToArray();

            var mightPlace = allExtraEnemies
                .Where(x => !x.HasTag(EnemyTags.Always))
                .ToArray();

            // Randomize and pick
            var count = (int)Math.Round(allExtraEnemies.Length * amount);
            mightPlace = mightPlace
                .Shuffle(randomizer.GetRng("modifier/enemyplace"))
                .Take(count)
                .ToArray();

            return mustPlace.Concat(mightPlace).ToImmutableArray();
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

        private static RszGameObject AddEnemyToSpawnController(ChainsawRandomizer randomizer, RszGameObject spawnController, EnemyPlacement e, RandomizerLogger logger)
        {
            var contextId = randomizer.FlagService.AllocateContextId(0, 0);
            logger.LogLine($"Enemy {contextId} Position = ({e.Position.X}, {e.Position.Y}, {e.Position.Z})");

            var rotation = e.HasEmptyRotation ? RandomRotation(randomizer.GetRng("modifier/enemyplace/rotation", e.GuidOrAuto)) : e.Rotation;
            var transform = RszFactory.CreateTransform(e.Position, rotation.ToQuaternion());
            var spawnParam = FileRepository.RszRepository.Create("chainsaw.Ch1c0SpawnParamCommon")
                .Set("Enabled", true)
                .Set("_StageID", e.Stage)
                .Set("_SpawmRadius", 20.0f)
                .Set("_DeathNotifyFlag", e.DeathFlag)
                .Set("_ContextID", contextId)
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
                .Set("_ForceFind", e.HasTag(EnemyTags.Aggroed));

            if (e.ItemId != 0)
            {
                spawnParam = spawnParam
                    .Set("_ShouldDropItem", true)
                    .Set("_DropItemID", e.ItemId)
                    .Set("_DropItemCount", 1);
            }

            var enemy = RszFactory.CreateGameObject("BioRandEnemy", "_Chainsaw/AppSystem/Prefab/ch1c0SpawnParam.pfb", [transform, spawnParam])
                .WithGuid(e.GuidOrAuto);
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
    }
}
