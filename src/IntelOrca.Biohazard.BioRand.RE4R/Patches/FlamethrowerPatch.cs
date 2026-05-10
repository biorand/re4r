using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    /// <summary>
    /// Adds the flamethrower with stock values and crafting.
    /// </summary>
    /// <remarks>
    /// Design and implementation by MightyKusKus.
    /// </remarks>
    [ExportMod(
        FileName = "flamethrower",
        Name = "Flamethrower",
        Description = "Adds the flamethrower to the merchant's shop.",
        Version = "1.0",
        Author = "MightKusKus")]
    internal class FlamethrowerPatch : IPatch
    {
        private const int FlamethrowerWeaponId = 4701;
        private const int FlamethrowerItemId = 275957056;
        private const int FuelItemId = 112814400;
        private const int SmallResourceItemId = 117606400;
        private const int GunpowderItemId = 117600000;

        private readonly ImmutableDictionary<string, object> _wpflamethrower;
        private readonly Campaign _campaign;

        public IReeRandomizerContext FileRepository { get; }

        public FlamethrowerPatch(IReeRandomizerContext context)
        {
            FileRepository = context;
            _wpflamethrower = new WeaponBaseStats(context.GetDynamicData()).Weapons.First(x => x["id"].Equals(FlamethrowerWeaponId));

            // Determine campaign from config
            var campaignConfig = context.GetConfigOption("campaign", "");
            _campaign = campaignConfig == "Separate Ways" ? Campaign.Ada : Campaign.Leon;
        }

        public void Apply()
        {
            AddMedia();
            UpdateStrings();
            AddFlamethrower();
            AddFuel();
            UpdateCrafting();
            UpdateShop();
            UpdateWeaponData();
            UpdateCharacters();
            FixSalazarCrash();
        }

        private string GetPath(string leonPath, string? adaPath = null)
        {
            if (_campaign == Campaign.Ada && adaPath != null)
                return adaPath;
            return leonPath;
        }

        private void AddMedia()
        {
            FileRepository.ApplyOverlay(FileRepository.GetSupplementFile("flamethrower.zip")!);
        }

        private void UpdateStrings()
        {
            var msgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22";

            SetStrings(msgPath, new Dictionary<Guid, string>
            {
                [new Guid("4f8a97ce-e2b6-40e0-81fd-53c61916e3e1")] = "A Small Canister of pressurized gas.",
                [new Guid("669452fa-762e-407a-8702-c579593818ea")] = "Uses pressurized gas to produce intense flames.\nJust like that night in the jungle…"
            });

            msgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22";

            SetStrings(msgPath, new Dictionary<Guid, string>
            {
                [new Guid("df3c0301-fa85-453f-91fe-1881c87a5140")] = "Flamethrower",
                [new Guid("09d0ff48-9b95-4381-b854-53ad864a052b")] = "Fuel"
            });

            msgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22";

            SetStrings(msgPath, new Dictionary<Guid, string>
            {
                [new Guid("eded9293-c0ed-45f2-8224-bb9d3c677b72")] = "Increases Flame Range",
                [new Guid("876c8ba0-3637-4aff-a065-86254207705d")] = "Experimental fuel. 1.5X damage and increased burn rate.",
                [new Guid("e8236563-0f8f-4d96-b662-d808852b48a7")] = "Increase damage 1.5X.\nReduce time to burn"
            });

            msgPath = "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22";

            SetStrings(msgPath, new Dictionary<Guid, string>
            {
                [new Guid("79ad9402-fb4b-42b0-8c98-5355812c931a")] = "Range",
                [new Guid("db128948-0960-4147-814d-fec706a5c34a")] = "Penetration"
            });
        }

        private void AddFlamethrower()
        {
            var flamethrowerFpsBalance = FileRepository.GetConfigOption("flamethrower-fps-balance", false);
            var intervalMultiplier = flamethrowerFpsBalance ? 0.80f : 1.0f;

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701shellinfo.user.2", root =>
            {
                return root
                    .Set("_LifeInfo._Distance", 5)
                    .Set("_LifeInfo._WaterSufaceHit", true)
                    .Set("_AttackInfo._ColliderRadius", 0.15);
            });

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701embershellinfo.user.2", root =>
            {
                return root
                    .Set("_LifeInfo._WaterSufaceHit", true)
                    .Set("_AttackInfo._ColliderRadius", 1.0)
                    .Set("_LifeInfo._Time", 3);
            });

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/wp4701/wp4701shellgeneratoruserdata.user.2", root =>
            {
                return root
                    .Set("_FolderType", 8)
                    .Set("_FlameShellIntervalTime", (float)_wpflamethrower["flameinterval"] * intervalMultiplier);
            });

            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_gun_cm.user.2", 686504397, true, 0);
            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_4700.user.2", 3074067878, false, 2926154213);
            SetSoundTrgr("natives/stm/_chainsaw/sound/resource/trigger/weapon/snd_trgr_wp_4700.user.2", 686504397, false, 2926154213);

            var bullethitPath = GetPath(
            "natives/stm/_chainsaw/appsystem/shell/bullet/bulletattackhituserdata.user.2",
            "natives/stm/_anotherorder/appsystem/shell/bullet/bulletattackhituserdata_ao.user.2");

            FileRepository.ModifyUserFile(bullethitPath, root =>
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
                            .Set("_Damage", (int)_wpflamethrower["damage"])
                            .Set("STRUCT__Break__Value", (int)_wpflamethrower["break"])
                            .Set("STRUCT__Stopping__Value", (int)_wpflamethrower["stopping"])
                            .Set("_AttackPower", (int)_wpflamethrower["attackpower"])
                            .Set("_IsThroughRestriction", true)
                            .Set("_ThroughNum", (int)_wpflamethrower["throughnum"])
                            .Set("_BreakLevel", (int)_wpflamethrower["breaklevel"]);
                        attackDataList = attackDataList.SetItem(i, attackData);
                    }
                    else if (attackData.Get<uint>("_KeyNameHash") == hashFloorEmber)
                    {
                        attackData = attackData
                            .Set("_Damage", (int)_wpflamethrower["damagecrit"])
                            .Set("STRUCT__Break__Value", (int)_wpflamethrower["breakcrit"])
                            .Set("STRUCT__Stopping__Value", (int)_wpflamethrower["stoppingcrit"]);
                        attackDataList = attackDataList.SetItem(i, attackData);
                    }
                }
                root = root.SetField("_AttackDataList", attackDataList);
                return root;
            });

            var itemDefPath = GetPath(
                "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2",
                "natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ovr_ao.user.2");

            FileRepository.ModifyUserFile(itemDefPath, root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == FlamethrowerItemId)
                    {
                        data = data
                            .Set("_WeaponDefineData._AmmoMax", (int)_wpflamethrower["baseammocapacity"]);
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
        }

        private void AddFuel()
        {
            var itemDefPath = GetPath(
                "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2",
                "natives/stm/_anotherorder/appsystem/ui/userdata/itemdefinitionuserdata_ao.user.2");

            FileRepository.ModifyUserFile(itemDefPath, root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == FuelItemId)
                    {
                        data = data
                            .Set("_ItemDefineData._ItemSize", 6)
                            .Set("_ItemDefineData._StackMax", 1000);
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
        }

        private void UpdateCrafting()
        {
            var craftPath = GetPath(
                "natives/stm/_chainsaw/appsystem/ui/userdata/itemcraftsettinguserdata.user.2",
                "natives/stm/_anotherorder/appsystem/ui/userdata/itemcraftsettinguserdata_ao.user.2");

            FileRepository.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_craft.user.2", root =>
             {
                 var settings = (RszArrayNode)root["_Settings"];
                 settings = settings.Add(FileRepository.TypeRepository
                     .Create("chainsaw.GuiResourceSetting_Craft")
                         .Set("_ItemId", FuelItemId)
                         .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/AttacheCase/ItemModel/CraftItemModel_sm70_509.pfb")));
                 return root.SetField("_Settings", settings);
             });
        }

        private void UpdateShop()
        {
            // Add flamethrower to weapon shop category
            var categoryPath = GetPath(
                "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshoppurchasecategorysettinguserdata.user.2",
                "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshoppurchasecategorysettinguserdata_cp11.user.2");

            FileRepository.ModifyUserFile(categoryPath, root =>
            {
                var userdata = RszSerializer.Deserialize<chainsaw.InGameShopPurchaseCategorySettingUserdata>(root)!;
                var category1 = userdata._Settings.First(x => x._Category == 1);
                category1._Datas.Add(new chainsaw.InGameShopPurchaseCategorySingleSetting.Data()
                {
                    _ItemId = FlamethrowerItemId,
                    _SortPriority = category1._Datas.Max(x => x._SortPriority) + 10
                });
                return (RszObjectNode)RszSerializer.Serialize(root.Type, userdata);
            });

            // Add flamethrower and fuel to shop
            var itemPath = GetPath(
                "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2",
                "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopitemsettinguserdata_ao.user.2");

            FileRepository.ModifyUserFile(itemPath, root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                // Flamethrower
                datas = datas.Add(FileRepository.TypeRepository
                    .Create("chainsaw.InGameShopItemSettingUserdata.Data")
                        .Set("_ItemId", FlamethrowerItemId)
                        .Set("_PriceSettings", new[] {
                            new
                            {
                                _Difficulty = 20,
                                _Price = new
                                {
                                    _PurchasePrice = (int)_wpflamethrower["price"],
                                    _SellingPrice = (int)_wpflamethrower["price"] * 0.5f,
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
                            .Set("_PriceSettings[0]._Price._PurchasePrice", 20)
                            .Set("_PriceSettings[0]._Price._SellingPrice", 10);
                        datas = datas.SetItem(i, data);
                        break;
                    }
                }

                root = root.SetField("_Datas", datas);
                return root;
            });

            var shopModelPath = GetPath(
            "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemmodelsettinguserdata_2nd.user.2",
            "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopitemmodelsettinguserdata_1st_ovr.user.2");

            FileRepository.ModifyUserFile(shopModelPath, root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                datas = datas.Add(FileRepository.TypeRepository
                    .Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                        .Set("_ItemId", FlamethrowerItemId)
                        .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/InGameShop/ItemModel/ingameshop_itemmodel_wp4701_00.pfb")));
                datas = datas.Add(FileRepository.TypeRepository
                    .Create("chainsaw.InGameShopItemModelSettingUserData.Data")
                        .Set("_ItemId", FuelItemId)
                        .Set("_Prefab.Path", new RszResourceNode("_Chainsaw/AppSystem/Prefab/Gui/InGameShop/ItemModel/ingameshop_itemmodel_sm70_509_00.pfb")));
                root = root.SetField("_Datas", datas);
                return root;
            });


            // Creating new fuel shop param file based on template
            var templateFuelShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_500_00.pfb.17";
            var fuelShopParamData = FileRepository.GetFile(templateFuelShopParamPath)!;
            FileRepository.SetFile(templateFuelShopParamPath, fuelShopParamData);
            var fuelShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_sm70_509_00.pfb.17";
            FileRepository.SetFile(fuelShopParamPath, fuelShopParamData);

            // Creating new fuel shop param file based on template
            var templateFlamethrowerShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4000_00.pfb.17";
            var flamethrowerShopParamData = FileRepository.GetFile(templateFlamethrowerShopParamPath)!;
            FileRepository.SetFile(templateFlamethrowerShopParamPath, flamethrowerShopParamData);
            var flamethrowerShopParamPath = "natives/stm/_chainsaw/appsystem/prefab/gui/ingameshop/itemmodel/ingameshop_itemmodel_wp4701_00.pfb.17";
            FileRepository.SetFile(flamethrowerShopParamPath, flamethrowerShopParamData);

            FileRepository.ModifyPfbFile(fuelShopParamPath, scene =>
            {
                var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                gameObjectTarget = gameObjectTarget.WithName("InGameShop_ItemModel_sm70_509_00");
                scene = scene.UpdateGameObject(gameObjectTarget);
                return scene;
            });

            FileRepository.ModifyPfbFile(fuelShopParamPath, scene =>
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

            FileRepository.ModifyPfbFile(fuelShopParamPath, scene =>
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

            FileRepository.ModifyPfbFile(flamethrowerShopParamPath, scene =>
            {
                var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                gameObjectTarget = gameObjectTarget.WithName("InGameShop_ItemModel_wp4701_00");
                scene = scene.UpdateGameObject(gameObjectTarget);
                return scene;
            });

            FileRepository.ModifyPfbFile(flamethrowerShopParamPath, scene =>
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

            FileRepository.ModifyPfbFile(flamethrowerShopParamPath, scene =>
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

            FileRepository.ModifyPfbFile(flamethrowerShopParamPath, scene =>
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

        private void UpdateWeaponData()
        {
            if (_campaign == Campaign.Ada)
            {
                // Template from Leon's campaign file
                var leonCatalogPath = "natives/stm/_chainsaw/appsystem/weapon/weaponcataloguserdata_2nd.user.2";
                RszObjectNode? templateEntry = null;

                FileRepository.ModifyUserFile(leonCatalogPath, root =>
                {
                    var dataTable = (RszArrayNode)root["_DataTable"];
                    templateEntry = (RszObjectNode?)dataTable.FirstOrDefault(x => x.Get<int>("_WeaponID") == FlamethrowerWeaponId);
                    return root;
                });

                // Apply to Ada's campaign file
                FileRepository.ModifyUserFile("natives/stm/_anotherorder/appsystem/weapon/weaponcataloguserdata_ao.user.2", root =>
                {
                    var dataTable = (RszArrayNode)root["_DataTable"];

                    var existingEntry = dataTable.FirstOrDefault(x => x.Get<int>("_WeaponID") == FlamethrowerWeaponId);
                    if (existingEntry == null && templateEntry != null)
                    {
                        dataTable = dataTable.Add(templateEntry);
                        root = root.SetField("_DataTable", dataTable);
                    }

                    return root;
                });

                // Fix weapon prefab animation for Ada campaign
                FileRepository.ModifyPfbFile("natives/stm/_chainsaw/appsystem/prefab/weapon/wp4701.pfb.17", scene =>
                {
                    var gameObject = scene.Children.OfType<RszGameObject>().FirstOrDefault();
                    if (gameObject == null)
                        return scene;

                    var component = gameObject.FindComponent("chainsaw.CharacterDynamicMotionBankAttacher");
                    if (component == null)
                        return scene;

                    component = component.Set("_Datas[0]._ID", 380000);
                    gameObject = gameObject.AddOrUpdateComponent(component);
                    scene = scene.UpdateGameObject(gameObject);
                    return scene;
                });
            }

            // Modifying the WeaponCustom file
            var weaponCustomPath = GetPath(
                "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2",
                "natives/stm/_anotherorder/appsystem/weaponcustom/weaponcustomuserdata_ao.user.2");

            FileRepository.ModifyUserFile(weaponCustomPath, root =>
            {
                var userdata = RszSerializer.Deserialize<chainsaw.WeaponCustomUserdata>(root)!;
                var stage = userdata._WeaponStages.First(x => x._WeaponID == FlamethrowerWeaponId);
                // adding missing 5th level for damage
                stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages.Add(new chainsaw.WeaponCustomUserdata.AttackUpCustomStage());
                // adding all info and cost values for damage
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[i]._Info = _wpflamethrower[$"damage level {i + 1}"].ToString()!;
                    stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[i]._Cost = (int)_wpflamethrower[$"damage cost level {i + 1}"];
                }
                // adding and setting the attack params
                for (var i = 0; i < 4; i++)
                {
                    stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[4]._AttackUpParams.Add(new chainsaw.WeaponCustomUserdata.AttackUpParam());
                    stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[4]._AttackUpParams[i]._AttackUp = i;
                    stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[4]._AttackUpParams[i]._Level = 4;
                }
                // adding new common upgrade path for ammo
                stage._WeaponCustom._Commons.Add(new chainsaw.WeaponCustomUserdata.Common());
                stage._WeaponCustom._Commons[1]._CommonCustomCategory = 2;
                // adding all info and cost values for damage
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages.Add(new chainsaw.WeaponCustomUserdata.AmmoMaxUpCustomStage());
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._Info = _wpflamethrower[$"ammo capacity level {i + 1}"].ToString()!;
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._Cost = (int)_wpflamethrower[$"ammo capacity cost level {i + 1}"];
                }
                // adding level info for ammo
                for (var i = 1; i < 5; i++)
                {
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._AmmoMaxUpParams.Add(new chainsaw.WeaponCustomUserdata.AmmoMaxUpParam());
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._AmmoMaxUpParams[0]._AmmoMaxUp = 0;
                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._AmmoMaxUpParams[0]._Level = i;
                }
                // change/add individual based on if it exists or not
                if (stage._WeaponCustom._Individuals.Count == 0)
                {
                    stage._WeaponCustom._Individuals.Add(new chainsaw.WeaponCustomUserdata.Individual());
                }
                stage._WeaponCustom._Individuals[0]._IndividualCustomCategory = 1;
                // update the values for the first existing level
                if (stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages.Count == 0)
                {
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages.Add(new chainsaw.WeaponCustomUserdata.ThroughNumCustomStage());
                }
                stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[0]._Cost = -1;
                stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[0]._Info = _wpflamethrower[$"penetration level {1}"].ToString()!;
                // adding all info and cost values for penetration
                for (var i = 1; i < 5; i++)
                {
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages.Add(new chainsaw.WeaponCustomUserdata.ThroughNumCustomStage());
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[i]._Info = _wpflamethrower[$"penetration level {i + 1}"].ToString()!;
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[i]._Cost = (int)_wpflamethrower[$"penetration cost level {i + 1}"];
                }
                // adding level info for penetration
                for (var i = 1; i < 5; i++)
                {
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[i]._ThroughNumParams.Add(new chainsaw.WeaponCustomUserdata.ThroughNumParam());
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[i]._ThroughNumParams[0]._ThroughNum = 0;
                    stage._WeaponCustom._Individuals[0]._CustomThroughNum._ThroughNumCustomStages[i]._ThroughNumParams[0]._Level = i;
                }
                // adding new individual upgrade path for flame distance
                stage._WeaponCustom._Individuals.Add(new chainsaw.WeaponCustomUserdata.Individual());
                stage._WeaponCustom._Individuals[1]._IndividualCustomCategory = 10;
                // adding all info and cost for flame distance
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages.Add(new chainsaw.WeaponCustomUserdata.FlameDistanceCustomStage());
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages[i]._Info = _wpflamethrower[$"flame distance level {i + 1}"].ToString()!;
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages[i]._Cost = (int)_wpflamethrower[$"flame distance cost level {i + 1}"];
                }
                // adding level info for flame distance
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages[i]._FlameDistanceParams.Add(new chainsaw.WeaponCustomUserdata.FlameDistanceParam());
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages[i]._FlameDistanceParams[0]._FlameDistance = 0;
                    stage._WeaponCustom._Individuals[1]._CustomFlameDistance._FlameDistanceCustomStages[i]._FlameDistanceParams[0]._Level = i;
                }
                // updating exclusive values
                stage._WeaponCustom._LimitBreak[0]._CustomLimitBreak._RateValue = (float)_wpflamethrower[$"damage exclusive"];
                stage._WeaponCustom._LimitBreak[0]._CustomLimitBreak._LimitBreakCustomStages[0]._Cost = (int)_wpflamethrower[$"damage cost exclusive"];
                return (RszObjectNode)RszSerializer.Serialize(root.Type, userdata);
            });

            FileRepository.ModifyUserFile(weaponCustomPath, root =>
            {
                var weaponStages = (RszArrayNode)root["_WeaponStages"];
                for (var i = 0; i < weaponStages.Length; i++)
                {
                    var weaponStage = weaponStages[i];
                    if (weaponStage.Get<int>("_WeaponID") == FlamethrowerWeaponId)
                    {
                        weaponStage = weaponStage.Set("_WeaponCustom._Commons[0]._CustomAttackUp._MessageId", new Guid("dd368036-649b-41a0-8f8a-4c8f26452d28"));
                        weaponStage = weaponStage.Set("_WeaponCustom._Commons[1]._CustomAmmoMaxUp._MessageId", new Guid("66757d70-f8da-4585-92de-8df01ea601d3"));
                        weaponStage = weaponStage.Set("_WeaponCustom._Individuals[0]._CustomThroughNum._MessageId", new Guid("db128948-0960-4147-814d-fec706a5c34a"));
                        weaponStage = weaponStage.Set("_WeaponCustom._Individuals[1]._CustomFlameDistance._MessageId", new Guid("79ad9402-fb4b-42b0-8c98-5355812c931a"));
                        weaponStage = weaponStage.Set("_WeaponCustom._LimitBreak[0]._CustomLimitBreak._MessageId", new Guid("876c8ba0-3637-4aff-a065-86254207705d"));
                        weaponStage = weaponStage.Set("_WeaponCustom._LimitBreak[0]._CustomLimitBreak._PerksMessageId", new Guid("e8236563-0f8f-4d96-b662-d808852b48a7"));
                        root = root.SetField("_WeaponStages", weaponStages.SetItem(i, weaponStage));
                        break;
                    }
                }
                return root;
            });

            // modify weapondetailcustom file
            var detailCustomPath = GetPath(
                "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2",
                "natives/stm/_anotherorder/appsystem/weaponcustom/weapondetailcustomuserdata_ao.user.2");

            FileRepository.ModifyUserFile(detailCustomPath, root =>
            {
                var userdata = RszSerializer.Deserialize<chainsaw.WeaponDetailCustomUserdata>(root)!;
                var stage = userdata._WeaponDetailStages.First(x => x._WeaponID == FlamethrowerWeaponId);
                // adding/editing all damage related upgrade values
                var attackUpList = new[] { "_DamageRates", "_WinceRates", "_BreakRates", "_StoppingRates" };
                foreach (var type in attackUpList)
                {
                    chainsaw.ShellBaseAttackInfo.CurveVariable templateAttackCustom = type switch
                    {
                        "_DamageRates" => stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._DamageRates[1],
                        "_WinceRates" => stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._WinceRates[1],
                        "_BreakRates" => stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._BreakRates[1],
                        "_StoppingRates" => stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._StoppingRates[1],
                        _ => throw new InvalidOperationException()
                    };
                    var newAttackCustom = CloneRszData(templateAttackCustom);
                    if (type == "_DamageRates")
                        stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._DamageRates.Add((chainsaw.ShellBaseAttackInfo.CurveVariable)newAttackCustom);
                    else if (type == "_WinceRates")
                        stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._WinceRates.Add((chainsaw.ShellBaseAttackInfo.CurveVariable)newAttackCustom);
                    else if (type == "_BreakRates")
                        stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._BreakRates.Add((chainsaw.ShellBaseAttackInfo.CurveVariable)newAttackCustom);
                    else if (type == "_StoppingRates")
                        stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._StoppingRates.Add((chainsaw.ShellBaseAttackInfo.CurveVariable)newAttackCustom);
                }
                // setting all values for damage, wince, break, and stopping
                for (var i = 1; i < 5; i++)
                {
                    stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._DamageRates[i]._BaseValue = (float)_wpflamethrower[$"damage level {i + 1}"] / (float)_wpflamethrower[$"damage level 1"];
                    stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._StoppingRates[i]._BaseValue = (float)_wpflamethrower[$"stopping level {i + 1}"] / (float)_wpflamethrower[$"stopping level 1"];
                    stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._WinceRates[i]._BaseValue = 1.0f;
                    stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._BreakRates[i]._BaseValue = 1.0f;
                }
                // adding/editing all ammo max related upgrade values
                var templateAmmoCustom = userdata._WeaponDetailStages
                    .First(x => x._WeaponID == 4000)
                    ._WeaponDetailCustom._CommonCustoms[1];
                var newAmmoCustom = CloneRszData(templateAmmoCustom);
                stage._WeaponDetailCustom._CommonCustoms.Add(newAmmoCustom);
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponDetailCustom._CommonCustoms[1]._AmmoMaxUp._AmmoMaxs[i] = (int)_wpflamethrower[$"ammo capacity level {i + 1}"];
                }
                // editing first individual custom penetration
                if (stage._WeaponDetailCustom._IndividualCustoms.Count == 0)
                {
                    var templatePenetrationCustom = userdata._WeaponDetailStages
                        .First(x => x._WeaponID == 4000)
                        ._WeaponDetailCustom._IndividualCustoms[1];
                    var newPenetrationCustom = CloneRszData(templatePenetrationCustom);
                    stage._WeaponDetailCustom._IndividualCustoms.Add(newPenetrationCustom);
                    stage._WeaponDetailCustom._IndividualCustoms[0]._IndividualCustomCategory = 1;
                    for (var i = 0; i < 5; i++)
                    {
                        stage._WeaponDetailCustom._IndividualCustoms[0]._ThroughNums._ThroughNum_Normal.Add(0);
                        stage._WeaponDetailCustom._IndividualCustoms[0]._ThroughNums._ThroughNum_Normal[i] = (int)_wpflamethrower[$"penetration level {i + 1}"];
                    }
                }
                else
                {
                    stage._WeaponDetailCustom._IndividualCustoms[0]._IndividualCustomCategory = 1;
                    for (var i = 0; i < 5; i++)
                    {
                        stage._WeaponDetailCustom._IndividualCustoms[0]._ThroughNums._ThroughNum_Normal.Add(0);
                        stage._WeaponDetailCustom._IndividualCustoms[0]._ThroughNums._ThroughNum_Normal[i] = (int)_wpflamethrower[$"penetration level {i + 1}"];
                    }
                }
                // adding/editing second individual custom flame distance
                var templateFlameCustom = userdata._WeaponDetailStages
                    .First(x => x._WeaponID == 4000)
                    ._WeaponDetailCustom._IndividualCustoms[1];
                var newFlameCustom = CloneRszData(templateFlameCustom);
                stage._WeaponDetailCustom._IndividualCustoms.Add(newFlameCustom);
                stage._WeaponDetailCustom._IndividualCustoms[1]._IndividualCustomCategory = 10;
                for (var i = 0; i < 5; i++)
                {
                    stage._WeaponDetailCustom._IndividualCustoms[1]._FlameDistance._ShellDistance.Add(new float());
                    stage._WeaponDetailCustom._IndividualCustoms[1]._FlameDistance._ShellDistance[i] = (float)_wpflamethrower[$"flame distance level {i + 1}"];
                }
                // editing limit break
                stage._WeaponDetailCustom._LimitBreakCustoms[0]._LimitBreakAttackUp._DamageRateScale = (float)_wpflamethrower[$"damage exclusive"];
                stage._WeaponDetailCustom._LimitBreakCustoms[0]._LimitBreakAttackUp._BreakRateScale = (float)_wpflamethrower[$"break exclusive"];
                return (RszObjectNode)RszSerializer.Serialize(root.Type, userdata);
            });

            var weaponEquipPath = GetPath(
                "natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2",
                "natives/stm/_anotherorder/appsystem/weapon/weaponequipparamcataloguserdata_ao.user.2");

            FileRepository.ModifyUserFile(weaponEquipPath, root =>
            {
                var datas = (RszArrayNode)root["_DataTable"];
                for (var i = 0; i < datas.Length; i++)
                {
                    if (datas[i].Get<int>("_WeaponID") == FlamethrowerWeaponId)
                    {
                        var data = datas[i];
                        if (!string.IsNullOrWhiteSpace(_wpflamethrower["baserateoffire"]?.ToString()))
                        {
                            data = data
                                .Set("_WeaponStructureParam._RapidSpeed", _wpflamethrower["baserateoffire"]);
                        }
                        if (!string.IsNullOrWhiteSpace(_wpflamethrower["basereloadrounds"]?.ToString()))
                        {
                            data = data
                                .Set("_WeaponStructureParam.ReloadNum", (Int32)_wpflamethrower["basereloadrounds"]);
                        }
                        if (!string.IsNullOrWhiteSpace(_wpflamethrower["basereloadspeed"]?.ToString()))
                        {
                            data = data
                                .Set("_WeaponStructureParam._ReloadSpeedRate", _wpflamethrower["basereloadspeed"]);
                        }
                        if (!string.IsNullOrWhiteSpace(_wpflamethrower["typeofshoot"]?.ToString()))
                        {
                            data = data
                                .Set("_WeaponStructureParam.TypeOfShoot", _wpflamethrower["typeofshoot"]);
                        }
                        root = root.SetField("_DataTable", datas.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });
        }

        private void UpdateCharacters()
        {
            string getBurnParamPath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}burnparamuserdata.user.2";

            var characters = new[]
            {
                "ch1b5z1", "ch1b7z0",
                "ch1c0z1", "ch1c0z2", "ch1c8z0",
                "ch1d0z0", "ch1d1z1", "ch1d2z0", "ch1d3z0", "ch1d4z0",
                "ch1d6z0",
                "ch1e0z0",
                "ch1f0z0", "ch1f1z0", "ch1f2z0", "ch1f4z1", "ch1f5z1", "ch1f6z0", "ch1f7z0", "ch1f8z0", "ch1fcz0", "ch1fdz0",
                "ch8g2z0", "ch8g3z0", "ch8gaz0"
            };

            var templateBurnParamPath = getBurnParamPath("ch1c0z0");
            FileRepository.ModifyUserFile(templateBurnParamPath, root =>
            {
                return root
                .Set("_BurnupBorder", 20000) // Stops enemies insta drying from fire
                .Set("_BurnLevelMax", 1200) // Increases the the maximum amount of burn an enemy can stack
                .Set("_SubsidePower", 50);  // Decreases the rate at which burn level decreases
            });

            foreach (var ch in characters)
            {
                var burnParamPath = getBurnParamPath(ch);
                if (FileRepository.TryGetFile(burnParamPath) == null)
                {
                    // User data not there, add reference to it in main param file
                    var paramPath = $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}paramuserdata.user.2";

                    FileRepository.ModifyUserFile(paramPath, root =>
                    {
                        return root.SetField("_BurnParam", new RszUserDataNode(
                            FileRepository.TypeRepository.FromName("chainsaw.EnemyBurnParamUserData")!,
                            $"_Chainsaw/AppSystem/Character/{ch}/UserData/{ch}BurnParamUserData.user"));
                    });
                }

                FileRepository.SetFile(burnParamPath, FileRepository.GetFile(templateBurnParamPath)!);
            }

            // Change garrador burn ID ISSUE
            var garradorburnPath = getBurnParamPath("ch1d0z0");
            FileRepository.ModifyUserFile(garradorburnPath, root =>
            {
                return root
                .Set("_BurnEffectID", new RszValueNode(RszFieldType.Uint2, new byte[] { 7, 0, 0, 0, 0, 0, 0, 0 }))
                .Set("_BurnupEffectID", new RszValueNode(RszFieldType.Uint2, new byte[] { 7, 0, 0, 0, 1, 0, 0, 0 }));
            });

            // Inserting missing damage values for burn
            string getAttackHitPath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}attackhituserdata.user.2";

            var charactersDamage = new[]
            {
                "ch1b5z1", "ch1b7z0","ch1c0z0",
                "ch1c0z1", "ch1c0z2", "ch1c8z0",
                "ch1d0z0", "ch1d1z1", "ch1d2z0", "ch1d3z0", "ch1d4z0", 
                //"ch1d6z0",
                "ch1e0z0",
                "ch1f0z0", "ch1f1z0", "ch1f2z0", "ch1f4z1", "ch1f5z1",
                "ch1f6z0",
                "ch1f7z0", "ch1f8z0", "ch1fcz0", "ch1fdz0",
                "ch8g2z0", "ch8g3z0", "ch8gaz0"
            };

            uint KeyNameHashValueBurnTickDamage = 4179126236;
            uint KeyNameHashValueFinalBurnTickDamage = 3042382614;

            foreach (var ch in charactersDamage)
            {
                var AttackHitPath = getAttackHitPath(ch);
                if (FileRepository.TryGetFile(AttackHitPath) != null)
                {
                    FileRepository.ModifyUserFile(AttackHitPath, root =>
                    {
                        var AttackDataList = (RszArrayNode)root["_AttackDataList"];
                        for (int i = 0; i < AttackDataList.Length; i++)
                        {
                            var AttackData = AttackDataList[i];
                            if (AttackData.Get<uint>("_KeyNameHash") == KeyNameHashValueBurnTickDamage)
                            {
                                AttackData = AttackData.Set("_Damage", (int)_wpflamethrower["burndamage"]);
                                AttackDataList = AttackDataList.SetItem(i, AttackData);
                            }
                            if (AttackData.Get<uint>("_KeyNameHash") == KeyNameHashValueFinalBurnTickDamage)
                            {
                                AttackData = AttackData.Set("_Damage", (int)_wpflamethrower["burndamage"] * 2);
                                AttackDataList = AttackDataList.SetItem(i, AttackData);
                            }
                        }
                        root = root.SetField("_AttackDataList", AttackDataList);
                        return root;
                    });
                }
            }

            foreach (var ch in charactersDamage)
            {
                var AttackHitPath = getAttackHitPath(ch);
                if (FileRepository.TryGetFile(AttackHitPath) != null)
                {
                    FileRepository.ModifyUserFile(AttackHitPath, root =>
                    {
                        var AttackDataList = (RszArrayNode)root["_AttackDataList"];
                        var AttackData = AttackDataList.FirstOrDefault(x => x.Get<uint>("_KeyNameHash") == KeyNameHashValueBurnTickDamage);
                        if (AttackData == null)
                        {
                            AttackDataList = AttackDataList.Add(FileRepository.TypeRepository
                              .Create("chainsaw.collision.AttackHitUserData.AttackData")
                                  .Set("_KeyNameHash", KeyNameHashValueBurnTickDamage)
                                  .Set("_Damage", (int)_wpflamethrower[$"burndamage"])
                                  .Set("STRUCT__Wince__HasValue", true)
                                  .Set("STRUCT__Wince__Value", 0)
                                  .Set("STRUCT__Break__HasValue", true)
                                  .Set("STRUCT__Break__Value", 0)
                                  .Set("STRUCT__Stopping__HasValue", true)
                                  .Set("STRUCT__Stopping__Value", 0)
                                  .Set("_IsPartnerDamage", false)
                                  .Set("_AttackType", 21)
                                  .Set("_AttackPower", 1)
                                  .Set("_DeadType", 7)
                                  .Set("_Priority", 8)
                                  .Set("_SortType", 0)
                                  .Set("_Option", 8)
                                  .Set("_IntervalTime", 0)
                                  .Set("_Enchant", 0)
                                  .Set("_IsThroughRestriction", false)
                                  .Set("_ThroughNum", 1)
                                  .Set("_DirectionType", 0)
                                  .Set("_JointNameHash", 2180083513)
                                  .Set("_Mute", true)
                                  .Set("_SoundTriggerId", 4294967295)
                                  .Set("_EffectJointNameHash", 2180083513)
                                  .Set("_AttackToEnemyUserData", new RszNullNode())
                                  .Set("_AttackToPlayerUserData", new RszNullNode())
                                  .Set("_AttackToGimmickUserData", new RszNullNode())
                                  .Set("_BreakLevel", 2));
                        }
                        var AttackData2 = AttackDataList.FirstOrDefault(x => x.Get<uint>("_KeyNameHash") == KeyNameHashValueFinalBurnTickDamage);
                        if (AttackData2 == null)
                        {
                            AttackDataList = AttackDataList.Add(FileRepository.TypeRepository
                                .Create("chainsaw.collision.AttackHitUserData.AttackData")
                                    .Set("_KeyNameHash", KeyNameHashValueFinalBurnTickDamage)
                                    .Set("_Damage", (int)_wpflamethrower["burndamage"] * 2)
                                    .Set("STRUCT__Wince__HasValue", true)
                                    .Set("STRUCT__Wince__Value", 0)
                                    .Set("STRUCT__Break__HasValue", true)
                                    .Set("STRUCT__Break__Value", 0)
                                    .Set("STRUCT__Stopping__HasValue", true)
                                    .Set("STRUCT__Stopping__Value", 0)
                                    .Set("_IsPartnerDamage", false)
                                    .Set("_AttackType", 21)
                                    .Set("_AttackPower", 1)
                                    .Set("_DeadType", 7)
                                    .Set("_Priority", 8)
                                    .Set("_SortType", 0)
                                    .Set("_Option", 8)
                                    .Set("_IntervalTime", 0)
                                    .Set("_Enchant", 0)
                                    .Set("_IsThroughRestriction", false)
                                    .Set("_ThroughNum", 1)
                                    .Set("_DirectionType", 0)
                                    .Set("_JointNameHash", 2180083513)
                                    .Set("_Mute", true)
                                    .Set("_SoundTriggerId", 4294967295)
                                    .Set("_EffectJointNameHash", 2180083513)
                                    .Set("_AttackToEnemyUserData", new RszNullNode())
                                    .Set("_AttackToPlayerUserData", new RszNullNode())
                                    .Set("_AttackToGimmickUserData", new RszNullNode())
                                    .Set("_BreakLevel", 2));
                        }
                        root = root.SetField("_AttackDataList", AttackDataList);
                        return root;
                    });
                }
            }

            // Modify damage multipliers for regular hits
            string getWeaponDamagePath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}weapondamagerateuserdata.user.2";

            foreach (var ch in charactersDamage)
            {
                var weaponDamagePath = getWeaponDamagePath(ch);
                if (FileRepository.TryGetFile(weaponDamagePath) != null)
                {
                    FileRepository.ModifyUserFile(weaponDamagePath, root =>
                    {
                        var weaponDamageList = (RszArrayNode)root["_DataList"];
                        for (int i = 0; i < weaponDamageList.Length; i++)
                        {
                            var weaponDamageData = weaponDamageList[i];
                            if (weaponDamageData.Get<int>("_WeaponID") == FlamethrowerWeaponId)
                            {
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__HasValue", true);
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__Value", 1.0f);
                                weaponDamageData = weaponDamageData.Set("_Probability", 0f);

                                weaponDamageList = weaponDamageList.SetItem(i, weaponDamageData);
                            }
                        }
                        root = root.SetField("_DataList", weaponDamageList);
                        return root;
                    });
                }
            }

            string getEnhancedWeaponDamagePath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}enhancedweapondamagerateuserdata.user.2";

            foreach (var ch in charactersDamage)
            {
                var weaponDamagePath = getEnhancedWeaponDamagePath(ch);
                if (FileRepository.TryGetFile(weaponDamagePath) != null)
                {
                    FileRepository.ModifyUserFile(weaponDamagePath, root =>
                    {
                        var weaponDamageList = (RszArrayNode)root["_DataList"];
                        for (int i = 0; i < weaponDamageList.Length; i++)
                        {
                            var weaponDamageData = weaponDamageList[i];
                            if (weaponDamageData.Get<int>("_WeaponID") == FlamethrowerWeaponId)
                            {
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__HasValue", true);
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__Value", 1.0f);
                                weaponDamageData = weaponDamageData.Set("_Probability", 0f);

                                weaponDamageList = weaponDamageList.SetItem(i, weaponDamageData);
                            }
                        }
                        root = root.SetField("_DataList", weaponDamageList);
                        return root;
                    });
                }
            }

            // Modify damage multipliers for headshot
            string getWeaponHeadDamagePath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}weapondamagerateuserdatahead.user.2";

            foreach (var ch in charactersDamage)
            {
                var weaponDamagePath = getWeaponHeadDamagePath(ch);
                if (FileRepository.TryGetFile(weaponDamagePath) != null)
                {
                    FileRepository.ModifyUserFile(weaponDamagePath, root =>
                    {
                        var weaponDamageList = (RszArrayNode)root["_DataList"];
                        for (int i = 0; i < weaponDamageList.Length; i++)
                        {
                            var weaponDamageData = weaponDamageList[i];
                            if (weaponDamageData.Get<int>("_WeaponID") == FlamethrowerWeaponId)
                            {
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__HasValue", true);
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__Value", 1.0f);
                                weaponDamageData = weaponDamageData.Set("_Probability", 0f);

                                weaponDamageList = weaponDamageList.SetItem(i, weaponDamageData);
                            }
                        }
                        root = root.SetField("_DataList", weaponDamageList);
                        return root;
                    });
                }
            }

            string getEnhancedWeaponHeadDamagePath(string ch) => $"natives/stm/_chainsaw/appsystem/character/{ch}/userdata/{ch}enhancedweapondamagerateuserdatahead.user.2";

            foreach (var ch in charactersDamage)
            {
                var weaponDamagePath = getEnhancedWeaponHeadDamagePath(ch);
                if (FileRepository.TryGetFile(weaponDamagePath) != null)
                {
                    FileRepository.ModifyUserFile(weaponDamagePath, root =>
                    {
                        var weaponDamageList = (RszArrayNode)root["_DataList"];
                        for (int i = 0; i < weaponDamageList.Length; i++)
                        {
                            var weaponDamageData = weaponDamageList[i];
                            if (weaponDamageData.Get<int>("_WeaponID") == FlamethrowerWeaponId)
                            {
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__HasValue", true);
                                weaponDamageData = weaponDamageData.Set("STRUCT__DamageRate__Value", 1.0f);
                                weaponDamageData = weaponDamageData.Set("_Probability", 0f);

                                weaponDamageList = weaponDamageList.SetItem(i, weaponDamageData);
                            }
                        }
                        root = root.SetField("_DataList", weaponDamageList);
                        return root;
                    });
                }
            }

            ///////////////////////////////////////
            // VFX for Burn
            ///////////////////////////////////////
            string getMainVfxPrefab(string ch) => $"natives/stm/_chainsaw/vfx/provider/epv_character/epv_{ch}/epvc_0015_{ch}_0000.pfb.17";

            var charactershort = new[]
            {
                "chb5", "chb7", "chc0", "chc8", "chd2", "chd3", "chd4", 
                //"chd6",
                "che0", "chf0", "chf1",
                "chf2",
                "chf4", "chf5", "chf6", "chf7", "chf8", "chfd",
                "chg2", "chg3", "chga"
            };

            foreach (var ch in charactershort)
            {
                var MainVfxPrefabPath = getMainVfxPrefab(ch);
                if (FileRepository.TryGetFile(MainVfxPrefabPath) != null)
                {
                    FileRepository.ModifyPfbFile(MainVfxPrefabPath, scene =>
                    {
                        var gameobject = scene.Children.OfType<RszGameObject>().First();
                        var component = gameobject.FindComponent("via.effect.script.EPVDataContainer")!;
                        var MainVfxPrefabList = (RszArrayNode)component["StandardData"];
                        var MainVfxPrefabData = MainVfxPrefabList.FirstOrDefault(x => x.Get<uint>("ID") == 6);
                        if (MainVfxPrefabData == null)
                        {
                            MainVfxPrefabList = MainVfxPrefabList.Add(FileRepository.TypeRepository
                              .Create("via.effect.script.EPVDataContainer.StandardDataSetting")
                                  .Set("Comment", "ガナード　炎ダメージ")
                                  .Set("ID", 6)
                                  .Set("Data.Standby", true)
                                  .Set("Data.Path", "_Chainsaw/VFX/Provider/EPV_Character/EPV_chc0/epvs_0015_chc0_burn_prg_0000.pfb"));
                        }
                        component = component.SetField("StandardData", MainVfxPrefabList);
                        gameobject = gameobject.AddOrUpdateComponent(component);
                        scene = scene.UpdateGameObject(gameobject);
                        return scene;
                    });
                }
            }

            // Setting Burn VFX for Garrador issue
            FileRepository.ModifyPfbFile("natives/stm/_chainsaw/vfx/provider/epv_character/epv_chd0/epvc_0015_chd0_0000.pfb.17", scene =>
            {
                var gameobject = scene.Children.OfType<RszGameObject>().First();
                var component = gameobject.FindComponent("via.effect.script.EPVDataContainer")!;
                var mainVfxPrefabList = (RszArrayNode)component["StandardData"];
                var mainVfxPrefabData = mainVfxPrefabList.FirstOrDefault(x => x.Get<uint>("ID") == 7);
                if (mainVfxPrefabData == null)
                {
                    mainVfxPrefabList = mainVfxPrefabList.Add(FileRepository.TypeRepository
                      .Create("via.effect.script.EPVDataContainer.StandardDataSetting")
                          .Set("Comment", "ガナード　炎ダメージ")
                          .Set("ID", 7)
                          .Set("Data.Standby", true)
                          .Set("Data.Path", "_Chainsaw/VFX/Provider/EPV_Character/EPV_chc0/epvs_0015_chc0_burn_prg_0000.pfb"));
                }
                component = component.SetField("StandardData", mainVfxPrefabList);
                gameobject = gameobject.AddOrUpdateComponent(component);
                scene = scene.UpdateGameObject(gameobject);
                return scene;
            });
        }

        private void FixSalazarCrash()
        {
            // Only fix for Leon's campaign
            if (_campaign != Campaign.Leon)
                return;

            FileRepository.ModifyScnFile("natives/stm/_chainsaw/environment/scene/gimmick/st56/gimmick_st56_200.scn.20",
                scene => scene.RemoveGameObject(new Guid("b9c3f5d1-a5df-44ce-80da-4024afb6e7b9")));
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

        private T CloneRszData<T>(T source) where T : notnull
        {
            var typeName = source.GetType().FullName!.Replace('+', '.');
            var rszNode = RszSerializer.Serialize(FileRepository.TypeRepository.FromName(typeName)!, source);
            var result = RszSerializer.Deserialize<T>(rszNode);
            return result!;
        }
    }
}
