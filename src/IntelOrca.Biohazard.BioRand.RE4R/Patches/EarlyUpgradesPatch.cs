using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    [ExportMod(
        FileName = "earlyupgrades",
        Name = "Early Upgrades",
        Description = "Allows weapons to be fully upgraded on all difficulties from the very start.",
        Version = "1.0",
        Author = "IntelOrca")]
    internal class EarlyUpgradesPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            // Main
            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/ingameshopupdateflagcataloguserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i <= 2; i++)
                {
                    var data = (RszObjectNode)datas[i];
                    data = data.SetField("_Flags", ((RszArrayNode)data["_Flags"]).Add(0));
                    data = data.SetField("_SaleFlags", ((RszArrayNode)data["_SaleFlags"]).Add(0));
                    datas = datas.SetItem(i, data);
                }
                return root.SetField("_Datas", datas);
            });

            // Separate Ways
            context.ModifyUserFile("natives/stm/_anotherorder/appsystem/ui/userdata/ingameshopupdateflagcataloguserdata_ao.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                var data = (RszObjectNode)datas[18];
                data = data.SetField("_Flags", ((RszArrayNode)data["_Flags"]).Add(17));
                data = data.SetField("_SaleFlags", ((RszArrayNode)data["_SaleFlags"]).Add(17));
                datas = datas.SetItem(18, data);
                return root.SetField("_Datas", datas);
            });
        }
    }
}
