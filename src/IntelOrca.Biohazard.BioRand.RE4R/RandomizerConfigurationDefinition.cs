using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class Re4rRandomizerConfigurationDefinition
    {
        public static RandomizerConfigurationDefinition Create(EnemyClassFactory enemyClassFactory)
        {
            var configDefinition = new RandomizerConfigurationDefinition();

            #region General

            var page = configDefinition.CreatePage("General");
            var group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = "game-version",
                Label = "Game Version",
                Description = "What version of the game to generate for.",
                Type = "dropdown",
                Options = ["4 Mar 2025", "3 Feb 2026"],
                Default = "3 Feb 2026"
            });

            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = $"campaign",
                Label = "Campaign",
                Description = "Which scenario to randomize, Main Story (Leon) or Separate Ways (Ada).",
                Type = "dropdown",
                Options = ["Main Story", "Separate Ways"],
                Default = "Main Story"
            });
#if ENABLE_BETA_FEATURES
            group.Items.Add(new GroupItem()
            {
                Id = $"start-chapter",
                Label = "Start Chapter",
                Description = "Which chapter to start on.",
                Type = "range",
                Min = 1,
                Max = 16,
                Default = 1
            });
#endif
            group.Items.Add(new GroupItem()
            {
                Id = $"enable-autosave-pro",
                Label = "Professional Autosaves",
                Description = "Enable autosaves on professional difficulty",
                Type = "switch",
                Default = false
            });
#if ENABLE_BETA_FEATURES
            group.Items.Add(new GroupItem()
            {
                Id = $"disable-radio-calls",
                Label = "Disable Radio Calls",
                Description = "Disable radio calls so they do not interrupt gameplay.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"skip-ashley-section",
                Label = "Skip Ashley Section",
                Description = "Skips the Ashley segment entirely. Leon will be able to pickup the chest keys.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = "randomized-messages",
                Label = "Randomize Text",
                Description = "Randomize various text in the game to a meme.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"random-events",
                Label = "Random Events",
                Description = "Enables events that create new environments, battle arenas, key movement, and much more.",
                Type = "switch",
                Default = true
            });
#endif
            group = page.CreateGroup("");
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
                Id = $"allow-mercenaries-items",
                Label = "Allow Mercenaries Weapons",
                Description = "Let Biorand include the Mercenaries weapons (Sawed-off W-870, XM96E1) in the pool. You must have Mercenaries installed and enabled.",
                Type = "switch",
                Default = true
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"zero-bonusdlc-weapon-sell-price",
                Label = "Zero Bonus/DLC Weapon Sell Prices",
                Description = "Set the sell price of bonus/DLC weapons to 0 (except starting ones). Sell them at first merchant so they can appear in the rando later on.",
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
            group = page.CreateGroup("");
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

            #endregion

            #region Merchant

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
            group = page.CreateGroup("Stock Increase per Chapter");
            group.Advanced = true;
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

            group = page.CreateGroup($"Perks");
            group.Items.Add(new GroupItem()
            {
                Id = $"random-case-perks",
                Label = "Random Case Perks",
                Description = "Randomize the perk and multiplier of each attaché case.",
                Type = "switch",
                Default = true
            });

            group = page.CreateGroup("Stack Limits");
            group.Advanced = true;
            foreach (var dropKind in DropKinds.Stackable)
            {
                group.Items.Add(new GroupItem()
                {
                    Id = $"inventory-stack-limit-{dropKind}",
                    Label = DropKinds.GetLabel(dropKind),
                    Type = "range",
                    Min = 0,
                    Max = 9999,
                    Step = 1,
                    Default = 0
                });
            }

            #endregion

            #region Weapon

            //Weapon Page
            page = configDefinition.CreatePage("Weapon");
            page.Advanced = true;
#if ENABLE_BETA_FEATURES
            group = page.CreateGroup("Legendary Weapons");
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-legendary-quantity-min",
                Label = $"Min. Quantity of Legendary Weapons",
                Description = "Minimum number of legendary weapons available to find.",
                Type = "range",
                Min = 0,
                Max = 32,
                Default = 0
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-legendary-quantity-max",
                Label = $"Max. Quantity of Legendary Weapons",
                Description = "Maximum number of legendary weapons available to find.",
                Type = "range",
                Min = 0,
                Max = 32,
                Default = 2
            });
