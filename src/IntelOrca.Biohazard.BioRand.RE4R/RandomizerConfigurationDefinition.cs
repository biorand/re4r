using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class RandomizerConfigurationDefinition
    {
        public List<Page> Pages { get; set; } = [];

        [JsonIgnore]
        public IEnumerable<GroupItem> AllItems
        {
            get
            {
                foreach (var p in Pages)
                {
                    foreach (var g in p.Groups)
                    {
                        foreach (var i in g.Items)
                        {
                            yield return i;
                        }
                    }
                }
            }
        }

        public Page CreatePage(string label)
        {
            var result = new Page(label);
            Pages.Add(result);
            return result;
        }

        public class Page(string label)
        {
            public string Label { get; set; } = label;
            public List<Group> Groups { get; set; } = [];

            public Group CreateGroup(string label)
            {
                var result = new Group(label);
                Groups.Add(result);
                return result;
            }
        }

        public class Group(string label)
        {
            public string Label { get; set; } = label;
            public string? Warning { get; set; }
            public List<GroupItem> Items { get; set; } = [];
        }

        public class GroupItem
        {
            public string? Id { get; set; }
            public string? Label { get; set; }
            public string? Description { get; set; }
            public GroupItemCategory? Category { get; set; }
            public string? Type { get; set; }
            public int? Size { get; set; }
            public double? Min { get; set; }
            public double? Max { get; set; }
            public double? Step { get; set; }
            public string[]? Options { get; set; }
            public object? Default { get; set; }
        }

        public class GroupItemCategory
        {
            public GroupItemCategory() { }
            public GroupItemCategory(ConfigCategory category)
            {
                Label = category.Label;
                TextColor = category.TextColor;
                BackgroundColor = category.BackgroundColor;
            }

            public string? Label { get; set; }
            public string? TextColor { get; set; }
            public string? BackgroundColor { get; set; }
        }

        public static RandomizerConfigurationDefinition Create(EnemyClassFactory enemyClassFactory)
        {
            var configDefinition = new RandomizerConfigurationDefinition();

            var page = configDefinition.CreatePage("General");
            var group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"enable-autosave-pro",
                Label = "Professional Autosaves",
                Description = "Enable autosaves on professional difficulty",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"allow-bonus-items",
                Label = "Allow Bonus Weapons",
                Description = "Let Biorand include the unlockable weapons (Primal Knife, Chicago Sweeper, Handcannon, Infinite Rocket Launcher) in the pool. You must have all the weapons unlocked.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"allow-dlc-items",
                Label = "Allow DLC Weapons",
                Description = "Let Biorand include the DLC weapons (Sentinel Nine, Skull Shaker) in the pool. You must have all the DLC weapons installed and enabled.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"valuable-limit-charm",
                Label = "Charms",
                Description = "The number of different charms to include in the seed.",
                Type = "range",
                Min = 0,
                Max = 32,
                Default = 8
            });

            page = configDefinition.CreatePage("Merchant");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-merchant",
                Label = "Random Shop",
                Description = "Let Biorand randomize the merchant's shop.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"random-merchant-prices",
                Label = "Random Shop Prices",
                Description = "Let Biorand randomize the merchant's shop prices.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"random-weapon-stats",
                Label = "Random Upgraded Weapon Stats",
                Description = "Let Biorand randomize the upgraded weapon stats.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"random-weapon-upgrade-prices",
                Label = "Random Weapon Upgrade Prices",
                Description = "Let Biorand randomize the merchant's prices for weapon upgrades.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"random-weapon-exclusives",
                Label = "Random Weapon Exclusives",
                Description = "Let Biorand randomize the weapon exclusive upgrades.",
                Type = "switch",
                Default = true
            });

            group = page.CreateGroup("Max. Stock Increase per Chapter");
            foreach (var kind in DropKinds.ShopCompatible)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"merchant-stock-max-{kind}",
                    Label = DropKinds.GetLabel(kind),
                    Type = "range",
                    Min = 0,
                    Max = 100,
                    Step = 1,
                    Default = kind.Contains("ammo") ? 0 : 5
                });
            }

            page = configDefinition.CreatePage("Inventory");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-inventory",
                Label = "Random Inventory",
                Description = "Let Biorand randomize your starting inventory.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"inventory-stack-multiplier",
                Label = "Stack Multiplier",
                Description = "Multiply every item stack size. This number will essentially be how many grenades or resources can be stacked.",
                Type = "range",
                Min = 1,
                Max = 999,
                Step = 1,
                Default = 1
            });

            foreach (var x in new[] { "Primary", "Secondary" })
            {
                group = page.CreateGroup($"{x} Weapon");
                foreach (var sw in ItemClasses.StartingWeapons)
                {
                    group.Items.Add(new GroupItem()
                    {
                        Id = $"inventory-weapon-{x.ToLowerInvariant()}-{sw}",
                        Label = sw.ToTitleCase(),
                        Type = "switch",
                        Default = true
                    });
                }
            }

            page = configDefinition.CreatePage("Items");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-items",
                Label = "Random Items",
                Description = "Let Biorand randomize all the static items in the game.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"preserve-item-models",
                Label = "Preserve Item Models",
                Description = "When randomizing items, keep the original item model in the world.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-drop-ammo-min",
                Label = "Min. Ammo Quantity",
                Description = "The minimum percentage of an ammo stack to drop.",
                Type = "percent",
                Min = 0.1,
                Max = 1,
                Step = 0.1,
                Default = 0.1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-drop-ammo-max",
                Label = "Max. Ammo Quantity",
                Description = "The maximum percentage of an ammo stack to drop.",
                Type = "percent",
                Min = 0.1,
                Max = 10,
                Step = 0.1,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-drop-money-min",
                Label = "Min. Money",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 100
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-drop-money-max",
                Label = "Max. Money",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 1000
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-treasure-drop-ratio",
                Label = "Treasure Ratio",
                Description = "The percentage of items that should be a treasure.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"item-drop-ammo-only-available-weapons",
                Label = "Ammo for available weapons only",
                Description = "Only drop ammo for weapons that are available before or in the chapter with the drop.",
                Type = "switch",
                Default = true
            });

            group = page.CreateGroup("General Drops");
            foreach (var dropKind in DropKinds.Generic)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"item-drop-ratio-{dropKind}",
                    Label = DropKinds.GetLabel(dropKind),
                    Description = dropKind switch
                    {
                        DropKinds.None => "No item is dropped.",
                        DropKinds.Automatic => "Let the game decide, usually based on DA.",
                        _ => null
                    },
                    Category = new GroupItemCategory()
                    {
                        Label = DropKinds.GetCategory(dropKind),
                        BackgroundColor = DropKinds.GetColor(dropKind).BackgroundColor,
                        TextColor = DropKinds.GetColor(dropKind).TextColor,
                    },
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01,
                    Default = 0.5
                });
            }

            group = page.CreateGroup("Valuable Drops");
            foreach (var dropKind in DropKinds.HighValue)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"item-drop-valuable-{dropKind}",
                    Label = DropKinds.GetLabel(dropKind),
                    Type = "switch",
                    Default = true
                });
            }

            page = configDefinition.CreatePage("Enemies");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-enemies",
                Label = "Random Enemies",
                Description = "Let Biorand randomize all the enemies in the game.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"extra-enemy-amount",
                Label = "Extra Enemies",
                Description = "The percentage of extra enemy spawns to add. (Includes peaceful areas, and boss arenas.)",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.25
            });
            group.Items.Add(new GroupItem()
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
            group.Items.Add(new GroupItem()
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
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-pack-max",
                Label = "Enemy Max. Pack Size",
                Description = "Controls the maximum size of an enemy pack. " +
                    "Enemy packs give you groups of similar enemies rather than every individual enemy being a different type.",
                Type = "range",
                Min = 1,
                Max = 10,
                Step = 1,
                Default = 6
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-scale-probability",
                Label = "Unusual scale probability",
                Description = "The percentage of enemies that are an unusual size.",
                Type = "percent",
                Min = 0.0,
                Max = 1,
                Step = 0.01,
                Default = 0.0
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-scale-min",
                Label = "Min. Enemy Scale",
                Description = "The minimum scale multiplier of enemies.",
                Type = "range",
                Min = 0.25,
                Max = 4.00,
                Step = 0.05,
                Default = 0.25
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-scale-max",
                Label = "Max. Enemy Scale",
                Description = "The maximum scale multiplier of enemies.",
                Type = "range",
                Min = 0.25,
                Max = 4.00,
                Step = 0.05,
                Default = 2
            });
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-enemy-drops",
                Label = "Random enemy drops",
                Description = "Let Biorand randomize the enemy drops.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-drop-ammo-only-available-weapons",
                Label = "Ammo for available weapons only",
                Description = "Only drop ammo for weapons that are available before or in the chapter with the drop.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-drop-ammo-min",
                Label = "Min. Ammo Quantity",
                Description = "The minimum percentage of an ammo stack to drop.",
                Type = "percent",
                Min = 0.1,
                Max = 1,
                Step = 0.1,
                Default = 0.1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-drop-ammo-max",
                Label = "Max. Ammo Quantity",
                Description = "The maximum percentage of an ammo stack to drop.",
                Type = "percent",
                Min = 0.1,
                Max = 1,
                Step = 0.1,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-drop-money-min",
                Label = "Min. Money Drop",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 100
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-drop-money-max",
                Label = "Max. Money Drop",
                Type = "range",
                Min = 100,
                Max = 10000,
                Step = 100,
                Default = 1000
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-treasure-drop-ratio",
                Label = "Treasure Drop Ratio",
                Description = "The percentage of enemies that should drop a treasure. Tougher enemies are more likely to drop treasure. The value of the treasure is based on the toughness of the enemy.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.25
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-ammosupply-drop-ratio",
                Label = "Large Ammo Supply Drop Ratio",
                Description = "The percentage of enemies that should drop a large supply of ammo. Tougher enemies are more likely to drop large ammo supplies. The amount of ammo dropped is based on the toughness of the enemy.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.05
            });

            group = page.CreateGroup("General Drops");
            foreach (var dropKind in DropKinds.GenericAll)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"enemy-drop-ratio-{dropKind}",
                    Label = DropKinds.GetLabel(dropKind),
                    Description = dropKind switch
                    {
                        DropKinds.None => "No item is dropped.",
                        DropKinds.Automatic => "Let the game decide, usually based on DA.",
                        _ => null
                    },
                    Category = new GroupItemCategory()
                    {
                        Label = DropKinds.GetCategory(dropKind),
                        BackgroundColor = DropKinds.GetColor(dropKind).BackgroundColor,
                        TextColor = DropKinds.GetColor(dropKind).TextColor,
                    },
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01,
                    Default = 0.5
                });
            }

            group = page.CreateGroup("Valuable Drops");
            foreach (var dropKind in DropKinds.HighValue)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"enemy-drop-valuable-{dropKind}",
                    Label = dropKind.Replace("-", " ").ToTitleCase(),
                    Type = "switch",
                    Default = true
                });
            }

            group = page.CreateGroup("Classes");
            group.Warning = "It is recommended to leave pesanta, u3, and krauser (mutated) at 0 as it currently causes some crashes.";
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                var defaultValue = 0.5;
                if (enemyClass.Key == "krauser_2" ||
                    enemyClass.Key == "pesanta" ||
                    enemyClass.Key == "u3")
                {
                    defaultValue = 0;
                }

                group.Items.Add(new GroupItem()
                {
                    Id = $"enemy-ratio-{enemyClass.Key}",
                    Label = enemyClass.Name,
                    Category = new GroupItemCategory(enemyClass.Category),
                    Type = "range",
                    Min = 0,
                    Max = 1,
                    Step = 0.01,
                    Default = defaultValue
                });
            }

            group = page.CreateGroup("Parasite");
            group.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-none",
                Label = "None",
                Category = new GroupItemCategory(new ConfigCategory("None", "#696", "#fff")),
                Description = "No Plaga",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.85
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-a",
                Label = "Plaga Guadaña",
                Category = new GroupItemCategory(new ConfigCategory("Guadaña", "#ff0", "#000")),
                Description = "Tenticle Plaga that slice you",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-b",
                Label = "Plaga Mandíbula",
                Category = new GroupItemCategory(new ConfigCategory("Mandíbula", "#f00", "#fff")),
                Description = "Hungry Plaga that eat your head",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.05
            });

            page = configDefinition.CreatePage("Health");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"boss-random-health",
                Label = "Random Boss Health",
                Description = "Let Biorand randomize the boss health using the min/max values.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-random-health",
                Label = "Random Enemy Health",
                Description = "Let Biorand randomize the enemy health using the min/max values.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-health-progressive-difficulty",
                Label = "Progressive Difficulty",
                Type = "switch",
                Default = false
            });

            group = page.CreateGroup("Bosses");
            group.Warning = "Random boss health must be enabled for these values to take affect.";
            foreach (var boss in Bosses.All)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"boss-health-min-{boss.Key}",
                    Label = $"Min. {boss.Name} HP",
                    Type = "scale",
                    Min = 0,
                    Max = 1_000_000,
                    Step = 1_000,
                    Default = 10_000
                });
                group.Items.Add(new GroupItem()
                {
                    Id = $"boss-health-max-{boss.Key}",
                    Label = $"Max. {boss.Name} HP",
                    Type = "scale",
                    Min = 0,
                    Max = 1_000_000,
                    Step = 1_000,
                    Default = 100_000
                });
            }

            group = page.CreateGroup("Enemies");
            group.Warning = "Random enemy health must be enabled for these values to take affect.";
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                // Super iron maiden HP can't be changed
                if (enemyClass.Key == "mendez_chase" ||
                    enemyClass.Key == "super_iron_maiden")
                {
                    continue;
                }

                group.Items.Add(new GroupItem()
                {
                    Id = $"enemy-health-min-{enemyClass.Key}",
                    Label = $"Min. {enemyClass.Name} HP",
                    Type = "scale",
                    Min = 0,
                    Max = 100000,
                    Step = 1,
                    Default = enemyClass.MinHealth
                });
                group.Items.Add(new GroupItem()
                {
                    Id = $"enemy-health-max-{enemyClass.Key}",
                    Label = $"Max. {enemyClass.Name} HP",
                    Type = "scale",
                    Min = 0,
                    Max = 100000,
                    Step = 1,
                    Default = enemyClass.MaxHealth
                });
            }

            page = configDefinition.CreatePage("Debug");
            group = page.CreateGroup("");
            group.Warning = "These options are only for testing / debugging the randomizer.";
            group.Items.Add(new GroupItem()
            {
                Id = $"debug-unique-enemy-hp",
                Label = "Unique Enemy HP",
                Description = "Gives every single enemy a unique HP value. Used to identify enemies within the game files.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"debug-stage-enemy-limit-default",
                Label = "Default Enemy Max. Per Stage",
                Description = "How many enemies can appear in each stage by default.",
                Type = "range",
                Min = 1,
                Max = 500,
                Step = 1,
                Default = 25
            });

            group = page.CreateGroup("Enemy Limits");
            foreach (var stage in StageIds.Stages)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"debug-stage-enemy-limit-{stage.Stage}",
                    Label = $"{stage.Stage}: {stage.Name}",
                    Description = stage.Name,
                    Type = "range",
                    Min = 0,
                    Max = 500,
                    Step = 1,
                    Default = 0
                });
            }

            var defaultProfile = configDefinition.GetDefault();
            foreach (var item in configDefinition.AllItems)
            {
                if (defaultProfile.TryGetValue(item.Id!, out var defaultOverride))
                {
                    item.Default = defaultOverride;
                }
            }
            return configDefinition;
        }

        public RandomizerConfiguration GetDefault()
        {
            var defaultProfile = ChainsawRandomizerFactory.GetDefaultProfile();
            var defaultProfileJson = Encoding.UTF8.GetString(defaultProfile);
            var defaultProfileDeserialized = RandomizerConfiguration.FromJson(defaultProfileJson);

            var result = new RandomizerConfiguration();
            foreach (var item in AllItems)
            {
                result[item.Id!] = item.Default!;
                if (defaultProfileDeserialized.TryGetValue(item.Id!, out var defaultOverride))
                {
                    result[item.Id!] = defaultOverride;
                }
            }
            return result;
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
