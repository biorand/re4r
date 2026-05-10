using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "laserhandguns",
        Name = "Laser On All Handguns",
        Description = "Allows laser sight to be attached to all handguns.",
        Version = "1.0",
        Author = "IntelOrca")]
    internal class LaserSightPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            // Main
            Apply(
                "natives/stm/_chainsaw/appsystem/ui/userdata/weaponpartscombinedefinitionuserdata.user.2",
                "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2",
                [4002, 4003, 4004], // Red9, Blacktail, Matilda
                [274838656, 274840256, 274841856]);

            // Separate Ways
            Apply(
                "natives/stm/_anotherorder/appsystem/ui/userdata/weaponpartscombinedefinitionuserdata_ao.user.2",
                "natives/stm/_anotherorder/appsystem/weaponcustom/weapondetailcustomuserdata_ao.user.2",
                [6103, 6113], // SW - Blacktail AC, SW - Red 9
                [278200256, 278216256]);
        }

        private void Apply(
            string weaponPartsCombineDefinitionPath,
            string weaponDetailCustomPath,
            int[] weaponIds,
            int[] itemIds)
        {
            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/weapon/lasersight/playerlasersightcontrolleruserdata.user.2", root =>
            {
                var settings = (RszArrayNode)root["_Settings"];
                var template = (RszObjectNode)settings[0];
                foreach (var wp in weaponIds)
                {
                    root = root.SetField("_Settings",
                        settings.Add(
                            template.SetField("_WeaponID", wp)));
                }
                return root;
            });

            context.ModifyUserFile(weaponPartsCombineDefinitionPath, root =>
            {
                var userData = RszSerializer.Deserialize<WeaponPartsCombineDefinitionUserdata>(root)!;
                userData._Datas[6]._TargetItemIds.AddRange(itemIds);
                return (RszObjectNode)RszSerializer.Serialize(root.Type, userData);
            });

            context.ModifyUserFile(weaponDetailCustomPath, root =>
            {
                var userData = RszSerializer.Deserialize<WeaponDetailCustomUserdata>(root)!;
                var attachment = userData._WeaponDetailStages[0]._WeaponDetailCustom._AttachmentCustoms[0];
                foreach (var wp in weaponIds)
                {
                    foreach (var w in userData._WeaponDetailStages)
                    {
                        if (w._WeaponID == wp)
                        {
                            var attachments = w._WeaponDetailCustom._AttachmentCustoms;
                            if (!attachments.Any(x => x._ItemID == 116008000))
                            {
                                attachments.Add(attachment);
                            }
                        }
                    }
                }
                return (RszObjectNode)RszSerializer.Serialize(root.Type, userData);
            });
        }
    }
}