#endif

            group = page.CreateGroup("");
            group.Warning = "WIP Page. This page requires all Random Weapon options to be enabled in the Merchant page to function.";
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-power-scale-enabled",
                Label = $"Enable Weapon Power Scaling",
                Description = "Enables weapon scaling from this page else default randomizer settings.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-scale-enabled",
                Label = $"Enable Weapon Exclusive Scaling",
                Description = "Enables weapon exclusive scaling from this page else default randomizer settings.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-god-roll-enabled",
                Label = $"Enable Weapon God Roll Scaling",
                Description = "Enables weapon god roll scaling from this page else default randomizer settings.",
                Type = "switch",
                Default = false
            });

            // Exclusives
            group = page.CreateGroup("Exclusives");
            // Power
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-power-min",
                Label = $"Min. Exclusive Power Multiplier",
                Type = "range",
                Min = 1.25,
                Max = 20,
                Step = 0.25,
                Default = 1.5
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-power-max",
                Label = $"Max. Exclusive Power Multiplier",
                Type = "range",
                Min = 1.25,
                Max = 20,
                Step = 0.25,
                Default = 2
            });
            // Critical
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-crit-min",
                Label = $"Min. Exclusive Crit Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 3
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-crit-max",
                Label = $"Max. Exclusive Crit Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 5
            });
            // Ammo
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-ammo-min",
                Label = $"Min. Exclusive Ammo Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 2
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-ammo-max",
                Label = $"Max. Exclusive Ammo Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 4
            });
            // Penetration
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-pen-min",
                Label = $"Min. Exclusive Penetration Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 2
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-exclusive-pen-max",
                Label = $"Max. Exclusive Penetration Multiplier",
                Type = "range",
                Min = 1,
                Max = 20,
                Step = 1,
                Default = 5
            });

            group = page.CreateGroup("God Roll Bonuses");
            group.Warning = "If Enabled, these bonuses will be applied as an additional multiplier to the Max Level 5 stats for the weapon if it gets a god roll.";
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-god-roll-min",
                Label = $"Min. God Roll Scaling Bonus",
                Type = "range",
                Min = 1,
                Max = 5,
                Step = 0.1,
                Default = 1.2
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-god-roll-max",
                Label = $"Max. God Roll Scaling Bonus",
                Type = "range",
                Min = 1,
                Max = 5,
                Step = 0.1,
                Default = 1.3
            });

            group = page.CreateGroup($"Power Scaling Details");
            group.Warning = "If Enabled, a value between the min and max will be rolled and applied to the weapon's vanilla base power for Level 1 and Level 5. Example: the vanilla SG base power = 1.0 thus it will have 1.2 power at level 1 if the level 1 multiplier rolls 1.2 and will have 2.5 power at level 5 if the level 5 multiplier rolls 2.5.";

            group = page.CreateGroup($"Knife Power Scaling");
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-lv1min-knife",
                Label = $"Min. Level 1 Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 5,
                Step = 0.1,
                Default = 0.8f
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-lv1max-knife",
                Label = $"Max. Level 1 Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 5,
                Step = 0.1,
                Default = 1.2f
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-lv5min-knife",
                Label = $"Min. Level 5 Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 5,
                Step = 0.1,
                Default = 1.5f
            });

            group.Items.Add(new GroupItem()
            {
                Id = $"weapon-lv5max-knife",
                Label = $"Max. Level 5 Multiplier",
                Type = "range",
                Min = 0.5,
                Max = 5,
                Step = 0.1,
                Default = 2.5f
            });
            foreach (var sw in ItemClasses.StartingWeapons)
            {
                if (sw == ItemClasses.None)
                    continue;

                float lvl1minDefault = 1.0f;
                float lvl1maxDefault = 1.0f;
                float lvl5mindefault = 2.0f;
                float lvl5maxdefault = 2.0f;
                if (sw == ItemClasses.Handgun)
                { lvl1minDefault = 0.8f; lvl1maxDefault = 1.2f; lvl5mindefault = 1.5f; lvl5maxdefault = 3.3f; }
                else if (sw == ItemClasses.Shotgun)
                { lvl1minDefault = 0.8f; lvl1maxDefault = 1.2f; lvl5mindefault = 1.5f; lvl5maxdefault = 3.0f; }
                else if (sw == ItemClasses.Smg)
                { lvl1minDefault = 0.9f; lvl1maxDefault = 1.2f; lvl5mindefault = 1.5f; lvl5maxdefault = 3.5f; }
                else if (sw == ItemClasses.Magnum)
                { lvl1minDefault = 0.8f; lvl1maxDefault = 1.1f; lvl5mindefault = 1.3f; lvl5maxdefault = 2.5f; }
                else if (sw == ItemClasses.Rifle)
                { lvl1minDefault = 0.8f; lvl1maxDefault = 1.2f; lvl5mindefault = 1.5f; lvl5maxdefault = 3.0f; }
                else if (sw == ItemClasses.Bolt)
                { lvl1minDefault = 1.0f; lvl1maxDefault = 1.0f; lvl5mindefault = 2.0f; lvl5maxdefault = 3.0f; }
                else if (sw == ItemClasses.Arrow)
                { lvl1minDefault = 1.0f; lvl1maxDefault = 1.0f; lvl5mindefault = 2.0f; lvl5maxdefault = 3.0f; }
                else if (sw == ItemClasses.Flame)
                { lvl1minDefault = 0.9f; lvl1maxDefault = 1.2f; lvl5mindefault = 1.8f; lvl5maxdefault = 2.2f; }

                group = page.CreateGroup($"{sw.ToTitleCase()} Power Scaling");
                group.Items.Add(new GroupItem()
                {
                    Id = $"weapon-lv1min-{sw}",
                    Label = $"Min. Level 1 Multiplier",
                    Type = "range",
                    Min = 0.5,
                    Max = 5,
                    Step = 0.1,
                    Default = lvl1minDefault
                });

                group.Items.Add(new GroupItem()
                {
                    Id = $"weapon-lv1max-{sw}",
                    Label = $"Max. Level 1 Multiplier",
                    Type = "range",
                    Min = 0.5,
                    Max = 5,
                    Step = 0.1,
                    Default = lvl1maxDefault
                });

                group.Items.Add(new GroupItem()
                {
                    Id = $"weapon-lv5min-{sw}",
                    Label = $"Min. Level 5 Multiplier",
                    Type = "range",
                    Min = 0.5,
                    Max = 5,
                    Step = 0.1,
                    Default = lvl5mindefault
                });

                group.Items.Add(new GroupItem()
                {
                    Id = $"weapon-lv5max-{sw}",
                    Label = $"Max. Level 5 Multiplier",
                    Type = "range",
                    Min = 0.5,
                    Max = 5,
                    Step = 0.1,
                    Default = lvl5maxdefault
                });
            }

            #endregion

            #region Valuables

            page = configDefinition.CreatePage("Valuables");
            group = page.CreateGroup("");
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-per-chapter-min",
                Label = "Min. Average Chapter Treasure (× 1,000)",
                Description = "The minimum total value of all treasure to include for an average length chapter.",
                Type = "range",
                Min = 0,
                Max = 1_000,
                Default = 50
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-per-chapter-max",
                Label = "Max. Average Chapter Treasure (× 1,000)",
                Description = "The maximum total value of all treasure to include for an average length chapter.",
                Type = "range",
                Min = 0,
                Max = 1_000,
                Default = 200
            });
            group = page.CreateGroup("Treasure Classes");
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-ratio-low",
                Label = "Low",
                Description = "Higher ratio will result in many smaller treasures.",
                Category = new GroupItemCategory()
                {
                    Label = "Low",
                    BackgroundColor = "#696",
                    TextColor = "#fff"
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.8
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-ratio-container",
                Label = "Container",
                Description = "Ratio of number of treasures that contain slots for other treasures. " +
                    "Compatible treasures will be also placed to fit inside the treasure.",
                Category = new GroupItemCategory()
                {
                    Label = "Container",
                    BackgroundColor = "#66f",
                    TextColor = "#fff"
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-ratio-high",
                Label = "High",
                Description = "Higher ratio will result in fewer large treasures.",
                Category = new GroupItemCategory()
                {
                    Label = "High",
                    BackgroundColor = "#ff0",
                    TextColor = "#000"
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.1
            });
            group = page.CreateGroup("Per-treasure Options");
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-slot-multiplier",
                Label = "Slotted Treasure Multiplier",
                Description = "The number of compatible treasures to place per slot. " +
                    "E.g. 2 will place twice as many compatible treasures per container slot.",
                Type = "range",
                Min = 0,
                Max = 5,
                Step = 0.1,
                Default = 1
            });
            group = page.CreateGroup("Treasure Distribution");
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-distribution-enemies",
                Label = "Enemies",
                Description = "Ratio of treasure found as enemy drops.",
                Category = new GroupItemCategory()
                {
                    Label = "Enemy",
                    BackgroundColor = "#f00",
                    TextColor = "#fff"
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.75
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-distribution-items",
                Label = "Items",
                Description = "Ratio of treasure found as static items.",
                Category = new GroupItemCategory()
                {
                    Label = "Item",
                    BackgroundColor = "#ff0",
                    TextColor = "#000"
                },
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.25
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-reward-min",
                Label = "Min. Treasure Merchant Rewards",
                Description = "The minimum number of treasures to include as a merchant reward.",
                Type = "range",
                Min = 0,
                Max = 5,
                Step = 1,
                Default = 0
            });
            group.Items.Add(new GroupItem()
            {
                Id = "treasure-reward-max",
                Label = "Max. Treasure Merchant Rewards",
                Description = "The maximum number of treasures to include as a merchant reward.",
                Type = "range",
                Min = 0,
                Max = 5,
                Step = 1,
                Default = 5
            });

            #endregion

            #region Items

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

            #endregion

            #region Enemies

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
#if ENABLE_BETA_FEATURES
            group.Items.Add(new GroupItem()
            {
                Id = "enemy-waves-min",
                Label = "Min. Enemy Waves",
                Description = "The minimum number of waves per enemy. A value of 2 will mean a new enemy is spawned for each enemy killed.",
                Type = "range",
                Min = 2,
                Max = 50,
                Step = 1,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "enemy-waves-max",
                Label = "Max. Enemy Waves",
                Description = "The maximum number of waves per enemy. A value of 4 will mean some enemies will get another 3 extra enemies which spawn in, one after another, when the last one is killed.",
                Type = "range",
                Min = 2,
                Max = 50,
                Step = 1,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "enemy-waves-probability",
                Label = "Enemy Wave Probability",
                Description = "The percentage of enemy spawns that will have waves.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "enemy-waves-distance",
                Label = "Enemy Wave Distance",
                Description = "The minimum distance the player needs to be for a spawn point to spawn a new enemy.",
                Type = "range",
                Min = 1,
                Max = 100,
                Step = 1,
                Default = 5
            });
