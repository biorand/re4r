using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class FlamethrowerPatch
    {
        private const int FlamethrowerWeaponId = 4701;
        private const int FlamethrowerItemId = 275957056;
        private const int FuelItemId = 112814400;
        private const int SmallResourceItemId = 117606400;
        private const int GunpowderItemId = 117600000;

        private readonly ChainsawRandomizer _randomizer;

        public FileRepository FileRepository => _randomizer.FileRepository;

        public FlamethrowerPatch(ChainsawRandomizer randomizer)
        {
            _randomizer = randomizer;
        }

        public void Apply()
        {
            var wpbase = WeaponBaseStats.Default;
            var wpflamethrower = wpbase.Weapons.First(x => x["id"].Equals(FlamethrowerWeaponId));

            SetStrings("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("4f8a97ce-e2b6-40e0-81fd-53c61916e3e1")] = "A Small Canister of pressurized gas.",
                [new Guid("669452fa-762e-407a-8702-c579593818ea")] = "Uses pressurized gas to produce intense flames.\nJust like that night in the jungle…"
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("df3c0301-fa85-453f-91fe-1881c87a5140")] = "Flamethrower",
                [new Guid("09d0ff48-9b95-4381-b854-53ad864a052b")] = "Fuel"
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("eded9293-c0ed-45f2-8224-bb9d3c677b72")] = "Increases Flame Range"
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("79ad9402-fb4b-42b0-8c98-5355812c931a")] = "Range"
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("db128948-0960-4147-814d-fec706a5c34a")] = "Penetration"
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("876c8ba0-3637-4aff-a065-86254207705d")] = "Experimental fuel. 1.5X damage and increased burn rate."
            });
            SetStrings("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22", new Dictionary<Guid, string>
            {
                [new Guid("e8236563-0f8f-4d96-b662-d808852b48a7")] = "Increase damage 1.5X.\nReduce time to burn"
            });

            ///////////////////////////////////////
            // Flamethrower
            ///////////////////////////////////////
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701shellinfo.user.2", root =>
            {
                return root
                    .Set("_LifeInfo._Distance", 5)
                    .Set("_LifeInfo._WaterSufaceHit", true)
                    .Set("_AttackInfo._ColliderRadius", 0.22);
            });
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701embershellinfo.user.2", root =>
            {
                return root
                    .Set("_LifeInfo._WaterSufaceHit", true)
                    .Set("_AttackInfo._ColliderRadius", 1.0)
                    .Set("_LifeInfo._Time", 7);
            });
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701shellgeneratoruserdata.user.2", root =>
            {
                return root
                    .Set("_FolderType", 8)
                    .Set("_FlameShellIntervalTime", 0.2);
            });

            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_gun_cm.user.2", 686504397, true, 0);
            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_4700.user.2", 3074067878, false, 2926154213);
            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_4700.user.2", 686504397, false, 2926154213);

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/bulletattackhituserdata.user.2", root =>
            {
                var hash4701 = (uint)MurMur3.HashData("wp4701");
                var hashFloorEmber = (uint)2204850508;
                var attackDataList = (RszArrayNode)root["_AttackDataList"];
                for (var i = 0; i < attackDataList.Length; i++)
                {
                    var attackData = attackDataList[i];
                    if (attackData.Get<uint>("_KeyNameHash") == hash4701)
                    {
                        attackData = attackData
                            .Set("_Damage", (int)wpflamethrower["damage"])
                            .Set("STRUCT__Break__Value", (int)wpflamethrower["break"])
                            .Set("STRUCT__Stopping__Value", (int)wpflamethrower["stopping"])
                            .Set("_AttackPower", (int)wpflamethrower["attackpower"])
                            .Set("_IsThroughRestriction", true)
                            .Set("_ThroughNum", (int)wpflamethrower["throughnum"])
                            .Set("_BreakLevel", (int)wpflamethrower["breaklevel"]);
                        attackDataList = attackDataList.SetItem(i, attackData);
                    }
                    else if (attackData.Get<uint>("_KeyNameHash") == hashFloorEmber)
                    {
                        attackData = attackData
                            .Set("_Damage", (int)wpflamethrower["damagecrit"])
                            .Set("STRUCT__Break__Value", (int)wpflamethrower["breakcrit"])
                            .Set("STRUCT__Stopping__Value", (int)wpflamethrower["stoppingcrit"]);
                        attackDataList = attackDataList.SetItem(i, attackData);
                    }
                }
                root = root.SetField("_AttackDataList", attackDataList);
                return root;
            });

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == FlamethrowerItemId)
                    {
                        data = data
                            .Set("_WeaponDefineData._AmmoMax", (int)wpflamethrower["baseammocapacity"]);
                        root = root.SetField("_Datas", datas.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });

            // Setting alternate vfx for flamethrower
            var targetGuidFloor = Guid.Parse("28a220f6-2a39-4815-bfbc-d186dd49482d");
            var targetGuidWall = Guid.Parse("b8253ee9-1bdf-4bce-ab9b-aac2d0f5c295");
            var resourcePathFloor = "_Chainsaw/VFX/EffectEditor/EFD_Weapon/EFD_wp5814/efd_0015_wp5814_firebomb_0012_low.efx";
            var resourcePathWall = "_Chainsaw/VFX/EffectEditor/EFD_Weapon/EFD_wp5814/efd_0015_wp5814_firebomb_0013.efx";

            FileRepository.ModifyPfbFile($"natives/stm/_chainsaw/vfx/provider/epv_weapon/epv_wp4701/epvs_0015_wp4701_fire_prg_0000.pfb.17", scene =>
            {
                var gameobject = scene.Children.OfType<RszGameObject>().First();
                var component = gameobject.FindComponent("via.effect.script.EPVStandardData")!;
                var mainVfxPrefabList = (RszArrayNode)component["Elements"];
                var elementsObject = component["Elements"];
                {
                    for (var i = 0; i < mainVfxPrefabList.Length; i++)
                    {
                        var mainVfxPrefabData = mainVfxPrefabList[i];
                        var guid = mainVfxPrefabData.Get<Guid>("GUID");
                        string? resourcePath = null;
                        if (guid == targetGuidFloor)
                            resourcePath = resourcePathFloor;
                        else if (guid == targetGuidWall)
                            resourcePath = resourcePathWall;
                        if (resourcePath != null)
                        {
                            var objectNode = (RszObjectNode)mainVfxPrefabData;
                            var resourcesObject = objectNode["Resources"];
                            if (resourcesObject is RszArrayNode resourceArray)
                            {
                                resourceArray = resourceArray.SetItem(0, new RszResourceNode(resourcePath));
                                mainVfxPrefabData = mainVfxPrefabData.Set("Resources", resourceArray);
                            }
                            mainVfxPrefabList = mainVfxPrefabList.SetItem(i, mainVfxPrefabData);
                        }
                    }
                }
                component = component.SetField("Elements", mainVfxPrefabList);
                gameobject = gameobject.AddOrUpdateComponent(component);
                scene = scene.UpdateGameObject(gameobject);
                return scene;
            });

            // Giving the flamethrower a proper model in attaché case
            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/attachecase/itemmodel/ac_itemmodel_wp4701.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectp2 = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectP3 = gameObjectp2.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP3.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("chainsaw.AcItemModelMeshController")!;
                var defaultEnablePartsList = (RszArrayNode)component["_DefaultEnableParts"];
                defaultEnablePartsList = defaultEnablePartsList.Add(1);
                defaultEnablePartsList = defaultEnablePartsList.Add(2);
                component = component.SetField("_DefaultEnableParts", defaultEnablePartsList);
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP3 = gameObjectP3.AddOrUpdateChild(gameObjectTarget);
                gameObjectp2 = gameObjectp2.AddOrUpdateChild(gameObjectP3);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectp2);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            ///////////////////////////////////////
            // Fuel
            ///////////////////////////////////////
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == FuelItemId)
                    {
                        data = data
                            .Set("_ItemDefineData._ItemSize", 6)
                            .Set("_ItemDefineData._StackMax", 1600);
                        root = root.SetField("_Datas", datas.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });

            // Updating item model for fuel in attaché case
            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/attachecase/itemmodel/ac_itemmodel_sm70_509.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectp2 = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectP3 = gameObjectp2.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP3.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("chainsaw.AcItemModelMeshController")!;
                var fuelAttacheCasePrefabList = (RszArrayNode)component["_Settings"];
                for (int i = 0; i < fuelAttacheCasePrefabList.Length; i++)
                {
                    var data = fuelAttacheCasePrefabList[i];
                    if (data.Get<int>("_Mode") == 0)
                    {
                        data = data
                            .Set("_Scale", new Vector3(0.2f, 0.2f, 0.2f))
                            .Set("_Offset", new Vector3(0f, -0.035f, 0f));
                        fuelAttacheCasePrefabList = fuelAttacheCasePrefabList.SetItem(i, data);
                    }
                }
                component = component.SetField("_Settings", fuelAttacheCasePrefabList);

                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP3 = gameObjectP3.AddOrUpdateChild(gameObjectTarget);
                gameObjectp2 = gameObjectp2.AddOrUpdateChild(gameObjectP3);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectp2);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/attachecase/itemmodel/ac_itemmodel_sm70_509.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectp2 = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectP3 = gameObjectp2.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP3.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("via.Transform")!;
                component = component
                            .Set("Rotation", new Quaternion(-0.035f, 0.70599997f, -0.035f, 0.70700002f));
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP3 = gameObjectP3.AddOrUpdateChild(gameObjectTarget);
                gameObjectp2 = gameObjectp2.AddOrUpdateChild(gameObjectP3);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectp2);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            ///////////////////////////////////////
            // Crafting for Fuel
            ///////////////////////////////////////
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/itemcraftsettinguserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_RecipeID") == 29)
                    {
                        data = data
                            .Set("_Category", 1)
                            .Set("_ResultSettings[0]._Result._ItemID", FuelItemId)
                            .Set("_ResultSettings[0]._Result._GeneratedNumMin", 300)
                            .Set("_ResultSettings[0]._Result._GeneratedNumMax", 300)
                            .Set("_ResultSettings[1]._Result._ItemID", FuelItemId)
                            .Set("_ResultSettings[1]._Result._GeneratedNumMin", 300)
                            .Set("_ResultSettings[1]._Result._GeneratedNumMax", 300)
                            .Set("_RequiredItems[0]._ItemID", 117606400)
                            .Set("_RequiredItems[0]._RequiredNum", 1)
                            .Set("_RequiredItems[1]._ItemID", 117600000)
                            .Set("_RequiredItems[1]._RequiredNum", 5);
                        root = root.SetField("_Datas", datas.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_craft.user.2", root =>
            {
                var settings = (RszArrayNode)root["_Settings"];
                settings = settings.Add(FileRepository.RszRepository
                    .Create("chainsaw.GuiResourceSetting_Craft")
                        .Set("_ItemId", FuelItemId)
                        .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/AttacheCase/ItemModel/CraftItemModel_sm70_509.pfb")));
                return root.SetField("_Settings", settings);
            });

            ///////////////////////////////////////
            // Shop
            ///////////////////////////////////////
            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                // Flamethrower
                datas = datas.Add(FileRepository.RszRepository
                    .Create("chainsaw.InGameShopItemSettingUserdata.Data")
                        .Set("_ItemId", FlamethrowerItemId)
                        .Set("_PriceSettings", new[] {
                            new
                            {
                                _Difficulty = 20,
                                _Price = new
                                {
                                    _PurchasePrice = (int)wpflamethrower["price"],
                                    _SellingPrice = (int)wpflamethrower["price"] * 0.5f,
                                }
                            }
                        })
                        .Set("_UnlockSetting",
                            new
                            {
                                _UnlockCondition = 2,
                                _UnlockFlag = default(Guid),
                                _UnlockTiming = 0,
                                _SpCondition = 1,
                            }
                        ));


                // Fuel
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == FuelItemId)
                    {
                        data = data
                            .Set("_PriceSettings[0]._Price._PurchasePrice", 40)
                            .Set("_PriceSettings[0]._Price._SellingPrice", 15);
                        datas = datas.SetItem(i, data);
                        break;
                    }
                }

                root = root.SetField("_Datas", datas);
                return root;
            });

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemmodelsettinguserdata_2nd.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                datas = datas.Add(FileRepository.RszRepository
                    .Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                        .Set("_ItemId", FlamethrowerItemId)
                        .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/InGameShop/ItemModel/ingameshop_itemmodel_wp4701_00.pfb")));
                datas = datas.Add(FileRepository.RszRepository
                    .Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                        .Set("_ItemId", FuelItemId)
                        .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/InGameShop/ItemModel/ingameshop_itemmodel_sm70_509_00.pfb")));
                root = root.SetField("_Datas", datas);
                return root;
            });


            //Creating new fuel shop param file based on template
            var templateFuelShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_500_00.pfb.17";
            var fuelShopParamData = FileRepository.GetGameFileData(templateFuelShopParamPath)!;
            FileRepository.SetGameFileData(templateFuelShopParamPath, fuelShopParamData);
            var fuelShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_509_00.pfb.17";
            FileRepository.SetGameFileData(fuelShopParamPath, fuelShopParamData);

            //Creating new fuel shop param file based on template
            var templateFlamethrowerShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4000_00.pfb.17";
            var flamethrowerShopParamData = FileRepository.GetGameFileData(templateFlamethrowerShopParamPath)!;
            FileRepository.SetGameFileData(templateFlamethrowerShopParamPath, flamethrowerShopParamData);
            var flamethrowerShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17";
            FileRepository.SetGameFileData(flamethrowerShopParamPath, flamethrowerShopParamData);

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_509_00.pfb.17", scene =>
            {
                var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                gameObjectTarget = gameObjectTarget.WithName("InGameShop_ItemModel_sm70_509_00");
                scene = scene.UpdateGameObject(gameObjectTarget);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_509_00.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("via.Transform")!;
                component = component
                    .Set("Position", new Vector3(0.3f, 0.05f, 0.24f))
                    .Set("Rotation", new Quaternion(-0.55f, 0.5f, -0.49f, 0.5f))
                    .Set("Scale", new Vector3(0.55f, 0.55f, 0.55f));
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_509_00.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("via.render.Mesh")!;
                component = component
                    .Set("Mesh", new RszResourceNode("_Chainsaw/Environment/sm/sm7X/sm70/sm70_509/sm70_509_00.mesh"))
                    .Set("Material", new RszResourceNode("_Chainsaw/Environment/sm/sm7X/sm70/sm70_509/sm70_509_00_Mat.mdf2"));
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17", scene =>
            {
                var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                gameObjectTarget = gameObjectTarget.WithName("InGameShop_ItemModel_wp4701_00");
                scene = scene.UpdateGameObject(gameObjectTarget);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("via.Transform")!;
                component = component
                    .Set("Position", new Vector3(0.1f, 0.1f, 0.2f))
                    .Set("Rotation", new Quaternion(-0.55f, 0.55f, -0.5f, 0.5f))
                    .Set("Scale", new Vector3(0.7f, 0.7f, 0.7f));
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("via.render.Mesh")!;
                component = component
                    .Set("Mesh", new RszResourceNode("_Chainsaw/Character/wp/wp47/wp4701/00/wp4701_00.mesh"))
                    .Set("Material", new RszResourceNode("_Chainsaw/Character/wp/wp47/wp4701/00/wp4701_00.mdf2"));
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });

            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17", scene =>
            {
                var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var gameObjectTarget = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                var component = gameObjectTarget.FindComponent("chainsaw.AcItemModelMeshController")!;
                var defaultEnablePartsList = (RszArrayNode)component["_DefaultEnableParts"];
                defaultEnablePartsList = defaultEnablePartsList.RemoveAt(1);
                defaultEnablePartsList = defaultEnablePartsList.Add(1);
                defaultEnablePartsList = defaultEnablePartsList.Add(2);
                component = component.SetField("_DefaultEnableParts", defaultEnablePartsList);
                gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectTarget);
                scene = scene.UpdateGameObject(gameObjectP1);
                return scene;
            });
        }

        private void SetStrings(string path, Dictionary<Guid, string> strings)
        {
            var msgFile = FileRepository.GetMsgFile(path).ToBuilder();
            foreach (var kvp in strings)
            {
                msgFile.SetStringAll(kvp.Key, kvp.Value);
            }
            FileRepository.SetMsgFile(path, msgFile.Build());
        }

        private void SetSoundTrgr(string path, uint triggerId, bool mute, uint eventId)
        {
            FileRepository.ModifyUserFile(path, root =>
            {
                var triggerInfoList = (RszArrayNode)root["_TriggerInfoList"];
                for (var i = 0; i < triggerInfoList.Length; i++)
                {
                    var data = triggerInfoList[i];
                    if (data.Get<uint>("_TriggerId") == triggerId)
                    {
                        data = data.Set("_Mute", mute);
                        data = data.Set("_EventId", eventId);
                        root = root.SetField("_TriggerInfoList", triggerInfoList.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });
        }
    }

    internal sealed class WeaponBaseStats
    {
        public static WeaponBaseStats Default { get; } = new WeaponBaseStats(EmbeddedData.GetFile("wpbase.csv"));

        public ImmutableArray<ImmutableDictionary<string, object>> Weapons { get; }

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
