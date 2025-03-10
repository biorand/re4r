﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using Namsku.REE.Messages;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class FixesModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();

            // Once
            SetBuyHoldTime(randomizer, logger);
            EnableEarlyUpgrades(randomizer, logger);

            if (randomizer.Campaign == Campaign.Leon)
            {
                DisableFirstAreaInhibitor(randomizer, logger);
                ForceNgPlusMerchantLeon(randomizer, logger);
                RandomizeFirstBearTrap(randomizer, logger, rng);
                SlowDownFactoryDoor(randomizer, logger);
                if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    ImproveKnightyKnightKnightRoom(randomizer, logger);
                }
                IncreaseJetSkiTimer(randomizer, logger);
                FixCharmDescriptions(randomizer, logger);
            }
            else
            {
                ForceNgPlusMerchantAda(randomizer, logger);
                if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    ImproveBellTriggeredEnemies(randomizer, logger);
                    ImproveAdaKnightRoom(randomizer, logger);
                    ImproveAdaMaze(randomizer, rng, logger);
                    ImproveAdaGarradorRoom(randomizer, logger);
                }
            }

            if (randomizer.GetConfigOption<bool>("automatic-bolt-thrower", true))
            {
                ImproveBoltThrower(randomizer, logger);
            }
            AllowLaserSightOnAnything(randomizer, logger);
            FixDeadEnemyCounters(randomizer, logger);
            FixSpawnControllers(randomizer, logger);
            if (randomizer.GetConfigOption<bool>("enable-autosave-pro"))
            {
                EnableProfessionalAutoSave(randomizer, logger);
            }

            ChangeMessages(randomizer, logger);
            FixNovisNavigation(randomizer, logger);
            FixAddedWeaponNames(randomizer, logger);
            FixEnemyHp(randomizer, rng, logger);
            FixEnemyWeaponDamage(randomizer, logger);
            // FixSmallKeySellable(randomizer, logger);
            FixSentinelNineIssue(randomizer, logger);
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
                    if (component != null && component.Get<bool>("_HasCountTargetIDs"))
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

        private void EnableEarlyUpgrades(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileRepository = randomizer.FileRepository;
            if (randomizer.Campaign == Campaign.Leon)
            {
                var path = "natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopupdateflagcataloguserdata.user.2";
                fileRepository.ModifyUserFile(path, (rsz, root) =>
                {
                    for (var i = 0; i <= 2; i++)
                    {
                        root.GetList($"_Datas[{i}]._Flags").Add(0);
                        root.GetList($"_Datas[{i}]._SaleFlags").Add(0);
                    }
                });
            }
            else
            {
                var path = "natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopupdateflagcataloguserdata_ao.user.2";
                fileRepository.ModifyUserFile(path, (rsz, root) =>
                {
                    root.GetList($"_Datas[18]._Flags").Add(17);
                    root.GetList($"_Datas[18]._SaleFlags").Add(17);
                });
            }
        }

        private void AllowLaserSightOnAnything(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var weaponPartsCombineDefinitionPath = "natives/stm/_chainsaw/appsystem/ui/userdata/weaponpartscombinedefinitionuserdata.user.2";
            var playerLaserSightControllerDefinitionPath = "natives/stm/_chainsaw/appsystem/weapon/lasersight/playerlasersightcontrolleruserdata.user.2";
            var weaponDetailCustomPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";

            var weaponIds = new[] { 4002, 4003, 4004 };

            if (randomizer.Campaign == Campaign.Ada)
            {
                weaponPartsCombineDefinitionPath = "natives/stm/_anotherorder/appsystem/ui/userdata/weaponpartscombinedefinitionuserdata_ao.user.2";
                weaponDetailCustomPath = "natives/stm/_anotherorder/appsystem/weaponcustom/weapondetailcustomuserdata_ao.user.2";
                weaponIds = [6103, 6113];
            }

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(weaponPartsCombineDefinitionPath, (file, rsz) =>
            {
                var list = rsz.GetList("_Datas[6]._TargetItemIds");
                if (randomizer.Campaign == Campaign.Leon)
                {
                    list.Add(274838656); // Red9
                    list.Add(274840256); // Blacktail
                    list.Add(274841856); // Matilda
                }
                else
                {
                    list.Add(278200256); // SW - Blacktail AC
                    list.Add(278216256); // SW - Red 9
                }
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
                            if (!attachments.Any(x => ((RszInstance)x!).Get<int>("_ItemID") == 116008000))
                            {
                                attachments.Add(file.CloneInstance(attachment));
                            }
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

        private void SetBuyHoldTime(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            const string userFilePath = "natives/stm/_chainsaw/appsystem/ui/userdata/guiparamholdersettinguserdata.user.2";
            var time = randomizer.GetConfigOption<double>("merchant-buy-hold-time", 0.6);
            if (time != 0.6)
            {
                time = Math.Clamp(time, 0, 1);

                logger.LogLine($"Set purchase hold time to {time:0.00}");
                var fileRepository = randomizer.FileRepository;
                fileRepository.ModifyUserFile(userFilePath, (file, rsz) =>
                {
                    rsz.Set("_InGameShopGuiParamHolder._HoldTime_Purchase", (float)time);
                });
            }
        }

        private void ImproveBoltThrower(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var userFilePath = randomizer.Campaign == Campaign.Leon
                ? "natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2"
                : "natives/stm/_anotherorder/appsystem/weapon/weaponequipparamcataloguserdata_ao.user.2";
            var index = randomizer.Campaign == Campaign.Leon ? 18 : 19;

            logger.LogLine($"Make bolt thrower fully automatic");

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(userFilePath, (file, rsz) =>
            {
                rsz.Set($"_DataTable[{index}]._WeaponStructureParam.TypeOfReload", 0);
                rsz.Set($"_DataTable[{index}]._WeaponStructureParam.TypeOfShoot", 1);
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
            if (!randomizer.GetConfigOption<bool>("random-enemies"))
                return;

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
                controller.SpawnSkipCondition.Clear();
                controller.SpawnSkipCondition.Add(scn, new Guid("84b73ea9-8de6-492d-a479-45f988e06492"));
            }

            // Bottom floor (triggered by gold bottle)
            // Transform wave controller to normal controller
            {
                // Remove old wave spawn controller
                var gameObject = scn.FindGameObject(new Guid("0d4eea6c-2722-43fd-b887-830fd4c915dd"))!;
                var normalTransform = gameObject.FindComponent("via.Transform")!;
                gameObject.Components.RemoveAt(1);
                scn.RemoveGameObject(gameObject);

                // Remove all the enemies
                var enemies = gameObject.Children.SelectMany(x => x.Children).ToArray();

                // Create 3 new controllers
                var waveFlags = new[]
                {
                    new Guid("84b73ea9-8de6-492d-a479-45f988e06492"),
                    new Guid("d2366665-7671-4ac1-8d94-d4cbc4e2b06e"),
                    new Guid("1f733c2d-fafd-4eaa-9ef9-e6ea313dff6d"),
                };
                var controllerObjects = new List<ScnFile.GameObjectData>();
                for (var i = 0; i < 3; i++)
                {
                    var newGameObject = scn.CreateGameObject($"Biorand_1F_{i}");
                    controllerObjects.Add(newGameObject);

                    newGameObject.Components.Add((RszInstance)normalTransform.Clone()!);

                    var controllerComponent = scn.RSZ!.CreateInstance("chainsaw.CharacterSpawnController");
                    scn.AddComponent(newGameObject, controllerComponent);

                    var controller = new CharacterSpawnController(controllerComponent);
                    controller.Enabled = true;
                    controller.Difficulty = 63;
                    controller.Guid = Guid.NewGuid();
                    controller.SpawnCondition.Add(scn, waveFlags[i]);

                    for (var j = 0; j < enemies.Length; j++)
                    {
                        if ((j % 3) != i)
                            continue;

                        var e = enemies[j];
                        newGameObject.Children.Add(e);
                        e.Parent = newGameObject;

                        var spawn = e.Components.FirstOrDefault(x => x.Name.Contains("SpawnParam"));
                        if (spawn != null)
                        {
                            // Make sure no damage flag is removed
                            var checkFlags = spawn.GetList("_NoDamageCtrlFlag._CheckFlags");
                            checkFlags.Clear();

                            // Enable force find, but without any conditions
                            spawn.Set("_ForceFind", true);
                            var flagCondition = new FlagCondition(spawn.Get<RszInstance>("_ForceFindCondition._ForceFindCondition")!);
                            flagCondition.Or = false;
                            flagCondition.Flags = [];
                        }
                    }
                }

                // Change dead enemy counter to check controllers instead of enemy types
                var deadEnemyCounter = scn.FindComponent(new Guid("0546811f-8274-4dd3-8655-e9bbee0a23d8"), "chainsaw.DeadEnemyCounter")!;
                deadEnemyCounter.Set("_HasStartFlag", false);
                deadEnemyCounter.Set("_StartFlag", Guid.Empty);
                deadEnemyCounter.Set("_HasCountTargetIDs", false);
                deadEnemyCounter.GetList("_CountTargetIDs").Clear();
                deadEnemyCounter.Set("_HasCountTargetSpawnControllers", true);
                var lst = deadEnemyCounter.GetList("_CountTargetSpawnControllers");
                var dataList = deadEnemyCounter.GetArray<RszInstance>("_DataList");
                var tally = 0;
                for (var i = 0; i < controllerObjects.Count; i++)
                {
                    lst.Add(controllerObjects[i].Guid);
                    tally += controllerObjects[i].Children.Count;
                    dataList[i].Set("_Num", tally - 1);
                }
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

        private void ImproveAdaMaze(ChainsawRandomizer randomizer, Rng rng, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-enemies"))
                return;

            var area = randomizer.Areas.FirstOrDefault(x => x.FileName == "level_loc51_chp3_1.scn.20");
            if (area == null)
                return;

            var keyHolderGuid = new Guid("1a4b7f3e-01fe-47b4-a23d-628aa94b5978");
            var keyHolder = area.Enemies.FirstOrDefault(x => x.Guid == keyHolderGuid);
            if (keyHolder != null)
            {
                const int KindNone = 0;
                const int KindAny = 1;
                const int KindSmall = 1;
                var positions = new[]
                {
                    (KindNone, 0, 0, 0, 0),
                    (KindAny, 86, 27, 18, -88),
                    (KindAny, 54, 21, 36, -179),
                    (KindAny, 62, 21, 47, 92),
                    (KindAny, 82, 21, 42, -159),
                    (KindAny, 82, 21, 48, 5),
                    (KindAny, 66, 21, 32, 114),
                    (KindAny, 85, 21, 22, -123),
                    (KindSmall, 92, 21, 30, 4),
                    (KindSmall, 73, 21, 28, -172),
                    (KindSmall, 47, 21, 55, -72),
                    (KindSmall, 57, 24, 36, 89),
                    (KindSmall, 72, 21, 44, -88),
                    (KindSmall, 72, 21, 37, -86)
                };
                var largeEnemies = new[] { "mendez_chase", "verdugo", "mendez_2", "krauser_2", "pesanta", "u3", "garrador" };
                if (largeEnemies.Contains(keyHolder.Kind.Key))
                {
                    positions = positions.Where(x => x.Item1 != KindSmall).ToArray();
                }
                var (kind, x, y, z, d) = rng.NextOf(positions);
                if (kind != KindNone)
                {
                    var transform = new Transform(keyHolder.GameObject);
                    transform.Position = new Vector3(x, y, z);
                    transform.Eular = new EulerAngles(d, 0, 0);
                }
            }
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
                var gameObject = scn.FindGameObject(controllerGuid)!;
                var spawnControllerComponent = gameObject.FindComponent("chainsaw.CharacterSpawnController");
                if (spawnControllerComponent != null)
                {
                    var controller = new CharacterSpawnController(spawnControllerComponent);
                    controller.SpawnCondition.Add(scn, new Guid("40807771-38e9-4ec8-a240-d75f4fdff461"));
                }

                foreach (var child in gameObject.Children)
                {
                    var transform = child.FindComponent("via.Transform")!;
                    var position = transform.Get<Vector4>("v0");
                    position.X = 152;
                    transform.Set("v0", position);
                }
            }
        }

        private void ChangeMessages(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.HasSpecialTouch("bawk"))
                return;

            const string itemNamePath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22";
            const string itemDescPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22";

            var fileRepository = randomizer.FileRepository;
            var msg = fileRepository.GetMsgFile(itemNamePath).ToBuilder();
            msg.SetStringAll(new Guid("fcac600e-8386-4221-906c-004c37c7f2b2"), "Soup Trooper Egg");
            msg.SetStringAll(new Guid("0588065f-4b31-40ca-8ff0-3789a1e23e8f"), "Soup Stirrer Egg");
            msg.SetStringAll(new Guid("9a941943-186f-4050-9e28-71bce241be54"), "Bawkbasoup Egg");
            fileRepository.SetMsgFile(itemNamePath, msg.ToMsg());

            msg = fileRepository.GetMsgFile(itemDescPath).ToBuilder();
            msg.SetStringAll(new Guid("7c4d5ec3-76ab-4e1c-a4aa-5dd704b252da"), "A Soup Trooper egg. Can be used to restore a sloppy amount of health.");
            msg.SetStringAll(new Guid("6c76bdbf-d110-4faa-8a0a-fc2a4d098ea0"), "A Soup Stirrer egg. Can be used to avoid 1998.");
            msg.SetStringAll(new Guid("8adebd37-0254-4889-9706-c150e06e3603"), "A highly valued Bawkbasoup egg. Can be used to restore a poggers amount of health.");
            fileRepository.SetMsgFile(itemDescPath, msg.ToMsg());
        }

        private void FixCharmDescriptions(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileRepository = randomizer.FileRepository;

            var charmStatusPath = "natives/stm/_chainsaw/appsystem/ui/userdata/charmeffectsettinguserdata.user.2";
            var itemMessagePath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemmessageidsettinguserdata.user.2";
            var itemMsgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22";
            var statusMsgPath = "natives/stm/_chainsaw/message/mes_main_charm/ch_mes_main_statuseffect.msg.22";

            var charmStatus = fileRepository.DeserializeUserFile<chainsaw.CharmEffectSettingUserdata>(charmStatusPath);
            var itemMessage = fileRepository.DeserializeUserFile<chainsaw.ItemMessageIdSettingUserdata>(itemMessagePath);
            var statusMsg = fileRepository.GetMsgFile(statusMsgPath);
            var statusMsgBuilder = fileRepository.GetMsgFile(statusMsgPath).ToBuilder();
            var itemMsg = fileRepository.GetMsgFile(itemMsgPath).ToBuilder();
            foreach (var item in itemMessage._Settings)
            {
                var status = charmStatus._Settings.FirstOrDefault(x => x._ItemId == item._ItemId);
                if (status == null)
                    continue;

                var effect = status._Effects[0];
                var effectMsgName = $"CH_Mes_Main_StatusEffectID_{effect._StatusEffectID:00_000_000_0}";
                var effectMsg = statusMsg.GetString(effectMsgName, LanguageId.English) ?? "(no string)";
                var formattedMsg = string.Format(effectMsg, effect._Value);
                itemMsg.SetStringAll(item._CaptionMsgId, formattedMsg);
                if (statusMsgBuilder.Entries.Any(x => x.Name == effectMsgName))
                {
                    statusMsgBuilder.SetStringAll(effectMsgName, formattedMsg);
                }
            }
            fileRepository.SetMsgFile(itemMsgPath, itemMsg.ToMsg());
            fileRepository.SetMsgFile(statusMsgPath, statusMsgBuilder.ToMsg());
        }

        private void FixNovisNavigation(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var pathFormatLeon = "natives/stm/_chainsaw/appsystem/navigation/loc{0}/navigation_loc{1}.scn.20";
            var pathFormatAda = "natives/stm/_anotherorder/appsystem/navigation/loc{0}/navigation_loc{1}.scn.20";
            var locationsLeon = new[]
            {
                4000, 4010, 4011, 4300, 4310, 4400, 4410, 4500, 4510, 4600, 4610, 4700, 4710,
                5000, 5010, 5100, 5110, 5200, 5300, 5400, 5410, 5500, 5510, 5600, 5610, 5700, 5900,
                6000, 6010, 6100, 6110, 6200, 6300, 6400, 6500, 6600, 6610, 6700, 6701, 6800, 6801, 6900,
            };
            var locationsAda = new[]
            {
                4010, 4300, 4400, 4410, 4500, 4700,
                5000, 5100, 5110, 5500, 5510, 5600, 5610, 5900,
                6010, 6100, 6110, 6200, 6400, 6800, 6900,
            };

            var pathFormat = randomizer.Campaign == Campaign.Leon ? pathFormatLeon : pathFormatAda;
            var locations = randomizer.Campaign == Campaign.Leon ? locationsLeon : locationsAda;
            var rootName = randomizer.Campaign == Campaign.Leon ? "AIMap" : "AIMap_AO";
            foreach (var loc in locations)
            {
                var path = string.Format(pathFormat, loc / 100, loc);
                randomizer.FileRepository.ModifyScnFile(path, scn =>
                {
                    var obj = scn.IterAllGameObjects().First(x => x.Name == rootName);
                    var navigationMapClient = obj.FindComponent("chainsaw.NavigationMapClient");
                    var bindInfoList = navigationMapClient!.GetList("_BindInfoList");
                    if (bindInfoList.Count < 3)
                    {
                        var bindInfo = scn.RSZ!.CreateInstance("chainsaw.NavigationMapClient.BindInfo");
                        bindInfo.Set("_Purpose", 1);
                        bindInfo.Set("_MapName", $"VolumeSpace_Loc{loc}");
                        bindInfoList.Add(bindInfo);
                    }
                });
            }
        }

        private void FixAddedWeaponNames(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (randomizer.Campaign == Campaign.Ada)
                return;

            var fileRepository = randomizer.FileRepository;

            var itemMessagePath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemmessageidsettinguserdata.user.2";
            var itemCaptionPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22";
            var itemNamePath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22";

            var itemMessage = fileRepository.DeserializeUserFile<chainsaw.ItemMessageIdSettingUserdata>(itemMessagePath);
            var itemCaption = fileRepository.GetMsgFile(itemCaptionPath).ToBuilder();
            var itemName = fileRepository.GetMsgFile(itemNamePath).ToBuilder();

            var wp6100 = itemMessage._Settings.FirstOrDefault(x => x._ItemId == ItemIds.SWSawedOffW870);
            if (wp6100 != null)
            {
                wp6100._NameMsgId = itemName.Create("Sawed-off W-870").Guid;
                wp6100._CaptionMsgId = itemCaption.Create("A pump-action shotgun designed for close encounters.\r\nIts sawed-off barrel makes it very versatile in combat.").Guid;
            }

            var wp6300 = itemMessage._Settings.FirstOrDefault(x => x._ItemId == ItemIds.XM96E1);
            if (wp6300 != null)
            {
                wp6300._NameMsgId = itemName.Create("XM96E1").Guid;
                wp6300._CaptionMsgId = itemCaption.Create("A rugged handgun with decent firepower.\r\nIt has a low ammo capacity but hits hard.").Guid;
            }

            fileRepository.SerializeUserFile(itemMessagePath, itemMessage);
            fileRepository.SetMsgFile(itemCaptionPath, itemCaption.ToMsg());
            fileRepository.SetMsgFile(itemNamePath, itemName.ToMsg());
        }

        private void FixEnemyHp(ChainsawRandomizer randomizer, Rng rng, RandomizerLogger logger)
        {
            var bruteHpPaths = randomizer.Campaign == Campaign.Leon
                ? ["natives/stm/_chainsaw/appsystem/character/ch1c0z0/userdata/ch1c0z0enhancedhp.user.2"]
                : new[] {
                    "natives/stm/_anotherorder/appsystem/character/ch1c0z0/userdata/ch1c0z0enhancedhp_cp11.user.2",
                    "natives/stm/_anotherorder/appsystem/character/ch1c0z1/userdata/ch1c0z1enhancedhp_cp11.user.2",
                    "natives/stm/_anotherorder/appsystem/character/ch1c0z2/userdata/ch1c0z2enhancedhp_cp11.user.2"
                };

            var regeneradorPath = randomizer.Campaign == Campaign.Leon
                ? "natives/stm/_chainsaw/appsystem/character/ch1d4z0/userdata/ch1d4z0paramuserdata.user.2"
                : "natives/stm/_anotherorder/appsystem/character/ch1d4z0/userdata/ch1d4z0paramuserdata_ao.user.2";

            var pesantaPath = "natives/stm/_anotherorder/appsystem/character/ch4faz1/userdata/ch4faz1chapterhp.user.2";
            var u3Path = "natives/stm/_anotherorder/appsystem/character/ch4fbz0/userdata/ch4fbz0paramuserdata.user.2";

            var fileRepository = randomizer.FileRepository;

            // Fix brutes HP
            foreach (var bruteHpPath in bruteHpPaths)
            {
                SetChapterHp(randomizer, bruteHpPath,
                    randomizer.GetConfigOption<int>("enemy-health-min-brute_weapon"),
                    randomizer.GetConfigOption<int>("enemy-health-max-brute_weapon"));
            }

            // Fix super iron maiden
            fileRepository.ModifyUserFile(regeneradorPath, (rsz, root) =>
            {
                root.Set("STRUCT__StrongTransformedHitPoint__HasValue", false);
            });

            // Fix Pesanta
            if (randomizer.Campaign == Campaign.Ada && randomizer.GetConfigOption<bool>("boss-random-health"))
            {
                SetChapterHp2(randomizer, pesantaPath, 30100, rng.Next(
                    randomizer.GetConfigOption<int>("boss-health-min-pesanta-1"),
                    randomizer.GetConfigOption<int>("boss-health-max-pesanta-1") + 1));
            }

            // Fix U3
            fileRepository.ModifyUserFile(u3Path, (rsz, root) =>
            {
                root.Set("STRUCT__SecondFormHitPoint__HasValue", false);
            });
        }

        private static void SetChapterHp(ChainsawRandomizer randomizer, string path, int minHp, int maxHp)
        {
            var progressiveDifficulty = randomizer.GetConfigOption("enemy-health-progressive-difficulty", false);
            var fileRepository = randomizer.FileRepository;
            var ecpud = fileRepository.DeserializeUserFile<chainsaw.EnemyChapterParamUserData>(path);
            if (randomizer.Campaign == Campaign.Leon)
                ecpud._ChapterParamList.RemoveAll(x => x._ChapterID < 30000);
            else
                ecpud._ChapterParamList.RemoveAll(x => x._ChapterID >= 30000);
            var chapters = ChapterId.GetAll(randomizer.Campaign);
            var numChapters = ChapterId.GetCount(randomizer.Campaign);
            for (var chapter = 1; chapter <= numChapters; chapter++)
            {
                var windowStart = (chapter - 1) / (double)numChapters;
                var windowEnd = chapter / (double)numChapters;
                if (!progressiveDifficulty)
                {
                    windowStart = 0;
                    windowEnd = 1;
                }

                var windowSize = windowEnd - windowStart;
                var numTableEntries = progressiveDifficulty ? 4 : 8;
                var hpValueIncrement = (windowEnd - windowStart) / numTableEntries;
                var hpValues = Enumerable
                    .Range(0, numTableEntries)
                    .Select(x => (float)Math.Round(lerp(minHp, maxHp, windowStart + (x * windowSize / numTableEntries))))
                    .ToArray();

                ecpud._ChapterParamList.Add(new chainsaw.EnemyChapterParamUserData.ChapterParamElement()
                {
                    _ChapterID = ChapterId.FromNumber(randomizer.Campaign, chapter),
                    _RandomTable = hpValues.Select(x => new chainsaw.EnemyChapterParamUserData.RandomTableElement()
                    {
                        Weight = 100.0f / numTableEntries,
                        Value = x
                    }).ToList()
                });
            }
            fileRepository.SerializeUserFile(path, ecpud);

            static double lerp(double a, double b, double t) => a + ((b - a) * t);
        }

        private static void SetChapterHp2(ChainsawRandomizer randomizer, string path, int chapterId, int hp)
        {
            var fileRepository = randomizer.FileRepository;
            var ecpud = fileRepository.DeserializeUserFile<chainsaw.EnemyChapterParamUserData>(path);
            var chapter = ecpud._ChapterParamList.FirstOrDefault(x => x._ChapterID == chapterId);
            if (chapter == null)
            {
                chapter = new chainsaw.EnemyChapterParamUserData.ChapterParamElement();
                ecpud._ChapterParamList.Add(chapter);
            }
            chapter._RandomTable.Clear();
            chapter._RandomTable.Add(new chainsaw.EnemyChapterParamUserData.RandomTableElement()
            {
                Value = hp,
                Weight = 100
            });
            fileRepository.SerializeUserFile(path, ecpud);
        }

        private void FixEnemyWeaponDamage(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var files = new[]
            {
                "natives/stm/_chainsaw/appsystem/character/ch1c0z0/userdata/ch1c0z0enhancedweapondamagerateuserdatahead.user.2",
                "natives/stm/_chainsaw/appsystem/character/ch1c0z0/userdata/ch1c0z0weapondamagerateuserdatahead.user.2",
                "natives/stm/_chainsaw/appsystem/character/ch1c0z0/userdata/ch1e2z0weapondamagerateuserdatahead.user.2",
                "natives/stm/_chainsaw/appsystem/character/ch1c0z0/userdata/ch1e3z0weapondamagerateuserdatahead.user.2"
            };

            var fileRepository = randomizer.FileRepository;
            foreach (var f in files)
            {
                var userData = fileRepository.DeserializeUserFile<chainsaw.CharacterWeaponDamageRateUserData>(f);
                var wp4000 = userData._DataList.First(x => x._WeaponID == 4000); // SG
                var wp4200 = userData._DataList.First(x => x._WeaponID == 4200); // TMP
                SetEntry(userData, 4201, wp4200);
                SetEntry(userData, 6300, wp4000);
                fileRepository.SerializeUserFile(f, userData);
            }

            static void SetEntry(
                chainsaw.CharacterWeaponDamageRateUserData userData,
                int wp,
                chainsaw.CharacterWeaponDamageRateUserData.Data src)
            {
                var result = new chainsaw.CharacterWeaponDamageRateUserData.Data()
                {
                    _WeaponID = wp,
                    STRUCT__DamageRate__HasValue = src.STRUCT__DamageRate__HasValue,
                    STRUCT__DamageRate__Value = src.STRUCT__DamageRate__Value,
                    STRUCT__WinceRate__HasValue = src.STRUCT__WinceRate__HasValue,
                    STRUCT__WinceRate__Value = src.STRUCT__WinceRate__Value,
                    STRUCT__BreakRate__HasValue = src.STRUCT__BreakRate__HasValue,
                    STRUCT__BreakRate__Value = src.STRUCT__BreakRate__Value,
                    STRUCT__StoppingRate__HasValue = src.STRUCT__StoppingRate__HasValue,
                    STRUCT__StoppingRate__Value = src.STRUCT__StoppingRate__Value,
                    _Probability = src._Probability
                };

                var index = userData._DataList.FindIndex(x => x._WeaponID == wp);
                if (index == -1)
                    userData._DataList.Add(result);
                else
                    userData._DataList[index] = result;
            }
        }

        private void FixSmallKeySellable(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var path = randomizer.Campaign == Campaign.Leon
                ? "natives/stm/_chainsaw/appsystem/ui/userdata/sellablekeyitemuserdata.user.2"
                : "natives/stm/_anotherorder/appsystem/ui/userdata/sellablekeyitemuserdata_ao.user.2";

            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(path, (rsz, root) =>
            {
                var list = root.GetArray<RszInstance>("Datas");
                foreach (var l in list)
                {
                    var id = l.Get<int>("ID");
                    if (id != ItemIds.SmallKey)
                        continue;

                    l.Set("Sellable[0]._Enable.Matters[0]._Data.Compare", 1);
                    l.Set("Sellable[0]._Enable.Matters[0]._Data.Chapter", -1);
                }
            });
        }

        private void FixSentinelNineIssue(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (randomizer.GetConfigOption<bool>("allow-dlc-items"))
                return;

            if (randomizer.Campaign == Campaign.Leon)
                return;

            // Remove DLC items from catalog since they cause black screen on SW
            var path = "natives/stm/_anotherorder/appsystem/weapon/weaponcataloguserdata_ao.user.2";
            randomizer.FileRepository.ModifyUserFile(path, (rsz, root) =>
            {
                var list = root.GetList("_DataTable");
                for (var i = 0; i < list.Count; i++)
                {
                    var weaponId = ((RszInstance)list[i]!).Get<int>("_WeaponID");
                    if (weaponId == 6000 || weaponId == 6001)
                    {
                        list.RemoveAt(i);
                        i--;
                    }
                }
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
