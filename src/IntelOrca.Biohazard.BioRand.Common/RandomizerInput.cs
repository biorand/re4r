namespace IntelOrca.Biohazard.BioRand
{
    public class RandomizerInput
    {
        public string? GamePath { get; set; }
        public string? ProfileName { get; set; }
        public string? ProfileDescription { get; set; }
        public string? ProfileAuthor { get; set; }
        public int Seed { get; set; }
        public RandomizerConfiguration Configuration { get; set; } = new();
    }
}
