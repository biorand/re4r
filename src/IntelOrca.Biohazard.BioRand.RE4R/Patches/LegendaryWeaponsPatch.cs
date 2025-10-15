using System;
using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class LegendaryWeaponsPatch(IPatchContext context) : IPatch
    {
        private readonly IPatchContext _context = context;
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
            _context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_name.msg.22", msg =>
            {
                msg.SetStringAll($"CH_Mes_Main_WEAPON_NAME_WP{id:0000}_00_0_000", (string)info["name"]);
            });
            _context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_item_caption.msg.22", msg =>
            {
                msg.SetStringAll($"CH_Mes_Main_WEAPON_CAPTION_WP{id:0000}_00_0_000", (string)info["description"]);
            });
            _context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_itemperks.msg.22", msg =>
            {
                msg.SetStringAll($"CH_Mes_Main_ItemPerks_WP{id:0000}_00_0_000", (string)info["perk"]);
            });
            _context.ModifyMsgFile("natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22", msg =>
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

        private void SetStrings(string path, Dictionary<Guid, string> strings)
        {
            var msgFile = _context.GetMsgFile(path).ToBuilder();
            foreach (var kvp in strings)
            {
                msgFile.SetStringAll(kvp.Key, kvp.Value);
            }
            _context.SetMsgFile(path, msgFile.Build());
        }
    }
}
