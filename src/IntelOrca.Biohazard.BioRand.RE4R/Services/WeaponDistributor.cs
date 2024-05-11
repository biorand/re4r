using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class WeaponDistributor(ChainsawRandomizer randomizer)
    {
        private readonly List<DistributedItem> _distributedItems = new();

        public void Setup(ItemRandomizer itemRandomizer, Rng rng, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var allWeapons = itemRepo.GetAll(ItemKinds.Weapon);

            // Start
            var primaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-primary", "handgun")!;
            var secondaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-secondary", "random")!;
            var primaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, primaryWeaponKind, allowReoccurance: false);
            var secondaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, secondaryWeaponKind, allowReoccurance: false);
            AddWeapon(0, primaryWeaponDefinition);
            AddWeapon(0, secondaryWeaponDefinition);
            AddWeapon(0, itemRandomizer.GetRandomWeapon(rng, ItemClasses.Knife, allowReoccurance: false));
            foreach (var weapon in new ItemDefinition?[] { primaryWeaponDefinition, secondaryWeaponDefinition })
            {
                if (weapon != null && rng.NextProbability(50))
                {
                    var attachment = itemRandomizer.GetRandomAttachment(rng, weapon, allowReoccurance: false);
                    if (attachment != null)
                    {
                        AddWeapon(0, attachment);
                        break;
                    }
                }
            }

            // Chapters
            var endlessBag = new EndlessBag<int>(rng, Enumerable.Range(1, 12));
            while (itemRandomizer.GetRandomWeapon(rng, allowReoccurance: false) is ItemDefinition weapon)
            {
                var chapter = endlessBag.Next();
                AddWeapon(chapter, weapon);
            }
            while (itemRandomizer.GetRandomAttachment(rng, allowReoccurance: false) is ItemDefinition attachment)
            {
                var chapter = attachment.Id == ItemIds.BiosensorScope
                    ? rng.Next(1, 5) // Ensure we get biosensor scope before cabin
                    : endlessBag.Next();
                AddWeapon(chapter, attachment);
            }

            RandomizeDiscovery(rng);

            LogDistribution(logger);
        }

        private void RandomizeDiscovery(Rng rng)
        {
            var discoveries = new Dictionary<ItemDiscovery, double>
            {
                [ItemDiscovery.Shop] = 0.5 * 0.75,
                [ItemDiscovery.Reward] = 0.5 * 0.25,
                [ItemDiscovery.Enemy] = 0.5 * 0.5,
                [ItemDiscovery.Item] = 0.5 * 0.5
            };

            var startingItems = _distributedItems
                .Where(x => x.Chapter == 0)
                .ToArray();
            var nonStartingItems = _distributedItems
                .Where(x => x.Chapter != 0)
                .Shuffle(rng);

            foreach (var dGroup in nonStartingItems.GroupByProportion(discoveries))
            {
                foreach (var dItem in dGroup)
                {
                    SetDiscovery(dItem, dGroup.Key);
                }
            }
            foreach (var dItem in startingItems)
            {
                SetDiscovery(dItem, ItemDiscovery.Start);
            }
        }

        private void SetDiscovery(DistributedItem dItem, ItemDiscovery discovery)
        {
            var index = _distributedItems.IndexOf(dItem);
            if (index != -1)
                _distributedItems[index] = _distributedItems[index].WithDiscovery(discovery);
        }

        public ImmutableArray<DistributedItem> GetWeapons(int chapter, ItemDiscovery discovery)
        {
            return _distributedItems
                .Where(x => x.Chapter == chapter && x.Discovery == discovery)
                .ToImmutableArray();
        }

        public ImmutableArray<DistributedItem> GetWeapons(ItemDiscovery discovery)
        {
            return _distributedItems
                .Where(x => x.Discovery == discovery)
                .ToImmutableArray();
        }

        public ImmutableDictionary<int, ImmutableArray<ItemDefinition>> GetWeaponsForShop()
        {
            var result = ImmutableDictionary.CreateBuilder<int, ImmutableArray<ItemDefinition>>();
            var chapters = _distributedItems
                .Select(x => x.Chapter)
                .Distinct()
                .Order()
                .ToArray();
            foreach (var chapter in chapters)
            {
                result[chapter] = GetWeaponsForShop(chapter);
            }
            return result.ToImmutable();
        }

        public ImmutableArray<ItemDefinition> GetWeaponsForShop(int chapter)
        {
            var builder = ImmutableArray.CreateBuilder<ItemDefinition>();
            foreach (var distributedItem in _distributedItems)
            {
                if (distributedItem.Discovery == ItemDiscovery.Start)
                {
                    if (chapter == 1)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
                else if (distributedItem.Discovery == ItemDiscovery.Enemy ||
                         distributedItem.Discovery == ItemDiscovery.Shop)
                {
                    // Add items that were found from item/enemy to shop in following chapter
                    if (distributedItem.Chapter == chapter - 1)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
                else
                {
                    if (distributedItem.Chapter == chapter)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
            }
            return builder.ToImmutable();
        }

        private void LogDistribution(RandomizerLogger logger)
        {
            logger.Push("Weapon Distribution");
            var chapters = _distributedItems
                .GroupBy(x => x.Chapter)
                .OrderBy(x => x.Key)
                .ToArray();
            foreach (var chapterItems in chapters)
            {
                logger.Push($"Chapter {chapterItems.Key}");
                foreach (var dItem in chapterItems)
                {
                    logger.LogLine($"{dItem.Definition} ({dItem.Discovery})");
                }
                logger.Pop();
            }
            logger.Pop();
        }

        private void AddWeapon(int chapter, ItemDefinition? definition)
        {
            if (definition == null)
                return;

            _distributedItems.Add(new DistributedItem(definition, ItemDiscovery.None, chapter));
        }

        private static readonly string[] _weaponClasses = [
            ItemClasses.None,
            ItemClasses.Handgun,
            ItemClasses.Shotgun,
            ItemClasses.Rifle,
            ItemClasses.Smg,
            ItemClasses.Magnum ];
    }

    public class DistributedItem(ItemDefinition definition, ItemDiscovery discovery, int chapter)
    {
        public ItemDefinition Definition => definition;
        public ItemDiscovery Discovery => discovery;
        public int Chapter => chapter;

        public DistributedItem WithDiscovery(ItemDiscovery discovery)
        {
            return new DistributedItem(Definition, discovery, chapter);
        }

        public override string ToString()
        {
            return $"{Definition} Discovery = {Discovery} Chapter = {Chapter}";
        }
    }

    public enum ItemDiscovery
    {
        None,
        Start,
        Enemy,
        Item,
        Shop,
        Reward
    }
}
