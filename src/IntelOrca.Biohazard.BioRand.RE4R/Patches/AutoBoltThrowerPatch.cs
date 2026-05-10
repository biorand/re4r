using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "autoboltthrower",
        Name = "Automatic Bolt Thrower",
        Description = "Makes the bolt thrower fully automatic.",
        Version = "1.0",
        Author = "MightKusKus")]
    internal class AutoBoltThrowerPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            if (!context.ExportingMod && !context.GetConfigOption("automatic-bolt-thrower", false))
                return;

            Apply("natives/stm/_chainsaw/appsystem/weapon/weaponequipparamcataloguserdata.user.2", 18);
            Apply("natives/stm/_anotherorder/appsystem/weapon/weaponequipparamcataloguserdata_ao.user.2", 19);
        }

        private void Apply(string path, int index)
        {
            context.ModifyUserFile(path, root =>
            {
                root = root.Set($"_DataTable[{index}]._WeaponStructureParam.TypeOfReload", 0);
                root = root.Set($"_DataTable[{index}]._WeaponStructureParam.TypeOfShoot", 1);
                return root;
            });
        }
    }
}
