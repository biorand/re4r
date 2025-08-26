using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyPlaceModifier : Modifier
    {
        private int _contextIdGroup;
        private int _contextIdIndex;
#if DEBUG
        private HashSet<Guid> _guids = [];
#endif

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();

            var extraEnemiesPercent = randomizer.GetConfigOption("extra-enemy-amount", 0.5);
            if (extraEnemiesPercent <= 0)
                return;

            var extraEnemiesToPlace = GetExtraEnemiesToPlace(randomizer, extraEnemiesPercent, rng);

            _contextIdGroup = 1;
            _contextIdIndex = 1;
            foreach (var area in randomizer.Areas)
            {
                var def = area.Definition;
                if (def.Extra == null)
                    continue;

                logger.Push(area.FileName);
                var scn = area.ScnFile;
                foreach (var extra in def.Extra)
                {
                    if (extra.Enemies == null)
                        continue;

                    if (extra.Kind == "points")
                    {
                        var spawnController = RszFactory.CreateSpawnPointController(rng.NextGuid(), "BioRandSpawnPointController", 15, extra.Enemies);
                        spawnController = AddSpawnControllerConditions(spawnController, extra.Condition, extra.SkipCondition);

                        logger.Push($"CharacterSpawnPointController Condition = {extra.Condition} SkipCondition = {extra.SkipCondition}");

                        foreach (var enemyDef in extra.Enemies)
                        {
                            spawnController = AddEnemyToSpawnController(def, spawnController, enemyDef, extra, rng, logger);
                        }

                        area.Scene = area.Scene.Add(spawnController);
                    }
                    else
                    {
                        foreach (var g in extra.Enemies.GroupBy(x => x.Stage))
                        {
                            var spawnController = RszFactory.CreateSpawnController("BioRandInitialSpawn");
                            spawnController = AddSpawnControllerConditions(spawnController, extra.Condition, extra.SkipCondition);

                            logger.Push($"CharacterSpawnController Condition = {extra.Condition} SkipCondition = {extra.SkipCondition}");

                            foreach (var enemyDef in g)
                            {
                                if (extraEnemiesToPlace.Contains(enemyDef))
                                {
                                    spawnController = AddEnemyToSpawnController(def, spawnController, enemyDef, extra, rng, logger);
                                }
                            }
                            logger.Pop();

                            area.Scene = area.Scene.Add(spawnController);
                        }
                    }
                }
                logger.Pop();
            }
        }

        private RszGameObject AddEnemyToSpawnController(AreaDefinition def, RszGameObject spawnController, AreaExtraEnemy enemyDef, AreaExtra extra, Rng rng, RandomizerLogger logger)
        {
            var position = new Vector3(enemyDef.X, enemyDef.Y, enemyDef.Z);
            var rotation = enemyDef.Direction == 0
                ? RandomRotation(rng)
                : new EulerAngles(enemyDef.Direction, 0, 0);
            var enemy = CreateEnemy("BioRandEnemy", enemyDef.Stage, position, rotation, enemyDef.FindPlayer, rng, logger)
                .WithGuid(enemyDef.Guid.HasValue
                    ? enemyDef.Guid.Value
                    : HashGuid(enemyDef.Stage, enemyDef.X, enemyDef.Y, enemyDef.Z, extra.Condition, extra.SkipCondition));
#if DEBUG
            if (!_guids.Add(enemy.Guid))
            {
                throw new Exception("Guid already used for enemy.");
            }
#endif

            if (enemyDef.Ranged)
            {
                var rangedClasses = EnemyClassFactory.Default.Classes
                    .Where(x => x.Ranged)
                    .Select(x => x.Key)
                    .ToArray();

                var restriction = new AreaRestriction()
                {
                    Guids = [enemy.Guid],
                    Include = rangedClasses
                };
                def.Restrictions = [.. (def.Restrictions ?? []), restriction];
            }
            if (enemyDef.Small)
            {
                var restriction = new AreaRestriction()
                {
                    Guids = [enemy.Guid],
                    Exclude = ["mendez_chase", "verdugo", "mendez_2", "krauser_2", "pesanta", "u3"]
                };
                def.Restrictions = [.. (def.Restrictions ?? []), restriction];
            }
            return spawnController.AddOrUpdateChild(enemy);
        }

        private static HashSet<AreaExtraEnemy> GetExtraEnemiesToPlace(ChainsawRandomizer randomizer, double amount, Rng rng)
        {
            var allExtraEnemies = randomizer.Areas
                .SelectMany(x => x.Definition.Extra ?? [])
                .SelectMany(x => x.Enemies ?? [])
                .Shuffle(rng);

            var count = (int)Math.Round(allExtraEnemies.Length * amount);
            return allExtraEnemies.Take(count).ToHashSet();
        }

        private ContextId GetNextContextId()
        {
            return new ContextId(0, 0, _contextIdGroup, _contextIdIndex++);
        }

        private static EulerAngles RandomRotation(Rng rng)
        {
            var angle = (float)rng.NextDouble(-180, 180);
            return new EulerAngles(angle, 0, 0);
        }

        private static RszStructNode CreateCheckFlag(Guid guid)
        {
            var checkFlagInfo = FileRepository.RszRepository.Create("chainsaw.CheckFlagInfo");
            checkFlagInfo.Set("_CheckFlag", guid);
            checkFlagInfo.Set("_CompareValue", true);
            return checkFlagInfo;
        }

        private static RszGameObject AddSpawnControllerConditions(RszGameObject spawnController, string? condition, string? skipCondition)
        {
            var component = spawnController.Components[1];
            if (!string.IsNullOrEmpty(condition))
            {
                component = component.Set("_SpawnCondition._Logic", 0);
                component = component.Set("_SpawnCondition._CheckFlags", new[]
                {
                    CreateCheckFlag(new Guid(condition)),
                });
            }
            if (!string.IsNullOrEmpty(skipCondition))
            {
                component = component.Set("_SpawnSkipCondition._Logic", 0);
                component = component.Set("_SpawnSkipCondition._CheckFlags", new[]
                {
                    CreateCheckFlag(new Guid(skipCondition)),
                });
            }
            return spawnController.AddOrUpdateComponent(component);
        }

        private RszGameObject CreateEnemy(string name, int stageId, Vector3 position, EulerAngles rotation, bool findPlayer, Rng rng, RandomizerLogger logger)
        {
            var repo = FileRepository.RszRepository;

            var contextId = GetNextContextId();
            logger.LogLine($"Enemy {contextId} Position = ({position.X}, {position.Y}, {position.Z})");

            var transform = RszFactory.CreateTransform(position, rotation.ToQuaternion());
            var spawnParam = repo.Create("chainsaw.Ch1c0SpawnParamCommon")
                .Set("_Enabled", true)
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

        private static Guid HashGuid(params object?[] args) => HashGuid(string.Concat(args));
        private static Guid HashGuid(string s)
        {
            var hash = MD5.HashData(Encoding.ASCII.GetBytes(s));
            hash[8] = (byte)(0x40 | (hash[8] & 0x0F));
            return new Guid(hash);
        }
    }
}
