using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class MendezPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            // Allow Mendez to be downed
            var downResistance = 100 - (int)(System.Math.Clamp(context.GetConfigOption("mendez-down-resistance", 0.2f), 0, 1) * 100);
            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/character/ch1b5z1/userdata/ch1b5z1damagereactionuserdata.user.2", root =>
            {
                return root
                    .Set("_BreakResistReactionData._CompletelyStoppingActionHash", 1041302437)
                    .Set("_BreakResistReactionData._CompletelyStoppingPercentage", new[] { downResistance });
            });

            // Stop Mendez picking up Ashley
            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/character/ch1b5z1/userdata/ch1b5z1actionpropertyuserdata.user.2", root =>
            {
                return root
                    .Set("_ActionPropertys[7]._Priority", 9999);
            });

            // Allow Mendez to cross bridges
            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/character/ch1b5z1/userdata/ch1b5z1configuration.user.2", root =>
            {
                return root
                    .Set("_SuspensionBridgeDetectorConfiguration.isEnable", true);
            });
        }
    }
}
