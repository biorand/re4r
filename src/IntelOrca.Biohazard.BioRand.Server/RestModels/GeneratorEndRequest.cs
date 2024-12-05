namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorEndRequest
    {
        public required string Id { get; set; }
        public int RandoId { get; set; }
        public required byte[] PakOutput { get; set; }
        public required byte[] FluffyOutput { get; set; }
    }
}
