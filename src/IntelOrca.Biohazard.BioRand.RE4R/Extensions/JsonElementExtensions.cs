using System;
using System.Text.Json;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class JsonElementExtensions
    {
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
