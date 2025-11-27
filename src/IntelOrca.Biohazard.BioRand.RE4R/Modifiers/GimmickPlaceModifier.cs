using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var gimmicks = randomizer.DynamicData.GetData(DynamicDataName.Gimmicks) ?? throw new Exception("Failed to get gimmick data");
            var placements = Csv.Deserialize<GimmickPlacement>(gimmicks)
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => x.Kind.Trim() is string s && !string.IsNullOrEmpty(s) && !s.StartsWith('#'))
                .ToImmutableArray();

            var rng = randomizer.CreateRng();

            var bawk = randomizer.HasSpecialTouch("bawk");
            var extraMerchants = randomizer.GetConfigOption("extra-merchants", true);
            var enableGimmicks = randomizer.GetConfigOption("ea-extra-gimmicks", false);
            var numBreakableContainers = randomizer.GetConfigOption<double>("gimmicks-breakable-containers", 1);

            var fileRepository = randomizer.FileRepository;
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            var gimmickPaths = areaRepo.Gimmicks.ToArray();
            var factory = new GimmickFactory(randomizer, gimmickPaths);

            if (!bawk)
                placements = placements.RemoveAll(x => x.Kind == "bawk");

            if (enableGimmicks)
            {
                placements = TakeRandomGimmicks(placements, rng, numBreakableContainers, "Biorand_SmallWoodenBox", "Biorand_WoodenBox", "Biorand_WoodenBarrel", "Biorand_Vase");
            }
            else
            {
                var allowed = new[]
                {
                    "bawk",
                    "Biorand_Merchant",
                    "Biorand_MerchantTorch",
                    "Biorand_Typewriter",
                    "Biorand_TableDrawer"
                };
                placements = placements
                    .Where(x => allowed.Contains(x.Kind))
                    .ToImmutableArray();
            }

            if (!extraMerchants)
            {
                placements = placements
                    .Where(x => x.Kind != "Biorand_Merchant" && x.Kind != "Biorand_MerchantTorch")
                    .ToImmutableArray();
            }

            foreach (var placement in placements)
            {
                factory.AddGimmick(rng, placement);
            }
            factory.SaveAll();
        }

        private static ImmutableArray<GimmickPlacement> TakeRandomGimmicks(ImmutableArray<GimmickPlacement> placements, Rng rng, double amount, params string[] kinds)
        {
            var breakables = placements.Where(x => kinds.Contains(x.Kind)).Shuffle(rng).ToArray();
            var remove = breakables.Take((int)(Math.Clamp(1 - amount, 0, 1) * breakables.Length)).ToArray();
            if (remove.Length == 0)
                return placements;
            return placements.Except(remove).ToImmutableArray();
        }

        private class GimmickFactory(ChainsawRandomizer randomizer, string[] paths)
        {
            private readonly Dictionary<int, FilePair> _stageToFilePair = new();
            private int _contextId = 50_000;
            private (string, int)[]? _pathsWithUserData;

            public FileRepository FileRepository => randomizer.FileRepository;

            private FilePair GetScnForStage(int stage)
            {
                var stageA = stage / 1000;
                var stageB = stage % 1000;

                if (!_stageToFilePair.TryGetValue(stage, out var filePair))
                {
                    var pathsWithUserData = _pathsWithUserData;
                    if (pathsWithUserData == null)
                    {
                        pathsWithUserData = paths
                            .Where(x => FileRepository.Exists($"{x[..^7]}_savedata.user.2"))
                            .Select(x => (x, int.Parse(Regex.Replace(x, @".+st(\d\d)_(\d\d\d).+", "$1$2"))))
                            .ToArray();
                        _pathsWithUserData = pathsWithUserData;
                    }

                    var first = pathsWithUserData
                        .Where(x => (x.Item2 / 1000) == (stage / 1000))
                        .OrderBy(x => Math.Abs(x.Item2 - stage))
                        .First();
                    filePair = new FilePair(FileRepository, first.Item1);
                    _stageToFilePair[stage] = filePair;
                }

                return filePair;
            }

            public void SaveAll()
            {
                foreach (var kvp in _stageToFilePair)
                {
                    kvp.Value.Save();
                }
            }

            public void AddGimmick(Rng rng, GimmickPlacement placement)
            {
                var contextId = GetNewContextId();
                var filePair = GetScnForStage(placement.Stage);

                var kind = placement.Kind;
                if (kind == "bawk") kind = "Biorand_Chicken";

                var gimmick = CloneGimmickFromTemplate(kind, rng);
                gimmick = gimmick.WithName($"{gimmick.Name}_{placement.Id}");

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("chainsaw.GimmickCore")!
                    .SetField("_ID", contextId.ToRsz(FileRepository.TypeRepository)));

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("via.Transform")!
                    .Set("Position", placement.Position)
                    .Set("Rotation", placement.Eular.ToQuaternion())
                    .Set("Scale", Vector3.One));

                gimmick = AddCondition(gimmick, placement);

                filePair.Scene = filePair.Scene.Add(gimmick);

                var userData = FileRepository.TypeRepository.Create("chainsaw.GimmickSaveDataTable.Data");
                userData = userData.SetField("ID", contextId.ToRsz(FileRepository.TypeRepository));
                userData = userData.Set("Save.Attr", new byte[] { 0, 0, 0, 0 });
                filePair.UserData = filePair.UserData.SetField("Datas",
                    ((RszArrayNode)filePair.UserData["Datas"]).Add(userData));
            }

            private RszGameObject AddCondition(RszGameObject gimmick, GimmickPlacement placement)
            {
                if (string.IsNullOrEmpty(placement.Condition) && placement.Chapter == 0)
                    return gimmick;

                var repo = FileRepository.TypeRepository;
                var paramObject = gimmick.FindGameObject("ParamObject")!;
                var stratumBool = repo
                    .Create("chainsaw.RuleStratum.StratumBool")
                    .Set("Value", true)
                    .Set("_Enable.Logic", 1);

                if (!string.IsNullOrEmpty(placement.Condition))
                {
                    stratumBool = stratumBool.Set("_Enable.Matters", stratumBool
                        .Get<RszArrayNode>("_Enable.Matters")
                        .Add(repo
                            .Create("chainsaw.RuleStratum.Container")
                            .Set("_Data", repo
                                .Create("chainsaw.RuleStratum.ParticleFlag")
                                .Set("Flags", new chainsaw.FlagCondition()
                                {
                                    _CheckFlags =
                                    {
                                        new chainsaw.CheckFlagInfo()
                                        {
                                            _CheckFlag = Guid.Parse(placement.Condition),
                                            _CompareValue = false
                                        }
                                    }
                                }))));
                }
                if (placement.Chapter != 0)
                {
                    stratumBool = stratumBool.Set("_Enable.Matters", stratumBool
                        .Get<RszArrayNode>("_Enable.Matters")
                        .Add(repo
                            .Create("chainsaw.RuleStratum.Container")
                            .Set("_Data", repo
                                .Create("chainsaw.RuleStratum.ParticleChapter")
                                .Set("Compare", 1)
                                .Set("Chapter", ChapterId.FromNumber(randomizer.Campaign, placement.Chapter)))));
                }

                paramObject = paramObject.AddOrUpdateComponent(repo
                    .Create("chainsaw.GmOptionHide")
                    .Set("Enabled", true)
                    .Set("Rule", new[] { stratumBool }));

                if (gimmick.Name.StartsWith("Biorand_MerchantTorch"))
                {
                    paramObject = paramObject.AddOrUpdateComponent(repo
                        .Create("chainsaw.ObjectHide")
                        .Set("Enabled", true)
                        .Set("Settings", new[] { stratumBool }));
                }

                return gimmick.AddOrUpdateChild(paramObject);
            }

            private static RszGameObject CloneGimmickFromTemplate(string kind, Rng rng)
            {
                var map = new Dictionary<Guid, Guid>();

                // Create new guids for all game objects
                var root = GimmickTemplate
                    .Get(kind)
                    .VisitGameObjects(gameObject =>
                    {
                        // Change to new guid (keep map of old to new)
                        var newGuid = rng.NextGuid();
                        map[gameObject.Guid] = newGuid;
                        return gameObject.WithGuid(newGuid);
                    });

                // Fix references
                return root.Visit(node =>
                {
                    if (node is RszValueNode valueNode && valueNode.Type == RszFieldType.GameObjectRef)
                    {
                        var refGuid = valueNode.Get<Guid>();
                        if (map.TryGetValue(refGuid, out var newGuid))
                        {
                            return RszSerializer.Serialize(RszFieldType.GameObjectRef, newGuid);
                        }
                    }
                    return node;
                });
            }

            private ContextId GetNewContextId()
            {
                return new ContextId(5, 0, 1, _contextId++);
            }

            private class FilePair
            {
                private readonly FileRepository _fileRepository;

                public string ScenePath { get; }
                public ScnFile.Builder Scn { get; }
                public UserFile.Builder User { get; }

                public RszScene Scene
                {
                    get => Scn.Scene;
                    set => Scn.Scene = value;
                }

                public RszObjectNode UserData
                {
                    get => (RszObjectNode)User.Objects[0];
                    set => User.Objects = User.Objects.SetItem(0, value);
                }

                public string UserPath => $"{ScenePath[..^7]}_savedata.user.2";

                public FilePair(FileRepository fileRepository, string path)
                {
                    _fileRepository = fileRepository;
                    ScenePath = path;
                    Scn = fileRepository.GetScnFile(ScenePath).ToBuilder(_fileRepository.TypeRepository);
                    User = fileRepository.GetUserFile(UserPath).ToBuilder(_fileRepository.TypeRepository);
                }

                public void Save()
                {
                    _fileRepository.SetScnFile(ScenePath, Scn.AddMissingResources().Build());
                    _fileRepository.SetUserFile(UserPath, User.Build());
                }
            }
        }

        private sealed class GimmickPlacement
        {
            public int Id { get; set; }
            public string Kind { get; set; } = "";
            public int Stage { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Yaw { get; set; }
            public float Pitch { get; set; }
            public float Roll { get; set; }
            public string Condition { get; set; } = "";
            public int Chapter { get; set; }
            public Campaign Campaign { get; set; }

            public Vector3 Position => new(X, Y, Z);
            public EulerAngles Eular => new(Yaw, Pitch, Roll);

            public override string ToString()
            {
                return $"{Id}_{Kind}";
            }
        }
    }
}
