using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("rando")]
    public class RandoDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull, Indexed]
        public DateTime Created { get; set; }
        [NotNull, Indexed]
        public int UserId { get; set; }
        [NotNull]
        public string Version { get; set; } = "";
        [NotNull]
        public int Seed { get; set; }
        [NotNull]
        public int ConfigId { get; set; }
        [NotNull]
        public RandoStatus Status { get; set; }
    }

    public enum RandoStatus
    {
        Unknown,
        Unassigned,
        Processing,
        Completed,
        Failed,
        Expired,
    }
}
