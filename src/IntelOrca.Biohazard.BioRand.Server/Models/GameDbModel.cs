using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("game")]
    public class GameDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public string Name { get; set; } = "";
        [NotNull]
        public string Moniker { get; set; } = "";
        public string? ConfigurationDefinition { get; set; }
        public string? DefaultConfiguration { get; set; }
    }
}
