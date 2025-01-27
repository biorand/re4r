namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorEndRequest
    {
        public required string Id { get; set; }
        public int RandoId { get; set; }
        public string Instructions { get; set; } = "";
    }
}
