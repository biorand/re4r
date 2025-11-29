using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        }

        private static bool IsShotgunWeapon(int weaponId) => weaponId is 4100 or 4101 or 4102 or 6001;

        private void SetStrings(string path, Dictionary<Guid, string> strings)
        {
            var msgFile = context.GetMsgFile(path).ToBuilder();
            foreach (var kvp in strings)
            {
                msgFile.SetStringAll(kvp.Key, kvp.Value);
            }
            context.SetMsgFile(path, msgFile.Build());
        }

        private static Range CreateRange(float min, float max)
        {
            Span<float> f = [min, max];
            return MemoryMarshal.Cast<float, Range>(f)[0];
        }
    }
}
