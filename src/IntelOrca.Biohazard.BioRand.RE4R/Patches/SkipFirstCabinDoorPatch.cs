using System;
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
                // Remove original door
                var originalDoor = scene.FindGameObject(new Guid("9a8b310d-6521-4905-bf55-fd1aeefbf2a3")) ?? throw new Exception("Unable to find door to replace.");
                scene = scene.RemoveGameObject(originalDoor.Guid);

                // Copy another door (copy transform & gimmickcore components over)
                var templateDoor = scene.FindGameObject(new Guid("51fa2073-9353-4025-81f4-c1a2fff8bd23")) ?? throw new Exception("Unable to find door to copy");
                scene = scene.Add(templateDoor
                    .Clone()
                    .WithGuid(new Guid("7d9da1e7-91d5-409d-8086-ff23375c1b87"))
                    .WithName("hunting_lodge_entrance_door")
                    .AddOrUpdateComponent(originalDoor.FindComponent("via.Transform")!)
                    .AddOrUpdateComponent(originalDoor.FindComponent("chainsaw.GimmickCore")!));

                return scene;
            });
            context.ModifyUserFile("natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_903_p000_savedata.user.2", root =>
            {
                var datas = root.Get<RszArrayNode>("Datas");
                datas = datas.SetItem(15, datas[10]
                    .Set("ID._Index", 1933));
                return root.Set("Datas", datas);
            });
        }
    }
}
