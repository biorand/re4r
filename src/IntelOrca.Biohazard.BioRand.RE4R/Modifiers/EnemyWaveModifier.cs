using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Variables;

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
                var minWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-min", 2), 2, 50);
                var maxWaves = Math.Clamp(randomizer.GetConfigOption("enemy-waves-max", 2), minWaves, 50);
                var waveDistance = Math.Clamp(randomizer.GetConfigOption<float>("enemy-waves-distance", 10), 1, 100);

                var waveProbability = Math.Clamp(randomizer.GetConfigOption<float>("enemy-waves-probability", 1), 0, 1);
                var allSpawns = randomizer.Areas
                    .SelectMany(x => x.Enemies)
                    .Shuffle(rng);

                var maxWavedEnemies = (int)(waveProbability * allSpawns.Length);
                var numWavedEnemies = 0;
                foreach (var oldSpawn in allSpawns)
                {
                    if (numWavedEnemies >= maxWavedEnemies)
                        break;
                    if (oldSpawn.PreventDuplicate)
                        continue;
                    if (!string.IsNullOrEmpty(oldSpawn.MiniBoss))
                        continue;
                    if (!oldSpawn.HasSimpleController)
                        continue;

                    var area = oldSpawn.Area;
                    var scn = oldSpawn.Area.ScnFile;
                    var oldSpawnController = oldSpawn.SpawnController;
                    var lastSpawn = oldSpawn;
                    var numWaves = rng.Next(minWaves, maxWaves + 1);
                    for (var i = 1; i < numWaves; i++)
                    {
                        var spawnControllerGameObject = RszFactory.CreateSpawnPointController(rng.NextGuid(), $"BioRandOnDeathSpawn_{i}", waveDistance, [lastSpawn.Enemy]);
                        var spawnController = area.CreateSpawnController(spawnControllerGameObject);

                        var newSpawn = lastSpawn.Duplicate(GetNextContextId());
                        spawnControllerGameObject = spawnControllerGameObject.AddOrUpdateChild(newSpawn.Enemy.GameObject);

                        var deathFlag = GetNextFlagGuid();
                        lastSpawn.Enemy.SetFieldValue("_DeathNotifyFlag", deathFlag);
                        spawnController.SpawnCondition.Add(deathFlag);
                        spawnController.SpawnSkipCondition.Flags = oldSpawnController.SpawnSkipCondition.Flags;
                        spawnController.SpawnSkipCondition.Or = oldSpawnController.SpawnSkipCondition.Or;

                        newSpawn.Enemy.SetFieldValue("_ForceFind", true);

                        lastSpawn = newSpawn;
                    }

                    numWavedEnemies++;
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
    }
}
