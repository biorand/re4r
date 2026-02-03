#if ENABLE_BETA_FEATURES
using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "removeradiocalls",
        Name = "Remove Radio Calls",
        Description = "Removes all radio calls from the game.",
        Version = "1.0",
        Author = "AfkKun")]
    internal class RemoveRadioCallsPatch(IPatchContext context) : IPatch
    {
        private static readonly ImmutableArray<(string, Guid, Guid)> _mainEntries = [
            //Radiomsg 1+2
            ("natives/stm/_chainsaw/leveldesign/location/loc40/level_loc40_100.scn.20", new Guid("e7fb21fb-65db-40d6-9ef4-acfbd3fe08a0"), new Guid("cfa6f539-d433-4964-933d-0ebe712efbd0")),
            ("natives/stm/_chainsaw/leveldesign/location/loc40/level_loc40_100.scn.20", new Guid("83d4fbf2-33f2-40cf-99c8-b55f6962de73"), new Guid("e33043ff-a39d-406e-895b-8336b7fb27fd")),
            //Radiomsg 3
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_020.scn.20", new Guid("b9b57035-e7d8-4f5f-bf68-04de5a3f0a24"), new Guid("65570892-69c3-42c2-9a9d-97421aeb2700")),
            //Radiomsg 4 
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_2/level_cp10_chp1_2.scn.20", new Guid("d37d03da-7500-4297-850d-537624c1b925"), new Guid("a8510e94-1ecf-46ef-bbd0-cf592be1a370")),
            //Radiomsg 5+6  Start of CH3 + At first Church visit
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_3/level_cp10_chp1_3.scn.20", new Guid("7c73577d-7381-4226-8ded-4580e968341d"), new Guid("72fc4d29-b612-42c0-bb9f-f1623dd5cb50")),
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_3/level_cp10_chp1_3.scn.20", new Guid("5c984a33-bf76-412f-a798-c2582fe8b32f"), new Guid("245fb3d0-5d6d-4790-88a3-dd29c9753b60")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp1_3/sound_cp10_chp1_3_level.scn.20", new Guid("7c73577d-7381-4226-8ded-4580e968341d"), new Guid("72fc4d29-b612-42c0-bb9f-f1623dd5cb50")),
            //Radiomsg 7+8 After DeLago + After inserting both heads
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_1/level_cp10_chp2_1.scn.20", new Guid("8f90e599-9205-4e47-bd3c-36bb973b890d"), new Guid("3acd39e5-1c4d-41d6-989b-bafede2f91d5")),
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_1/level_cp10_chp2_1.scn.20", new Guid("07cc420c-d70f-4f7c-aa17-7039518af24e"), new Guid("6666a6eb-2ab3-4a7b-83fe-3372826d2b7c")),
            //Radiomsg 9   Escaping Church with ashley
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_2/level_cp10_chp2_2.scn.20", new Guid("872e792a-62a6-4542-90fb-d3fb9a0d1402"), new Guid("da96fc08-dd42-409f-b562-d8fbc178370b")),
            //Radiomsg 10   After cabin fight
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_3/level_cp10_chp2_3.scn.20", new Guid("72944c8f-3dee-4c9c-aa1e-5080049406c3"), new Guid("709f7fe5-75b5-4099-b2cb-b553ee4d5cce")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp2_3/sound_cp10_chp2_3_level.scn.20", new Guid("72944c8f-3dee-4c9c-aa1e-5080049406c3"), new Guid("709f7fe5-75b5-4099-b2cb-b553ee4d5cce")),
            //Radiomsg 11   Beginning of Castle CH7
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp3_1/level_cp10_chp3_1.scn.20", new Guid("bd92e730-c53b-43cc-af3c-704cb66c4a8d"), new Guid("d45b8983-f094-418d-beed-daa68a5376f6")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp3_1/sound_cp10_chp3_1_level.scn.20", new Guid("bd92e730-c53b-43cc-af3c-704cb66c4a8d"), new Guid("d45b8983-f094-418d-beed-daa68a5376f6")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st47/gimmick_st47_901.scn.20", new Guid("bd92e730-c53b-43cc-af3c-704cb66c4a8d"), new Guid("d45b8983-f094-418d-beed-daa68a5376f6")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_100.scn.20", new Guid("bd92e730-c53b-43cc-af3c-704cb66c4a8d"), new Guid("d45b8983-f094-418d-beed-daa68a5376f6")),
            //Radiomsg 12  After Waterhall
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp3_2/level_cp10_chp3_2.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp3_2/sound_cp10_chp3_2_level.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st47/gimmick_st47_901.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_900.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_601.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_100.scn.20", new Guid("2ab5d365-1517-4cb3-a7bd-33b69b502583"), new Guid("6f573200-a60a-4b97-9f52-407d17afd064")),
            //Radiomsg 13  At maze after meeting ashley again
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp3_3/level_cp10_chp3_3.scn.20", new Guid("826c9324-31ac-4fd3-bf27-c8d0a24f3638"), new Guid("cc044e73-6ec5-4919-bcd4-5afd6502efbb")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st47/gimmick_st47_901.scn.20", new Guid("826c9324-31ac-4fd3-bf27-c8d0a24f3638"), new Guid("cc044e73-6ec5-4919-bcd4-5afd6502efbb")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_100.scn.20", new Guid("826c9324-31ac-4fd3-bf27-c8d0a24f3638"), new Guid("cc044e73-6ec5-4919-bcd4-5afd6502efbb")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900.scn.20", new Guid("826c9324-31ac-4fd3-bf27-c8d0a24f3638"), new Guid("cc044e73-6ec5-4919-bcd4-5afd6502efbb")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900_savedata.user.2", new Guid("826c9324-31ac-4fd3-bf27-c8d0a24f3638"), new Guid("cc044e73-6ec5-4919-bcd4-5afd6502efbb")),
            //Radiomsg 14 In Cage after ashley kidnapped
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp4_1/level_cp10_chp4_1_000.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp4_1/sound_cp10_chp4_1_level.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st47/gimmick_st47_901.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_100.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_401.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_202.scn.20", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900_savedata.user.2", new Guid("6899d75f-fc1b-4a48-a978-5577ed0c1ec9"), new Guid("6bc8771a-b201-47a8-9454-ed066cadf581")),
            //Radiomsg 15  Elevator after Krauser Knife Fight
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp4_3/level_cp10_chp4_3.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_202.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/leveldesign/location/loc51/level_loc51_001.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            //("natives/stm/_chainsaw/leveldesign/location/loc51/level_loc51_002.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/leveldesign/location/loc53/level_loc53_003.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/sound/scene/chapter/cp10_chp4_3/sound_cp10_chp4_3_level.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            //("natives/stm/_chainsaw/sound/scene/location/loc51/sound_loc51_level.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st54/gimmick_st54_900.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_900.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st47/gimmick_st47_901.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_601.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st50/gimmick_st50_100.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_200_p000.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st53/gimmick_st53_200_p000.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st53/gimmick_st53_202_p000.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_101.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_401.scn.20", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_200_p000_savedata.user.2", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st51/gimmick_st51_900_savedata.user.2", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            ("natives/stm/_chainsaw/environment/scene/gimmick/st52/gimmick_st52_101_savedata.user.2", new Guid("6f0b532b-d9e6-4904-b068-0d6458cda383"), new Guid("0d2fcb87-de99-4bb2-96a7-7db12a48eb00")),
            //Radiomsg 16  After rescuing Ashley with lv3 Keycard
            ("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp5_2/level_cp10_chp5_2.scn.20", new Guid("1c9b3bda-4029-4af3-abca-c0bb85f7f249"), new Guid("afba3822-37c8-4688-ba54-82717f4c17bd"))
        ];

        private static readonly ImmutableArray<(string, Guid, Guid)> _swEntries = [
            //Radiomsg 2
            ("natives/stm/_anotherorder/leveldesign/chapter/cp11_chp1_1/level_cp11_chp1_1.scn.20", new Guid("e9527445-1a28-4feb-9e69-6a2ff464f908"), new Guid("90d9a767-7fa8-4445-8830-e766a5e4aced")),
            //Radiomsg 3
            ("natives/stm/_anotherorder/leveldesign/chapter/cp11_chp1_2/level_cp11_chp1_2.scn.20", new Guid("bdeda832-3341-48be-b89a-f477628adfcc"), new Guid("221226c4-3e00-445d-9e4b-a3ff9d135b03")),
            //Radiomsg 6
            ("natives/stm/_anotherorder/leveldesign/chapter/cp11_chp2_1/level_cp11_chp2_1.scn.20", new Guid("fb9c97f9-37cf-4b28-9233-c401d8a04ee4"), new Guid("4db1e529-89af-4888-b169-811f13c1c00f")),
            //Radiomsg 7 
            ("natives/stm/_anotherorder/leveldesign/chapter/cp11_chp2_2/level_cp11_chp2_2.scn.20", new Guid("9d2048c7-c8c4-4d3b-be5f-8f1eeb384e0b"), new Guid("3b2455f5-5b63-4ad6-96a2-42155f0bf191")),
            //Radiomsg 10 
            ("natives/stm/_anotherorder/leveldesign/location/loc51/level_loc51_chp3_1.scn.20", new Guid("d30153b1-6752-4089-b748-6d0d2df07990"), new Guid("3ca9a255-00d3-44cc-ba9a-835b47192b0e")),
            ("natives/stm/_anotherorder/sound/scene/chapter/cp11_chp3_1/sound_cp11_chp3_1_level.scn.20", new Guid("d30153b1-6752-4089-b748-6d0d2df07990"), new Guid("3ca9a255-00d3-44cc-ba9a-835b47192b0e")),
            ("natives/stm/_anotherorder/environment/scene/gimmick/st51/gimmick_st51_300_ao.scn.20", new Guid("d30153b1-6752-4089-b748-6d0d2df07990"), new Guid("3ca9a255-00d3-44cc-ba9a-835b47192b0e")),
            //Radiomsg 12
            ("natives/stm/_anotherorder/leveldesign/location/loc56/level_loc56.scn.20", new Guid("0ad6f369-04a9-4d58-bd95-15b71db2366a"), new Guid("c12da548-f050-49db-8c61-cbd15ee77f7f")),
            //Radiomsg 13
            ("natives/stm/_anotherorder/leveldesign/location/loc55/level_loc55.scn.20", new Guid("10b4126f-38bb-43d5-b919-33ce75b7f9c2"), new Guid("49c18da1-9ce5-4766-8368-f3bd217c0b51")),
            ("natives/stm/_anotherorder/sound/scene/chapter/cp11_chp3_2/sound_cp11_chp3_2_level.scn.20", new Guid("10b4126f-38bb-43d5-b919-33ce75b7f9c2"), new Guid("49c18da1-9ce5-4766-8368-f3bd217c0b51")),
            //Radiomsg 16
            ("natives/stm/_anotherorder/leveldesign/chapter/cp11_chp4_1/level_cp11_chp4_1.scn.20", new Guid("76edc9e4-5df7-4388-abfb-d300d9c9708d"), new Guid("aa1bc9fd-bf6a-44a7-8620-b4bb34872917"))
        ];

        public void Apply()
        {
            if (!context.ExportingMod && !context.GetConfigOption<bool>("disable-radio-calls"))
                return;

            Apply(_mainEntries);
            Apply(_swEntries);
        }

        private void Apply(ImmutableArray<(string, Guid, Guid)> entries)
        {
            foreach (var entry in entries.GroupBy(x => x.Item1))
            {
                var path = entry.Key;
                if (path.EndsWith(".user.2"))
                {
                    context.ModifyUserFile(path, root =>
                    {
                        foreach (var replacement in entry)
                        {
                            root = root.Visit(node =>
                            {
                                if (node is RszObjectNode objectNode && objectNode.Type.Name == "chainsaw.CheckFlagInfo")
                                {
                                    if (objectNode.Get<Guid>("_CheckFlag") == replacement.Item2)
                                    {
                                        return objectNode.Set("_CheckFlag", replacement.Item3);
                                    }
                                }
                                return node;
                            });
                        }
                        return root;
                    });
                }
                else
                {
                    context.ModifyScnFile(path, root =>
                    {
                        foreach (var replacement in entry)
                        {
                            root = SearchAndReplaceFlag(root, replacement.Item2, replacement.Item3);
                        }
                        return root;
                    });
                }
            }
        }

        /// <summary>
        /// Replace GUID in Set+Check Flags 
        /// </summary>
        /// <param name="root"></param>
        /// <param name="searchGuid"></param>
        /// <param name="replaceGuid"></param>
        private RszScene SearchAndReplaceFlag(RszScene root, Guid searchGuid, Guid replaceGuid)
        {
            return root.VisitComponents(component =>
            {
                var componentName = component.Type.Name;
                if (componentName == "chainsaw.CheckFlagSettings" ||
                    componentName == "chainsaw.GmOptionSleep" ||
                    componentName == "chainsaw.GmOptionDoorLock")
                {
                    return component.Visit(x =>
                    {
                        if (x is RszObjectNode gameObject && gameObject.Type.Name == "chainsaw.CheckFlagInfo")
                        {
                            return gameObject.Get<Guid>("_CheckFlag") == searchGuid
                                ? gameObject.Set("_CheckFlag", replaceGuid)
                                : gameObject;
                        }
                        return x;
                    });
                }
                else if (componentName == "chainsaw.SetFlagSettings")
                {
                    return component.Visit(x =>
                    {
                        if (x is RszObjectNode gameObject && gameObject.Type.Name == "chainsaw.SetFlagSettings.SetFlagData")
                        {
                            return gameObject.Get<Guid>("_Flag") == searchGuid
                                ? gameObject.Set("_Flag", replaceGuid)
                                : gameObject;
                        }
                        return x;
                    });
                }
                return component;
            });
        }
    }
}
#endif
