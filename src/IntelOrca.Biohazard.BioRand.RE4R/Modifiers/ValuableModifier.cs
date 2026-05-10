using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.BioRand.REE;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    [Order(ModifierOrders.Valuable)]
    internal class ValuableModifier : Modifier
    {
        public override void Apply(IReeRandomizerContext context, RandomizerLogger logger)
        {
            var valuableDistributor = context.GetService<ValuableDistributor>();
            valuableDistributor.Setup(logger);
        }
    }
}
