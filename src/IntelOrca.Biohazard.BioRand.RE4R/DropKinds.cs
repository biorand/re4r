﻿using System.Collections.Immutable;
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

        // Categories
        public const string CategoryAmmo = "Ammo";
        public const string CategoryGrenade = "Grenade";
        public const string CategoryHealth = "Health";
        public const string CategoryMoney = "Money";
        public const string CategoryNone = "None";
        public const string CategoryResource = "Resource";
        public const string CategoryOther = "Other";

        public static ImmutableArray<string> All => _generic.Concat(_highValue).ToImmutableArray();
        public static ImmutableArray<string> GenericAll => _special.Concat(_generic).ToImmutableArray();
        public static ImmutableArray<string> Generic => _generic.ToImmutableArray();
        public static ImmutableArray<string> HighValue => _highValue.ToImmutableArray();

        public static string GetCategory(string drop)
        {
            return drop switch
            {
                None => CategoryNone,
                Automatic => CategoryOther,
                Ammo => CategoryAmmo,
                Fas => CategoryHealth,
                Fish => CategoryHealth,
                EggBrown => CategoryHealth,
                EggWhite => CategoryHealth,
                EggGold => CategoryHealth,
                GrenadeFlash => CategoryGrenade,
                GrenadeHeavy => CategoryGrenade,
                GrenadeLight => CategoryGrenade,
                Gunpowder => CategoryResource,
                HerbG => CategoryHealth,
                HerbGG => CategoryHealth,
                HerbGGY => CategoryHealth,
                HerbGGG => CategoryHealth,
                HerbGR => CategoryHealth,
                HerbGRY => CategoryHealth,
                HerbGY => CategoryHealth,
                HerbR => CategoryHealth,
                HerbRY => CategoryHealth,
                HerbY => CategoryHealth,
                Knife => CategoryResource,
                Money => CategoryMoney,
                ResourceLarge => CategoryResource,
                ResourceSmall => CategoryResource,
                TokenSilver => CategoryOther,
                TokenGold => CategoryOther,
                _ => CategoryOther,
            };
        }

        public static (string BackgroundColor, string TextColor) GetColor(string category)
        {
            return GetCategory(category) switch
            {
                CategoryAmmo => ("#66f", "#fff"),
                CategoryHealth => ("#696", "#fff"),
                CategoryGrenade => ("#833", "#fff"),
                CategoryResource => ("#866", "#000"),
                CategoryMoney => ("#ff0", "#000"),
                CategoryNone => ("#333", "#fff"),
                _ => ("#ddd", "#000"),
            };
        }

        private static readonly string[] _special = [
            None,
            Automatic,
        ];

        private static readonly string[] _generic = [
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
