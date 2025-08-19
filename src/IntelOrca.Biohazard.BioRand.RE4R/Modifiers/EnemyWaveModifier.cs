using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Variables;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyWaveModifier : Modifier
    {
        private int _contextId = 9000;
        private List<Guid> _flagGuids = [];

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                var minWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-min", 1), 1, 10);
                var maxWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-max", 1), minWaves, 50);
                var waveDistance = Math.Clamp(randomizer.GetConfigOption<float>("enemy-waves-distance", 10), 1, 100);
                foreach (var area in randomizer.Areas)
                {
                    logger.Push(area.FileName);

                    var scn = area.ScnFile;
                    var spawns = area.GetEnemySpawns(randomizer);
                    foreach (var oldSpawn in spawns)
                    {
                        if (oldSpawn.PreventDuplicate)
                            continue;
                        if (!string.IsNullOrEmpty(oldSpawn.MiniBoss))
                            continue;
                        if (!oldSpawn.HasSimpleController)
                            continue;

                        var oldSpawnController = oldSpawn.Controller!;
                        var lastSpawn = oldSpawn;
                        var numWaves = rng.Next(minWaves, maxWaves + 1);
                        for (var i = 1; i < numWaves; i++)
                        {
                            var spawnControllerGameObject = CreateSpawnPointController(scn, $"BioRandOnDeathSpawn_{i}", rng.NextGuid(), waveDistance, [lastSpawn.Enemy]);
                            var spawnController = new CharacterSpawnController(spawnControllerGameObject.Components[1]);

                            var newSpawn = lastSpawn.Duplicate(GetNextContextId());
                            Reparent(newSpawn.Enemy.GameObject, spawnControllerGameObject);

                            var deathFlag = GetNextFlagGuid();
                            lastSpawn.Enemy.SetFieldValue("_DeathNotifyFlag", deathFlag);
                            spawnController.SpawnCondition.Add(scn, deathFlag);
                            spawnController.SpawnSkipCondition.Flags = oldSpawnController.SpawnSkipCondition.Flags;
                            spawnController.SpawnSkipCondition.Or = oldSpawnController.SpawnSkipCondition.Or;

                            newSpawn.Enemy.SetFieldValue("_ForceFind", true);

                            lastSpawn = newSpawn;
                        }
                    }

                    logger.Pop();
                }
                SetVariables(randomizer, logger);
            }
        }

        private int GetNextContextId()
        {
            return _contextId++;
        }

        private Guid GetNextFlagGuid()
        {
            var biorandFlagIndex = _flagGuids.Count;
            var guid = HashGuid($"BioRand_{biorandFlagIndex:00000}");
            _flagGuids.Add(guid);
            return guid;
        }

        private static Guid HashGuid(string s)
        {
            var hash = MD5.HashData(Encoding.ASCII.GetBytes(s));
            hash[8] = (byte)(0x40 | (hash[8] & 0x0F));
            return new Guid(hash);
        }

        private void SetVariables(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string variableTablePath = "natives/stm/_chainsaw/leveldesign/scenario/scenarioflag/tabledefine.user.2";
            const string globalVariablesPath = "natives/stm/_authoring/appsystem/globalvariables/globalvariables.uvar.3";

            var fileRepository = randomizer.FileRepository;

            // uvar
            var uvarBytes = fileRepository.GetGameFileData(globalVariablesPath) ?? throw new Exception();
            var uvar = new UvarFile(uvarBytes);

            var biorandGroup = new UvarFile.Builder(uvar.GetEmbedded(0)); // TODO improve API
            biorandGroup.Name = "BioRand";
            biorandGroup.Hash = MurMur3.HashData("BioRand");
            biorandGroup.Children.Clear();
            biorandGroup.Variables.Clear();

            var flagIndex = 0;
            foreach (var flagGuid in _flagGuids)
            {
                biorandGroup.Variables.Add(new UvarFile.Builder.Variable()
                {
                    Guid = flagGuid,
                    Name = $"BioRand_{flagIndex:00000}",
                    TypeVal = 2
                });
                flagIndex++;
                if (flagIndex >= 100000)
                    break;
            }

            var uvarBuilder = uvar.ToBuilder();
            uvarBuilder.Children.Add(biorandGroup);
            fileRepository.SetGameFileData(globalVariablesPath, uvarBuilder.Build().Data);

            // tabledefine
            var tableDefine = fileRepository.DeserializeUserFile<chainsaw.ScenarioFlagData>(variableTablePath);
            tableDefine.Datas.Add(new chainsaw.ScenarioFlagData.Data()
            {
                DataName = "BioRand",
                DigitNum = 0,
                DigitIndex = 5,
                Block = new List<chainsaw.ScenarioFlagData.Block>()
                {
                    new chainsaw.ScenarioFlagData.Block()
                    {
                        Group = 0,
                        Num = flagIndex,
                        ReadOnly = false,
                        ResetInNewGame = true
                    }
                }
            });
            fileRepository.SerializeUserFile(variableTablePath, tableDefine);
        }

        private static void Reparent(ScnFile.GameObjectData gameObject, ScnFile.GameObjectData newParent)
        {
            gameObject.Parent?.Children.Remove(gameObject);
            newParent.Children.Add(gameObject);
            gameObject.Parent = newParent;
        }

        private static ScnFile.GameObjectData CreateSpawnPointController(ScnFile scn, string name, Guid guid, float waveDistance, Enemy[] enemies)
        {
            var newGameObject = scn.CreateGameObject(name);
            newGameObject.Prefab = new ScnFile.PrefabInfo()
            {
                Path = "_Chainsaw/AppSystem/Prefab/CharacterSpawnPointController.pfb"
            };
            SetTransform(scn, newGameObject, Vector3.Zero);

            var characterSpawnControllerComponent = CreateComponent(scn, newGameObject, "chainsaw.CharacterSpawnPointController");
            characterSpawnControllerComponent.Set("v0", (byte)1);
            characterSpawnControllerComponent.Set("_DifficutyParam", 63U);
            characterSpawnControllerComponent.Set("_GUID", guid);
            characterSpawnControllerComponent.Set("_ActiveCountLimit", 100);
            characterSpawnControllerComponent.Set("_ActiveCountType", 0);
            characterSpawnControllerComponent.Set("_IntervalTime", 1.0f);
            characterSpawnControllerComponent.Set("_SpawnDistanceMin", waveDistance);

            characterSpawnControllerComponent.Set("_SpawnPoints",
                enemies.Select(enemyDef =>
                {
                    var transform = new Transform(GetOrCreateComponent(scn, enemyDef.GameObject, "via.Transform"));
                    var spawnPoint = scn.RSZ!.CreateInstance("chainsaw.CharacterSpawnPoint");
                    spawnPoint.Set("_Transform", transform.Matrix);
                    spawnPoint.Set("_IsOutOfCameraOnly", true);
                    spawnPoint.Set("_CoolDownTime", 3.0f);
                    return (object)spawnPoint;
                }).ToList());

            return newGameObject;
        }

        private static void SetTransform(ScnFile scn, ScnFile.GameObjectData gameObject, Vector3 position, EulerAngles? eular = null)
        {
            var transform = new Transform(GetOrCreateComponent(scn, gameObject, "via.Transform"));
            transform.Position = position;
            transform.Eular = eular ?? new EulerAngles();
            transform.Scale = Vector3.One;
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
