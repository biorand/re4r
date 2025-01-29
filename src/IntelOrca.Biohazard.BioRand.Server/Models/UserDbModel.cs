using System;
using System.Linq;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("user")]
    public class UserDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public DateTime Created { get; set; }

        [NotNull]
        public string Name { get; set; } = "";

        [NotNull, Indexed]
        public string NameLowerCase { get; set; } = "";

        [NotNull, Indexed]
        public string Email { get; set; } = "";

        [NotNull]
        public int Flags { get; set; }

        [Ignore]
        public bool ShareHistory
        {
            get => GetFlag(0);
            set => SetFlag(0, value);
        }

        public int? TwitchId { get; set; }
        public string? KofiEmail { get; set; }
        [Indexed]
        public string? KofiEmailVerification { get; set; }
        public DateTime? KofiEmailTimestamp { get; set; }

        [Ignore]
        public bool KofiMember
        {
            get => GetFlag(1);
            set => SetFlag(1, value);
        }

        [Ignore]
        public bool TwitchSubscriber
        {
            get => GetFlag(2);
            set => SetFlag(2, value);
        }

        private bool GetFlag(int i) => (Flags & (1 << i)) != 0;
        private void SetFlag(int i, bool value)
        {
            if (value)
                Flags |= (1 << i);
            else
                Flags &= ~(1 << i);
        }
    }

    public class ExtendedUserDbModel : UserDbModel
    {
        private string[] _tags = [];

        public string Tags
        {
            get => string.Join(",", _tags);
            set
            {
                _tags = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public bool ContainsTag(string label)
        {
            return _tags.Contains(label);
        }

        public bool IsAdmin => ContainsTag("admin");
        public bool IsPending => ContainsTag("pending");
    }
}
