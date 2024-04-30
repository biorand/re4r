using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyPlaceModifier : Modifier
    {
        private int _contextIdGroup;
        private int _contextIdIndex;

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();

            if (!randomizer.GetConfigOption("extra-enemies", false))
                return;

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

                    foreach (var g in extra.Enemies.GroupBy(x => x.Stage))
                    {
                        var spawnController = CreateSpawnController(scn, "BioRandSpawnController");
                        if (!string.IsNullOrEmpty(extra.Condition))
                        {
                            spawnController.Components[1].Set("_SpawnCondition._Logic", 0);
                            spawnController.Components[1].Set("_SpawnCondition._CheckFlags", new List<object>()
                            {
                                CreateCheckFlag(scn, new Guid(extra.Condition)),
                            });
                            logger.Push($"CharacterSpawnController Condition = ({extra.Condition})");
                        }
                        else
                        {
                            logger.Push($"CharacterSpawnController");
                        }

                        foreach (var enemyDef in g)
                        {
                            var position = new Vector3(enemyDef.X, enemyDef.Y, enemyDef.Z);
                            var enemy = CreateEnemy(scn, spawnController, "BioRandEnemy", enemyDef.Stage, position, RandomRotation(rng), rng, logger);

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
                        }
                        logger.Pop();
                    }
                }
                logger.Pop();
            }
        }

        private ContextId GetNextContextId()
        {
            return new ContextId(0, 0, _contextIdGroup, _contextIdIndex++);
        }

        private Guid NextGuid(Rng rng)
        {
            return rng.NextGuid();
        }

        private static Vector4 RandomRotation(Rng rng)
        {
            var angle = (float)rng.NextDouble(0, Math.PI * 2);
            return new Vector4(0, MathF.Sin(angle), 0, MathF.Cos(angle));
        }

        private static RszInstance CreateCheckFlag(ScnFile scn, Guid guid)
        {
            var checkFlagInfo = scn.RSZ!.CreateInstance("chainsaw.CheckFlagInfo");
            checkFlagInfo.Set("_CheckFlag", guid);
            checkFlagInfo.Set("_CompareValue", true);
            return checkFlagInfo;
        }

        private static ScnFile.GameObjectData CreateSpawnController(ScnFile scn, string name)
        {
            var newGameObject = scn.CreateGameObject(name);
            // newGameObject.Prefab!.Path = "_Chainsaw/AppSystem/Prefab/CharacterSpawnController.pfb";
            SetTransform(scn, newGameObject, Vector4.Zero);

            var characterSpawnControllerComponent = CreateComponent(scn, newGameObject, "chainsaw.CharacterSpawnController");
            characterSpawnControllerComponent.Set("v0", (byte)1);
            characterSpawnControllerComponent.Set("_DifficutyParam", 63U);
            characterSpawnControllerComponent.Set("_GUID", Guid.NewGuid());
            return newGameObject;
        }

        private ScnFile.GameObjectData CreateEnemy(ScnFile scn, ScnFile.GameObjectData parent, string name, int stageId, Vector3 position, Vector4 rotation, Rng rng, RandomizerLogger logger)
        {
            var newGameObject = scn.CreateGameObject(name);
            SetTransform(scn, newGameObject, new Vector4(position, 1), rotation);
            scn.RemoveGameObject(newGameObject);
            newGameObject = scn.ImportGameObject(newGameObject, parent: parent);
            newGameObject.Guid = NextGuid(rng);

            var contextId = GetNextContextId();
            var spawnParam = CreateComponent(scn, newGameObject, "chainsaw.Ch1c0SpawnParamCommon");
            spawnParam.Set("v0", (byte)1);
            spawnParam.Set("_StageID", stageId);
            spawnParam.Set("_SpawmRadius", 20.0f);
            spawnParam.Set("_ContextID._Group", contextId.Group);
            spawnParam.Set("_ContextID._Index", contextId.Index);
            spawnParam.Set("_RoleType", 3);
            spawnParam.Set("_IsEnableUnreachable", true);
            spawnParam.Set("_RolePatternHash", 3152132219U);
            spawnParam.Set("_SegmentID", 1);
            spawnParam.Set("_FirstForceMoveEndTime", -1.0f);
            spawnParam.Set("_FirstForceMoveEndRadius", 0.2f);
            spawnParam.Set("_PreFirstForceMovePatternHash", 3152132219U);
            spawnParam.Set("_RoleActionEndOnDamage", true);
            spawnParam.Set("_CriticalResistRate", 0.25f);
            spawnParam.Set("_MontageID", 1017464743U);

            logger.LogLine($"Enemy {contextId} Position = ({position.X}, {position.Y}, {position.Z})");
            return newGameObject;
        }

        private static void SetTransform(ScnFile scn, ScnFile.GameObjectData gameObject, Vector4 position, Vector4? rotation = null)
        {
            var transformComponent = GetOrCreateComponent(scn, gameObject, "via.Transform");
            transformComponent!.Set("v0", position);
            transformComponent!.Set("v1", rotation ?? new Vector4(0, 0, 0, 1));
            transformComponent!.Set("v2", new Vector4(1, 1, 1, 0));
        }

        private static RszInstance CreateComponent(ScnFile scn, ScnFile.GameObjectData gameObject, string className)
        {
            scn.AddComponent(gameObject, className);
            return gameObject.Components.Last();
        }

        private static RszInstance GetOrCreateComponent(ScnFile scn, ScnFile.GameObjectData gameObject, string className)
        {
            var component = gameObject.FindComponent(className);
            if (component == null)
            {
                scn.AddComponent(gameObject, className);
                component = gameObject.Components.Last();
            }
            return component;
        }
    }
}
