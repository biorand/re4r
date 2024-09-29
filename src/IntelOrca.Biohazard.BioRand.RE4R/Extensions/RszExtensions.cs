using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
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

        public static RszInstance? FindComponent(this ScnFile scnFile, Guid gameObjectGuid, string name)
        {
            return FindGameObject(scnFile, gameObjectGuid)?.FindComponent(name);
        }

        public static RszInstance? FindComponent(this IGameObjectData gameObject, string name)
        {
            foreach (var component in gameObject.Components)
            {
                var componentName = component.Name;
                var nameEnd = componentName.IndexOf('[');
                if (nameEnd != -1)
                    componentName = componentName.Substring(0, nameEnd);
                if (componentName == name)
                    return component;
            }
            return null;
        }

        public static int GetLength(this RszInstance instance, string xpath)
        {
            var list = GetList(instance, xpath);
            return list.Count;
        }

        public static List<T> GetArray<T>(this RszInstance instance, string xpath)
        {
            var list = GetList(instance, xpath);
            return list.Cast<T>().ToList();
        }

        public static List<object?> GetList(this RszInstance instance, string xpath)
        {
            return Get<List<object>?>(instance, xpath)!;
        }

        public static T? Get<T>(this RszInstance instance, string xpath, bool? relaxed = false)
        {
            return (T?)Get(instance, xpath, relaxed);
        }

        public static object? Get(this RszInstance instance, string xpath, bool? relaxed = false)
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
                    var list = (List<object>?)value;
                    if (list == null || list.Count <= arrayIndex)
                        return null;

                    value = list[arrayIndex];
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

        public static ScnFile.GameObjectData CreateGameObject(this ScnFile scnFile, string name)
        {
            var gameObject = scnFile.IterAllGameObjects(true).First(x => x.Children.Count == 0);
            var newGameObject = scnFile.ImportGameObject(gameObject, null, null, true);
            newGameObject.Folder = null;
            newGameObject.Parent = null;
            newGameObject.Instance!.SetFieldValue("v0", name);
            newGameObject.Instance!.SetFieldValue("v1", "");
            newGameObject.Instance!.SetFieldValue("v2", (byte)1);
            newGameObject.Instance!.SetFieldValue("v3", (byte)1);
            newGameObject.Instance!.SetFieldValue("v4", -1.0f);
            newGameObject.Components.Clear();
            return newGameObject;
        }

        public static Dictionary<string, object> ToDictionary(this RszInstance instance)
        {
            var dict = new Dictionary<string, object>();
            for (var i = 0; i < instance.Fields.Length; i++)
            {
                var field = instance.Fields[i];
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

        public static T Deserialize<T>(this RszParser parser, RszInstance instance)
        {
            return (T)Deserialize(parser, instance);
        }

        public static object Deserialize(this RszParser parser, RszInstance instance)
        {
            var dotNetType = GetDotNetType(instance.RszClass) ?? throw new ArgumentException($"{GetDotNetName(instance.RszClass)} not found");
            var obj = Activator.CreateInstance(dotNetType)!;
            foreach (var property in dotNetType.GetProperties())
            {
                var value = instance.GetFieldValue(property.Name);
                if (value is RszInstance child)
                {
                    property.SetValue(obj, Deserialize(parser, child));
                }
                else if (value is List<object> list)
                {
                    property.SetValue(obj, DeserializeList(list, property.PropertyType.GenericTypeArguments[0]));
                }
                else
                {
                    property.SetValue(obj, value);
                }
            }
            return obj;

            object DeserializeList(List<object> list, Type elementType)
            {
                var result = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
                for (var i = 0; i < list.Count; i++)
                {
                    var value = list[i];
                    if (value is RszInstance child)
                    {
                        value = Deserialize(parser, child);
                    }
                    else if (value is List<object> childList)
                    {
                        value = DeserializeList(list, elementType.GetElementType()!);
                    }
                    result.Add(value);
                }
                return result;
            }
        }

        [return: NotNullIfNotNull(nameof(obj))]
        public static RszInstance? Serialize(this RszParser parser, object? obj)
        {
            if (obj == null)
                return null;

            var objType = obj.GetType();
            var objTypeName = objType.FullName!.Replace("+", ".");
            var rszClass = parser.GetRSZClass(objTypeName) ?? throw new ArgumentException("Class not found");
            var result = RszInstance.CreateInstance(parser, rszClass);

            foreach (var property in objType.GetProperties())
            {
                var value = property.GetValue(obj);
                if (value is null)
                    continue;

                var rszField = result.Fields.FirstOrDefault(x => x.name == property.Name);
                if (rszField == null)
                    continue;

                if (rszField.array)
                {
                    var valueAsList = (IList)value;
                    var list = (List<object?>)result.GetFieldValue(rszField.name)!;
                    for (var i = 0; i < valueAsList.Count; i++)
                    {
                        var valueAtIndex = valueAsList[i];
                        if (rszField.type == RszFieldType.Object)
                        {
                            list.Add(Serialize(parser, valueAtIndex));
                        }
                        else
                        {
                            list.Add(valueAtIndex);
                        }
                    }
                }
                else if (rszField.type == RszFieldType.Object)
                {
                    var serializedValue = Serialize(parser, value);
                    if (serializedValue is null)
                        continue;

                    result.SetFieldValue(rszField.name, serializedValue);
                }
                else
                {
                    result.SetFieldValue(rszField.name, value);
                }
            }

            return result;
        }

        private static Type? GetDotNetType(RszClass rszClass)
        {
            var dotNetName = GetDotNetName(rszClass);
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(x => x.FullName == dotNetName);
        }

        private static string GetDotNetName(RszClass rszClass)
        {
            var parts = rszClass.name.Split('.');
            var sb = new StringBuilder();
            var classHit = false;
            foreach (var p in parts)
            {
                sb.Append(p);
                if (classHit || char.IsUpper(p[0]))
                {
                    classHit = true;
                    sb.Append('+');
                }
                else
                {
                    sb.Append('.');
                }
            }
            sb.Remove(sb.Length - 1, 1);
            return sb.ToString();
        }

        public static string[] GetClasses(this RszParser parser, string className)
        {
            var set = new HashSet<string>();
            GetClasses(parser, className, set);
            return set.OrderBy(x => x).ToArray();
        }

        private static void GetClasses(this RszParser parser, string className, HashSet<string> classes)
        {
            var rszClass = parser.GetRSZClass(className);
            if (rszClass == null)
                throw new ArgumentException("Class not found");

            classes.Add(className);
            foreach (var field in rszClass.fields)
            {
                if (field.type != RszFieldType.Object)
                    continue;

                var subTypeName = field.array
                    ? field.original_type[..^2]
                    : field.original_type;
                GetClasses(parser, subTypeName, classes);
            }
        }

        public static string GetCsharpClass(this RszParser parser, string className)
        {
            var allRequiredClasses = GetClasses(parser, className)
                .Select(GetOuterMostClass)
                .Distinct()
                .ToArray();

            var builder = new CsharpClassBuilder();
            foreach (var c in allRequiredClasses.GroupBy(GetNamespace))
            {
                builder.Namespace(c.Key);
                builder.OpenBrace();
                foreach (var cc in c)
                {
                    BuildClass(builder, parser, cc);
                }
                builder.CloseBrace();
            }
            return builder.ToString();

            static void BuildClass(CsharpClassBuilder builder, RszParser parser, string className)
            {
                var rszClass = parser.GetRSZClass(className) ?? throw new ArgumentException("Class not found");
                var innerClasses = parser.ClassDict.Values
                    .Select(x => x.name)
                    .Where(x => x.IndexOfAny(['[', '<']) == -1)
                    .Where(x => x.StartsWith(className + '.') && !x[(className.Length + 1)..].Any(x => x == '.'))
                    .ToArray();
                var nameParts = rszClass.name.Split('.');

                if (IsEnum(rszClass))
                {
                    builder.Enum(nameParts[^1]);
                    builder.OpenBrace();
                    builder.CloseBrace();
                }
                else
                {
                    builder.Class(nameParts[^1]);
                    builder.OpenBrace();
                    foreach (var field in rszClass.fields)
                    {
                        string typeName;
                        if (field.type == RszFieldType.Object)
                        {
                            typeName = field.original_type;
                            if (typeName.StartsWith(className + '.'))
                            {
                                typeName = typeName[(className.Length + 1)..];
                            }
                            if (field.array)
                            {
                                typeName = $"System.Collections.Generic.List<{typeName.Substring(0, typeName.Length - 2)}>";
                            }
                        }
                        else
                        {
                            var nativeType = RszInstance.RszFieldTypeToCSharpType(field.type);
                            typeName = nativeType.FullName!;
                            if (field.array)
                            {
                                typeName = $"System.Collections.Generic.List<{typeName}>";
                            }
                        }

                        builder.Property(typeName, field.name);
                    }
                    foreach (var innerClass in innerClasses)
                    {
                        BuildClass(builder, parser, innerClass);
                    }
                    builder.CloseBrace();
                }
            }

            static string GetNamespace(string className)
            {
                var nameParts = className.Split('.');
                var ns = string.Join(".", nameParts.TakeWhile(IsNamespace));
                return ns;
            }

            static bool IsNamespace(string s)
            {
                return char.IsLower(s[0]);
            }

            static string GetOuterMostClass(string className)
            {
                var ns = GetNamespace(className);
                var i = className.IndexOf('.', ns.Length + 1);
                if (i == -1)
                    return className;
                return className[..i];
            }

            static bool IsEnum(RszClass cls)
            {
                return cls.fields.Length == 1 && cls.fields[0].name == "value__";
            }
        }

        private sealed class CsharpClassBuilder
        {
            private StringBuilder _sb = new StringBuilder();
            private int _indent;

            public int Index { get; set; }

            public void Namespace(string ns) => AppendLine($"namespace {ns}");
            public void Class(string name) => AppendLine($"internal class {name}");
            public void Enum(string name) => AppendLine($"internal enum {name}");
            public void Property(string type, string name) => AppendLine($"public {type} {name} {{ get; set; }}");

            public void OpenBrace()
            {
                AppendLine("{");
                _indent++;
            }

            public void CloseBrace()
            {
                _indent--;
                AppendLine("}");
            }

            private void AppendLine(string s)
            {
                _sb.Append(' ', _indent * 4);
                _sb.AppendLine(s);
            }

            public override string ToString() => _sb.ToString();
        }
    }
}
