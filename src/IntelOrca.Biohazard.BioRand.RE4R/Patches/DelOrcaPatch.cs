using System;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.BioRand.REE.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "delorca",
        Name = "Del Orca",
        Description = "Changes Del Lago to Del Orca.",
        Version = "1.0",
        Author = "MightKusKus")]
    internal class DelOrcaPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            context.ApplyOverlay(context.GetSupplementFile("delorca.zip")
                ?? throw new Exception("delorca.zip not found"));
        }
    }
}
