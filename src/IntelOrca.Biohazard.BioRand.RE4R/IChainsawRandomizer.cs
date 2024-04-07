namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public interface IChainsawRandomizer
    {
        EnemyClassFactory EnemyClassFactory { get; }

        RandomizerOutput Randomize(RandomizerInput input);
    }
}
