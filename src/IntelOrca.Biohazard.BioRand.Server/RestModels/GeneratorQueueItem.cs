namespace IntelOrca.Biohazard.BioRand.Server.RestModels
{
    public class GeneratorQueueItem
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public long Created { get; set; }
        public int UserId { get; set; }
        public int Seed { get; set; }
        public int ConfigId { get; set; }
        public int Status { get; set; }
        public int UserRole { get; set; }
        public string? UserName { get; set; }
        public string[] UserTags { get; set; } = [];
        public int ProfileId { get; set; }
        public string? ProfileName { get; set; }
        public string? ProfileDescription { get; set; }
        public int ProfileUserId { get; set; }
        public string? ProfileUserName { get; set; }
        public string? Config { get; set; }
    }
}
