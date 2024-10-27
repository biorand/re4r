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

            // Once
            EnableInstantBuy(randomizer, logger);

            if (randomizer.Campaign == Campaign.Leon)
            {
                AllowLaserSightOnAnything(randomizer, logger);
                ImproveBoltThrower(randomizer, logger);
                DisableFirstAreaInhibitor(randomizer, logger);
                ForceNgPlusMerchantLeon(randomizer, logger);
                RandomizeFirstBearTrap(randomizer, logger, rng);
                SlowDownFactoryDoor(randomizer, logger);
                if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    ImproveKnightyKnightKnightRoom(randomizer, logger);
                }
                IncreaseJetSkiTimer(randomizer, logger);
            }
            else
            {
                ForceNgPlusMerchantAda(randomizer, logger);
                if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    ImproveBellTriggeredEnemies(randomizer, logger);
                    ImproveAdaKnightRoom(randomizer, logger);
                    ImproveAdaGarradorRoom(randomizer, logger);
                }
            }

            FixDeadEnemyCounters(randomizer, logger);
            FixSpawnControllers(randomizer, logger);
            if (randomizer.GetConfigOption<bool>("enable-autosave-pro"))
            {
                EnableProfessionalAutoSave(randomizer, logger);
            }
        }

        private void ForceNgPlusMerchantLeon(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var path = "natives/stm/_chainsaw/environment/scene/gimmick/st40/gimmick_st40_502_p000.scn.20";
            randomizer.FileRepository.ModifyScnFile(path, scn =>
            {
                scn.RemoveGameObject(new Guid("ca0ac85f-1238-49d9-a0fb-0d58a42487a1")); // merchant
                scn.RemoveGameObject(new Guid("4a975fc1-2e1c-4fd3-a49a-1f35d6a30f0f")); // merchant flame
            });
        }

        private void ForceNgPlusMerchantAda(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var path = "natives/stm/_anotherorder/environment/scene/gimmick/st50/gimmick_st50_501_ao.scn.20";
            randomizer.FileRepository.ModifyScnFile(path, scn =>
            {
                scn.RemoveGameObject(new Guid("41a87b99-d47f-438d-a686-f19e6865379e")); // merchant
                scn.RemoveGameObject(new Guid("33ba7a17-4b7d-4a23-b272-c5afcd62f3f1")); // merchant flame
                scn.RemoveGameObject(new Guid("bf5cc10b-ff6b-46be-99e3-814629dfcff8")); // typwriter
            });
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

        private void ImproveBellTriggeredEnemies(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var area = randomizer.Areas.FirstOrDefault(x => x.FileName == "level_loc45.scn.20");
            if (area == null)
                return;

            // Non-ganado enemies just spawn next to the church and can't be damaged.
            // So remove the no-damage control flags from them.
            var scn = area.ScnFile;
            var controllerGuids = new Guid[]
            {
                new Guid("56426f4d-01e8-4079-a8b8-3d4ee343b224"),
                new Guid("8d84b8ba-babc-4a73-8f02-3d4bd3974b9b")
            };

            foreach (var guid in controllerGuids)
            {
                var go = scn.FindGameObject(guid);
                if (go == null)
                    continue;

                foreach (var child in go.Children)
                {
                    var spawn = child.Components.FirstOrDefault(x => x.Name.Contains("SpawnParam"));
                    if (spawn == null)
                        continue;

                    var checkFlags = spawn.GetList("_NoDamageCtrlFlag._CheckFlags");
                    checkFlags.Clear();
                }
            }
        }

        private void ImproveAdaKnightRoom(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var area = randomizer.Areas.FirstOrDefault(x => x.FileName == "level_loc51_chp3_1.scn.20");
            if (area == null)
                return;

            var scn = area.ScnFile;

            // Top floor (triggered by silver bottle)
            var spawnControllerComponent = scn.FindComponent(new Guid("d82d24e0-cac6-471e-9b2e-808f84053fb9"), "chainsaw.CharacterSpawnController");
            if (spawnControllerComponent != null)
            {
                var controller = new CharacterSpawnController(spawnControllerComponent);
                controller.SpawnCondition.Add(scn, new Guid("b9a3aaa9-700c-4e5c-a31f-df66bfbda362"));
            }

            // Bottom floor (triggered by gold bottle)
            spawnControllerComponent = scn.FindComponent(new Guid("0d4eea6c-2722-43fd-b887-830fd4c915dd"), "chainsaw.CharacterSpawnWaveController");
            if (spawnControllerComponent != null)
            {
                var controller = new CharacterSpawnController(spawnControllerComponent);
                controller.SpawnCondition.Add(scn, new Guid("84b73ea9-8de6-492d-a479-45f988e06492"));
            }

            // There is one enemy that you can't seem to damage,
            // so disable all no damage flags.
            var noDamageEnemies = new Guid[]
            {
                new Guid("8168407b-a4f3-48fb-a553-39527eaab8ce"),
                new Guid("4a43992c-71b0-4a2e-ba91-9a588716f255"),
                new Guid("025c604b-e2ac-4c15-afc8-166d95a56618")
            };
            foreach (var guid in noDamageEnemies)
            {
                var go = scn.FindGameObject(guid);
                if (go == null)
                    continue;

                var spawn = go.Components.FirstOrDefault(x => x.Name.Contains("SpawnParam"));
                if (spawn == null)
                    continue;

                var checkFlags = spawn.GetList("_NoDamageCtrlFlag._CheckFlags");
                checkFlags.Clear();
            }

            // Remove the tsuitates (since they don't break if we switch out the armaduras)
            var gimmickPath = "natives/stm/_anotherorder/environment/scene/gimmick/st51/gimmick_st51_857_ao.scn.20";
            randomizer.FileRepository.ModifyScnFile(gimmickPath, scn2 =>
            {
                scn2.RemoveGameObject(new Guid("1f1c503b-b032-43da-9e3a-792960586343"));
                scn2.RemoveGameObject(new Guid("4b30301b-aca4-4199-9745-91acad51ece3"));
                scn2.RemoveGameObject(new Guid("b76e9b43-a4b7-4d8e-871a-30a67d34b544"));
                scn2.RemoveGameObject(new Guid("9502ec07-cdb8-4107-a4bc-9e76cc029ec0"));
                scn2.RemoveGameObject(new Guid("581cace6-a401-42e0-9dd7-383aafbdd551"));
                scn2.RemoveGameObject(new Guid("06815784-ae02-49b4-b3d6-c7528fcf3d54"));
                scn2.RemoveGameObject(new Guid("ace7c27b-1cf1-4bb7-bb68-798101d596ef"));
                scn2.RemoveGameObject(new Guid("9c389ab6-c2b3-488e-b9a9-304ef4831d59"));
            });
        }

        private void ImproveAdaGarradorRoom(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var area = randomizer.Areas.FirstOrDefault(x => x.FileName == "level_loc55.scn.20");
            if (area == null)
                return;

            // Spawn enemies when doors shut, otherwise enemies (originally garradors)
            // can't be hurt and they leave the area and attack you prematurely.
            var controllerGuids = new[] {
                new Guid("f5402bf4-4c55-4332-86c0-53851701e532"), // standard
                new Guid("4924d5ff-3905-421f-b6b3-1d30d900be95") // pro
            };

            var scn = area.ScnFile;
            foreach (var controllerGuid in controllerGuids)
            {
                var spawnControllerComponent = scn.FindComponent(controllerGuid, "chainsaw.CharacterSpawnController");
                if (spawnControllerComponent != null)
                {
                    var controller = new CharacterSpawnController(spawnControllerComponent);
                    controller.SpawnCondition.Add(scn, new Guid("40807771-38e9-4ec8-a240-d75f4fdff461"));
                }
            }
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
