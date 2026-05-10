using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "disablecaseopen",
        Name = "Disable Case Open",
        Description = "Disables the case opening event at the start of chapter 2.",
        Version = "1.0",
        Author = "404runnotfound")]
    internal class DisableCaseOpenPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            context.ModifyScnFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_2/level_cp10_chp1_2.scn.20", root =>
            {
                return root.RemoveGameObject(new Guid("6f0cd2bf-4059-465d-85ea-d78926cc8ce6"));
            });
        }
    }
}
