using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Models
{
    [Table("user")]
    internal class UserDbModel
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
        public UserRoleKind Role { get; set; }

        [NotNull]
        public int Flags { get; set; }

        [Ignore]
        public bool ShareHistory
        {
            get => GetFlag(0);
            set => SetFlag(0, value);
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
}
