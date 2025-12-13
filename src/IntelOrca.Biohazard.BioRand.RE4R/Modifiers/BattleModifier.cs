using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class BattleModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("battle-arenas"))
            {
                var battleEnemies = randomizer.EnemyService.EnemyPlacements
                    .Where(x => !string.IsNullOrEmpty(x.Battle))
                    .ToArray();
                randomizer.EnemyService.Remove(battleEnemies);
                return;
            }

            var areaService = randomizer.AreaService;

            var battleCsv = randomizer.DynamicData.GetData(DynamicDataName.Battle) ?? throw new Exception("Battle data not found");
            var battles = Csv.Deserialize<BattleParameter>(battleCsv)
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .GroupBy(x => x.Name);

            foreach (var battle in battles)
            {
                var trigger = battle.FirstOrDefault(x => x.Operation == "trigger");
                if (trigger == null)
                    continue;

                var area = areaService.Areas
                    .Where(x => x.Definition.Kind == AreaKind.General)
                    .FirstOrDefault(x => x.Definition.ChapterOnly && x.Definition.Chapter == trigger.Chapter);
                if (area == null)
                    continue;

                var lockFlag = randomizer.FlagService.AllocateFlag();
                AddAreaHit(area, new Vector3(trigger.X, trigger.Y, trigger.Z), trigger.Radius, lockFlag);

                var enemies = randomizer.EnemyService.EnemyPlacements
                    .Where(x => x.Battle == battle.Key)
                    .ToArray();

                var unlockFlags = new List<Guid>();
                foreach (var e in enemies)
                {
                    e.Chapter = trigger.Chapter;
                    e.Condition = lockFlag.ToString();
                    if (e.HasTag(EnemyTags.Guardian))
                    {
                        e.DeathFlag = randomizer.FlagService.AllocateFlag();
                        unlockFlags.Add(e.DeathFlag);
                    }
                }

                var doors = battle.Where(x => x.Operation == "lockdoor").ToArray();
                foreach (var d in doors)
                {
                    AddDoorLock(randomizer, d.Guid, lockFlag, unlockFlags);
                }
            }
        }

        private void AddAreaHit(Area area, Vector3 position, float radius, Guid flag)
        {
            var contextId = area.Randomizer.FlagService.AllocateContextId(1, 1);

            var gimmick = GimmickTemplate
                .Get("Biorand_AreaHit")
                .Clone()
                .WithName("BioRand_AreaHit_1");

            var transform = new Transform(gimmick)
            {
                Position = position
            };
            gimmick = gimmick.AddOrUpdateComponent(transform.ToComponent());

            var gimmickCore = gimmick.FindComponent("chainsaw.GimmickCore")!;
            gimmickCore = gimmickCore.Set("_ID", contextId);
            gimmick = gimmick.AddOrUpdateComponent(gimmickCore);

            var colliders = gimmick.FindComponent("via.physics.Colliders")!;
            colliders = colliders.Set("Colliders[0].Shape.Radius", radius);
            gimmick = gimmick.AddOrUpdateComponent(colliders);

            var setFlagComponent = gimmick.FindComponent("chainsaw.SetFlagSettings")!;
            setFlagComponent = setFlagComponent.Set("_Params._Params[0]._SetFlags[0]._Flag", flag);
            gimmick = gimmick.AddOrUpdateComponent(setFlagComponent);

            area.Scene = area.Scene.Add(gimmick);
            area.GimmickSaveData.AddBasic("", contextId);
        }

        private static void AddDoorLock(ChainsawRandomizer randomizer, Guid guid, Guid lockFlag, IList<Guid> unlockFlags)
        {
            var areaService = randomizer.AreaService;
            var area = areaService.FindAreaContainingGameObject(guid);
            if (area == null)
                return;

            var gameObject = area.Scene.FindGameObject(guid);
            if (gameObject == null)
                return;

            var isDoor = gameObject.FindComponent("chainsaw.GmDoor") != null;
            var isBigDoor = gameObject.FindComponent("chainsaw.GmBigDoor") != null;
            if (!isDoor && !isBigDoor)
                return;

            var lockComponentName = isBigDoor ? "chainsaw.GmOptionBigDoorLock" : "chainsaw.GmOptionDoorLock";
            var paramObject = gameObject.FindGameObject("ParamObject");
            if (paramObject == null)
                return;

            var gmOptionDoorLock = paramObject.FindComponent(lockComponentName);
            if (gmOptionDoorLock == null)
            {
                gmOptionDoorLock = randomizer.FileRepository.TypeRepository.Create(lockComponentName);
            }
            gmOptionDoorLock = gmOptionDoorLock.Set("Enabled", true);

            var lockRule = gmOptionDoorLock.Get<RszArrayNode>("LockRule");

            for (var i = 0; i < lockRule.Length; i++)
            {
                if (!lockRule[i].Get<bool>("Value"))
                {
                    lockRule = lockRule.RemoveAt(i);
                    i--;
                }
            }

            lockRule = lockRule.Add(RszSerializer.Serialize(
                randomizer.FileRepository.TypeRepository.FromName("chainsaw.RuleStratum.StratumBool")!,
                new chainsaw.RuleStratum.StratumBool()
                {
                    Value = true,
                    _Enable = new chainsaw.RuleStratum.Rule()
                    {
                        Logic = 0,
                        Matters =
                        [
                            new chainsaw.RuleStratum.Container()
                            {
                                _Data = new chainsaw.RuleStratum.ParticleFlag()
                                {
                                    Flags = new FlagCondition()
                                    {
                                        _Logic = 0,
                                        _CheckFlags =
                                        [
                                            new CheckFlagInfo()
                                            {
                                                _CheckFlag = lockFlag,
                                                _CompareValue = true
                                            }
                                        ]
                                    }
                                }
                            },
                            new chainsaw.RuleStratum.Container()
                            {
                                _Data = new chainsaw.RuleStratum.ParticleFlag()
                                {
                                    Flags = new FlagCondition()
                                    {
                                        _Logic = 1,
                                        _CheckFlags =
                                        [
                                            .. unlockFlags.Select(x => new CheckFlagInfo()
                                            {
                                                _CheckFlag = x,
                                                _CompareValue = false
                                            })
                                        ]
                                    }
                                }
                            }
                        ]
                    }
                }));
            gmOptionDoorLock = gmOptionDoorLock.Set("LockRule", lockRule);
            paramObject = paramObject.AddOrUpdateComponent(gmOptionDoorLock);
            area.Scene = area.Scene.UpdateGameObject(paramObject);
        }

        internal class BattleParameter
        {
            public string Name { get; set; } = "";
            public string Operation { get; set; } = "";
            public Guid Guid { get; set; }
            public int Chapter { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Radius { get; set; }
        }
    }
}
