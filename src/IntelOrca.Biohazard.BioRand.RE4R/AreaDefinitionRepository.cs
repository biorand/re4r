using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinitionRepository
    {
        private static AreaDefinitionRepository? _leon;
        private static AreaDefinitionRepository? _ada;

        public AreaDefinition[] General { get; set; } = [];
        public AreaDefinition[] Items { get; set; } = [];
        public AreaDefinition[] Gimmicks { get; set; } = [];

        public IEnumerable<AreaDefinition> All
        {
            get
            {
                foreach (var d in General)
                    d.Kind = AreaKind.General;
                foreach (var d in Items)
                    d.Kind = AreaKind.Items;
                foreach (var d in Gimmicks)
                    d.Kind = AreaKind.Gimmicks;
                return General.Concat(Items).Concat(Gimmicks);
            }
        }

        public static AreaDefinitionRepository GetRepository(Campaign campaign)
        {
            return campaign == Campaign.Leon ? Leon : Ada;
        }

        private static AreaDefinitionRepository Leon
        {
            get
            {
                _leon ??= EmbeddedData.GetFile("areas.json").DeserializeJson<AreaDefinitionRepository>();
                return _leon;
            }
        }

        private static AreaDefinitionRepository Ada
        {
            get
            {
                _ada ??= EmbeddedData.GetFile("areas_sw.json").DeserializeJson<AreaDefinitionRepository>();
                return _ada;
            }
        }
    }
}
