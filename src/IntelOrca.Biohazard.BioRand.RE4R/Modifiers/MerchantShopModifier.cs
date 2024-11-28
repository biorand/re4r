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
            _shop ??= ChainsawMerchantShop.FromData(randomizer.FileRepository, randomizer.Campaign);
            var shop = _shop;
            var itemRepo = ItemDefinitionRepository.Default;

            var chapterItems = shop.Items._Datas
                .GroupBy(x => x._UnlockSetting._UnlockTiming)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var firstChapter = randomizer.Campaign == Campaign.Leon ? 0 : 17;
            var chapterRewards = shop.Rewards._Settings
                .GroupBy(x => x._DisplaySetting._StartTiming == 0 ? firstChapter : x._DisplaySetting._StartTiming)
                .ToDictionary(x => x.Key, x => x.OrderBy(x => x._SpinelCount).ToArray());

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
                var chapterNumber = GetChapterNumber(chapter);
                if (chapterNumber < 0)
                    continue;

                foreach (var reward in rewards)
                {
                    var item = itemRepo.Find(reward._RewardItemId);
                    if (item == null)
                        continue;

                    if (!pushedHeading)
                    {
                        pushedHeading = true;
                        logger.Push($"Chapter {chapterNumber}");
                    }

                    logger.LogLine($"{item} | Spinels = {reward._SpinelCount}");
                }
                foreach (var shopItem in items)
                {
                    var corePriceSetting = shopItem._PriceSettings
                        .FirstOrDefault(x => x._Difficulty == 20)
                        ?? shopItem._PriceSettings.FirstOrDefault();
                    var buyPrice = corePriceSetting?._Price._PurchasePrice ?? 0;
                    var sellPrice = corePriceSetting?._Price._SellingPrice ?? 0;

                    if (buyPrice <= 0 || shopItem._UnlockSetting._UnlockCondition == 4)
                        continue;

                    var item = itemRepo.Find(shopItem._ItemId);
                    if (item == null)
                        continue;

                    if (!pushedHeading)
                    {
                        pushedHeading = true;
                        logger.Push($"Chapter {chapterNumber}");
                    }

                    var sellString = sellPrice == -1 ? "" : $"Sell = {sellPrice:n0}";
                    logger.LogLine($"{item} | Buy = {buyPrice:n0} {sellString:n0}");

                    var sales = shopItem._SaleSetting._Settings;
                    if (sales.Count != 0)
                    {
                        logger.Push();
                        foreach (var sale in sales)
                        {
                            var startChapter = GetChapterNumber(sale._StartTiming);
                            var endChapter = GetChapterNumber(sale._EndTiming);
                            logger.LogLine($"{-sale._SaleRate}% discount between chapter {startChapter} and {endChapter}");
                        }
                        logger.Pop();
                    }
                }
                if (pushedHeading)
                    logger.Pop();
            }

            int GetChapterNumber(int chapter) => randomizer.Campaign == Campaign.Ada ? chapter - 16 : chapter + 1;
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-merchant"))
                return;

            _shop ??= ChainsawMerchantShop.FromData(randomizer.FileRepository, randomizer.Campaign);

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

            private int ConvertChapterNumber(int chapter) => randomizer.Campaign == Campaign.Ada ? Math.Max(17, chapter + 17) : chapter;

            public void Go()
            {
                InitializeShop();
                DistributeValuables();
                DistributeStockItems();
                DistributeMiscItems();
                DistributeTreasures();
                RandomizeDiscounts();
                LogAvailableItems();

                SetRewards();
                SetShop();
                SetStock();
                SetSellPrice(ItemIds.SmallKey, _priceRng.Next(1, 6) * 10_000);
                shop.Save(randomizer.FileRepository);
            }

            private void InitializeShop()
            {
                if (randomizer.Campaign == Campaign.Leon)
                {
                    // Add missing shop items
                    AddItemToCategory(ItemIds.SWSawedOffW870, 1);
                    shop.Items._Datas.Add(new chainsaw.InGameShopItemSettingUserdata.Data()
                    {
                        _ItemId = ItemIds.SWSawedOffW870,
                        _PriceSettings = [
                            new chainsaw.gui.shop.ItemPriceSetting()
                            {
                                _Difficulty = 20,
                                _Price = new chainsaw.gui.shop.ItemPrice()
                                {
                                    _PurchasePrice = 12000,
                                    _SellingPrice = 6000,
                                }
                            }
                        ]
                    });
                    AddItemToCategory(ItemIds.XM96E1, 0);
                    shop.Items._Datas.Add(new chainsaw.InGameShopItemSettingUserdata.Data()
                    {
                        _ItemId = ItemIds.XM96E1,
                        _PriceSettings = [
                            new chainsaw.gui.shop.ItemPriceSetting()
                            {
                                _Difficulty = 20,
                                _Price = new chainsaw.gui.shop.ItemPrice()
                                {
                                    _PurchasePrice = 10000,
                                    _SellingPrice = 5000,
                                }
                            }
                        ]
                    });
                }
                else
                {
                    AddItemToCategory(ItemIds.SWChicagoSweeper, 0);
                }

                // All shop items should have one price for all difficulties
                foreach (var item in shop.Items._Datas)
                {
                    var corePriceSetting = item._PriceSettings.RemoveAll(x => x._Difficulty != 20);
                    if (item._PriceSettings.Count == 0)
                    {
                        item._PriceSettings.Add(new chainsaw.gui.shop.ItemPriceSetting()
                        {
                            _Difficulty = 20
                        });
                    }
                }
            }

            private void AddItemToCategory(int itemId, int category)
            {
                var setting = shop.Categories._Settings.FirstOrDefault(x => x._Category == category);
                if (setting != null)
                {
                    setting._Datas.Add(new chainsaw.InGameShopPurchaseCategorySingleSetting.Data()
                    {
                        _ItemId = itemId,
                        _SortPriority = setting._Datas.Max(x => x._SortPriority) + 10
                    });
                }
            }

            private void SetSellPrice(int itemId, int price)
            {
                var shopItem = shop.Items._Datas.First(x => x._ItemId == itemId);
                foreach (var s in shopItem._PriceSettings)
                {
                    s._Price._SellingPrice = price;
                }
            }

            private void DistributeValuables()
            {
                var valuableDistributor = randomizer.ValuableDistributor;
                var weapons = valuableDistributor.GetItemsForShop();
                foreach (var kvp in weapons)
                {
                    var chapter = kvp.Key;
                    foreach (var w in kvp.Value)
                    {
                        var item = CreateAvailableItem(w);
                        item.UnlockChapter = chapter;
                        RandomizePrice(item, spinel: false);
                    }
                }

                var rewards = valuableDistributor.GetItems(ItemDiscovery.Reward);
                foreach (var dItem in rewards)
                {
                    var item = CreateAvailableItem(dItem.Definition);
                    item.UnlockChapter = dItem.Chapter;
                    RandomizePrice(item, spinel: true);
                }

                var startingItems = valuableDistributor.GetItems(ItemDiscovery.Start);
                foreach (var dItem in startingItems)
                {
                    var item = CreateAvailableItem(dItem.Definition);
                    item.UnlockChapter = 0;
                    RandomizePrice(item, spinel: false);
                }
            }

            private void DistributeStockItems()
            {
                var itemRandomizer = randomizer.ItemRandomizer;
                var settings = new RandomItemSettings();

                foreach (var kind in DropKinds.ShopCompatible)
                {
                    var minStock = randomizer.GetConfigOption($"merchant-stock-min-{kind}", 0);
                    var maxStock = randomizer.GetConfigOption($"merchant-stock-max-{kind}", 0);
                    if (maxStock == 0)
                        continue;

                    var drop = itemRandomizer.GetRandomDrop(_distRng, kind, settings);
                    if (!drop.HasValue)
                        continue;

                    var item = CreateAvailableItem(drop.Value.Id);
                    item.InitialStock = rng.Next(minStock, maxStock + 1);
                    item.StockPerChapter = rng.NextFloat(Math.Max(minStock, 0.1f), maxStock);
                    item.MaxStock = 1000;
                    RandomizePrice(item, spinel: false);
                }
            }

            private void DistributeMiscItems()
            {
                for (var i = 0; i < rng.Next(1, 3); i++)
                {
                    var item = CreateAvailableItem(ItemIds.ExclusiveUpgradeTicket);
                    item.UnlockChapter = rng.Next(1, randomizer.Campaign == Campaign.Leon ? 10 : 8);
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

                var infiniteRocketLauncher = CreateAvailableItem(ItemIds.RocketLauncherInfinite);
                infiniteRocketLauncher.UnlockChapter = rng.Next(0, 6);
                RandomizePrice(infiniteRocketLauncher, spinel: rng.NextProbability(10));
            }

            private void DistributeTreasures()
            {
                var itemRepo = ItemDefinitionRepository.Default;
                var teasureCount = rng.Next(0, 8);
                var treasures = itemRepo.KindToItemMap[ItemKinds.Treasure]
                    .Where(x => x.Id != ItemIds.VelvetBlue && x.Id != ItemIds.Spinel)
                    .Shuffle(rng)
                    .Take(teasureCount)
                    .ToArray();

                foreach (var treasure in treasures)
                {
                    var item = CreateAvailableItem(treasure);
                    item.UnlockChapter = rng.Next(0, randomizer.Campaign == Campaign.Leon ? 10 : 8);
                    RandomizePrice(item, spinel: true);
                }
            }

            private void RandomizeDiscounts()
            {
                foreach (var item in _availableItems)
                {
                    if (item.ItemDefinition.Id == ItemIds.RocketLauncher ||
                        item.ItemDefinition.Id == ItemIds.RocketLauncherInfinite ||
                        item.ItemDefinition.Id == ItemIds.RocketLauncherAda ||
                        item.ItemDefinition.Id == ItemIds.RocketLauncherInfiniteAda)
                    {
                        continue;
                    }

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

                    if (shop.Rewards._Settings.Count >= 30)
                        return;

                    var unlockChapter = item.UnlockChapter;
                    var convertedUnlockChapter = unlockChapter <= 1
                        ? 0
                        : ConvertChapterNumber(unlockChapter);

                    var reward = shop.AddReward(
                        new Item(item.ItemDefinition.Id, item.Quantity),
                        item.SpinelPrice,
                        item.UnlimitedReward,
                        convertedUnlockChapter);
                    // logger.LogLine($"Add reward {reward.RewardId} {item} Cost = {reward.SpinelCount} spinel Chapter = {reward.StartChapter}");
                }
            }

            private void SetShop()
            {
                var itemRandomizer = randomizer.ItemRandomizer;
                var itemRepo = ItemDefinitionRepository.Default;
                var shopItems = shop.Items._Datas;
                foreach (var shopItem in shopItems)
                {
                    var item = _availableItems.FirstOrDefault(x => x.ItemDefinition.Id == shopItem._ItemId);
                    if (item == null || item.BuyPrice == 0)
                    {
                        if (shopItem._PriceSettings[0]._Price._PurchasePrice != -1)
                        {
                            var itemDefinition = itemRepo.Find(shopItem._ItemId);
                            if (itemDefinition == null || itemDefinition.SupportsCampaign(randomizer.Campaign))
                            {
                                if (itemDefinition != null)
                                {
                                    item = new AvailableItem(itemDefinition);
                                    RandomizePrice(item, spinel: false);
                                    shopItem._PriceSettings[0]._Price._PurchasePrice = item.BuyPrice;
                                    shopItem._PriceSettings[0]._Price._SellingPrice = item.SellPrice;
                                }

                                shopItem._UnlockSetting._UnlockCondition = 4;
                                shopItem._UnlockSetting._UnlockFlag = Guid.Empty;
                                shopItem._UnlockSetting._UnlockTiming = ConvertChapterNumber(0);
                                shopItem._UnlockSetting._SpCondition = 1;
                            }
                        }
                    }
                    else
                    {
                        shopItem._PriceSettings[0]._Price._PurchasePrice = item.BuyPrice;
                        shopItem._PriceSettings[0]._Price._SellingPrice = item.SellPrice;
                        shopItem._UnlockSetting._UnlockCondition = 2;
                        shopItem._UnlockSetting._UnlockFlag = Guid.Empty;
                        shopItem._UnlockSetting._UnlockTiming = ConvertChapterNumber(Math.Max(0, item.UnlockChapter - 1));
                        shopItem._UnlockSetting._SpCondition = 1;
                        shopItem._StockSetting._EnableStockSetting = item.MaxStock != 0;
                        shopItem._StockSetting._EnableSelectCount = item.MaxStock != 0;
                        shopItem._StockSetting._MaxStock = item.MaxStock;
                        shopItem._StockSetting._DefaultStock = item.InitialStock;
                    }

                    // Make items unlock at first chapter work
                    if (shopItem._UnlockSetting._UnlockCondition != 4 && (shopItem._UnlockSetting._UnlockTiming <= 0 || shopItem._UnlockSetting._UnlockTiming == 17))
                    {
                        shopItem._UnlockSetting._UnlockTiming = ConvertChapterNumber(0);
                        shopItem._UnlockSetting._UnlockCondition = 0;
                        shopItem._UnlockSetting._SpCondition = 0;
                    }

                    var isAvailable = shopItem._UnlockSetting._UnlockCondition == 2 && shopItem._PriceSettings[0]._Price._PurchasePrice > 0;
                    if (item != null)
                    {
                        logger.LogLine(string.Join(
                            $"Shop item {item.ItemDefinition.Name}",
                            $"Buy = {shopItem._PriceSettings[0]._Price._PurchasePrice}",
                            $"Sell = {shopItem._PriceSettings[0]._Price._SellingPrice}",
                            $"Available = {isAvailable}",
                            $"Unlock = {shopItem._UnlockSetting._UnlockTiming}", " "));
                    }

                    // Sale change
                    if ((item?.Discount ?? 0) != 0)
                    {
                        shopItem._SaleSetting._Settings.Clear();
                        shopItem._SaleSetting._Settings.Add(new chainsaw.InGameShopItemSaleSingleSetting()
                        {
                            _SaleType = 2,
                            _StartTiming = ConvertChapterNumber(item!.DiscountStartChapter),
                            _EndTiming = ConvertChapterNumber(item.DiscountEndChapter),
                            _SaleRate = -item.Discount
                        });
                        // logger.LogLine($"    {item.Discount}% discount at chapter {item.DiscountStartChapter} to {item.DiscountEndChapter}");
                    }
                    else
                    {
                        shopItem._SaleSetting._Settings.Clear();
                    }
                }
            }

            private void SetStock()
            {
                // Clear all existing stock
                foreach (var stockAddition in shop.StockAdditions._Settings)
                {
                    stockAddition._Settings =
                    [
                        new chainsaw.InGameShopStockAdditionSingleSetting.Setting()
                        {
                            _Difficulty = 20,
                            _Datas = []
                        }
                    ];
                }

                // Add stock for each item that gets new stock per chapter
                foreach (var item in _availableItems)
                {
                    if (item.StockPerChapter == 0)
                        continue;

                    var stock = 0.0f;
                    foreach (var stockAddition in shop.StockAdditions._Settings)
                    {
                        var take = (int)stock;
                        stock -= take;
                        if (take > 0)
                        {
                            var entry = stockAddition._Settings[0];
                            entry._Datas.Add(new chainsaw.InGameShopStockAdditionSingleSetting.Data()
                            {
                                _AddItemId = item.ItemDefinition.Id,
                                _AddCount = take
                            });
                        }
                        stock += item.StockPerChapter;
                    }
                }
            }

            private void AddReward(int itemId, int count = 1, int? spinel = null, bool unlimited = false)
            {
                if (shop.Rewards._Settings.Count >= 30)
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
                logger.LogLine($"Add reward {reward._RewardId} {item} Cost = {spinel} spinel Chapter = {reward._DisplaySetting._StartTiming}");
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
                        _ when itemId == ItemIds.ExclusiveUpgradeTicket => _rewardsRng.Next(15, 40),
                        _ when itemId == ItemIds.RocketLauncherInfinite => _rewardsRng.Next(75, 100),
                        _ when itemId == ItemIds.RocketLauncher => _rewardsRng.Next(10, 20),
                        ItemKinds.Weapon => _rewardsRng.Next(4, 13),
                        ItemKinds.Attachment => _rewardsRng.Next(4, 13),
                        ItemKinds.CaseSize => _rewardsRng.Next(6, 13),
                        ItemKinds.Armor => _rewardsRng.Next(4, 16),
                        _ => _rewardsRng.Next(1, 4),
                    };
                }
                else if (itemId == ItemIds.RocketLauncherInfinite)
                {
                    item.BuyPrice = ((double)rng.Next(2_000_000, 5_000_000)).RoundPrice();
                    item.SellPrice = ((double)(item.BuyPrice * 3 / 4)).RoundPrice();
                }
                else if (itemId == ItemIds.RocketLauncher)
                {
                    item.BuyPrice = ((double)rng.Next(50_000, 250_000)).RoundPrice();
                    item.SellPrice = ((double)(item.BuyPrice * 3 / 4)).RoundPrice();
                }
                else
                {
                    var shopItem = shop.Items._Datas.FirstOrDefault(x => x._ItemId == item.ItemDefinition.Id);
                    var corePriceSetting = shopItem?._PriceSettings[0] ?? new chainsaw.gui.shop.ItemPriceSetting();
                    if (corePriceSetting._Price._PurchasePrice > 0)
                    {
                        item.BuyPrice = corePriceSetting._Price._PurchasePrice;
                        item.SellPrice = corePriceSetting._Price._SellingPrice;
                    }
                    else if (corePriceSetting._Price._SellingPrice > 0)
                    {
                        item.BuyPrice = corePriceSetting._Price._SellingPrice * 2;
                        item.SellPrice = corePriceSetting._Price._SellingPrice;
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
