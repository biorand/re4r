using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("token")]
    internal class TokenDbModel
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
}
