using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("usertag")]
    public class UserTagDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull, Unique]
        public string Label { get; set; } = "";

        [NotNull]
        public uint ColorLight { get; set; }

        [NotNull]
        public uint ColorDark { get; set; }
    }
}
