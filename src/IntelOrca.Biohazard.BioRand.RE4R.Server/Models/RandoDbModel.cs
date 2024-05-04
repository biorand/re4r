using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("rando")]
    internal class RandoDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public DateTime Created { get; set; }
        [Indexed]
        public int UserId { get; set; }
        public int Seed { get; set; }
        public int ConfigId { get; set; }
    }
}
