using System.Text.Json;

namespace IntelOrca.Biohazard.BioRand.Server.Extensions
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
    }
}
