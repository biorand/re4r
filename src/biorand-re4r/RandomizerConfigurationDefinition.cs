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
            public string? Description { get; set; }
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
                Id = $"ammo-quantity",
                Label = "Ammo quantity",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.1
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"progressive-difficulty",
                Label = "Progressive Difficulty",
                Type = "switch"
            });
            configDefinition.Groups.Add(generalGroup);

            var dropGroup = new Group("General Drops");
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-none",
                Label = "None",
                Description = "No item is dropped.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-automatic",
                Label = "Automatic",
                Description = "Let the game decide, usually based on DA.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-ammo",
                Label = "Ammo",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-grenade",
                Label = "Grenade",
                Description = "Flash Grenades, Small Grenades, Heavy Grenades",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-health",
                Label = "Health",
                Description = "Herbs, Eggs",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-money",
                Label = "Money",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-other",
                Label = "Other",
                Description = "Knives, Resources, Gunpowder",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01
            });
            configDefinition.Groups.Add(dropGroup);

            var dropValuableGroup = new Group("Valuable Drops");
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-ammo",
                Label = "Ammo",
                Description = "A more generous portion of ammo.",
                Type = "switch"
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-health",
                Label = "Health",
                Description = "First-Aid-Spray, Yellow Herbs, Fish, Golden Eggs",
                Type = "switch"
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-treasure",
                Label = "Treasure",
                Description = "Spinels, Velvet Blue, Gemstones etc.",
                Type = "switch"
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-weapon",
                Label = "Weapon",
                Description = "A weapon will be dropped, if not already in possession.",
                Type = "switch"
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-attachment",
                Label = "Weapon Attachment",
                Description = "A weapon attachment will be dropped, if not already in possession.",
                Type = "switch"
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-other",
                Label = "Other",
                Description = "Small keys / Recipes / Charms / Tokens",
                Type = "switch"
            });
            configDefinition.Groups.Add(dropValuableGroup);

            var enemyGroup = new Group("Enemies");
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                enemyGroup.Items.Add(new GroupItem()
                {
                    Id = $"enemy-ratio-{enemyClass.Key}",
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
