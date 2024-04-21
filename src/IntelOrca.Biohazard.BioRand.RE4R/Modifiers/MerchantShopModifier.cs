using System;
using System.Collections.Generic;
using System.Linq;
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
                var chapterNumber = chapter == 0 ? 1 : chapter;
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

                    logger.LogLine($"{item} Buy = {reward.SpinelCount} spinels");
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
                    logger.LogLine($"{item} Buy = {shopItem.BuyPrice:n0} {sellString:n0}");

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
                DistributeCases();
                RandomizeRewards();
                RandomizeShop();
                shop.Save(randomizer.FileRepository);
            }

            private void DistributeWeapons()
            {
                var rng = _distRng;
                var weaponDistributor = randomizer.WeaponDistributor;
                var weapons = weaponDistributor.GetWeaponsForShop();
                foreach (var kvp in weapons)
                {
                    var chapter = kvp.Key;
                    foreach (var w in kvp.Value)
                    {
                        SetItemChapter(w.Id, chapter);
                    }
                }

                var rewards = rng.Next(0, 5);
                _rewardWeapons.AddRange(weapons
                    .SelectMany(x => x.Value)
                    .Select(x => x.Id)
                    .Shuffle(rng)
                    .Take(rewards));
            }

            private void DistributeCases()
            {
                var rng = _distRng;
                var itemRepo = ItemDefinitionRepository.Default;
                var caseIds = itemRepo
                    .GetAll(ItemKinds.CaseSize)
                    .OrderBy(x => x.Value)
                    .Select(x => x.Id)
                    .ToArray();
                var caseChapters = new int[caseIds.Length];
                var caseChapter = rng.Next(0, 3);
                for (var i = 0; i < caseChapters.Length; i++)
                {
                    var caseId = caseIds[i];
                    SetItemChapter(caseId, caseChapter);
                    caseChapter += rng.Next(1, 3);
                }
            }

            private void RandomizeRewards()
            {
                var rng = _rewardsRng;

                // Rewards
                shop.ClearRewards();

                // * Weapons / attachments
                foreach (var w in _rewardWeapons)
                {
                    AddReward(w, spinel: rng.Next(4, 13));
                }

                // * Recipes
                for (var i = 0; i < rng.Next(0, 4); i++)
                {
                    var recipe = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Recipe, allowReoccurance: false);
                    if (recipe != null)
                        AddReward(recipe.Id, spinel: rng.Next(4, 9));
                }

                // * Exclusive upgrades
                var ticketSpinel = rng.Next(15, 35);
                for (var i = 0; i < rng.Next(0, 3); i++)
                {
                    AddReward(ItemIds.ExclusiveUpgradeTicket, spinel: ticketSpinel, unlimited: true);
                    ticketSpinel += rng.Next(1, 5);
                }

                // * Case sizes
                for (var i = 0; i < rng.Next(0, 3); i++)
                {
                    var randomCase = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.CaseSize, allowReoccurance: false);
                    if (randomCase != null)
                    {
                        var spinel = randomCase.Value / 2500;
                        AddReward(randomCase.Id, spinel: rng.Next(spinel - 3, spinel + 3));
                    }
                }

                // * Health (unlimited)
                var itemIds = new[] { ItemIds.FirstAidSpray, ItemIds.HerbG, ItemIds.EggWhite };
                var healthItem = rng.Next(itemIds);
                AddReward(healthItem, spinel: rng.Next(1, 4), unlimited: true);

                // * Health (single)
                for (var i = 0; i < rng.Next(0, 4); i++)
                {
                    var item = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Health);
                    if (item != null)
                        AddReward(item.Id, spinel: rng.Next(1, 4));
                }

                // * Velvet blue
                AddReward(ItemIds.VelvetBlue, spinel: 1, unlimited: true);

                if (rng.NextProbability(50))
                    AddReward(ItemIds.BodyArmor, spinel: rng.Next(5, 20), unlimited: true);

                // * Grenades
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.GrenadeFlash, spinel: rng.Next(2, 4), unlimited: true);
                }
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.GrenadeLight, spinel: rng.Next(2, 4), unlimited: true);
                }
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.GrenadeHeavy, spinel: rng.Next(2, 4), unlimited: true);
                }

                // * Resources / gunpowder
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.ResourcesLarge, spinel: rng.Next(2, 4), unlimited: true);
                }
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.ResourcesSmall, spinel: rng.Next(2, 4), unlimited: true);
                }
                if (rng.NextProbability(30))
                {
                    AddReward(ItemIds.Gunpowder, count: 10, spinel: rng.Next(2, 4), unlimited: true);
                }

                // * Charms
                for (var i = 0; i < rng.Next(0, 6); i++)
                {
                    var charm = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Charm);
                    if (charm != null)
                        AddReward(charm.Id, spinel: rng.Next(1, 4));
                }

                // * Teasures
                for (var i = 0; i < rng.Next(0, 8); i++)
                {
                    var treasure = ItemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Treasure);
                    if (treasure != null)
                        AddReward(treasure.Id);
                }
            }

            private void RandomizeShop()
            {
                var itemRandomizer = randomizer.ItemRandomizer;
                var itemRepo = ItemDefinitionRepository.Default;
                var shopItems = shop.ShopItems;
                foreach (var shopItem in shopItems)
                {
                    var itemDef = itemRepo.Find(shopItem.ItemId);
                    if (itemDef == null)
                        continue;

                    // Availability change
                    if (itemDef.Kind == ItemKinds.Ammo)
                    {
                    }
                    else if (itemDef.Kind == ItemKinds.Weapon ||
                             itemDef.Kind == ItemKinds.Attachment)
                    {
                        if (_rewardWeapons.Contains(shopItem.ItemId))
                        {
                            shopItem.UnlockCondition = 4;
                            shopItem.UnlockFlag = Guid.Empty;
                            shopItem.UnlockChapter = 0;
                            shopItem.SpCondition = 1;
                        }
                        else
                        {
                            shopItem.UnlockCondition = 2;
                            shopItem.UnlockFlag = Guid.Empty;
                            shopItem.UnlockChapter = _shopRng.Next(0, 10);
                            shopItem.SpCondition = 1;
                        }
                    }
                    else if (itemDef.Kind == ItemKinds.Armor ||
                             itemDef.Kind == ItemKinds.CaseSize)
                    {
                        if (itemRandomizer.IsItemPlaced(shopItem.ItemId))
                        {
                            shopItem.UnlockCondition = 4;
                            shopItem.UnlockFlag = Guid.Empty;
                            shopItem.UnlockChapter = 0;
                            shopItem.SpCondition = 1;
                        }
                        else
                        {
                            shopItem.UnlockCondition = 2;
                            shopItem.UnlockFlag = Guid.Empty;
                            shopItem.UnlockChapter = _shopRng.Next(0, 10);
                            shopItem.SpCondition = 1;
                        }
                    }
                    else if (itemDef.Kind == ItemKinds.Gunpowder ||
                             itemDef.Kind == ItemKinds.Resource)
                    {
                        shopItem.UnlockCondition = 2;
                        shopItem.UnlockFlag = Guid.Empty;
                        shopItem.UnlockChapter = 0;
                        shopItem.SpCondition = 1;
                        shopItem.EnableStockSetting = true;
                        shopItem.EnableSelectCount = true;
                        shopItem.MaxStock = 30;
                        shopItem.DefaultStock = 10;
                    }
                    else if (shopItem.ItemId == ItemIds.FirstAidSpray)
                    {
                        shopItem.UnlockCondition = 2;
                        shopItem.UnlockFlag = Guid.Empty;
                        shopItem.UnlockChapter = 0;
                        shopItem.SpCondition = 1;
                        shopItem.EnableStockSetting = true;
                        shopItem.EnableSelectCount = true;
                        shopItem.MaxStock = 5;
                        shopItem.DefaultStock = 1;
                    }
                    else
                    {
                        shopItem.UnlockCondition = 2;
                        shopItem.UnlockFlag = Guid.Empty;
                        shopItem.UnlockChapter = _shopRng.Next(0, 10);
                        shopItem.SpCondition = 1;
                        shopItem.EnableStockSetting = true;
                        shopItem.MaxStock = 1;
                        shopItem.DefaultStock = 1;
                    }
                    if (itemToChapterMap.TryGetValue(shopItem.ItemId, out var unlockChapter))
                        shopItem.UnlockChapter = unlockChapter;

                    // Make items unlock at first chapter work
                    if (shopItem.UnlockCondition != 4 && shopItem.UnlockChapter <= 1)
                    {
                        shopItem.UnlockChapter = 0;
                        shopItem.UnlockCondition = 0;
                        shopItem.SpCondition = 0;
                    }

                    if (randomizer.GetConfigOption<bool>("random-merchant-prices"))
                    {
                        // Price change
                        if (shopItem.BuyPrice > 0)
                        {
                            var priceMultiplier = _priceRng.NextDouble(0.25, 2);
                            shopItem.SetPrice(priceMultiplier);
                        }
                        else
                        {
                            shopItem.UnlockChapter = 0;
                        }
                    }

                    var isAvailable = shopItem.UnlockCondition == 2 && shopItem.BuyPrice > 0;
                    logger.LogLine($"Shop item {itemDef.Name} Buy = {shopItem.BuyPrice} Sell = {shopItem.SellPrice} Available = {isAvailable} Unlock = {shopItem.UnlockChapter}");

                    // Sale change
                    if (isAvailable && _shopRng.NextProbability(25))
                    {
                        var startChapter = _shopRng.Next(shopItem.UnlockChapter, shopItem.UnlockChapter + 3);
                        var endChapter = _shopRng.Next(startChapter + 1, startChapter + 3);
                        var disount = _shopRng.Next(1, 8) * 10;
                        shopItem.SetSale(shop, startChapter, endChapter, -disount);
                        logger.LogLine($"    {disount}% discount at chapter {startChapter} to {endChapter}");
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
        }
    }
}
