using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class MendezPatch : IPatch
    {
        private readonly ChainsawRandomizer _randomizer;

        public FileRepository FileRepository => _randomizer.FileRepository;

        public MendezPatch(ChainsawRandomizer randomizer)
        {
            _randomizer = randomizer;
        }

        public void Apply()
        {
            // Allow Mendez to be downed
            var downResistance = 100 - (int)(Math.Clamp(_randomizer.GetConfigOption("mendez-down-resistance", 0.2f), 0, 1) * 100);
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/character/ch1b5z1/userdata/ch1b5z1damagereactionuserdata.user.2", root =>
            {
                return root
                    .Set("_BreakResistReactionData._CompletelyStoppingActionHash", 1041302437)
                    .Set("_BreakResistReactionData._CompletelyStoppingPercentage", new[] { downResistance });
            });

            // Stop Mendez picking up Ashley
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/character/ch1b5z1/userdata/ch1b5z1actionpropertyuserdata.user.2", root =>
            {
                return root
                    .Set("_ActionPropertys[7]._Priority", 9999);
            });
        }
    }
}
