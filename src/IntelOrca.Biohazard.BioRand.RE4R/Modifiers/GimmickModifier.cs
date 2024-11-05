using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
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

            var fileRepository = randomizer.FileRepository;
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            var gimmickPaths = areaRepo.Gimmicks.ToArray();

            var factory = new GimmickFactory(randomizer, gimmickPaths);
            factory.AddGimmick("Biorand_WoodenBarrel", 40501, new Vector3(-255, 6, 50));
            factory.AddGimmick("Biorand_MerchantTorch", 40200, new Vector3(23.8f, 2.1f, -27.0f));
            factory.AddGimmick("Biorand_Merchant", 40200, new Vector3(22.9f, 2.2f, -31.3f));
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

            public void AddGimmick(string name, int stage, Vector3 position)
            {
                var contextId = GetNewContextId();
                var filePair = GetScnForStage(stage);

                var gimmick = CloneGimmickFromTemplate(name);

                var gimmickCore = gimmick.FindComponent("chainsaw.GimmickCore")!;
                contextId.CopyTo(gimmickCore.Get<RszInstance>("_ID")!);

                var gimmickTransform = gimmick.FindComponent("via.Transform")!;
                gimmickTransform.Set("v0", new Vector4(position, 1));
                gimmickTransform.Set("v1", new Vector4(0, 0, 0, 1));
                gimmickTransform.Set("v2", new Vector4(1, 1, 1, 0));

                if (name == "Biorand_WoodenBarrel")
                {
                    var gimmickObjectHideSettings = gimmick.FindComponent("chainsaw.ObjectHideSettings");
                    if (gimmickObjectHideSettings != null)
                    {
                        var before = gimmick.Children.FirstOrDefault(x => x.Name == "Before");
                        if (before != null)
                        {
                            gimmickObjectHideSettings.Set("_Params._Params[0].Hide[0]", before.Guid);
                        }

                        var after = gimmick.Children.FirstOrDefault(x => x.Name == "After");
                        if (after != null)
                        {
                            gimmickObjectHideSettings.Set("_Params._Params[0].Disp[0]", after.Guid);
                        }
                    }
                }
                else if (name == "Biorand_MerchantTorch")
                {
                    var gimmickEffectsPlaySettings = gimmick.FindComponent("chainsaw.EffectsPlaySettings");
                    if (gimmickEffectsPlaySettings != null)
                    {
                        var vfxParent = gimmick.Children.FirstOrDefault(x => x.Name == "VFXParent");
                        if (vfxParent != null)
                        {
                            gimmickEffectsPlaySettings.Set("_Params._Params[0].Plays[0].TargetParent", vfxParent.Guid);
                        }
                    }
                }

                filePair.Scene.ImportGameObject(gimmick);

                var userData = filePair.User.RSZ!.CreateInstance("chainsaw.GimmickSaveDataTable.Data");
                contextId.CopyTo(userData.Get<RszInstance>("ID")!);
                userData.GetList("Save.Attr").AddRange([(byte)0, (byte)0, (byte)0, (byte)0]);
                var dataList = filePair.User.RSZ.ObjectList[0].GetList("Datas");
                dataList.Add(userData);
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
    }
}
