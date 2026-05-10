using System;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class ForceNgMerchantPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            Leon();
            Ada();
        }

        private void Leon()
        {
            var path = "natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_502_p000.scn.20";
            context.ModifyScnFile(path, scene =>
            {
                return scene
                    .RemoveGameObject(new Guid("ca0ac85f-1238-49d9-a0fb-0d58a42487a1"))  // merchant
                    .RemoveGameObject(new Guid("4a975fc1-2e1c-4fd3-a49a-1f35d6a30f0f")); // merchant flame
            });
        }

        private void Ada()
        {
            var path = "natives/stm/_anotherorder/environment/scene/gimmick/st50/gimmick_st50_501_ao.scn.20";
            context.ModifyScnFile(path, scene =>
            {
                return scene
                    .RemoveGameObject(new Guid("41a87b99-d47f-438d-a686-f19e6865379e"))  // merchant
                    .RemoveGameObject(new Guid("33ba7a17-4b7d-4a23-b272-c5afcd62f3f1"))  // merchant flame
                    .RemoveGameObject(new Guid("bf5cc10b-ff6b-46be-99e3-814629dfcff8")); // typwriter
            });
        }
    }
}
