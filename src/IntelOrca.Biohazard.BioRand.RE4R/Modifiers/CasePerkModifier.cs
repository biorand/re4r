using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;

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
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_caption.msg.22",
            "natives/stm/_chainsaw/message/mes_main_charm/ch_mes_main_statuseffect.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_common.msg.22"
        ];
        private static (int ItemId, Guid CaptionMsgGuid, Guid StatusMsgGuid, string Caption)[] _caseMessages = [
            (124176000, new Guid("8b0a0a1e-9ac0-4e9a-b4c1-fe27f91ebb84"), new Guid("c5760a3e-33d6-4258-b1ca-54e98fc6c4d7"), "A metallic silver attaché case."),
            (124177600, new Guid("f27f936d-42d6-4cfd-bee1-45155ed0d84a"), new Guid("191c5f4f-0fb1-4b34-b905-aa9792a8ed2b"), "A metallic black attaché case."),
            (124179200, new Guid("794cdbb8-829b-4ff7-bb66-a638a65f4603"), new Guid("22d159ca-7916-4953-a18d-4a6572890c28"), "A leather attaché case."),
            (123684800, new Guid("4e14ee31-3411-4b56-a0b0-53348944bc62"), new Guid("7bc7769a-5ac8-417f-88d0-6d02d942b7cd"), "A carbon fiber attaché case."),
            (123686400, new Guid("d5a612c7-d9c3-425a-8235-095dd080f31f"), new Guid("c5d15e6c-8e50-45ee-b329-1a3703c7fec5"), "A vintage attaché case."),
            (124192000, new Guid("547ad9d0-bf0b-4875-ae12-c4e59ec7e1a2"), new Guid("7185d3d0-dbb3-4b1e-92c5-1d266379e098"), "A metallic gold attaché case."),
            (124193600, new Guid("c7d6aed7-d48e-44ef-a3d5-72a5087a2f54"), new Guid("cbf63460-c3b7-4547-ba68-636938ecca2c"), "A classic leather attaché case.")
        ];
        private static int[] _startingCases = [124176000, 124192000, 124193600];

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
            var availablePerks = new WeightTable<CasePerk>(rng, CasePerks.Default.All
                .Where(x => x.Enabled != 0)
                .Select(x => new WeightTableEntry<CasePerk>(x, x.Weight)));

            var msgDict = new Dictionary<Guid, string>();
            foreach (var userDataPath in userDataPaths)
            {
                var userData = randomizer.FileRepository.DeserializeUserFile<chainsaw.AttacheCaseSkinEffectSettingUserdata>(userDataPath);
                foreach (var suitcase in userData._Settings)
                {
                    var itemDefinition = itemRepo.Find(suitcase._ItemId);
                    if (itemDefinition == null)
                        continue;

                    var isStartingCase = _startingCases.Contains(suitcase._ItemId);
                    var minPerks = isStartingCase ? 1 : 2;
                    var maxPerks = isStartingCase ? 1 : 3;
                    var numPerks = rng.Next(minPerks, maxPerks + 1);
                    var perks = new List<CasePerkWithValue>();
                    for (var i = 0; i < numPerks; i++)
                    {
                        var perk = availablePerks.Next();
                        var isGodValue = !isStartingCase && rng.NextProbability(10);
                        var perkValue = isGodValue
                            ? rng.Next(perk.GodMin, perk.GodMax + 1)
                            : rng.Next(perk.Min, perk.Max + 1);
                        perks.Add(new CasePerkWithValue()
                        {
                            Perk = perk,
                            Value = perkValue
                        });
                    }

                    suitcase._Effects[0]._Value = 0;
                    suitcase._Effects.AddRange(perks.Select(x => x.StatusEffectSetting));
                    var caseMessage = _caseMessages.FirstOrDefault(x => x.ItemId == suitcase._ItemId);
                    if (caseMessage != default)
                    {
                        msgDict[caseMessage.CaptionMsgGuid] = caseMessage.Caption + "\r\n" +
                            string.Join("\r\n", perks.Select(x => $"    {x.Description}"));
                    }
                    msgDict[caseMessage.StatusMsgGuid] = string.Join("\r\n", perks.Select(x => x.Description));
                }
                randomizer.FileRepository.SerializeUserFile(userDataPath, userData);
            }

            foreach (var msgPath in _msgPaths)
            {
                var msg = randomizer.FileRepository.GetMsgFile(msgPath).ToBuilder();
                var sv = false;
                foreach (var kvp in msgDict)
                {
                    var entry = msg.FindMessage(kvp.Key);
                    if (entry != null)
                    {
                        msg.SetStringAll(kvp.Key, kvp.Value);
                        sv = true;
                    }
                }
                if (sv)
                {
                    randomizer.FileRepository.SetMsgFile(msgPath, msg.Build());
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
            public double Weight { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
            public int GodMin { get; set; }
            public int GodMax { get; set; }
            public int Enabled { get; set; }
            public string Description { get; set; } = "";
        }

        private class CasePerkWithValue
        {
            public required CasePerk Perk { get; init; }
            public required int Value { get; init; }

            public string Description => string.Format(Perk.Description, Math.Abs(Value));
            public StatusEffectSetting StatusEffectSetting => new StatusEffectSetting()
            {
                _StatusEffectID = Perk.Id,
                _Value = Value
            };
        }

        private class WeightTable<T>(Rng rng, IEnumerable<WeightTableEntry<T>> entries)
        {
            public List<WeightTableEntry<T>> Entries = entries.ToList();

            public T Next()
            {
                var sum = Entries.Sum(x => x.Weight);
                if (sum <= 0)
                {
                    foreach (var e in Entries)
                    {
                        e.Weight = e.OriginalWeight;
                    }
                    sum = Entries.Sum(x => x.Weight);
                }
                if (sum <= 0)
                    throw new Exception("Weight <= 0");

                var rValue = rng.NextDouble(0, sum);
                var rCurrent = 0.0;
                foreach (var e in Entries)
                {
                    if (e.Weight <= 0)
                        continue;

                    var rNext = rCurrent + e.Weight;
                    if (rValue <= rNext)
                    {
                        e.Weight = 0;
                        return e.Value;
                    }
                    rCurrent += e.Weight;
                }
                throw new Exception();
            }
        }

        private class WeightTableEntry<T>(T value, double weight)
        {
            public T Value => value;
            public double OriginalWeight => weight;
            public double Weight { get; set; }
        }
    }
}
