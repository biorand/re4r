using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class ItemRandomizer
    {
        private readonly ChainsawRandomizer _randomizer;
        private readonly HashSet<int> _placedItemIds = new HashSet<int>();
        private readonly bool _allowBonusItems;
        private readonly Dictionary<RandomItemSettings, EndlessBag<string>> _generalDrops = new();

        public int[] PlacedItemIds => _placedItemIds.ToArray();
        public ItemDefinition[] PlacedItems => _placedItemIds
            .Select(x => ItemDefinitionRepository.Default.Find(x)!)
            .ToArray();

        public ItemRandomizer(ChainsawRandomizer randomizer, bool allowBonusItems)
        {
            _randomizer = randomizer;
            _allowBonusItems = allowBonusItems;
        }

        public ItemDefinition? GetRandomWeapon(Rng rng, string? classification = null, bool allowReoccurance = true)
        {
            var weaponKinds = new[] {
                ItemClasses.None, ItemClasses.Handgun, ItemClasses.Shotgun,
                ItemClasses.Rifle, ItemClasses.Smg, ItemClasses.Magnum,
                ItemClasses.Bolt };

            if (classification == ItemClasses.None)
                return null;

            if (classification == ItemClasses.Random)
                classification = rng.Next(weaponKinds);

            return GetRandomItemDefinition(rng, ItemKinds.Weapon, classification, allowReoccurance);
        }

        public ItemDefinition? GetRandomAttachment(Rng rng, string? classification = null, bool allowReoccurance = true)
        {
            return GetRandomItemDefinition(rng, ItemKinds.Attachment, classification, allowReoccurance);
        }

        public ItemDefinition? GetRandomAttachment(Rng rng, ItemDefinition? weapon, bool allowReoccurance = true)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var poolEnumerable = itemRepo
                .GetAll(ItemKinds.Attachment)
                .Where(x => _allowBonusItems || !(x.Bonus ?? false));
            if (!allowReoccurance)
            {
                poolEnumerable = poolEnumerable
                    .Where(x => !_placedItemIds.Contains(x.Id));
            }
            if (weapon != null)
            {
                poolEnumerable = poolEnumerable.Where(x => x.Weapons?.Contains(weapon.Id) ?? false);
            }
            var pool = poolEnumerable.ToArray();
            if (pool.Length == 0)
                return null;

            var chosen = rng.Next(pool);
            _placedItemIds.Add(chosen.Id);
            return chosen;
        }

        public ItemDefinition? GetRandomItemDefinition(Rng rng, string kind, string? classification = null, bool allowReoccurance = true)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var poolEnumerable = itemRepo
                .GetAll(kind, classification)
                .Where(x => _allowBonusItems || !(x.Bonus ?? false));
            if (!allowReoccurance)
            {
                poolEnumerable = poolEnumerable
                    .Where(x => !_placedItemIds.Contains(x.Id));

            }
            var pool = poolEnumerable.ToArray();
            if (pool.Length == 0)
                return null;

            var chosen = rng.Next(pool);
            _placedItemIds.Add(chosen.Id);
            return chosen;
        }

        public Item? GetRandomDrop(Rng rng, string dropKind, RandomItemSettings settings)
        {
            return dropKind switch
            {
                // General
                DropKinds.None => null,
                DropKinds.Automatic => new Item(-1, 0),
                DropKinds.AmmoHandgun => GetRandomAmmo(ItemIds.AmmoHandgun, rng, settings),
                DropKinds.AmmoShotgun => GetRandomAmmo(ItemIds.AmmoShotgun, rng, settings),
                DropKinds.AmmoRifle => GetRandomAmmo(ItemIds.AmmoRifle, rng, settings),
                DropKinds.AmmoSmg => GetRandomAmmo(ItemIds.AmmoSmg, rng, settings),
                DropKinds.AmmoMagnum => GetRandomAmmo(ItemIds.AmmoMagnum, rng, settings),
                DropKinds.AmmoBolts => GetRandomAmmo(ItemIds.AmmoBolts, rng, settings),
                DropKinds.AmmoMines => GetRandomAmmo(ItemIds.AmmoMines, rng, settings),
                DropKinds.Fas => new Item(ItemIds.FirstAidSpray, 1),
                DropKinds.Fish => GetRandomSingleItem(rng, ItemKinds.Fish, allowReoccurance: true),
                DropKinds.EggBrown => new Item(ItemIds.EggBrown, 1),
                DropKinds.EggWhite => new Item(ItemIds.EggWhite, 1),
                DropKinds.EggGold => new Item(ItemIds.EggGold, 1),
                DropKinds.GrenadeFlash => new Item(ItemIds.GrenadeFlash, 1),
                DropKinds.GrenadeHeavy => new Item(ItemIds.GrenadeHeavy, 1),
                DropKinds.GrenadeLight => new Item(ItemIds.GrenadeLight, 1),
                DropKinds.Gunpowder => GetRandomGunpowder(rng),
                DropKinds.HerbG => new Item(ItemIds.HerbG, 1),
                DropKinds.HerbGG => new Item(ItemIds.HerbGG, 1),
                DropKinds.HerbGGY => new Item(ItemIds.HerbGGY, 1),
                DropKinds.HerbGGG => new Item(ItemIds.HerbGGG, 1),
                DropKinds.HerbGR => new Item(ItemIds.HerbGR, 1),
                DropKinds.HerbGRY => new Item(ItemIds.HerbGRY, 1),
                DropKinds.HerbGY => new Item(ItemIds.HerbGY, 1),
                DropKinds.HerbR => new Item(ItemIds.HerbR, 1),
                DropKinds.HerbRY => new Item(ItemIds.HerbRY, 1),
                DropKinds.HerbY => new Item(ItemIds.HerbY, 1),
                DropKinds.Knife => GetRandomSingleItem(rng, ItemKinds.Knife, allowReoccurance: true),
                DropKinds.Money => GetRandomMoney(rng, settings),
                DropKinds.ResourceLarge => new Item(ItemIds.ResourcesLarge, 1),
                DropKinds.ResourceSmall => new Item(ItemIds.ResourcesSmall, 1),
                DropKinds.TokenSilver => new Item(ItemIds.TokenSilver, 1),
                DropKinds.TokenGold => new Item(ItemIds.TokenGold, 1),

                // High value
                DropKinds.Attachment => GetRandomSingleItem(rng, ItemKinds.Attachment),
                DropKinds.CasePerk => GetRandomSingleItem(rng, ItemKinds.CasePerk),
                DropKinds.CaseSize => GetRandomSingleItem(rng, ItemKinds.CaseSize),
                DropKinds.Charm => GetRandomSingleItem(rng, ItemKinds.Charm),
                DropKinds.Recipe => GetRandomSingleItem(rng, ItemKinds.Recipe),
                DropKinds.SmallKey => new Item(ItemIds.SmallKey, 1),
                DropKinds.Treasure => GetRandomSingleItem(rng, ItemKinds.Treasure, allowReoccurance: true),
                DropKinds.Weapon => GetRandomSingleItem(rng, ItemKinds.Weapon),

                _ => null,
            };
        }

        public Item? GetNextGeneralDrop(Rng rng, RandomItemSettings settings)
        {
            var bag = CreateGeneralItemPool(settings, rng);
            var kind = bag.Next();
            return GetRandomDrop(rng, kind, settings);
        }

        public EndlessBag<string> CreateGeneralItemPool(RandomItemSettings settings, Rng rng)
        {
            if (!_generalDrops.TryGetValue(settings, out var result))
            {
                var ratios = new Dictionary<string, double>();
                foreach (var dropKind in DropKinds.GenericAll)
                {
                    var ratio = settings.GetItemRatio(dropKind);
                    if (ratio > 0)
                    {
                        ratios.Add(dropKind, ratio);
                    }
                }

                if (ratios.Count == 0)
                    return new EndlessBag<string>(rng, [DropKinds.None]);

                var smallestRatio = ratios.Min(x => x.Value);
                foreach (var k in ratios.Keys)
                {
                    ratios[k] = ratios[k] / smallestRatio;
                }

                var pool = new List<string>();
                foreach (var kvp in ratios)
                {
                    for (var i = 0; i < kvp.Value; i++)
                    {
                        pool.Add(kvp.Key);
                    }
                }
                result = new EndlessBag<string>(rng, pool);
                _generalDrops[settings] = result;
            }
            return result;
        }

        private Item? GetRandomSingleItem(Rng rng, string kind, string? classification = null, bool allowReoccurance = false)
        {
            ItemDefinition? itemDefinition;
            switch (kind)
            {
                case ItemKinds.Weapon:
                    itemDefinition = GetRandomWeapon(rng, classification, allowReoccurance);
                    break;
                case ItemKinds.Attachment:
                    itemDefinition = GetRandomAttachment(rng, classification, allowReoccurance);
                    break;
                default:
                    itemDefinition = GetRandomItemDefinition(rng, kind, classification, allowReoccurance);
                    break;
            }
            if (itemDefinition != null)
                return new Item(itemDefinition.Id, 1);
            return null;
        }

        public Item? GetRandomAmmo(int? itemId, Rng rng, RandomItemSettings settings)
        {
            var itemDef = itemId == null
                ? GetRandomItemDefinition(rng, ItemKinds.Ammo)
                : ItemDefinitionRepository.Default.Find(itemId.Value);
            if (itemDef == null)
                return null;

            var min = settings.MinAmmoQuantity;
            var max = settings.MaxAmmoQuantity;
            var minAmount = Math.Max(1, (int)Math.Round(min * itemDef.Stack));
            var maxAmount = Math.Min(itemDef.Stack, (int)Math.Round(max * itemDef.Stack));
            var amount = rng.Next(minAmount, maxAmount + 1);
            return new Item(itemDef.Id, amount);
        }

        public Item GetRandomMoney(Rng rng, RandomItemSettings settings)
        {
            var min = Math.Max(settings.MinMoneyQuantity, 1);
            var max = Math.Min(settings.MaxMoneyQuantity, 1000000);
            var value = rng.Next(min, max + 1);
            return new Item(ItemIds.Money, value);
        }

        public Item? GetRandomTreasure(Rng rng, int classNumber)
        {
            var min = GetMaxTreasureValue(classNumber + 1);
            var max = GetMaxTreasureValue(classNumber);
            return GetRandomTreasure(rng, min, max);
        }

        static int GetMaxTreasureValue(int cn)
        {
            var f = Math.Pow(Math.Clamp((6 - cn) / 5.0, 0, 1), 2);
            var v = 2500 + ((30000 - 2500) * f);
            return (int)Math.Clamp(v, 2500, 30000);
        }

        public Item? GetRandomTreasure(Rng rng, int minValue, int maxValue)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var treasureItems = itemRepo.KindToItemMap[ItemKinds.Treasure]
                .Shuffle(rng)
                .OrderBy(x => x.Value)
                .Where(x => x.Value >= minValue && x.Value <= maxValue)
                .ToArray();

            if (treasureItems.Length == 0)
            {
                return new Item(ItemIds.Spinel);
            }

            var def = rng.Next(treasureItems);
            return new Item(def.Id, 1);
        }

        public static Item GetRandomGunpowder(Rng rng)
        {
            return new Item(ItemIds.Gunpowder, rng.Next(1, 11));
        }

        public void MarkItemPlaced(int id)
        {
            _placedItemIds.Add(id);
        }

        public bool IsItemPlaced(int id) => _placedItemIds.Contains(id);
    }

    public class RandomItemSettings
    {
        public double MinAmmoQuantity { get; set; }
        public double MaxAmmoQuantity { get; set; }
        public int MinMoneyQuantity { get; set; }
        public int MaxMoneyQuantity { get; set; }
        public Func<string, double>? ItemRatioKeyFunc { get; set; }

        public double GetItemRatio(string dropKind)
        {
            return ItemRatioKeyFunc?.Invoke(dropKind) ?? 0;
        }
    }
}
