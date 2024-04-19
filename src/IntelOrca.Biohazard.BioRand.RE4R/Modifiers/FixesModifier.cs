using System;
using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class FixesModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            StaticChanges(randomizer, logger);
            DisableFirstAreaInhibitor(randomizer, logger);
            FixDeadEnemyCounters(randomizer, logger);
            SlowDownFactoryDoor(randomizer, logger);
            if (randomizer.GetConfigOption<bool>("enable-autosave-pro"))
            {
                EnableProfessionalAutoSave(randomizer, logger);
            }
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
