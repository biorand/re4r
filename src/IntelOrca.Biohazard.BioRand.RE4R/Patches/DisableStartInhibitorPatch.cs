using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class DisableStartInhibitorPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            context.ModifyScnFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_010.scn.20", scene =>
            {
                var inhibitor = scene.FindGameObject(new Guid("9fc712ca-478c-45b5-be12-5233edf4fe95")) ?? throw new Exception("Failed to find inhibitor");
                var inhibitorComponent = inhibitor.Components[1];
                for (var i = 0; i < 5; i++)
                {
                    inhibitorComponent = inhibitorComponent.Set(
                        $"_Datas[{i}].Rule[0]._Enable.Matters[0]._Data.Flags._CheckFlags[0]._CheckFlag",
                        new Guid("0fb10e00-5384-4732-881a-af1fae2036c7"));
                }
                scene = scene.UpdateGameObject(inhibitor
                    .AddOrUpdateComponent(inhibitorComponent));
                return scene;
            });
        }
    }
}
