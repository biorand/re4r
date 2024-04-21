﻿namespace IntelOrca.Biohazard.BioRand.RE4R
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
        public bool? Bonus { get; set; }
        public int Value { get; set; }
        public int[]? Weapons { get; set; }

        public bool IsAutomatic => Id == -1;
        public int Width => int.Parse((Size ?? "2x2").Split('x')[0]);
        public int Height => int.Parse((Size ?? "2x2").Split('x')[1]);

        public override string ToString() => Name ?? Id.ToString();
    }
}
