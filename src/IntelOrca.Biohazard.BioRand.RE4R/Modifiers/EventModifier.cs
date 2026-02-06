using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.Graphing;
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

            var piers = new List<PierPosition>();

            var eventTree = GetEventTree(randomizer);
            AddNoEvents(eventTree);
            randomizer.AddLogFile("events_all.mmd", DumpTree(eventTree));
            eventTree.Choose(randomizer);
            randomizer.AddLogFile("events_chosen.mmd", DumpTree(eventTree));
            ProcessEventNode(eventTree);
            UpdatePierData(randomizer, piers);
            ApplyBoatFixes(randomizer);

            void ProcessEventNode(EventNode node)
            {
                foreach (var child in node.Children)
                {
                    logger.Push(child.FullName);
                    ProcessSingleEventNode(child);
                    ProcessEventNode(child);
                    logger.Pop();
                }
            }

            void ProcessSingleEventNode(EventNode node)
            {
                var areaService = randomizer.AreaService;
                var name = node.FullName;
                var parameters = node.Parameters;
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
                        return;

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
                        case EventOperation.AddPier:
                            piers.Add(new PierPosition()
                            {
                                Position = param.Position,
                                Euler = param.Euler
                            });
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
                itemPlacement.Stage = param.Stage;
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
                        Stage = param.Stage,
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

        private static void UpdatePierData(ChainsawRandomizer randomizer, IEnumerable<PierPosition> piers)
        {
            const string path = "natives/stm/_chainsaw/appsystem/gimmick/details/boat/userdata/pierdata.user.2";
            randomizer.FileRepository.ModifyUserFile<chainsaw.PierData>(path, root =>
            {
                foreach (var p in piers)
                {
                    root.PierGroupList.Add(new PierData.PierGroup()
                    {
                        Enable = true,
                        InputKeyCenterPos = p.Position,
                        InputKeyRadius = 7,
                        Piers =
                        {
                            new PierData.Pier()
                            {
                                Enable = true,
                                Position = p.Position,
                                DegreeY = p.Euler.Yaw,
                                StopDir = 1,
                                Width_L = 1,
                                Width_R = 1,
                                PlayerWerpPos = p.Position
                            }
                        }
                    });
                }
                return root;
            });
        }

        private static EventNode GetEventTree(ChainsawRandomizer randomizer)
        {
            var eventCsv = randomizer.DynamicData.GetData(DynamicDataName.Events) ?? throw new Exception("Event data not found");
            var eventParams = Csv.Deserialize<EventParameter>(eventCsv)
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .GroupBy(x => x.Name);

            var root = new EventNode(null, "");
            foreach (var g in eventParams)
            {
                var name = g.Key;
                var chapter = g.Select(x => x.Chapter).Where(x => x != 0).FirstOrDefault();

                var pipeSplit = name.Split('|');
                var sub = new string[0];
                if (pipeSplit.Length > 1)
                {
                    name = pipeSplit[0];
                    sub = pipeSplit.Skip(1).ToArray();
                }

                var node = root;
                var dotSplit = name.Split('.');
                for (var i = 0; i < dotSplit.Length; i++)
                {
                    node = node.GetOrCreateChild(dotSplit[i]);
                }

                node.Parent?.Kind = NodeKind.Choose;
                node.Kind = NodeKind.Event;

                for (var i = 0; i < sub.Length; i++)
                {
                    node = node.GetOrCreateChild(sub[i]);
                    node.Kind = NodeKind.Part;
                }

                node.Chapter = chapter;
                node.Parameters = g.ToImmutableArray();
                node.Tags = node.Parameters.SelectMany(x => x.Tags).Distinct().ToImmutableArray();
                node.Weight = node.Parameters.Select(x => x.Weight).FirstOrDefault(x => x != 0);
                if (node.Weight <= 0)
                    node.Weight = 1;
            }

            root.Kind = NodeKind.Group;
            return root;
        }

        private static void AddNoEvents(EventNode tree)
        {
            Visit(tree);

            static void Visit(EventNode node)
            {
                if (node.Kind == NodeKind.Choose)
                {
                    var emptyNode = node.GetOrCreateChild("EMPTY");
                    emptyNode.Kind = NodeKind.Empty;
                }
                else
                {
                    foreach (var child in node.Children)
                    {
                        Visit(child);
                    }
                }
            }
        }

        private string DumpTree(EventNode tree)
        {
            var mb = new MermaidBuilder();
            Visit(tree);
            return mb.ToString();

            void Visit(EventNode node)
            {
                var shape = node.Kind switch
                {
                    NodeKind.Group => MermaidShape.Circle,
                    NodeKind.Choose => MermaidShape.Hexagon,
                    NodeKind.Event or NodeKind.Empty => MermaidShape.Rounded,
                    NodeKind.Part => MermaidShape.Square,
                    _ => throw new NotImplementedException(),
                };
                mb.Node(node.Guid.ToString(), node.Name == "" ? " " : node.Name, shape);
                foreach (var child in node.Children)
                {
                    Visit(child);
                }
                foreach (var child in node.Children)
                {
                    var label = node.Kind == NodeKind.Choose ? $"{child.Ratio * 100:0.00}%" : null;
                    var kind = child.Kind == NodeKind.Part ? MermaidEdgeType.Dotted : MermaidEdgeType.Solid;
                    mb.Edge(node.Guid.ToString(), child.Guid.ToString(), label, kind);
                }
            }


            // var sb = new StringBuilder();
            // Print(tree, 0);
            // var s = sb.ToString();
            // return s;
            // 
            // void Print(EventNode node, int level)
            // {
            //     sb.Append('-', level);
            //     sb.Append(' ');
            //     if (node.Kind == NodeKind.Event)
            //     {
            //         sb.Append('*');
            //         sb.Append(' ');
            //     }
            //     sb.Append(node.Name);
            //     sb.AppendLine();
            //     foreach (var child in node.Children)
            //     {
            //         Print(child, level + 1);
            //     }
            // }
        }

        private void ApplyBoatFixes(ChainsawRandomizer randomizer)
        {
            if (randomizer.Campaign != Campaign.Leon)
                return;

            var boatEnableFlag = new Guid("89323c20-14fb-49ce-a1c4-f0447714d4d2");

            // Set Del Lago event flag to true at game start, so boat works
            // DIDN'T WORK:
            //     randomizer.FlagService.SetFlag(boatEnableFlag, true);
            var areaService = randomizer.GetService<AreaService>();
            var campaignService = randomizer.GetService<CampaignService>();
            var firstChapterArea = areaService.FindBestArea(AreaKind.General, 0, campaignService.StartChapter);
            AddFlagTrigger(firstChapterArea, "BioRand/Boat/Enable", [], boatEnableFlag);

            // Fix Del Lago event, trigger via a different flag
            var gameObjectsGuids = new[]
            {
                new Guid("4c020093-f8bb-457d-872c-b871036d8b48"),
                new Guid("fd22e328-c623-40eb-b130-b42307ab198f")
            };
            var delLagoTriggerFlag = randomizer.FlagService.AllocateFlag();

            foreach (var gameObjectGuid in gameObjectsGuids)
            {
                var area = areaService.FindAreaContainingGameObject(gameObjectGuid)!;
                var gameObject = area.Scene.FindGameObject(gameObjectGuid)!;
                var component = gameObject.FindComponent("chainsaw.CheckFlagSettings")!;
                component = component.Set("_Params._Params[0]._FlagCondition._CheckFlags[0]._CheckFlag", delLagoTriggerFlag);
                area.Scene = area.Scene.UpdateGameObject(
                    gameObject.AddOrUpdateComponent(component));
            }

            // Add an area hit for new flag
            var delLagoArea = areaService.FindBestArea(AreaKind.General, 0, 3);
            var delLagoPosition = new Vector3(233.76f, -7.5f, 58.67f);
            var delLagoRadius = 1.0f;
            AddAreaTrigger(delLagoArea, "BioRand/DelLago/Trigger", [], delLagoPosition, delLagoRadius, delLagoTriggerFlag);
        }

        [DebuggerDisplay("[{Kind}] {FullName} <{Weight}>")]
        private class EventNode(EventNode? parent, string name)
        {
            public Guid Guid { get; } = Guid.NewGuid();
            public EventNode? Parent => parent;
            public string Name => name;
            public ImmutableArray<EventNode> Children { get; private set; } = [];
            public NodeKind Kind { get; set; }
            public int Chapter { get; set; }
            public ImmutableArray<EventParameter> Parameters { get; set; } = [];
            public ImmutableArray<string> Tags { get; set; } = [];
            public double Weight { get; set; } = 1;

            public double Ratio => Parent == null ? double.NaN : Weight / Parent.TotalChildWeight;
            private double TotalChildWeight => Children.Length == 0 ? 0 : Children.Sum(x => x.Weight);

            public EventNode GetOrCreateChild(string name)
            {
                var result = Children.FirstOrDefault(x => x.Name == name);
                if (result == null)
                {
                    result = new EventNode(this, name);
                    Children = Children
                        .Add(result)
                        .OrderBy(x => x.Name)
                        .ToImmutableArray();
                }
                return result;
            }

            public void Choose(ChainsawRandomizer randomizer)
            {
                if (Kind == NodeKind.Choose && Children.Length >= 2)
                {
                    var index = Children.FindIndex(x => x.Tags.Contains(EventTags.Always));
                    if (index != -1)
                    {
                        Children = [Children[index]];
                    }
                    else
                    {
                        index = Children.Length - 1;
                        var rng = randomizer.GetRng($"modifier/event/{FullName}");
                        var totalWeight = TotalChildWeight;
                        var number = rng.NextDouble(0, totalWeight);
                        var current = 0.0;
                        for (var i = 0; i < Children.Length; i++)
                        {
                            var next = current + Children[i].Weight;
                            if (number < next)
                            {
                                index = i;
                                break;
                            }
                            current = next;
                        }
                        Children = [Children[index]];
                    }
                }
                Children = Children.RemoveAll(x => x.Kind == NodeKind.Empty);
                foreach (var child in Children)
                {
                    child.Choose(randomizer);
                }
            }

            public string FullName
            {
                get
                {
                    if (string.IsNullOrEmpty(Parent?.FullName))
                        return Name;

                    var sep = Parent.Kind == NodeKind.Event ? '|' : '.';
                    return Parent.FullName + sep + Name;
                }
            }

            public IEnumerable<EventNode> GetAllEvents()
            {
                if (Kind == NodeKind.Event)
                {
                    yield return this;
                }
                else
                {
                    foreach (var child in Children)
                    {
                        foreach (var node in child.GetAllEvents())
                        {
                            yield return node;
                        }
                    }
                }
            }
        }

        [DebuggerDisplay("{Name} | {Operation}")]
        private class EventParameter
        {
            public string Name { get; set; } = "";
            public double Weight { get; set; }
            public ImmutableArray<string> Tags { get; set; } = [];
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

        private readonly struct PierPosition
        {
            public Vector3 Position { get; init; }
            public EulerAngles Euler { get; init; }
        }

        private enum EventOperation
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
            AddPier,
        }

        private enum NodeKind
        {
            Group,
            Choose,
            Event,
            Part,
            Empty
        }

        private static class EventTags
        {
            public const string Never = "never";
            public const string Always = "always";
        }
    }
}
