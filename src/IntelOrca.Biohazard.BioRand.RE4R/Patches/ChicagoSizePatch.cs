using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class ChicagoSizePatch(IReeRandomizerContext context) : IPatch
    {
        public void Apply()
        {

            context.ModifyUserFile("natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2", root =>
            {
                var datas = (RszArrayNode)root["_Datas"];
                for (var i = 0; i < datas.Length; i++)
                {
                    var data = datas[i];
                    if (data.Get<int>("_ItemId") == 275157056)
                    {
                        data = data
                            .Set("_WeaponDefineData._ItemSize", 11);
                        root = root.SetField("_Datas", datas.SetItem(i, data));
                        break;
                    }
                }
                return root;
            });
        }
    }
}
