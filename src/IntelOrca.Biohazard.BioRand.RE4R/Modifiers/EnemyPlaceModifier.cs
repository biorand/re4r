using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

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
                        var spawnController = CreateSpawnPointController(scn, "BioRandSpawnPointController", extra.Enemies);
                        AddSpawnControllerConditions(scn, spawnController, extra.Condition, extra.SkipCondition);

                        logger.Push($"CharacterSpawnPointController Condition = {extra.Condition} SkipCondition = {extra.SkipCondition}");

                        foreach (var enemyDef in extra.Enemies)
                        {
                            AddEnemyToSpawnController(def, scn, spawnController, enemyDef, extra, rng, logger);
                        }
                    }
                    else
                    {
                        foreach (var g in extra.Enemies.GroupBy(x => x.Stage))
                        {
                            var spawnController = CreateSpawnController(scn, "BioRandSpawnController");
                            AddSpawnControllerConditions(scn, spawnController, extra.Condition, extra.SkipCondition);

                            logger.Push($"CharacterSpawnController Condition = {extra.Condition} SkipCondition = {extra.SkipCondition}");

                            foreach (var enemyDef in g)
                            {
                                if (extraEnemiesToPlace.Contains(enemyDef))
                                {
                                    AddEnemyToSpawnController(def, scn, spawnController, enemyDef, extra, rng, logger);
                                }
                            }
                            logger.Pop();
                        }
                    }
                }
                logger.Pop();
            }
        }

        private void AddEnemyToSpawnController(AreaDefinition def, ScnFile scn, ScnFile.GameObjectData spawnController, AreaExtraEnemy enemyDef, AreaExtra extra, Rng rng, RandomizerLogger logger)
        {
            var position = new Vector3(enemyDef.X, enemyDef.Y, enemyDef.Z);
            var rotation = enemyDef.Direction == 0
                ? RandomRotation(rng)
                : RotationToQuaternion(enemyDef.Direction, 0, 0);
            var enemy = CreateEnemy(scn, spawnController, "BioRandEnemy", enemyDef.Stage, position, rotation, enemyDef.FindPlayer, rng, logger);
            enemy.Guid = enemyDef.Guid.HasValue
                ? enemyDef.Guid.Value
                : HashGuid(enemyDef.Stage, enemyDef.X, enemyDef.Y, enemyDef.Z, extra.Condition, extra.SkipCondition);
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

        private static Vector4 RandomRotation(Rng rng)
        {
            var angle = (float)rng.NextDouble(-180, 180);
            return RotationToQuaternion(angle, 0, 0);
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
            newGameObject.Prefab = new ScnFile.PrefabInfo()
            {
                Path = "_Chainsaw/AppSystem/Prefab/CharacterSpawnController.pfb"
            };
            SetTransform(scn, newGameObject, Vector4.Zero);

            var characterSpawnControllerComponent = CreateComponent(scn, newGameObject, "chainsaw.CharacterSpawnController");
            characterSpawnControllerComponent.Set("v0", (byte)1);
            characterSpawnControllerComponent.Set("_DifficutyParam", 63U);
            characterSpawnControllerComponent.Set("_GUID", Guid.NewGuid());
            return newGameObject;
        }

        private static ScnFile.GameObjectData CreateSpawnPointController(ScnFile scn, string name, AreaExtraEnemy[] enemies)
        {
            var newGameObject = scn.CreateGameObject(name);
            newGameObject.Prefab = new ScnFile.PrefabInfo()
            {
                Path = "_Chainsaw/AppSystem/Prefab/CharacterSpawnPointController.pfb"
            };
            SetTransform(scn, newGameObject, Vector4.Zero);

            var characterSpawnControllerComponent = CreateComponent(scn, newGameObject, "chainsaw.CharacterSpawnPointController");
            characterSpawnControllerComponent.Set("v0", (byte)1);
            characterSpawnControllerComponent.Set("_DifficutyParam", 63U);
            characterSpawnControllerComponent.Set("_GUID", Guid.NewGuid());
            characterSpawnControllerComponent.Set("_ActiveCountLimit", 1);
            characterSpawnControllerComponent.Set("_ActiveCountType", 1);
            characterSpawnControllerComponent.Set("_IntervalTime", 1.0f);
            characterSpawnControllerComponent.Set("_SpawnDistanceMin", 5.0f);

            var spawnCondition = scn.RSZ!.CreateInstance("chainsaw.CharacterSpawnPointController.ImmediateSpawnCondition");
            spawnCondition.Set("SpawnCount", enemies.Length);
            characterSpawnControllerComponent.Set("_ImmediateSpawnConditionList", new List<object>() { spawnCondition });

            characterSpawnControllerComponent.Set("_SpawnPoints",
                enemies.Select(enemyDef =>
                {
                    var spawnPoint = scn.RSZ!.CreateInstance("chainsaw.CharacterSpawnPoint");
                    spawnPoint.Set("_Transform", CreateMatrix(new Vector3(enemyDef.X, enemyDef.Y, enemyDef.Z), enemyDef.Direction));
                    spawnPoint.Set("_IsOutOfCameraOnly", true);
                    spawnPoint.Set("_CoolDownTime", 3.0f);
                    return (object)spawnPoint;
                }).ToList());

            return newGameObject;
        }

        private static void AddSpawnControllerConditions(ScnFile scn, ScnFile.GameObjectData spawnController, string? condition, string? skipCondition)
        {
            if (!string.IsNullOrEmpty(condition))
            {
                spawnController.Components[1].Set("_SpawnCondition._Logic", 0);
                spawnController.Components[1].Set("_SpawnCondition._CheckFlags", new List<object>()
                    {
                        CreateCheckFlag(scn, new Guid(condition)),
                    });
            }
            if (!string.IsNullOrEmpty(skipCondition))
            {
                spawnController.Components[1].Set("_SpawnSkipCondition._Logic", 0);
                spawnController.Components[1].Set("_SpawnSkipCondition._CheckFlags", new List<object>()
                    {
                        CreateCheckFlag(scn, new Guid(skipCondition)),
                    });
            }
        }

        private ScnFile.GameObjectData CreateEnemy(ScnFile scn, ScnFile.GameObjectData parent, string name, int stageId, Vector3 position, Vector4 rotation, bool findPlayer, Rng rng, RandomizerLogger logger)
        {
            var newGameObject = scn.CreateGameObject(name);
            newGameObject.Prefab = new ScnFile.PrefabInfo()
            {
                Path = "_Chainsaw/AppSystem/Prefab/ch1c0SpawnParam.pfb"
            };
            SetTransform(scn, newGameObject, new Vector4(position, 1), rotation);
            scn.RemoveGameObject(newGameObject);
            newGameObject = scn.ImportGameObject(newGameObject, parent: parent);

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
            spawnParam.Set("_ForceFind", findPlayer);

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

        private static Vector4 RotationToQuaternion(float yaw, float pitch, float roll)
        {
            return RotationToQuaternion(new Vector3(yaw, pitch, roll));
        }

        private static Vector4 RotationToQuaternion(Vector3 euler)
        {
            // Convert degrees to radians
            var yawRad = MathF.PI * euler.Z / 180;
            var pitchRad = MathF.PI * euler.X / 180;
            var rollRad = MathF.PI * euler.Y / 180;

            // Calculate half angles
            var halfYaw = yawRad * 0.5f;
            var halfPitch = pitchRad * 0.5f;
            var halfRoll = rollRad * 0.5f;

            // Calculate the sine and cosine of the half angles
            var cy = MathF.Cos(halfYaw);
            var sy = MathF.Sin(halfYaw);
            var cp = MathF.Cos(halfPitch);
            var sp = MathF.Sin(halfPitch);
            var cr = MathF.Cos(halfRoll);
            var sr = MathF.Sin(halfRoll);

            // Calculate the quaternion components
            var w = cr * cp * cy + sr * sp * sy;
            var x = sr * cp * cy - cr * sp * sy;
            var y = cr * sp * cy + sr * cp * sy;
            var z = cr * cp * sy - sr * sp * cy;

            return new Vector4(x, y, z, w);
        }

        public static Vector3 QuaternionToEulerDegrees(Vector4 rotation)
        {
            var x = rotation.X;
            var y = rotation.Y;
            var z = rotation.Z;
            var w = rotation.W;

            var yaw = (float)Math.Atan2(2 * (y * w + x * z), 1 - 2 * (y * y + z * z));
            var pitch = (float)Math.Asin(2 * (y * z - x * w));
            var roll = (float)Math.Atan2(2 * (x * y + z * w), 1 - 2 * (x * x + y * y));

            var yawDegrees = RadToDeg(yaw);
            var pitchDegrees = RadToDeg(pitch);
            var rollDegrees = RadToDeg(roll);

            return new Vector3(yawDegrees, pitchDegrees, rollDegrees);

            static float RadToDeg(float radians) => radians * (180 / MathF.PI);
        }

        private static RszTool.via.mat4 CreateMatrix(Vector3 position, float direction)
        {
            var translate = Matrix4x4.CreateTranslation(position);
            var rotation = Matrix4x4.CreateFromYawPitchRoll(direction, 0, 0);
            var result = rotation * translate;

            var mat4 = new RszTool.via.mat4();
            mat4.m00 = result.M11;
            mat4.m10 = result.M21;
            mat4.m20 = result.M31;
            mat4.m30 = result.M41;
            mat4.m01 = result.M12;
            mat4.m11 = result.M22;
            mat4.m21 = result.M32;
            mat4.m31 = result.M42;
            mat4.m02 = result.M13;
            mat4.m12 = result.M23;
            mat4.m22 = result.M33;
            mat4.m32 = result.M43;
            mat4.m03 = result.M14;
            mat4.m13 = result.M24;
            mat4.m23 = result.M34;
            mat4.m33 = result.M44;
            return mat4;
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
