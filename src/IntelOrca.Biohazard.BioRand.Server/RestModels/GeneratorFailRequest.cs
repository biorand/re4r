namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorFailRequest
    {
        public required string Id { get; set; }
        public int RandoId { get; set; }
        public required string Reason { get; set; }
    }
}
