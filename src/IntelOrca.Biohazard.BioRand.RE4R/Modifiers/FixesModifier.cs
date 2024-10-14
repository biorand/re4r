using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class FixesModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();

            StaticChanges(randomizer, logger);
            DisableFirstAreaInhibitor(randomizer, logger);
            FixDeadEnemyCounters(randomizer, logger);
            FixSpawnControllers(randomizer, logger);
            SlowDownFactoryDoor(randomizer, logger);
            IncreaseJetSkiTimer(randomizer, logger);
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                ImproveKnightyKnightKnightRoom(randomizer, logger);
            }
            if (randomizer.GetConfigOption<bool>("enable-autosave-pro"))
            {
                EnableProfessionalAutoSave(randomizer, logger);
            }
            AllowLaserSightOnAnything(randomizer, logger);
            RandomizeFirstBearTrap(randomizer, logger, rng);
            EnableInstantBuy(randomizer, logger);
            ImproveBoltThrower(randomizer, logger);
        }

        private void StaticChanges(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var path = "natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_502_p000.scn.20";
            var fileRepository = randomizer.FileRepository;
            var data = fileRepository.GetGameFileData(path);
            if (data == null)
                return;

            var scn = ChainsawRandomizerFactory.Default.ReadScnFile(data);
            scn.RemoveGameObject(new Guid("ca0ac85f-1238-49d9-a0fb-0d58a42487a1"));
            scn.RemoveGameObject(new Guid("4a975fc1-2e1c-4fd3-a49a-1f35d6a30f0f"));
            fileRepository.SetGameFileData(path, scn.ToByteArray());
        }

        private void DisableFirstAreaInhibitor(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            logger.LogLine("Updating first area inhibitor");
            var areas = randomizer.Areas;
            var firstArea = areas.FirstOrDefault(x => x.FileName == "level_cp10_chp1_1_010.scn.20");
            if (firstArea == null)
                return;

            var scnFile = firstArea.ScnFile;
            var inhibitor = scnFile.FindGameObject(new Guid("9fc712ca-478c-45b5-be12-5233edf4fe95"));
            if (inhibitor == null)
                return;

            var inhibitorComponent = inhibitor.Components[1];
            for (var i = 0; i < 5; i++)
            {
                inhibitorComponent.Set(
                    $"_Datas[{i}].Rule[0]._Enable.Matters[0]._Data.Flags._CheckFlags[0]._CheckFlag",
                    new Guid("0fb10e00-5384-4732-881a-af1fae2036c7"));
            }
        }

        private void FixDeadEnemyCounters(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            logger.LogLine("Updating dead enemy counters");
            var areas = randomizer.Areas;
            foreach (var area in areas)
            {
                var scnFile = area.ScnFile;
                foreach (var go in scnFile.IterAllGameObjects(true))
                {
                    var component = go.Components.FirstOrDefault(x => x.Name.StartsWith("chainsaw.DeadEnemyCounter"));
                    if (component != null)
                    {
                        var targetIds = component.GetFieldValue("_CountTargetIDs") as List<object>;
                        if (targetIds != null)
                        {
                            targetIds.Clear();
                            foreach (var id in _characterKindIds)
                            {
                                targetIds.Add(id);
                            }
                        }
                    }
                }
            }
        }

        private void FixSpawnControllers(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            logger.LogLine("Updating spawn controllers");
            var areas = randomizer.Areas;

            var throneRoomArea = areas.FirstOrDefault(x => x.FileName == "level_cp10_chp3_1_002.scn.20");
            if (throneRoomArea != null)
            {
                var component = throneRoomArea.ScnFile.FindComponent(new Guid("b1729389-c445-4c24-b500-72007144dfe6"), "chainsaw.CharacterSpawnController");
                if (component != null)
                {
                    var controller = new CharacterSpawnController(component);
                    controller.SpawnCondition.Add(throneRoomArea.ScnFile, new Guid("0ef6f99b-43f7-41de-b22a-be79b599a469"));
                }
            }

            var checkpointArea = areas.FirstOrDefault(x => x.FileName == "level_loc47_002.scn.20");
            if (checkpointArea != null)
            {
                var component = checkpointArea.ScnFile.FindComponent(new Guid("31f4c494-ea57-41dd-a209-52a6ddbc9423"), "chainsaw.CharacterSpawnController");
                if (component != null)
                {
                    var controller = new CharacterSpawnController(component);
                    controller.SpawnCondition.Flags = controller.SpawnCondition.Flags
                        .RemoveAll(x => x.Flag == new Guid("6ac9f5b8-a8a6-4e43-9410-54908e542128"));
                }
            }
        }

        private void EnableProfessionalAutoSave(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            logger.LogLine("Updating auto saves");
            var areas = randomizer.Areas;
            foreach (var area in areas)
            {
                foreach (var go in area.ScnFile.IterAllGameObjects(true))
                {
                    var autoSaveSetting = go.FindComponent("chainsaw.AutoSaveSetting");
                    if (autoSaveSetting != null)
                    {
                        autoSaveSetting.Set("_SaveOnPro", true);
                    }
                }
            }
        }

        private void SlowDownFactoryDoor(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string scnPath = "natives/stm/_chainsaw/environment/scene/gimmick/st44/gimmick_st44_210_p000.scn.20";

            logger.LogLine("Slow down factory door");

            var fileRepository = randomizer.FileRepository;
            var scn = fileRepository.GetScnFile(scnPath);
            if (scn == null)
                return;

            var wheelObject = scn.FindGameObject(new Guid("f6ab6635-ec2f-420c-8d9b-c14583ce30a4"));
            if (wheelObject == null)
                return;

            var holdHandleComponent = wheelObject.FindComponent("chainsaw.GmHoldHandle");
            if (holdHandleComponent == null)
                return;

            const float speed = 0.025f;
            holdHandleComponent.Set("_ReduceProcess", speed);
            holdHandleComponent.Set("_ReduceProcessLv2", speed);
            holdHandleComponent.Set("_ReduceProcessLv3", speed);

            fileRepository.SetScnFile(scnPath, scn);
        }

        private void IncreaseJetSkiTimer(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string userFilePath = "natives/stm/_chainsaw/appsystem/ui/userdata/guiparamholdersettinguserdata.user.2";
            const float updatedTimerSeconds = 7 * 60;

            logger.LogLine($"Set jet ski timer to {updatedTimerSeconds} seconds");

            var fileRepository = randomizer.FileRepository;
            var userFile = fileRepository.GetUserFile(userFilePath);
            if (userFile == null)
                return;

            var timerSettings = userFile.RSZ!.ObjectList[0].Get<RszInstance>("_TimerGuiParamHolder._TimerParamSettings[0]")!;
            timerSettings.Set("_MaxSecond", updatedTimerSeconds);
            timerSettings.Set("_RespawnTimer", updatedTimerSeconds);
            foreach (var i in new[] { 10, 20, 30, 40 })
            {
                var sub = $"_TimerParam_Defficulty{i}";
                var subObject = timerSettings.Get<RszInstance>(sub)!;
                subObject.Set("MaxSecond", updatedTimerSeconds);
                subObject.Set("RespawnTimer", updatedTimerSeconds);
            }

            fileRepository.SetUserFile(userFilePath, userFile);
        }

        private void ImproveKnightyKnightKnightRoom(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var area = randomizer.Areas.FirstOrDefault(x => x.FileName == "level_cp10_chp3_3_007.scn.20");
            if (area == null)
                return;

            // Knights become active by triggering their force find flag.
            // This is won't work for other enemies, so instead have them only spawn in
            // once the lion head has been picked up.
            var controllerGuids = new[] {
                new Guid("8ea3614a-e6bb-4ee3-94ed-e41a459e4303"), // easy
                new Guid("f47d8cbc-15ed-4a06-b20f-a307c09d678e") // hard
            };

            var scn = area.ScnFile;
            foreach (var controllerGuid in controllerGuids)
            {
                var spawnControllerComponent = scn.FindComponent(controllerGuid, "chainsaw.CharacterSpawnController");
                if (spawnControllerComponent != null)
                {
                    var controller = new CharacterSpawnController(spawnControllerComponent);
                    controller.SpawnCondition.Add(scn, new Guid("6ac0d9ef-16d3-46e6-af89-4efb1f8370ac"));
                }
            }
        }

        private void AllowLaserSightOnAnything(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string weaponPartsCombineDefinitionPath = "natives/stm/_chainsaw/appsystem/ui/userdata/weaponpartscombinedefinitionuserdata.user.2";
            const string playerLaserSightControllerDefinitionPath = "natives/stm/_chainsaw/appsystem/weapon/lasersight/playerlasersightcontrolleruserdata.user.2";
            const string weaponDetailCustomPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";

            var weaponIds = new[] { 4002, 4003, 4004 };

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(weaponPartsCombineDefinitionPath, (file, rsz) =>
            {
                var list = rsz.GetList("_Datas[6]._TargetItemIds");
                list.Add(274838656);
                list.Add(274840256);
                list.Add(274841856);
            });

            fileRepository.ModifyUserFile(playerLaserSightControllerDefinitionPath, (file, rsz) =>
            {
                var list = rsz.GetList("_Settings");
                foreach (var wp in weaponIds)
                {
                    var newItem = file.CloneInstance((RszInstance)list[0]!);
                    newItem.Set("_WeaponID", wp);
                    list.Add(newItem);
                }
            });

            fileRepository.ModifyUserFile(weaponDetailCustomPath, (file, rsz) =>
            {
                var list = rsz.GetArray<RszInstance>("_WeaponDetailStages");
                var attachment = list[0].Get<RszInstance>("_WeaponDetailCustom._AttachmentCustoms[0]")!;
                foreach (var wp in weaponIds)
                {
                    foreach (var w in list)
                    {
                        if (w.Get<int>("_WeaponID") == wp)
                        {
                            var attachments = w.GetList("_WeaponDetailCustom._AttachmentCustoms");
                            attachments.Add(file.CloneInstance(attachment));
                        }
                    }
                }
            });
        }

        private void RandomizeFirstBearTrap(ChainsawRandomizer randomizer, RandomizerLogger logger, Rng rng)
        {
            const string scnPath = "natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_505_p000.scn.20";

            if (rng.NextProbability(50))
                return;

            logger.LogLine("Randomize first bear trap location");
            randomizer.FileRepository.ModifyScnFile(scnPath, scn =>
            {
                var bearTrapObject = scn.FindGameObject(new Guid("601d0ce7-ca40-40d0-bba9-73918a141a96"));
                if (bearTrapObject == null)
                    return;

                var transform = bearTrapObject.FindComponent("via.Transform");
                if (transform == null)
                    return;

                transform.Set("v0", new Vector4(-76.99f, 5.14f, 35.3336f, 0.0f));
            });
        }

        private void EnableInstantBuy(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string userFilePath = "natives/stm/_chainsaw/appsystem/ui/userdata/guiparamholdersettinguserdata.user.2";

            logger.LogLine($"Set purchase hold time to 0");

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(userFilePath, (file, rsz) =>
            {
                rsz.Set("_InGameShopGuiParamHolder._HoldTime_Purchase", 0.0f);
            });
        }

        private void ImproveBoltThrower(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string userFilePath = "natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2";

            logger.LogLine($"Make bolt thrower fully automatic");

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(userFilePath, (file, rsz) =>
            {
                rsz.Set("_DataTable[18]._WeaponStructureParam.TypeOfReload", 0);
                rsz.Set("_DataTable[18]._WeaponStructureParam.TypeOfShoot", 1);
            });
        }

        private static readonly int[] _characterKindIds = new int[]
        {
            100000,
            110000,
            199999,
            200000,
            200001,
            200002,
            200003,
            200004,
            200005,
            200006,
            200007,
            200008,
            200009,
            200010,
            200011,
            200012,
            200013,
            200014,
            200015,
            200016,
            200017,
            200018,
            200019,
            200020,
            200021,
            200022,
            200023,
            200024,
            200025,
            200026,
            200027,
            200028,
            200029,
            200030,
            200031,
            200032,
            200033,
            200034,
            200035,
            200036,
            200037,
            200038,
            200039,
            200040,
            200041,
            200042,
            200043,
            200044,
            200045,
            200046,
            200047,
            380000,
            600000,
            600001,
            600002,
            600003,
            600004,
            600005,
            80000,
            81000,
            81100,
            81101,
            81102,
            81103,
            81104,
            81105,
            81106,
            81107,
            81108,
            81109,
            500000,
        };
    }
}
