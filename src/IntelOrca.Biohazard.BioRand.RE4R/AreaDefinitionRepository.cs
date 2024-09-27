using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinitionRepository
    {
        private static AreaDefinitionRepository? _leon;
        private static AreaDefinitionRepository? _ada;

        public AreaDefinition[] Areas { get; set; } = [];
        public ItemAreaDefinition[] Items { get; set; } = [];
        public string[] Gimmicks { get; set; } = [];

        public static AreaDefinitionRepository GetRepository(Campaign campaign)
        {
            return campaign == Campaign.Leon ? Leon : Ada;
        }

        public static AreaDefinitionRepository Leon
        {
            get
            {
                _leon ??= Resources.areas.DeserializeJson<AreaDefinitionRepository>();
                return _leon;
            }
        }

        public static AreaDefinitionRepository Ada
        {
            get
            {
                _ada ??= Resources.areas_sw.DeserializeJson<AreaDefinitionRepository>();
                return _ada;
            }
        }
    }

    public class ItemAreaDefinition
    {
        public int Chapter { get; set; }
        public string Path { get; set; } = "";
        public string DataPath { get; set; } = "";
        public ItemAreaItem[]? Items { get; set; }
    }

    public class ItemAreaItem
    {
        public string? ContextId { get; set; }
        public string[]? Include { get; set; }
        public string[]? Exclude { get; set; }
        public string? Valuable { get; set; }
        public int? Chapter { get; set; }

        public ContextId? CtxId
        {
            get
            {
                if (ContextId == null)
                    return null;

                var parts = ContextId.Split(',');
                return new ContextId(
                    sbyte.Parse(parts[0]),
                    byte.Parse(parts[1]),
                    int.Parse(parts[2]),
                    int.Parse(parts[3]));
            }
        }
    }
}
