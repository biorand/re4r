namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawMerchantShop
    {
        private const string ItemSettingsPathLeon = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2";
        private const string StockAdditionSettingsPathLeon = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopstockadditionsettinguserdata.user.2";
        private const string RewardSettingsPathLeon = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshoprewardsettinguserdata.user.2";
        private const string CategorySettingsPathLeon = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshoppurchasecategorysettinguserdata.user.2";
        private const string ItemSettingsPathAda = "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopitemsettinguserdata_ao.user.2";
        private const string StockAdditionSettingsPathAda = "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopstockadditionsettinguserdata_ao.user.2";
        private const string RewardSettingsPathAda = "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshoprewardsettinguserdata_ao.user.2";
        private const string CategorySettingsPathAda = "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshoppurchasecategorysettinguserdata_cp11.user.2";

        private readonly Campaign _campaign;
        private readonly string _itemSettingsPath;
        private readonly string _stockAdditionSettingsPath;
        private readonly string _rewardSettingsPath;
        private readonly string _categorySettingsPath;
        private readonly chainsaw.InGameShopItemSettingUserdata _itemSettings;
        private readonly chainsaw.InGameShopStockAdditionSettingUserdata _stockAdditionSettings;
        private readonly chainsaw.InGameShopRewardSettingUserdata _rewardSettings;
        private readonly chainsaw.InGameShopPurchaseCategorySettingUserdata _categorySettings;

        public chainsaw.InGameShopItemSettingUserdata Items => _itemSettings;
        public chainsaw.InGameShopStockAdditionSettingUserdata StockAdditions => _stockAdditionSettings;
        public chainsaw.InGameShopRewardSettingUserdata Rewards => _rewardSettings;
        public chainsaw.InGameShopPurchaseCategorySettingUserdata Categories => _categorySettings;

        private ChainsawMerchantShop(FileRepository fileRepository, Campaign campaign)
        {
            _campaign = campaign;
            if (campaign == Campaign.Leon)
            {
                _itemSettingsPath = ItemSettingsPathLeon;
                _stockAdditionSettingsPath = StockAdditionSettingsPathLeon;
                _rewardSettingsPath = RewardSettingsPathLeon;
                _categorySettingsPath = CategorySettingsPathLeon;
            }
            else
            {
                _itemSettingsPath = ItemSettingsPathAda;
                _stockAdditionSettingsPath = StockAdditionSettingsPathAda;
                _rewardSettingsPath = RewardSettingsPathAda;
                _categorySettingsPath = CategorySettingsPathAda;
            }
            _itemSettings = fileRepository.DeserializeUserFile<chainsaw.InGameShopItemSettingUserdata>(_itemSettingsPath);
            _stockAdditionSettings = fileRepository.DeserializeUserFile<chainsaw.InGameShopStockAdditionSettingUserdata>(_stockAdditionSettingsPath);
            _rewardSettings = fileRepository.DeserializeUserFile<chainsaw.InGameShopRewardSettingUserdata>(_rewardSettingsPath);
            _categorySettings = fileRepository.DeserializeUserFile<chainsaw.InGameShopPurchaseCategorySettingUserdata>(_categorySettingsPath);
        }

        public static ChainsawMerchantShop FromData(FileRepository fileRepository, Campaign campaign) => new ChainsawMerchantShop(fileRepository, campaign);

        public void Save(FileRepository fileRepository)
        {
            fileRepository.SerializeUserFile(_itemSettingsPath, _itemSettings);
            fileRepository.SerializeUserFile(_stockAdditionSettingsPath, _stockAdditionSettings);
            fileRepository.SerializeUserFile(_rewardSettingsPath, _rewardSettings);
            fileRepository.SerializeUserFile(_categorySettingsPath, _categorySettings);
        }

        public void ClearRewards()
        {
            _rewardSettings._Settings.Clear();
        }

        public chainsaw.InGameShopRewardSingleSetting AddReward(Item item, int requiredSpinel, bool unlimited, int startChapter = 0)
        {
            var reward = new chainsaw.InGameShopRewardSingleSetting();
            reward._RewardId = _rewardSettings._Settings.Count;
            reward._Enable = true;
            reward._SpinelCount = requiredSpinel;
            reward._RewardItemId = item.Id;
            reward._ItemCount = item.Count;
            reward._RecieveType = unlimited ? 1 : 0;
            reward._DisplaySetting._StartTiming = startChapter;
            if (startChapter == 0)
            {
                reward._DisplaySetting._Mode = 0;
                reward._DisplaySetting._EndTiming = -1;
            }
            else
            {
                reward._DisplaySetting._Mode = 1;
                reward._DisplaySetting._EndTiming = 16;
            }
            _rewardSettings._Settings.Add(reward);
            return reward;
        }
    }
}
