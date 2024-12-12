using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class CasePerkModifier : Modifier
    {
        private static string[] _leonPaths = [
            "natives/stm/_chainsaw/appsystem/ui/userdata/attachecaseskineffectsettinguserdata.user.2",
            "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1102/attachecaseskineffectsettinguserdata_dlc_1102.user.2",
            "natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1101/attachecaseskineffectsettinguserdata_dlc_1101.user.2"];
        private static string[] _adaPaths = [
            "natives/stm/_anotherorder/appsystem/ui/userdata/attachecaseskineffectsettinguserdata_ao.user.2"
        ];
        private static string[] _msgPaths = [
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1101.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1102.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_caption.msg.22"
        ];
        private static (int ItemId, Guid Guid, string Caption)[] _caseMessages = [
            (124176000, new Guid("8b0a0a1e-9ac0-4e9a-b4c1-fe27f91ebb84"), "A metallic silver attaché case."),
            (124177600, new Guid("f27f936d-42d6-4cfd-bee1-45155ed0d84a"), "A metallic black attaché case."),
            (124179200, new Guid("794cdbb8-829b-4ff7-bb66-a638a65f4603"), "A leather attaché case."),
            (123684800, new Guid("4e14ee31-3411-4b56-a0b0-53348944bc62"), "A carbon fiber attaché case."),
            (123686400, new Guid("d5a612c7-d9c3-425a-8235-095dd080f31f"), "A vintage attaché case."),
            (124192000, new Guid("547ad9d0-bf0b-4875-ae12-c4e59ec7e1a2"), "A metallic gold attaché case."),
            (124193600, new Guid("c7d6aed7-d48e-44ef-a3d5-72a5087a2f54"), "A classic leather attaché case.")
        ];

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var userDataPaths = randomizer.Campaign == Campaign.Leon ? _leonPaths : _adaPaths;
            foreach (var userDataPath in userDataPaths)
            {
                var userData = randomizer.FileRepository.DeserializeUserFile<chainsaw.AttacheCaseSkinEffectSettingUserdata>(userDataPath);
                foreach (var suitcase in userData._Settings)
                {
                    var itemDefinition = itemRepo.Find(suitcase._ItemId);
                    if (itemDefinition == null)
                        continue;

                    logger.Push($"{itemDefinition.Name}");
                    foreach (var effect in suitcase._Effects)
                    {
                        var casePerk = CasePerks.Default.FromStatusEffectId(effect._StatusEffectID);
                        var description = casePerk == null
                            ? effect._StatusEffectID.ToString()
                            : string.Format(casePerk.Description, Math.Abs(effect._Value));
                        logger.LogLine($"Effect {description} Value = {effect._Value}");
                    }
                    logger.Pop();
                }
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();
            if (!randomizer.GetConfigOption<bool>("random-case-perks"))
                return;

            var itemRepo = ItemDefinitionRepository.Default;
            var userDataPaths = randomizer.Campaign == Campaign.Leon ? _leonPaths : _adaPaths;
            var availablePerks = CasePerks.Default.All
                .Where(x => x.Enabled != 0)
                .Shuffle(rng)
                .ToQueue();

            var msgDict = new Dictionary<Guid, string>();
            foreach (var userDataPath in userDataPaths)
            {
                var userData = randomizer.FileRepository.DeserializeUserFile<chainsaw.AttacheCaseSkinEffectSettingUserdata>(userDataPath);
                foreach (var suitcase in userData._Settings)
                {
                    var itemDefinition = itemRepo.Find(suitcase._ItemId);
                    if (itemDefinition == null)
                        continue;

                    var perk = availablePerks.Dequeue();
                    var value = rng.Next(perk.Min, perk.Max + 1);
                    suitcase._Effects = [
                        new StatusEffectSetting()
                        {
                            _StatusEffectID = perk.Id,
                            _Value = value
                        }
                    ];

                    var caseMessage = _caseMessages.FirstOrDefault(x => x.ItemId == suitcase._ItemId);
                    if (caseMessage != default)
                    {
                        msgDict[caseMessage.Guid] = $"{caseMessage.Caption} {string.Format(perk.Description, Math.Abs(value))}";
                    }
                }
                randomizer.FileRepository.SerializeUserFile(userDataPath, userData);
            }

            foreach (var msgPath in _msgPaths)
            {
                var msg = randomizer.FileRepository.GetMsgFile(msgPath).ToBuilder();
                var sv = false;
                foreach (var kvp in msgDict)
                {
                    var entry = msg.Entries.FirstOrDefault(x => x.Guid == kvp.Key);
                    if (entry != null)
                    {
                        msg.SetStringAll(kvp.Key, kvp.Value);
                        sv = true;
                    }
                }
                if (sv)
                {
                    randomizer.FileRepository.SetMsgFile(msgPath, msg.ToMsg());
                }
            }
        }

        private class CasePerks
        {
            public static CasePerks Default { get; } = new CasePerks();

            public ImmutableArray<CasePerk> All { get; } = [];

            private CasePerks()
            {
                var data = EmbeddedData.GetFile("case_perks.csv");
                All = [.. Csv.Deserialize<CasePerk>(data)];
            }

            public CasePerk? FromStatusEffectId(int id)
            {
                return All.FirstOrDefault(x => x.Id == id);
            }
        }

        private class CasePerk
        {
            public int Id { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
            public int Enabled { get; set; }
            public string Description { get; set; } = "";
        }
    }
}
