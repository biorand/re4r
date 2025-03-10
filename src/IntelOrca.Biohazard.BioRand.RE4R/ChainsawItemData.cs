﻿using System;
using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class ChainsawItemData
    {
        private readonly FileRepository _fileRepository;
        private readonly (string Path, UserFile Data)[] _itemDefinitions;

        private ChainsawItemData(FileRepository repository, (string, UserFile)[] itemDefinitions)
        {
            _fileRepository = repository;
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

            var fileRepository = randomizer.FileRepository;
            var itemDefinitions = files
                .Select(x => (x, fileRepository.GetUserFile(x)))
                .ToArray();
            return new ChainsawItemData(fileRepository, itemDefinitions);
        }

        public void Save()
        {
            for (var i = 0; i < _itemDefinitions.Length; i++)
            {
                _fileRepository.SetUserFile(_itemDefinitions[i].Path, _itemDefinitions[i].Data);
            }
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

                    var itemDefinition = ItemDefinitionRepository.Default.Find(itemId);
                    if (itemDefinition == null)
                        return itemSize;

                    var isWeapon =
                        itemDefinition.Kind == ItemKinds.Weapon ||
                        itemDefinition.Kind == ItemKinds.Grenade ||
                        itemDefinition.Kind == ItemKinds.Knife;
                    return isWeapon ? weaponItemSize : itemSize;
                }
            }

            var itemDefinition2 = ItemDefinitionRepository.Default.Find(itemId);
            if (itemDefinition2?.Size == null)
                return new ItemSize();

            return ItemSize.Parse(itemDefinition2.Size);
        }

        public ChainsawItemDefinition[] Definitions =>
            _itemDefinitions.SelectMany(x =>
                x.Data.RSZ!.ObjectList[0].GetList("_Datas")
                    .Select(x => new ChainsawItemDefinition((RszInstance)x!))
                    .ToArray())
                .ToArray();

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
            public int StackMax
            {
                get => _instance.Get<int>("_StackMax")!;
                set => _instance.Set("_StackMax", value);
            }
            public int DefaultDurabilityMax => _instance.Get<int>("_DefaultDurabilityMax")!;
        }

        public sealed class WeaponDefineData(RszInstance instance) : ItemDefineData(instance)
        {
            public int AmmoMax
            {
                get => Instance.Get<int>("_AmmoMax")!;
                set => Instance.Set("_AmmoMax", value);
            }
            public int AmmoCost => Instance.Get<int>("_AmmoCost")!;
        }
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
