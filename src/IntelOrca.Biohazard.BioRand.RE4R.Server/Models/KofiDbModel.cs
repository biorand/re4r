using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("kofi")]
    internal class KofiDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Unique, NotNull]
        public Guid MessageId { get; set; }
        [NotNull]
        public DateTime Timestamp { get; set; }
        [Indexed]
        public int? UserId { get; set; }
        [Indexed]
        public string? Email { get; set; }
        public decimal Price { get; set; }
        public string? TierName { get; set; }
        public string Data { get; set; } = "";
    }
}
