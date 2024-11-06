using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickModifier : Modifier
    {
        private static ScnFile? _template = null;

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileRepository = randomizer.FileRepository;
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            foreach (var path in areaRepo.Gimmicks)
            {
                logger.Push($"{Path.GetFileName(path)}");

                var scnFile = fileRepository.GetScnFile(path);
                var gimmicks = GetGimmicksFromScn(scnFile);
                foreach (var g in gimmicks)
                {
                    var props = g.Properties.OrderBy(x => x.Key).ToArray();
                    logger.LogLine($"{g.ContextId} Gimmick = {g.Kind} {string.Join(" ", props.Select(x => $"{x.Key} = {x.Value}"))}");
                }

                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (randomizer.Campaign != Campaign.Leon)
                return;

            var bawk = randomizer.HasSpecialTouch("bawk");
            var extraMerchants = randomizer.GetConfigOption("extra-merchants", true);

            var fileRepository = randomizer.FileRepository;
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            var gimmickPaths = areaRepo.Gimmicks.ToArray();

            var factory = new GimmickFactory(randomizer, gimmickPaths);
            var placements = GimmickPlacement.GetPlacements();
            foreach (var placement in placements)
            {
                var kind = placement.Kind;
                if (kind == "bawk" && !bawk)
                    continue;

                if (kind.Contains("Merchant") && !extraMerchants)
                    continue;

                factory.AddGimmick(placement);
            }
            factory.SaveAll();
        }

        private void ModifyChickens(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var fileRepository = randomizer.FileRepository;
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            foreach (var path in areaRepo.Gimmicks)
            {
                logger.Push($"{Path.GetFileName(path)}");

                fileRepository.ModifyScnFile(path, scnFile =>
                {
                    var gimmicks = GetGimmicksFromScn(scnFile);
                    foreach (var g in gimmicks)
                    {
                        if (g.Kind == "GmChicken")
                        {
                            SetChickenDropItem(g, new Item(ItemIds.RocketLauncher, 1));
                        }
                    }
                });

                logger.Pop();
            }
        }

        private static Gimmick[] GetGimmicksFromScn(ScnFile scnFile)
        {
            var result = new List<Gimmick>();
            var gameObjects = scnFile.IterAllGameObjects(false).ToArray();
            foreach (var go in gameObjects)
            {
                var gimmick = Gimmick.FromGameObject(go);
                if (gimmick != null)
                {
                    result.Add(gimmick);
                }
            }
            return result.ToArray();
        }

        private static void SetChickenDropItem(Gimmick gimmick, Item item)
        {
            var gmChicken = gimmick.GameObject.FindComponent("chainsaw.GmChicken");
            if (gmChicken != null)
            {
                var paramObject = gimmick.GameObject
                    .GetChildren()
                    .FirstOrDefault(x => x.Name == "ParamObject");
                if (paramObject != null)
                {
                    var gmOptionDropItem = paramObject.FindComponent("chainsaw.GmOptionDropItem");
                    if (gmOptionDropItem != null)
                    {
                        gmChicken.Set("_LaysEgg", true);
                        gmOptionDropItem.Set("ID", item.Id);
                        gmOptionDropItem.Set("Count", item.Count);
                    }
                }
            }
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
                var gimmick = CloneGimmickFromTemplate(kind);

                var gimmickCore = gimmick.FindComponent("chainsaw.GimmickCore")!;
                contextId.CopyTo(gimmickCore.Get<RszInstance>("_ID")!);

                var gimmickTransform = gimmick.FindComponent("via.Transform")!;
                gimmickTransform.Set("v0", new Vector4(placement.Position, 1));
                gimmickTransform.Set("v1", CreateRotation(placement.Rotation));
                gimmickTransform.Set("v2", new Vector4(1, 1, 1, 0));

                if (kind == "Biorand_WoodenBarrel" || kind == "Biorand_WoodenBox")
                {
                    AddChildHide(gimmick, 0, "Before");
                    AddChildDisp(gimmick, 0, "After");
                }
                else if (kind == "Biorand_MerchantTorch")
                {
                    AddChildEffect(gimmick, 0, "VFXParent");
                }
                else if (kind == "Biorand_Tripwire")
                {
                    AddChildHide(gimmick, 0, "Sender", "Laser", "Receiver");
                    AddChildHide(gimmick, 1, "Sender", "Laser", "Receiver");
                    AddChildHide(gimmick, 2, "Laser", "Battery");
                    AddChildHide(gimmick, 3, "Sender", "Laser", "Receiver");
                    AddChildEffect(gimmick, 0, "EffectPos_Sender", "EffectPos_Receiver");
                    AddChildEffect(gimmick, 1, "EffectPos_Sender", "EffectPos_Receiver");
                    FindChildRecursive(gimmick, "GimmickAimAssist")!
                        .FindComponent("chainsaw.GimmickAimAssist")!
                        .Set("_TargetObjRef", FindChildRecursive(gimmick, "AimAssistPointObj")!.Guid);
                }

                AddCondition(filePair.Scene, gimmick, placement);

                filePair.Scene.ImportGameObject(gimmick);

                var userData = filePair.User.RSZ!.CreateInstance("chainsaw.GimmickSaveDataTable.Data");
                contextId.CopyTo(userData.Get<RszInstance>("ID")!);
                userData.GetList("Save.Attr").AddRange([(byte)0, (byte)0, (byte)0, (byte)0]);
                var dataList = filePair.User.RSZ.ObjectList[0].GetList("Datas");
                dataList.Add(userData);
            }

            private static void AddChildHide(ScnFile.GameObjectData gimmick, int index, params string[] targetChildNames)
            {
                AddChildHideDisp("Hide", gimmick, index, targetChildNames);
            }

            private static void AddChildDisp(ScnFile.GameObjectData gimmick, int index, params string[] targetChildNames)
            {
                AddChildHideDisp("Disp", gimmick, index, targetChildNames);
            }

            private static void AddChildHideDisp(string kind, ScnFile.GameObjectData gimmick, int index, params string[] targetChildNames)
            {
                var gimmickObjectHideSettings = gimmick.FindComponent("chainsaw.ObjectHideSettings");
                if (gimmickObjectHideSettings != null)
                {
                    var list = gimmickObjectHideSettings.GetList($"_Params._Params[{index}].{kind}");
                    list.Clear();
                    foreach (var name in targetChildNames)
                    {
                        var child = FindChildRecursive(gimmick, name);
                        if (child != null)
                        {
                            list.Add(child.Guid);
                        }
                    }
                }
            }

            private static void AddChildEffect(ScnFile.GameObjectData gimmick, int index, params string[] targetChildNames)
            {
                var gimmickEffectsPlaySettings = gimmick.FindComponent("chainsaw.EffectsPlaySettings");
                if (gimmickEffectsPlaySettings != null)
                {
                    for (var i = 0; i < targetChildNames.Length; i++)
                    {
                        var child = FindChildRecursive(gimmick, targetChildNames[i]);
                        if (child != null)
                        {
                            gimmickEffectsPlaySettings.Set($"_Params._Params[{index}].Plays[{i}].TargetParent", child.Guid);
                        }
                    }
                }
            }

            private static ScnFile.GameObjectData? FindChildRecursive(ScnFile.GameObjectData parent, string name)
            {
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

                if (gimmick.Name == "Biorand_MerchantTorch")
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
                return new ContextId(1, 0, 1, _contextId++);
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

        private static ScnFile.GameObjectData CloneGimmickFromTemplate(string name)
        {
            var template = _template;
            if (template == null)
            {
                template = ChainsawRandomizerFactory.Default.ReadScnFile(Resources.gimmick_template);
                _template = template;
            }

            var gameObject = template
                .IterAllGameObjects()
                .First(x => x.Name == name);
            return (ScnFile.GameObjectData)gameObject.Clone();
        }

        private class Gimmick
        {
            public IGameObjectData GameObject { get; }
            public string Kind { get; }
            public ContextId ContextId { get; }
            public ImmutableDictionary<string, object> Properties { get; }

            public Gimmick(IGameObjectData gameObject, string kind, ContextId contextId, ImmutableDictionary<string, object> properties)
            {
                GameObject = gameObject;
                Kind = kind;
                ContextId = contextId;
                Properties = properties;
            }

            public static Gimmick? FromGameObject(IGameObjectData gameObject)
            {
                var coreComponent = gameObject.FindComponent("chainsaw.GimmickCore");
                if (coreComponent == null)
                    return null;

                var contextId = ContextId.FromRsz(coreComponent.Get<RszInstance>("_ID")!);

                var gmComponent = gameObject.Components.FirstOrDefault(x => x.Name.StartsWith("chainsaw.Gm"));
                if (gmComponent == null)
                {
                    return new Gimmick(gameObject, "Unknown", contextId, ImmutableDictionary<string, object>.Empty);
                }

                var kind = gmComponent.RszClass.name.Split('.').Last();
                var properties = new Dictionary<string, object>();

                if (kind == "GmSmoothWoodBox")
                {
                    var dropCount = gmComponent.Get<int>("_RandomDropItemNum");
                    properties["DropCount"] = dropCount;

                    var options = FindAllComponents(gameObject, "chainsaw.GmOptionSmoothWoodBox").FirstOrDefault();
                    if (options != null)
                    {
                        var enemyContextId = ContextId.FromRsz(options.Get<RszInstance>("_EnemyContextID")!);
                        if (enemyContextId.Category != -1)
                        {
                            properties["EnemyContextId"] = enemyContextId;
                        }
                        properties["NoDropWoodBoxB"] = options.Get<bool>("_NoDropWoodBoxB"); ;
                    }
                }

                return new Gimmick(gameObject, kind, contextId, properties.ToImmutableDictionary());
            }

            private static IEnumerable<RszInstance> FindAllComponents(IGameObjectData gameObject, string name)
            {
                var component = gameObject.FindComponent(name);
                if (component != null)
                    yield return component;

                foreach (var child in gameObject.GetChildren())
                {
                    foreach (var c in FindAllComponents(child, name))
                        yield return c;
                }
            }
        }

        private sealed class GimmickPlacement
        {
            public string Kind { get; }
            public int Stage { get; }
            public Vector3 Position { get; }
            public Vector3 Rotation { get; }
            public string Condition { get; }
            public int Chapter { get; }

            private GimmickPlacement(string[] p)
            {
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
                Chapter = string.IsNullOrEmpty(p[9]) ? 0 : ConvertChapter(int.Parse(p[9]));
            }

            public static ImmutableArray<GimmickPlacement> GetPlacements()
            {
                return Encoding.UTF8.GetString(Resources.gimmicks)
                    .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !x.StartsWith("#") || x.Length == 0)
                    .Skip(1)
                    .Select(x => new GimmickPlacement(x.Split(',')))
                    .ToImmutableArray();
            }

            private static int ConvertChapter(int chapter)
            {
                var chapters = new int[]
                {
                    21000,// 0
                    21100,// 1
                    21200,// 2
                    21300,// 3
                    22100,// 4
                    22200,// 5
                    22300,// 6
                    23100,// 7
                    23200,// 8
                    23300,// 9
                    24100,// 10
                    24200,// 11
                    24300,// 12
                    25100,// 13
                    25200,// 14
                    25300,// 15
                    25400 // 16
                };
                return chapters[chapter];
            }
        }
    }
}
