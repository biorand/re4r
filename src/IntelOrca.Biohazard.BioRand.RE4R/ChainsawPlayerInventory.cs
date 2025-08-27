using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;
using RectangleBinPacking;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawPlayerInventory
    {
        private const string InventoryCatalogPathLeon = "natives/stm/_chainsaw/appsystem/inventory/inventorycatalog/inventorycatalog_main.user.2";
        private const string InventoryCatalogPathAda = "natives/stm/_anotherorder/appsystem/inventory/inventorycatalog/inventorycatalog_ao.user.2";

        private readonly string _path;
        private readonly UserFile.Builder _inventoryCatalog;
        private readonly chainsaw.InventoryCatalogUserData _root;
        private readonly int _index;

        private ChainsawPlayerInventory(string path, UserFile inventoryCatalog, int index)
        {
            _path = path;
            _inventoryCatalog = inventoryCatalog.ToBuilder(FileRepository.RszRepository);
            _root = RszSerializer.Deserialize<chainsaw.InventoryCatalogUserData>(_inventoryCatalog.Objects[0])!;
            _index = index;
        }

        public static ChainsawPlayerInventory FromData(FileRepository fileRepository, Campaign campaign)
        {
            var path = campaign == Campaign.Leon
                ? InventoryCatalogPathLeon
                : InventoryCatalogPathAda;
            var inventoryCatalog = fileRepository.GetUserFile(path);
            var index = campaign == Campaign.Leon ? 0 : 1;
            return new ChainsawPlayerInventory(path, inventoryCatalog, index);
        }

        public void Save(FileRepository fileRepository)
        {
            var rszType = ((RszObjectNode)_inventoryCatalog.Objects[0]).Type;
            _inventoryCatalog.Objects = [RszSerializer.Serialize(rszType, _root)];
            fileRepository.SetUserFile(_path, _inventoryCatalog.Build());
        }

        public void ClearItems()
        {
            PlayerData.InventoryData.InventoryItems = [];
        }

        public void AddItem(Item item)
        {
            var inventoryItem = CreateInventoryItem(item);
            PlayerData.InventoryData.InventoryItems.Add(inventoryItem);
        }

        public void UpdateWeapons(ChainsawItemData itemData)
        {
            foreach (var item in PlayerData.InventoryData.InventoryItems)
            {
                if (item.Item is chainsaw.WeaponItem weaponStack)
                {
                    weaponStack._CurrentItemCount = 1;
                    weaponStack._CurrentAmmoCount = itemData.GetMaxAmmo(item.Item._ItemId);
                }
                else
                {
                    item.Item._CurrentItemCount = Math.Max(1, itemData.GetMaxAmmo(item.Item._ItemId));
                }
                item.Item._CurrentDurability = itemData.GetMaxDurability(item.Item._ItemId);
            }
        }

        public void AutoSort(ChainsawItemData itemData)
        {
            var items = PlayerData.InventoryData.InventoryItems
                .OrderByDescending(x => itemData.GetSize(x.Item._ItemId).LongSide)
                .ToList();
            PlayerData.InventoryData.InventoryItems = items;

            var caseWidth = 10;
            var caseHeight = 7;
            var binPack = new MaxRectsBinPack<int>(caseWidth, caseHeight, FreeRectChoiceHeuristic.RectBestAreaFit);
            var id = 0;
            foreach (var item in items)
            {
                var size = itemData.GetSize(item.Item._ItemId);
                var packResult = binPack.Insert(id++, size.Width, size.Height);
                if (packResult == null)
                {
                    item.STRUCT_SlotIndex_Column = -1;
                    item.STRUCT_SlotIndex_Row = -1;
                    item.CurrDirection = 0;
                }
                else
                {
                    item.STRUCT_SlotIndex_Column = packResult.X;
                    item.STRUCT_SlotIndex_Row = packResult.Y;
                    item.CurrDirection = packResult.Rotate ? 1 : 0;
                }
            }
        }

        public void AssignShortcuts()
        {
            var directionOrder = new int[] { 3, 1, 2, 0 };
            var items = PlayerData.InventoryData.InventoryItems.ToArray();
            var equips = PlayerData.InventoryData.EquipInfos;
            var shortcuts = PlayerData.InventoryData.ShortcutInfos;
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
                var itemId = item.Item._ItemId;
                var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                if (itemDefinition == null || (itemDefinition.Kind != ItemKinds.Weapon && itemDefinition.Kind != ItemKinds.Grenade))
                    continue;

                chainsaw.InventoryShortcutSaveData? shortcut = null;
                if (itemDefinition.Class == ItemClasses.Knife)
                {
                    if (!knifeDone)
                    {
                        knifeDone = true;
                        equips[1].ID = item.Item._ID;
                    }
                    shortcut = knifeShortcut;
                }
                else
                {
                    if (!primaryDone)
                    {
                        primaryDone = true;
                        equips[0].ID = item.Item._ID;
                    }
                    weaponShortcuts.TryDequeue(out shortcut);
                }
                if (shortcut != null)
                {
                    shortcut.ID = item.Item._ID;
                    shortcut.ItemId = itemId;
                    shortcut.ItemCount = 1;
                }
            }
        }

        private chainsaw.InventoryItemSaveData CreateInventoryItem(Item item, int count = 1)
        {
            var repo = FileRepository.RszRepository;
            var itemRepo = ItemDefinitionRepository.Default;
            var definition = itemRepo.Find(item.Id)!;
            var definitionAmmo = itemRepo.GetAmmo(definition);

            chainsaw.Item itemStack;
            switch (definition.Kind)
            {
                case ItemKinds.Weapon:
                case ItemKinds.Grenade:
                case ItemKinds.Knife:
                case ItemKinds.Egg:
                {
                    var witem = new chainsaw.WeaponItem();
                    witem._CurrentAmmo = definitionAmmo?.Id ?? -1;
                    witem._CurrentAmmoCount = 4;
                    itemStack = witem;
                    break;
                }
                default:
                    itemStack = new chainsaw.Item();
                    break;
            }
            itemStack._ID = Guid.NewGuid();
            itemStack._ItemId = definition.Id;
            itemStack._CurrentDurability = 1000;
            itemStack._CurrentItemCount = count;

            var inventoryItem = new chainsaw.InventoryItemSaveData();
            inventoryItem.Item = itemStack;
            return inventoryItem;
        }

        public int PTAS
        {
            get => _root._PTAS;
            set => _root._PTAS = value;
        }

        public int SpinelCount
        {
            get => _root._SpinelCount;
            set => _root._SpinelCount = value;
        }

        public chainsaw.InventoryCatalogUserData.Data PlayerData => _root._Datas[_index];
    }
}
