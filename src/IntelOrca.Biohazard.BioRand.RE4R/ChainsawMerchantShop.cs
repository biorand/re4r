using System;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawMerchantShop
    {
        private const string ItemSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2";
        private const string StockAdditionSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopstockadditionsettinguserdata.user.2";
        private const string RewardSettingsPath = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshoprewardsettinguserdata.user.2";
        private readonly UserFile _itemSettings;
        private readonly UserFile _stockAdditionSettings;
        private readonly UserFile _rewardSettings;

        private ChainsawMerchantShop(UserFile itemSettings, UserFile stockAdditionSettings, UserFile rewardSettings)
        {
            _itemSettings = itemSettings;
            _stockAdditionSettings = stockAdditionSettings;
            _rewardSettings = rewardSettings;
        }

        public static ChainsawMerchantShop FromData(FileRepository fileRepository)
        {
            var itemSettings = GetUserFile(fileRepository, ItemSettingsPath);
            var stockAdditionSettings = GetUserFile(fileRepository, StockAdditionSettingsPath);
            var rewardSettings = GetUserFile(fileRepository, RewardSettingsPath);
            return new ChainsawMerchantShop(itemSettings, stockAdditionSettings, rewardSettings);
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
            fileRepository.SetGameFileData(ItemSettingsPath, _itemSettings.ToByteArray());
            fileRepository.SetGameFileData(StockAdditionSettingsPath, _stockAdditionSettings.ToByteArray());
            fileRepository.SetGameFileData(RewardSettingsPath, _rewardSettings.ToByteArray());
        }

        public void ClearRewards()
        {
            Rewards = [];
        }

        public Reward AddReward(Item item, int requiredSpinel, bool unlimited, int startChapter = 0)
        {
            var instance = _rewardSettings.RSZ!.CreateInstance("chainsaw.InGameShopRewardSingleSetting");
            var reward = new Reward(instance);
            reward.Enable = true;
            reward.SpinelCount = requiredSpinel;
            reward.ItemId = item.Id;
            reward.ItemCount = item.Count;
            reward.RecieveType = unlimited ? 1 : 0;
            reward.StartChapter = startChapter;
            if (startChapter == 0)
            {
                reward.Mode = 0;
                reward.EndChapter = -1;
            }
            else
            {
                reward.Mode = 1;
                reward.EndChapter = 16;
            }

            var rewards = Rewards.ToList();
            reward.RewardId = rewards.Count == 0 ? 0 : rewards.Select(x => x.RewardId).Max() + 1;
            rewards.Add(reward);
            Rewards = rewards.ToArray();
            return reward;
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
            get
            {
                return _rewardSettings.RSZ!.ObjectList[0].GetList("_Settings")
                    .Select(x => new Reward((RszInstance)x!))
                    .ToArray();
            }
            set
            {
                var list = _rewardSettings.RSZ!.ObjectList[0].GetList("_Settings");
                list.Clear();
                list.AddRange(value.Select(x => x.Instance));
            }
        }

        public sealed class ShopItem(RszInstance _instance)
        {
            public void SetPrice(double multiplier)
            {
                var prices = Price;
                var corePrice = prices.FirstOrDefault(x => x.Difficulty == 20);
                if (corePrice == null)
                {
                    corePrice = prices.FirstOrDefault();
                    if (corePrice == null)
                        return;
                }
                corePrice.Difficulty = 20;
                corePrice.PurchasePrice = Round(corePrice.PurchasePrice * multiplier);
                corePrice.SellingPrice = Round(corePrice.SellingPrice * multiplier);
                Price = [corePrice];
            }

            private static int Round(double value)
            {
                if (value >= 1000)
                    return (int)(value / 1000) * 1000;
                if (value >= 100)
                    return (int)(value / 100) * 100;
                if (value >= 10)
                    return (int)(value / 10) * 10;
                return (int)value;
            }

            public void SetSale(ChainsawMerchantShop shop, int startChapter, int endChapter, int discount)
            {
                var instance = shop._itemSettings.RSZ!.CreateInstance("chainsaw.InGameShopItemSaleSingleSetting");
                var sale = new Sale(instance);
                sale.SaleType = 2;
                sale.StartTiming = startChapter;
                sale.EndTiming = endChapter;
                sale.SaleRate = discount;
                Sales = [sale];
            }

            public int BuyPrice
            {
                get => Price.FirstOrDefault()?.PurchasePrice ?? 0;
            }

            public int SellPrice
            {
                get => Price.FirstOrDefault()?.SellingPrice ?? 0;
            }

            public int ItemId => (int)_instance.GetFieldValue("_ItemId")!;

            public PriceSetting[] Price
            {
                get
                {
                    return _instance.GetList("_PriceSettings")
                        .Select(x => new PriceSetting((RszInstance)x!))
                        .ToArray();
                }
                set
                {
                    var lst = _instance.GetList("_PriceSettings");
                    lst.Clear();
                    lst.AddRange(value.Select(x => x.Instance));
                }
            }

            public uint UnlockCondition
            {
                get => _instance.Get<uint>("_UnlockSetting._UnlockCondition")!;
                set => _instance.Set("_UnlockSetting._UnlockCondition", value);
            }

            public Guid UnlockFlag
            {
                get => _instance.Get<Guid>("_UnlockSetting._UnlockFlag")!;
                set => _instance.Set("_UnlockSetting._UnlockFlag", value);
            }

            public int UnlockChapter
            {
                get => _instance.Get<int>("_UnlockSetting._UnlockTiming")!;
                set => _instance.Set("_UnlockSetting._UnlockTiming", value);
            }

            public uint SpCondition
            {
                get => _instance.Get<uint>("_UnlockSetting._SpCondition")!;
                set => _instance.Set("_UnlockSetting._SpCondition", value);
            }

            public bool EnableStockSetting
            {
                get => _instance.Get<bool>("_StockSetting._EnableStockSetting")!;
                set => _instance.Set("_StockSetting._EnableStockSetting", value);
            }

            public bool EnableSelectCount
            {
                get => _instance.Get<bool>("_StockSetting._EnableSelectCount")!;
                set => _instance.Set("_StockSetting._EnableSelectCount", value);
            }

            public int MaxStock
            {
                get => _instance.Get<int>("_StockSetting._MaxStock")!;
                set => _instance.Set("_StockSetting._MaxStock", value);
            }

            public int DefaultStock
            {
                get => _instance.Get<int>("_StockSetting._DefaultStock")!;
                set => _instance.Set("_StockSetting._DefaultStock", value);
            }

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
                set
                {
                    var values = (value ?? [])
                        .Select(x => (object)x.Instance)
                        .ToList();
                    _instance.Set("_SaleSetting._Settings", values);
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
            public RszInstance Instance => _instance;

            public int Difficulty
            {
                get => _instance.Get<int>("_Difficulty");
                set => _instance.Set("_Difficulty", value);
            }

            public int PurchasePrice
            {
                get => _instance.Get<int>("_Price._PurchasePrice");
                set => _instance.Set("_Price._PurchasePrice", value);
            }

            public int SellingPrice
            {
                get => _instance.Get<int>("_Price._SellingPrice");
                set => _instance.Set("_Price._SellingPrice", value);
            }

            public override string ToString()
            {
                return $"Difficulty = {Difficulty} Buy = {PurchasePrice} Sell = {SellingPrice}";
            }
        }

        public sealed class Sale(RszInstance _instance)
        {
            public RszInstance Instance => _instance;

            public int Mode
            {
                get => _instance.Get<int>("_Mode");
                set => _instance.Set("_Mode", value);
            }

            public int SaleType
            {
                get => _instance.Get<int>("_SaleType");
                set => _instance.Set("_SaleType", value);
            }

            public int StartTiming
            {
                get => _instance.Get<int>("_StartTiming");
                set => _instance.Set("_StartTiming", value);
            }

            public int EndTiming
            {
                get => _instance.Get<int>("_EndTiming");
                set => _instance.Set("_EndTiming", value);
            }

            public Guid StartGlobalFlag => _instance.Get<Guid>("_StartGlobalFlag");

            public Guid EndGlobalFlag => _instance.Get<Guid>("_EndGlobalFlag");

            public int SaleRate
            {
                get => _instance.Get<int>("_SaleRate");
                set => _instance.Set("_SaleRate", value);
            }

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
            public RszInstance Instance => _instance;

            public bool Enable
            {
                get => _instance.Get<bool>("_Enable");
                set => _instance.Set("_Enable", value);
            }

            public int RewardId
            {
                get => _instance.Get<int>("_RewardId");
                set => _instance.Set("_RewardId", value);
            }

            public int SpinelCount
            {
                get => _instance.Get<int>("_SpinelCount");
                set => _instance.Set("_SpinelCount", value);
            }

            public int ItemId
            {
                get => _instance.Get<int>("_RewardItemId");
                set => _instance.Set("_RewardItemId", value);
            }

            public int ItemCount
            {
                get => _instance.Get<int>("_ItemCount");
                set => _instance.Set("_ItemCount", value);
            }

            public int Progress => _instance.Get<int>("_Progress");

            public int RecieveType
            {
                get => _instance.Get<int>("_RecieveType");
                set => _instance.Set("_RecieveType", value);
            }

            public int Mode
            {
                get => _instance.Get<int>("_DisplaySetting._Mode");
                set => _instance.Set("_DisplaySetting._Mode", value);
            }

            public int StartChapter
            {
                get => _instance.Get<int>("_DisplaySetting._StartTiming");
                set => _instance.Set("_DisplaySetting._StartTiming", value);
            }

            public int EndChapter
            {
                get => _instance.Get<int>("_DisplaySetting._EndTiming");
                set => _instance.Set("_DisplaySetting._EndTiming", value);
            }

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
