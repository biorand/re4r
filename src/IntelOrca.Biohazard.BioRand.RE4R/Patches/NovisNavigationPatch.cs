using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "novisnavigation",
        Name = "Fix Novis Navigation",
        Description = "Fixes novistador navigation.",
        Version = "1.0",
        Author = "IntelOrca")]
    internal class NovisNavigationPatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {
            Apply(
                "natives/stm/_chainsaw/appsystem/navigation/loc{0}/navigation_loc{1}.scn.20",
                 "AIMap",
                [
                    4000, 4010, 4011, 4300, 4310, 4400, 4410, 4500, 4510, 4600, 4610, 4700, 4710,
                    5000, 5010, 5100, 5110, 5200, 5300, 5400, 5410, 5500, 5510, 5600, 5610, 5700, 5900,
                    6000, 6010, 6100, 6110, 6200, 6300, 6400, 6500, 6600, 6610, 6700, 6701, 6800, 6801, 6900,
                ]);

            Apply(
                "natives/stm/_anotherorder/appsystem/navigation/loc{0}/navigation_loc{1}.scn.20",
                "AIMap_AO",
                [
                    4010, 4300, 4400, 4410, 4500, 4700,
                    5000, 5100, 5110, 5500, 5510, 5600, 5610, 5900,
                    6010, 6100, 6110, 6200, 6400, 6800, 6900,
                ]);
        }

        private void Apply(string pathFormat, string rootName, int[] locations)
        {
            var repo = context.TypeRepository;
            foreach (var loc in locations)
            {
                var path = string.Format(pathFormat, loc / 100, loc);
                context.ModifyScnFile(path, scene =>
                {
                    var obj = scene.FindGameObject(rootName)!;
                    var navigationMapClient = obj.FindComponent("chainsaw.NavigationMapClient")!;
                    var bindInfoList = (RszArrayNode)navigationMapClient["_BindInfoList"];
                    if (bindInfoList.Length < 3)
                    {
                        scene = scene.UpdateGameObject(
                            obj.AddOrUpdateComponent(
                                navigationMapClient.SetField("_BindInfoList",
                                    bindInfoList.Add(repo
                                        .Create("chainsaw.NavigationMapClient.BindInfo")
                                            .Set("_Purpose", 1)
                                            .Set("_MapName", $"VolumeSpace_Loc{loc}")))));
                    }
                    return scene;
                });
            }
        }
    }
}
