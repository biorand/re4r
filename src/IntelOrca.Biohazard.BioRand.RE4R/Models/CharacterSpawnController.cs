using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class CharacterSpawnController(RszInstance instance)
    {
        public RszInstance Instance => instance;

        public uint Difficulty => instance.Get<uint>("_DifficutyParam");
        public Guid Guid => instance.Get<Guid>("_GUID");
        public FlagCondition SpawnCondition => new FlagCondition(instance.Get<RszInstance>("_SpawnCondition")!);
        public FlagCondition SpawnSkipCondition => new FlagCondition(instance.Get<RszInstance>("_SpawnSkipCondition")!);
    }
}
