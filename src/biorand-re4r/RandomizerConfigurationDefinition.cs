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
            public string[]? Options { get; set; }
            public object? Default { get; set; }
        }

        public static RandomizerConfigurationDefinition Create(EnemyClassFactory enemyClassFactory)
        {
            var configDefinition = new RandomizerConfigurationDefinition();

            configDefinition.Groups = new List<Group>();

            var generalGroup = new Group("General");
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"random-enemies",
                Label = "Random Enemies",
                Description = "Let Biorand randomize all the enemies in the game.",
                Type = "switch",
                Default = true
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"enemy-custom-health",
                Label = "Custom Enemy Health",
                Description = "Let Biorand randomize the enemy health using the min/max values.",
                Type = "switch",
                Default = false
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"random-items",
                Label = "Random Items",
                Description = "Let Biorand randomize all the static items in the game.",
                Type = "switch",
                Default = true
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"preserve-item-models",
                Label = "Preserve Item Models",
                Description = "When randomizing items, keep the original item model in the world.",
                Type = "switch",
                Default = false
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"random-inventory",
                Label = "Random Inventory",
                Description = "Let Biorand randomize your starting inventory.",
                Type = "switch",
                Default = true
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"random-merchant",
                Label = "Random Merchant Shop",
                Description = "Let Biorand randomize the merchant's shop.",
                Type = "switch",
                Default = true
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"random-merchant-prices",
                Label = "Random Merchant Prices",
                Description = "Let Biorand randomize the merchant's shop prices.",
                Type = "switch",
                Default = true
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"allow-bonus-items",
                Label = "Allow Bonus Weapons",
                Description = "Let Biorand include the unlockable and DLC weapons in the pool. You must have them all unlocked.",
                Type = "switch",
                Default = false
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"enemy-multiplier",
                Label = "Enemy multiplier",
                Description = "Duplicate enemies by this amount. Warning: high values can cause stability issues.",
                Type = "range",
                Min = 0.25,
                Max = 10,
                Step = 0.05,
                Default = 1
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"enemy-variety",
                Label = "Enemy Variety",
                Description = "Controls how many different enemy types you can have in a single area.",
                Type = "range",
                Min = 0,
                Max = 10,
                Step = 1,
                Default = 0
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"ammo-quantity",
                Label = "Ammo quantity",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 0.5
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"money-quantity",
                Label = "Money quantity",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 0.5
            });
            // generalGroup.Items.Add(new GroupItem()
            // {
            //     Id = $"progressive-difficulty",
            //     Label = "Progressive Difficulty",
            //     Type = "switch",
            //     Default = false
            // });
            configDefinition.Groups.Add(generalGroup);

            var inventoryGroup = new Group("Inventory");
            inventoryGroup.Items.Add(new GroupItem()
            {
                Id = $"inventory-weapon-primary",
                Label = "Primary Weapon",
                Description = "The primary weapon you start off with in your inventory.",
                Type = "dropdown",
                Options = ["random", "handgun", "shotgun", "smg", "rifle", "magnum", "none"],
                Default = "handgun"
            });
            inventoryGroup.Items.Add(new GroupItem()
            {
                Id = $"inventory-weapon-secondary",
                Label = "Secondary Weapon",
                Description = "An additional weapon you start off with in your inventory.",
                Type = "dropdown",
                Options = ["random", "handgun", "shotgun", "smg", "rifle", "magnum", "none"],
                Default = "random"
            });
            configDefinition.Groups.Add(inventoryGroup);

            var debugGroup = new Group("Debug");
            debugGroup.Items.Add(new GroupItem()
            {
                Id = $"debug-unique-enemy-hp",
                Label = "Unique Enemy HP",
                Description = "Gives every single enemy a unique HP value. Used to identify enemies within the game files.",
                Type = "switch",
                Default = false
            });
            configDefinition.Groups.Add(debugGroup);

            var dropGroup = new Group("General Drops");
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-none",
                Label = "None",
                Description = "No item is dropped.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-automatic",
                Label = "Automatic",
                Description = "Let the game decide, usually based on DA.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-ammo",
                Label = "Ammo",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-grenade",
                Label = "Grenade",
                Description = "Flash Grenades, Small Grenades, Heavy Grenades",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-health",
                Label = "Health",
                Description = "Herbs, Eggs",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-money",
                Label = "Money",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-knife",
                Label = "Knife",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-gunpowder",
                Label = "Gunpowder",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            dropGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-ratio-resource",
                Label = "Resource",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            configDefinition.Groups.Add(dropGroup);

            var dropValuableGroup = new Group("Valuable Drops");
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-ammo",
                Label = "Ammo",
                Description = "A more generous portion of ammo.",
                Type = "switch",
                Default = true
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-health",
                Label = "Health",
                Description = "First-Aid-Spray, Yellow Herbs, Fish, Golden Eggs",
                Type = "switch",
                Default = true
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-treasure",
                Label = "Treasure",
                Description = "Spinels, Velvet Blue, Gemstones etc.",
                Type = "switch",
                Default = true
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-weapon",
                Label = "Weapon",
                Description = "A weapon will be dropped, if not already in possession.",
                Type = "switch",
                Default = true
            });
            dropValuableGroup.Items.Add(new GroupItem()
            {
                Id = $"drop-valuable-attachment",
                Label = "Weapon Attachment",
                Description = "A weapon attachment will be dropped, if not already in possession.",
                Type = "switch",
                Default = true
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
                    Step = 0.01,
                    Default = 0.5
                });
            }
            configDefinition.Groups.Add(enemyGroup);

            var parasiteGroup = new Group("Parasite");
            parasiteGroup.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-none",
                Label = "None",
                Description = "No Plaga",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.75
            });
            parasiteGroup.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-a",
                Label = "Plaga Guadaña",
                Description = "Tenticle Plaga that slice you",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.25 / 4 * 3
            });
            parasiteGroup.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-b",
                Label = "Plaga Mandíbula",
                Description = "Hungry Plaga that eat your head",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.25 / 4
            });
            configDefinition.Groups.Add(parasiteGroup);

            var enemyHealthGroup = new Group("Custom Enemy Health");
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                enemyHealthGroup.Items.Add(new GroupItem()
                {
                    Id = $"enemy-health-min-{enemyClass.Key}",
                    Label = $"Min. {enemyClass.Name} HP",
                    Type = "range",
                    Min = 1,
                    Max = 100000,
                    Step = 1,
                    Default = enemyClass.MinHealth
                });
                enemyHealthGroup.Items.Add(new GroupItem()
                {
                    Id = $"enemy-health-max-{enemyClass.Key}",
                    Label = $"Max. {enemyClass.Name} HP",
                    Type = "range",
                    Min = 1,
                    Max = 100000,
                    Step = 1,
                    Default = enemyClass.MaxHealth
                });
            }
            configDefinition.Groups.Add(enemyHealthGroup);

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
