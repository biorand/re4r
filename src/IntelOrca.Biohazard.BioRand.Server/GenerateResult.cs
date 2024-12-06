using System;

namespace IntelOrca.Biohazard.BioRand.Server
{
    public sealed class GenerateResult(int randoId, int seed, string gameMoniker, byte[] zipFile, byte[] modFile)
    {
        public int RandoId => randoId;
        public int Seed => seed;
        public string GameMoniker => gameMoniker;
        public byte[] ZipFile => zipFile;
        public byte[] ModFile => modFile;
        public DateTime CreatedAt => DateTime.UtcNow;
    }
}
