using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class LakeHeadPatch(IReeRandomizerContext context) : IPatch
    {
        private static readonly Guid TrueFlag = new("84cd36d0-faff-4069-ae26-cc30b38c01d8");

        public void Apply()
        {
            // Allow statue heads to be placed without first inspecting the wall
            var path = @"natives/stm/_chainsaw/environment/scene/gimmick/st46/gimmick_st46_201_p000.scn.20";
            context.ModifyScnFile(path, scene =>
            {
                return scene.VisitGameObjects(go =>
                {
                    // Statue_0, Statue_1
                    if (go.Guid == new Guid("595b7cc7-d75f-0813-1d73-c22e89ae3f00") ||
                        go.Guid == new Guid("1a94c331-0f9d-06a3-15f4-af5c84a702b1"))
                    {
                        go = go.AddOrUpdateComponent(context.TypeRepository.Serialize(new chainsaw.CheckFlagSettings()
                        {
                            Enabled = true,
                            _Params = new chainsaw.OptionSettings<chainsaw.CheckFlagSettings.Param>()
                            {
                                _Params =
                                [
                                    new chainsaw.CheckFlagSettings.Param()
                                    {
                                        _KeyHash = 3090179045,
                                        _BindTriggerNameHash = 2180083513,
                                        _FlagCondition = new chainsaw.FlagCondition()
                                        {
                                            _CheckFlags =
                                            [
                                                new chainsaw.CheckFlagInfo()
                                                {
                                                    _CheckFlag = TrueFlag,
                                                    _CompareValue = true
                                                }
                                            ]
                                        }
                                    }
                                ]
                            }
                        }));
                    }
                    return go;
                });
            });
        }
    }
}
