using System;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "skipashleysection",
        Name = "Skip Ashley Section",
        Description = "Skips the whole Ashley section.",
        Version = "1.0",
        Author = "IntelOrca")]
    internal class SkipAshleyPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            if (!context.ExportingMod && !context.GetConfigOption<bool>("skip-ashley-section"))
                return;

            var repo = context.TypeRepository;

            // Spawn Ashley at the end to instantly trigger the chapter end cutscene
            context.ModifyUserFile("natives/stm/_chainsaw/leveldesign/initialsettings/chp3-3-a.user.2", userdata =>
            {
                return userdata.Set("_CampaignInitialSettingList[0]._CharacterList[0]._Locator", new
                {
                    _Stage = 53202,
                    _Position = new Vector3(131.3f, 33.2f, 50.0f),
                    _Rotation = Quaternion.Identity
                });
            });

            // Enable the flag that gets set when Ashley picks up the Salazar crest
            // This is what unlocks the shortcut from library 1F to 2F
            context.ModifyScnFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp4_1/level_cp10_chp4_1_000.scn.20", scene =>
            {
                var paramObject = scene.FindGameObject(new Guid("5fc9255d-5b91-43f6-9ed5-d4b8c55b2451")) ?? throw new Exception("Failed to find chapter 10 objective event");
                var setFlag = paramObject.FindComponent("chainsaw.SetFlagSettings")!;
                var arr = setFlag.Get<RszArrayNode>("_Params._Params[0]._SetFlags");
                arr = arr.Add(RszSerializer.Serialize(repo.FromName("chainsaw.SetFlagSettings.SetFlagData")!, new chainsaw.SetFlagSettings.SetFlagData()
                {
                    _Flag = new Guid("319b884a-a483-4640-8284-e7cc87edb40f")
                }));
                setFlag = setFlag.Set("_Params._Params[0]._SetFlags", arr);
                paramObject = paramObject.AddOrUpdateComponent(setFlag);
                scene = scene.UpdateGameObject(paramObject);
                return scene;
            });

            // Hide the bookcase in the library that moves when you use the lantern
            // This allows Leon to turn the crank to lower the stairs
            context.ModifyScnFile("natives/stm/_chainsaw/environment/scene/gimmick/st53/gimmick_st53_200_p000.scn.20", scene =>
            {
                var paramObject = scene.FindGameObject(new Guid("008aedb9-3b13-402c-b5d7-d9176e237e70")) ?? throw new Exception("Failed to find moving bookcase gimmick");
                scene = scene.UpdateGameObject(paramObject.AddOrUpdateComponent(
                    repo.Create("chainsaw.ObjectHide")
                        .Set("Enabled", true)
                        .Set("Settings", new[]
                        {
                            new chainsaw.RuleStratum.StratumBool()
                            {
                                _Enable = new chainsaw.RuleStratum.Rule()
                                {
                                    Logic = 1,
                                    Matters =
                                    [
                                        new()
                                        {
                                            _Data = new chainsaw.RuleStratum.ParticleChapter()
                                            {
                                                Compare = 1, // not
                                                Chapter = 23300 // ch9
                                            }
                                        }
                                    ]
                                },
                                Value = true
                            }
                        })));

                return scene;
            });

            // Do not hide bunch of keys item so Leon can pick them up and use them
            context.ModifyScnFile("natives/stm/_chainsaw/leveldesign/location/loc53/item_loc53.scn.20", scene =>
            {
                var itemGameObject = scene.FindGameObject(new Guid("c1d0972b-2174-4db8-834c-40db87df9883")) ?? throw new Exception("Failed to find bunch of keys item");
                return scene.UpdateGameObject(
                    itemGameObject.WithComponents(
                        itemGameObject.Components.RemoveAll(x => x.Type.Name == "chainsaw.ObjectHide")));
            });
        }
    }
}
