using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorRegisterRequest
    {
        public required RandomizerConfigurationDefinition ConfigurationDefinition { get; set; }
        public required Dictionary<string, object> DefaultConfiguration { get; set; }
    }
}
