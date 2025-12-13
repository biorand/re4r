using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class ArmaduraPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            // Allow Armadura to cross bridges
            context.ModifyUserFile("natives\\stm\\_chainsaw\\appsystem\\character\\ch1d6z0\\userdata\\ch1d6z0configuration.user.2", root =>
            {
                return root
                    .Set("_SuspensionBridgeDetectorConfiguration.isEnable", true);
            });
        }
    }
}
