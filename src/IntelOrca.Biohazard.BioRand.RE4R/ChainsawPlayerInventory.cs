using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RectangleBinPacking;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawPlayerInventory
    {
        private const string InventoryCatalogPathLeon = "natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/inventorycatalog_main.user.2";
        private const string InventoryCatalogPathAda = "natives/stm/_anotherorder/appsystem/inventory/inventorycatalog/inventorycatalog_ao.user.2";

        private readonly string _path;
        private readonly UserFile _inventoryCatalog;
        private readonly RszInstance _root;
        private readonly int _index;

        private ChainsawPlayerInventory(string path, UserFile inventoryCatalog, int index)
        {
            _path = path;
            _inventoryCatalog = inventoryCatalog;
            _root = _inventoryCatalog.RSZ!.ObjectList[0];
            _index = index;
        }

        public static ChainsawPlayerInventory FromData(FileRepository fileRepository, Campaign campaign)
        {
            var path = campaign == Campaign.Leon
                ? InventoryCatalogPathLeon
                : InventoryCatalogPathAda;
            var inventoryCatalog = GetUserFile(fileRepository, path);
            var index = campaign == Campaign.Leon ? 0 : 1;
            return new ChainsawPlayerInventory(path, inventoryCatalog, index);
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
            fileRepository.SetGameFileData(_path, _inventoryCatalog.ToByteArray());
        }

        public void ClearItems()
        {
            PlayerData.InventoryItems = [];
        }

        public void AddItem(Item item)
        {
            var inventoryItem = CreateInventoryItem(item);
            inventoryItem.SlotIndexColumn = 5;

            var items = PlayerData.InventoryItems.ToList();
            items.Add(inventoryItem);
            PlayerData.InventoryItems = items.ToArray();
        }

        public void UpdateWeapons(ChainsawItemData itemData)
        {
            foreach (var item in PlayerData.InventoryItems)
            {
                if (item.Item is WeaponItemStack weaponStack)
                {
                    weaponStack.CurrentItemCount = 1;
                    weaponStack.CurrentAmmoCount = itemData.GetMaxAmmo(item.Item.ItemId);
                }
                else
                {
                    item.Item.CurrentItemCount = Math.Max(1, itemData.GetMaxAmmo(item.Item.ItemId));
                }
                item.Item.CurrentDurability = itemData.GetMaxDurability(item.Item.ItemId);
            }
        }

        public void AutoSort(ChainsawItemData itemData)
        {
            var items = PlayerData.InventoryItems
                .OrderByDescending(x => itemData.GetSize(x.Item.ItemId).LongSide)
                .ToArray();
            PlayerData.InventoryItems = items;

            var caseWidth = 10;
            var caseHeight = 7;
            var binPack = new MaxRectsBinPack<int>(caseWidth, caseHeight, FreeRectChoiceHeuristic.RectBestAreaFit);
            var id = 0;
            foreach (var item in items)
            {
                var size = itemData.GetSize(item.Item.ItemId);
                var packResult = binPack.Insert(id++, size.Width, size.Height);
                if (packResult == null)
                {
                    item.SlotIndexColumn = -1;
                    item.SlotIndexRow = -1;
                    item.CurrDirection = 0;
                }
                else
                {
                    item.SlotIndexColumn = packResult.X;
                    item.SlotIndexRow = packResult.Y;
                    item.CurrDirection = packResult.Rotate ? 1 : 0;
                }
            }
        }

        public void AssignShortcuts()
        {
            var directionOrder = new int[] { 3, 1, 2, 0 };
            var items = PlayerData.InventoryItems.ToArray();
            var equips = PlayerData.EquipInfos;
            var shortcuts = PlayerData.ShortcutInfos;
            var knifeShortcut = shortcuts.First(x => x.EquipType == 1);
            var weaponShortcuts = shortcuts
                .Where(x => x.EquipType == 0 && x.Direction != 4)
                .OrderBy(x => x.ShortcutType)
                .ThenBy(x => directionOrder[x.Direction])
                .ToQueue();

            var primaryDone = false;
            var knifeDone = false;
            foreach (var item in items)
            {
                var itemId = item.Item.ItemId;
                var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                if (itemDefinition == null || (itemDefinition.Kind != ItemKinds.Weapon && itemDefinition.Kind != ItemKinds.Grenade))
                    continue;

                InventoryShortcutSaveData? shortcut = null;
                if (itemDefinition.Class == ItemClasses.Knife)
                {
                    if (!knifeDone)
                    {
                        knifeDone = true;
                        equips[1].Id = item.Item.Id;
                    }
                    shortcut = knifeShortcut;
                }
                else
                {
                    if (!primaryDone)
                    {
                        primaryDone = true;
                        equips[0].Id = item.Item.Id;
                    }
                    weaponShortcuts.TryDequeue(out shortcut);
                }
                if (shortcut != null)
                {
                    shortcut.Id = item.Item.Id;
                    shortcut.ItemId = itemId;
                    shortcut.ItemCount = 1;
                }
            }
        }

        private InventoryItem CreateInventoryItem(Item item, int count = 1)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var definition = itemRepo.Find(item.Id)!;
            var definitionAmmo = itemRepo.GetAmmo(definition);

            var rsz = _inventoryCatalog.RSZ!;
            ItemStack itemStack;
            switch (definition.Kind)
            {
                case ItemKinds.Weapon:
                case ItemKinds.Grenade:
                case ItemKinds.Knife:
                case ItemKinds.Egg:
                {
                    var witem = new WeaponItemStack(rsz.CreateInstance("chainsaw.WeaponItem"));
                    witem.CurrentAmmo = definitionAmmo?.Id ?? -1;
                    witem.CurrentAmmoCount = 4;
                    itemStack = witem;
                    break;
                }
                default:
                    itemStack = new ItemStack(rsz.CreateInstance("chainsaw.Item"));
                    break;
            }
            itemStack.Id = Guid.NewGuid();
            itemStack.ItemId = definition.Id;
            itemStack.CurrentDurability = 1000;
            itemStack.CurrentItemCount = count;

            var inventoryItem = new InventoryItem(rsz.CreateInstance("chainsaw.InventoryItemSaveData"));
            inventoryItem.Item = itemStack;
            return inventoryItem;
        }

        public int PTAS
        {
            get => _root.Get<int>("_PTAS");
            set => _root.SetFieldValue("_PTAS", value);
        }

        public int SpinelCount
        {
            get => _root.Get<int>("_SpinelCount");
            set => _root.SetFieldValue("_SpinelCount", value);
        }

        public CatalogData[] Data => _root
            .GetList("_Datas")
            .Select(x => new CatalogData((RszInstance)x!))
            .ToArray();

        public CatalogData PlayerData => Data[_index];

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

            public InventoryEquipSaveData[] EquipInfos => _instance
                .GetArray<RszInstance>("InventoryData.EquipInfos")
                .Select(x => new InventoryEquipSaveData(x))
                .ToArray();

            public InventoryShortcutSaveData[] ShortcutInfos => _instance
                .GetArray<RszInstance>("InventoryData.ShortcutInfos")
                .Select(x => new InventoryShortcutSaveData(x))
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

            public ItemStack Item
            {
                get
                {
                    var instance = (RszInstance)_instance.Get("Item")!;
                    return instance.RszClass.name == "chainsaw.WeaponItem"
                        ? new WeaponItemStack(instance)
                        : new ItemStack(instance);
                }
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

            public override string ToString() => Item.ToString();
        }

        public class UniqueItem(RszInstance _instance)
        {
            public ItemStack Item => new ItemStack((RszInstance)_instance.Get("Item")!);
        }

        public class WeaponItemStack(RszInstance instance) : ItemStack(instance)
        {
            public int CurrentAmmo
            {
                get => Instance.Get<int>("_CurrentAmmo");
                set => Instance.Set("_CurrentAmmo", value);
            }

            public int CurrentAmmoCount
            {
                get => Instance.Get<int>("_CurrentAmmoCount");
                set => Instance.Set("_CurrentAmmoCount", value);
            }

            public uint CurrentTacticalAmmoCount
            {
                get => Instance.Get<uint>("_CurrentTacticalAmmoCount");
                set => Instance.Set("_CurrentTacticalAmmoCount", value);
            }
        }

        public class ItemStack(RszInstance _instance)
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

        public class InventoryEquipSaveData(RszInstance _instance)
        {
            public Guid Id
            {
                get => _instance.Get<Guid>("ID");
                set => _instance.Set("ID", value);
            }

            public override string ToString() => Id.ToString();
        }

        public class InventoryShortcutSaveData(RszInstance _instance)
        {
            public Guid Id
            {
                get => _instance.Get<Guid>("ID");
                set => _instance.Set("ID", value);
            }

            public int EquipType => _instance.Get<int>("EquipType");

            public int ShortcutType => _instance.Get<int>("ShortcutType");

            public int Direction => _instance.Get<int>("Direction");

            public int ItemId
            {
                get => _instance.Get<int>("ItemId");
                set => _instance.Set("ItemId", value);
            }

            public int ItemCount
            {
                get => _instance.Get<int>("ItemCount");
                set => _instance.Set("ItemCount", value);
            }

            public override string ToString() => $"Type = {ShortcutType} Direction = {Direction} Id = {Id}";
        }
    }
}
