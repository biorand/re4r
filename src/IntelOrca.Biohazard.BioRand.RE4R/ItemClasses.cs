using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class ItemClasses
    {
        public const string None = "none";
        public const string Handgun = "handgun";
        public const string Shotgun = "shotgun";
        public const string Smg = "smg";
        public const string Magnum = "magnum";
        public const string Rifle = "rifle";
        public const string Bolt = "bolt";
        public const string Arrow = "arrow";
        public const string Knife = "knife";
        public const string Special = "special";

        public static ImmutableArray<string> StartingWeapons { get; } =
            [None, Handgun, Shotgun, Smg, Magnum, Rifle, Bolt, Arrow];
    }
}
