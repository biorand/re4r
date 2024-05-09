using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class RandomizerInput
    {
        public string? GamePath { get; set; }
        public string? ProfileName { get; set; }
        public string? ProfileDescription { get; set; }
        public string? ProfileAuthor { get; set; }
        public int Seed { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}
