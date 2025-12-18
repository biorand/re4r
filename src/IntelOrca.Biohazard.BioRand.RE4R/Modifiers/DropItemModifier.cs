using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class DropItemModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var areaService = randomizer.AreaService;
            foreach (var area in areaService.Areas)
            {
                var items = area.Items.ToArray();
                if (items.Length == 0)
                    continue;

                logger.Push($"{area.FileName}");
                foreach (var go in items)
                {
                    var dropItem = go.FindComponent("chainsaw.DropItem");
                    if (dropItem == null)
                        continue;

                    var stage = dropItem.Get<int>("_ItemData.StageID");
                    var itemId = dropItem.Get<int>("_ItemData.ItemID");
                    var itemCount = dropItem.Get<int>("_ItemData.Count");
                    // dropItem.Get<int>("_ItemData.AmmoItemID");
                    // dropItem.Get<int>("_ItemData.AmmoCount");

                    var contextId = dropItem.Get<chainsaw.ContextID>("_ID");
                    var position = new Transform(go).Position;
                    var item = new Item(itemId, itemCount);
                    logger.LogLine(
                        go.Guid,
                        item,
                        stage,
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

            var areaService = randomizer.AreaService;
            var itemService = randomizer.ItemService;

            // Get context IDs for each item
            foreach (var item in areaService.Areas)
            {
                foreach (var gameObject in item.Items)
                {
                    var placement = itemService.FromGuid(gameObject.Guid);
                    if (placement != null)
                    {
                        var itemDrop = gameObject.FindComponent("chainsaw.DropItem")!;
                        var itemData = itemDrop.Get<chainsaw.DropItemContext.SaveData>("_ItemData");
                        placement.OldItem = new Item(itemData.ItemID, itemData.Count);
                        placement.ContextId = itemDrop.Get<chainsaw.ContextID>("_ID");
                    }
                }
            }

            var itemsToRemove = itemService.ItemPlacements
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => x.Tags.Contains(ItemTags.Remove))
                .ToArray();

            var itemsToChange = itemService.ItemPlacements
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => !x.Tags.Contains(ItemTags.Remove))
                .Where(x => CanChangeItem(randomizer, x))
                .ToArray();

            var result = Randomize(randomizer, itemsToChange, logger);

            foreach (var area in areaService.Areas)
            {
                if (!preserveModels)
                {
                    UpdateModels(area, result);
                }
                area.ItemSaveData.Update(result);
            }

            foreach (var item in itemsToRemove)
            {
                var area = areaService.FindAreaContainingGameObject(item.Guid);
                if (area == null)
                    continue;

                area.Scene = area.Scene.RemoveGameObject(item.Guid);
                area.ItemSaveData.Remove(item.ContextId);
            }
        }

        private void UpdateModels(Area area, Dictionary<chainsaw.ContextID, Item> placements)
        {
            area.Scene = area.Scene.VisitGameObjects(go =>
            {
                var itemDrop = go.FindComponent("chainsaw.DropItem");
                if (itemDrop != null)
                {
                    var contextId = itemDrop.Get<chainsaw.ContextID>("_ID");
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

        private Dictionary<chainsaw.ContextID, Item> Randomize(ChainsawRandomizer randomizer, IEnumerable<ItemPlacement> placements, RandomizerLogger logger)
        {
            var result = new Dictionary<chainsaw.ContextID, Item>();
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
                var rng = randomizer.GetRng("modifier/dropitem");
                var valuableItems = chapterItems
                    .Where(x => x.Exclude.IsDefaultOrEmpty && !x.Tags.Contains(ItemTags.Dlc))
                    .Shuffle(rng)
                    .ToList();

                var valuables = valuableDistributor.GetItems(chapter, ItemDiscovery.Item);
                logger.Push("Valuables");
                foreach (var valuable in valuables)
                {
                    var placement = TakeRandomHighValueItem(valuableItems, rng);
                    if (placement == null)
                        break;

                    chapterItems.Remove(placement);
                    var newItem = new Item(valuable.Definition.Id, 1);
                    result[placement.ContextId] = newItem;
                    LogItemChange(placement, newItem);
                }
                logger.Pop();

                // Treasure
                var treasureRatio = randomizer.GetConfigOption("item-treasure-drop-ratio", 0.1);
                var treasureCount = (int)(chapterItems.Count * treasureRatio);
                logger.Push("Treasure");
                for (var i = 0; i < treasureCount; i++)
                {
                    var placement = TakeRandomHighValueItem(valuableItems, rng);
                    if (placement == null)
                        break;

                    chapterItems.Remove(placement);
                    var newItem = randomizer.ItemRandomizer.GetRandomTreasure(rng);
                    result[placement.ContextId] = newItem;
                    LogItemChange(placement, newItem);
                }
                logger.Pop();

                // General items
                logger.Push("General");
                var generalItems = chapterItems.Shuffle(rng).ToQueue();
                while (generalItems.TryDequeue(out var placement))
                {
                    if (ammoOnlyAvailableWeapons)
                    {
                        randomItemSettings.ValidateDropKind = (drop) =>
                        {
                            var ammoType = DropKinds.GetAmmoType(drop);
                            return ammoType == null || randomizer.ValuableDistributor.IsAmmoAvailableYet(ammoType.Value, placement.Chapter);
                        };
                    }
                    var randomItem = itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
                    if (randomItem is Item newItem)
                    {
                        result[placement.ContextId] = newItem;
                        LogItemChange(placement, newItem);
                    }
                }
                logger.Pop();
                logger.Pop();
            }

            logger.Pop();
            return result;

            void LogItemChange(ItemPlacement placement, Item item)
            {
                logger.LogLine($"{placement.GuidOrAuto} becomes {item}");
            }
        }

        private static bool CanChangeItem(ChainsawRandomizer randomizer, ItemPlacement placement)
        {
            var oldItemDefinition = ItemDefinitionRepository.Default.Find(placement.OldItem.Id);
            if (oldItemDefinition != null && oldItemDefinition.Kind == ItemKinds.Key)
            {
                if (!placement.Tags.Contains(ItemTags.ChangeKey))
                {
                    return false;
                }
            }

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

        private class ItemPlacementThing
        {
            public required ItemPlacement Placement { get; init; }
            public required ImmutableArray<RszGameObject> GameObjects { get; init; }
            public required ImmutableArray<chainsaw.DropItemSaveDataTable.Data> Data { get; init; }
        }
    }
}
