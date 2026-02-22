using System;
using System.Collections.Generic;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class CorpseDisposalPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            // Allow All Enemy Corpses to be Disposed of using games Corpse Disposal Mechanic

            var charactersByBasePath = new Dictionary<string, string[]>
            {
                ["_chainsaw"] = new[]
                {
                    "ch1b5z1", "ch1b7z0", "ch1c0z1", "ch1c0z2", "ch1c8z0",
                    "ch1d2z0", "ch1d3z0",
                    "ch1d6z0", "ch1e0z0", "ch1f2z0",
                    "ch1f4z1", "ch1f7z0", "ch1f8z0",
                    "ch1fcz0", "ch1fdz0", "ch8g3z0", "ch8gaz0"
                },
                ["_anotherorder"] = new[]
                {
                    "ch4fbz0"
                }
            };

            foreach (var (basePath, characters) in charactersByBasePath)
            {
                foreach (var ch in characters)
                {
                    var paramPath = $"natives/stm/{basePath}/appsystem/character/{ch}/userdata/{ch}paramuserdata.user.2";
                    context.ModifyUserFile(paramPath, root =>
                    {
                        return root.SetField("_EnableManagementDeadBody", true);
                    });
                }
            }
        }
    }
}
