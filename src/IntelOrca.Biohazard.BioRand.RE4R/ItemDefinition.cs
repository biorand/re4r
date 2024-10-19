namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class ItemDefinition
    {
        public static ItemDefinition Automatic { get; } = new ItemDefinition() { Id = -1, Name = "Automatic" };

        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Kind { get; set; }
        public string? Mode { get; set; }
        public string? Size { get; set; }
        public string? Class { get; set; }
        public bool Bonus { get; set; }
        public bool Dlc { get; set; }
        public int Stack { get; set; }
        public int Value { get; set; }
        public int[]? Weapons { get; set; }
        public int? WeaponId { get; set; }

        public bool IsAutomatic => Id == -1;
        public int Width => int.Parse((Size ?? "2x2").Split('x')[0]);
        public int Height => int.Parse((Size ?? "2x2").Split('x')[1]);

        public override string ToString() => Name ?? Id.ToString();

        public bool SupportsCampaign(Campaign campaign)
        {
            var mode = Mode;
            if (mode == "main")
                return campaign == Campaign.Leon;
            else if (mode == "sw")
                return campaign == Campaign.Ada;
            else if (!string.IsNullOrEmpty(mode))
                return false;
            return true;
        }
    }
}
