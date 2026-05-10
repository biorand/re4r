using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Messages;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    [Order(ModifierOrders.Message)]
    internal class MessageModifier : Modifier
    {
        public override void LogState(IReeRandomizerContext context, RandomizerLogger logger)
        {
            var randomizer = (ChainsawRandomizer)context;
            foreach (var msgPath in g_msgPaths)
            {
                logger.Push(msgPath);
                var msgFile = randomizer.FileRepository.GetMsgFile(msgPath);
                var count = msgFile.Count;
                for (var i = 0; i < count; i++)
                {
                    var msg = msgFile.GetMessage(i);
                    logger.LogLine(msg.Guid, msg.Name, '"' + msg[LanguageId.English] + '"');
                }
                logger.Pop();
            }
        }

        public override void Apply(IReeRandomizerContext context, RandomizerLogger logger)
        {
            var randomizer = (ChainsawRandomizer)context;
            if (!randomizer.GetConfigOption("randomized-messages", false))
                return;

            var nameToPath = GetNameToMsgFileMap(randomizer.FileRepository);
            var mappings = GetChosenMessages();
            var filesToModify = mappings.Keys
                .Where(nameToPath.ContainsKey)
                .GroupBy(x => nameToPath[x])
                .Select(x => x.Key)
                .ToArray();
            foreach (var filePath in filesToModify)
            {
                randomizer.FileRepository.ModifyMsgFile(filePath, msgFile =>
                {
                    foreach (var msg in msgFile.Messages)
                    {
                        if (mappings.TryGetValue(msg.Name, out var text))
                        {
                            for (int i = 0; i < msg.Values.Count; i++)
                            {
                                msg.Values[i] = new MsgValue(msg.Values[i].Language, text);
                            }
                        }
                    }
                });
            }

            Dictionary<string, string> GetChosenMessages()
            {
                var allMessages = GetAllMessages();
                var result = new Dictionary<string, string>();
                foreach (var msg in allMessages)
                {
                    var rng = randomizer.GetRng($"message/{msg.Key}");
                    result[msg.Key] = ReplaceVariables(rng.Next(msg.Value));
                }
                return result;
            }

            string ReplaceVariables(string input)
            {
                input = input.Replace("${seed}", randomizer.Seed.ToString());
                input = input.Replace("${user.name}", randomizer.User);
                input = input.Replace("${profile.name}", randomizer.Input.ProfileName);
                input = input.Replace("${profile.author}", randomizer.Input.ProfileAuthor);
                input = input.Replace("${profile.description}", randomizer.Input.ProfileDescription);
                return input;
            }

            Dictionary<string, List<string>> GetAllMessages()
            {
                var result = new Dictionary<string, List<string>>();
                var csv = Csv.Deserialize<MessageSheetEntry>(randomizer.DynamicData.GetData(DynamicDataName.Messages)!);
                var names = new List<string>();
                foreach (var row in csv)
                {
                    if (!string.IsNullOrEmpty(row.Name1) || !string.IsNullOrEmpty(row.Name2))
                    {
                        names.Clear();
                        if (!string.IsNullOrEmpty(row.Name1))
                            names.Add(row.Name1);
                        if (!string.IsNullOrEmpty(row.Name2))
                            names.Add(row.Name2);
                    }

                    if (string.IsNullOrWhiteSpace(row.English))
                        continue;

                    foreach (var name in names)
                    {
                        result.TryGetValue(name, out var list);
                        if (list == null)
                        {
                            list = new List<string>();
                            result[name] = list;
                        }
                        list.Add(row.English);
                    }
                }
                return result;
            }
        }

        private class MessageSheetEntry
        {
            public string Name1 { get; set; } = "";
            public string Name2 { get; set; } = "";
            public string English { get; set; } = "";
        }

        private static Dictionary<string, string> GetNameToMsgFileMap(IReeRandomizerContext context)
        {
            var result = new Dictionary<string, string>();
            foreach (var msgPath in g_msgPaths)
            {
                var msgFile = context.GetMsgFile(msgPath);
                var count = msgFile.Count;
                for (var i = 0; i < count; i++)
                {
                    var msg = msgFile.GetMessage(i);
                    result[msg.Name] = msgPath;
                }
            }
            return result;
        }

        private static ImmutableArray<string> g_msgPaths = [
            "natives/stm/_anotherorder/message/mes_main_accessory/ao_mes_main_accessory.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp01.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp11.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp31.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp32.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp41.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_bino_cp51.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_merchant.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_questconv.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_radio.msg.22",
            "natives/stm/_anotherorder/message/mes_main_conv/ao_mes_main_resultconv.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_caption_changed.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_caption.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_name_changed.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_name.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_search_specific.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_item_search.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_itemperks.msg.22",
            "natives/stm/_anotherorder/message/mes_main_item/ao_mes_main_wpcustom.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_action.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_activity.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_bonus.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_common.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_costume.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_file.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_generalguide.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_gimmickui.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_hud.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_inventory.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_log.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_mainmenu.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_map.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_mapgimmick.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_mapname.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_network.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_option.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_pc.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_purpose.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_result.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_richtutorial.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_saveload.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_shop.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_shopitem.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_subprogress.msg.22",
            "natives/stm/_anotherorder/message/mes_main_sys/ao_mes_main_sys_tutorial.msg.22",
            "natives/stm/_anotherorder/message/mes_main_tips/ao_mes_main_tips.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_gameover.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_hud.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_inventory.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_menu.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_mercenaries.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_pcoption.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_photomode.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_ranking.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_saveload.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_sound.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_startup.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_storeanddlc.msg.22",
            "natives/stm/_chainsaw/message/dev1_term/dev1_term_zz_other.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1001.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1002.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1003.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1004.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1005.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1006.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1101.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1102.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1201.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1202.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1301.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1401.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_1402.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_2001.msg.22",
            "natives/stm/_chainsaw/message/dlc/ch_mes_dlc_3101.msg.22",
            "natives/stm/_chainsaw/message/mes_develop_misc/ch_mes_develop_ao.msg.22",
            "natives/stm/_chainsaw/message/mes_develop_misc/ch_mes_develop_sys_map.msg.22",
            "natives/stm/_chainsaw/message/mes_main_accessory/ch_mes_main_accessory.msg.22",
            "natives/stm/_chainsaw/message/mes_main_charm/ch_mes_main_statuseffect.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_bino_cp13.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_bino_cp31.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_bino_cp51.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_bino_cp53.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_merchant.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_questconv.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_radio.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_resultconv.msg.22",
            "natives/stm/_chainsaw/message/mes_main_conv/ch_mes_main_shootinggallery.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption_changed.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name_changed.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_itemperks.msg.22",
            "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22",
            "natives/stm/_chainsaw/message/mes_main_shop/ch_mes_main_shopitem_caption.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_action.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_activity.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_bonus.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_common.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_costume.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_file.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_generalguide.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_gimmickui.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_hud.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_inventory.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_log.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_mainmenu.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_map.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_mapgimmick.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_mapname.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_pc.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_photomode.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_purpose.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_result.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_richtutorial.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_saveload.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shootinggallery.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_steamhard.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_subprogress.msg.22",
            "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_tutorial.msg.22",
            "natives/stm/_chainsaw/message/mes_main_tips/ch_mes_main_tips.msg.22",
            "natives/stm/_chainsaw/message/tu/mes_tu0001_sys.msg.22",
            "natives/stm/_chainsaw/message/tu/mes_tu0002_sys.msg.22",
            "natives/stm/_chainsaw/message/tu/mes_tu0003_sys.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_caption_changed.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_caption_misc.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_caption.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_name_changed.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_name_misc.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_name.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_search_specific.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_item_search.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_itemperks.msg.22",
            "natives/stm/_mercenaries/message/mes_main_item/mc_mes_main_wpcustom.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_activity.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_bonus.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_common.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_generalguide.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_hud.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_mainmenu.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_misc.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_network.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_option.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_pc.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_purpose.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_richtutorial.msg.22",
            "natives/stm/_mercenaries/message/mes_main_sys/mc_mes_main_sys_tutorial.msg.22",
            "natives/stm/_mercenaries/message/mes_main_tips/mc_mes_main_tips.msg.22"
        ];
    }
}
