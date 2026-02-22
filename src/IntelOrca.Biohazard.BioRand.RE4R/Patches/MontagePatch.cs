using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class MontagePatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            var operations = context.DynamicData
                .GetCsv<MontageCopy>(DynamicDataName.Costumes)
                .Where(x => !string.IsNullOrEmpty(x.Key))
                .ToImmutableArray();

            var costumeService = context.GetService<CostumeService>();
            foreach (var op in operations)
            {
                Copy(op);
                costumeService.Add(op.Key, op.FieldName, op.TargetMontage);
            }
        }

        private void Copy(MontageCopy m)
        {
            var targetPath = GetCostumeUserDataPath(m.TargetCharacter);
            context.ModifyUserFile(targetPath, root =>
            {
                var targetDataTable = root.Get<RszArrayNode>("_DataTable");
                if (targetDataTable.Any(x => x.Get<uint>("_ID") == m.TargetMontage))
                    return root;

                var sourceMontage = GetMontage(m.SourcePath, m.SourceCharacter, m.SourceMontage);
                var targetMontage = sourceMontage.Set("_ID", m.TargetMontage);
                return root.Set("_DataTable", root.Get<RszArrayNode>("_DataTable")
                    .Add(targetMontage));
            });
        }

        private IRszNode GetMontage(string sourcePath, string character, uint id)
        {
            sourcePath = string.IsNullOrEmpty(sourcePath) ? GetCostumeUserDataPath(character) : sourcePath;
            var source = context.GetUserFile(sourcePath).GetObjects(context.TypeRepository)[0];
            var sourceArray = source.Get<RszArrayNode>("_DataTable");
            var sourceMontage = sourceArray.FirstOrDefault(x => x.Get<uint>("_ID") == id)
                ?? throw new RandomizerUserException($"{character}:{id} not found");
            return sourceMontage;
        }

        private static string GetCostumeUserDataPath(string character)
        {
            return $"natives/stm/_chainsaw/appsystem/character/{character}/costume/{character}costumepresetuserdata.user.2";
        }

        private class MontageCopy
        {
            public string Key { get; set; } = "";
            public string FieldName { get; set; } = "";

            public string SourcePath { get; set; } = "";
            public string SourceCharacter { get; set; } = "";
            public uint SourceMontage { get; set; }
            public string TargetCharacter { get; set; } = "";
            public uint TargetMontage { get; set; }
        }
    }
}
