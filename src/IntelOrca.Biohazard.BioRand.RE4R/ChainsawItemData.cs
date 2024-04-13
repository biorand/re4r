using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class ChainsawItemData
    {
        private const string ItemDefinitionPath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2";

        private readonly UserFile _itemDefinitions;

        private ChainsawItemData(UserFile itemDefinitions)
        {
            _itemDefinitions = itemDefinitions;
        }

        public static ChainsawItemData FromData(FileRepository fileRepository)
        {
            var itemDefinitions = fileRepository.GetUserFile(ItemDefinitionPath);
            return new ChainsawItemData(itemDefinitions);
        }

        public void Save(FileRepository fileRepository)
        {
            fileRepository.SetUserFile(ItemDefinitionPath, _itemDefinitions);
        }

        public int GetMaxAmmo(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def.ItemId == itemId)
                {
                    return Math.Max(def.ItemDefineData.StackMax, def.WeaponDefineData.AmmoMax);
                }
            }
            return 0;
        }

        public int GetMaxDurability(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def.ItemId == itemId)
                {
                    return Math.Max(def.ItemDefineData.DefaultDurabilityMax, def.WeaponDefineData.DefaultDurabilityMax);
                }
            }
            return 0;
        }

        public ItemSize GetSize(int itemId)
        {
            foreach (var def in Definitions)
            {
                if (def.ItemId == itemId)
                {
                    var itemSize = def.ItemDefineData.ItemSize;
                    var weaponItemSize = def.WeaponDefineData.ItemSize;
                    return itemSize.Kind > weaponItemSize.Kind ? itemSize : weaponItemSize;
                }
            }
            return new ItemSize(0);
        }

        public ChainsawItemDefinition[] Definitions
        {
            get => _itemDefinitions.RSZ!.ObjectList[0].GetList("_Datas")
                .Select(x => new ChainsawItemDefinition((RszInstance)x!))
                .ToArray();
        }

        public sealed class ChainsawItemDefinition(RszInstance _instance)
        {
            public int ItemId => _instance.Get<int>("_ItemId")!;
            public ItemDefineData ItemDefineData => new ItemDefineData((RszInstance)_instance.Get("_ItemDefineData")!);
            public WeaponDefineData WeaponDefineData => new WeaponDefineData((RszInstance)_instance.Get("_WeaponDefineData")!);
        }

        public class ItemDefineData(RszInstance _instance)
        {
            public RszInstance Instance => _instance;

            public ItemSize ItemSize => new ItemSize(_instance.Get<int>("_ItemSize")!);
            public int StackMax => _instance.Get<int>("_StackMax")!;
            public int DefaultDurabilityMax => _instance.Get<int>("_DefaultDurabilityMax")!;
        }

        public sealed class WeaponDefineData(RszInstance instance) : ItemDefineData(instance)
        {
            public int AmmoMax => Instance.Get<int>("_AmmoMax")!;
            public int AmmoCost => Instance.Get<int>("_AmmoCost")!;
        }
    }

    public struct ItemSize(int kind)
    {
        public int Kind => kind;
        public int Width => IsValid ? _sizes[kind, 1] : 1;
        public int Height => IsValid ? _sizes[kind, 0] : 1;
        public bool IsValid => kind >= 0 && kind < _sizes.Length;
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
    }
}
