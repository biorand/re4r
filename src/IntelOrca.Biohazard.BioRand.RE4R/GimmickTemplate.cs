using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class GimmickTemplate
    {
        private static RszScene? _template = null;

        public static RszGameObject Get(string name)
        {
            var template = _template;
            if (template == null)
            {
                var scnFile = new ScnFile(20, EmbeddedData.GetFile("gimmick.template.scn.20"));
                template = scnFile.ReadScene(FileRepository.RszRepository);
                _template = template;
            }
            return template.FindGameObject(name) ?? throw new Exception($"{name} template not found");
        }
    }
}
