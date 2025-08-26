using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class RszExtensions
    {
        public static RszStructNode? FindComponent(this IRszSceneNode sceneNode, Guid gameObjectGuid, string componentName)
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
    }
}
