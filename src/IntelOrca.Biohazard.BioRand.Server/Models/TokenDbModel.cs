using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("token")]
    public class TokenDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public DateTime Created { get; set; }

        [NotNull]
        public int UserId { get; set; }

        [NotNull]
        public int Code { get; set; }

        [NotNull, Indexed]
        public string Token { get; set; } = "";

        public DateTime? LastUsed { get; set; }
    }

    public class TokenUserDbViewModel : TokenDbModel
    {
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string UserAvatarUrl { get; set; } = "";
    }
}
