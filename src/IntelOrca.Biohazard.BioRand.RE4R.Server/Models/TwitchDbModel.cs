using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("twitch")]
    internal class TwitchDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public DateTime LastUpdated { get; set; }

        [NotNull]
        public string AccessToken { get; set; } = "";

        [NotNull]
        public string RefreshToken { get; set; } = "";

        [NotNull]
        public string TwitchId { get; set; } = "";

        [NotNull]
        public string TwitchDisplayName { get; set; } = "";

        [NotNull]
        public string TwitchProfileImageUrl { get; set; } = "";

        [NotNull]
        public bool IsSubscribed { get; set; }
    }
}
