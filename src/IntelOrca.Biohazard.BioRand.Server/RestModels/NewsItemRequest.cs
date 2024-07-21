namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class NewsItemRequest
    {
        public int Timestamp { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
    }
}
