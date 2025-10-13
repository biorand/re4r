using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class DynamicData(bool download)
    {
        private const string GoogleSheetUrl = "https://docs.google.com/spreadsheets/d/1YAOHcvyQ6Tp2n6io9iEcJjpjZoQXuUC0NGwGFKKafQ4/export?format=csv&gid={0}";
        private static readonly ImmutableDictionary<DynamicDataName, (string, int)> g_map = new Dictionary<DynamicDataName, (string, int)>
        {
            [DynamicDataName.Recipe] = ("recipe.csv", 327970340),
            [DynamicDataName.WeaponBase] = ("wpbase.csv", 882011316),
            [DynamicDataName.WeaponRng] = ("wpstats.csv", 345409638),
        }.ToImmutableDictionary();

        private readonly Dictionary<DynamicDataName, byte[]> _map = [];

        public byte[] GetData(DynamicDataName name)
        {
            if (!_map.TryGetValue(name, out var data))
            {
                var (fileName, gid) = g_map[name];
                if (download)
                {
                    var downloadUrl = string.Format(GoogleSheetUrl, gid);
                    data = Download(downloadUrl);
                }
                else
                {
                    data = EmbeddedData.GetFile(fileName);
                }
                _map[name] = data;
            }
            return data;
        }

        private static byte[] Download(string url)
        {
            using var httpClient = new HttpClient();
            return httpClient.GetByteArrayAsync(url).Result;
        }
    }

    internal enum DynamicDataName
    {
        Recipe,
        WeaponBase,
        WeaponRng,
    }
}
