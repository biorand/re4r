using System;
using System.Text.Json;

namespace IntelOrca.Biohazard.BioRand.Extensions
{
    internal static class JsonExtensions
    {
        public static string ToJson(this object o, bool indented = true, bool camelCase = false)
        {
            return JsonSerializer.Serialize(
                o, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = camelCase ? JsonNamingPolicy.CamelCase : null,
                    WriteIndented = indented
                })!;
        }

        public static T DeserializeJson<T>(this byte[] json)
        {
            return JsonSerializer.Deserialize<T>(
                json, new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })!;
        }

        public static T DeserializeJson<T>(this string json)
        {
            return JsonSerializer.Deserialize<T>(
                json, new JsonSerializerOptions()
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })!;
        }

        public static bool? GetBooleanProperty(this JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) ? value.GetBoolean() : null;
        }

        public static int? GetInt32Property(this JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) ? value.GetInt32() : null;
        }

        public static string? GetStringProperty(this JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) ? value.GetString() : null;
        }

        public static object? GetValue(this JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => true,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.Null => null,
                _ => throw new NotSupportedException()
            };
        }
    }
}
