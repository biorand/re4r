using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("randoconfig")]
    public class RandoConfigDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public DateTime Created { get; set; }
        [NotNull]
        public int BasedOnProfileId { get; set; }
        [NotNull]
        public string Data { get; set; } = "";
    }
}
