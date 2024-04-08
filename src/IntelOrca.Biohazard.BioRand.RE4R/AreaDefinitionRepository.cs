using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinitionRepository
    {
        private static AreaDefinitionRepository? _default;

        public AreaDefinition[] Areas { get; set; } = new AreaDefinition[0];

        public static AreaDefinitionRepository Default
        {
            get
            {
                _default ??= Resources.areas.DeserializeJson<AreaDefinitionRepository>();
                return _default;
            }
        }
    }
}
