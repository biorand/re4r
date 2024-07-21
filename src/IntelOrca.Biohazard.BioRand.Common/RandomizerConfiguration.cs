using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntelOrca.Biohazard.BioRand.Extensions;

namespace IntelOrca.Biohazard.BioRand
{
    [JsonConverter(typeof(RandomizerConfigurationJsonConverter))]
    public sealed class RandomizerConfiguration
    {
        private Dictionary<string, object> _dict = new Dictionary<string, object>();

        internal Dictionary<string, object> InternalDictionary => _dict;

        public RandomizerConfiguration()
        {
        }

        private RandomizerConfiguration(Dictionary<string, object> dict)
        {
            _dict = dict;
        }

        public static RandomizerConfiguration FromDictionary(Dictionary<string, object> d)
        {
            return new RandomizerConfiguration(ProcessConfig(d));
        }

        public static RandomizerConfiguration FromJson(string json)
        {
            var deserialized = json.DeserializeJson<Dictionary<string, object>>();
            return new RandomizerConfiguration(ProcessConfig(deserialized));
        }

        public object this[string key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public bool TryGetValue(string key, out object value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public T? GetValueOrDefault<T>(string key, T? defaultValue = default)
        {
            if (TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        public string ToJson(bool indented)
        {
            return _dict.ToJson(indented);
        }

        public RandomizerConfiguration Clone()
        {
            return new RandomizerConfiguration(new Dictionary<string, object>(_dict));
        }

        public static RandomizerConfiguration operator +(RandomizerConfiguration a, RandomizerConfiguration b)
        {
            var result = a.Clone();
            foreach (var kvp in b._dict)
            {
                result._dict[kvp.Key] = kvp.Value;
            }
            return result;
        }

        private static Dictionary<string, object> ProcessConfig(Dictionary<string, object>? config)
        {
            var result = new Dictionary<string, object>();
            if (config != null)
            {
                foreach (var kvp in config)
                {
                    var value = ProcessConfigValue(kvp.Value);
                    if (value is not null)
                        result[kvp.Key] = value;
                }
            }
            return result;
        }

        private static object? ProcessConfigValue(object? value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Number => ProcessNumber(element.GetDouble()),
                    JsonValueKind.String => element.GetString(),
                    _ => null
                };
            }
            return value;
        }

        private static object? ProcessNumber(double d)
        {
            var l = (long)d;
            if (l == d)
            {
                int i = (int)l;
                return i == l ? i : (object)l;
            }
            return d;
        }
    }

    public class RandomizerConfigurationJsonConverter : JsonConverter<RandomizerConfiguration>
    {
        public override RandomizerConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, RandomizerConfiguration value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.InternalDictionary, options);
        }
    }
}
