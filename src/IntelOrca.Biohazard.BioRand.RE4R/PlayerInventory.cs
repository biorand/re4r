using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class PlayerInventory
    {
        private const string InventoryCatalogPath = "natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/inventorycatalog_main.user.2";

        private readonly UserFile _inventoryCatalog;

        private PlayerInventory(UserFile inventoryCatalog)
        {
            _inventoryCatalog = inventoryCatalog;
        }

        public static PlayerInventory FromData(FileRepository fileRepository)
        {
            var inventoryCatalog = GetUserFile(fileRepository, InventoryCatalogPath);
            return new PlayerInventory(inventoryCatalog);
        }

        private static UserFile GetUserFile(FileRepository fileRepository, string path)
        {
            var data = fileRepository.GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : ChainsawRandomizerFactory.Default.ReadUserFile(data);
        }

        public void Save(FileRepository fileRepository)
        {
            fileRepository.SetGameFileData(InventoryCatalogPath, _inventoryCatalog.ToByteArray());
        }

        public void SetItems(params ItemDefinition[] items)
        {
            Data[0].InventoryItems = items.Select(x => CreateInventoryItem(x)).ToArray();
            AutoSort();
        }

        public void AddItem(RE4R.Item item)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var itemDef = itemRepo.Find(item.Id);
            var inventoryItem = CreateInventoryItem(itemDef!);
            inventoryItem.SlotIndexColumn = 7;

            var items = Data[0].InventoryItems.ToList();
            items.Add(inventoryItem);
            Data[0].InventoryItems = items.ToArray();
        }

        private void AutoSort()
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var column = 0;
            foreach (var item in Data[0].InventoryItems)
            {
                var itemDef = itemRepo.Find(item.Item.ItemId);
                if (itemDef != null)
                {
                    item.SlotIndexColumn = column;
                    column += itemDef.Width;
                }
            }
        }

        private InventoryItem CreateInventoryItem(ItemDefinition definition, int count = 1)
        {
            var rsz = _inventoryCatalog.RSZ!;
            var item = new Item(rsz.CreateInstance("chainsaw.Item"));
            item.Id = Guid.NewGuid();
            item.ItemId = definition.Id;
            item.CurrentDurability = 1000;
            item.CurrentItemCount = count;

            var inventoryItem = new InventoryItem(rsz.CreateInstance("chainsaw.InventoryItemSaveData"));
            inventoryItem.Item = item;
            return inventoryItem;
        }

        public int PTAS
        {
            get
            {
                var root = _inventoryCatalog.RSZ!.ObjectList[0];
                return root.Get<int>("_PTAS");
            }
            set
            {
                var root = _inventoryCatalog.RSZ!.ObjectList[0];
                root.SetFieldValue("_PTAS", value);
            }
        }

        public int SpinelCount
        {
            get
            {
                var root = _inventoryCatalog.RSZ!.ObjectList[0];
                return root.Get<int>("_SpinelCount");
            }
            set
            {
                var root = _inventoryCatalog.RSZ!.ObjectList[0];
                root.SetFieldValue("_SpinelCount", value);
            }
        }

        public CatalogData[] Data
        {
            get
            {
                var root = _inventoryCatalog.RSZ!.ObjectList[0];
                return root.GetList("_Datas")
                    .Select(x => new CatalogData((RszInstance)x!))
                    .ToArray();
            }
        }

        public sealed class CatalogData(RszInstance _instance)
        {
            public int CharacterKindId => _instance.Get<int>("CharacterKindID");

            public int InventorySize
            {
                get => _instance.Get<int>("InventoryData.InventorySize.CurrInventorySize");
                set => _instance.Set("InventoryData.InventorySize.CurrInventorySize", value);
            }

            public InventoryItem[] InventoryItems
            {
                get
                {
                    return _instance.GetList("InventoryData.InventoryItems")
                        .Select(x => new InventoryItem((RszInstance)x!))
                        .ToArray();
                }
                set
                {
                    var list = _instance.GetList("InventoryData.InventoryItems");
                    list.Clear();
                    list.AddRange(value.Select(x => x.Instance));
                }
            }

            public UniqueItem[] UniqueItems => _instance.GetList("UniqueInventorySaveData.Items")
                .Select(x => new UniqueItem((RszInstance)x!))
                .ToArray();

            public int CharacterMaxHp
            {
                get => _instance.Get<int>("CharacterData._CharacterMaxHP");
                set => _instance.Set("CharacterData._CharacterMaxHP", value);
            }
        }

        public sealed class InventoryItem(RszInstance _instance)
        {
            public RszInstance Instance => _instance;

            public Item Item
            {
                get => new Item((RszInstance)_instance.Get("Item")!);
                set => _instance.Set("Item", value.Instance);
            }

            public int SlotType
            {
                get => _instance.Get<int>("SlotType");
                set => _instance.Set("SlotType", value);
            }

            public int SlotIndexRow
            {
                get => _instance.Get<int>("STRUCT_SlotIndex_Row");
                set => _instance.Set("STRUCT_SlotIndex_Row", value);
            }

            public int SlotIndexColumn
            {
                get => _instance.Get<int>("STRUCT_SlotIndex_Column");
                set => _instance.Set("STRUCT_SlotIndex_Column", value);
            }

            public int CurrDirection
            {
                get => _instance.Get<int>("CurrDirection");
                set => _instance.Set("CurrDirection", value);
            }
        }

        public class UniqueItem(RszInstance _instance)
        {
            public Item Item => new Item((RszInstance)_instance.Get("Item")!);
        }

        public class Item(RszInstance _instance)
        {
            public RszInstance Instance => _instance;

            public Guid Id
            {
                get => _instance.Get<Guid>("_ID");
                set => _instance.Set("_ID", value);
            }

            public int ItemId
            {
                get => _instance.Get<int>("_ItemId");
                set => _instance.Set("_ItemId", value);
            }

            public uint CurrentCondition
            {
                get => _instance.Get<uint>("_CurrentCondition");
                set => _instance.Set("_CurrentCondition", value);
            }

            public int CurrentDurability
            {
                get => _instance.Get<int>("_CurrentDurability");
                set => _instance.Set("_CurrentDurability", value);
            }

            public int CurrentItemCount
            {
                get => _instance.Get<int>("_CurrentItemCount");
                set => _instance.Set("_CurrentItemCount", value);
            }

            public override string ToString()
            {
                var itemName = ItemDefinitionRepository.Default.GetName(ItemId);
                return $"{itemName} x{CurrentItemCount}";
            }
        }
    }
}
