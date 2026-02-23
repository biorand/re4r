using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using chainsaw;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [Order(10)]
    internal class AdditionalWeaponsPatch(IPatchContext context) : IPatch
    {
        private readonly WeaponBaseStats _baseStats = new(context.DynamicData);

        public void Apply()
        {
            var weapons = GetSelection();
            var weaponNames = weapons.Select(x => (string)x["name"]).ToArray();
            foreach (var wpName in weaponNames)
            {
                ApplyWeapon(wpName);
            }
        }

        private ImmutableArray<ImmutableDictionary<string, object>> GetSelection()
        {
            var randomizer = (context as FileRepository)?.Randomizer;
            if (randomizer == null)
                return [];

            var campaign = randomizer.Campaign == Campaign.Leon ?
                "leon" :
                "ada";

            var weapons = _baseStats.Weapons
                .Where(x => x["type"] as string == "additional")
                .Where(x =>
                {
                    var gamemode = x.GetValueOrDefault("gamemode") as string;
                    if (string.IsNullOrEmpty(gamemode))
                        return true;

                    if (campaign == "leon")
                        return gamemode.Contains("leon", StringComparison.OrdinalIgnoreCase);
                    else
                        return gamemode.Contains("ada", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(x => x["id"])
                .ToImmutableArray();

            return weapons;
        }

        private void ApplyWeapon(string name)
        {
            var randomizer = (context as FileRepository)?.Randomizer;
            var campaign = randomizer?.Campaign ?? Campaign.Leon;

            var info = _baseStats.Weapons.First(x => (string)x["name"] == name);
            var wpid = (int)info["id"];
            var itemId = (int)info["itemid"];

            CreateBulletAttackHit(campaign);
            UpdateBulletHitUserData();
            AddWeaponToCatalog(campaign);
            AddWeaponCustom(campaign);
            AddUIUserdata(campaign);
            FixInGameShop();
            FixCamera();
            FixItemMessages();
            AddShellData();

            void CreateBulletAttackHit(Campaign campaign)
            {
                var templateBulletHitUserData = "natives/stm/_mercenaries/appsystem/shell/bullet/bulletattackhituserdata_mc.user.2";
                var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/shell/bullet/bulletattackhituserdata.user.2" :
                        "natives/stm/_anotherorder/appsystem/shell/bullet/bulletattackhituserdata_ao.user.2";

                context.ModifyUserFile(path, root =>
                {
                    uint[] attackhashes;

                    if (wpid == 6102)
                    {
                        var boltHash = (uint)3679695750;
                        var directBombHash = (uint)3870021170;
                        var indirectBombHash = (uint)2453219919;
                        attackhashes = [boltHash, directBombHash, indirectBombHash];
                    }
                    else if (IsShotgunWeapon(wpid))
                    {
                        var baseHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center");
                        var critHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_c");
                        var baseFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_f");
                        var critFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_f_c");
                        var baseAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around");
                        var critAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_c");
                        var baseAroundFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f");
                        var critAroundFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f_c");
                        attackhashes = [baseHash, critHash, baseFitHash, critFitHash, baseAroundHash, critAroundHash, baseAroundFitHash, critAroundFitHash];
                    }
                    else
                    {
                        var baseHash = (uint)MurMur3.HashData($"wp{wpid:0000}");
                        var critHash = (uint)MurMur3.HashData($"wp{wpid:0000}_c");
                        var baseFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}_f");
                        var critFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}_f_c");
                        var baseAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around");
                        var critAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_c");
                        var baseAroundFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f");
                        var critAroundFitHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f_c");
                        attackhashes = [baseHash, critHash, baseFitHash, critFitHash, baseAroundHash, critAroundHash, baseAroundFitHash, critAroundFitHash];
                    }

                    var templateRoot = context.GetUserFile(templateBulletHitUserData);
                    var templateBuilder = templateRoot.ToBuilder(context.TypeRepository);
                    var templateAttackDataList = (RszArrayNode)templateBuilder.Objects[0]["_AttackDataList"];
                    var targetAttackDataList = (RszArrayNode)root["_AttackDataList"];

                    foreach (var hash in attackhashes)
                    {
                        var templateEntry = templateAttackDataList.FirstOrDefault(x => x.Get<uint>("_KeyNameHash") == hash);
                        if (templateEntry != null)
                        {

                            var existingEntry = targetAttackDataList.FirstOrDefault(x => x.Get<uint>("_KeyNameHash") == hash);
                            if (existingEntry == null)
                            {
                                targetAttackDataList = targetAttackDataList.Add(templateEntry);
                            }
                        }
                    }

                    return root.SetField("_AttackDataList", targetAttackDataList);
                });
            }

            void UpdateBulletHitUserData()
            {
                if (info["weight"] is not int weight || weight != 1)
                    return;

                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/bulletattackhituserdata.user.2", root =>
                {
                    uint baseHash;
                    uint critHash;
                    uint baseAttachHash;
                    uint critAttachHash;
                    if (IsShotgunWeapon(wpid))
                    {
                        baseHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center"); // Base Hash
                        critHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_c"); // Crit Hash
                        baseAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_f"); // Base Hash that has an attachment
                        critAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}Center_f_c"); // Crit Hash that has an attachment
                    }
                    else
                    {
                        baseHash = (uint)MurMur3.HashData($"wp{wpid:0000}"); // Base Hash
                        critHash = (uint)MurMur3.HashData($"wp{wpid:0000}_c"); // Crit Hash
                        baseAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}_f"); // Base Hash that has an attachment
                        critAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}_f_c"); // Crit Hash that has an attachment
                    }

                    var baseAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around"); // Base Hash
                    var critAroundHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_c"); // Crit Hash
                    var baseAroundAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f"); // Base Hash that has an attachment
                    var critAroundAttachHash = (uint)MurMur3.HashData($"wp{wpid:0000}Around_f_c"); // Crit Hash that has an attachment

                    var attackDataList = (RszArrayNode)root["_AttackDataList"];
                    for (var i = 0; i < attackDataList.Length; i++)
                    {
                        var attackData = attackDataList[i];
                        var keyNameHash = attackData.Get<uint>("_KeyNameHash");

                        if (keyNameHash == baseHash || keyNameHash == baseAttachHash)
                        {
                            if (info["damage"] is int damage)
                            {
                                attackData = attackData
                                    .Set("_Damage", damage)
                                    .Set("STRUCT__Break__Value", (int)info["break"])
                                    .Set("STRUCT__Stopping__Value", (int)info["stopping"])
                                    .Set("STRUCT__Wince__Value", (int)info["wince"])
                                    .Set("_AttackType", (int)info["attacktype"])
                                    .Set("_AttackPower", (int)info["attackpower"])
                                    .Set("_IsThroughRestriction", true)
                                    .Set("_ThroughNum", (int)info["throughnum"])
                                    .Set("_BreakLevel", (int)info["breaklevel"]);
                                attackDataList = attackDataList.SetItem(i, attackData);
                            }
                        }
                        else if (keyNameHash == critHash || keyNameHash == critAttachHash)
                        {
                            if (info["damagecrit"] is int damageCrit)
                            {
                                attackData = attackData
                                    .Set("_Damage", damageCrit)
                                    .Set("STRUCT__Break__Value", (int)info["breakcrit"])
                                    .Set("STRUCT__Stopping__Value", (int)info["stoppingcrit"])
                                    .Set("STRUCT__Wince__Value", (int)info["wincecrit"])
                                    .Set("_AttackType", (int)info["attacktypecrit"])
                                    .Set("_AttackPower", (int)info["attackpowercrit"])
                                    .Set("_IsThroughRestriction", true)
                                    .Set("_ThroughNum", (int)info["throughnumcrit"])
                                    .Set("_BreakLevel", (int)info["breaklevelcrit"]);
                                attackDataList = attackDataList.SetItem(i, attackData);
                            }
                        }

                        if (IsShotgunWeapon(wpid))
                        {
                            if (keyNameHash == baseAroundHash || keyNameHash == baseAroundAttachHash)
                            {
                                if (info["damagearound"] is int damageAround)
                                {
                                    attackData = attackData
                                        .Set("_Damage", damageAround)
                                        .Set("STRUCT__Break__Value", (int)info["breakaround"])
                                        .Set("STRUCT__Stopping__Value", (int)info["stoppingaround"])
                                        .Set("STRUCT__Wince__Value", (int)info["wincearound"])
                                        .Set("_AttackType", (int)info["attacktypearound"])
                                        .Set("_AttackPower", (int)info["attackpoweraround"])
                                        .Set("_IsThroughRestriction", true)
                                        .Set("_ThroughNum", (int)info["throughnumaround"])
                                        .Set("_BreakLevel", (int)info["breaklevelaround"]);
                                    attackDataList = attackDataList.SetItem(i, attackData);
                                }
                            }
                            else if (keyNameHash == critAroundHash || keyNameHash == critAroundAttachHash)
                            {
                                if (info["damagearoundcrit"] is int damageAroundCrit)
                                {
                                    attackData = attackData
                                        .Set("_Damage", damageAroundCrit)
                                        .Set("STRUCT__Break__Value", (int)info["breakaroundcrit"])
                                        .Set("STRUCT__Stopping__Value", (int)info["stoppingaroundcrit"])
                                        .Set("STRUCT__Wince__Value", (int)info["wincearoundcrit"])
                                        .Set("_AttackType", (int)info["attacktypearoundcrit"])
                                        .Set("_AttackPower", (int)info["attackpoweraroundcrit"])
                                        .Set("_IsThroughRestriction", true)
                                        .Set("_ThroughNum", (int)info["throughnumaroundcrit"])
                                        .Set("_BreakLevel", (int)info["breaklevelaroundcrit"]);
                                    attackDataList = attackDataList.SetItem(i, attackData);
                                }
                            }
                        }
                    }
                    return root.SetField("_AttackDataList", attackDataList);
                });
            }

            void AddWeaponToCatalog(Campaign campaign)
            {
                var wpid = (int)info["id"];
                FixWeaponCatalog();
                FixWeaponEquipCatalog();
                FixAnimation();

                void FixWeaponCatalog()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/weapon/weaponcataloguserdata_mc_2nd.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_DataTable");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpid);

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/weapon/weaponcataloguserdata_2nd.user.2" :
                        "natives/stm/_anotherorder/appsystem/weapon/weaponcataloguserdata_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_DataTable");
                        if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpid))
                            return root;

                        return root.Set("_DataTable", dataTable.Add(sourceNode));
                    });
                }

                void FixWeaponEquipCatalog()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/weapon/weaponequipparamcataloguserdata_mc_2nd.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_DataTable");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpid);

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2" :
                        "natives/stm/_anotherorder/appsystem/weapon/weaponequipparamcataloguserdata_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_DataTable");
                        if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpid))
                            return root;

                        return root.Set("_DataTable", dataTable.Add(sourceNode));
                    });


                }

                void FixAnimation()
                {
                    if (wpid != 6100 && wpid != 6102)
                        return;

                    context.ModifyPfbFile($"natives/stm/_mercenaries/appsystem/prefab/weapon/wp{wpid:0000}_mc.pfb.17", scene =>
                    {
                        var gameObject = scene.Children.OfType<RszGameObject>().FirstOrDefault();
                        if (gameObject == null)
                            return scene;

                        var component = gameObject.FindComponent("chainsaw.CharacterDynamicMotionBankAttacher");
                        if (component == null)
                            return scene;

                        component = component.Set("_Datas[0]._ID", 100000);
                        gameObject = gameObject.AddOrUpdateComponent(component);
                        scene = scene.UpdateGameObject(gameObject);
                        return scene;
                    });

                }
            }

            void AddWeaponCustom(Campaign campaign)
            {
                FixWeaponCustom();
                FixWeaponCustomDetail();

                void FixWeaponCustom()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/weaponcustom/weaponcustomuserdata_mc.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_WeaponStages");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpid);

                    if (wpid == 6102)
                    {
                        var weaponCustom = sourceNode.Get<RszObjectNode>("_WeaponCustom");
                        var limitBreak = (RszArrayNode)weaponCustom["_LimitBreak"];

                        for (var i = 0; i < limitBreak.Length; i++)
                        {
                            var lb = (RszObjectNode)limitBreak[i];
                            if (lb.Get<int>("_LimitBreakCustomCategory") == 10)
                            {
                                lb = lb.Set("_LimitBreakCustomCategory", 1);
                                limitBreak = limitBreak.SetItem(i, lb);
                            }
                        }

                        weaponCustom = weaponCustom.Set("_LimitBreak", limitBreak);
                        sourceNode = sourceNode.Set("_WeaponCustom", weaponCustom);
                    }

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2" :
                        "natives/stm/_anotherorder/appsystem/weaponcustom/weaponcustomuserdata_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_WeaponStages");
                        if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpid))
                            return root;

                        return root.Set("_WeaponStages", dataTable.Add(sourceNode));
                    });
                }

                void FixWeaponCustomDetail()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/weaponcustom/weapondetailcustomuserdata_mc.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_WeaponDetailStages");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpid);

                    if (wpid == 6102)
                    {
                        var weaponDetailCustom = sourceNode.Get<RszObjectNode>("_WeaponDetailCustom");
                        var limitBreakCustoms = (RszArrayNode)weaponDetailCustom["_LimitBreakCustoms"];

                        for (var i = 0; i < limitBreakCustoms.Length; i++)
                        {
                            var lbc = (RszObjectNode)limitBreakCustoms[i];
                            if (lbc.Get<int>("_LimitBreakCustomCategory") == 10)
                            {
                                lbc = lbc.Set("_LimitBreakCustomCategory", 1);
                                limitBreakCustoms = limitBreakCustoms.SetItem(i, lbc);
                            }
                        }

                        weaponDetailCustom = weaponDetailCustom.Set("_LimitBreakCustoms", limitBreakCustoms);
                        sourceNode = sourceNode.Set("_WeaponDetailCustom", weaponDetailCustom);
                    }

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2" :
                        "natives/stm/_anotherorder/appsystem/weaponcustom/weapondetailcustomuserdata_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_WeaponDetailStages");
                        if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpid))
                            return root;

                        return root.Set("_WeaponDetailStages", dataTable.Add(sourceNode));
                    });
                }

            }

            void AddUIUserdata(Campaign campaign)
            {
                FixItemDefinition();
                FixAttacheCase();
                FixInGameShop();
                FixInGameShopItemModelSettings();
                FixCraftingForBolts();


                void FixItemDefinition()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/ui/userdata/itemdefinitionuserdata_mc.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_Datas");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_ItemId") == itemId);

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2" :
                        "natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_Datas");
                        if (dataTable.Any(x => x.Get<int>("_ItemId") == itemId))
                            return root;
                        if (info["attache case size"] is int)
                        {
                            var modifiedNode = sourceNode
                            .Set("_WeaponDefineData._ItemSize", (int)info["attache case size"]);
                            return root.Set("_Datas", dataTable.Add(modifiedNode));
                        }
                        else
                        {
                            return root.Set("_Datas", dataTable.Add(sourceNode));
                        }
                    });


                    if (wpid == 6102)
                    {
                        var sourceNode2 = sourceDataTable.First(x => x.Get<int>("_ItemId") == 112480000);
                        context.ModifyUserFile(path, root =>
                        {
                            var dataTable = root.Get<RszArrayNode>("_Datas");
                            if (dataTable.Any(x => x.Get<int>("_ItemId") == 112480000))
                                return root;

                            var modifiedNode = sourceNode2
                            .Set("_ItemDefineData._StackMax", 10);
                            return root.Set("_Datas", dataTable.Add(modifiedNode));
                        });
                    }
                }


                void FixAttacheCase()
                {
                    var sourcePath = "natives/stm/_mercenaries/appsystem/ui/userdata/guiresourcesettinguserdata_attachecase_mc_2nd.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_Settings");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_ItemId") == itemId);

                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_attachecase.user.2" :
                        "natives/stm/_anotherorder/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_attachecase_ao.user.2";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_Settings");
                        if (dataTable.Any(x => x.Get<int>("_ItemId") == itemId))
                            return root;

                        return root.Set("_Settings", dataTable.Add(sourceNode));
                    });

                    if (wpid == 6102)
                    {
                        var sourceNode2 = sourceDataTable.First(x => x.Get<int>("_ItemId") == 112480000);
                        context.ModifyUserFile(path, root =>
                        {
                            var dataTable = root.Get<RszArrayNode>("_Settings");
                            if (dataTable.Any(x => x.Get<int>("_ItemId") == 112480000))
                                return root;
                            return root.Set("_Settings", dataTable.Add(sourceNode2));
                        });
                    }

                }

                void FixInGameShopItemModelSettings()
                {
                    var path = campaign == Campaign.Leon ?
                        "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemmodelsettinguserdata_1st.user.2" :
                        "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopitemmodelsettinguserdata_1st.user.2";
                    var shopPath = $"_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp{wpid:0000}_00.pfb";
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_Datas");
                        if (dataTable.Any(x => x.Get<int>("_ItemId") == itemId))
                            return root;

                        return root.Set("_Datas", dataTable.Add(
                                context.TypeRepository.Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                                    .Set("_ItemId", itemId)
                                    .Set("_Prefab.Path", shopPath)));
                    });

                    if (wpid == 6102)
                    {
                        var additionalShopPath = "_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_300_00.pfb";
                        context.ModifyUserFile(path, root =>
                        {
                            var dataTable = root.Get<RszArrayNode>("_Datas");
                            if (dataTable.Any(x => x.Get<int>("_ItemId") == 112480000))
                                return root;

                            return root.Set("_Datas", dataTable.Add(
                                    context.TypeRepository.Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                                        .Set("_ItemId", 112480000)
                                        .Set("_Prefab.Path", additionalShopPath)));
                        });
                    }
                }

                void FixCraftingForBolts()
                {
                    if (wpid != 6102)
                        return;

                    var sourcePath = "natives/stm/_mercenaries/appsystem/ui/userdata/guiresourcesettinguserdata_craft_cp12.user.2";
                    var sourceUserFile = context.GetUserFile(sourcePath);
                    var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                    var sourceDataTable = sourceRoot.Get<RszArrayNode>("_Settings");
                    var sourceNode = sourceDataTable.First(x => x.Get<int>("_ItemId") == 112480000);

                    context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_craft.user.2", root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_Settings");
                        if (dataTable.Any(x => x.Get<int>("_ItemId") == 112480000))
                            return root;

                        return root.Set("_Settings", dataTable.Add(sourceNode));
                    });
                }

            }

            void FixInGameShop()
            {
                CreateShopModelFile();
                ModifyShopModelFile();

                void CreateShopModelFile()
                {
                    var templateShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4000_00.pfb.17";
                    var newShopParamData = context.GetFile(templateShopParamPath)!;
                    context.SetFile(templateShopParamPath, newShopParamData);
                    var newShopParamPath = $"natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp{wpid:0000}_00.pfb.17";
                    context.SetFile(newShopParamPath, newShopParamData);

                    if (wpid == 6102)
                    {
                        var additionalShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_300_00.pfb.17";
                        context.SetFile(additionalShopParamPath, newShopParamData);
                    }
                }

                void ModifyShopModelFile()
                {
                    var transform = WeaponTransforms.GetValueOrDefault(wpid);

                    context.ModifyPfbFile($"natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp{wpid:0000}_00.pfb.17", scene =>
                    {
                        var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                        gameObjectTarget = gameObjectTarget.WithName($"InGameShop_ItemModel_wp{wpid:0000}_00");
                        scene = scene.UpdateGameObject(gameObjectTarget);
                        return scene;
                    });

                    context.ModifyPfbFile($"natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp{wpid:0000}_00.pfb.17", scene =>
                    {
                        var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                        var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                        var component = gameObjectTarget.FindComponent("via.Transform")!;
                        component = component
                        .Set("Position", transform.Position)
                        .Set("Rotation", transform.Rotation)
                        .Set("Scale", transform.Scale);
                        gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                        gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                        scene = scene.UpdateGameObject(gameObjectP1);
                        return scene;
                    });

                    context.ModifyPfbFile($"natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp{wpid:0000}_00.pfb.17", scene =>
                    {
                        var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                        var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                        var component = gameObjectTarget.FindComponent("via.render.Mesh")!;
                        var (meshPath, materialPath) = WeaponResourcePaths.GetResourcePaths(wpid);
                        component = component
                            .Set("Mesh", new RszResourceNode(meshPath))
                            .Set("Material", new RszResourceNode(materialPath));
                        gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                        gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                        scene = scene.UpdateGameObject(gameObjectP1);
                        return scene;
                    });

                    if (wpid == 6102)
                    {
                        context.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_300_00.pfb.17", scene =>
                        {
                            var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                            gameObjectTarget = gameObjectTarget.WithName("InGameShop_ItemModel_sm70_300_00");
                            scene = scene.UpdateGameObject(gameObjectTarget);
                            return scene;
                        });

                        context.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_300_00.pfb.17", scene =>
                        {
                            var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                            var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                            var component = gameObjectTarget.FindComponent("via.Transform")!;
                            component = component
                            .Set("Position", new Vector3(0.1f, 0f, 0.2f))
                            .Set("Scale", new Vector3(1.0f, 1.0f, 1.0f));
                            gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                            gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                            scene = scene.UpdateGameObject(gameObjectP1);
                            return scene;
                        });

                        context.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_300_00.pfb.17", scene =>
                        {
                            var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                            var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                            var component = gameObjectTarget.FindComponent("via.render.Mesh")!;
                            component = component
                                .Set("Mesh", new RszResourceNode("_chainsaw/environment/sm/sm7x/sm70/sm70_300/sm70_300_00.mesh"))
                                .Set("Material", new RszResourceNode("_chainsaw/environment/sm/sm7x/sm70/sm70_300/sm70_300_00_mat.mdf2"));
                            gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                            gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                            scene = scene.UpdateGameObject(gameObjectP1);
                            return scene;
                        });
                    }
                }
            }

            void FixCamera()
            {

                var sourcePath = "natives/stm/_mercenaries/appsystem/character/ch6common/userdata/ch6commonweaponholdstanceadjusterunituserdata.user.2";
                var sourceUserFile = context.GetUserFile(sourcePath);
                var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                var sourceDataTable = sourceRoot.Get<RszArrayNode>("_WeaponSettings");
                var sourceNode = sourceDataTable.First(x => x.Get<int>("_WeaponID") == wpid);

                var path = "natives/stm/_chainsaw/appsystem/character/userdata/commonweaponholdstanceadjusterunituserdata.user.2";
                context.ModifyUserFile(path, root =>
                {
                    var dataTable = root.Get<RszArrayNode>("_WeaponSettings");
                    if (dataTable.Any(x => x.Get<int>("_WeaponID") == wpid))
                        return root;

                    return root.Set("_WeaponSettings", dataTable.Add(sourceNode));
                });

            }

            void FixItemMessages()
            {
                var sourcePath = "natives/stm/_mercenaries/appsystem/ui/userdata/itemmessageidsettinguserdata_mc_2nd.user.2";
                var sourceUserFile = context.GetUserFile(sourcePath);
                var sourceRoot = sourceUserFile.GetObjects(context.TypeRepository)[0];
                var sourceDataTable = sourceRoot.Get<RszArrayNode>("_Settings");
                var sourceNode = sourceDataTable.First(x => x.Get<int>("_ItemId") == itemId);

                var path = "natives/stm/_chainsaw/appsystem/ui/userdata/itemmessageidsettinguserdata.user.2";
                context.ModifyUserFile(path, root =>
                {
                    var dataTable = root.Get<RszArrayNode>("_Settings");
                    if (dataTable.Any(x => x.Get<int>("_ItemId") == itemId))
                        return root;

                    return root.Set("_Settings", dataTable.Add(sourceNode));
                });

                if (wpid == 6102)
                {
                    var sourceNode2 = sourceDataTable.First(x => x.Get<int>("_ItemId") == 112480000);
                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_Settings");
                        if (dataTable.Any(x => x.Get<int>("_ItemId") == 112480000))
                            return root;

                        return root.Set("_Settings", dataTable.Add(sourceNode2));
                    });

                    var fileRepository = (context as FileRepository)!;
                    var itemMessagePath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemmessageidsettinguserdata.user.2";
                    var itemCaptionPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22";
                    var itemNamePath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22";

                    var itemMessage = fileRepository.DeserializeUserFile<chainsaw.ItemMessageIdSettingUserdata>(itemMessagePath);
                    var itemCaption = fileRepository.GetMsgFile(itemCaptionPath).ToBuilder();
                    var itemName = fileRepository.GetMsgFile(itemNamePath).ToBuilder();

                    var sm70300 = itemMessage._Settings.FirstOrDefault(x => x._ItemId == 112480000);
                    if (sm70300 != null)
                    {
                        sm70300._NameMsgId = itemName.Create("Explosive Arrows").Guid;
                        sm70300._CaptionMsgId = itemCaption.Create("Explosive arrows that have a large blast radius.\r\nFitted with handmade explosives,\r\ntheir destructive power is tremendous.").Guid;
                    }

                    fileRepository.SerializeUserFile(itemMessagePath, itemMessage);
                    fileRepository.SetMsgFile(itemCaptionPath, itemCaption.Build());
                    fileRepository.SetMsgFile(itemNamePath, itemName.Build());
                }
            }

            void AddShellData()
            {
                CreateShellGeneratorData();
                CreateShellInfoData();
                UpdateWeaponPfbShellGenerator();
                if (wpid == 6102)
                {
                    AddShellToCatalogue();
                }

                void CreateShellGeneratorData()
                {
                    string templateWeaponId;
                    string templateShellGeneratorDataPath;

                    if (wpid == 6102)
                    {
                        templateWeaponId = "6102";
                        templateShellGeneratorDataPath = $"natives/stm/_mercenaries/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellgeneratoruserdata.user.2";
                    }
                    else if (IsShotgunWeapon(wpid))
                    {
                        templateWeaponId = "4100";
                        templateShellGeneratorDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellgeneratoruserdata.user.2";
                    }
                    else
                    {
                        templateWeaponId = "4000";
                        templateShellGeneratorDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellgeneratoruserdata.user.2";
                    }

                    var newShellDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellgeneratoruserdata.user.2";
                    var centerShellInfoPath = $"_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo_center.user";
                    var aroundShellInfoPath = $"_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo_around.user";
                    var shellInfoPath = $"_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo.user";

                    var templateUserFile = context.GetUserFile(templateShellGeneratorDataPath);
                    var builder = templateUserFile.ToBuilder(context.TypeRepository);
                    var root = builder.Objects[0];

                    if (IsShotgunWeapon(wpid))
                    {
                        root = root
                            .SetField("_CenterShellInfoUserData", new RszUserDataNode(
                             context.TypeRepository.FromName("chainsaw.ShotgunShellGeneratorUserData")!,
                             centerShellInfoPath));
                        root = root
                            .SetField("_AroundShellInfoUserData", new RszUserDataNode(
                             context.TypeRepository.FromName("chainsaw.ShotgunShellGeneratorUserData")!,
                             aroundShellInfoPath));
                    }
                    else
                    {
                        root = root
                            .SetField("_ShellInfoUserData", new RszUserDataNode(
                             context.TypeRepository.FromName("chainsaw.BulletShellGeneratorUserData")!,
                             shellInfoPath));
                    }

                    builder.Objects = [root];
                    var newUserFile = builder.Build();
                    context.SetUserFile(newShellDataPath, newUserFile);
                }

                void CreateShellInfoData()
                {
                    var templateWeaponId = IsShotgunWeapon(wpid) ? "4100" : "4000";

                    if (IsShotgunWeapon(wpid))
                    {
                        // Create center shell info
                        var templateCenterShellInfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellinfo_center.user.2";
                        var newCenterShellinfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo_center.user.2";

                        var templateCenterUserFile = context.GetUserFile(templateCenterShellInfoDataPath);
                        var centerBuilder = templateCenterUserFile.ToBuilder(context.TypeRepository);
                        var centerRoot = centerBuilder.Objects[0];

                        centerBuilder.Objects = [centerRoot];
                        var newCenterUserFile = centerBuilder.Build();
                        context.SetUserFile(newCenterShellinfoDataPath, newCenterUserFile);

                        // Create around shell info
                        var templateAroundShellInfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellinfo_around.user.2";
                        var newAroundShellinfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo_around.user.2";

                        var templateAroundUserFile = context.GetUserFile(templateAroundShellInfoDataPath);
                        var aroundBuilder = templateAroundUserFile.ToBuilder(context.TypeRepository);
                        var aroundRoot = aroundBuilder.Objects[0];

                        aroundBuilder.Objects = [aroundRoot];
                        var newAroundUserFile = aroundBuilder.Build();
                        context.SetUserFile(newAroundShellinfoDataPath, newAroundUserFile);
                    }
                    else if (wpid != 6102 && !IsShotgunWeapon(wpid))
                    {
                        var templateShellInfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{templateWeaponId}/wp{templateWeaponId}shellinfo.user.2";
                        var newShellinfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellinfo.user.2";

                        var templateUserFile = context.GetUserFile(templateShellInfoDataPath);
                        var builder = templateUserFile.ToBuilder(context.TypeRepository);
                        var root = builder.Objects[0];

                        builder.Objects = [root];
                        var newUserFile = builder.Build();
                        context.SetUserFile(newShellinfoDataPath, newUserFile);
                    }
                    else
                    {
                        var templateShellInfoDataPath = $"natives/stm/_mercenaries/appsystem/Shell/bullet/wp6102/wp6102shellinfo.user.2";
                        var newShellinfoDataPath = $"natives/stm/_chainsaw/appsystem/Shell/bullet/wp6102/wp6102shellinfo.user.2";

                        var templateUserFile = context.GetUserFile(templateShellInfoDataPath);
                        var builder = templateUserFile.ToBuilder(context.TypeRepository);
                        var root = builder.Objects[0];

                        builder.Objects = [root];
                        var newUserFile = builder.Build();
                        context.SetUserFile(newShellinfoDataPath, newUserFile);
                    }


                }

                void UpdateWeaponPfbShellGenerator()
                {
                    var weaponPfbPath = $"natives/stm/_mercenaries/appsystem/prefab/weapon/wp{wpid:0000}_mc.pfb.17";
                    var shellGeneratorPath = $"_chainsaw/appsystem/Shell/bullet/wp{wpid:0000}/wp{wpid:0000}shellgeneratoruserdata.user";

                    context.ModifyPfbFile(weaponPfbPath, scene =>
                    {
                        var gameObject = scene.Children.OfType<RszGameObject>().FirstOrDefault();
                        if (gameObject == null)
                            return scene;

                        var componentType = wpid == 6102 ? "chainsaw.ArrowShellWp6102Generator" : "chainsaw.BulletShellGenerator";
                        var component = gameObject.FindComponent(componentType);
                        if (component == null)
                            return scene;

                        component = component.Set("_UserData", shellGeneratorPath);
                        gameObject = gameObject.AddOrUpdateComponent(component);
                        scene = scene.UpdateGameObject(gameObject);
                        return scene;
                    });
                }

                void AddShellToCatalogue()
                {
                    var templatePath = "natives/stm/_mercenaries/appsystem/shell/shellcataloguserdata_mc.user.2";
                    var templateUserFile = context.GetUserFile(templatePath);
                    var templateRoot = templateUserFile.GetObjects(context.TypeRepository)[0];
                    var templateDataTable = templateRoot.Get<RszArrayNode>("_DataTable");
                    var templateNode = templateDataTable.First(x => x.Get<int>("_ShellType") == 84);

                    var path = "natives/stm/_chainsaw/appsystem/shell/shellcataloguserdata_2nd.user.2";

                    context.ModifyUserFile(path, root =>
                    {
                        var dataTable = root.Get<RszArrayNode>("_DataTable");
                        if (dataTable.Any(x => x.Get<int>("_ShellType") == 84))
                            return root;

                        return root.Set("_DataTable", dataTable.Add(templateNode));
                    });
                }

            }

        }


        private static class WeaponResourcePaths
        {
            private readonly record struct WeaponResources(string MeshPath, string MaterialPath);

            private static readonly Dictionary<int, WeaponResources> Resources = new()
            {
                [6100] = new($"_mercenaries/Character/wp/wp61/wp6100/00/wp6100_00.mesh", $"_mercenaries/Character/wp/wp61/wp6100/00/wp6100_00.mdf2"),
                [6300] = new($"_mercenaries/Character/wp/wp61/wp6110/00/wp6110_00.mesh", $"_mercenaries/Character/wp/wp61/wp6110/00/wp6110_00.mdf2")
            };

            public static (string MeshPath, string MaterialPath) GetResourcePaths(int weaponId)
            {
                if (Resources.TryGetValue(weaponId, out var resources))
                {
                    return (resources.MeshPath, resources.MaterialPath);
                }

                // Default fallback pattern
                return ($"_mercenaries/Character/wp/wp{weaponId / 100}/wp{weaponId}/00/wp{weaponId}_00.mesh",
                        $"_mercenaries/Character/wp/wp{weaponId / 100}/wp{weaponId}/00/wp{weaponId}_00.mdf2");
            }
        }

        private readonly record struct WeaponTransform(Vector3 Position, Quaternion Rotation, Vector3 Scale);
        private static readonly Dictionary<int, WeaponTransform> WeaponTransforms = new()
        {
            [6100] = new(new Vector3(0.1f, 0f, 0.2f), new Quaternion(-0.52f, 0.542f, -0.457f, 0.476f), new Vector3(0.7f, 0.7f, 0.7f)),
            [6102] = new(new Vector3(0.1f, 0f, 0.2f), new Quaternion(-0.52f, 0.542f, -0.457f, 0.476f), new Vector3(0.7f, 0.7f, 0.7f)),
            [6300] = new(new Vector3(0.1f, 0f, 0.2f), new Quaternion(-0.52f, 0.542f, -0.457f, 0.476f), new Vector3(1f, 1f, 1f))
        };

        private static bool IsShotgunWeapon(int weaponId) => weaponId is 4100 or 4101 or 4102 or 6001 or 6100;
    }

    internal sealed class WeaponBaseStats
    {
        public ImmutableArray<ImmutableDictionary<string, object>> Weapons { get; }

        public WeaponBaseStats(DynamicData dynamicData)
            : this(dynamicData.GetData(DynamicDataName.WeaponBase)!)
        {
        }

        private WeaponBaseStats(byte[] wpbase)
        {
            var content = Encoding.UTF8.GetString(wpbase);
            var cells = Csv.Read(content);
            var weapons = ImmutableArray.CreateBuilder<ImmutableDictionary<string, object>>();
            for (var x = 2; x < cells.GetLength(0); x++)
            {
                var dict = ImmutableDictionary.CreateBuilder<string, object>();
                for (var y = 0; y < cells.GetLength(1); y++)
                {
                    var key0 = cells[0, y];
                    var key1 = cells[1, y];
                    var key = string.IsNullOrEmpty(key1) ? key0 : $"{key0} {key1}";
                    var value = DeserializeValue(cells[x, y]);
                    dict[key] = value;
                }
                weapons.Add(dict.ToImmutable());
            }
            Weapons = weapons.ToImmutable();
        }

        private static object DeserializeValue(string value)
        {
            if (int.TryParse(value, NumberStyles.AllowThousands, null, out var i))
            {
                return i;
            }
            else if (float.TryParse(value, NumberStyles.AllowDecimalPoint, null, out var f))
            {
                return f;
            }
            return value;
        }
    }
}