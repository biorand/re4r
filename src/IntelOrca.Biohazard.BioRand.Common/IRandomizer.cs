namespace IntelOrca.Biohazard.BioRand
{
    public interface IRandomizer
    {
        RandomizerConfigurationDefinition ConfigurationDefinition { get; }
        RandomizerConfiguration DefaultConfiguration { get; }

        /// <summary>
        /// Usually the 7 character git hash.
        /// </summary>
        public string BuildVersion { get; }

        RandomizerOutput Randomize(RandomizerInput input);
    }
}
