using System;
using System.Collections.Generic;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class ChainsawItemData
    {
        private readonly IReeRandomizerContext _context;
        private readonly (string Path, ItemDefinitionUserData Data)[] _itemDefinitions;

        private ChainsawItemData(IReeRandomizerContext context, (string, ItemDefinitionUserData)[] itemDefinitions)
        {
            _context = context;
            _itemDefinitions = itemDefinitions;
        }

        public static ChainsawItemData FromRandomizer(ChainsawRandomizer randomizer)
        {
            var files = new List<string>();
            if (randomizer.Campaign == Campaign.Leon)
            {
                files.Add("natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2");
                if (randomizer.GetConfigOption<bool>("allow-dlc-items"))
                {
                    files.Add("natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1401/itemdefinitionuserdata_dlc_1401.user.2");
                    files.Add("natives/stm/_chainsaw/appsystem/catalog/dlc/dlc_1402/itemdefinitionuserdata_dlc_1402.user.2");
                }
            }
            else
            {
                files.Add("natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ao.user.2");
                files.Add("natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ovr_ao.user.2");
            }

            var context = (IReeRandomizerContext)randomizer;
            var itemDefinitions = files
                .Select(x => (x, context.DeserializeUserFile<ItemDefinitionUserData>(x)))
                .ToArray();
            return new ChainsawItemData(context, itemDefinitions);
        }

        public void Save()
        {
            for (var i = 0; i < _itemDefinitions.Length; i++)
            {
                _context.SerializeUserFile(_itemDefinitions[i].Path, _itemDefinitions[i].Data);
            }
        }

        public int GetMaxAmmo(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def._ItemId == itemId)
                {
                    return Math.Max(def._ItemDefineData._StackMax, def._WeaponDefineData._AmmoMax);
                }
            }
            return 0;
        }

        public int GetMaxDurability(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def._ItemId == itemId)
                {
                    return Math.Max(def._ItemDefineData._DefaultDurabilityMax, def._WeaponDefineData._DefaultDurabilityMax);
                }
            }
            return 0;
        }

        public ItemSize GetSize(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def._ItemId == itemId)
                {
                    var itemSize = def._ItemDefineData._ItemSize;
                    var weaponItemSize = def._WeaponDefineData._ItemSize;

                    var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                    if (itemDefinition == null)
                        return new ItemSize(itemSize);

                    var isWeapon =
                        itemDefinition.Kind == ItemKinds.Weapon ||
                        itemDefinition.Kind == ItemKinds.Grenade ||
                        itemDefinition.Kind == ItemKinds.Knife;
                    return new ItemSize(isWeapon ? weaponItemSize : itemSize);
                }
            }

            var itemDefinition2 = ItemDefinitionRepository.Default.Find(itemId);
            if (itemDefinition2?.Size == null)
                return new ItemSize();

            return ItemSize.Parse(itemDefinition2.Size);
        }

        public IEnumerable<chainsaw.ItemDefinitionUserData.Data> Definitions => _itemDefinitions.SelectMany(x => x.Data._Datas);
    }

    public struct ItemSize(int kind)
    {
        public int Kind => kind;
        public int Width => IsValid ? _sizes[kind, 1] : 1;
        public int Height => IsValid ? _sizes[kind, 0] : 1;
        public int LongSide => Math.Max(Width, Height);
        public bool IsValid => kind >= 0 && kind < _sizes.Length;

        public object Area => Width * Height;

        public override string ToString() => $"{Width}x{Height}";

        private static byte[,] _sizes =
        {
            { 1, 1 },
            { 1, 2 },
            { 1, 3 },
            { 1, 4 },
            { 1, 5 },
            { 1, 9 },
            { 2, 1 },
            { 2, 2 },
            { 2, 3 },
            { 2, 4 },
            { 2, 5 },
            { 2, 6 },
            { 2, 7 },
            { 2, 8 },
            { 3, 1 },
            { 3, 5 },
            { 3, 7 },
            { 4, 1 },
            { 4, 2 },
            { 6, 2 },
        };

        public static ItemSize Parse(string size)
        {
            var wh = size.Split('x').Take(2).Select(int.Parse).ToArray();
            for (int i = 0; i < _sizes.GetLength(0); i++)
            {
                if (_sizes[i, 0] == wh[0] && _sizes[i, 1] == wh[1])
                {
                    return new ItemSize(i);
                }
            }
            throw new ArgumentException($"No size defined for {size}");
        }
    }
}
