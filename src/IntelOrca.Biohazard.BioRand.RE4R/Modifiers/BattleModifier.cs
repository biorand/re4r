using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
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
            var battleColllections = Csv.Deserialize<BattleParameter>(battleCsv)
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .GroupBy(x => x.Name)
                .GroupBy(x => x.First().Collection);

            var battles = new List<BattleParameter[]>();
            foreach (var collection in battleColllections)
            {
                if (collection.Key == null)
                {
                    foreach (var b in collection)
                    {
                        battles.Add(b.ToArray());
                    }
                }
                else
                {
                    var pick = randomizer.Seed % collection.Count();
                    var b = collection.ElementAt(pick);
                    battles.Add(b.ToArray());
                }
            }

            foreach (var battle in battles)
            {
                var name = battle[0].Name;
                var triggers = battle.Where(x => x.Operation == "trigger").ToArray();
                if (triggers.Length == 0)
                    continue;

                var chapter = triggers.Select(x => x.Chapter).FirstOrDefault(x => x != 0);
                var flagTriggers = triggers.Select(x => x.Guid).Where(x => x != default).ToArray();
                var areaTrigger = triggers.FirstOrDefault(x => x.Radius != 0);

                var area = areaService.Areas
                    .Where(x => x.Definition.Kind == AreaKind.General)
                    .FirstOrDefault(x => x.Definition.ChapterOnly && x.Definition.Chapter == chapter);
                if (area == null)
                    continue;

                var lockFlag = randomizer.FlagService.AllocateFlag();
                if (areaTrigger == null)
                {
                    AddFlagTrigger(area,
                        $"BioRand/Events/{name}/BioRand_Trigger_{name}",
                        flagTriggers,
                        lockFlag);
                }
                else
                {
                    AddAreaTrigger(area,
                        $"BioRand/Events/{name}/BioRand_Trigger_{name}",
                        flagTriggers,
                        new Vector3(areaTrigger.X, areaTrigger.Y, areaTrigger.Z),
                        areaTrigger.Radius,
                        lockFlag);
                }

                var enemies = randomizer.EnemyService.EnemyPlacements
                    .Where(x => x.Battle == name)
                    .ToArray();

                var unlockFlags = new List<Guid>();
                foreach (var e in enemies)
                {
                    e.Chapter = chapter;
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

        private void AddFlagTrigger(Area area, SceneHierachyPath hier, Guid[] requiredFlags, Guid flagSet)
        {
            var repo = area.Randomizer.FileRepository.TypeRepository;
            var contextId = area.Randomizer.FlagService.AllocateContextId(1, 1);

            var gimmick = GimmickTemplate
                .Get("Biorand_FlagCheckSet")
                .Clone()
                .WithName(hier.Name);

            gimmick = UpdateGimmickContextId(gimmick, contextId);
            gimmick = AddCheckFlagsComponent(repo, gimmick, requiredFlags);
            gimmick = AddSetFlagsComponent(repo, gimmick, flagSet);

            area.Scene = area.Scene.Add(repo, hier, gimmick);
            area.GimmickSaveData.AddBasic("", contextId);
        }

        private void AddAreaTrigger(Area area, SceneHierachyPath hier, Guid[] requiredFlags, Vector3 position, float radius, Guid flagSet)
        {
            var repo = area.Randomizer.FileRepository.TypeRepository;
            var contextId = area.Randomizer.FlagService.AllocateContextId(1, 1);

            var gimmick = GimmickTemplate
                .Get("Biorand_AreaHit")
                .Clone()
                .WithName(hier.Name);

            var transform = new Transform(gimmick)
            {
                Position = position
            };
            gimmick = gimmick.AddOrUpdateComponent(transform.ToComponent());

            gimmick = UpdateGimmickContextId(gimmick, contextId);

            var colliders = gimmick.FindComponent("via.physics.Colliders")!;
            colliders = colliders.Set("Colliders[0].Shape.Radius", radius);
            gimmick = gimmick.AddOrUpdateComponent(colliders);

            if (requiredFlags.Length != 0)
            {
                var interactHolder = gimmick.FindComponent("chainsaw.InteractHolder")!;
                interactHolder = interactHolder.Set("_Triggers[0]._Trigger.EnableCheckFlag", "CF");
                gimmick = gimmick.AddOrUpdateComponent(interactHolder);

                gimmick = AddCheckFlagsComponent(repo, gimmick, requiredFlags);
            }

            gimmick = AddSetFlagsComponent(repo, gimmick, flagSet);

            area.Scene = area.Scene.Add(repo, hier, gimmick);
            area.GimmickSaveData.AddBasic("", contextId);
        }

        private RszGameObject UpdateGimmickContextId(RszGameObject gameObject, chainsaw.ContextID contextId)
        {
            var gimmickCore = gameObject.FindComponent("chainsaw.GimmickCore")!;
            gimmickCore = gimmickCore.Set("_ID", contextId);
            gameObject = gameObject.AddOrUpdateComponent(gimmickCore);
            return gameObject;
        }

        private RszGameObject AddCheckFlagsComponent(RszTypeRepository repo, RszGameObject gameObject, params Guid[] flags)
        {
            return gameObject.AddOrUpdateComponent(repo.Serialize(new chainsaw.CheckFlagSettings()
            {
                Enabled = true,
                _Params = new OptionSettings<CheckFlagSettings.Param>()
                {
                    _Params =
                        [
                            new chainsaw.CheckFlagSettings.Param()
                            {
                                _KeyHash = 3090179045,
                                _BindTriggerNameHash = 2180083513,
                                _FlagCondition = new FlagCondition()
                                {
                                    _CheckFlags =
                                    [
                                        ..flags.Select(x => new CheckFlagInfo()
                                        {
                                            _CheckFlag = x,
                                            _CompareValue = true
                                        })
                                    ]
                                }
                            }
                        ]
                }
            }));
        }

        private RszGameObject AddSetFlagsComponent(RszTypeRepository repo, RszGameObject gameObject, params Guid[] flags)
        {
            return gameObject.AddOrUpdateComponent(repo.Serialize(new chainsaw.SetFlagSettings()
            {
                Enabled = true,
                _Params = new OptionSettings<SetFlagSettings.Param>()
                {
                    _Params =
                    [
                        new chainsaw.SetFlagSettings.Param()
                        {
                            _KeyHash = 2180083513,
                            _BindTriggerNameHash = 923965768,
                            _SetFlags =
                            [
                                ..flags.Select(x => new chainsaw.SetFlagSettings.SetFlagData()
                                {
                                    _Flag = x
                                })
                            ]
                        }
                    ]
                }
            }));
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

        [DebuggerDisplay("{Name} | {Operation}")]
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

            public string? Collection
            {
                get
                {
                    var index = Name.IndexOf('.');
                    if (index == -1)
                        return null;
                    return Name.Substring(0, index);
                }
            }
        }
    }
}
