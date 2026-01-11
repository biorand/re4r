using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;
using Range = IntelOrca.Biohazard.REE.Rsz.Native.Range;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "legendaryweapons",
        Name = "Legendary Weapons",
        Description = "Modifies weapons in the game to legendary weapons.",
        Version = "1.0",
        Author = "MightKusKus, 7rayD")]
    [Order(10)]
    internal class LegendaryWeaponsPatch(IPatchContext context) : IPatch
    {
        private readonly WeaponBaseStats _baseStats = new(context.DynamicData);

        public void Apply()
        {
            var weapons = GetSelection();
            var weaponNames = weapons.Select(x => (string)x["name"]).ToArray();
            foreach (var wpName in weaponNames)
            {
                ApplyWeapon(wpName);
            }
        }

        private ImmutableArray<ImmutableDictionary<string, object>> GetSelection()
        {
            var randomizer = (context as FileRepository)?.Randomizer;
            if (randomizer == null)
                return [];

            var results = new List<ImmutableDictionary<string, object>>();
            var rng = randomizer.GetRng("weapons/legendary");

            var min = context.GetConfigOption("weapon-legendary-quantity-min", 0);
            var max = context.GetConfigOption("weapon-legendary-quantity-max", 0);
            var count = rng.Next(min, max + 1);

            var weapons = _baseStats.Weapons
                .Select(x => (Data: x, Weight: x["weight"] as int? ?? 0))
                .Where(x => x.Weight > 0)
                .ToList();

            while (weapons.Count > 0 && results.Count < count)
            {
                var total = weapons.Sum(x => x.Weight);
                var value = rng.Next(0, total);
                var q = 0;
                for (var i = 0; i < weapons.Count; i++)
                {
                    q += weapons[i].Weight;
                    if (value < q)
                    {
                        results.Add(weapons[i].Data);
                        weapons.RemoveAt(i);
                        break;
                    }
                }
            }

            return results
                .OrderBy(x => x["id"])
                .ToImmutableArray();
        }

        private void ApplyWeapon(string name)
        {
            var info = _baseStats.Weapons.First(x => (string)x["name"] == name);
            var id = (int)info["id"];

            var weaponService = (context as FileRepository)?.Randomizer?.GetService<WeaponService>();
            weaponService?.RestrictUpgrades(id);

            // Store exclusive message GUIDs
            Guid excDescGuid1 = Guid.Empty;
            Guid excPerkGuid1 = Guid.Empty;
            Guid excDescGuid2 = Guid.Empty;
            Guid excPerkGuid2 = Guid.Empty;

            UpdateMessages();
            UpdateShellInfo();
            UpdateBulletAttackHit();
            UpdateItemDefinition();
            UpdateWeaponEquipParam();
            UpdateShop();
            UpdateCustomFiles();

            void UpdateMessages()
            {
                context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22", msg =>
                {
                    msg.SetStringAll($"CH_Mes_Main_WEAPON_NAME_WP{id:0000}_00_0_000", (string)info["name"]);
                });
                context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22", msg =>
                {
                    msg.SetStringAll($"CH_Mes_Main_WEAPON_CAPTION_WP{id:0000}_00_0_000", (string)info["description"]);
                });
                context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_itemperks.msg.22", msg =>
                {
                    msg.SetStringAll($"CH_Mes_Main_ItemPerks_WP{id:0000}_00_0_000", (string)info["perk"]);
                });
                context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22", msg =>
                {
                    var exDesc1 = (string)info["exclusive description 1"];
                    var exPerk1 = (string)info["exclusive perk 1"];
                    var exDesc2 = (string)info["exclusive description 2"];
                    var exPerk2 = (string)info["exclusive perk 2"];

                    // Generate new message entries for exclusive 1
                    if (!string.IsNullOrEmpty(exDesc1))
                    {
                        var exc1DescName = $"CH_Mes_Main_WpCustom_{id:0000}_Legendary_00";
                        var exc1PerkName = $"CH_Mes_Main_WpCustom_{id:0000}_Legendary_01";

                        var descMsg = msg.FindMessage(exc1DescName);
                        if (descMsg == null)
                        {
                            var newDesc = msg.Create(exc1DescName, exDesc1);
                            excDescGuid1 = newDesc.Guid;
                        }
                        else
                        {
                            excDescGuid1 = descMsg.Guid;
                            msg.SetStringAll(exc1DescName, exDesc1);
                        }

                        var perkMsg = msg.FindMessage(exc1PerkName);
                        if (perkMsg == null)
                        {
                            var newPerk = msg.Create(exc1PerkName, exPerk1);
                            excPerkGuid1 = newPerk.Guid;
                        }
                        else
                        {
                            excPerkGuid1 = perkMsg.Guid;
                            msg.SetStringAll(exc1PerkName, exPerk1);
                        }
                    }

                    // Generate new message entries for exclusive 2
                    if (!string.IsNullOrEmpty(exDesc2))
                    {
                        var exc2DescName = $"CH_Mes_Main_WpCustom_{id:0000}_Legendary_02";
                        var exc2PerkName = $"CH_Mes_Main_WpCustom_{id:0000}_Legendary_03";

                        var descMsg = msg.FindMessage(exc2DescName);
                        if (descMsg == null)
                        {
                            var newDesc = msg.Create(exc2DescName, exDesc2);
                            excDescGuid2 = newDesc.Guid;
                        }
                        else
                        {
                            excDescGuid2 = descMsg.Guid;
                            msg.SetStringAll(exc2DescName, exDesc2);
                        }

                        var perkMsg = msg.FindMessage(exc2PerkName);
                        if (perkMsg == null)
                        {
                            var newPerk = msg.Create(exc2PerkName, exPerk2);
                            excPerkGuid2 = newPerk.Guid;
                        }
                        else
                        {
                            excPerkGuid2 = perkMsg.Guid;
                            msg.SetStringAll(exc2PerkName, exPerk2);
                        }
                    }
                });
            }

            void UpdateShellInfo()
            {
                static string getWeaponShellGeneratorPath(int wpId) => $"natives/stm/_chainsaw/appsystem/shell/bullet/wp{wpId:0000}/wp{wpId:0000}shellgeneratoruserdata.user.2";
                static string getWeaponShellPath(int wpId, string kind = "")
                {
                    var postfix = string.IsNullOrEmpty(kind) ? "" : "_" + kind;
                    return $"natives/stm/_chainsaw/appsystem/shell/bullet/wp{wpId:0000}/wp{wpId:0000}shellinfo{postfix}.user.2";
                }

                string weaponShellPath = getWeaponShellPath(id, IsShotgunWeapon(id) ? "center" : "");
                context.ModifyUserFile(weaponShellPath, root =>
                {
                    if (info["basecrit"] is int baseCrit)
                    {
                        return root
                            .Set("_AttackInfo._CriticalRate", baseCrit)
                            .Set("_AttackInfo._CriticalRate_Fit", baseCrit);
                    }
                    return root;
                });

                if (IsShotgunWeapon(id))
                {
                    context.ModifyUserFile(getWeaponShellPath(id, "around"), root =>
                    {
                        if (info["basecritaround"] is int baseCritAround)
                        {
                            return root
                                .Set("_AttackInfo._CriticalRate", baseCritAround)
                                .Set("_AttackInfo._CriticalRate_Fit", baseCritAround);
                        }
                        return root;
                    });

                    context.ModifyUserFile(getWeaponShellGeneratorPath(id), root =>
                    {
                        if (info["aroundbulletcount"] is int aroundbulletcount)
                        {
                            root = root.Set("_AroundShellSetting._AroundBulletCount", aroundbulletcount);
                        }
                        if (info["horizontalaroundscatterpatternmin"] is int)
                        {
                            root = root
                                .Set("_AroundShellSetting._AroundScatterParam._HorizontalScatterDegreeRange", CreateRange((int)info["horizontalaroundscatterpatternmin"], (int)info["horizontalaroundscatterpatternmax"]))
                                .Set("_AroundShellSetting._AroundScatterParam._VerticalScatterDegreeRange", CreateRange((int)info["verticalaroundscatterpatternmin"], (int)info["verticalaroundscatterpatternmax"]));
                        }
                        return root;
                    });
                }
            }

            void UpdateBulletAttackHit()
            {
                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/shell/bullet/bulletattackhituserdata.user.2", root =>
                {
                    uint baseHash;
                    uint critHash;
                    uint baseAttachHash;
                    uint critAttachHash;
                    if (IsShotgunWeapon(id))
                    {
                        baseHash = (uint)MurMur3.HashData($"wp{id:0000}Center"); // Base Hash
                        critHash = (uint)MurMur3.HashData($"wp{id:0000}Center_c"); // Crit Hash
                        baseAttachHash = (uint)MurMur3.HashData($"wp{id:0000}Center_f"); // Base Hash that has an attachment
                        critAttachHash = (uint)MurMur3.HashData($"wp{id:0000}Center_f_c"); // Crit Hash that has an attachment
                    }
                    else
                    {
                        baseHash = (uint)MurMur3.HashData($"wp{id:0000}"); // Base Hash
                        critHash = (uint)MurMur3.HashData($"wp{id:0000}_c"); // Crit Hash
                        baseAttachHash = (uint)MurMur3.HashData($"wp{id:0000}_f"); // Base Hash that has an attachment
                        critAttachHash = (uint)MurMur3.HashData($"wp{id:0000}_f_c"); // Crit Hash that has an attachment
                    }

                    var baseAroundHash = (uint)MurMur3.HashData($"wp{id:0000}Around"); // Base Hash
                    var critAroundHash = (uint)MurMur3.HashData($"wp{id:0000}Around_c"); // Crit Hash
                    var baseAroundAttachHash = (uint)MurMur3.HashData($"wp{id:0000}Around_f"); // Base Hash that has an attachment
                    var critAroundAttachHash = (uint)MurMur3.HashData($"wp{id:0000}Around_f_c"); // Crit Hash that has an attachment

                    var attackDataList = (RszArrayNode)root["_AttackDataList"];
                    for (var i = 0; i < attackDataList.Length; i++)
                    {
                        var attackData = attackDataList[i];
                        var keyNameHash = attackData.Get<uint>("_KeyNameHash");
                        if (keyNameHash == baseHash || keyNameHash == baseAttachHash)
                        {
                            attackData = attackData
                                .Set("_Damage", (int)info["damage"])
                                .Set("STRUCT__Break__Value", (int)info["break"])
                                .Set("STRUCT__Stopping__Value", (int)info["stopping"])
                                .Set("STRUCT__Wince__Value", (int)info["wince"])
                                .Set("_AttackType", (int)info["attacktype"])
                                .Set("_AttackPower", (int)info["attackpower"])
                                .Set("_IsThroughRestriction", true)
                                .Set("_ThroughNum", (int)info["throughnum"])
                                .Set("_BreakLevel", (int)info["breaklevel"]);
                            attackDataList = attackDataList.SetItem(i, attackData);
                        }
                        else if (keyNameHash == critHash || keyNameHash == critAttachHash)
                        {
                            attackData = attackData
                                .Set("_Damage", (int)info["damagecrit"])
                                .Set("STRUCT__Break__Value", (int)info["breakcrit"])
                                .Set("STRUCT__Stopping__Value", (int)info["stoppingcrit"])
                                .Set("STRUCT__Wince__Value", (int)info["wincecrit"])
                                .Set("_AttackType", (int)info["attacktypecrit"])
                                .Set("_AttackPower", (int)info["attackpowercrit"])
                                .Set("_IsThroughRestriction", true)
                                .Set("_ThroughNum", (int)info["throughnumcrit"])
                                .Set("_BreakLevel", (int)info["breaklevelcrit"]);
                            attackDataList = attackDataList.SetItem(i, attackData);
                        }
                        if (IsShotgunWeapon(id))
                        {
                            if (keyNameHash == baseAroundHash || keyNameHash == baseAroundAttachHash)
                            {
                                attackData = attackData
                                    .Set("_Damage", (int)info["damagearound"])
                                    .Set("STRUCT__Break__Value", (int)info["breakaround"])
                                    .Set("STRUCT__Stopping__Value", (int)info["stoppingaround"])
                                    .Set("STRUCT__Wince__Value", (int)info["wincearound"])
                                    .Set("_AttackType", (int)info["attacktypearound"])
                                    .Set("_AttackPower", (int)info["attackpoweraround"])
                                    .Set("_IsThroughRestriction", true)
                                    .Set("_ThroughNum", (int)info["throughnumaround"])
                                    .Set("_BreakLevel", (int)info["breaklevelaround"]);
                                attackDataList = attackDataList.SetItem(i, attackData);
                            }
                            else if (keyNameHash == critAroundHash || keyNameHash == critAroundAttachHash)
                            {
                                attackData = attackData
                                    .Set("_Damage", (int)info["damagearoundcrit"])
                                    .Set("STRUCT__Break__Value", (int)info["breakaroundcrit"])
                                    .Set("STRUCT__Stopping__Value", (int)info["stoppingaroundcrit"])
                                    .Set("STRUCT__Wince__Value", (int)info["wincearoundcrit"])
                                    .Set("_AttackType", (int)info["attacktypearoundcrit"])
                                    .Set("_AttackPower", (int)info["attackpoweraroundcrit"])
                                    .Set("_IsThroughRestriction", true)
                                    .Set("_ThroughNum", (int)info["throughnumaroundcrit"])
                                    .Set("_BreakLevel", (int)info["breaklevelaroundcrit"]);
                                attackDataList = attackDataList.SetItem(i, attackData);
                            }
                        }
                    }
                    return root.SetField("_AttackDataList", attackDataList);
                });
            }

            void UpdateItemDefinition()
            {
                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2", root =>
                {
                    var datas = (RszArrayNode)root["_Datas"];
                    for (var i = 0; i < datas.Length; i++)
                    {
                        var data = datas[i];
                        if (data.Get<int>("_ItemId") == (int)info["itemid"])
                        {
                            if (info["baseammocapacity"] is int baseAmmoCapacity)
                            {
                                data = data.Set("_WeaponDefineData._AmmoMax", baseAmmoCapacity); // Setting base ammo capacity
                            }
                            if (info["ammopershot"] is int ammoPerShot)
                            {
                                data = data.Set("_WeaponDefineData._AmmoCost", ammoPerShot); // Setting ammo cost per shot
                            }
                            if (info["ammo type"] is int ammoType)
                            {
                                data = data.Set("_WeaponDefineData._UsableAmmoList", new[] { ammoType }); // Setting ammo type
                            }
                            root = root.SetField("_Datas", datas.SetItem(i, data));
                            break;
                        }
                    }
                    return root;
                });
            }

            void UpdateWeaponEquipParam()
            {
                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2", root =>
                {
                    var datas = (RszArrayNode)root["_DataTable"];
                    for (var i = 0; i < datas.Length; i++)
                    {
                        if (datas[i].Get<int>("_WeaponID") == id)
                        {
                            var data = datas[i];
                            {
                                if (info["baserateoffire"] is int baseRateOfFire)
                                {
                                    data = data.Set("_WeaponStructureParam._RapidSpeed", baseRateOfFire);
                                }
                                if (info["basereloadrounds"] is int baseReloadRounds)
                                {
                                    data = data.Set("_WeaponStructureParam.ReloadNum", baseReloadRounds);
                                }
                                if (info["basereloadspeed"] is int baseReloadSpeed)
                                {
                                    data = data.Set("_WeaponStructureParam._ReloadSpeedRate", baseReloadSpeed);
                                }
                                if (info["typeofshoot"] is int typeOfShoot)
                                {
                                    data = data.Set("_WeaponStructureParam.TypeOfShoot", new[] { typeOfShoot });
                                }
                                root = root.SetField("_DataTable", datas.SetItem(i, data));
                                break;
                            }
                        }
                    }
                    return root;
                });
            }

            void UpdateShop()
            {
                context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopitemsettinguserdata.user.2", root =>
                {
                    var datas = (RszArrayNode)root["_Datas"];
                    for (var i = 0; i < datas.Length; i++)
                    {
                        var data = datas[i];
                        if (data.Get<int>("_ItemId") == id)
                        {
                            var price = (int)info["price"];
                            data = data.Set("_PriceSettings", new[] {
                                new
                                {
                                    _Difficulty = 20,
                                    _Price = new
                                    {
                                        _PurchasePrice = price, // Setting purchase price
                                        _SellingPrice = price / 2, // Setting selling price to 50% of purchase price
                                    }
                                }
                            });
                            root = root.SetField("_Datas", datas.SetItem(i, data));
                            break;
                        }
                    }
                    return root;
                });
            }

            void UpdateCustomFiles()
            {
                var individualDataMap = new Dictionary<string, int>
                {
                    ["ReloadSpeed"] = 7,
                    ["Rapid"] = 8,
                    ["CriticalRate"] = 0,
                    ["ThroughNum"] = 1
                };

                var individualCsvMap = new Dictionary<string, int>
                {
                    ["reload speed"] = 7,
                    ["fire rate"] = 8,
                    ["critical rate"] = 0,
                    ["penetration"] = 1
                };

                var limitDataMap = new Dictionary<string, int>
                {
                    ["critical rate"] = 0,
                    ["damage"] = 1,
                    ["break"] = 1,
                    ["stopping"] = 1,
                    ["penetration"] = 3,
                    ["ammo capacity"] = 4,
                    ["fire rate"] = 5
                };

                var individualMessageIdMap = new Dictionary<int, Guid>
                {
                    [0] = new Guid("6f60b94f-1766-4c98-8335-a69958e2d927"), // CriticalRate
                    [1] = new Guid("db128948-0960-4147-814d-fec706a5c34a"), // Penetration
                    [7] = new Guid("a3e8cc54-b462-4be3-9e77-e6660ecf0e17"), // ReloadSpeed
                    [8] = new Guid("99bfca71-e54c-497d-82e2-8df885ef54fb"), // FireRate
                };

                var individualMessageIdPropertyMap = new Dictionary<int, string>
                {
                    [0] = "_CustomCriticalRate._MessageId", // CriticalRate
                    [1] = "_CustomThroughNum._MessageId",   // Penetration/ThroughNum
                    [7] = "_CustomReloadSpeed._MessageId",  // ReloadSpeed
                    [8] = "_CustomRapid._MessageId",        // FireRate
                };

                UpdateCustom();
                UpdateCustomDetail();

                void UpdateCustom()
                {
                    context.ModifyUserFile("natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2", root =>
                    {
                        var userdata = RszSerializer.Deserialize<chainsaw.WeaponCustomUserdata>(root)!;
                        var stage = userdata._WeaponStages.First(x => x._WeaponID == id);

                        //adding all info and cost values for damage
                        for (var i = 0; i < 5; i++)
                        {
                            stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[i]._Info = info[$"damage level {i + 1}"].ToString()!;
                            stage._WeaponCustom._Commons[0]._CustomAttackUp._AttackUpCustomStages[i]._Cost = (int)info[$"damage cost level {i + 1}"];
                        }
                        //adding all info and cost values for ammo
                        if (stage._WeaponCustom._Commons.Count > 1)
                        {
                            if (info["ammo capacity level 1"] is not int)
                            {
                                stage._WeaponCustom._Commons.RemoveAt(1);
                            }
                            else
                            {
                                for (var i = 0; i < 5; i++)
                                {
                                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._Info = info[$"ammo capacity level {i + 1}"].ToString()!;
                                    stage._WeaponCustom._Commons[1]._CustomAmmoMaxUp._AmmoMaxUpCustomStages[i]._Cost = (int)info[$"ammo capacity cost level {i + 1}"];
                                }
                            }
                        }
                        // change/add individual based on if it exists or not
                        var individualTypeCount = individualCsvMap.Keys
                            .Count(key => info[$"{key} level 1"] is not "");
                        var missingIndividuals = individualTypeCount - stage._WeaponCustom._Individuals.Count;
                        if (missingIndividuals > 0)
                        {
                            for (var i = 0; i < missingIndividuals; i++)
                            {
                                stage._WeaponCustom._Individuals.Add(new chainsaw.WeaponCustomUserdata.Individual());
                            }
                        }
                        else if (missingIndividuals < 0)
                        {
                            stage._WeaponCustom._Individuals.RemoveRange(individualTypeCount, stage._WeaponCustom._Individuals.Count - individualTypeCount);
                        }

                        //updating individual correct type
                        var individualsCount = stage._WeaponCustom._Individuals.Count;
                        var usedIndices = new HashSet<int>();
                        var matchTypes = individualCsvMap
                            .Where(kvp => info[$"{kvp.Key} level 1"] is not "")
                            .Select(kvp => kvp.Value)
                            .ToList();

                        // First, assign each matchType to an individual that doesn't already match it
                        for (int t = 0; t < matchTypes.Count; t++)
                        {
                            bool assigned = false;
                            for (int i = 0; i < individualsCount; i++)
                            {
                                var individual = stage._WeaponCustom._Individuals[i];
                                if (!usedIndices.Contains(i) && individual._IndividualCustomCategory != matchTypes[t])
                                {
                                    individual._IndividualCustomCategory = matchTypes[t];
                                    usedIndices.Add(i);
                                    assigned = true;
                                    break;
                                }
                            }
                            // If not assigned, assign to the first unused individual
                            if (!assigned)
                            {
                                for (int i = 0; i < individualsCount; i++)
                                {
                                    if (!usedIndices.Contains(i))
                                    {
                                        stage._WeaponCustom._Individuals[i]._IndividualCustomCategory = matchTypes[t];
                                        usedIndices.Add(i);
                                        break;
                                    }
                                }
                            }
                        }

                        // MessageId based on the category
                        for (var i = 0; i < individualsCount; i++)
                        {
                            var individual = stage._WeaponCustom._Individuals[i];
                            if (individualMessageIdMap.TryGetValue(individual._IndividualCustomCategory, out var messageId) &&
                            individualMessageIdPropertyMap.TryGetValue(individual._IndividualCustomCategory, out var individualName))
                            {
                                var individualRoot = individual;
                                var individualParts = individualName.Split('.');
                                var individualTarget = (object)individualRoot;
                                PropertyInfo? individualProperty = null;
                                for (int m = 0; m < individualParts.Length - 1; m++)
                                {
                                    individualProperty = individualTarget.GetType().GetProperty(individualParts[m])!;
                                    individualTarget = individualProperty.GetValue(individualTarget)!;
                                }
                                individualProperty = individualTarget.GetType().GetProperty(individualParts[^1])!;
                                individualProperty.SetValue(individualTarget, messageId);
                            }
                        }
                        //Creating the individual stages and setting all info and cost values for each individual type
                        for (var t = 1; t <= individualsCount; t++)
                        {
                            var individualValue = stage._WeaponCustom._Individuals[t - 1]._IndividualCustomCategory;
                            var individualKey = individualCsvMap.FirstOrDefault(x => x.Value == individualValue).Key;
                            var individualData = individualDataMap.FirstOrDefault(x => x.Value == individualValue).Key;
                            var individualRoot = stage._WeaponCustom._Individuals[t - 1];
                            var individualName = $"_Custom{individualData}";
                            var individualProperty = individualRoot.GetType().GetProperty(individualName)!;
                            var individualObject = individualProperty.GetValue(individualRoot)!;
                            var individualStageName = $"_{individualData}CustomStages";
                            var individiualStageProperty = individualObject.GetType().GetProperty(individualStageName)!;
                            var individualStagesList = (IList)individiualStageProperty.GetValue(individualObject)!;
                            var individualStagesListCount = individualStagesList.Count;
                            for (var s = individualStagesListCount; s < 5; s++)
                            {
                                var individualStageType = individualStagesList.GetType().GetGenericArguments()[0];
                                var individualNewStage = Activator.CreateInstance(individualStageType);
                                individualStagesList.Add(individualNewStage);
                            }
                            //setting all info and cost values for the individual type
                            for (var s = 0; s <= individualStagesList.Count; s++)
                            {
                                for (var i = 0; i < 5; i++)
                                {
                                    var individualInfo = $"{individualKey} level {i + 1}";
                                    var individualCost = $"{individualKey} cost level {i + 1}";
                                    var stageInstance = individualStagesList[i]!;
                                    var infoValue = stageInstance.GetType().GetProperty("_Info")!;
                                    infoValue.SetValue(stageInstance, info[individualInfo].ToString());
                                    var costValue = stageInstance.GetType().GetProperty("_Cost")!;
                                    if (info[individualCost] is int value)
                                    {
                                        costValue.SetValue(stageInstance, value);
                                    }
                                }
                            }

                            //Create param for the individual
                            string individualParamName = $"_{individualData}Params";
                            // For each custom stage, add a param value
                            for (int s = 0; s < individualStagesList.Count; s++)
                            {
                                var stageInstance = individualStagesList[s]!;
                                var paramProperty = stageInstance.GetType().GetProperty(individualParamName)!;
                                var paramList = (IList)paramProperty.GetValue(stageInstance)!;
                                var paramType = paramList.GetType().GetGenericArguments()[0];
                                if (paramList.Count == 0)
                                {
                                    if (individualData == "CriticalRate" || individualData == "ThroughNum")
                                    {
                                        while (paramList.Count < 2)
                                        {
                                            var newParam = Activator.CreateInstance(paramType);
                                            paramList.Add(newParam);
                                        }
                                    }
                                    else
                                    {
                                        var newParam = Activator.CreateInstance(paramType);
                                        paramList.Add(newParam);
                                    }
                                }
                            }

                            //setting all param values for the individual
                            for (var s = 0; s < individualStagesList.Count; s++)
                            {
                                var stageInstance = individualStagesList[s]!;
                                var paramProperty = stageInstance.GetType().GetProperty(individualParamName)!;
                                var paramList = (IList)paramProperty.GetValue(stageInstance)!;
                                for (int p = 0; p < paramList.Count; p++)
                                {
                                    int paramType;
                                    if ((individualData == "ReloadSpeed") && info["basereloadrounds"] is not int)
                                    {
                                        paramType = 1;
                                    }
                                    else
                                    {
                                        paramType = p;
                                    }
                                    var paramInstance = paramList[p]!;
                                    var paramValue = paramInstance.GetType().GetProperty($"_{individualData}")!;
                                    paramValue.SetValue(paramInstance, paramType);
                                    var levelValue = paramInstance.GetType().GetProperty("_Level")!;
                                    levelValue.SetValue(paramInstance, s);
                                }
                            }
                        }

                        //updating exclusive values
                        var exclusiveKeys = limitDataMap.Keys
                            .Where(k => info[$"{k} exclusive"] is not "")
                            .ToList();

                        // Ensure LimitBreak count matches exclusiveKeys count
                        var limitBreaks = stage._WeaponCustom._LimitBreak;
                        while (limitBreaks.Count < exclusiveKeys.Count)
                        {
                            var template = limitBreaks.Count > 0
                                ? CloneRszData(limitBreaks[0])
                                : new chainsaw.WeaponCustomUserdata.LimitBreak();
                            limitBreaks.Add(template);
                        }
                        while (limitBreaks.Count > exclusiveKeys.Count)
                        {
                            limitBreaks.RemoveAt(limitBreaks.Count - 1);
                        }

                        // Set each exclusive type and value
                        for (int i = 0; i < exclusiveKeys.Count; i++)
                        {
                            var key = exclusiveKeys[i];
                            var limitBreak = limitBreaks[i];
                            limitBreak._LimitBreakCustomCategory = limitDataMap[key];

                            // Set message IDs
                            if (i == 0 && excDescGuid1 != Guid.Empty)
                            {
                                limitBreak._CustomLimitBreak._MessageId = excDescGuid1;
                                limitBreak._CustomLimitBreak._PerksMessageId = excPerkGuid1;
                            }
                            else if (i == 1 && excDescGuid2 != Guid.Empty)
                            {
                                limitBreak._CustomLimitBreak._MessageId = excDescGuid2;
                                limitBreak._CustomLimitBreak._PerksMessageId = excPerkGuid2;
                            }

                            limitBreak._CustomLimitBreak._RateValue = Convert.ToSingle(info[$"{key} exclusive"]);

                            var costKey = $"{key} cost exclusive";
                            if (info[costKey] is int costExclusive)
                            {
                                limitBreak._CustomLimitBreak._LimitBreakCustomStages[0]._Cost = costExclusive;
                            }
                        }
                        return (RszObjectNode)RszSerializer.Serialize(root.Type, userdata);
                    });
                }

                void UpdateCustomDetail()
                {
                    context.ModifyUserFile("natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2", root =>
                    {
                        var userdata = RszSerializer.Deserialize<chainsaw.WeaponDetailCustomUserdata>(root)!;
                        var stage = userdata._WeaponDetailStages.First(x => x._WeaponID == id);
                        //adding/editing all damage related upgrade values
                        for (var i = 1; i < 5; i++)
                        {
                            stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._DamageRates[i]._BaseValue = (float)info[$"damage level {i + 1}"] / (float)info[$"damage level 1"];
                            stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._StoppingRates[i]._BaseValue = (float)info[$"damage level {i + 1}"] / (float)info[$"damage level 1"];
                            stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._WinceRates[i]._BaseValue = (float)info[$"damage level {i + 1}"] / (float)info[$"damage level 1"];
                            stage._WeaponDetailCustom._CommonCustoms[0]._AttackUp._BreakRates[i]._BaseValue = (float)info[$"damage level {i + 1}"] / (float)info[$"damage level 1"];
                        }
                        //adding/editing all ammo max related upgrade values
                        if (stage._WeaponDetailCustom._CommonCustoms.Count > 1)
                        {
                            if (info[$"ammo capacity level 1"] is not int)
                            {
                                stage._WeaponDetailCustom._CommonCustoms.RemoveAt(1);
                            }
                            else
                            {
                                for (var i = 1; i < 5; i++)
                                {
                                    stage._WeaponDetailCustom._CommonCustoms[1]._AmmoMaxUp._AmmoMaxs[i] = (int)info[$"ammo capacity level {i + 1}"];
                                }
                            }
                        }
                        //change/add individual based on if it exists or not
                        var individualCount = individualCsvMap.Keys
                            .Count(key => info[$"{key} level 1"] is not "");
                        var missingIndividuals = individualCount - stage._WeaponDetailCustom._IndividualCustoms.Count;
                        if (missingIndividuals > 0)
                        {
                            for (var i = 0; i < missingIndividuals; i++)
                            {
                                var templateIndividualCustom = userdata._WeaponDetailStages
                                    .First(x => x._WeaponID == id)
                                    ._WeaponDetailCustom._IndividualCustoms[0];
                                var newIndividualCustom = CloneRszData(templateIndividualCustom);
                                stage._WeaponDetailCustom._IndividualCustoms.Add(newIndividualCustom);
                                stage._WeaponDetailCustom._IndividualCustoms[individualCount - 1]._IndividualCustomCategory = 1;
                            }
                        }
                        else if (missingIndividuals < 0)
                        {
                            stage._WeaponDetailCustom._IndividualCustoms.RemoveRange(individualCount, stage._WeaponDetailCustom._IndividualCustoms.Count - individualCount);
                        }

                        //UPDATING INDIVIDUAL TO CORRECT TYPE
                        var individualsCount = stage._WeaponDetailCustom._IndividualCustoms.Count;
                        var individualIndices = new HashSet<int>();
                        var matchTypes = individualCsvMap
                            .Where(kvp => info[$"{kvp.Key} level 1"] is not "")
                            .Select(kvp => kvp.Value)
                            .ToList();

                        // assign each matchType to an individual that doesn't already match it
                        for (int t = 0; t < matchTypes.Count; t++)
                        {
                            bool individualAssigned = false;
                            for (int i = 0; i < individualsCount; i++)
                            {
                                var individual = stage._WeaponDetailCustom._IndividualCustoms[i];
                                if (!individualIndices.Contains(i) && individual._IndividualCustomCategory != matchTypes[t])
                                {
                                    individual._IndividualCustomCategory = matchTypes[t];
                                    individualIndices.Add(i);
                                    individualAssigned = true;
                                    break;
                                }
                            }
                            // assign to the first unused individual
                            if (!individualAssigned)
                            {
                                for (int i = 0; i < individualsCount; i++)
                                {
                                    if (!individualIndices.Contains(i))
                                    {
                                        stage._WeaponDetailCustom._IndividualCustoms[i]._IndividualCustomCategory = matchTypes[t];
                                        individualIndices.Add(i);
                                        break;
                                    }
                                }
                            }
                        }

                        //CREATING THE INDIVIDUAL STAGES AND SETTING ALL INFO AND COST VALUES FOR EACH INDIVIDUAL TYPE
                        for (var t = 1; t <= individualsCount; t++)
                        {
                            var individualValue = stage._WeaponDetailCustom._IndividualCustoms[t - 1]._IndividualCustomCategory;
                            var individualKey = individualCsvMap.FirstOrDefault(x => x.Value == individualValue).Key;
                            var individualData = individualDataMap.FirstOrDefault(x => x.Value == individualValue).Key;
                            var individualRoot = stage._WeaponDetailCustom._IndividualCustoms[t - 1];
                            string individualName = individualData == "ThroughNum" ? "_ThroughNums" : $"_{individualData}";
                            var individualProperty = individualRoot.GetType().GetProperty(individualName)!;
                            var individualObject = individualProperty.GetValue(individualRoot)!;
                            if (individualData == "CriticalRate" || individualData == "ThroughNum")
                            {
                                // create _CriticalRate/ThroughNums_Normal
                                string normalStageName = $"_{individualData}_Normal";
                                var normalStageProperty = individualObject.GetType().GetProperty(normalStageName)!;
                                var normalStagesList = (IList?)normalStageProperty.GetValue(individualObject)!;
                                var normalStagesListCount = normalStagesList.Count;
                                for (var s = normalStagesListCount; s < 5; s++)
                                {
                                    var stageType = normalStagesList.GetType().GetGenericArguments()[0];
                                    var newStage = Activator.CreateInstance(stageType);
                                    normalStagesList.Add(newStage);
                                }
                                // create _CriticalRate/ThroughNums_Fit
                                string fitStageName = $"_{individualData}_Fit";
                                var fitStageProperty = individualObject.GetType().GetProperty(fitStageName)!;
                                var fitStagesList = (IList)fitStageProperty.GetValue(individualObject)!;
                                var fitStagesListCount = fitStagesList.Count;
                                for (var s = fitStagesListCount; s < 5; s++)
                                {
                                    var stageType = fitStagesList.GetType().GetGenericArguments()[0];
                                    var newStage = Activator.CreateInstance(stageType);
                                    fitStagesList.Add(newStage);
                                }
                                for (var i = 0; i < 5; i++)
                                {
                                    var individualInfo = $"{individualKey} level {i + 1}";
                                    if (info[individualInfo] is not "")
                                    {
                                        if (individualData == "ThroughNum")
                                        {
                                            normalStagesList[i] = Convert.ToInt32(info[individualInfo]);
                                        }
                                        else
                                        {
                                            normalStagesList[i] = Convert.ToSingle(info[individualInfo]);
                                        }
                                    }
                                }
                                for (var i = 0; i < 5; i++)
                                {
                                    var individualInfo = $"{individualKey} level {i + 1}";
                                    if (info[individualInfo] is not "")
                                    {
                                        if (individualData == "ThroughNum")
                                        {
                                            fitStagesList[i] = Convert.ToInt32(info[individualInfo]);
                                        }
                                        else
                                        {
                                            fitStagesList[i] = Convert.ToSingle(info[individualInfo]);
                                        }
                                    }
                                }
                            }
                            else if (individualData == "ReloadSpeed")
                            {
                                var reloadNum = info["basereloadrounds"] is int ? "_ReloadNums" : "_ReloadSpeedRates";
                                string individualStageName = $"{reloadNum}";
                                var individiualStageProperty = individualObject.GetType().GetProperty(individualStageName)!;
                                var individualStagesList = (IList)individiualStageProperty.GetValue(individualObject)!;
                                var individualStagesListCount = individualStagesList.Count;
                                for (var s = individualStagesListCount; s < 5; s++)
                                {
                                    var individualStageType = individualStagesList.GetType().GetGenericArguments()[0];
                                    var individualNewStage = Activator.CreateInstance(individualStageType);
                                    individualStagesList.Add(individualNewStage);
                                }
                                for (var i = 0; i < 5; i++)
                                {
                                    if (reloadNum == "_ReloadSpeedRates")
                                    {
                                        if (i == 0)
                                        {
                                            individualStagesList[i] = 1.0f;
                                        }
                                        else
                                        {
                                            var individualInfo = $"{individualKey} level {i + 1}";
                                            individualStagesList[i] = Convert.ToSingle(info[individualInfo]) / Convert.ToSingle(info["reload speed level 1"]);
                                        }
                                    }
                                    else if (i == 0)
                                    {
                                        individualStagesList[i] = info["basereloadrounds"];
                                    }
                                    else
                                    {
                                        var individualInfo = $"{individualKey} level {i + 1}";
                                        individualStagesList[i] = Convert.ToInt32(info[individualInfo]);
                                    }
                                }
                            }
                            else if (individualData == "Rapid")
                            {
                                string rapidType;
                                if (id == 4100 || id == 4400)
                                {
                                    rapidType = $"_PumpActionRapidSpeed";
                                }
                                else
                                {
                                    rapidType = $"_RapidSpeed";
                                }
                                string individualStageName = rapidType;
                                var individiualStageProperty = individualObject.GetType().GetProperty(individualStageName)!;
                                var individualStagesList = (IList)individiualStageProperty.GetValue(individualObject)!;
                                var individualStagesListCount = individualStagesList.Count;
                                for (var s = individualStagesListCount; s < 5; s++)
                                {
                                    var individualStageType = individualStagesList.GetType().GetGenericArguments()[0];
                                    var individualNewStage = Activator.CreateInstance(individualStageType);
                                    individualStagesList.Add(individualNewStage);
                                }
                                for (var i = 0; i < 5; i++)
                                {
                                    if (i == 0)
                                    {
                                        individualStagesList[i] = 1.0f;
                                    }
                                    else
                                    {
                                        var individualInfo = $"{individualKey} level {i + 1}";
                                        individualStagesList[i] = Convert.ToSingle(info[individualInfo]) / Convert.ToSingle(info["fire rate level 1"]);
                                    }
                                }
                            }
                        }

                        //EDITING EXCLUSIVE VALUES
                        var exclusiveKeys = limitDataMap.Keys
                            .Where(k => info[$"{k} exclusive"] is not "")
                            .ToList();

                        // check LimitBreakCustoms count matches exclusiveKeys count
                        var limitBreakCustoms = stage._WeaponDetailCustom._LimitBreakCustoms;
                        while (limitBreakCustoms.Count < exclusiveKeys.Count)
                        {
                            // Clone the first as a template
                            var template = limitBreakCustoms.Count > 0
                                ? CloneRszData(limitBreakCustoms[0])
                                : new chainsaw.WeaponDetailCustomUserdata.LimitBreakCustom();
                            limitBreakCustoms.Add(template);
                        }
                        while (limitBreakCustoms.Count > exclusiveKeys.Count)
                        {
                            limitBreakCustoms.RemoveAt(limitBreakCustoms.Count - 1);
                        }

                        // Set each exclusive type and value
                        for (int i = 0; i < exclusiveKeys.Count; i++)
                        {
                            var key = exclusiveKeys[i];
                            var custom = limitBreakCustoms[i];
                            custom._LimitBreakCustomCategory = limitDataMap[key];

                            var value = info[$"{key} exclusive"];
                            switch (key)
                            {
                                case "damage":
                                    custom._LimitBreakAttackUp._DamageRateScale = Convert.ToSingle(value);
                                    break;
                                case "break":
                                    custom._LimitBreakAttackUp._BreakRateScale = Convert.ToSingle(value);
                                    break;
                                case "stopping":
                                    custom._LimitBreakAttackUp._StoppingRateScale = Convert.ToSingle(value);
                                    break;
                                case "critical rate":
                                    custom._LimitBreakCriticalRate._CriticalRateNormalScale = Convert.ToSingle(value);
                                    custom._LimitBreakCriticalRate._CriticalRateFitScale = Convert.ToSingle(value);
                                    break;
                                case "penetration":
                                    custom._LimitBreakThroughNum._ThroughNumNormal = Convert.ToInt32(value);
                                    custom._LimitBreakThroughNum._ThroughNumFit = Convert.ToInt32(value);
                                    break;
                                case "ammo capacity":
                                    custom._LimitBreakAmmoMaxUp._AmmoMaxScale = Convert.ToSingle(value);
                                    break;
                                case "fire rate":
                                    custom._LimitBreakRapid._RapidSpeedScale = Convert.ToSingle(value);
                                    break;
                            }
                        }
                        return (RszObjectNode)RszSerializer.Serialize(root.Type, userdata);
                    });
                }
            }
        }

        private static bool IsShotgunWeapon(int weaponId) => weaponId is 4100 or 4101 or 4102 or 6001;

        private static Range CreateRange(float min, float max)
        {
            Span<float> f = [min, max];
            return MemoryMarshal.Cast<float, Range>(f)[0];
        }

        private T CloneRszData<T>(T source)
        {
            var typeName = source!.GetType().FullName!.Replace('+', '.');
            var rszNode = RszSerializer.Serialize(context.TypeRepository.FromName(typeName)!, source);
            var result = RszSerializer.Deserialize<T>(rszNode);
            return result!;
        }
    }
}
