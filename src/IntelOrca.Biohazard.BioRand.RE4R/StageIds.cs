using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class StageIds
    {
        private static ImmutableArray<StageDefinition> _stages;

        public static ImmutableArray<StageDefinition> Stages
        {
            get
            {
                if (_stages != null)
                    return _stages;

                var def = Resources.stages.DeserializeJson<StagesDefinition>();
                _stages = def.Stages.ToImmutableArray();
                return _stages;
            }
        }

        public static StageDefinition? FromId(int id)
        {
            return Stages.FirstOrDefault(x => x.Stage == id);
        }

        public class StagesDefinition
        {
            public StageDefinition[] Stages { get; set; } = [];
        }

        public class StageDefinition
        {
            public int Chapter { get; set; }
            public int Stage { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
