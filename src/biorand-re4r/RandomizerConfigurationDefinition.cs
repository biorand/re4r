using System.Text.Json;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class RandomizerConfigurationDefinition
    {
        public List<Group>? Groups { get; set; }

        public class Group(string label)
        {
            public string? Warning { get; set; }
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
                Id = $"enable-autosave-pro",
                Label = "Professional Autosaves",
                Description = "Enable autosaves on professional difficulty",
                Type = "switch",
                Default = false
            });
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
                Id = $"extra-enemies",
                Label = "Extra Enemies",
                Description = "Add extra enemies to peaceful areas and boss fights.",
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
                Label = "Enemy Multiplier",
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
                Min = 1,
                Max = 50,
                Step = 1,
                Default = 50
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"enemy-pack-max",
                Label = "Enemy Max. Pack Size",
                Description = "Controls the maximum size of an enemy pack." +
                    "Enemy packs give you groups of similar enemies rather than every individual enemy being a different type.",
                Type = "range",
                Min = 1,
                Max = 10,
                Step = 1,
                Default = 6
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"ammo-quantity",
                Label = "Ammo Quantity",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 0.5
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"money-drop-min",
                Label = "Min. Money Drop",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 100
            });
            generalGroup.Items.Add(new GroupItem()
            {
                Id = $"money-drop-max",
                Label = "Max. Money Drop",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 1000
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
            foreach (var dropKind in DropKinds.Generic)
            {
                dropGroup.Items.Add(new GroupItem()
                {
                    Id = $"drop-ratio-{dropKind}",
                    Label = dropKind.Replace("-", " ").ToTitleCase(),
                    Description = dropKind switch
                    {
                        DropKinds.None => "No item is dropped.",
                        DropKinds.Automatic => "Let the game decide, usually based on DA.",
                        _ => null
                    },
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01,
                    Default = 0.5
                });
            }
            configDefinition.Groups.Add(dropGroup);

            var dropValuableGroup = new Group("Valuable Drops");
            foreach (var dropKind in DropKinds.HighValue)
            {
                dropValuableGroup.Items.Add(new GroupItem()
                {
                    Id = $"drop-valuable-{dropKind}",
                    Label = dropKind.Replace("-", " ").ToTitleCase(),
                    Type = "switch",
                    Default = true
                });
            }
            configDefinition.Groups.Add(dropValuableGroup);

            var enemyGroup = new Group("Enemies");
            enemyGroup.Warning = "It is recommended to leave pig, pesanta, u3, and krauser (mutated) at 0 as it currently causes some crashes.";
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                var defaultValue = 0.5;
                if (enemyClass.Key == "pig" ||
                    enemyClass.Key == "krauser_2" ||
                    enemyClass.Key == "pesanta" ||
                    enemyClass.Key == "u3")
                {
                    defaultValue = 0;
                }

                enemyGroup.Items.Add(new GroupItem()
                {
                    Id = $"enemy-ratio-{enemyClass.Key}",
                    Label = enemyClass.Name,
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01,
                    Default = defaultValue
                });
            }
            configDefinition.Groups.Add(enemyGroup);

            var parasiteGroup = new Group("Parasite");
            parasiteGroup.Warning = "It is recommended to leave parasite at 0 as it currently causes some crashes.";
            parasiteGroup.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-none",
                Label = "None",
                Description = "No Plaga",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0
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
                Default = 0
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
                Default = 0
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
