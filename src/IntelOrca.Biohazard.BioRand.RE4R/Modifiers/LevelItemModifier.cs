using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class LevelItemModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            foreach (var area in AreaDefinitionRepository.Default.Items)
            {
                if (area.DataPath == null)
                    continue;

                var userFile = fileRepository.GetUserFile(area.DataPath);
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
                        logger.Push($"{Path.GetFileName(area.DataPath)}");
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

            var fileRepository = randomizer.FileRepository;
            var rng = randomizer.CreateRng();

            var levelItems = GetAllItems(fileRepository);
            RandomizeItems(randomizer, levelItems, rng, logger);
            UpdateItemData(randomizer, levelItems);
            if (!randomizer.GetConfigOption<bool>("preserve-item-models"))
            {
                UpdateItemModels(randomizer, levelItems);
            }
        }

        private ImmutableArray<LevelItem> GetAllItems(FileRepository fileRepository)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var levelItems = ImmutableArray.CreateBuilder<LevelItem>();
            foreach (var area in AreaDefinitionRepository.Default.Items)
            {
                if (area.DataPath == null)
                    continue;

                var userFile = fileRepository.GetUserFile(area.DataPath);
                if (userFile == null)
                    continue;

                var list = userFile.RSZ!.ObjectList[0].Get("Datas") as List<object>;
                if (list == null)
                    continue;

                foreach (var item in list)
                {
                    var instance = (RszInstance)item;
                    var oldItem = GetItem(instance);
                    if (oldItem == null)
                        continue;

                    var oldItemDef = itemRepo.Find(oldItem.Value.Id);
                    if (oldItemDef == null)
                        continue;

                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    var itemInfo = area.Items?.FirstOrDefault(x => x.CtxId == contextId);
                    var levelItem = new LevelItem(area.Chapter, contextId, oldItemDef, oldItem.Value)
                    {
                        Include = itemInfo?.Include,
                        Exclude = itemInfo?.Exclude,
                        IsDlc = instance.Get<bool>("ItemStatic.IsDLC")
                    };
                    levelItems.Add(levelItem);
                }
            }
            return levelItems.ToImmutable();
        }

        private void RandomizeItems(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems, Rng rng, RandomizerLogger logger)
        {
            var randomItemSettings = new RandomItemSettings
            {
                ItemRatioKeyFunc = (dropKind) => randomizer.GetConfigOption<double>($"item-drop-ratio-{dropKind}"),
                MinAmmoQuantity = randomizer.GetConfigOption("item-drop-ammo-min", 0.1),
                MaxAmmoQuantity = randomizer.GetConfigOption("item-drop-ammo-max", 1.0),
                MinMoneyQuantity = randomizer.GetConfigOption("item-drop-money-min", 100),
                MaxMoneyQuantity = randomizer.GetConfigOption("item-drop-money-max", 1000),
            };
            var ammoOnlyAvailableWeapons = randomizer.GetConfigOption("item-drop-ammo-only-available-weapons", true);

            logger.Push($"Randomizing items");

            var itemRandomizer = randomizer.ItemRandomizer;
            var valuableDistributor = randomizer.ValuableDistributor;
            foreach (var kvp in levelItems.GroupBy(x => x.Chapter).OrderBy(x => x.Key))
            {
                var chapter = kvp.Key;
                var chapterItems = kvp
                    .Where(x => x.CanChange)
                    .ToHashSet();

                logger.Push($"Chapter {chapter}");

                // Valuables
                var valuableItems = chapterItems
                    .Where(x => x.Exclude == null && !x.IsDlc)
                    .Shuffle(rng)
                    .ToQueue();
                var valuables = valuableDistributor.GetItems(chapter, ItemDiscovery.Item);
                foreach (var valuable in valuables)
                {
                    if (!valuableItems.TryDequeue(out var levelItem))
                        continue;

                    chapterItems.Remove(levelItem);
                    levelItem.NewItem = new Item(valuable.Definition.Id, 1);
                    LogItemChange(levelItem, logger);
                }

                // Treasure
                var treasureRatio = randomizer.GetConfigOption<double>("item-treasure-drop-ratio", 0.1);
                var treasureCount = (int)(chapterItems.Count * treasureRatio);
                logger.Push("Treasure");
                for (var i = 0; i < treasureCount; i++)
                {
                    if (!valuableItems.TryDequeue(out var levelItem))
                        continue;

                    chapterItems.Remove(levelItem);
                    levelItem.NewItem = randomizer.ItemRandomizer.GetRandomTreasure(rng);
                    LogItemChange(levelItem, logger);
                }
                logger.Pop();

                // General items
                var generalItems = chapterItems.Shuffle(rng).ToQueue();
                while (generalItems.TryDequeue(out var levelItem))
                {
                    if (levelItem.Include?.Length == 0)
                        continue;

                    if (ammoOnlyAvailableWeapons)
                    {
                        randomItemSettings.ValidateDropKind = (drop) =>
                        {
                            var ammoType = DropKinds.GetAmmoType(drop);
                            return ammoType == null || randomizer.ValuableDistributor.IsAmmoAvailableYet(ammoType.Value, levelItem.Chapter);
                        };
                    }
                    var randomItem = itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
                    if (randomItem is Item newItem)
                    {
                        levelItem.NewItem = newItem;
                        LogItemChange(levelItem, logger);
                    }
                }
                logger.Pop();
            }

            logger.Pop();
        }

        private static void LogItemChange(LevelItem levelItem, RandomizerLogger logger)
        {
            logger.LogLine($"{levelItem.ContextId} {levelItem.OriginalItem} becomes {levelItem.NewItem}");
        }

        private void UpdateItemData(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems)
        {
            var map = levelItems.ToDictionary(x => x.ContextId);
            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            foreach (var area in AreaDefinitionRepository.Default.Items)
            {
                var userFile = fileRepository.GetUserFile(area.DataPath);
                if (userFile == null)
                    continue;

                var list = userFile.RSZ!.ObjectList[0].Get("Datas") as List<object>;
                if (list == null)
                    continue;

                foreach (var item in list)
                {
                    var instance = (RszInstance)item;
                    var oldItem = GetItem(instance);
                    if (oldItem == null)
                        continue;

                    var contextId = ContextId.FromRsz(instance.Get<RszInstance>("ID")!);
                    if (map.TryGetValue(contextId, out var levelItem))
                    {
                        if (levelItem.NewItem is Item newItem)
                        {
                            UpdateItem(instance, newItem);
                        }
                    }
                }

                fileRepository.SetUserFile(area.DataPath, userFile);
            }
        }

        private void UpdateItemModels(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems)
        {
            var map = levelItems.ToDictionary(x => x.ContextId);
            var fileRepository = randomizer.FileRepository;
            foreach (var area in AreaDefinitionRepository.Default.Items)
            {
                if (area.Path == null)
                    continue;

                var scnFile = fileRepository.GetScnFile(area.Path);
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
                    if (map.TryGetValue(contextId, out var levelItem))
                    {
                        if (levelItem.NewItem is Item newItem)
                        {
                            itemData.Set("ItemID", newItem.Id);
                            itemData.Set("Count", newItem.Count);
                            itemData.Set("AmmoItemID", 0);
                            itemData.Set("AmmoCount", 0);
                        }
                    }
                }

                fileRepository.SetScnFile(area.Path, scnFile);
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

        private class LevelItem(int chapter, ContextId contextId, ItemDefinition originalItemDefinition, Item originalItem)
        {
            public int Chapter => chapter;
            public ContextId ContextId => contextId;
            public ItemDefinition OriginalDefinition => originalItemDefinition;
            public Item OriginalItem => originalItem;

            public string[]? Include { get; set; }
            public string[]? Exclude { get; set; }
            public bool IsDlc { get; set; }
            public Item? NewItem { get; set; }

            public bool IsKey => OriginalDefinition.Kind == ItemKinds.Key;
            public bool CanChange => !IsKey && Include?.Length != 0;
        }
    }
}
