using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class Re4rRandomizerConfigurationDefinition
    {
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
                Id = $"early-case-drops",
                Label = "Front-loaded case drops",
                Description = "Larger case upgrades are guaranteed to be available by certain chapters. If disabled, you may find larger case upgrades are not available until the second half of the game.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"automatic-bolt-thrower",
                Label = "Automatic Bolt Thrower",
                Description = "If enabled, the bolt thrower can be repeatedly fired without loading a new bolt each time.",
                Type = "switch",
                Default = true
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
            group.Items.Add(new GroupItem()
            {
                Id = $"valuable-limit-weapons-per-class",
                Label = "Weapons (per class)",
                Description = "The number of different weapons per class to include in the seed. 2 would include 2 shotguns, and 2 hanguns, etc.",
                Type = "range",
                Min = 1,
                Max = 8,
                Default = 8
            });

            page = configDefinition.CreatePage("Merchant");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"extra-merchants",
                Label = "Extra Merchants",
                Description = "Add extra merchants to the game.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"merchant-buy-hold-time",
                Label = "Purchase Hold Time",
                Description = "The number of seconds you need to hold the purchase button down for. Setting to 0 is convenient for buying individual bullets. Vanilla game uses 0.6.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.05,
                Default = 0
            });

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
                Id = "random-weapon-upgrades",
                Label = "Random Weapon Upgrades",
                Description = "Let Biorand randomize the weapon upgrades.",
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
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-power-min",
                Label = "Exclusive Power Min.",
                Description = "The minimum multiplier value a power exclusive upgrade can be.",
                Type = "range",
                Default = 1.5,
                Min = 1.5,
                Max = 100,
                Step = 0.25
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-power-max",
                Label = "Exclusive Power Max.",
                Description = "The maximum multiplier value a power exclusive upgrade can be.",
                Type = "range",
                Default = 2.5,
                Min = 1.5,
                Max = 100,
                Step = 0.25
            });

            group = page.CreateGroup("Stock Increase per Chapter");
            foreach (var kind in DropKinds.ShopCompatible)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"merchant-stock-min-{kind}",
                    Label = $"Min. {DropKinds.GetLabel(kind)}",
                    Type = "scale",
                    Min = 0,
                    Max = 10000,
                    Step = 1,
                    Default = 0
                });
                group.Items.Add(new GroupItem()
                {
                    Id = $"merchant-stock-max-{kind}",
                    Label = $"Max. {DropKinds.GetLabel(kind)}",
                    Type = "scale",
                    Min = 0,
                    Max = 10000,
                    Step = 1,
                    Default = 0
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
            group.Items.Add(new GroupItem()
            {
                Id = $"inventory-additional-recipes",
                Label = "Additional Recipes",
                Description = "Adds additional recipes for crafting ammo from other ammo. Warning: replaces herb recipes which may be unwanted when stack multiplier is enabled.",
                Type = "switch",
                Default = true
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
            group.Items.Add(new GroupItem()
            {
                Id = $"enemy-strong-mini-boss",
                Label = "Strong Mini Bosses",
                Description = "Randomize mini bosses to strong elite enemies. Examples of mini bosses are bella sisters, red zealot with lantern, and garradors.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"nice-mendez-hill",
                Label = "Prevent Toxic Mendez Hill",
                Description = "Prevent difficult enemies appearing on Mendez Hill. Enable this during your permadeath runs.",
                Type = "switch",
                Default = false
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

            group = page.CreateGroup("Enemies");
            group.Warning = "Random enemy health must be enabled for these values to take affect.";
            foreach (var enemyClass in enemyClassFactory.Classes)
            {
                if (enemyClass.Key == "mendez_chase")
                    continue;

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

            foreach (var campaign in new[] { Campaign.Leon, Campaign.Ada })
            {
                group = page.CreateGroup($"Bosses ({campaign})");
                group.Warning = "Random boss health must be enabled for these values to take affect.";
                foreach (var boss in Bosses.GetByCampaign(campaign))
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
            }

            page = configDefinition.CreatePage("Gimmicks");
            group = page.CreateGroup("");
            group.Warning = "This feature is currently work in progress.";
            group.Items.Add(new GroupItem()
            {
                Id = $"ea-extra-gimmicks",
                Label = "Extra Gimmicks",
                Description = "Add extra gimmicks to the game. Gimmicks are interactable objects, like boxes, barrels, trip wires, turrets etc.",
                Type = "switch",
                Default = false
            });
            group = page.CreateGroup("Gimmicks");
            group.Items.Add(new GroupItem()
            {
                Id = $"gimmicks-breakable-containers",
                Label = "Breakable Containers",
                Description = "The amount of extra wooden boxes, barrels, vases to place.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"gimmicks-hiding-lockers",
                Label = "Hiding Lockers",
                Description = "The amount of lockers that Ashley can hide in.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 0.5
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"gimmicks-traps",
                Label = "Traps",
                Description = "The amount of bear traps, and trip wires to place.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 1
            });

            page = configDefinition.CreatePage("Debug");
            group = page.CreateGroup("");
            group.Warning = "These options are only for testing / debugging the randomizer.";
            group.Items.Add(new GroupItem()
            {
                Id = $"enable-special",
                Label = "Enable Personal Touch",
                Description = "Enables a personal touch or meme for the current user.",
                Type = "switch",
                Default = true
            });
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

            var defaultProfileBytes = ChainsawRandomizerFactory.GetDefaultProfile();
            var defaultProfileJson = Encoding.UTF8.GetString(defaultProfileBytes);
            var defaultProfile = RandomizerConfiguration.FromJson(defaultProfileJson);
            foreach (var item in configDefinition.AllItems)
            {
                if (defaultProfile.TryGetValue(item.Id!, out var defaultOverride))
                {
                    item.Default = defaultOverride;
                }
            }
            return configDefinition;
        }
    }
}
