using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class DropItemModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var filePairs = GetFilePairs(randomizer);
            foreach (var fp in filePairs)
            {
                logger.Push($"{Path.GetFileName(fp.ScenePath)}");

                var datas = fp.GetItemDatas().ToDictionary(x => ContextId.FromRszValue(x.ID));
                foreach (var go in fp.GetItemGameObjects())
                {
                    var dropItem = go.FindComponent("chainsaw.DropItem");
                    if (dropItem == null)
                        continue;

                    var contextId = ContextId.FromRsz(dropItem.Get<RszObjectNode>("_ID"));
                    if (!datas.TryGetValue(contextId, out var data))
                        continue;

                    var position = new Transform(go).Position;
                    var item = new Item(data.ItemData.ItemID, data.ItemData.Count);
                    logger.LogLine(
                        go.Guid,
                        item,
                        data.ItemData.StageID,
                        position.X.ToString("0.0"),
                        position.Y.ToString("0.0"),
                        position.Z.ToString("0.0"),
                        contextId);
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var preserveModels = randomizer.GetConfigOption<bool>("preserve-item-models");
            var rng = randomizer.CreateRng();

            var filePairs = GetFilePairs(randomizer);

            var itemsCsv = randomizer.DynamicData.GetData(DynamicDataName.Items) ?? throw new Exception("Unable to find item data.");
            var items = Csv.Deserialize<ItemPlacement>(itemsCsv)
                .Where(x => x.Chapter != 0)
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => CanChangeItem(randomizer, x))
                .ToDictionary(x => x.Guid);

            // Get context IDs for each item
            foreach (var fp in filePairs)
            {
                foreach (var gameObject in fp.GetItemGameObjects())
                {
                    if (items.TryGetValue(gameObject.Guid, out var placement))
                    {
                        var itemDrop = gameObject.FindComponent("chainsaw.DropItem")!;
                        var itemData = itemDrop.Get<chainsaw.DropItemContext.SaveData>("_ItemData");
                        placement.OldItem = new Item(itemData.ItemID, itemData.Count);
                        placement.ContextId = ContextId.FromRsz(itemDrop.Get<RszObjectNode>("_ID"));
                    }
                }
            }

            var result = Randomize(randomizer, items.Values, rng, logger);
            foreach (var fp in filePairs)
            {
                if (!preserveModels)
                {
                    fp.UpdateModels(result);
                }
                fp.UpdateData(result);
                fp.Save();
            }
        }

        private Dictionary<ContextId, Item> Randomize(ChainsawRandomizer randomizer, IEnumerable<ItemPlacement> placements, Rng rng, RandomizerLogger logger)
        {
            var result = new Dictionary<ContextId, Item>();
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
            foreach (var kvp in placements.GroupBy(x => x.Chapter).OrderBy(x => x.Key))
            {
                var chapter = kvp.Key;
                var chapterItems = kvp.ToHashSet();

                logger.Push($"Chapter {chapter}");

                // Valuables
                var valuableItems = chapterItems
                    .Where(x => x.Exclude.IsDefaultOrEmpty && !x.Tags.Contains(ItemTags.Dlc))
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
                    var newItem = new Item(valuable.Definition.Id, 1);
                    result[levelItem.ContextId] = newItem;
                    LogItemChange(levelItem, newItem);
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
                    var newItem = randomizer.ItemRandomizer.GetRandomTreasure(rng);
                    result[levelItem.ContextId] = newItem;
                    LogItemChange(levelItem, newItem);
                }
                logger.Pop();

                // General items
                logger.Push("General");
                var generalItems = chapterItems.Shuffle(rng).ToQueue();
                while (generalItems.TryDequeue(out var levelItem))
                {
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
                        result[levelItem.ContextId] = newItem;
                        LogItemChange(levelItem, newItem);
                    }
                }
                logger.Pop();
                logger.Pop();
            }

            logger.Pop();
            return result;

            void LogItemChange(ItemPlacement placement, Item item)
            {
                logger.LogLine($"{placement.Guid} becomes {item}");
            }
        }

        private ImmutableArray<ItemFilePair> GetFilePairs(ChainsawRandomizer randomizer)
        {
            var areaRepository = AreaDefinitionRepository.GetRepository(randomizer.Campaign);
            return areaRepository.Items
                .Select(x => new ItemFilePair(randomizer, x.Path, x.DataPath))
                .ToImmutableArray();
        }

        private static bool CanChangeItem(ChainsawRandomizer randomizer, ItemPlacement placement)
        {
            var oldItemDefinition = ItemDefinitionRepository.Default.Find(placement.OldItem.Id);
            if (oldItemDefinition != null && oldItemDefinition.Kind == ItemKinds.Key)
                return false;

            if (placement.Tags.Contains(ItemTags.Preserve))
                return false;

            return true;
        }

        private static ItemPlacement? TakeRandomHighValueItem(List<ItemPlacement> items, Rng rng)
        {
            if (items.Count == 0)
                return null;

            var index = -1;
            var valuableOrder = new[] { "boss", "multikey", "bawk", "ashley", "smallkey", "long", "key", "display", "chest" };
            foreach (var v in valuableOrder)
            {
                if (v == "boss" || rng.NextProbability(75))
                {
                    index = items.FindIndex(x => x.Container == v);
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

        private class ItemFilePair
        {
            private readonly ChainsawRandomizer _randomizer;
            private readonly ScnFile.Builder _scn;

            public string ScenePath { get; }
            public string UserdataPath { get; }

            public RszScene Scene
            {
                get => _scn.Scene;
                set => _scn.Scene = value;
            }

            public chainsaw.DropItemSaveDataTable Userdata { get; private set; }

            public ItemFilePair(ChainsawRandomizer randomizer, string scenePath, string userdataPath)
            {
                _randomizer = randomizer;
                _scn = randomizer.FileRepository.GetScnFile(scenePath).ToBuilder(_randomizer.FileRepository.TypeRepository);

                ScenePath = scenePath;
                UserdataPath = userdataPath;
                Userdata = randomizer.FileRepository.DeserializeUserFile<chainsaw.DropItemSaveDataTable>(userdataPath);
            }

            public void Save()
            {
                _randomizer.FileRepository.SetScnFile(ScenePath, _scn.Build());
                _randomizer.FileRepository.SerializeUserFile(UserdataPath, Userdata);
            }

            public IEnumerable<RszGameObject> GetItemGameObjects()
            {
                var result = new List<RszGameObject>();
                Scene.VisitGameObjects(gameObject =>
                {
                    var dropItem = gameObject.FindComponent("chainsaw.DropItem");
                    if (dropItem != null)
                    {
                        result.Add(gameObject);
                    }
                });
                return result;
            }

            public IEnumerable<chainsaw.DropItemSaveDataTable.Data> GetItemDatas()
            {
                return Userdata.Datas;
            }

            public void UpdateModels(Dictionary<ContextId, Item> placements)
            {
                Scene = Scene.VisitGameObjects(go =>
                {
                    var itemDrop = go.FindComponent("chainsaw.DropItem");
                    if (itemDrop != null)
                    {
                        var contextId = ContextId.FromRsz(itemDrop["_ID"]);
                        if (placements.TryGetValue(contextId, out var item))
                        {
                            go = go.AddOrUpdateComponent(itemDrop
                                .Set("_ItemData.ItemID", item.Id)
                                .Set("_ItemData.Count", item.Count)
                                .Set("_ItemData.AmmoItemID", 0)
                                .Set("_ItemData.AmmoCount", item.Count));
                        }
                    }
                    return go;
                });
            }

            public void UpdateData(Dictionary<ContextId, Item> placements)
            {
                foreach (var data in Userdata.Datas)
                {
                    if (placements.TryGetValue(ContextId.FromRszValue(data.ID), out var item))
                    {
                        UpdateItem(data, item);
                    }
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
        }

        private class ItemPlacementThing
        {
            public required ItemPlacement Placement { get; init; }
            public required ItemFilePair FilePair { get; init; }
            public required ImmutableArray<RszGameObject> GameObjects { get; init; }
            public required ImmutableArray<chainsaw.DropItemSaveDataTable.Data> Data { get; init; }
        }
    }

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
        public ImmutableArray<string> Tags { get; set; } = [];
        public ImmutableArray<string> Include { get; set; } = [];
        public ImmutableArray<string> Exclude { get; set; } = [];

        public Item OldItem { get; set; }
        public ContextId ContextId { get; set; }
    }

    public static class ItemTags
    {
        public const string Preserve = "preserve";
        public const string Dlc = "dlc";
    }
}
