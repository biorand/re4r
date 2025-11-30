using System;
using System.Linq;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;
using Range = IntelOrca.Biohazard.REE.Rsz.Native.Range;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class LegendaryWeaponsPatch(IPatchContext context) : IPatch
    {
        private readonly WeaponBaseStats _baseStats = new(context.DynamicData);

        public void Apply()
        {
            var weapons = _baseStats.Weapons
                .Where(x => x["weight"] is int w && w > 0)
                .ToArray();
            var weaponNames = weapons.Select(x => (string)x["name"]).ToArray();
            foreach (var wpName in weaponNames)
            {
                ApplyWeapon(wpName);
            }
        }

        private void ApplyWeapon(string name)
        {
            var info = _baseStats.Weapons.First(x => (string)x["name"] == name);
            var id = (int)info["id"];

            UpdateMessages();
            UpdateShellInfo();
            UpdateBulletAttackHit();
            UpdateItemDefinition();
            UpdateWeaponEquipParam();
            UpdateShop();

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
                    if (!string.IsNullOrEmpty(exDesc1))
                    {
                        msg.SetStringAll($"CH_Mes_Main_WpCustom_{id:0000}_02_00", exDesc1);
                        msg.SetStringAll($"CH_Mes_Main_WpCustom_{id:0000}_02_01", exPerk1);
                    }
                    if (!string.IsNullOrEmpty(exDesc2))
                    {
                        msg.SetStringAll($"CH_Mes_Main_WpCustom_{id:0000}_02_02", exDesc2);
                        msg.SetStringAll($"CH_Mes_Main_WpCustom_{id:0000}_02_03", exPerk2);
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

                    var baseAroundHash = (uint)MurMur3.HashData($"wp{id:0000}"); // Base Hash
                    var critAroundHash = (uint)MurMur3.HashData($"wp{id:0000}_c"); // Crit Hash
                    var baseAroundAttachHash = (uint)MurMur3.HashData($"wp{id:0000}_f"); // Base Hash that has an attachment
                    var critAroundAttachHash = (uint)MurMur3.HashData($"wp{id:0000}_f_c"); // Crit Hash that has an attachment

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
        }

        private static bool IsShotgunWeapon(int weaponId) => weaponId is 4100 or 4101 or 4102 or 6001;

        private static Range CreateRange(float min, float max)
        {
            Span<float> f = [min, max];
            return MemoryMarshal.Cast<float, Range>(f)[0];
        }
    }
}
