using IntelOrca.Biohazard.BioRand.REE;
namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class ContextExtensions
    {
        public static ChainsawRandomizer AsChainsaw(this IReeRandomizerContext context) => (ChainsawRandomizer)context;
        public static DynamicData GetDynamicData(this IReeRandomizerContext context) => context.AsChainsaw().DynamicData;
    }
}
