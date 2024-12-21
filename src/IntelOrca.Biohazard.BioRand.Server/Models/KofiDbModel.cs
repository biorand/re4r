using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("kofi")]
    public class KofiDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public int GameId { get; set; }
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

    public class KofiUserDbViewModel : KofiDbModel
    {
        public string UserName { get; set; } = "";
        public int UserRole { get; set; }
        public string UserAvatarUrl { get; set; } = "";
    }

    public class KofiDailyDbViewModel
    {
        public string Day { get; set; } = "";
        public int Donations { get; set; }
        public int Amount { get; set; }
    }
}
