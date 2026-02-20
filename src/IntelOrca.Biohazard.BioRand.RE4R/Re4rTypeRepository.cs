using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class Re4rTypeRepository
    {
        private static RszTypeRepository? _v4;
        private static RszTypeRepository? _v5;

        public static RszTypeRepository FromVersion(int gameVersion)
        {
            if (gameVersion == 4)
            {
                _v4 ??= Load(gameVersion);
                return _v4;
            }
            else if (gameVersion == 5)
            {
                _v5 ??= Load(gameVersion);
                return _v5;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static RszTypeRepository Load(int version)
        {
            var rszJson = EmbeddedData.GetFile($"rszre4_v{version}.json.gz");
            return RszRepositorySerializer.Default.FromJsonGz(rszJson);
        }
    }
}
