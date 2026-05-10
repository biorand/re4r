using System;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class EnableEarlyCabinPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            context.ModifyScnFile("natives/stm/_chainsaw/environment/scene/gimmick/st43/gimmick_st43_301_p000.scn.20", scene =>
            {
                // Remove lock from iron door preventing you entering cabin
                var doorParamObject = scene.FindGameObject(new Guid("3e5c7e73-fd33-49b6-b4ac-bba642abb1fc")) ?? throw new Exception("Unable to find big door.");
                doorParamObject = doorParamObject.WithComponents(
                    doorParamObject.Components.RemoveAll(x => x.Type.Name == "chainsaw.GmOptionBigDoorLock"));
                scene = scene.UpdateGameObject(doorParamObject);

                return scene;
            });
            context.ModifyScnFile("natives/stm/_chainsaw/environment/scene/gimmick/st43/gimmick_st43_900.scn.20", scene =>
            {
                var repo = context.TypeRepository;

                // Hide door bar until cabin time
                var doorBarObject = scene.FindGameObject(new Guid("7a2d6128-79f7-0a71-388f-0ea0a80ce6e7")) ?? throw new Exception("Unable to find cabin door bar.");
                doorBarObject = doorBarObject.AddOrUpdateComponent(
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
                                            _Data = new chainsaw.RuleStratum.ParticleFlag()
                                            {
                                                Flags = new chainsaw.FlagCondition()
                                                {
                                                    _CheckFlags =
                                                    [
                                                        new() {
                                                            _CheckFlag = new Guid("fda3e111-6ce3-4283-87d0-dee5f19e1459"),
                                                            _CompareValue = false
                                                        }
                                                    ]
                                                }
                                            }
                                        }
                                    ]
                                },
                                Value = true
                            }
                        }));
                scene = scene.UpdateGameObject(doorBarObject);

                return scene;
            });

            var lights2_2 = context.GetFile($"natives/stm/_chainsaw/environment/scene/light/st43/light_st43_311_cp10_chp2_2.scn.20") ?? throw new Exception("Unable to find lights scene.");
            context.SetFile("natives/stm/_chainsaw/environment/scene/light/st43/light_st43_311_cp10_chp1_1.scn.20", lights2_2);
            context.SetFile("natives/stm/_chainsaw/environment/scene/light/st43/light_st43_311_cp10_chp1_3.scn.20", lights2_2);
        }
    }
}
