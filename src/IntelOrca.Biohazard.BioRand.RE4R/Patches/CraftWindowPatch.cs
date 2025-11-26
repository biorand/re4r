using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class CraftWindowPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            string getCraftItemModelPath(string item) => $"natives/stm/_Chainsaw/AppSystem/Prefab/Gui/AttacheCase/ItemModel/craftitemmodel_{item}.pfb.17";
            string getCraftItemModelPathGui(string item) => $"_Chainsaw/AppSystem/Prefab/Gui/AttacheCase/ItemModel/craftitemmodel_{item}.pfb";
            var itemModels = new[]
            {
                "sm73_500", // Gunpowder
                "sm73_501", // Small Resource
                "sm73_504", // Large Resource
                "wp5400", // Hand Grenade
                "wp5003" // Boot Knife
            };

            //add new craft items to craft gui settings
            foreach (var item in itemModels)
            {
                var itemId = item switch
                {
                    "sm73_500" => ItemIds.Gunpowder,
                    "sm73_501" => ItemIds.SmallResource,
                    "sm73_504" => ItemIds.LargeResource,
                    "wp5400" => ItemIds.HandGrenade,
                    "wp5003" => ItemIds.BootKnife,
                    _ => 0
                };

                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/guiresource/guiresourcesettinguserdata_craft.user.2", root =>
                {
                    var settings = (RszArrayNode)root["_Settings"];
                    settings = settings.Add(FileRepository.RszRepository
                        .Create("chainsaw.GuiResourceSetting_Craft")
                            .Set("_ItemId", itemId)
                            .Set("_Prefab.Path", new RszResourceNode(getCraftItemModelPathGui(item))));
                    root = root.SetField("_Settings", settings);
                    return root;
                });
            }


            //add new prefab files for craft items
            var templateCraftItemModel = $"natives/stm/_Chainsaw/AppSystem/Prefab/Gui/AttacheCase/ItemModel/craftitemmodel_sm70_500.pfb.17";

            foreach (var item in itemModels)
            {
                var craftItemModelPath = getCraftItemModelPath(item);
                context.SetFile(craftItemModelPath, context.GetFile(templateCraftItemModel)!);
            }

            //modify prefab files to use correct meshes and materials
            foreach (var item in itemModels)
            {
                var meshPath = item switch
                {
                    "sm73_500" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_500/sm73_500_00.mesh",
                    "sm73_501" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_501/sm73_501_00.mesh",
                    "sm73_504" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_504/sm73_504_00.mesh",
                    "wp5400" => "_Chainsaw/Character/wp/wp54/wp5400/00/wp5400_00.mesh",
                    "wp5003" => "_Chainsaw/Character/wp/wp50/wp5003/00/wp5003_00.mesh",
                    _ => ""

                };
                var materialPath = item switch
                {
                    "sm73_500" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_500/sm73_500_00_Mat.mdf2",
                    "sm73_501" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_501/sm73_501_00_Mat.mdf2",
                    "sm73_504" => "_Chainsaw/Environment/sm/sm7X/sm73/sm73_504/sm73_504_00_Mat.mdf2",
                    "wp5400" => "_Chainsaw/Character/wp/wp54/wp5400/00/wp5400_00.mdf2",
                    "wp5003" => "_Chainsaw/Character/wp/wp50/wp5003/00/wp5003_00.mdf2",
                    _ => ""
                };

                var itemScale = item switch
                {
                    "sm73_500" => new Vector3(0.2f, 0.2f, 0.2f),
                    "sm73_501" => new Vector3(0.15f, 0.15f, 0.15f),
                    "sm73_504" => new Vector3(0.15f, 0.15f, 0.15f),
                    "wp5400" => new Vector3(0.3f, 0.3f, 0.3f),
                    "wp5003" => new Vector3(0.15f, 0.15f, 0.15f),
                    _ => new Vector3(0f, 0f, 0f)
                };

                var itemOffset = item switch
                {
                    "sm73_500" => new Vector3(0f, -0.02f, 0f),
                    "sm73_501" => new Vector3(0.0f, 0.0f, 0.0f),
                    "sm73_504" => new Vector3(0.0f, 0.0f, 0.0f),
                    "wp5400" => new Vector3(0f, -0.02f, 0f),
                    "wp5003" => new Vector3(0.02f, -0.02f, 0f),
                    _ => new Vector3(0f, 0f, 0f)
                };

                var craftItemModelPath = getCraftItemModelPath(item);

                context.ModifyPfbFile(craftItemModelPath, scene =>
                {
                    var gameObjectTarget = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    gameObjectTarget = gameObjectTarget.WithName($"CharmItemModel_{item}");
                    scene = scene.UpdateGameObject(gameObjectTarget);
                    return scene;
                });

                context.ModifyPfbFile(craftItemModelPath, scene =>
                {
                    var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectp2 = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectP3 = gameObjectp2.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectTarget = gameObjectP3.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var component = gameObjectTarget.FindComponent("via.render.Mesh")!;
                    component = component
                                .Set("Mesh", new RszResourceNode(meshPath))
                                .Set("Material", new RszResourceNode(materialPath));
                    gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                    gameObjectP3 = gameObjectP3.AddOrUpdateChild(gameObjectTarget);
                    gameObjectp2 = gameObjectp2.AddOrUpdateChild(gameObjectP3);
                    gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectp2);
                    scene = scene.UpdateGameObject(gameObjectP1);
                    return scene;
                });

                context.ModifyPfbFile(craftItemModelPath, scene =>
                {
                    var gameObjectP1 = scene.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectp2 = gameObjectP1.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectP3 = gameObjectp2.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var gameObjectTarget = gameObjectP3.Children.OfType<RszGameObject>().FirstOrDefault()!;
                    var component = gameObjectTarget.FindComponent("chainsaw.AcItemModelMeshController")!;
                    var attacheCasePrefabList = (RszArrayNode)component["_Settings"];
                    for (int i = 0; i < attacheCasePrefabList.Length; i++)
                    {
                        var data = attacheCasePrefabList[i];
                        if (data.Get<int>("_Mode") == 4)
                        {
                            data = data
                                .Set("_Scale", itemScale)
                                .Set("_Offset", itemOffset);
                            attacheCasePrefabList = attacheCasePrefabList.SetItem(i, data);
                        }
                    }
                    component = component.SetField("_Settings", attacheCasePrefabList);

                    gameObjectTarget = gameObjectTarget.AddOrUpdateComponent(component);
                    gameObjectP3 = gameObjectP3.AddOrUpdateChild(gameObjectTarget);
                    gameObjectp2 = gameObjectp2.AddOrUpdateChild(gameObjectP3);
                    gameObjectP1 = gameObjectP1.AddOrUpdateChild(gameObjectp2);
                    scene = scene.UpdateGameObject(gameObjectP1);
                    return scene;
                });

            }
        }

        static class ItemIds
        {
            public const int Gunpowder = 117600000;
            public const int HandGrenade = 277075456;
            public const int SmallResource = 117606400;
            public const int LargeResource = 117601600;
            public const int BootKnife = 276440256;
        }
    }
}
