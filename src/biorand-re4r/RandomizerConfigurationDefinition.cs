using System.Text.Json;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class RandomizerConfigurationDefinition
    {
        public List<Group>? Groups { get; set; }

        public class Group(string label)
        {
            public string? Label { get; set; } = label;
            public List<GroupItem> Items { get; set; } = new List<GroupItem>();
        }

        public class GroupItem
        {
            public string? Id { get; set; }
            public string? Label { get; set; }
            public string? Type { get; set; }
            public int? Size { get; set; }
            public double? Min { get; set; }
            public double? Max { get; set; }
            public double? Step { get; set; }
        }

        public static RandomizerConfigurationDefinition Create(EnemyClassFactory enemyClassFactory)
        {
            var configDefinition = new RandomizerConfigurationDefinition();

            configDefinition.Groups = new List<Group>();

            var generalGroup = new Group("General");
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"enemy-multiplier",
                Label = "Enemy multiplier",
                Type = "range",
                Min = 0.25,
                Max = 10,
                Step = 0.05
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"progressive-difficulty",
                Label = "Progressive Difficulty",
                Type = "switch"
            });
            configDefinition.Groups.Add(generalGroup);

            var enemyGroup = new Group("Enemies");
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                enemyGroup.Items.Add(new GroupItem()
                {
                    Id = $"enemy-ratio-{enemyClass.Name}",
                    Label = enemyClass.Name,
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01
                });
            }
            configDefinition.Groups.Add(enemyGroup);

            return configDefinition;
        }

        public static Dictionary<string, object> ProcessConfig(string configJson)
        {
            var deserialized = configJson.DeserializeJson<Dictionary<string, object>>();
            return ProcessConfig(deserialized);
        }

        public static Dictionary<string, object> ProcessConfig(Dictionary<string, object>? config)
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
}
