using System;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class GarradorRoomPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            context.ModifyScnFile("natives/stm/_anotherorder/leveldesign/location/loc55/level_loc55.scn.20", scene =>
            {
                // Spawn enemies when doors shut, otherwise enemies (originally garradors)
                // can't be hurt and they leave the area and attack you prematurely.
                var controllerGuids = new[] {
                    new Guid("f5402bf4-4c55-4332-86c0-53851701e532"), // standard
                    new Guid("4924d5ff-3905-421f-b6b3-1d30d900be95") // pro
                };

                foreach (var controllerGuid in controllerGuids)
                {
                    // Add another condition so garradors spawn when shutter closes
                    var gameObject = scene.FindGameObject(controllerGuid)!;
                    var spawnController = gameObject.FindComponent<chainsaw.CharacterSpawnController>()!;
                    spawnController._SpawnCondition._CheckFlags.Add(new CheckFlagInfo()
                    {
                        _CheckFlag = new Guid("40807771-38e9-4ec8-a240-d75f4fdff461"),
                        _CompareValue = true
                    });
                    gameObject = gameObject.AddOrUpdateComponent(spawnController);
                    scene = scene.UpdateGameObject(gameObject);

                    // Move enemies to a better position (TODO move to spreadsheet?)
                    foreach (var child in gameObject.Children)
                    {
                        var transform = new Transform(child);
                        transform.Position = new Vector3(152, transform.Position.Y, transform.Position.Z);
                        scene = scene.UpdateGameObject(
                            child.AddOrUpdateComponent(transform.ToComponent(context)));
                    }
                }

                return scene;
            });
        }
    }
}
