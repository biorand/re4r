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
    internal class EventModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("battle-arenas"))
                return;

            var areaService = randomizer.AreaService;

            var eventCsv = randomizer.DynamicData.GetData(DynamicDataName.Events) ?? throw new Exception("Event data not found");
            var eventCollections = Csv.Deserialize<EventParameter>(eventCsv)
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .GroupBy(x => x.Name)
                .GroupBy(x => x.First().Collection);

            var events = new List<EventParameter[]>();
            foreach (var collection in eventCollections)
            {
                if (collection.Key == null)
                {
                    foreach (var b in collection)
                    {
                        events.Add(b.ToArray());
                    }
                }
                else
                {
                    var groups = collection.GroupBy(x => x.First().Group);
                    var pick = randomizer.Seed % groups.Count();
                    var g = groups.ElementAt(pick);
                    foreach (var b in g)
                    {
                        events.Add(b.ToArray());
                    }
                }
            }

            foreach (var parameters in events)
            {
                var name = parameters[0].Name;
                var chapter = parameters.Select(x => x.Chapter).FirstOrDefault(x => x != 0);
                var beginFlag = default(Guid);
                var endFlags = new List<Guid>();

                // Process triggers
                var triggers = parameters.Where(x => x.Operation == EventOperation.Trigger).ToArray();
                if (triggers.Length != 0)
                {
                    var flagTriggers = triggers.Select(x => x.Guid).Where(x => x != default).ToArray();
                    var areaTrigger = triggers.FirstOrDefault(x => x.Radius != 0);

                    var area = areaService.Areas
                        .Where(x => x.Definition.Kind == AreaKind.General)
                        .FirstOrDefault(x => x.Definition.ChapterOnly && x.Definition.Chapter == chapter);
                    if (area == null)
                        continue;

                    beginFlag = randomizer.FlagService.AllocateFlag();
                    if (areaTrigger == null)
                    {
                        AddFlagTrigger(area,
                            $"BioRand/Events/{name}/BioRand_Trigger_{name}",
                            flagTriggers,
                            beginFlag);
                    }
                    else
                    {
                        AddAreaTrigger(area,
                            $"BioRand/Events/{name}/BioRand_Trigger_{name}",
                            flagTriggers,
                            new Vector3(areaTrigger.X, areaTrigger.Y, areaTrigger.Z),
                            areaTrigger.Radius,
                            beginFlag);
                    }
                }

                // Items
                foreach (var item in randomizer.GetService<ItemService>().ItemPlacements)
                {
                    if (item.Events.Contains(name))
                    {
                        item.Chapter = chapter;
                        item.Tags = item.Tags.Add(ItemTags.Always);
                        item.Condition = beginFlag;
                    }
                }

                // Gimmicks
                foreach (var gimmick in randomizer.GimmickService.GimmickPlacements)
                {
                    if (gimmick.Events.Contains(name))
                    {
                        gimmick.Chapter = gimmick.Tags.Contains(GimmickTags.ChapterOnly) ? chapter : 0;
                        gimmick.Tags = gimmick.Tags.Add(GimmickTags.Always);
                        gimmick.Condition = beginFlag;
                    }
                }

                EnemyPlacement? keyHolder = null;
                foreach (var e in randomizer.EnemyService.EnemyPlacements)
                {
                    if (e.Events.Contains(name))
                    {
                        e.Chapter = chapter;
                        e.Tags = e.Tags.Add(EnemyTags.Always);

                        // If there is a trigger, give enemy trigger condition
                        if (beginFlag != default)
                        {
                            e.Condition = beginFlag.ToString();
                            if (e.HasTag(EnemyTags.Guardian))
                            {
                                // Give guardian enemies their death flag to complete event
                                e.DeathFlag = randomizer.FlagService.AllocateFlag();
                                endFlags.Add(e.DeathFlag);
                                keyHolder ??= e;
                            }
                        }
                    }
                }

                // Other parameters for the event
                foreach (var param in parameters)
                {
                    switch (param.Operation)
                    {
                        case EventOperation.EndTrigger:
                            {
                                var area = areaService.Areas
                                    .Where(x => x.Definition.Kind == AreaKind.General)
                                    .First(x => x.Definition.ChapterOnly && x.Definition.Chapter == chapter);

                                var newFlag = randomizer.FlagService.AllocateFlag();
                                AddAreaTrigger(area,
                                    $"BioRand/Events/{name}/BioRand_EndTrigger_{name}",
                                    beginFlag == default ? [] : [beginFlag],
                                    param.Position,
                                    param.Radius,
                                    newFlag);
                                endFlags.Add(newFlag);
                                break;
                            }
                        case EventOperation.LockDoor:
                            AddDoorLock(randomizer, param.Guid, beginFlag, endFlags);
                            break;
                        case EventOperation.RemoveKey:
                            AddKeyTag(param.Guid, ItemTags.Remove);
                            break;
                        case EventOperation.ChangeKey:
                            AddKeyTag(param.Guid, ItemTags.ChangeKey);
                            break;
                        case EventOperation.GiveKey:
                            keyHolder?.ItemId = param.ItemId;
                            break;
                        case EventOperation.PlaceFile:
                            var fileService = randomizer.GetService<FileService>();
                            fileService.FilePlacements.Add(new FilePlacement()
                            {
                                TemplateId = 32,
                                Id = fileService.GetNextId(),
                                Stage = param.Stage,
                                X = param.X,
                                Y = param.Y,
                                Z = param.Z,
                                Yaw = param.Yaw,
                                Pitch = param.Pitch,
                                Roll = param.Roll,
                                Content = param.Notes
                            });
                            break;
                        case EventOperation.Remove:
                            RemoveGimmick(randomizer, param);
                            break;
                        case EventOperation.Move:
                            MoveGimmick(randomizer, param);
                            break;
                        case EventOperation.Start:
                            var campaignService = randomizer.GetService<CampaignService>();
                            var campaignChapter = campaignService.GetChapter(param.Chapter);
                            campaignChapter.StartStage = param.Stage;
                            campaignChapter.StartPosition = param.Position;
                            campaignChapter.StartEuler = param.Euler;
                            break;
                    }
                }
            }

            void AddKeyTag(Guid guid, string tag)
            {
                var itemPlacement = randomizer.GetService<ItemService>().FromGuid(guid);
                if (itemPlacement == null)
                    return;

                itemPlacement.Tags = itemPlacement.Tags.Add(tag);
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

            gimmick = gimmick.WithGimmickContextId(contextId);
            gimmick = AddCheckFlagsComponent(repo, gimmick, 1383070635, 2180083513, requiredFlags);
            gimmick = AddSetFlagsComponent(repo, gimmick, 2180083513, 2092886954, flagSet);

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

            gimmick = gimmick.WithGimmickContextId(contextId);

            var colliders = gimmick.FindComponent("via.physics.Colliders")!;
            colliders = colliders.Set("Colliders[0].Shape.Radius", radius);
            gimmick = gimmick.AddOrUpdateComponent(colliders);

            if (requiredFlags.Length != 0)
            {
                var interactHolder = gimmick.FindComponent("chainsaw.InteractHolder")!;
                interactHolder = interactHolder.Set("_Triggers[0]._Trigger.EnableCheckFlag", "CF");
                gimmick = gimmick.AddOrUpdateComponent(interactHolder);

                gimmick = AddCheckFlagsComponent(repo, gimmick, 3090179045, 2180083513, requiredFlags);
            }

            gimmick = AddSetFlagsComponent(repo, gimmick, 2180083513, 923965768, flagSet);

            area.Scene = area.Scene.Add(repo, hier, gimmick);
            area.GimmickSaveData.AddBasic("", contextId);
        }

        private RszGameObject AddCheckFlagsComponent(RszTypeRepository repo, RszGameObject gameObject, uint key, uint bind, params Guid[] flags)
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
                                _KeyHash = key,
                                _BindTriggerNameHash = bind,
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

        private RszGameObject AddSetFlagsComponent(RszTypeRepository repo, RszGameObject gameObject, uint key, uint bind, params Guid[] flags)
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
                            _KeyHash = key,
                            _BindTriggerNameHash = bind,
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
            var gimmickService = randomizer.GimmickService;
            var placement = gimmickService.FromGuid(guid);
            if (placement == null)
            {
                placement = new GimmickPlacement()
                {
                    Campaign = randomizer.Campaign,
                    Guid = guid,
                    Vanilla = true
                };
                gimmickService.AddPlacement(placement);
            }
            placement.Param1 = lockFlag.ToString();
            placement.Param2 = string.Join(" ", unlockFlags);
        }

        private static void MoveGimmick(ChainsawRandomizer randomizer, EventParameter param)
        {
            var itemService = randomizer.GetService<ItemService>();
            var itemPlacement = itemService.FromGuid(param.Guid);
            if (itemPlacement != null)
            {
                itemPlacement.X = param.X;
                itemPlacement.Y = param.Y;
                itemPlacement.Z = param.Z;
                itemPlacement.Yaw = param.Yaw;
                itemPlacement.Pitch = param.Pitch;
                itemPlacement.Roll = param.Roll;
            }
            else
            {
                var gimmickService = randomizer.GimmickService;
                var placement = gimmickService.FromGuid(param.Guid);
                if (placement == null)
                {
                    placement = new GimmickPlacement()
                    {
                        Campaign = randomizer.Campaign,
                        Guid = param.Guid,
                        Vanilla = true,
                        X = param.X,
                        Y = param.Y,
                        Z = param.Z,
                        Yaw = param.Yaw,
                        Pitch = param.Pitch,
                        Roll = param.Roll,
                    };
                    gimmickService.AddPlacement(placement);
                }
            }
        }

        private static void RemoveGimmick(ChainsawRandomizer randomizer, EventParameter param)
        {
            var gimmickService = randomizer.GimmickService;
            var placement = gimmickService.FromGuid(param.Guid);
            if (placement == null)
            {
                placement = new GimmickPlacement()
                {
                    Campaign = randomizer.Campaign,
                    Guid = param.Guid,
                    Vanilla = true,
                    Tags = [GimmickTags.Never]
                };
                gimmickService.AddPlacement(placement);
            }
        }

        [DebuggerDisplay("{Name} | {Operation}")]
        internal class EventParameter
        {
            public string Name { get; set; } = "";
            public EventOperation Operation { get; set; }
            public Guid Guid { get; set; }
            public int Chapter { get; set; }
            public int Stage { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Yaw { get; set; }
            public float Pitch { get; set; }
            public float Roll { get; set; }
            public float Radius { get; set; }
            public int ItemId { get; set; }
            public string Notes { get; set; } = "";

            public string? Collection
            {
                get
                {
                    var index = Name.IndexOf('.');
                    return index == -1 ? null : Name[..index];
                }
            }

            public string Group
            {
                get
                {
                    var fullStopIndex = Name.IndexOf('.');
                    if (fullStopIndex == -1)
                    {
                        var index = Name.IndexOf('|');
                        return Name;
                    }
                    else
                    {
                        var nameIndex = Name.IndexOf('|');
                        return nameIndex == -1
                            ? Name[(fullStopIndex + 1)..]
                            : Name.Substring(fullStopIndex + 1, nameIndex - fullStopIndex - 1);
                    }
                }
            }

            public Vector3 Position => new(X, Y, Z);
            public EulerAngles Euler => new(Yaw, Pitch, Roll);
        }

        internal enum EventOperation
        {
            None,
            Trigger,
            EndTrigger,
            LockDoor,
            RemoveKey,
            ChangeKey,
            GiveKey,
            PlaceFile,
            Remove,
            Move,
            Start,
        }
    }
}
