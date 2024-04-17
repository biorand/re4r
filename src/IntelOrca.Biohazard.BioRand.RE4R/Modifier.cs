namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal abstract class Modifier
    {
        public virtual void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
        }

        public virtual void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
        }
    }
}
