namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    /// <summary>
    /// Changes the title screen logo from RESIDENT EVIL/BIOHAZARD 4 to BIORAND.
    /// </summary>
    /// <param name="context"></param>
    internal class BiorandTitlePatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            context.ApplyOverlay(context.GetSupplementFile("biorand_title.zip")!);
        }
    }
}
