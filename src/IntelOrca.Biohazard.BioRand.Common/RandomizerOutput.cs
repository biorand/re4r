using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand
{
    public class RandomizerOutput
    {
        public byte[] PakOutput { get; }
        public byte[] FluffyOutput { get; }
        public Dictionary<string, string> Logs { get; }

        public RandomizerOutput(byte[] pakOutput, byte[] fluffyOutput, Dictionary<string, string> logs)
        {
            PakOutput = pakOutput;
            FluffyOutput = fluffyOutput;
            Logs = logs;
        }
    }
}
