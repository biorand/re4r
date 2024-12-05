using System;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Models
{
    [Table("profile")]
    public class ProfileDbModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed, NotNull]
        public int GameId { get; set; }
        [NotNull]
        public DateTime Created { get; set; }
        [NotNull]
        public int UserId { get; set; }
        [NotNull]
        public string Name { get; set; } = "";
        [NotNull]
        public string Description { get; set; } = "";
        [NotNull]
        public int ConfigId { get; set; }
        [NotNull]
        public int Flags { get; set; }
        [NotNull]
        public int StarCount { get; set; }
        [NotNull]
        public int SeedCount { get; set; }

        [Ignore]
        public bool Deleted
        {
            get => GetFlag(0);
            set => SetFlag(0, value);
        }

        [Ignore]
        public bool Public
        {
            get => GetFlag(1);
            set => SetFlag(1, value);
        }

        [Ignore]
        public bool Official
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
}
