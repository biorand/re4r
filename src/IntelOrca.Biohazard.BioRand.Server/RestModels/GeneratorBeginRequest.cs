namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorBeginRequest
    {
        public required string Id { get; set; }
        public int RandoId { get; set; }
        public required string Version { get; set; }
    }
}
