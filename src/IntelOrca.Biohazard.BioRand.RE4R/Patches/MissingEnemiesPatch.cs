using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    /// <summary>
    /// Adds enemies from Ada's campaign to Leon, and visa versa. Also adds mad chainsaw
    /// from mercenaries to both Leon and Ada's campaign.
    /// </summary>
    /// <param name="context"></param>
    internal class MissingEnemiesPatch(IPatchContext context) : IPatch
    {
        private static readonly ImmutableArray<EnemyCatalogInfo> EnemyInfo = [
            new EnemyCatalogInfo("colmillos", Campaign.Leon, 200004, "ch1d2z0", 8),
            new EnemyCatalogInfo("krauser_1", Campaign.Leon, 200011, "ch1b7z0", 20),
            new EnemyCatalogInfo("krauser_2", Campaign.Leon, 200013, "ch1f7z0", 22),
            new EnemyCatalogInfo("verdugo", Campaign.Leon, 200015, "ch1f2z0", 24),
            new EnemyCatalogInfo("salazar", Campaign.Leon, 200017, "ch1f6z0", 12),
            new EnemyCatalogInfo("sadler_1", Campaign.Leon, 200018, "ch1f8z0", 28),
            new EnemyCatalogInfo("mendez_2", Campaign.Leon, 200020, "ch1f4z1", 32),
            new EnemyCatalogInfo("mendez_3", Campaign.Leon, 200021, "ch1f5z1", 33),

            new EnemyCatalogInfo("martinico", Campaign.Ada, 200022, "ch4d7z0", 34),
            new EnemyCatalogInfo("pesanta_phantom", Campaign.Ada, 200024, "ch4faz0", 36),
            new EnemyCatalogInfo("pesanta", Campaign.Ada, 200025, "ch4faz1", 37),
            new EnemyCatalogInfo("sadler_human", Campaign.Ada, 200028, "ch4fez0", 40),
            new EnemyCatalogInfo("u3", Campaign.Ada, 200029, "ch4fbz0", 48),

            new EnemyCatalogInfo("chainsaw_mad", Campaign.Mercenaries, 81109, "ch7k0z0", 49),
        ];

        public void Apply()
        {
            // Add characters to catalog
            AddCharacterToCatalog(Campaign.Leon);
            AddCharacterToCatalog(Campaign.Ada);

            // Mad chainsaw specific
            AddWeaponToCatalog_6400(Campaign.Leon);
            AddWeaponToCatalog_6400(Campaign.Ada);
            FixMadChainsawKnifeAnimation();
        }

        private void AddCharacterToCatalog(Campaign campaign)
        {
            var infos = EnemyInfo.Where(x => x.Campaign != campaign).ToImmutableArray();

            var bodyPath = campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/catalog/character/characterbodycatalog_2nd.user.2" :
                "natives/stm/_anotherorder/appsystem/catalog/maincontents/characterbodycatalog_ao.user.2";
            var headPath = campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/catalog/character/characterheadcatalog_2nd.user.2" :
                "natives/stm/_anotherorder/appsystem/catalog/maincontents/characterheadcatalog_ao.user.2";
            var costumePath = campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/catalog/character/costumepresetcatalog_2nd.user.2" :
                "natives/stm/_anotherorder/appsystem/catalog/maincontents/costumepresetcatalog_ao.user.2";
            var attackPath = campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/catalog/maincontents/attackdatacatalog.user.2" :
                "natives/stm/_anotherorder/appsystem/catalog/maincontents/attackdatacatalog_ao.user.2";
            var rankPath = campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/catalog/maincontents/gamerankcatalog.user.2" :
                "natives/stm/_anotherorder/appsystem/catalog/maincontents/gamerankcatalog_ao.user.2";

            context.ModifyUserFile(bodyPath, root =>
            {
                var dataTable = root.Get<RszArrayNode>("_DataTable");
                foreach (var info in infos)
                {
                    if (dataTable.Any(x => x.Get<int>("_CharacterKindID") == info.Id))
                        continue;

                    dataTable = dataTable.Add(
                        context.TypeRepository.Create("chainsaw.CharacterBodyCatalogUserData.Data")
                            .Set("_CharacterKindID", info.Id)
                            .Set("_BodyPrefab.Path", $"{info.GetBasePath()}_body.pfb"));
                }
                return root.Set("_DataTable", dataTable);
            });
            context.ModifyUserFile(headPath, root =>
            {
                var dataTable = root.Get<RszArrayNode>("_DataTable");
                foreach (var info in infos)
                {
                    if (dataTable.Any(x => x.Get<int>("_CharacterKindID") == info.Id))
                        continue;

                    dataTable = dataTable.Add(
                        context.TypeRepository.Create("chainsaw.CharacterHeadCatalogUserData.Data")
                            .Set("_CharacterKindID", info.Id)
                            .Set("_HeadPrefab.Path", $"{info.GetBasePath()}_head.pfb"));
                }
                return root.Set("_DataTable", dataTable);
            });
            context.ModifyUserFile(costumePath, root =>
            {
                var dataTable = root.Get<RszArrayNode>("_DataTable");
                foreach (var info in infos)
                {
                    if (dataTable.Any(x => x.Get<int>("_KindID") == info.Id))
                        continue;

                    dataTable = dataTable.Add(
                        context.TypeRepository.Create("chainsaw.CostumePresetCatalogUserData.Data")
                            .Set("_KindID", info.Id)
                            .Set("_CostumePresetUserData", new RszUserDataNode(
                                context.TypeRepository.FromName("chainsaw.CostumePresetUserData")!,
                                $"{info.GetBasePath("Costume")}CostumePresetUserData.user")));
                }
                return root.Set("_DataTable", dataTable);
            });

            context.ModifyUserFile(attackPath, root =>
            {
                var dataTable = root.Get<RszArrayNode>("_DataTable");
                foreach (var info in infos)
                {
                    if (dataTable.Any(x => x.Get<int>("_AttackDataID") == info.AttackDataId))
                        continue;

                    dataTable = dataTable.Add(
                        context.TypeRepository.Create("chainsaw.collision.AttackDataCatalogUserData.Data")
                            .Set("_AttackDataID", info.AttackDataId)
                            .Set("_AttackHitUserData", new RszUserDataNode(
                                context.TypeRepository.FromName("chainsaw.collision.AttackHitUserData")!,
                                $"{info.GetBasePath("UserData")}AttackHitUserData.user")));
                }
                return root.Set("_DataTable", dataTable);
            });

            context.ModifyUserFile(rankPath, root =>
            {
                var dataTable = root.Get<RszArrayNode>("_DataTable");
                foreach (var info in infos)
                {
                    if (dataTable.Any(x => x.Get<int>("_EnemyID") == info.Id))
                        continue;

                    dataTable = dataTable.Add(
                        context.TypeRepository.Create("chainsaw.GameRankCatalogUserData.Data")
                            .Set("_EnemyID", info.AttackDataId)
                            .Set("_EnemyRankData", new RszUserDataNode(
                                context.TypeRepository.FromName("chainsaw.EnemyRankParamDataUserData")!,
                                $"{info.GetBasePath("UserData")}RankParamDataUserData.user")));
                }
                return root.Set("_DataTable", dataTable);
            });
        }

        private void AddWeaponToCatalog_6400(Campaign campaign)
        {
            int wpId = 6400;
            FixWeaponCatalog();
            FixWeaponEquipCatalog();

            void FixWeaponCatalog()
            {
                var path = campaign == Campaign.Leon ?
                    "natives/stm/_chainsaw/appsystem/weapon/weaponcataloguserdata_2nd.user.2" :
                    "natives/stm/_anotherorder/appsystem/weapon/weaponcataloguserdata_ao.user.2";
                context.ModifyUserFile(path, root =>
                {
                    var dataTable = root.Get<RszArrayNode>("_DataTable");
                    if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpId))
                        return root;

                    return root.Set("_DataTable", dataTable.Add(
                            context.TypeRepository.Create("chainsaw.WeaponCatalogUserData.Data")
                                .Set("_WeaponID", wpId)
                                .Set("_Prefab.Path", "_Mercenaries/AppSystem/Prefab/Weapon/wp6400_MC.pfb")));
                });
            }

            void FixWeaponEquipCatalog()
            {
                var sourcePath = "natives/stm/_mercenaries/appsystem/weapon/weaponequipparamcataloguserdata_mc_2nd.user.2";
                var sourceUserFile = context.GetUserFile(sourcePath);
                var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                var sourceDataTable = sourceRoot.Get<RszArrayNode>("_DataTable");
                var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpId);

                var path = campaign == Campaign.Leon ?
                    "natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2" :
                    "natives/stm/_anotherorder/appsystem/weapon/weaponequipparamcataloguserdata_ao.user.2";
                context.ModifyUserFile(path, root =>
                {
                    var dataTable = root.Get<RszArrayNode>("_DataTable");
                    if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpId))
                        return root;

                    return root.Set("_DataTable", dataTable.Add(sourceNode));
                });
            }
        }

        void FixMadChainsawKnifeAnimation()
        {
            context.ApplyOverlay(context.GetSupplementFile("madchainsaw_animationfix.zip")!);
        }

        private record EnemyCatalogInfo(string Name, Campaign Campaign, int Id, string Key, int AttackDataId)
        {
            public string BasePath => $"{CampaignBasePath}/AppSystem/Character/{Key}";

            public string GetBasePath(string directory = "")
            {
                return string.IsNullOrEmpty(directory) ? $"{BasePath}/{Key}" : $"{BasePath}/{directory}/{Key}";
            }

            private string CampaignBasePath => Campaign switch
            {
                Campaign.Leon => "_Chainsaw",
                Campaign.Ada => "_AnotherOrder",
                Campaign.Mercenaries => "_Mercenaries",
                _ => throw new NotSupportedException()
            };
        }
    }
}
