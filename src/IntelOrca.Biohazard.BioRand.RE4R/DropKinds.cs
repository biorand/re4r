using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class DropKinds
    {
        // Generic
        public const string None = "none";
        public const string Automatic = "automatic";
        public const string Ammo = "ammo";
        public const string Fas = "fas";
        public const string Fish = "fish";
        public const string EggBrown = "egg-brown";
        public const string EggWhite = "egg-white";
        public const string EggGold = "egg-gold";
        public const string GrenadeFlash = "grenade-flash";
        public const string GrenadeHeavy = "grenade-heavy";
        public const string GrenadeLight = "grenade-light";
        public const string Gunpowder = "gunpowder";
        public const string HerbG = "herb-g";
        public const string HerbGG = "herb-gg";
        public const string HerbGGY = "herb-ggy";
        public const string HerbGGG = "herb-ggg";
        public const string HerbGR = "herb-gr";
        public const string HerbGRY = "herb-gry";
        public const string HerbGY = "herb-gy";
        public const string HerbR = "herb-r";
        public const string HerbRY = "herb-ry";
        public const string HerbY = "herb-y";
        public const string Knife = "knife";
        public const string Money = "money";
        public const string ResourceLarge = "resource-large";
        public const string ResourceSmall = "resource-small";
        public const string TokenSilver = "token-silver";
        public const string TokenGold = "token-gold";

        // High value
        public const string Attachment = "attachment";
        public const string CasePerk = "case-perk";
        public const string CaseSize = "case-size";
        public const string Charm = "charm";
        public const string Recipe = "recipe";
        public const string SmallKey = "small-key";
        public const string Treasure = "treasure";
        public const string Weapon = "weapon";

        public static ImmutableArray<string> All => _generic.Concat(_highValue).ToImmutableArray();
        public static ImmutableArray<string> Generic => _generic.ToImmutableArray();
        public static ImmutableArray<string> HighValue => _highValue.ToImmutableArray();

        private static readonly string[] _generic = [
            None,
            Automatic,
            Ammo,
            Fas,
            Fish,
            EggBrown,
            EggWhite,
            EggGold,
            GrenadeFlash,
            GrenadeHeavy,
            GrenadeLight,
            Gunpowder,
            HerbG,
            HerbGG,
            HerbGGY,
            HerbGGG,
            HerbGR,
            HerbGRY,
            HerbGY,
            HerbR,
            HerbRY,
            HerbY,
            Knife,
            Money,
            ResourceLarge,
            ResourceSmall,
            TokenSilver,
            TokenGold
        ];

        private static readonly string[] _highValue = [
            Attachment,
            CasePerk,
            CaseSize,
            Charm,
            Recipe,
            SmallKey,
            Treasure,
            Weapon,
        ];
    }
}
