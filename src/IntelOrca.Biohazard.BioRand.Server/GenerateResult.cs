using System;

namespace IntelOrca.Biohazard.BioRand.Server
{
    public class GenerateResult
    {
        public ulong Id { get; }
        public int Seed { get; }
        public byte[] ZipFile { get; }
        public byte[] ModFile { get; }
        public DateTime CreatedAt { get; }

        public GenerateResult(ulong id, int seed, byte[] zipFile, byte[] modFile)
        {
            Id = id;
            Seed = seed;
            ZipFile = zipFile;
            ModFile = modFile;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
