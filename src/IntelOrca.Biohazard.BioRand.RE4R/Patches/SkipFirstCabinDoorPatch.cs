using System;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "skipfirstcabindoor",
        Name = "Skip First Cabin Door",
        Description = "Turns the first cabin door into a normal door that isn't slow to open.",
        Version = "1.0",
        Author = "IntelOrca")]
    internal class SkipFirstCabinDoorPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            context.ModifyScnFile("natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_903_p000.scn.20", scene =>
            {
                scene = ReplaceDoor(
                    scene,
                    new Guid("9a8b310d-6521-4905-bf55-fd1aeefbf2a3"),
                    new Guid("51fa2073-9353-4025-81f4-c1a2fff8bd23"),
                    new Guid("7d9da1e7-91d5-409d-8086-ff23375c1b87"),
                    "hunting_lodge_entrance_door");

                var upstairsDoor = scene.FindGameObject(new Guid("74a99e6f-a333-4542-9bac-2a4c65b158bc"))!;
                var t = new Transform(upstairsDoor);
                t.Eular = new EulerAngles(-90, 0, 0);
                scene = scene.UpdateGameObject(upstairsDoor.AddOrUpdateComponent(t.ToComponent()));
                var paramObject = upstairsDoor.FindGameObject("ParamObject")!;
                scene = scene.UpdateGameObject(
                    paramObject.WithComponents(paramObject.Components
                        .RemoveAll(x =>
                            x.Type.Name == "chainsaw.GmOptionSleep" ||
                            x.Type.Name == "chainsaw.GmOptionDoorLock")));
                return scene;
            });

            context.ModifyScnFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_010.scn.20", scene =>
            {
                return scene.RemoveGameObject(new Guid("dcfb68ba-35e9-40f1-bec2-7a42601513e9"));
            });

            context.ModifyUserFile("natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_903_p000_savedata.user.2", root =>
            {
                var origin = new Vector3(-196.38161f, 10.06376f, 85.859848f);
                var accessPoint0 = origin + new Vector3(-0.02361f, 0.0f, -0.499443f);

                var datas = root.Get<RszArrayNode>("Datas");
                datas = datas.SetItem(15, datas[10]
                    .Set("ID._Index", 1933)
                    .Set("AccessPoints[0].Position", accessPoint0)
                    .Set("AIMapData[0].Position", origin)
                    .Set("AIMapData[0].RotationY", MathF.PI / 2));
                return root.Set("Datas", datas);
            });
        }

        private static RszScene ReplaceDoor(RszScene scene, Guid targetDoorGuid, Guid templateDoorGuid, Guid newGuid, string newName)
        {
            // Remove original door
            var originalDoor = scene.FindGameObject(targetDoorGuid) ?? throw new Exception("Unable to find door to replace.");
            scene = scene.RemoveGameObject(targetDoorGuid);

            // Copy another door (copy transform & gimmickcore components over)
            var templateDoor = scene.FindGameObject(templateDoorGuid) ?? throw new Exception("Unable to find door to copy");
            scene = scene.Add(templateDoor
                .Clone()
                .WithGuid(newGuid)
                .WithName(newName)
                .AddOrUpdateComponent(originalDoor.FindComponent("via.Transform")!)
                .AddOrUpdateComponent(originalDoor.FindComponent("chainsaw.GimmickCore")!));

            return scene;
        }
    }
}
