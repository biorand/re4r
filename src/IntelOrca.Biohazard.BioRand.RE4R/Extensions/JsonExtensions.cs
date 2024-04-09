using System;
using System.Text.Json;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class JsonExtensions
    {
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
                JsonValueKind.Number => element.GetUInt32(),
                JsonValueKind.Null => null,
                _ => throw new NotSupportedException()
            };
        }
    }
}
