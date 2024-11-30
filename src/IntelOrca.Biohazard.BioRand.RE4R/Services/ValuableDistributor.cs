using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class ValuableDistributor(ChainsawRandomizer randomizer)
    {
        private readonly List<DistributedItem> _distributedItems = new();

        public void Setup(ItemRandomizer itemRandomizer, Rng rng, RandomizerLogger logger)
        {
            AssumeStartingItems(itemRandomizer);
            RandomizeStartLoadout(itemRandomizer, rng);
            RandomizeChapters(itemRandomizer, rng);
            foreach (var kind in _kinds)
                RandomizeDiscovery(kind, rng);
            LogDistribution(logger);
        }

        private static void AssumeStartingItems(ItemRandomizer itemRandomizer)
        {
            foreach (var id in _startingItems)
            {
                itemRandomizer.MarkItemPlaced(id);
            }
        }

        private void RandomizeStartLoadout(ItemRandomizer itemRandomizer, Rng rng)
        {
            var primaryWeaponKind = GetWeaponKind("inventory-weapon-primary", rng);
            var secondaryWeaponKind = GetWeaponKind("inventory-weapon-secondary", rng, primaryWeaponKind);
            var primaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, primaryWeaponKind, allowReoccurance: false);
            var secondaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, secondaryWeaponKind, allowReoccurance: false);
            AddItem(0, primaryWeaponDefinition);
            AddItem(0, secondaryWeaponDefinition);
            AddItem(0, itemRandomizer.GetRandomWeapon(rng, ItemClasses.Knife, allowReoccurance: false));
            foreach (var weapon in new ItemDefinition?[] { primaryWeaponDefinition, secondaryWeaponDefinition })
            {
                if (weapon != null && rng.NextProbability(50))
                {
                    var attachment = itemRandomizer.GetRandomAttachment(rng, weapon, allowReoccurance: false);
                    if (attachment != null)
                    {
                        AddItem(0, attachment);
                        break;
                    }
                }
            }
        }

        private string GetWeaponKind(string key, Rng rng, string? avoid = null)
        {
            var classes = new List<string>();
            foreach (var sw in ItemClasses.StartingWeapons)
            {
                var fullKey = $"{key}-{sw}";
                var value = randomizer.GetConfigOption(fullKey, true);
                if (value)
                {
                    classes.Add(sw);
                }
            }

            if (avoid != null)
            {
                var exceptAvoid = classes.Except([avoid]).ToList();
                if (exceptAvoid.Count >= 1 && !exceptAvoid.Contains(ItemClasses.None))
                {
                    classes = exceptAvoid;
                }
            }

            if (classes.Count == 0)
                return ItemClasses.None;
            return rng.Next(classes);
        }

        private void RandomizeChapters(ItemRandomizer itemRandomizer, Rng rng)
        {
            var numCharms = randomizer.GetConfigOption("valuable-limit-charm", 0);
            var numSmallKeysPerBag = 6;

            var bagEarly = new EndlessBag<int>(rng, [1, 2, 3, 4]);
            var bagMiddle = new EndlessBag<int>(rng, [5, 6, 7, 8, 9, 10, 11]);
            var bagMiddleLate = new EndlessBag<int>(rng, [5, 6, 7, 8, 9, 10, 11, 12, 13, 14]);
            var bagAll = new EndlessBag<int>(rng, [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14]);
            if (randomizer.Campaign == Campaign.Ada)
            {
                numSmallKeysPerBag = 3;
                bagEarly = new EndlessBag<int>(rng, [1, 2, 3]);
                bagMiddle = new EndlessBag<int>(rng, [4, 5]);
                bagMiddleLate = new EndlessBag<int>(rng, [4, 5, 6, 7]);
                bagAll = new EndlessBag<int>(rng, [1, 2, 3, 4, 5, 6, 7]);
            }

            foreach (var kind in _kinds)
            {
                if (kind == ItemKinds.SmallKey)
                {
                    var itemDefinition = ItemDefinitionRepository.Default.Find(ItemIds.SmallKey);
                    for (var i = 0; i < numSmallKeysPerBag; i++)
                    {
                        AddItem(bagEarly.Next(), itemDefinition);
                    }
                    for (var i = 0; i < numSmallKeysPerBag; i++)
                    {
                        AddItem(bagMiddle.Next(), itemDefinition);
                    }
                }
                else if (kind == ItemKinds.CaseSize)
                {
                    int[] chapterMax = randomizer.GetConfigOption<bool>("early-case-drops", true)
                        ? randomizer.Campaign == Campaign.Leon
                            ? [3, 5, 10, 12]
                            : [3, 4, 5, 6]
                        : randomizer.Campaign == Campaign.Leon
                            ? [16, 16, 16, 16]
                            : [7, 7, 7, 7];
                    var chapters = new List<int>();
                    var endlessBag = new EndlessBag<int>(rng);
                    for (var i = 0; i < chapterMax.Length; i++)
                    {
                        var lastChapterMax = i == 0 ? 0 : chapterMax[i - 1];
                        endlessBag.AddRange(Enumerable.Range(lastChapterMax + 1, chapterMax[i] - lastChapterMax));
                        chapters.Add(endlessBag.Next());
                    }
                    chapters.Sort();

                    // Place case sizes in order
                    var items = ItemDefinitionRepository.Default.KindToItemMap[kind]
                        .Where(x => !itemRandomizer.IsItemPlaced(x.Id))
                        .Zip(chapters)
                        .ToArray();
                    foreach (var (item, chapter) in items)
                    {
                        AddItem(chapter, item);
                    }
                }
                else
                {
                    var count = 0;
                    while (itemRandomizer.GetRandomItemDefinition(rng, kind, allowReoccurance: false) is ItemDefinition itemDefinition)
                    {
                        // Ensure we get essential items before cabin (or castle for SW)
                        var chapter = itemDefinition.Id switch
                        {
                            ItemIds.BiosensorScope => bagEarly.Next(),
                            ItemIds.RecipeBolts1Ammo => bagEarly.Next(),
                            ItemIds.RecipeMinesAmmo => bagEarly.Next(),
                            ItemIds.RecipeFlashGrenade => bagEarly.Next(),
                            ItemIds.RecipeMagnumAmmo =>
                                randomizer.Campaign == Campaign.Leon
                                    ? bagAll.Next()
                                    : bagMiddleLate.Next(),
                            _ => bagAll.Next()
                        };

                        AddItem(chapter, itemDefinition);

                        count++;
                        if (count > numCharms && kind == ItemKinds.Charm)
                            break;
                    }
                }
            }
        }

        private void RandomizeDiscovery(string kind, Rng rng)
        {
            var discoveryProportions = GetDiscoveryProportion(kind);
            var distributedItems = _distributedItems
                .Where(x => x.Definition.Kind == kind)
                .ToArray();
            var startingItems = distributedItems
                .Where(x => x.Chapter == 0)
                .ToArray();
            var nonStartingItems = distributedItems
                .Where(x => x.Chapter != 0)
                .Shuffle(rng);

            foreach (var dGroup in nonStartingItems.GroupByProportion(discoveryProportions))
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

        private Dictionary<ItemDiscovery, double> GetDiscoveryProportion(string kind)
        {
            var discoveries = new Dictionary<ItemDiscovery, double>
            {
                [ItemDiscovery.Shop] = 0.5 * 0.75,
                [ItemDiscovery.Reward] = 0.5 * 0.25,
                [ItemDiscovery.Enemy] = 0.5 * 0.5,
                [ItemDiscovery.Item] = 0.5 * 0.5
            };
            return discoveries
                .Where(x => IsValuableEnabled(x.Key, kind))
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private bool IsValuableEnabled(ItemDiscovery discovery, string kind)
        {
            return discovery switch
            {
                ItemDiscovery.Enemy => randomizer.GetConfigOption($"enemy-drop-valuable-{kind}", false),
                ItemDiscovery.Item => randomizer.GetConfigOption($"item-drop-valuable-{kind}", false),
                ItemDiscovery.Shop => kind != ItemKinds.SmallKey,
                _ => true
            };
        }

        private void SetDiscovery(DistributedItem dItem, ItemDiscovery discovery)
        {
            var index = _distributedItems.IndexOf(dItem);
            if (index != -1)
                _distributedItems[index] = _distributedItems[index].WithDiscovery(discovery);
        }

        public ImmutableArray<DistributedItem> GetItems(int chapter, ItemDiscovery discovery)
        {
            return _distributedItems
                .Where(x => x.Chapter == chapter && x.Discovery == discovery)
                .ToImmutableArray();
        }

        public ImmutableArray<DistributedItem> GetItems(ItemDiscovery discovery)
        {
            return _distributedItems
                .Where(x => x.Discovery == discovery)
                .ToImmutableArray();
        }

        public ImmutableDictionary<int, ImmutableArray<ItemDefinition>> GetItemsForShop()
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
                if (distributedItem.Definition.Kind == ItemKinds.SmallKey)
                {
                    // Don't add small keys to shop
                }
                else if (distributedItem.Discovery == ItemDiscovery.Reward)
                {
                    // Don't add rewards to shop
                }
                else if (distributedItem.Discovery == ItemDiscovery.Start)
                {
                    // Start items are for chapter 1
                    if (chapter == 1)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
                else if (distributedItem.Discovery == ItemDiscovery.Enemy ||
                         distributedItem.Discovery == ItemDiscovery.Item)
                {
                    // Add items that were found from item/enemy to shop in following chapter
                    if (distributedItem.Chapter == chapter - 1)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
                else
                {
                    // Shop only items
                    if (distributedItem.Chapter == chapter)
                    {
                        builder.Add(distributedItem.Definition);
                    }
                }
            }
            return builder.ToImmutable();
        }

        public bool IsAmmoAvailableYet(int itemId, int chapter)
        {
            var itemRepo = ItemDefinitionRepository.Default;

            var weapons = ItemDefinitionRepository.Default.KindToItemMap[ItemKinds.Weapon];
            foreach (var weapon in weapons)
            {
                var ditem = _distributedItems.FirstOrDefault(x => x.Definition == weapon);
                if (ditem == null)
                    continue;

                if (ditem.Chapter > chapter)
                    continue;

                var ammoDef = itemRepo.GetAmmo(weapon);
                if (ammoDef?.Id == itemId)
                {
                    return true;
                }
            }
            return false;
        }

        private void LogDistribution(RandomizerLogger logger)
        {
            logger.Push("Valuable Distribution");
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

        private void AddItem(int chapter, ItemDefinition? definition)
        {
            if (definition == null)
                return;

            _distributedItems.Add(new DistributedItem(definition, ItemDiscovery.None, chapter));
        }

        private static readonly ImmutableArray<string> _kinds = [
            ItemKinds.Weapon,
            ItemKinds.Attachment,
            ItemKinds.CaseSize,
            ItemKinds.CasePerk,
            ItemKinds.Recipe,
            ItemKinds.Charm,
            ItemKinds.SmallKey
        ];

        private static readonly ImmutableArray<int> _startingItems = [
            ItemIds.RecipeHandgunAmmo,
            ItemIds.RecipeShotgunAmmo,
            ItemIds.RecipeRifleAmmo,
            ItemIds.RecipeSmgAmmo,
            ItemIds.RecipeHerbGG,
            ItemIds.RecipeHerbGR,
            ItemIds.RecipeHerbGY,
            ItemIds.RecipeHerbRY,
            ItemIds.RecipeHerbGGG,
            ItemIds.RecipeHerbGGY1,
            ItemIds.RecipeHerbGGY2,
            ItemIds.RecipeHerbGRY1,
            ItemIds.RecipeHerbGRY2,
            ItemIds.RecipeHerbGRY3,
            ItemIds.RecipeHerbGRY3,
            ItemIds.Case7x10,
            ItemIds.CaseSilver,
        ];
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
