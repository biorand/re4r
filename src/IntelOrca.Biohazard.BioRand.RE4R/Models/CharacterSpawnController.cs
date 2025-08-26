using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class CharacterSpawnController(RszStructNode node)
    {
        public RszStructNode Node => node;

        public bool Enabled
        {
            get => node.Get<bool>("Enabled");
            set => node.Set("Enabled", value);
        }

        public uint Difficulty
        {
            get => node.Get<uint>("_DifficutyParam");
            set => node.Set("_DifficutyParam", value);
        }

        public Guid Guid
        {
            get => node.Get<Guid>("_GUID");
            set => node.Set("_GUID", value);
        }

        public FlagCondition SpawnCondition => new FlagCondition(node.Get<RszStructNode>("_SpawnCondition")!);
        public FlagCondition SpawnSkipCondition => new FlagCondition(node.Get<RszStructNode>("_SpawnSkipCondition")!);
    }
}
