using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class CharacterSpawnController(RszInstance instance)
    {
        public RszInstance Instance => instance;

        public bool Enabled
        {
            get => instance.Get<byte>("v0") != 0;
            set => instance.Set("v0", (byte)1);
        }

        public uint Difficulty
        {
            get => instance.Get<uint>("_DifficutyParam");
            set => instance.Set("_DifficutyParam", value);
        }

        public Guid Guid
        {
            get => instance.Get<Guid>("_GUID");
            set => instance.Set("_GUID", value);
        }

        public FlagCondition SpawnCondition => new FlagCondition(instance.Get<RszInstance>("_SpawnCondition")!);
        public FlagCondition SpawnSkipCondition => new FlagCondition(instance.Get<RszInstance>("_SpawnSkipCondition")!);
    }
}
