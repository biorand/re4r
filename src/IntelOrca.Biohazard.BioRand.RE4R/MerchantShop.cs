using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class MerchantShop
    {
        private const string ItemSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2";
        private const string StockAdditionSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopstockadditionsettinguserdata.user.2";
        private const string RewardSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshoprewardsettinguserdata.user.2";
        private readonly UserFile _itemSettings;
        private readonly UserFile _stockAdditionSettings;
        private readonly UserFile _rewardSettings;

        private MerchantShop(UserFile itemSettings, UserFile stockAdditionSettings, UserFile rewardSettings)
        {
            _itemSettings = itemSettings;
            _stockAdditionSettings = stockAdditionSettings;
            _rewardSettings = rewardSettings;
        }

        public static MerchantShop FromData(FileRepository fileRepository)
        {
            var itemSettings = GetUserFile(fileRepository, ItemSettingsPath);
            var stockAdditionSettings = GetUserFile(fileRepository, StockAdditionSettingsPath);
            var rewardSettings = GetUserFile(fileRepository, RewardSettingsPath);
            return new MerchantShop(itemSettings, stockAdditionSettings, rewardSettings);
        }

        private static UserFile GetUserFile(FileRepository fileRepository, string path)
        {
            var data = fileRepository.GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : ChainsawRandomizerFactory.Default.ReadUserFile(data);
        }

        public ShopItem[] ShopItems
        {
            get => _itemSettings.RSZ!.ObjectList[0].GetList("_Datas")
                .Select(x => new ShopItem((RszInstance)x!))
                .ToArray();
        }

        public StockAddition[] StockAdditions
        {
            get => _stockAdditionSettings.RSZ!.ObjectList[0].GetList("_Settings")
                .Select(x => new StockAddition((RszInstance)x!))
                .ToArray();
        }

        public Reward[] Rewards
        {
            get => _rewardSettings.RSZ!.ObjectList[0].GetList("_Settings")
                .Select(x => new Reward((RszInstance)x!))
                .ToArray();
        }

        public sealed class ShopItem(RszInstance _instance)
        {
            public int ItemId => (int)_instance.GetFieldValue("_ItemId")!;
            public PriceSetting[] Price => _instance.GetList("_PriceSettings")
                .Select(x => new PriceSetting((RszInstance)x!))
                .ToArray();
            public uint UnlockCondition => _instance.Get<uint>("_UnlockSetting._UnlockCondition")!;
            public Guid UnlockFlag => _instance.Get<Guid>("_UnlockSetting._UnlockFlag")!;
            public int UnlockChapter => _instance.Get<int>("_UnlockSetting._UnlockTiming")!;
            public uint SpCondition => _instance.Get<uint>("_UnlockSetting._SpCondition")!;
            public bool EnableStockSetting => _instance.Get<bool>("_StockSetting._EnableStockSetting")!;
            public bool EnableSelectCount => _instance.Get<bool>("_StockSetting._EnableSelectCount")!;
            public int MaxStock => _instance.Get<int>("_StockSetting._MaxStock")!;
            public int DefaultStock => _instance.Get<int>("_StockSetting._DefaultStock")!;
            public Sale[] Sales
            {
                get
                {
                    var list = _instance.GetList("_SaleSetting._Settings");
                    if (list == null)
                        return [];

                    return list
                        .Select(x => new Sale((RszInstance)x!))
                        .ToArray();
                }
            }

            public override string ToString()
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(ItemId);
                var itemName = itemDefinition?.Name ?? ItemId.ToString();
                return itemName;
            }
        }

        public sealed class PriceSetting(RszInstance _instance)
        {
            public int Difficulty => _instance.Get<int>("_Difficulty");
            public int PurchasePrice => _instance.Get<int>("_Price._PurchasePrice");
            public int SellingPrice => _instance.Get<int>("_Price._SellingPrice");

            public override string ToString()
            {
                return $"Difficulty = {Difficulty} Buy = {PurchasePrice} Sell = {SellingPrice}";
            }
        }

        public sealed class Sale(RszInstance _instance)
        {
            public int Mode => _instance.Get<int>("_Mode");
            public int SaleType => _instance.Get<int>("_SaleType");
            public int StartTiming => _instance.Get<int>("_StartTiming");
            public int EndTiming => _instance.Get<int>("_EndTiming");
            public Guid StartGlobalFlag => _instance.Get<Guid>("_StartGlobalFlag");
            public Guid EndGlobalFlag => _instance.Get<Guid>("_EndGlobalFlag");
            public int SaleRate => _instance.Get<int>("_SaleRate");

            public override string ToString()
            {
                return $"Discount = {SaleRate}% Start = {StartTiming} End = {EndTiming}";
            }
        }

        public sealed class StockAddition(RszInstance _instance)
        {
            public int Chapter => _instance.Get<int>("_FlagType");
            public StockAdditionEntry[] Entries => _instance.GetList("_Settings")
                .Select(x => new StockAdditionEntry((RszInstance)x!))
                .ToArray();

            public override string ToString()
            {
                return $"Chapter = {Chapter}";
            }
        }

        public sealed class StockAdditionEntry(RszInstance _instance)
        {
            public int Difficulty => _instance.Get<int>("_Difficulty");
            public StockAdditionData[] Items => _instance.GetList("_Datas")
                .Select(x => new StockAdditionData((RszInstance)x!))
                .ToArray();

            public override string ToString()
            {
                return $"Difficulty = {Difficulty}";
            }
        }

        public sealed class StockAdditionData(RszInstance _instance)
        {
            public int ItemId => _instance.Get<int>("_AddItemId");
            public int Count => _instance.Get<int>("_AddCount");

            public override string ToString()
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(ItemId);
                var itemName = itemDefinition?.Name ?? ItemId.ToString();
                return $"Item = {itemName} Count = {Count}";
            }
        }

        public sealed class Reward(RszInstance _instance)
        {
            public bool Enable => _instance.Get<bool>("_Enable");
            public int RewardId => _instance.Get<int>("_RewardId");
            public int SpinelCount => _instance.Get<int>("_SpinelCount");
            public int ItemId => _instance.Get<int>("_RewardItemId");
            public int ItemCount => _instance.Get<int>("_ItemCount");
            public int Progress => _instance.Get<int>("_Progress");
            public int RecieveType => _instance.Get<int>("_RecieveType");
            public int Mode => _instance.Get<int>("_DisplaySetting._Mode");
            public int StartChapter => _instance.Get<int>("_DisplaySetting._StartTiming");
            public int EndChapter => _instance.Get<int>("_DisplaySetting._EndTiming");
            public Guid StartGlobalFlag => _instance.Get<Guid>("_DisplaySetting._StartGlobalFlag");
            public Guid EndGlobalFlag => _instance.Get<Guid>("_DisplaySetting._EndGlobalFlag");

            public override string ToString()
            {
                var itemName = ItemDefinitionRepository.Default.GetName(ItemId);
                return $"RewardId = {RewardId} Spinel = {SpinelCount} Item = {itemName} x{ItemCount} RecieveType = {RecieveType}";
            }
        }
    }
}
