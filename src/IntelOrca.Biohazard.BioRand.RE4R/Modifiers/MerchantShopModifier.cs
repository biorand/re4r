using System;
using System.Linq;

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
                .OrderBy(x => x.Key)
                .ToArray();

            foreach (var group in chapterItems)
            {
                var pushedHeading = false;
                foreach (var shopItem in group)
                {
                    if (shopItem.BuyPrice <= 0 || shopItem.UnlockCondition == 4)
                        continue;

                    var item = itemRepo.Find(shopItem.ItemId);
                    if (item == null)
                        continue;

                    if (!pushedHeading)
                    {
                        pushedHeading = true;
                        logger.Push($"Chapter {group.Key}");
                    }

                    var sellString = shopItem.SellPrice == -1 ? "" : $"Sell = {shopItem.SellPrice}";
                    logger.LogLine($"{item} Buy = {shopItem.BuyPrice} {sellString}");

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
            _shop ??= ChainsawMerchantShop.FromData(randomizer.FileRepository);
            var merchantShop = _shop;
            var itemRandomizer = randomizer.ItemRandomizer;

            if (!randomizer.GetConfigOption<bool>("random-merchant"))
                return;

            var rng = randomizer.CreateRng();
            var rewardsRng = rng.NextFork();
            var shopRng = rng.NextFork();
            var priceRng = rng.NextFork();

            var itemRepo = ItemDefinitionRepository.Default;
            var shopItems = merchantShop.ShopItems;
            var stocks = merchantShop.StockAdditions;

            var caseIds = itemRepo
                .GetAll(ItemKinds.CaseSize)
                .OrderBy(x => x.Value)
                .Select(x => x.Id)
                .ToArray();
            var caseChapters = new int[caseIds.Length];
            var caseChapter = rng.Next(0, 3);
            for (var i = 0; i < caseChapters.Length; i++)
            {
                caseChapters[i] = caseChapter;
                caseChapter += rng.Next(1, 3);
            }
            var itemToChapterMap = caseIds.Zip(caseChapters).ToDictionary(x => x.First, x => x.Second);

            // Rewards
            merchantShop.ClearRewards();

            // * Weapon attachments
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var attachment = itemRandomizer.GetRandomAttachment(rewardsRng, allowReoccurance: false);
                if (attachment != null)
                    AddReward(attachment.Id, spinel: rewardsRng.Next(2, 7));
            }

            // * Weapons
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var weapon = itemRandomizer.GetRandomWeapon(rewardsRng, allowReoccurance: false);
                if (weapon != null)
                    AddReward(weapon.Id, spinel: rewardsRng.Next(4, 13));
            }

            // * Recipes
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var recipe = itemRandomizer.GetRandomItemDefinition(rewardsRng, ItemKinds.Recipe, allowReoccurance: false);
                if (recipe != null)
                    AddReward(recipe.Id, spinel: rewardsRng.Next(4, 9));
            }

            // * Exclusive upgrades
            var ticketSpinel = rewardsRng.Next(15, 35);
            for (var i = 0; i < rewardsRng.Next(0, 3); i++)
            {
                AddReward(ItemIds.ExclusiveUpgradeTicket, spinel: ticketSpinel, unlimited: true);
                ticketSpinel += rewardsRng.Next(1, 5);
            }

            // * Case sizes
            for (var i = 0; i < rewardsRng.Next(0, 3); i++)
            {
                var randomCase = itemRandomizer.GetRandomItemDefinition(rewardsRng, ItemKinds.CaseSize, allowReoccurance: false);
                if (randomCase != null)
                {
                    var spinel = randomCase.Value / 2500;
                    AddReward(randomCase.Id, spinel: rewardsRng.Next(spinel - 3, spinel + 3));
                }
            }

            // * Health (unlimited)
            var itemIds = new[] { ItemIds.FirstAidSpray, ItemIds.HerbG, ItemIds.EggWhite };
            var healthItem = rewardsRng.Next(itemIds);
            AddReward(healthItem, spinel: rewardsRng.Next(1, 4), unlimited: true);

            // * Health (single)
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var item = itemRandomizer.GetRandomItemDefinition(rewardsRng, ItemKinds.Health);
                if (item != null)
                    AddReward(item.Id, spinel: rewardsRng.Next(1, 4));
            }

            // * Velvet blue
            AddReward(ItemIds.VelvetBlue, spinel: 1, unlimited: true);

            if (rewardsRng.NextProbability(50))
                AddReward(ItemIds.BodyArmor, spinel: rewardsRng.Next(5, 20), unlimited: true);

            // * Grenades
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.GrenadeFlash, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.GrenadeLight, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.GrenadeHeavy, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }

            // * Resources / gunpowder
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.ResourcesLarge, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.ResourcesSmall, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.Gunpowder, count: 10, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }

            // * Charms
            for (var i = 0; i < rewardsRng.Next(0, 6); i++)
            {
                var charm = itemRandomizer.GetRandomItemDefinition(rewardsRng, ItemKinds.Charm);
                if (charm != null)
                    AddReward(charm.Id, spinel: rewardsRng.Next(1, 4));
            }

            // * Teasures
            for (var i = 0; i < rewardsRng.Next(0, 8); i++)
            {
                var treasure = itemRandomizer.GetRandomItemDefinition(rewardsRng, ItemKinds.Treasure);
                if (treasure != null)
                    AddReward(treasure.Id);
            }

            // Shop
            foreach (var shopItem in shopItems)
            {
                var itemDef = itemRepo.Find(shopItem.ItemId);
                if (itemDef == null)
                    continue;

                // Availability change
                if (itemDef.Kind == ItemKinds.Ammo)
                {
                }
                else if (
                    itemDef.Kind == ItemKinds.Weapon ||
                    itemDef.Kind == ItemKinds.Attachment ||
                    itemDef.Kind == ItemKinds.Armor ||
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
                        shopItem.UnlockChapter = shopRng.Next(0, 10);
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
                    shopItem.UnlockChapter = shopRng.Next(0, 10);
                    shopItem.SpCondition = 1;
                    shopItem.EnableStockSetting = true;
                    shopItem.MaxStock = 1;
                    shopItem.DefaultStock = 1;
                }
                if (itemToChapterMap.TryGetValue(shopItem.ItemId, out var unlockChapter))
                    shopItem.UnlockChapter = unlockChapter;

                // Make items unlock at first chapter work
                if (shopItem.UnlockCondition != 4 && shopItem.UnlockChapter == 0)
                {
                    shopItem.UnlockCondition = 0;
                    shopItem.SpCondition = 0;
                }

                if (randomizer.GetConfigOption<bool>("random-merchant-prices"))
                {
                    // Price change
                    if (shopItem.BuyPrice > 0)
                    {
                        var priceMultiplier = priceRng.NextDouble(0.25, 2);
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
                if (isAvailable && shopRng.NextProbability(25))
                {
                    var startChapter = shopRng.Next(shopItem.UnlockChapter, shopItem.UnlockChapter + 3);
                    var endChapter = shopRng.Next(startChapter + 1, startChapter + 3);
                    var disount = shopRng.Next(1, 8) * 10;
                    shopItem.SetSale(merchantShop, startChapter, endChapter, -disount);
                    logger.LogLine($"    {disount}% discount at chapter {startChapter} to {endChapter}");
                }
                else
                {
                    shopItem.Sales = [];
                }
            }

            merchantShop.Save(randomizer.FileRepository);

            void AddReward(int itemId, int count = 1, int? spinel = null, bool unlimited = false)
            {
                if (merchantShop.Rewards.Length >= 30)
                    return;

                var itemDefinition = itemRepo.Find(itemId);
                if (itemDefinition == null)
                    return;

                if (spinel == null)
                {
                    var avgSpinel = itemDefinition.Value / 2500;
                    var minSpinel = Math.Max(1, avgSpinel - 2);
                    var maxSpinel = avgSpinel + 1;
                    spinel = rng.Next(minSpinel, maxSpinel + 1);
                }
                else
                {
                    spinel = Math.Max(1, spinel.Value);
                }

                var item = new Item(itemId, count);
                var startChapter = Math.Max(0, rng.Next(-3, 6));
                if (itemToChapterMap.TryGetValue(itemId, out var chapter))
                    startChapter = chapter;
                var reward = merchantShop.AddReward(new Item(itemId, count), spinel.Value, false, startChapter);
                logger.LogLine($"Add reward {reward.RewardId} {item} Cost = {spinel} spinel Chapter = {reward.StartChapter}");
            }
        }
    }
}
