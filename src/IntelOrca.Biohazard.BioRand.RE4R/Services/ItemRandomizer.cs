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
        private Rng.Table<string>? _itemRngTable;

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
                ItemClasses.Rifle, ItemClasses.Smg, ItemClasses.Magnum };

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

        public Item? GetRandomDrop(Rng rng)
        {
            var table = GetItemRngTable(rng);
            var dropKind = table.Next();
            return dropKind switch
            {
                // General
                DropKinds.None => null,
                DropKinds.Automatic => new Item(-1, 0),
                DropKinds.Ammo => GetRandomAmmo(rng),
                DropKinds.Fas => new Item(ItemIds.FirstAidSpray, 1),
                DropKinds.Fish => GetRandomItem(rng, ItemKinds.Fish),
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
                DropKinds.Knife => GetRandomItem(rng, ItemKinds.Knife),
                DropKinds.Money => GetRandomItem(rng, ItemKinds.Money),
                DropKinds.ResourceLarge => new Item(ItemIds.ResourcesLarge, 1),
                DropKinds.ResourceSmall => new Item(ItemIds.ResourcesSmall, 1),
                DropKinds.TokenSilver => new Item(ItemIds.TokenSilver, 1),
                DropKinds.TokenGold => new Item(ItemIds.TokenGold, 1),

                // High value
                DropKinds.Attachment => GetRandomItem(rng, ItemKinds.Attachment),
                DropKinds.CasePerk => GetRandomItem(rng, ItemKinds.CasePerk),
                DropKinds.CaseSize => GetRandomItem(rng, ItemKinds.CaseSize),
                DropKinds.Charm => GetRandomItem(rng, ItemKinds.Charm),
                DropKinds.Recipe => GetRandomItem(rng, ItemKinds.Recipe),
                DropKinds.SmallKey => new Item(ItemIds.SmallKey, 1),
                DropKinds.Treasure => GetRandomItem(rng, ItemKinds.Treasure),
                DropKinds.Weapon => GetRandomItem(rng, ItemKinds.Weapon),

                _ => null,
            };
        }

        private Rng.Table<string> GetItemRngTable(Rng rng)
        {
            if (_itemRngTable == null)
            {
                var table = rng.CreateProbabilityTable<string>();
                foreach (var dropKind in DropKinds.Generic)
                {
                    var ratio = _randomizer.GetConfigOption<double>($"drop-ratio-{dropKind}");
                    if (ratio != 0)
                    {
                        table.Add(dropKind, ratio);
                    }
                }
                if (table.IsEmpty)
                    table.Add(DropKinds.None, 1);
                _itemRngTable = table;
            }
            return _itemRngTable;
        }

        public Item? GetRandomItem(Rng rng, string kind, string? classification = null, bool allowReoccurance = false)
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
                case ItemKinds.Ammo:
                    return GetRandomAmmo(rng);
                case ItemKinds.Money:
                    return GetRandomMoney(rng);
                default:
                    itemDefinition = GetRandomItemDefinition(rng, kind, classification, allowReoccurance);
                    break;
            }
            if (itemDefinition != null)
                return new Item(itemDefinition.Id, 1);
            return null;
        }

        public Item GetRandomAmmo(Rng rng)
        {
            // TODO this should be percentage of a stack
            var multiplier = _randomizer.GetConfigOption<double>("ammo-quantity");
            var amount = Math.Max(1, (int)(rng.Next(10, 50) * multiplier));
            return new Item(ItemIds.Gunpowder, amount);
        }

        public Item GetRandomMoney(Rng rng)
        {
            var min = _randomizer.GetConfigOption("money-drop-min", 1);
            var max = _randomizer.GetConfigOption("money-drop-max", 1000);
            var value = rng.Next(min, max + 1);
            return new Item(ItemIds.Money, value);
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
}
