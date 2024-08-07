using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("game")]
    public class GameDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Moniker { get; set; } = "";
    }
}
