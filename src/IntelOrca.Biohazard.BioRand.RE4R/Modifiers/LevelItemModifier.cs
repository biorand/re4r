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

                    var stageId = itemData.Get<int>("StageID");
                    var itemId = itemData.Get<int>("ItemID");
                    var itemCount = itemData.Get<int>("Count");
                    var ammoItemId = itemData.Get<int>("AmmoItemID");
                    var ammoCount = itemData.Get<int>("AmmoCount");
                    var item = itemRepo.Find(itemId);
                    if (item == null)
                        continue;

                    var ammoItem = itemRepo.Find(ammoItemId);
                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    logger.LogLine($"{contextId} {stageId} Item = {item} x{itemCount} Ammo = {ammoItem ?? (null)} x{ammoCount}");
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            var itemRandomizer = randomizer.ItemRandomizer;
            var chainsawItemData = ChainsawItemData.FromData(fileRepository);
            var rng = randomizer.CreateRng();

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
                    var oldItem = GetItem(instance);
                    if (oldItem == null)
                        continue;

                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    if (contextId == new ContextId(2, 0, 12, 472) ||
                        contextId == new ContextId(2, 0, 12, 405) ||
                        contextId == new ContextId(2, 0, 61, 67))
                    {
                        continue;
                    }

                    var oldItemDef = itemRepo.Find(oldItem.Value.Id);
                    if (oldItemDef == null || oldItemDef.Kind == ItemKinds.Key)
                        continue;

                    var randomItem = itemRandomizer.GetNextGeneralDrop("item-drop-ratio", rng);
                    if (randomItem is Item newItem)
                    {
                        storedItems[contextId] = newItem;
                        UpdateItem(instance, newItem);
                        logger.LogLine($"{contextId} {oldItem} becomes {newItem}");
                    }
                }
                logger.Pop();

                fileRepository.SetUserFile(path, userFile);
            }

            if (!randomizer.GetConfigOption<bool>("preserve-item-models"))
            {
                UpdateItemModels(randomizer, storedItems);
            }
        }

        private void UpdateItem(RszInstance instance, Item newItem)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var newItemDef = itemRepo.Find(newItem.Id);
            if (newItemDef == null)
                return;

            ItemDefinition? ammoDefinition = null;
            if (newItemDef.Kind == ItemKinds.Weapon)
            {
                ammoDefinition = itemRepo.GetAmmo(newItemDef);
            }

            var itemData = instance.Get<RszInstance>("ItemData");
            if (itemData == null)
                return;

            itemData.Set("ItemID", newItem.Id);
            if (ammoDefinition == null)
            {
                itemData.Set("Count", newItem.Count);
                itemData.Set("AmmoItemID", 0);
                itemData.Set("AmmoCount", 0);
            }
            else
            {
                itemData.Set("Count", 1);
                itemData.Set("AmmoItemID", ammoDefinition.Id);
                itemData.Set("AmmoCount", newItem.Count);
            }
        }

        private void UpdateItemModels(ChainsawRandomizer randomizer, Dictionary<ContextId, Item> storedItems)
        {
            var fileRepository = randomizer.FileRepository;
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

        private static Item? GetItem(RszInstance instance)
        {
            var itemData = instance.Get<RszInstance>("ItemData");
            if (itemData == null)
                return null;

            var itemId = itemData.Get<int>("ItemID");

            var itemRepo = ItemDefinitionRepository.Default;
            var itemDef = itemRepo.Find(itemId);
            if (itemDef != null)
            {
                if (itemDef.Kind == ItemKinds.Weapon)
                {
                    var ammoCount = itemData.Get<int>("AmmoCount");
                    return new Item(itemId, ammoCount);
                }
                else
                {
                    var itemCount = itemData.Get<int>("Count");
                    return new Item(itemId, itemCount);
                }
            }

            return null;
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
