using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickModifier : Modifier
    {
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
                        if (g.Kind == "GmSmoothWoodBox")
                        {
                            var component = g.GameObject.FindComponent("chainsaw.GmSmoothWoodBox");
                            component?.Set("_RandomDropItemNum", 6);
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
