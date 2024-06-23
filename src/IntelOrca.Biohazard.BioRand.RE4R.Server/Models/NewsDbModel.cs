using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("news")]
    public class NewsDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed, NotNull]
        public DateTime Timestamp { get; set; }
        [NotNull]
        public string Title { get; set; } = "";
        [NotNull]
        public string Body { get; set; } = "";
    }
}