#endif
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
                Id = $"balanced-enemies",
                Label = "Balanced Enemies",
                Description = "Restrict certain enemies to a set of types that produce a more fair and consistent randomizer. Good for permadeath runs but may reduce chaos.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"nice-mendez-hill",
                Label = "Safer Mendez Hill / Krauser Fight",
                Description = "Prevent difficult enemies appearing on Mendez Hill and first Krauser fight. Enable this during your permadeath runs.",
                Type = "switch",
                Default = false
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"ashley-safe-enemies",
                Label = "Safer Ashley Escorting",
                Description = "Disable enemies that can easily kill Ashley during chapters where you are escorting her.",
                Type = "switch",
                Default = false
            });
#if ENABLE_BETA_FEATURES
            group.Items.Add(new GroupItem()
            {
                Id = $"mendez-down-resistance",
                Label = "Mendez Down Resistance",
                Description = "Higher percentage will increase required number of hits on Mendez to down him.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.1,
                Default = 0.2
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"arana-latch-probability",
                Label = "Araña Latch Probability",
                Description = "The probability that an Araña (spider plaga thing) will latch onto another enemy and control it.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            group.Items.Add(new GroupItem()
            {
                Id = $"enemies-unleashed",
                Label = "Enemies Unleashed",
                Description = "Reduce the colliders for Verdugo and Garrador so they can move more freely. Yes, they will run through doors.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 1,
                Default = 1
            });
#endif
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
            group.Items.Add(new GroupItem()
            {
                Id = $"parasite-ratio-c",
                Label = "Plaga Araña",
                Category = new GroupItemCategory(new ConfigCategory("Araña", "#0f0", "#000")),
                Description = "Spider Plaga that come off and attack you or control another enemy.",
                Type = "range",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.05
            });

            #endregion

            #region Health

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
                Id = "gimmicks-breakable-containers",
                Label = "Breakable Containers",
                Description = "The amount of extra wooden boxes, barrels, vases to place.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "gimmicks-hiding-lockers",
                Label = "Hiding Lockers",
                Description = "The amount of lockers that Ashley can hide in.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 0.5
            });
            group.Items.Add(new GroupItem()
            {
                Id = "gimmicks-traps",
                Label = "Traps",
                Description = "The amount of bear traps, and trip wires to place.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 1
            });
            group.Items.Add(new GroupItem()
            {
                Id = "gimmicks-exploding-containers",
                Label = "Exploding Containers",
                Description = "The amount of breakable containers which explode.",
                Type = "percent",
                Min = 0,
                Max = 1,
                Step = 0.01,
                Default = 1
            });

            #endregion

            #region Debug

            page = configDefinition.CreatePage("Debug");
            page.Advanced = true;
            group = page.CreateGroup("");
            group.Warning = "These options are only for testing / debugging the randomizer.";
#if ENABLE_BETA_FEATURES
            group.Items.Add(new GroupItem()
            {
                Id = "debug-download-data",
                Label = "Download Data",
                Description = "Download latest spreadsheet data before generating the randomizer.",
                Type = "switch",
                Default = false
            });
#endif
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

            #endregion

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
