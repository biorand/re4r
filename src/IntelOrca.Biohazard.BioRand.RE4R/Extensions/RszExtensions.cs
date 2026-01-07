using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    internal static class RszExtensions
    {
        public static RszObjectNode? FindComponent(this IRszSceneNode sceneNode, Guid gameObjectGuid, string componentName)
        {
            var gameObject = sceneNode.FindGameObject(gameObjectGuid);
            return gameObject != null ? gameObject.FindComponent(componentName) : null;
        }

#if false
        public static Dictionary<string, object> ToDictionary(this RszInstance instance)
        {
            var dict = new Dictionary<string, object>();
            for (var i = 0; i < instance.Fields.Length; i++)
            {
                var field = instance.Fields[i];
                if (instance.Values.Length <= i)
                    continue;

                var value = instance.Values[i];
                if (value is RszInstance child)
                {
                    value = ToDictionary(child);
                }
                else if (value is List<object> list)
                {
                    var copy = list.ToList();
                    for (var j = 0; j < copy.Count; j++)
                    {
                        if (copy[j] is RszInstance el)
                        {
                            copy[j] = ToDictionary(el);
                        }
                    }
                    value = copy;
                }
                dict[field.name] = value;
            }
            return dict;
        }

        public static string ToSimpleJson(this RszInstance instance)
        {
            var dict = ToDictionary(instance);
            return JsonSerializer.Serialize(dict, new JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true
            });
        }
#endif

        public static RszObjectNode Serialize<T>(this RszTypeRepository repo, T obj)
        {
            return (RszObjectNode)RszSerializer.Serialize(
                repo.FromName(obj!.GetType().FullName!)!,
                obj);
        }

        public static RszScene Add(
            this RszScene scene,
            RszTypeRepository repo,
            SceneHierachyPath hier,
            RszGameObject gameObject)
        {
            var folders = hier.Folders;
            var updatedRoot = AddToNode(scene, 0);
            return (RszScene)updatedRoot;

            IRszSceneNode AddToNode(
                IRszSceneNode node,
                int index)
            {
                if (index >= folders.Count)
                {
                    // No more folders, add the game object here
                    return node.WithChildren(node.Children.Add(gameObject));
                }

                // Find or add folder
                var folderName = folders[index];
                var childIndex = node.Children
                    .FindIndex(x => x is RszFolder f && f.Name == folderName);
                var child = childIndex != -1
                    ? node.Children[childIndex]
                    : new RszFolder(
                        repo.Create("via.Folder")
                            .Set("Name", folderName)
                            .Set("Update", true)
                            .Set("Draw", true)
                            .Set("Startup", true),
                        []);

                // Add sub folders/game object
                child = AddToNode(child, index + 1);

                // Rebuild root
                return childIndex != -1
                    ? node.WithChildren(node.Children.SetItem(childIndex, child))
                    : node.WithChildren(node.Children.Add(child));
            }
        }

        public static RszGameObject WithGimmickContextId(this RszGameObject gameObject, chainsaw.ContextID contextId)
        {
            var gimmickCore = gameObject.FindComponent("chainsaw.GimmickCore")!;
            gimmickCore = gimmickCore.Set("_ID", contextId);
            gameObject = gameObject.AddOrUpdateComponent(gimmickCore);
            return gameObject;
        }
    }
}
