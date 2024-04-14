using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class RszExtensions
    {
        public static ScnFile.GameObjectData? FindGameObject(this ScnFile scnFile, Guid guid)
        {
            return scnFile
                .IterAllGameObjects(true)
                .FirstOrDefault(x => x.Guid == guid);
        }

        public static void RemoveGameObject(this ScnFile scnFile, Guid guid)
        {
            var obj = FindGameObject(scnFile, guid);
            if (obj != null)
                scnFile.RemoveGameObject(obj);
        }

        public static List<object?> GetList(this RszInstance instance, string xpath)
        {
            return Get<List<object>?>(instance, xpath)!;
        }

        public static T? Get<T>(this RszInstance instance, string xpath)
        {
            return (T?)Get(instance, xpath);
        }

        public static object? Get(this RszInstance instance, string xpath)
        {
            var value = (object?)instance;
            var parts = xpath.Split('.');
            foreach (var part in parts)
            {
                var arrayStartIndex = part.IndexOf('[');
                if (arrayStartIndex == -1)
                {
                    value = ((RszInstance)value!).GetFieldValue(part);
                }
                else
                {
                    if (arrayStartIndex != 0)
                    {
                        var name = part[..arrayStartIndex];
                        value = ((RszInstance)value!).GetFieldValue(name);
                    }
                    arrayStartIndex++;
                    var arrayEndIndex = part.IndexOf("]");
                    var szArrayIndex = part[arrayStartIndex..arrayEndIndex];
                    var arrayIndex = int.Parse(szArrayIndex);
                    value = ((List<object>)value!)[arrayIndex];
                }
            }
            return value;
        }

        public static void Set(this RszInstance instance, string xpath, object? newValue)
        {
            var value = (object?)instance;
            var parts = xpath.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                var lastPart = i == parts.Length - 1;
                string? part = parts[i];
                var arrayStartIndex = part.IndexOf('[');
                if (arrayStartIndex == -1)
                {
                    var instance2 = ((RszInstance)value!);
                    if (lastPart)
                        instance2.SetFieldValue(part, newValue!);
                    else
                        value = instance2.GetFieldValue(part);
                }
                else
                {
                    if (arrayStartIndex != 0)
                    {
                        var name = part[..arrayStartIndex];
                        value = ((RszInstance)value!).GetFieldValue(name);
                    }
                    arrayStartIndex++;
                    var arrayEndIndex = part.IndexOf("]");
                    var szArrayIndex = part[arrayStartIndex..arrayEndIndex];
                    var arrayIndex = int.Parse(szArrayIndex);
                    var lst = ((List<object>)value!);
                    if (lastPart)
                        lst[arrayIndex] = newValue!;
                    else
                        value = lst[arrayIndex];
                }
            }
        }

        public static byte[] ToByteArray(this BaseRszFile scnFile)
        {
            var ms = new MemoryStream();
            var fileHandler = new FileHandler(ms);
            scnFile.WriteTo(fileHandler);
            return ms.ToArray();
        }
    }
}
