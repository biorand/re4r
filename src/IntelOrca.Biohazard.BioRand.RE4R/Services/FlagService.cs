using System;
using System.Collections.Generic;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Variables;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class FlagService(ChainsawRandomizer randomizer)
    {
        private SortedDictionary<(int, int), int> _contextIdNum = new();
        private List<Guid> _flagGuids = [];

        public Guid AllocateFlag()
        {
            var biorandFlagIndex = _flagGuids.Count;
            var guid = $"BioRand_{biorandFlagIndex:00000}".GetGuidHash();
            _flagGuids.Add(guid);
            return guid;
        }

        public chainsaw.ContextID AllocateContextId(int category, int group, int offset = 0)
        {
            _contextIdNum.TryGetValue((category, group), out var num);
            var result = new ContextID()
            {
                _Category = (sbyte)category,
                _Kind = 0,
                _Group = group,
                _Index = num + offset
            };
            _contextIdNum[(category, group)] = num + 1;
            return result;
        }

        public void Save(RandomizerLogger logger)
        {
            const string variableTablePath = "natives/stm/_chainsaw/leveldesign/scenario/scenarioflag/tabledefine.user.2";
            const string globalVariablesPath = "natives/stm/_authoring/appsystem/globalvariables/globalvariables.uvar.3";

            var fileRepository = randomizer.FileRepository;

            // uvar
            var uvarBytes = fileRepository.GetFile(globalVariablesPath) ?? throw new Exception();
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
            fileRepository.SetFile(globalVariablesPath, uvarBuilder.Build().Data);

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
