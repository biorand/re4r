using System;
using System.Collections.Generic;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    /// <summary>
    /// Change the wine bottle battle arena in Separate Ways to use normal spawn controllers instead
    /// of a wave spawn controller. This allows normal enemies to work rather than just armaduras with
    /// activate. Also removes the wooden boards as normal enemies can't break through those.
    /// </summary>
    /// <param name="context"></param>
    internal class WineBottleRoomPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            if (context.GetConfigOption("campaign", "") != "Separate Ways")
                return;

            if (!context.GetConfigOption<bool>("random-enemies"))
                return;

            context.ModifyScnFile("natives/stm/_anotherorder/leveldesign/location/loc51/level_loc51_chp3_1.scn.20", scene =>
            {
                scene = FixTopFloor(scene);
                scene = FixBottomFloor(scene);
                return scene;
            });

            RemoveTsuitates();
        }

        /// <summary>
        /// Top floor (triggered by silver bottle)
        /// Spawn enemies rather than armadura activation.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        private static RszScene FixTopFloor(RszScene scene)
        {
            var spawnControllerGameObject = scene.FindGameObject(new Guid("d82d24e0-cac6-471e-9b2e-808f84053fb9"))!;
            var spawnController = spawnControllerGameObject.FindComponent<chainsaw.CharacterSpawnController>()!;
            spawnController._SpawnCondition._CheckFlags = [
                new CheckFlagInfo()
                {
                    _CheckFlag = new Guid("b9a3aaa9-700c-4e5c-a31f-df66bfbda362"),
                    _CompareValue = true
                }
            ];
            spawnController._SpawnSkipCondition._CheckFlags = [
                new CheckFlagInfo()
                {
                    _CheckFlag = new Guid("84b73ea9-8de6-492d-a479-45f988e06492"),
                    _CompareValue = true
                }
            ];
            return scene.UpdateGameObject(
                spawnControllerGameObject.AddOrUpdateComponent(spawnController));
        }

        /// <summary>
        /// Bottom floor (triggered by gold bottle)
        /// Convert wave controller to normal controllers.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        private RszScene FixBottomFloor(RszScene scene)
        {
            // Remove old wave spawn controller
            var gameObject = scene.FindGameObject(new Guid("0d4eea6c-2722-43fd-b887-830fd4c915dd"))!;
            var normalTransform = new Transform(gameObject);
            scene = scene.RemoveGameObject(gameObject.Guid);

            // Get all the enemies from the wave spawn controller
            var enemies = gameObject.Children.SelectMany(x => x.Children).ToArray();

            // Create 3 new controllers
            var waveFlags = new[]
            {
                new Guid("84b73ea9-8de6-492d-a479-45f988e06492"),
                new Guid("d2366665-7671-4ac1-8d94-d4cbc4e2b06e"),
                new Guid("1f733c2d-fafd-4eaa-9ef9-e6ea313dff6d"),
            };
            var controllerObjects = new List<RszGameObject>();
            for (var i = 0; i < 3; i++)
            {
                var newGameObject = context.GetService<RszFactory>().CreateSpawnController($"Biorand_1F_{i}");
                var spawnController = newGameObject.FindComponent<chainsaw.CharacterSpawnController>()!;
                spawnController._SpawnCondition._CheckFlags.Add(new CheckFlagInfo()
                {
                    _CheckFlag = waveFlags[i],
                    _CompareValue = true
                });
                newGameObject = newGameObject.AddOrUpdateComponent(spawnController);

                for (var j = 0; j < enemies.Length; j++)
                {
                    if ((j % 3) != i)
                        continue;

                    var e = enemies[j];
                    var spawnComponent = e.Components.FirstOrDefault(x => x.Type.Name.Contains("SpawnParam"));
                    if (spawnComponent != null)
                    {
                        // Make sure no damage flag is removed
                        spawnComponent = spawnComponent.Set("_NoDamageCtrlFlag._CheckFlags", Array.Empty<RszObjectNode>());

                        // Enable force find, but without any conditions
                        spawnComponent = spawnComponent.Set("_ForceFind", true);
                        spawnComponent = spawnComponent.Set("_ForceFindCondition._ForceFindCondition", new chainsaw.FlagCondition());
                    }
                    newGameObject = newGameObject.AddOrUpdateChild(e);
                }

                scene = scene.Add(newGameObject);
                controllerObjects.Add(newGameObject);
            }

            // Change dead enemy counter to check controllers instead of enemy types
            var deadEnemyCounterGameObject = scene.FindGameObject(new Guid("0546811f-8274-4dd3-8655-e9bbee0a23d8"))!;
            var deadEnemyCounter = deadEnemyCounterGameObject.FindComponent<chainsaw.DeadEnemyCounter>()!;

            deadEnemyCounter._HasStartFlag = false;
            deadEnemyCounter._StartFlag = Guid.Empty;
            deadEnemyCounter._HasCountTargetIDs = false;
            deadEnemyCounter._CountTargetIDs = [];
            deadEnemyCounter._HasCountTargetSpawnControllers = true;
            deadEnemyCounter._CountTargetSpawnControllers = controllerObjects.Select(x => x.Guid).ToList();

            var tally = 0;
            for (var i = 0; i < controllerObjects.Count; i++)
            {
                tally += controllerObjects[i].Children.Length;
                deadEnemyCounter._DataList[i]._Num = tally - 1;
            }

            scene = scene.UpdateGameObject(deadEnemyCounterGameObject.AddOrUpdateComponent(deadEnemyCounter));

            return scene;
        }

        /// <summary>
        /// Remove the tsuitates (since they don't break if we switch out the armaduras).
        /// </summary>
        private void RemoveTsuitates()
        {
            context.ModifyScnFile("natives/stm/_anotherorder/environment/scene/gimmick/st51/gimmick_st51_857_ao.scn.20", scene =>
            {
                return scene
                    .RemoveGameObject(new Guid("1f1c503b-b032-43da-9e3a-792960586343"))
                    .RemoveGameObject(new Guid("4b30301b-aca4-4199-9745-91acad51ece3"))
                    .RemoveGameObject(new Guid("b76e9b43-a4b7-4d8e-871a-30a67d34b544"))
                    .RemoveGameObject(new Guid("9502ec07-cdb8-4107-a4bc-9e76cc029ec0"))
                    .RemoveGameObject(new Guid("581cace6-a401-42e0-9dd7-383aafbdd551"))
                    .RemoveGameObject(new Guid("06815784-ae02-49b4-b3d6-c7528fcf3d54"))
                    .RemoveGameObject(new Guid("ace7c27b-1cf1-4bb7-bb68-798101d596ef"))
                    .RemoveGameObject(new Guid("9c389ab6-c2b3-488e-b9a9-304ef4831d59"));
            });
        }
    }
}
