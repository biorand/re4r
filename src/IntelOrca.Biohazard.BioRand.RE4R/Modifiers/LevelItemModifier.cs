using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class ItemPlacement
    {
        public Campaign Campaign { get; set; }
        public int Chapter { get; set; }
        public Guid Guid { get; set; }
        public string Description { get; set; } = "";
        public int Stage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
        public string Container { get; set; } = "";
        public string Tags { get; set; } = "";
        public string Include { get; set; } = "";
        public string Exclude { get; set; } = "";
    }

    internal class LevelItemModifier : Modifier
    {
        private static readonly List<ItemPlacement> _items = [];
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            var areaRepository = AreaDefinitionRepository.GetRepository(randomizer.Campaign);

            if (logger.Name == "input")
            {
                foreach (var area in areaRepository.Items)
                {
                    if (area.DataPath == null)
                        continue;

                    var dropItemSaveDataTable = fileRepository.DeserializeUserFile<DropItemSaveDataTable>(area.DataPath);
                    if (dropItemSaveDataTable == null)
                        continue;

                    var scene = fileRepository.GetScnFile(area.Path).ReadScene(fileRepository.TypeRepository);
                    scene.VisitGameObjects(gameObject =>
                    {
                        var dropItem = gameObject.FindComponent("chainsaw.DropItem");
                        if (dropItem == null)
                            return;

                        if (gameObject.Guid == new Guid("d6876afa-1b5b-4444-a674-c4fa3b420db4"))
                        {
                        }

                        var transform = new Transform(gameObject);
                        var contextId = dropItem.Get<ContextID>("_ID");
                        var ctxId = ContextId.FromRszValue(contextId);
                        var itemData = dropItem.Get<chainsaw.DropItemContext.SaveData>("_ItemData");
                        var itemStatic = dropItem.Get<chainsaw.DropItemContext.StaticData>("_ItemStatic");

                        var count = itemData.Count;
                        var itemDef = itemRepo.Find(itemData.ItemID);
                        if (itemDef?.Kind == ItemKinds.Weapon)
                        {
                            count = itemData.AmmoCount;
                        }

                        var ourSpec = area.Items?.FirstOrDefault(x => x.CtxId == ctxId);

                        var tags = new List<string>();
                        if (itemStatic.IsDLC)
                        {
                            tags.Add("dlc");
                        }
                        if (area.Path.Contains("chapter"))
                        {
                            tags.Add("chapteronly");
                        }

                        // var data = dropItemSaveDataTable.Datas.FirstOrDefault(x => x.ID == contextId);

                        _items.Add(new ItemPlacement()
                        {
                            Campaign = randomizer.Campaign,
                            Chapter = ourSpec?.Chapter ?? area.Chapter,
                            Guid = gameObject.Guid,
                            Container = ourSpec?.Valuable ?? "",
                            Description = $"{itemDef?.Name} x{count}",
                            Stage = itemData.StageID,
                            Tags = string.Join(" ", tags),
                            Include = string.Join(" ", ourSpec?.Include ?? []),
                            Exclude = string.Join(" ", ourSpec?.Exclude ?? []),
                        });
                    });
                }
                if (randomizer.Campaign == Campaign.Ada)
                {
                    var sb = new StringBuilder();
                    foreach (var item in _items)
                    {
                        sb.AppendLine(string.Join(",", new object[] {
                            item.Campaign,
                            item.Chapter,
                            item.Guid,
                            item.Description,
                            item.Container,
                            item.Stage,
                            item.X,
                            item.Y,
                            item.Z,
                            item.Yaw,
                            item.Pitch,
                            item.Roll,
                            item.Tags,
                            item.Include,
                            item.Exclude
                        }));
                    }
                    var s = sb.ToString();
                    { }
                }
            }

            foreach (var area in areaRepository.Items)
            {
                if (area.DataPath == null)
                    continue;

                var dropItemSaveDataTable = fileRepository.DeserializeUserFile<DropItemSaveDataTable>(area.DataPath);
                if (dropItemSaveDataTable == null)
                    continue;

                var pushedHeader = false;
                foreach (var data in dropItemSaveDataTable.Datas)
                {
                    var itemData = data.ItemData;
                    if (itemData == null)
                        continue;

                    if (!pushedHeader)
                    {
                        pushedHeader = true;
                        logger.Push($"{Path.GetFileName(area.DataPath)}");
                    }

                    var stageId = itemData.StageID;
                    var itemId = itemData.ItemID;
                    var itemCount = itemData.Count;
                    var ammoItemId = itemData.AmmoItemID;
                    var ammoCount = itemData.AmmoCount;
                    var item = itemRepo.Find(itemId);
                    if (item == null)
                        continue;

                    var ammoItem = itemRepo.Find(ammoItemId);
                    var contextId = ContextId.FromRszValue(data.ID);
                    logger.LogLine($"{contextId} {stageId} Item = {item} x{itemCount} Ammo = {ammoItem ?? (null)} x{ammoCount}");
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var rng = randomizer.CreateRng();

            var levelItems = GetAllItems(randomizer);
            RandomizeItems(randomizer, levelItems, rng, logger);
            UpdateItemData(randomizer, levelItems);
            if (!randomizer.GetConfigOption<bool>("preserve-item-models"))
            {
                UpdateItemModels(randomizer, levelItems);
            }
        }

        private ImmutableArray<LevelItem> GetAllItems(ChainsawRandomizer randomizer)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var levelItems = ImmutableArray.CreateBuilder<LevelItem>();
            var fileRepository = randomizer.FileRepository;
            var areaRepository = AreaDefinitionRepository.GetRepository(randomizer.Campaign);
            foreach (var area in areaRepository.Items)
            {
                if (area.DataPath == null)
                    continue;

                var dropItemSaveDataTable = fileRepository.DeserializeUserFile<DropItemSaveDataTable>(area.DataPath);
                if (dropItemSaveDataTable == null)
                    continue;

                foreach (var item in dropItemSaveDataTable.Datas)
                {
                    var oldItem = GetItem(item);
                    if (oldItem == null)
                        continue;

                    var oldItemDef = itemRepo.Find(oldItem.Value.Id);
                    if (oldItemDef == null)
                        continue;

                    var contextId = ContextId.FromRszValue(item.ID);
                    var itemInfo = area.Items?.FirstOrDefault(x => x.CtxId == contextId);
                    var levelItem = new LevelItem(itemInfo?.Chapter ?? area.Chapter, contextId, oldItemDef, oldItem.Value)
                    {
                        Include = itemInfo?.Include,
                        Exclude = itemInfo?.Exclude,
                        IsDlc = item.ItemStatic.IsDLC,
                        Valuable = itemInfo?.Valuable
                    };
                    levelItems.Add(levelItem);
                }
            }
            return levelItems.ToImmutable();
        }

        private static void RandomizeItems(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems, Rng rng, RandomizerLogger logger)
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
                    .ToList();

                var valuables = valuableDistributor.GetItems(chapter, ItemDiscovery.Item);
                logger.Push("Valuables");
                foreach (var valuable in valuables)
                {
                    var levelItem = TakeRandomHighValueItem(valuableItems, rng);
                    if (levelItem == null)
                        break;

                    chapterItems.Remove(levelItem);
                    levelItem.NewItem = new Item(valuable.Definition.Id, 1);
                    LogItemChange(levelItem, logger);
                }
                logger.Pop();

                // Treasure
                var treasureRatio = randomizer.GetConfigOption("item-treasure-drop-ratio", 0.1);
                var treasureCount = (int)(chapterItems.Count * treasureRatio);
                logger.Push("Treasure");
                for (var i = 0; i < treasureCount; i++)
                {
                    var levelItem = TakeRandomHighValueItem(valuableItems, rng);
                    if (levelItem == null)
                        break;

                    chapterItems.Remove(levelItem);
                    levelItem.NewItem = randomizer.ItemRandomizer.GetRandomTreasure(rng);
                    LogItemChange(levelItem, logger);
                }
                logger.Pop();

                // General items
                logger.Push("General");
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
                logger.Pop();
            }

            logger.Pop();
        }

        private static LevelItem? TakeRandomHighValueItem(List<LevelItem> items, Rng rng)
        {
            if (items.Count == 0)
                return null;

            var index = -1;
            var valuableOrder = new[] { "boss", "multikey", "bawk", "ashley", "smallkey", "long", "key", "display", "chest" };
            foreach (var v in valuableOrder)
            {
                if (v == "boss" || rng.NextProbability(75))
                {
                    index = items.FindIndex(x => x.Valuable == v);
                    if (index != -1)
                        break;
                }
            }

            if (index == -1)
                index = rng.Next(0, items.Count);

            var result = items[index];
            items.RemoveAt(index);
            return result;
        }

        private static void LogItemChange(LevelItem levelItem, RandomizerLogger logger)
        {
            logger.LogLine($"{levelItem.ContextId} {levelItem.OriginalItem} becomes {levelItem.NewItem}");
        }

        private static void UpdateItemData(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems)
        {
            var map = levelItems.ToDictionary(x => x.ContextId);
            var itemRepo = ItemDefinitionRepository.Default;
            var fileRepository = randomizer.FileRepository;
            var areaRepository = AreaDefinitionRepository.GetRepository(randomizer.Campaign);
            foreach (var area in areaRepository.Items)
            {
                var dropItemSaveDataTable = fileRepository.DeserializeUserFile<DropItemSaveDataTable>(area.DataPath);
                foreach (var item in dropItemSaveDataTable.Datas)
                {
                    var oldItem = GetItem(item);
                    if (oldItem == null)
                        continue;

                    var contextId = ContextId.FromRszValue(item.ID);
                    if (map.TryGetValue(contextId, out var levelItem))
                    {
                        if (levelItem.NewItem is Item newItem)
                        {
                            UpdateItem(item, newItem);
                        }
                    }
                }
                fileRepository.SerializeUserFile(area.DataPath, dropItemSaveDataTable);
            }
        }

        private void UpdateItemModels(ChainsawRandomizer randomizer, ImmutableArray<LevelItem> levelItems)
        {
            var map = levelItems.ToDictionary(x => x.ContextId);
            var fileRepository = randomizer.FileRepository;
            var areaRepository = AreaDefinitionRepository.GetRepository(randomizer.Campaign);
            foreach (var area in areaRepository.Items)
            {
                if (area.Path == null)
                    continue;

                fileRepository.ModifyScnFile(area.Path, scene =>
                {
                    return scene.VisitGameObjects(go =>
                    {
                        var itemDrop = go.FindComponent("chainsaw.DropItem");
                        if (itemDrop != null)
                        {
                            var contextId = ContextId.FromRsz(itemDrop["_ID"]);
                            if (map.TryGetValue(contextId, out var levelItem) && levelItem.NewItem is Item newItem)
                            {
                                go = go.AddOrUpdateComponent(itemDrop
                                    .Set("_ItemData.ItemID", newItem.Id)
                                    .Set("_ItemData.Count", newItem.Count)
                                    .Set("_ItemData.AmmoItemID", 0)
                                    .Set("_ItemData.AmmoCount", 0));
                            }
                        }
                        return go;
                    });
                });
            }
        }

        private static void UpdateItem(DropItemSaveDataTable.Data data, Item newItem)
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

            var itemData = data.ItemData;
            if (itemData == null)
                return;

            itemData.ItemID = newItem.Id;
            if (ammoDefinition == null)
            {
                itemData.Count = newItem.Count;
                itemData.AmmoItemID = 0;
                itemData.AmmoCount = 0;
            }
            else
            {
                itemData.Count = 1;
                itemData.AmmoItemID = ammoDefinition.Id;
                itemData.AmmoCount = newItem.Count;
            }
        }

        private static Item? GetItem(DropItemSaveDataTable.Data data)
        {
            var itemData = data.ItemData;
            if (itemData == null)
                return null;

            var itemId = itemData.ItemID;

            var itemRepo = ItemDefinitionRepository.Default;
            var itemDef = itemRepo.Find(itemId);
            if (itemDef != null)
            {
                if (itemDef.Kind == ItemKinds.Weapon)
                {
                    var ammoCount = itemData.AmmoCount;
                    return new Item(itemId, ammoCount);
                }
                else
                {
                    var itemCount = itemData.Count;
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
            public string? Valuable { get; set; }
            public Item? NewItem { get; set; }

            public bool IsKey => OriginalDefinition.Kind == ItemKinds.Key;
            public bool CanChange => !IsKey && Include?.Length != 0;

            public override string ToString() => $"Chapter {chapter} {ContextId} {NewItem ?? OriginalItem}";
        }
    }
}
