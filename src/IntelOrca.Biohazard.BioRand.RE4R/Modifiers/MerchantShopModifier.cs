using System;
using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class MerchantShopModifier : Modifier
    {
        private ChainsawMerchantShop? _shop;

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _shop ??= ChainsawMerchantShop.FromData(randomizer.FileRepository);
            var shop = _shop;
            var itemRepo = ItemDefinitionRepository.Default;

            var chapterItems = shop.ShopItems
                .GroupBy(x => x.UnlockChapter)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var chapterRewards = shop.Rewards
                .GroupBy(x => x.StartChapter)
                .ToDictionary(x => x.Key, x => x.OrderBy(x => x.SpinelCount).ToArray());

            var chapters = chapterItems.Keys
                .Concat(chapterRewards.Keys)
                .Distinct()
                .Order()
                .ToArray();

            foreach (var chapter in chapters)
            {
                var pushedHeading = false;
                var rewards = chapterRewards.GetValueOrDefault(chapter, []);
                var items = chapterItems.GetValueOrDefault(chapter, []);
                var chapterNumber = chapter + 1;
                foreach (var reward in rewards)
                {
                    var item = itemRepo.Find(reward.ItemId);
                    if (item == null)
                        continue;

                    if (!pushedHeading)
                    {
                        pushedHeading = true;
                        logger.Push($"Chapter {chapterNumber}");
                    }

                    logger.LogLine($"{item} | Spinels = {reward.SpinelCount}");
                }
                foreach (var shopItem in items)
                {
                    if (shopItem.BuyPrice <= 0 || shopItem.UnlockCondition == 4)
                        continue;

                    var item = itemRepo.Find(shopItem.ItemId);
                    if (item == null)
                        continue;

                    if (!pushedHeading)
                    {
                        pushedHeading = true;
                        logger.Push($"Chapter {chapterNumber}");
                    }

                    var sellString = shopItem.SellPrice == -1 ? "" : $"Sell = {shopItem.SellPrice:n0}";
                    logger.LogLine($"{item} | Buy = {shopItem.BuyPrice:n0} {sellString:n0}");

                    var sales = shopItem.Sales;
                    if (sales.Length != 0)
                    {
                        logger.Push();
                        foreach (var sale in shopItem.Sales)
                        {
                            logger.LogLine($"{-sale.SaleRate}% discount between chapter {sale.StartTiming + 1} and {sale.EndTiming + 1}");
                        }
                        logger.Pop();
                    }
                }
                if (pushedHeading)
                    logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-merchant"))
                return;

            _shop ??= ChainsawMerchantShop.FromData(randomizer.FileRepository);

            var rng = randomizer.CreateRng();
            var internalRandomizer = new ShopRandomizer(rng, randomizer, _shop, logger);
            internalRandomizer.Go();
        }

        private class ShopRandomizer(
            Rng rng,
            ChainsawRandomizer randomizer,
            ChainsawMerchantShop shop,
            RandomizerLogger logger)
        {
            private readonly List<AvailableItem> _availableItems = new List<AvailableItem>();

            private readonly Dictionary<int, int> itemToChapterMap = new Dictionary<int, int>();
            private readonly List<int> _rewardWeapons = new List<int>();

            private readonly Rng _distRng = rng.NextFork();
            private readonly Rng _rewardsRng = rng.NextFork();
            private readonly Rng _shopRng = rng.NextFork();
            private readonly Rng _priceRng = rng.NextFork();

            private ItemRandomizer ItemRandomizer => randomizer.ItemRandomizer;

            public void Go()
            {
                DistributeWeapons();
                DistributeCaseSizes();
                DistributeMiscItems();
                DistributeCriticalItems();
                DistributeCharms();
                DistributeTreasures();
                RandomizeDiscounts();
                LogAvailableItems();

                SetRewards();
                SetShop();
                shop.Save(randomizer.FileRepository);
            }

            private void DistributeWeapons()
            {
                var rng = _distRng;
                var weaponDistributor = randomizer.WeaponDistributor;
                var startingWeapons = weaponDistributor.GetStartingWeapons();
                var weapons = weaponDistributor.GetWeaponsForShop();
                foreach (var kvp in weapons)
                {
                    var chapter = kvp.Key;
                    foreach (var w in kvp.Value)
                    {
                        var isReward = !startingWeapons.Contains(w) && rng.NextProbability(25);
                        var item = CreateAvailableItem(w);
                        item.UnlockChapter = chapter;
                        RandomizePrice(item, spinel: isReward);
                    }
                }
            }

            private void DistributeCaseSizes()
            {
                var rng = _distRng;
                var itemRepo = ItemDefinitionRepository.Default;
                var caseSizes = itemRepo
                    .GetAll(ItemKinds.CaseSize)
                    .OrderBy(x => x.Value)
                    .ToArray();

                var caseChapter = rng.Next(0, 3);
                foreach (var caseSize in caseSizes)
                {
                    var item = CreateAvailableItem(caseSize);
                    item.UnlockChapter = caseChapter;
                    RandomizePrice(item, spinel: rng.NextProbability(25));
                    caseChapter += rng.Next(1, 3);
                }
            }

            private void DistributeMiscItems()
            {
                var itemRepo = ItemDefinitionRepository.Default;

                var recipeDefs = itemRepo.KindToItemMap[ItemKinds.Recipe];
                foreach (var def in recipeDefs)
                {
                    if (ItemRandomizer.IsItemPlaced(def.Id))
                        continue;

                    var item = CreateAvailableItem(def);
                    item.UnlockChapter = rng.Next(1, 6);
                    RandomizePrice(item, spinel: rng.NextProbability(25));
                }

                var casePerks = itemRepo.KindToItemMap[ItemKinds.CasePerk];
                foreach (var def in casePerks)
                {
                    if (ItemRandomizer.IsItemPlaced(def.Id))
                        continue;

                    var item = CreateAvailableItem(def);
                    item.UnlockChapter = rng.Next(1, 11);
                    RandomizePrice(item, spinel: true);
                }

                for (var i = 0; i < rng.Next(1, 3); i++)
                {
                    var item = CreateAvailableItem(ItemIds.ExclusiveUpgradeTicket);
                    item.UnlockChapter = rng.Next(1, 10);
                    RandomizePrice(item, spinel: true);
                }

                var armour = CreateAvailableItem(ItemIds.BodyArmor);
                RandomizePrice(armour, spinel: rng.NextProbability(50));

                var velvetBlue = CreateAvailableItem(ItemIds.VelvetBlue);
                velvetBlue.SpinelPrice = 1;
                velvetBlue.MaxStock = -1;
                velvetBlue.UnlimitedReward = true;

                if (rng.NextProbability(10))
                {
                    var item = CreateAvailableItem(ItemIds.EggGold);
                    item.UnlockChapter = rng.Next(1, 6);
                    RandomizePrice(item, spinel: true);
                }

                if (rng.NextProbability(50))
                {
                    var item = CreateAvailableItem(ItemIds.SmallKey);
                    RandomizePrice(item, spinel: rng.NextProbability(25));
                }

                var randomItems = new[]
                {
                    ItemIds.EggBrown,
                    ItemIds.EggWhite,
                    ItemIds.BlackBassSmall,
                    ItemIds.BlackBassLarge,
                    ItemIds.HerbG,
                    ItemIds.HerbR,
                    ItemIds.HerbY,
                    ItemIds.HerbGG,
                    ItemIds.HerbGGG,
                    ItemIds.HerbGGY,
                    ItemIds.HerbGR,
                    ItemIds.HerbGRY,
                    ItemIds.HerbRY,
                };
                foreach (var itemId in randomItems)
                {
                    if (rng.NextProbability(50))
                    {
                        var item = CreateAvailableItem(itemId);
                        item.UnlockChapter = rng.Next(1, 6);
                        if (rng.NextProbability(25))
                        {
                            item.InitialStock = rng.Next(5, 10);
                            item.MaxStock = rng.Next(5, 10);
                            item.StockPerChapter = rng.Next(1, 5);
                        }
                        RandomizePrice(item, spinel: rng.NextProbability(10));
                    }
                }

                var ammoItemDefs = itemRepo.KindToItemMap[ItemKinds.Ammo];
                foreach (var ammoItemDef in ammoItemDefs)
                {
                    var item = CreateAvailableItem(ammoItemDef.Id);
                    item.InitialStock = rng.Next(10, 50);
                    item.StockPerChapter = rng.Next(10, 50);
                    item.MaxStock = rng.Next(50, 100);
                    RandomizePrice(item, spinel: false);
                }

                var knifeDefs = itemRepo.KindToItemMap[ItemKinds.Knife];
                foreach (var def in knifeDefs)
                {
                    var item = CreateAvailableItem(def.Id);
                    item.InitialStock = rng.Next(0, 10);
                    item.StockPerChapter = rng.Next(5, 15);
                    item.MaxStock = rng.Next(30, 50);
                    RandomizePrice(item, spinel: false);
                }

                var grenades = new[]
                {
                    ItemIds.GrenadeFlash,
                    ItemIds.GrenadeLight,
                    ItemIds.GrenadeHeavy,
                };
                foreach (var itemId in grenades)
                {
                    var item = CreateAvailableItem(itemId);
                    item.UnlockChapter = rng.Next(1, 4);
                    item.InitialStock = rng.Next(0, 5);
                    item.StockPerChapter = rng.Next(1, 5);
                    item.MaxStock = rng.Next(10, 20);
                    RandomizePrice(item, spinel: false);
                    if (rng.NextProbability(25))
                    {
                        RandomizePrice(item, spinel: true);
                    }
                }

                var tokens = itemRepo.KindToItemMap[ItemKinds.Token];
                foreach (var def in tokens)
                {
                    if (rng.NextProbability(75))
                        continue;

                    var item = CreateAvailableItem(def);
                    item.UnlockChapter = rng.Next(1, 11);
                    RandomizePrice(item, spinel: true);
                }
            }

            private void DistributeCriticalItems()
            {
                var itemRepo = ItemDefinitionRepository.Default;
                var criticalItems = new[]
                {
                    ItemIds.Gunpowder,
                    ItemIds.ResourcesSmall,
                    ItemIds.FirstAidSpray,
                    ItemIds.ResourcesLarge,
                };
                foreach (var itemId in criticalItems)
                {
                    var itemDef = itemRepo.Find(itemId)!;
                    var item = CreateAvailableItem(itemDef);
                    if (itemId == ItemIds.Gunpowder)
                        item.Quantity = 10;
                    item.StockPerChapter = (float)rng.NextDouble(5, 10);
                    item.MaxStock = rng.Next(10, 30);
                    RandomizePrice(item, spinel: false);
                    if (rng.NextProbability(25))
                        RandomizePrice(item, spinel: true);
                }
            }

            private void DistributeCharms()
            {
                for (var i = 0; i < rng.Next(10, 20); i++)
                {
                    var charm = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Charm, allowReoccurance: false);
                    if (charm != null)
                    {
                        var item = CreateAvailableItem(charm);
                        item.UnlockChapter = rng.Next(0, 10);
                        RandomizePrice(item, spinel: rng.NextProbability(25));
                    }
                }
            }

            private void DistributeTreasures()
            {
                var itemRepo = ItemDefinitionRepository.Default;
                var teasureCount = rng.Next(0, 8);
                var treasures = itemRepo.KindToItemMap[ItemKinds.Treasure]
                    .Where(x => x.Id != ItemIds.VelvetBlue)
                    .Shuffle(rng)
                    .Take(teasureCount)
                    .ToArray();

                foreach (var treasure in treasures)
                {
                    var item = CreateAvailableItem(treasure);
                    item.UnlockChapter = rng.Next(0, 10);
                    RandomizePrice(item, spinel: true);
                }
            }

            private void RandomizeDiscounts()
            {
                foreach (var item in _availableItems)
                {
                    if (_shopRng.NextProbability(25))
                    {
                        var startChapter = _shopRng.Next(item.UnlockChapter, item.UnlockChapter + 3);
                        var endChapter = _shopRng.Next(startChapter + 1, startChapter + 3);

                        item.DiscountStartChapter = startChapter;
                        item.DiscountEndChapter = endChapter;
                        item.Discount = _shopRng.Next(1, 8) * 10;
                    }
                }
            }

            private void LogAvailableItems()
            {
                logger.Push("Available items");
                foreach (var item in _availableItems)
                {
                    logger.LogLine(item);
                }
                logger.Pop();
            }

            private void SetRewards()
            {
                shop.ClearRewards();
                foreach (var item in _availableItems)
                {
                    if (item.SpinelPrice == 0)
                        continue;

                    if (shop.Rewards.Length >= 30)
                        return;

                    var unlockChapter = item.UnlockChapter;
                    if (unlockChapter == 1)
                        unlockChapter = 0;

                    var reward = shop.AddReward(new Item(item.ItemDefinition.Id, item.Quantity), item.SpinelPrice, item.UnlimitedReward, unlockChapter);
                    // logger.LogLine($"Add reward {reward.RewardId} {item} Cost = {reward.SpinelCount} spinel Chapter = {reward.StartChapter}");
                }
            }

            private void SetShop()
            {
                var itemRandomizer = randomizer.ItemRandomizer;
                var itemRepo = ItemDefinitionRepository.Default;
                var shopItems = shop.ShopItems;
                foreach (var shopItem in shopItems)
                {
                    var item = _availableItems.FirstOrDefault(x => x.ItemDefinition.Id == shopItem.ItemId);
                    if (item == null || item.BuyPrice == 0)
                    {
                        if (shopItem.BuyPrice != -1)
                        {
                            var itemDefinition = itemRepo.Find(shopItem.ItemId);
                            if (itemDefinition != null)
                            {
                                item = new AvailableItem(itemDefinition);
                                RandomizePrice(item, spinel: false);
                                shopItem.BuyPrice = item.BuyPrice;
                                shopItem.SellPrice = item.SellPrice;
                            }

                            shopItem.UnlockCondition = 4;
                            shopItem.UnlockFlag = Guid.Empty;
                            shopItem.UnlockChapter = 0;
                            shopItem.SpCondition = 1;
                        }
                    }
                    else
                    {
                        shopItem.BuyPrice = item.BuyPrice;
                        shopItem.SellPrice = item.SellPrice;
                        shopItem.UnlockCondition = 2;
                        shopItem.UnlockFlag = Guid.Empty;
                        shopItem.UnlockChapter = Math.Max(0, item.UnlockChapter - 1);
                        shopItem.SpCondition = 1;
                        shopItem.EnableStockSetting = item.MaxStock != 0;
                        shopItem.EnableSelectCount = item.MaxStock != 0;
                        shopItem.MaxStock = item.MaxStock;
                        shopItem.DefaultStock = item.InitialStock;
                    }

                    // Make items unlock at first chapter work
                    if (shopItem.UnlockCondition != 4 && shopItem.UnlockChapter <= 0)
                    {
                        shopItem.UnlockChapter = 0;
                        shopItem.UnlockCondition = 0;
                        shopItem.SpCondition = 0;
                    }

                    var isAvailable = shopItem.UnlockCondition == 2 && shopItem.BuyPrice > 0;
                    if (item != null)
                    {
                        logger.LogLine($"Shop item {item.ItemDefinition.Name} Buy = {shopItem.BuyPrice} Sell = {shopItem.SellPrice} Available = {isAvailable} Unlock = {shopItem.UnlockChapter}");
                    }

                    // Sale change
                    if ((item?.Discount ?? 0) != 0)
                    {
                        shopItem.SetSale(shop, item!.DiscountStartChapter, item.DiscountEndChapter, -item.Discount);
                        // logger.LogLine($"    {item.Discount}% discount at chapter {item.DiscountStartChapter} to {item.DiscountEndChapter}");
                    }
                    else
                    {
                        shopItem.Sales = [];
                    }
                }
            }

            private void AddReward(int itemId, int count = 1, int? spinel = null, bool unlimited = false)
            {
                if (shop.Rewards.Length >= 30)
                    return;

                var itemRepo = ItemDefinitionRepository.Default;
                var itemDefinition = itemRepo.Find(itemId);
                if (itemDefinition == null)
                    return;

                if (spinel == null)
                {
                    var avgSpinel = itemDefinition.Value / 2500;
                    var minSpinel = Math.Max(1, avgSpinel - 2);
                    var maxSpinel = avgSpinel + 1;
                    spinel = _rewardsRng.Next(minSpinel, maxSpinel + 1);
                }
                else
                {
                    spinel = Math.Max(1, spinel.Value);
                }

                var item = new Item(itemId, count);
                var startChapter = Math.Max(0, _rewardsRng.Next(-3, 6));
                if (itemToChapterMap.TryGetValue(itemId, out var chapter))
                {
                    startChapter = chapter;
                }
                if (startChapter == 1)
                    startChapter = 0;
                var reward = shop.AddReward(new Item(itemId, count), spinel.Value, false, startChapter);
                logger.LogLine($"Add reward {reward.RewardId} {item} Cost = {spinel} spinel Chapter = {reward.StartChapter}");
            }

            private void SetItemChapter(int itemId, int chapter)
            {
                itemToChapterMap.Add(itemId, chapter);
            }

            private AvailableItem CreateAvailableItem(int itemId)
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(itemId)!;
                return CreateAvailableItem(itemDefinition);
            }

            private AvailableItem CreateAvailableItem(ItemDefinition itemDefinition)
            {
                var result = new AvailableItem(itemDefinition);
                _availableItems.Add(result);
                return result;
            }

            private void RandomizePrice(AvailableItem item, bool spinel)
            {
                var itemId = item.ItemDefinition.Id;
                if (spinel)
                {
                    item.SpinelPrice = item.ItemDefinition.Kind switch
                    {
                        ItemKinds.Weapon => _rewardsRng.Next(4, 13),
                        ItemKinds.Attachment => _rewardsRng.Next(4, 13),
                        ItemKinds.CaseSize => _rewardsRng.Next(6, 13),
                        ItemKinds.Armor => _rewardsRng.Next(4, 16),
                        _ when itemId == ItemIds.ExclusiveUpgradeTicket => _rewardsRng.Next(15, 40),
                        _ => _rewardsRng.Next(1, 4),
                    };
                }
                else
                {
                    var shopItem = shop.ShopItems.FirstOrDefault(x => x.ItemId == item.ItemDefinition.Id);
                    if (shopItem?.BuyPrice > 0)
                    {
                        item.BuyPrice = shopItem.BuyPrice;
                        item.SellPrice = shopItem.SellPrice;
                    }
                    else if (shopItem?.SellPrice > 0)
                    {
                        item.BuyPrice = shopItem.SellPrice * 2;
                        item.SellPrice = shopItem.SellPrice;
                    }
                    else
                    {
                        item.BuyPrice = item.ItemDefinition.Value;
                        item.SellPrice = item.ItemDefinition.Value / 2;
                    }

                    if (randomizer.GetConfigOption<bool>("random-merchant-prices"))
                    {
                        var priceMultiplier = _priceRng.NextDouble(0.25, 2);
                        item.BuyPrice = (item.BuyPrice * priceMultiplier).RoundPrice();
                        item.SellPrice = (item.SellPrice * priceMultiplier).RoundPrice();
                    }
                }
            }

            private class AvailableItem(ItemDefinition itemDefinition)
            {
                public ItemDefinition ItemDefinition => itemDefinition;

                public int Quantity { get; set; } = 1;
                public int BuyPrice { get; set; }
                public int SellPrice { get; set; }
                public int SpinelPrice { get; set; }
                public int UnlockChapter { get; set; }
                public int Discount { get; set; }
                public int DiscountStartChapter { get; set; }
                public int DiscountEndChapter { get; set; }
                public float StockPerChapter { get; set; }
                public int InitialStock { get; set; }
                public int MaxStock { get; set; }
                public bool UnlimitedReward { get; set; }

                public override string ToString()
                {
                    var parts = new List<string>();
                    parts.Add($"Chapter {UnlockChapter}:");
                    parts.Add(ItemDefinition.ToString());
                    if (Quantity > 1)
                    {
                        parts.Add($"x{Quantity}");
                    }
                    if (BuyPrice != 0)
                    {
                        parts.Add($"Buy = {BuyPrice} Sell = {SellPrice}");
                    }
                    if (SpinelPrice != 0)
                    {
                        parts.Add($"Spinel = {SpinelPrice}");
                    }
                    if (Discount != 0)
                    {
                        parts.Add($"Discount = {Discount}% from Chapter {DiscountStartChapter} to Chapter {DiscountEndChapter}");
                    }
                    if (StockPerChapter != 0)
                    {
                        parts.Add($"Stock = +{StockPerChapter:0.00} / {MaxStock}");
                    }
                    if (UnlimitedReward)
                    {
                        parts.Add($"Unlimited");
                    }
                    return string.Join(" ", parts);
                }
            }
        }
    }
}
