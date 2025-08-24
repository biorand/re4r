using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
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
            var placements = GimmickPlacement.GetPlacements(randomizer.Campaign);

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
                factory.AddGimmick(placement);
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
                            .Where(x => randomizer.FileRepository.Exists($"{x[..^7]}_savedata.user.2"))
                            .Select(x => (x, int.Parse(Regex.Replace(x, @".+st(\d\d)_(\d\d\d).+", "$1$2"))))
                            .ToArray();
                        _pathsWithUserData = pathsWithUserData;
                    }

                    var first = pathsWithUserData
                        .Where(x => (x.Item2 / 1000) == (stage / 1000))
                        .OrderBy(x => Math.Abs(x.Item2 - stage))
                        .First();
                    filePair = new FilePair(randomizer.FileRepository, first.Item1);
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

            public void AddGimmick(GimmickPlacement placement)
            {
                var contextId = GetNewContextId();
                var filePair = GetScnForStage(placement.Stage);

                var kind = placement.Kind;
                if (kind == "bawk") kind = "Biorand_Chicken";

                var gimmick = GimmickTemplate.Get(kind);
                gimmick = gimmick.WithName($"{gimmick.Name}_{placement.LineNumber}");

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("chainsaw.GimmickCore")!
                    .SetField("_ID", contextId.ToRsz(FileRepository.RszRepository)));

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("via.Transform")!
                    .Set("Position", placement.Position)
                    .Set("Rotation", placement.Eular.ToQuaternion())
                    .Set("Scale", Vector3.One));

                gimmick = AddCondition(gimmick, placement);

                filePair.Scene = filePair.Scene.Add(gimmick);

                var userData = FileRepository.RszRepository.Create("chainsaw.GimmickSaveDataTable.Data");
                userData = userData.SetField("ID", contextId.ToRsz(FileRepository.RszRepository));
                userData = userData.Set("Save.Attr", new byte[] { 0, 0, 0, 0 });
                filePair.UserData = filePair.UserData.SetField("Datas",
                    ((RszArrayNode)filePair.UserData["Datas"]).Add(userData));
            }

            private RszGameObject AddCondition(RszGameObject gimmick, GimmickPlacement placement)
            {
                if (string.IsNullOrEmpty(placement.Condition) && placement.Chapter == 0)
                    return gimmick;

                var repo = FileRepository.RszRepository;
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
                                            _CompareValue = true
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

                public RszStructNode UserData
                {
                    get => (RszStructNode)User.Objects[0];
                    set => User.Objects = User.Objects.SetItem(0, value);
                }

                public string UserPath => $"{ScenePath[..^7]}_savedata.user.2";

                public FilePair(FileRepository fileRepository, string path)
                {
                    _fileRepository = fileRepository;
                    ScenePath = path;
                    Scn = fileRepository.GetScnFile2(ScenePath).ToBuilder(FileRepository.RszRepository);
                    User = fileRepository.GetUserFile(UserPath).ToBuilder(FileRepository.RszRepository);
                }

                public void Save()
                {
                    _fileRepository.SetScnFile2(ScenePath, Scn.Build());
                    _fileRepository.SetUserFile(UserPath, User.Build());
                }
            }
        }

        private sealed class GimmickPlacement
        {
            public int LineNumber { get; }
            public string Kind { get; }
            public int Stage { get; }
            public Vector3 Position { get; }
            public EulerAngles Eular { get; }
            public string Condition { get; }
            public int Chapter { get; }

            private GimmickPlacement(int lineNumber, string[] p)
            {
                LineNumber = lineNumber;
                Kind = p[0];
                Stage = int.Parse(p[1]);
                Position = new Vector3(
                    float.Parse(p[2]),
                    float.Parse(p[3]),
                    float.Parse(p[4]));
                Eular = new EulerAngles(
                    float.Parse(p[5]),
                    float.Parse(p[6]),
                    float.Parse(p[7]));
                Condition = p[8];
                Chapter = string.IsNullOrEmpty(p[9]) ? 0 : int.Parse(p[9]);
            }

            public static ImmutableArray<GimmickPlacement> GetPlacements(Campaign campaign)
            {
                var gimmicksFile = campaign == Campaign.Leon
                    ? EmbeddedData.GetFile("gimmicks.csv")
                    : EmbeddedData.GetFile("gimmicks_sw.csv");

                var lines = Encoding.UTF8.GetString(gimmicksFile)
                    .ReplaceLineEndings("\n")
                    .Split("\n");

                var result = ImmutableArray.CreateBuilder<GimmickPlacement>();
                var header = true;
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("#") || line.Length == 0)
                        continue;

                    if (header)
                    {
                        header = false;
                        continue;
                    }

                    result.Add(new GimmickPlacement(i + 1, line.Split(',')));
                }
                return result.ToImmutable();
            }

            public override string ToString()
            {
                return $"{Kind}";
            }
        }
    }
}
