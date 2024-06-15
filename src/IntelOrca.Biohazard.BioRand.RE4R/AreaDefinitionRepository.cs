using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinitionRepository
    {
        private static AreaDefinitionRepository? _default;

        public AreaDefinition[] Areas { get; set; } = new AreaDefinition[0];
        public ItemAreaDefinition[] Items { get; set; } = new ItemAreaDefinition[0];

        public static AreaDefinitionRepository Default
        {
            get
            {
                _default ??= Resources.areas.DeserializeJson<AreaDefinitionRepository>();
                return _default;
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
