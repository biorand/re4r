using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorRegisterRequest
    {
        public int GameId { get; set; }
        public required RandomizerConfigurationDefinition ConfigurationDefinition { get; set; }
        public required Dictionary<string, object> DefaultConfiguration { get; set; }
    }
}
