using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class CorpseDisposalPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            // Allow All Enemy Corpses to be Disposed of using games Corpse Disposal Mechanic

            var characters = new[]
            {
                "ch1b5z1", "ch1b7z0", "ch1c0z1", "ch1c0z2", "ch1c8z0",
                "ch1d0z0", "ch1d1z1", "ch1d2z0", "ch1d3z0", "ch1d4z0",
                "ch1d6z0", "ch1e0z0", "ch1f0z0", "ch1f1z0", "ch1f2z0", 
                "ch1f4z1", "ch1f5z1", "ch1f6z0", "ch1f7z0", "ch1f8z0", 
                "ch1fcz0", "ch1fdz0", "ch8g2z0", "ch8g3z0", "ch8gaz0"
            };

            foreach (var ch in characters)
            {
                    var paramPath = $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}paramuserdata.user.2";
                    context.ModifyUserFile(paramPath, root =>
                    {
                        return root.SetField("_EnableManagementDeadBody", true);
                    });
            }

        }
    }
}
