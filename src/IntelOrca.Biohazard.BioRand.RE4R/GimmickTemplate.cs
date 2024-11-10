using System.Linq;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class GimmickTemplate
    {
        private static ScnFile? _template = null;

        public static ScnFile.GameObjectData Get(string name)
        {
            var template = _template;
            if (template == null)
            {
                template = ChainsawRandomizerFactory.Default.ReadScnFile(Resources.gimmick_template);
                _template = template;
            }

            return template
                .IterAllGameObjects()
                .First(x => x.Name == name);
        }
    }
}
