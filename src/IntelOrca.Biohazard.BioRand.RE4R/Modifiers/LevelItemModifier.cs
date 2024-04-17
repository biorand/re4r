using System;
using System.Collections.Generic;
using System.IO;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class LevelItemModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            foreach (var path in _itemDataFiles)
            {
                var userFile = fileRepository.GetUserFile(path);
                if (userFile == null)
                    continue;

                var list = userFile.RSZ!.ObjectList[0].Get("Datas") as List<object>;
                if (list == null || list.Count == 0)
                    continue;

                var pushedHeader = false;
                foreach (RszInstance instance in list)
                {
                    var itemData = instance.Get<RszInstance>("ItemData");
                    if (itemData == null)
                        continue;

                    if (!pushedHeader)
                    {
                        pushedHeader = true;
                        logger.Push($"{Path.GetFileName(path)}");
                    }

                    var itemId = itemData.Get<int>("ItemID");
                    var itemCount = itemData.Get<int>("Count");
                    var ammoItemId = itemData.Get<int>("AmmoItemID");
                    var ammoCount = itemData.Get<int>("AmmoCount");
                    var item = itemRepo.Find(itemId);
                    if (item == null)
                        continue;

                    var ammoItem = itemRepo.Find(ammoItemId);
                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    logger.LogLine($"{contextId} Item = {item} x{itemCount} Ammo = {ammoItem ?? (null)} x{ammoCount}");
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var itemRandomizer = randomizer.ItemRandomizer;
            var fileRepository = randomizer.FileRepository;
            var chainsawItemData = ChainsawItemData.FromData(fileRepository);
            var itemRepo = ItemDefinitionRepository.Default;
            var rng = randomizer.CreateRng();
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
                var userFile = fileRepository.GetUserFile(path);
                if (userFile == null)
                    continue;

                var list = userFile.RSZ!.ObjectList[0].Get("Datas") as List<object>;
                if (list == null)
                    continue;

                logger.Push($"{Path.GetFileName(path)}");
                foreach (var item in list)
                {
                    var instance = (RszInstance)item;
                    var itemData = instance.Get<RszInstance>("ItemData");
                    if (itemData == null)
                        continue;

                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    if (contextId == new ContextId(2, 0, 12, 472) ||
                        contextId == new ContextId(2, 0, 12, 405))
                    {
                        continue;
                    }

                    var randomItem = RandomizeItem(randomizer, contextId, chainsawItemData, itemData, rng, logger);
                    if (randomItem != null)
                        storedItems[contextId] = (Item)randomItem;
                }
                logger.Pop();

                fileRepository.SetUserFile(path, userFile);
            }

            if (!randomizer.GetConfigOption<bool>("preserve-item-models"))
            {
                foreach (var path in _itemFiles)
                {
                    var scnFile = fileRepository.GetScnFile(path);
                    if (scnFile == null)
                        continue;

                    foreach (var go in scnFile.IterAllGameObjects(true))
                    {
                        var itemDrop = go.FindComponent("chainsaw.DropItem");
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

                    fileRepository.SetScnFile(path, scnFile);
                }
            }
        }

        private static Item? RandomizeItem(
            ChainsawRandomizer randomizer,
            ContextId contextId,
            ChainsawItemData chainsawItemData,
            RszInstance itemData,
            Rng rng,
            RandomizerLogger logger)
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
            var newItem = randomizer.ItemRandomizer.GetRandomItem(rng, randomKind, allowReoccurance: allowReoccurance);
            if (newItem == null)
                return null;

            var count = GetRandomItemQuantity(randomizer, newItem, rng);
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

            logger.LogLine($"{contextId} {item} x{itemCount} becomes {newItem}");
            return new Item(newItem.Id, count);
        }

        private static int GetRandomItemQuantity(ChainsawRandomizer randomizer, ItemDefinition def, Rng rng)
        {
            var amount = 1;
            if (def.Kind == ItemKinds.Money)
            {
                var multiplier = randomizer.GetConfigOption<double>("money-quantity");
                amount = Math.Max(1, (int)(rng.Next(100, 2000) * multiplier));
            }
            else if (def.Kind == ItemKinds.Ammo)
            {
                var multiplier = randomizer.GetConfigOption<double>("ammo-quantity");
                amount = Math.Max(1, (int)(rng.Next(10, 50) * multiplier));
            }
            else if (def.Kind == ItemKinds.Gunpowder)
            {
                amount = Math.Max(1, 10);
            }
            return amount;
        }

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
