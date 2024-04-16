using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer : IChainsawRandomizer
    {
        private FileRepository _fileRepository = new FileRepository();
        private readonly RandomizerLogger _loggerInput = new RandomizerLogger();
        private readonly RandomizerLogger _loggerProcess = new RandomizerLogger();
        private readonly RandomizerLogger _loggerOutput = new RandomizerLogger();
        private RandomizerInput _input = new RandomizerInput();
        private Rng.Table<EnemyClassDefinition>? _enemyRngTable;
        private Rng.Table<ItemDefinition?>? _itemRngTable;
        private Rng.Table<int>? _parasiteRngTable;
        private Queue<EnemyClassDefinition> _enemyClassQueue = new Queue<EnemyClassDefinition>();
        private int _uniqueHp = 1;
        private int _contextId = 5000;
        private ItemRandomizer _itemRandomizer = new ItemRandomizer(false);

        public EnemyClassFactory EnemyClassFactory { get; }

        public ChainsawRandomizer(EnemyClassFactory enemyClassFactory)
        {
            EnemyClassFactory = enemyClassFactory;
        }

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            _input = input;
            if (input.GamePath != null)
            {
                _fileRepository = new FileRepository(input.GamePath);
            }

            _itemRandomizer = new ItemRandomizer(
                allowBonusItems: GetConfigOption<bool>("allow-bonus-items"));

            // Supplement files
            var supplementZip = new ZipArchive(new MemoryStream(Resources.supplement));
            foreach (var entry in supplementZip.Entries)
            {
                if (entry.Length == 0)
                    continue;

                var data = entry.GetData();
                _fileRepository.SetGameFileData(entry.FullName, data);
            }

            var rng = new Rng(input.Seed);
            var inventoryRng = rng.NextFork();
            var merchantRng = rng.NextFork();
            var enemyRng = rng.NextFork();
            var itemRng = rng.NextFork();

            var itemData = ChainsawItemData.FromData(_fileRepository);

            StaticChanges();
            if (GetConfigOption<bool>("random-inventory"))
            {
                RandomizeInventory(itemData, inventoryRng);
            }
            else
            {
                _itemRandomizer.MarkItemPlaced(ItemIds.SG09R);
                _itemRandomizer.MarkItemPlaced(ItemIds.CombatKnife);
            }
            if (GetConfigOption<bool>("random-merchant"))
            {
                RandomizeMerchantShop(merchantRng);
            }
            if (GetConfigOption<bool>("random-items"))
            {
                RandomizeItems(itemData, itemRng);
            }

            var areaRepo = AreaDefinitionRepository.Default;
            var areas = new List<Area>();
            foreach (var areaDef in areaRepo.Areas)
            {
                var areaData = _fileRepository.GetGameFileData(areaDef.Path);
                if (areaData == null)
                    continue;

                var area = new Area(areaDef, EnemyClassFactory, areaData);
                areas.Add(area);
            }

            DisableFirstAreaInhibitor(areas);
            FixDeadEnemyCounters(areas);
            if (GetConfigOption<bool>("random-enemies"))
            {
                RandomizeEnemies(enemyRng, areas);
            }

            var logFiles = new LogFiles(_loggerInput.Output, _loggerProcess.Output, _loggerOutput.Output);
            return new RandomizerOutput(input, _fileRepository.GetOutputPakFile(), logFiles);
        }

        private void StaticChanges()
        {
            var path = "natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_502_p000.scn.20";
            var data = _fileRepository.GetGameFileData(path);
            if (data == null)
                return;

            var scn = ChainsawRandomizerFactory.Default.ReadScnFile(data);
            scn.RemoveGameObject(new Guid("ca0ac85f-1238-49d9-a0fb-0d58a42487a1"));
            scn.RemoveGameObject(new Guid("4a975fc1-2e1c-4fd3-a49a-1f35d6a30f0f"));
            _fileRepository.SetGameFileData(path, scn.ToByteArray());
        }

        private void RandomizeInventory(ChainsawItemData itemData, Rng rng)
        {
            _loggerProcess.LogHeader("Randomizing inventory");

            var inventory = ChainsawPlayerInventory.FromData(_fileRepository);
            inventory.PTAS = rng.Next(0, 200) * 100;
            inventory.SpinelCount = rng.Next(0, 5);
            inventory.ClearItems();

            // Weapons
            var primaryWeaponKind = GetConfigOption("inventory-weapon-primary", "handgun")!;
            var secondaryWeaponKind = GetConfigOption("inventory-weapon-secondary", "random")!;
            var knifeWeapon = _itemRandomizer.GetRandomWeapon(rng, ItemClasses.Knife, allowReoccurance: false);
            var primaryWeapon = _itemRandomizer.GetRandomWeapon(rng, primaryWeaponKind, allowReoccurance: false);
            var secondaryWeapon = _itemRandomizer.GetRandomWeapon(rng, secondaryWeaponKind, allowReoccurance: false);
            if (knifeWeapon != null)
            {
                inventory.AddItem(new Item(knifeWeapon.Id));
            }
            if (primaryWeapon != null)
            {
                inventory.AddItem(new Item(primaryWeapon.Id));
                var ammo = _itemRandomizer.GetRandomItem(rng, ItemKinds.Ammo, primaryWeapon.Class);
                if (ammo != null)
                    inventory.AddItem(new Item(ammo.Id));
            }
            if (secondaryWeapon != null)
            {
                inventory.AddItem(new Item(secondaryWeapon.Id));
                var ammo = _itemRandomizer.GetRandomItem(rng, ItemKinds.Ammo, secondaryWeapon.Class);
                if (ammo != null)
                    inventory.AddItem(new Item(ammo.Id));
            }

            // Other stuff
            var randomKinds = new[] {
                ItemKinds.Fish,
                ItemKinds.Health,
                ItemKinds.Egg,
                ItemKinds.Grenade,
                ItemKinds.Knife,
                ItemKinds.Gunpowder,
                ItemKinds.Resource };

            var kinds = new List<string>();
            for (var i = 0; i < 10; i++)
            {
                kinds.Add(rng.Next(randomKinds));
            }

            foreach (var kind in kinds)
            {
                var randomItem = _itemRandomizer.GetRandomItem(rng, kind);
                if (randomItem != null)
                    inventory.AddItem(new Item(randomItem.Id, -1));
            }

            inventory.UpdateWeapons(itemData);
            inventory.AutoSort(itemData);
            inventory.Save(_fileRepository);

            foreach (var item in inventory.Data[0].InventoryItems)
            {
                _loggerProcess.LogLine($"Add item {item.Item} {item.Item.CurrentItemCount}");
            }
        }

        private void RandomizeMerchantShop(Rng rng)
        {
            var rewardsRng = rng.NextFork();
            var shopRng = rng.NextFork();
            var priceRng = rng.NextFork();

            _loggerProcess.LogHeader("Randomizing merchant");

            var itemRepo = ItemDefinitionRepository.Default;
            var merchantShop = ChainsawMerchantShop.FromData(_fileRepository);
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
                var attachment = _itemRandomizer.GetRandomAttachment(rewardsRng, allowReoccurance: false);
                if (attachment != null)
                    AddReward(attachment.Id, spinel: rewardsRng.Next(2, 7));
            }

            // * Weapons
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var weapon = _itemRandomizer.GetRandomWeapon(rewardsRng, allowReoccurance: false);
                if (weapon != null)
                    AddReward(weapon.Id, spinel: rewardsRng.Next(4, 13));
            }

            // * Recipes
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var recipe = _itemRandomizer.GetRandomItem(rewardsRng, ItemKinds.Recipe, allowReoccurance: false);
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
                var randomCase = _itemRandomizer.GetRandomItem(rewardsRng, ItemKinds.CaseSize, allowReoccurance: false);
                if (randomCase != null)
                {
                    var spinel = randomCase.Value / 2500;
                    AddReward(randomCase.Id, spinel: rewardsRng.Next(spinel - 3, spinel + 3));
                }
            }

            // * Health (unlimited)
            var itemIds = new[] { ItemIds.FirstAidSpray, ItemIds.GreenHerb, ItemIds.ChickenEgg };
            var healthItem = rewardsRng.Next(itemIds);
            AddReward(healthItem, spinel: rewardsRng.Next(1, 4), unlimited: true);

            // * Health (single)
            for (var i = 0; i < rewardsRng.Next(0, 4); i++)
            {
                var item = _itemRandomizer.GetRandomItem(rewardsRng, ItemKinds.Health);
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
                AddReward(ItemIds.FlashGrenade, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.HandGrenade, spinel: rewardsRng.Next(2, 4), unlimited: true);
            }
            if (rewardsRng.NextProbability(30))
            {
                AddReward(ItemIds.HeavyGrenade, spinel: rewardsRng.Next(2, 4), unlimited: true);
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
                var charm = _itemRandomizer.GetRandomItem(rewardsRng, ItemKinds.Charm);
                if (charm != null)
                    AddReward(charm.Id, spinel: rewardsRng.Next(1, 4));
            }

            // * Teasures
            for (var i = 0; i < rewardsRng.Next(0, 8); i++)
            {
                var treasure = _itemRandomizer.GetRandomItem(rewardsRng, ItemKinds.Treasure);
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
                    if (_itemRandomizer.IsItemPlaced(shopItem.ItemId))
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

                if (GetConfigOption<bool>("random-merchant-prices"))
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
                _loggerProcess.LogLine($"Shop item {itemDef.Name} Buy = {shopItem.BuyPrice} Sell = {shopItem.SellPrice} Available = {isAvailable} Unlock = {shopItem.UnlockChapter}");

                // Sale change
                if (isAvailable && shopRng.NextProbability(25))
                {
                    var startChapter = shopRng.Next(shopItem.UnlockChapter, 12);
                    var endChapter = shopRng.Next(startChapter + 1, startChapter + 3);
                    var disount = shopRng.Next(1, 8) * 10;
                    shopItem.SetSale(merchantShop, startChapter, endChapter, -disount);
                    _loggerProcess.LogLine($"    {disount}% discount at chapter {startChapter} to {endChapter}");
                }
            }

            merchantShop.Save(_fileRepository);
            LogShop(merchantShop);

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
                _loggerProcess.LogLine($"Add reward {reward.RewardId} {item} Cost = {spinel} spinel Chapter = {reward.StartChapter}");
            }
        }

        private void LogShop(ChainsawMerchantShop shop)
        {
            var itemRepo = ItemDefinitionRepository.Default;

            _loggerOutput.LogHeader("Merchant");
            var orderedShopItems = shop.ShopItems.OrderBy(x => x.UnlockChapter).ToArray();
            var chapter = -1;
            foreach (var shopItem in orderedShopItems)
            {
                if (shopItem.UnlockChapter != chapter)
                {
                    chapter = shopItem.UnlockChapter;
                    _loggerOutput.LogHr();
                    _loggerOutput.LogLine($"Chapter {chapter + 1}:");
                    _loggerOutput.LogHr();
                }

                if (shopItem.BuyPrice <= 0 || shopItem.UnlockCondition == 4)
                    continue;

                var item = itemRepo.Find(shopItem.ItemId);
                if (item == null)
                    continue;

                var sellString = shopItem.SellPrice == -1 ? "" : $"Sell = {shopItem.SellPrice}";
                _loggerOutput.LogLine($"{item} Buy = {shopItem.BuyPrice} {sellString}");
                foreach (var sale in shopItem.Sales)
                {
                    _loggerOutput.LogLine($"    {-sale.SaleRate}% discount between chapter {sale.StartTiming + 1} and {sale.EndTiming + 1}");
                }
            }
        }

        private void DisableFirstAreaInhibitor(List<Area> areas)
        {
            var firstArea = areas.FirstOrDefault(x => x.FileName == "level_cp10_chp1_1_010.scn.20");
            if (firstArea == null)
                return;

            var scnFile = firstArea.ScnFile;
            var inhibitor = scnFile.FindGameObject(new Guid("9fc712ca-478c-45b5-be12-5233edf4fe95"));
            if (inhibitor == null)
                return;

            var inhibitorComponent = inhibitor.Components[1];
            for (var i = 0; i < 5; i++)
            {
                inhibitorComponent.Set(
                    $"_Datas[{i}].Rule[0]._Enable.Matters[0]._Data.Flags._CheckFlags[0]._CheckFlag",
                    new Guid("0fb10e00-5384-4732-881a-af1fae2036c7"));
            }
        }

        private void FixDeadEnemyCounters(List<Area> areas)
        {
            foreach (var area in areas)
            {
                var scnFile = area.ScnFile;
                foreach (var go in scnFile.IterAllGameObjects(true))
                {
                    var component = go.Components.FirstOrDefault(x => x.Name.StartsWith("chainsaw.DeadEnemyCounter"));
                    if (component != null)
                    {
                        var targetIds = component.GetFieldValue("_CountTargetIDs") as List<object>;
                        if (targetIds != null)
                        {
                            targetIds.Clear();
                            foreach (var id in _characterKindIds)
                            {
                                targetIds.Add(id);
                            }
                        }
                    }
                }
            }
        }

        private void RandomizeEnemies(Rng rng, List<Area> areas)
        {
#if DEBUG
            LogAllEnemies(areas, e => e.Kind.Key == "armadura");
#endif
            LogAreas(_loggerInput, areas);
            foreach (var area in areas)
            {
                RandomizeArea(area, rng);
            };
            Parallel.ForEach(areas, area =>
            {
                _fileRepository.SetGameFileData(area.Definition.Path, area.SaveData());
            });
            LogAreas(_loggerOutput, areas);
        }

        private void LogAllEnemies(List<Area> areas, Predicate<Enemy> predicate)
        {
            var all = new List<Dictionary<string, object?>>();
            foreach (var area in areas)
            {
                foreach (var enemy in area.Enemies)
                {
                    if (!predicate(enemy)) continue;

                    var enemyDict = GetRszDictionary(enemy.MainComponent);
                    enemyDict["area"] = area.FileName;
                    enemyDict["guid"] = enemy.Guid;
                    all.Add(enemyDict);
                }
            }
            var jsonOutput = JsonSerializer.Serialize(all, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            jsonOutput.WriteToFile(@"C:\Users\Ted\.biorand\logs\enemy_attributes.json");
        }

        private Dictionary<string, object?> GetRszDictionary(RszInstance instance)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var field in instance.Fields)
            {
                var value = instance.GetFieldValue(field.name);
                if (value is RszInstance child)
                {
                    value = GetRszDictionary(child);
                }
                dict[field.name] = value;
            }
            return dict;
        }

        private void LogAreas(RandomizerLogger logger, List<Area> areas)
        {
            logger.LogHr();
            logger.LogVersion();
            logger.LogHr();
            foreach (var area in areas)
            {
                logger.LogArea(area);
                foreach (var enemy in area.Enemies)
                {
                    logger.LogEnemy(enemy);
                }
            }
        }

        private T? GetConfigOption<T>(string key, T? defaultValue = default)
        {
            if (_input.Configuration != null && _input.Configuration.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        private void RandomizeArea(Area area, Rng rng)
        {
            var healthRng = rng.NextFork();
            var dropRng = rng.NextFork();
            var parasiteRng = rng.NextFork();

            var oldEnemies = area.Enemies
                .Where(x => !x.Kind.Closed)
                .ToArray();
            var def = area.Definition;

            var oldEnemiesSummary = area.Enemies.Select(GetEnemySummary).ToArray();
            var multiplier = GetConfigOption<double>("enemy-multiplier", 1);
            var newEnemyCount = oldEnemies.Length * multiplier;
            var delta = (int)Math.Round(newEnemyCount - oldEnemies.Length);
            if (delta != 0)
            {
                var bag = new EndlessBag<Enemy>(rng, oldEnemies);
                var enemiesToCopy = bag.Next(delta);
                foreach (var e in enemiesToCopy)
                {
                    area.Duplicate(e, GetNextContextId());
                }
            }

            var enemies = area.Enemies;
            var numEnemies = enemies.Length;
            var enemyClasses = new HashSet<EnemyClassDefinition>();
            foreach (var enemy in enemies)
            {
                var e = enemy;
                if (e.Kind.Closed)
                    continue;

                var excludedClasses = GetExcludedClasses(area, e);
                if (excludedClasses == null)
                    continue;

                var ecd = GetRandomEnemyClass(enemyClasses, rng, excludedClasses);
                if (ecd == null)
                    continue;

                e = area.ConvertTo(e, ecd.Kind.ComponentName);

                // Reset various fields
                e.SetFieldValue("_RandamizeMontageID", false);
                e.SetFieldValue("_RandomMontageID", 0);
                e.SetFieldValue("_MontageID", 0);
                e.SetFieldValue("_FixedVoiceID", 0);
                e.ParasiteKind = 0;
                e.ForceParasiteAppearance = false;
                e.ParasiteAppearanceProbability = 0;

                if (ecd.Weapon.Length == 0)
                {
                    e.Weapon = 0;
                    e.SecondaryWeapon = 0;
                }
                else
                {
                    var weaponChoice = rng.Next(ecd.Weapon);
                    e.Weapon = weaponChoice.Primary?.Id ?? 0;
                    e.SecondaryWeapon = weaponChoice.Secondary?.Id ?? 0;
                }

                foreach (var fd in ecd.Fields)
                {
                    var fieldValue = rng.Next(fd.Values);
                    e.SetFieldValue(fd.Name, fieldValue);
                }

                RandomizeHealth(e, ecd, healthRng);
                RandomizeDrop(e, ecd, dropRng);

                // If there are a lot of enemies, plaga seems to randomly crash the game
                // E.g. village, 360 zealots, 25 plaga will crash
                if (ecd.Plaga && numEnemies < 100)
                {
                    RandomizeParasite(e, parasiteRng);
                }
            }
        }

        private void RandomizeItems(ChainsawItemData chainsawItemData, Rng rng)
        {
            _loggerProcess.LogHeader("Randomizing Items");

            var itemRepo = ItemDefinitionRepository.Default;
            var randomKinds = new[] {
                ItemKinds.Ammo,
                ItemKinds.Fish,
                ItemKinds.Health,
                ItemKinds.Egg,
                ItemKinds.Treasure,
                ItemKinds.Attachment,
                ItemKinds.Gunpowder,
                ItemKinds.Resource,
                ItemKinds.Weapon,
                ItemKinds.Knife,
                ItemKinds.Token,
                ItemKinds.Money,
                ItemKinds.Armor,
                ItemKinds.Map,
                ItemKinds.CaseSize,
                ItemKinds.CasePerk,
                ItemKinds.Recipe,
                ItemKinds.Charm,
                ItemKinds.Grenade,
            };

            var storedItems = new Dictionary<ContextId, Item>();
            foreach (var path in _itemDataFiles)
            {
                var userFile = _fileRepository.GetUserFile(path);
                if (userFile == null)
                    continue;

                _loggerProcess.LogHr();
                _loggerProcess.LogLine($"{Path.GetFileName(path)}");
                _loggerProcess.LogHr();

                var list = userFile.RSZ!.ObjectList[0].Get("Datas") as List<object>;
                if (list == null)
                    continue;

                foreach (var item in list)
                {
                    var instance = (RszInstance)item;
                    var itemData = instance.Get<RszInstance>("ItemData");
                    if (itemData == null)
                        continue;

                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    var randomItem = RandomizeItem(chainsawItemData, itemData, rng);
                    if (randomItem != null)
                        storedItems[contextId] = (Item)randomItem;
                }
                _fileRepository.SetUserFile(path, userFile);
            }

            if (!GetConfigOption<bool>("preserve-item-models"))
            {
                foreach (var path in _itemFiles)
                {
                    var scnFile = _fileRepository.GetScnFile(path);
                    if (scnFile == null)
                        continue;

                    foreach (var go in scnFile.IterAllGameObjects(true))
                    {
                        var itemDrop = go.Components.FirstOrDefault(x => x.Name.StartsWith("chainsaw.DropItem"));
                        if (itemDrop == null)
                            continue;

                        var itemData = itemDrop.Get<RszInstance>("_ItemData");
                        if (itemData == null)
                            continue;

                        var contextId = ContextId.FromRsz(itemDrop.Get<RszInstance>("_ID")!);
                        if (storedItems.TryGetValue(contextId, out var randomItem))
                        {
                            itemData.Set("ItemID", randomItem.Id);
                            itemData.Set("Count", randomItem.Count);
                            itemData.Set("AmmoItemID", 0);
                            itemData.Set("AmmoCount", 0);
                        }
                    }

                    _fileRepository.SetScnFile(path, scnFile);
                }
            }
        }

        private Item? RandomizeItem(ChainsawItemData chainsawItemData, RszInstance itemData, Rng rng)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var randomKinds = new[] {
                ItemKinds.Ammo,
                ItemKinds.Fish,
                ItemKinds.Health,
                ItemKinds.Egg,
                ItemKinds.Treasure,
                ItemKinds.Attachment,
                ItemKinds.Gunpowder,
                ItemKinds.Resource,
                ItemKinds.Weapon,
                ItemKinds.Knife,
                ItemKinds.Token,
                ItemKinds.Money,
                ItemKinds.CaseSize,
                ItemKinds.CasePerk,
                ItemKinds.Charm,
                ItemKinds.Grenade,
            };

            var itemId = itemData.Get<int>("ItemID");
            var itemCount = itemData.Get<int>("Count");
            var ammoItemId = itemData.Get<int>("AmmoItemID");
            var ammoCount = itemData.Get<int>("AmmoCount");
            var item = itemRepo.Find(itemId);
            if (item == null)
                return null;

            if (item.Kind == ItemKinds.Key)
                return null;

            var randomKind = rng.Next(randomKinds);
            var allowReoccurance =
                randomKind != ItemKinds.Weapon &&
                randomKind != ItemKinds.Attachment &&
                randomKind != ItemKinds.CasePerk &&
                randomKind != ItemKinds.CaseSize &&
                randomKind != ItemKinds.Charm;
            var newItem = _itemRandomizer.GetRandomItem(rng, randomKind);
            if (newItem == null)
                return null;

            var count = GetRandomItemQuantity(newItem, rng);
            itemData.Set("ItemID", newItem.Id);
            itemData.Set("Count", count);
            itemData.Set("AmmoItemID", 0);
            itemData.Set("AmmoCount", 0);
            if (newItem.Kind == ItemKinds.Weapon)
            {
                var definitionAmmo = itemRepo.GetAmmo(newItem);
                if (definitionAmmo != null)
                {
                    itemData.Set("AmmoItemID", definitionAmmo.Id);
                    itemData.Set("AmmoCount", chainsawItemData.GetMaxAmmo(newItem.Id));
                }
            }

            _loggerProcess.LogLine($"{item} x{itemCount} becomes {newItem}");
            return new Item(newItem.Id, count);
        }

        private static string[]? GetExcludedClasses(Area area, Enemy e)
        {
            var restrictions = area.Definition.Restrictions;
            if (restrictions == null)
                return [];

            var restrictionBlock = restrictions.FirstOrDefault(x => x.Guids == null || x.Guids.Contains(e.Guid));
            var excludedClasses = restrictionBlock?.Exclude;
            if (restrictionBlock != null && excludedClasses == null)
                return null;

            return excludedClasses ?? [];
        }

        private int GetNextContextId()
        {
            return _contextId++;
        }

        private void RandomizeHealth(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var debugUniqueHp = GetConfigOption<bool>("debug-unique-enemy-hp");
            if (debugUniqueHp)
            {
                enemy.Health = _uniqueHp++;
            }
            else
            {
                var randomHealth = GetConfigOption<bool>("enemy-custom-health");
                if (randomHealth)
                {
                    var minHealth = GetConfigOption<int>($"enemy-health-min-{ecd.Key}");
                    var maxHealth = GetConfigOption<int>($"enemy-health-max-{ecd.Key}");
                    minHealth = Math.Clamp(minHealth, 1, 100000);
                    maxHealth = Math.Clamp(maxHealth, minHealth, 100000);
                    enemy.Health = rng.Next(minHealth, maxHealth + 1);
                }
                else
                {
                    enemy.Health = null;
                }
            }
        }

        private void RandomizeParasite(Enemy enemy, Rng rng)
        {
            if (_parasiteRngTable == null)
            {
                var table = rng.CreateProbabilityTable<int>();
                table.Add(0, GetConfigOption<double>("parasite-ratio-none"));
                table.Add(1, GetConfigOption<double>("parasite-ratio-a"));
                table.Add(2, GetConfigOption<double>("parasite-ratio-b"));
                _parasiteRngTable = table;
            }
            if (enemy.ParasiteKind != null)
            {
                var kind = 0;
                if (!_parasiteRngTable.IsEmpty)
                    kind = _parasiteRngTable.Next();
                if (kind == 0)
                {
                    enemy.ParasiteKind = 0;
                    enemy.ForceParasiteAppearance = false;
                    enemy.ParasiteAppearanceProbability = 0;
                }
                else
                {
                    enemy.ParasiteKind = kind;
                    enemy.ForceParasiteAppearance = true;
                    enemy.ParasiteAppearanceProbability = 100;
                }
            }
        }

        private void RandomizeDrop(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var repo = ItemDefinitionRepository.Default;
            if (enemy.ItemDrop is Item drop && !drop.IsAutomatic)
            {
                var currentItemId = drop.Id;
                var itemDef = repo.Find(currentItemId);
                if (itemDef?.Kind == null || itemDef.Kind == ItemKinds.Key)
                {
                    // Don't change the drop for this enemy
                    return;
                }
            }

            if (ecd.Class < 6)
                enemy.ItemDrop = GetRandomValuableItem(enemy, ecd, rng);
            else
                enemy.ItemDrop = GetRandomItem(enemy, rng);
        }

        private void LogEnemyTable(Area area, string[] oldEnemies)
        {
            var newEnemies = area.Enemies.Select(GetEnemySummary).ToArray();
            var enemyCount = Math.Max(oldEnemies.Length, newEnemies.Length);
            if (enemyCount != 0)
            {
                var lhColumn = oldEnemies.Max(x => x.Length);
                var rhColumn = newEnemies.Max(x => x.Length);
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine($"Area: {area.FileName}");
                Console.WriteLine($"------------------------------------------------------");
                for (var i = 0; i < enemyCount; i++)
                {
                    var oldE = i < oldEnemies.Length ? oldEnemies[i] : "";
                    var newE = i < newEnemies.Length ? newEnemies[i] : "";
                    Console.WriteLine($"| {oldE.PadRight(lhColumn)} | {newE.PadRight(rhColumn)} |");
                }
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine();
            }
        }

        private static string GetEnemySummary(Enemy enemy)
        {
            return $"{enemy.Kind} ({enemy.Health})";
        }

        private EnemyClassDefinition? GetRandomEnemyClass(HashSet<EnemyClassDefinition> enemyClasses, Rng rng, string[] excludedClasses)
        {
            if (_enemyRngTable == null)
            {
                var table = rng.CreateProbabilityTable<EnemyClassDefinition>();
                foreach (var enemyClass in EnemyClassFactory.Classes)
                {
                    var ratio = GetConfigOption<double>($"enemy-ratio-{enemyClass.Key}");
                    if (ratio != 0)
                    {
                        table.Add(enemyClass, ratio);
                    }
                }
                _enemyRngTable = table;
            }

            if (excludedClasses.Length != 0)
            {
                var pool = _enemyRngTable.Values
                    .Where(x => !excludedClasses.Contains(x.Key))
                    .ToArray();
                if (pool.Length == 0)
                    return null;
                return rng.Next(pool);
            }

            if (_enemyClassQueue.Count == 0)
            {
                EnemyClassDefinition ecd;
                var variety = GetConfigOption<int>($"enemy-variety");
                if (variety != 0 && enemyClasses.Count >= variety)
                {
                    ecd = enemyClasses.Shuffle(rng).First();
                }
                else
                {
                    ecd = _enemyRngTable.Next();
                    enemyClasses.Add(ecd);
                }

                var packCount = GetPackCount(ecd, rng);
                for (var i = 0; i < packCount; i++)
                {
                    _enemyClassQueue.Enqueue(ecd);
                }
            }
            return _enemyClassQueue.Dequeue();
        }

        private int GetPackCount(EnemyClassDefinition ecd, Rng rng)
        {
            return rng.Next(1, ecd.Class);
        }

        private Item? GetRandomItem(Enemy enemy, Rng rng)
        {
            if (_itemRngTable == null)
            {
                var table = rng.CreateProbabilityTable<ItemDefinition?>();

                var repo = ItemDefinitionRepository.Default;
                var kindRatios = new List<(string, double)>();
                foreach (var itemKind in repo.Kinds)
                {
                    var ratio = GetConfigOption<double>($"drop-ratio-{itemKind}");
                    if (ratio != 0)
                    {
                        kindRatios.Add((itemKind, ratio));
                    }
                }

                var total = kindRatios.Select(x => x.Item2).Sum();

                var autoRatio = GetConfigOption<double>("drop-ratio-automatic");
                if (autoRatio != 0)
                {
                    total += autoRatio;
                    table.Add(ItemDefinition.Automatic, autoRatio / total);
                }

                var noneRatio = GetConfigOption<double>("drop-ratio-none");
                if (noneRatio != 0)
                {
                    total += noneRatio;
                    table.Add(null, noneRatio / total);
                }

                foreach (var (kind, ratio) in kindRatios)
                {
                    var itemsForThisKind = repo.KindToItemMap[kind];
                    var p = (ratio / total) / itemsForThisKind.Length;
                    foreach (var itemDef in itemsForThisKind)
                    {
                        if (string.IsNullOrEmpty(itemDef.Mode))
                        {
                            table.Add(itemDef, p);
                        }
                    }
                }

                _itemRngTable = table;
            }

            var def = _itemRngTable.Next();
            if (def == null)
            {
                return null;
            }
            else
            {
                var amount = GetRandomItemQuantity(def, rng);
                var item = new Item(def.Id, amount);
                return item;
            }
        }

        private int GetRandomItemQuantity(ItemDefinition def, Rng rng)
        {
            var amount = 1;
            if (def.Kind == ItemKinds.Money)
            {
                var multiplier = GetConfigOption<double>("money-quantity");
                amount = Math.Max(1, (int)(rng.Next(100, 2000) * multiplier));
            }
            else if (def.Kind == ItemKinds.Ammo)
            {
                var multiplier = GetConfigOption<double>("ammo-quantity");
                amount = Math.Max(1, (int)(rng.Next(10, 50) * multiplier));
            }
            else if (def.Kind == ItemKinds.Gunpowder)
            {
                amount = Math.Max(1, 10);
            }
            return amount;
        }

        private Item? GetRandomValuableItem(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var kinds = itemRepo.Kinds
                .Where(x => GetConfigOption<bool>($"drop-valuable-{x}"))
                .ToArray();

            if (kinds.Length == 0)
                return GetRandomItem(enemy, rng);

            var kind = rng.Next(kinds);
            var itemPool = itemRepo.KindToItemMap[kind];

            var minValue = (10 - ecd.Class) * 800;
            var maxValue = minValue * 3;
            if (kind == ItemKinds.Money)
            {
                var amount = rng.Next(minValue, maxValue + 1);
                return new Item(itemPool[0].Id, amount);
            }
            else
            {
                var filteredItems = itemPool
                    .Where(x => string.IsNullOrEmpty(x.Mode))
                    .Where(x => x.Value >= minValue && x.Value <= maxValue)
                    .ToImmutableArray();

                if (filteredItems.Length == 0)
                    filteredItems = itemPool;

                var chosenItem = rng.Next(filteredItems);
                return new Item(chosenItem.Id, 1);
            }
        }

        private static readonly int[] _characterKindIds = new int[]
        {
            100000,
            110000,
            199999,
            200000,
            200001,
            200002,
            200003,
            200004,
            200005,
            200006,
            200007,
            200008,
            200009,
            200010,
            200011,
            200012,
            200013,
            200014,
            200015,
            200016,
            200017,
            200018,
            200019,
            200020,
            200021,
            200022,
            200023,
            200024,
            200025,
            200026,
            200027,
            200028,
            200029,
            200030,
            200031,
            200032,
            200033,
            200034,
            200035,
            200036,
            200037,
            200038,
            200039,
            200040,
            200041,
            200042,
            200043,
            200044,
            200045,
            200046,
            200047,
            380000,
            600000,
            600001,
            600002,
            600003,
            600004,
            600005,
            80000,
            81000,
            81100,
            81101,
            81102,
            81103,
            81104,
            81105,
            81106,
            81107,
            81108,
            81109,
            500000,
        };

        private static readonly string[] _itemFiles = new string[]
        {
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_0/item_cp10_chp1_0.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/item_cp10_chp1_1.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_2/item_cp10_chp1_2.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_1/item_cp10_chp2_1.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_2/item_cp10_chp2_2.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc40/item_loc40.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc43/item_loc43.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc44/item_loc44.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc45/item_loc45.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc46/item_loc46.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc47/item_loc47_001.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc47/item_loc47.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_000.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_001.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_002.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_003.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_004.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_005.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_007.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_000.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_002.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_003.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_004.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_006.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_000.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_002.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_003.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_004.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_005.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc53/item_loc53.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_000.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_001.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_002.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_003.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_004.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_005.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc55/item_loc55.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56_000.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56_001.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc58/item_loc58.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc59/item_loc59.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc60/item_loc60.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc61/item_loc61.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc62/item_loc62.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc63/item_loc63.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc64/item_loc64.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc65/item_loc65.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc66/item_loc66.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc67/item_loc67.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc68/item_loc68.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc69/item_loc69.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc70/item_loc70.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_200.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_201.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_300.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_400.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_401.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_402.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_403.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_404.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_405.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_406.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_500.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc73/item_loc73_900.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc78/item_loc78.scn.20",
        };

        private static readonly string[] _itemDataFiles = new[]
        {
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_0/item_cp10_chp1_0_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/item_cp10_chp1_1_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_2/item_cp10_chp1_2_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_1/item_cp10_chp2_1_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp2_2/item_cp10_chp2_2_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc40/item_loc40_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc41/item_loc41_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc42/item_loc42_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc43/item_loc43_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc44/item_loc44_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc45/item_loc45_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc46/item_loc46_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc47/item_loc47_001_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc47/item_loc47_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_000_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_001_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_002_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_003_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_004_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_005_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc50/item_loc50_007_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_000_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_002_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_003_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_004_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc51/item_loc51_006_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_000_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_002_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_003_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_004_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc52/item_loc52_005_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc53/item_loc53_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_000_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_001_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_002_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_003_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_004_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc54/item_loc54_005_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc55/item_loc55_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56_000_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56_001_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc56/item_loc56_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc59/item_loc59_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc60/item_loc60_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc61/item_loc61_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc62/item_loc62_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc63/item_loc63_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc64/item_loc64_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc65/item_loc65_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc66/item_loc66_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc67/item_loc67_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc68/item_loc68_itemdata.user.2",
            "natives/stm/_chainsaw/leveldesign/location/loc69/item_loc69_itemdata.user.2",
        };
    }
}
