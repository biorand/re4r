using System;
using System.Collections.Immutable;
using IntelOrca.Biohazard.BioRand.Server.Models;

namespace IntelOrca.Biohazard.BioRand.Server
{
    public sealed class GenerateResult(int randoId, int seed)
    {
        public int RandoId => randoId;
        public int Seed => seed;
        public DateTime StartTime { get; } = DateTime.UtcNow;

        public RandoStatus Status { get; set; } = RandoStatus.Processing;
        public DateTime FinishTime { get; set; }
        public string Instructions { get; set; } = "";
        public string FailReason { get; set; } = "";
        public ImmutableArray<GenerateResultAsset> Assets { get; set; } = [];
    }

    public sealed class GenerateResultAsset(
        string key,
        string title,
        string description,
        string fileName,
        byte[] data)
    {
        public string Key => key;
        public string Title => title;
        public string Description => description;
        public string FileName => fileName;
        public byte[] Data => data;
    }
}
