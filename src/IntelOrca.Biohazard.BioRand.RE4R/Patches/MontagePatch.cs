using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class MontagePatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            var operations = context.DynamicData
                .GetCsv<MontageCopy>(DynamicDataName.Costumes)
                .Where(x => !string.IsNullOrEmpty(x.SourceCharacter))
                .ToImmutableArray();

            foreach (var op in operations)
            {
                Copy(op);
            }
        }

        private void Copy(MontageCopy montageCopy)
        {
            var sourcePath = GetCostumeUserDataPath(montageCopy.SourceCharacter);
            var targetPath = GetCostumeUserDataPath(montageCopy.TargetCharacter);

            var source = context.GetUserFile(sourcePath).GetObjects(context.TypeRepository)[0];
            var sourceArray = source.Get<RszArrayNode>("_DataTable");
            var sourceMontage = sourceArray.FirstOrDefault(x => x.Get<uint>("_ID") == montageCopy.SourceMontage)
                ?? throw new RandomizerUserException($"{montageCopy.SourceCharacter}:{montageCopy.SourceMontage} not found");
            var targetMontage = sourceMontage.Set("_ID", montageCopy.TargetMontage);

            context.ModifyUserFile(targetPath, root =>
            {
                return root.Set("_DataTable", root.Get<RszArrayNode>("_DataTable")
                    .Add(targetMontage));
            });
        }

        private static string GetCostumeUserDataPath(string character)
        {
            return $"natives/stm/_chainsaw/appsystem/character/{character}/costume/{character}costumepresetuserdata.user.2";
        }

        private class MontageCopy
        {
            public string SourceCharacter { get; set; } = "";
            public uint SourceMontage { get; set; }
            public string TargetCharacter { get; set; } = "";
            public uint TargetMontage { get; set; }
        }
    }
}
