using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("meta")]
    public class MetaDbModel
    {
        [PrimaryKey]
        public int Id { get; set; }
        [NotNull]
        public int Version { get; set; }
    }
}
