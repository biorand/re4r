using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using RszTool;

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

            private FilePair GetScnForStage(int stage)
            {
                var stageA = stage / 1000;
                var stageB = stage % 1000;

                if (!_stageToFilePair.TryGetValue(stage, out var filePair))
                {
                    var first = paths.First(x => x.Contains($"st{stageA}") && x.Contains($"_{stageB}"));
                    filePair = new FilePair(randomizer.FileRepository, first);
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

                var gimmick = filePair.Scene.ImportGameObject(GimmickTemplate.Get(kind));
                gimmick.Instance!.SetFieldValue("v0", $"{gimmick.Name}_{placement.LineNumber}");

                var gimmickCore = gimmick.FindComponent("chainsaw.GimmickCore")!;
                contextId.CopyTo(gimmickCore.Get<RszInstance>("_ID")!);

                var gimmickTransform = gimmick.FindComponent("via.Transform")!;
                gimmickTransform.Set("v0", new Vector4(placement.Position, 1));
                gimmickTransform.Set("v1", CreateRotation(placement.Rotation));
                gimmickTransform.Set("v2", new Vector4(1, 1, 1, 0));

                AddCondition(filePair.Scene, gimmick, placement);

                var userData = filePair.User.RSZ!.CreateInstance("chainsaw.GimmickSaveDataTable.Data");
                contextId.CopyTo(userData.Get<RszInstance>("ID")!);
                userData.GetList("Save.Attr").AddRange([(byte)0, (byte)0, (byte)0, (byte)0]);
                var dataList = filePair.User.RSZ.ObjectList[0].GetList("Datas");
                dataList.Add(userData);
            }

            private static ScnFile.GameObjectData? FindChildRecursive(ScnFile.GameObjectData parent, string name)
            {
                if (parent.Name == name)
                    return parent;

                foreach (var child in parent.Children)
                {
                    if (child.Name == name)
                        return child;

                    var d = FindChildRecursive(child, name);
                    if (d != null)
                        return d;
                }
                return null;
            }

            private static void AddCondition(ScnFile scn, ScnFile.GameObjectData gimmick, GimmickPlacement placement)
            {
                if (string.IsNullOrEmpty(placement.Condition) && placement.Chapter == 0)
                    return;

                var paramObject = gimmick.Children.First(x => x.Name == "ParamObject");
                var stratumBool = scn.RSZ!.CreateInstance("chainsaw.RuleStratum.StratumBool");
                stratumBool.Set("Value", true);
                stratumBool.Set("_Enable.Logic", 1);

                if (!string.IsNullOrEmpty(placement.Condition))
                {
                    var particleFlag = scn.RSZ!.CreateInstance("chainsaw.RuleStratum.ParticleFlag");
                    var fc = new FlagCondition(particleFlag.Get<RszInstance>("Flags")!);
                    var f = CheckFlagInfo.Create(scn, Guid.Parse(placement.Condition));
                    f.CompareValue = false;
                    fc.Flags = [f];
                    var container = scn.RSZ!.CreateInstance("chainsaw.RuleStratum.Container");
                    container.Set("_Data", particleFlag);
                    stratumBool.GetList("_Enable.Matters").Add(container);
                }
                if (placement.Chapter != 0)
                {
                    var particleChapter = scn.RSZ!.CreateInstance("chainsaw.RuleStratum.ParticleChapter");
                    particleChapter.Set("Compare", 1);
                    particleChapter.Set("Chapter", placement.Chapter);
                    var container = scn.RSZ!.CreateInstance("chainsaw.RuleStratum.Container");
                    container.Set("_Data", particleChapter);
                    stratumBool.GetList("_Enable.Matters").Add(container);
                }

                var gmOptionHide = scn.RSZ!.CreateInstance("chainsaw.GmOptionHide");
                gmOptionHide.Set("v0", (byte)1);
                gmOptionHide.GetList("Rule").Add(stratumBool);
                paramObject.Components.Add(gmOptionHide);

                if (gimmick.Name!.StartsWith("Biorand_MerchantTorch"))
                {
                    var objectHide = scn.RSZ!.CreateInstance("chainsaw.ObjectHide");
                    objectHide.Set("v0", (byte)1);
                    objectHide.GetList("Settings").Add(stratumBool.Clone());
                    paramObject.Components.Add(objectHide);
                }
            }

            private static Vector4 CreateRotation(Vector3 euler)
            {
                const float toRad = MathF.PI / 180.0f;
                var q = Quaternion.CreateFromYawPitchRoll(euler.X * toRad, euler.Y * toRad, euler.Z * toRad);
                return new Vector4(q.X, q.Y, q.Z, q.W);
            }

            private ContextId GetNewContextId()
            {
                return new ContextId(5, 0, 1, _contextId++);
            }

            private class FilePair
            {
                private readonly FileRepository _fileRepository;

                public string ScenePath { get; }
                public ScnFile Scene { get; }
                public UserFile User { get; }

                public string UserPath => $"{ScenePath[..^7]}_savedata.user.2";

                public FilePair(FileRepository fileRepository, string path)
                {
                    _fileRepository = fileRepository;
                    ScenePath = path;
                    Scene = fileRepository.GetScnFile(ScenePath);
                    User = fileRepository.GetUserFile(UserPath);
                }

                public void Save()
                {
                    _fileRepository.SetScnFile(ScenePath, Scene);
                    _fileRepository.SetUserFile(UserPath, User);
                }
            }
        }

        private sealed class GimmickPlacement
        {
            public int LineNumber { get; }
            public string Kind { get; }
            public int Stage { get; }
            public Vector3 Position { get; }
            public Vector3 Rotation { get; }
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
                Rotation = new Vector3(
                    float.Parse(p[5]),
                    float.Parse(p[6]),
                    float.Parse(p[7]));
                Condition = p[8];
                Chapter = string.IsNullOrEmpty(p[9]) ? 0 : ChapterId.FromNumber(Campaign.Leon, int.Parse(p[9]));
            }

            public static ImmutableArray<GimmickPlacement> GetPlacements(Campaign campaign)
            {
                var gimmicksFile = campaign == Campaign.Leon
                    ? Resources.gimmicks
                    : Resources.gimmicks_sw;

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
