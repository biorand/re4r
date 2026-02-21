using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class DynamicData(bool download)
    {
        private const string GoogleSheetUrl = "https://docs.google.com/spreadsheets/d/1YAOHcvyQ6Tp2n6io9iEcJjpjZoQXuUC0NGwGFKKafQ4/export?format=csv&gid={0}";
        private static readonly ImmutableDictionary<DynamicDataName, (string, int)> g_map = new Dictionary<DynamicDataName, (string, int)>
        {
            [DynamicDataName.Recipe] = ("recipe.csv", 327970340),
            [DynamicDataName.WeaponBase] = ("wpbase.csv", 882011316),
            [DynamicDataName.WeaponRng] = ("wpstats.csv", 345409638),
            [DynamicDataName.Gimmicks] = ("gimmicks.csv", 857253225),
            [DynamicDataName.CasePerks] = ("case_perks.csv", 433155121),
            [DynamicDataName.Enemies] = ("enemies.csv", 2122364676),
            [DynamicDataName.Items] = ("items.csv", 827998375),
            [DynamicDataName.Events] = ("events.csv", 677674082),
            [DynamicDataName.Messages] = ("messages.csv", 257348148),
            [DynamicDataName.StageLimits] = ("stagelimits.csv", 962898699),
            [DynamicDataName.Costumes] = ("costumes.csv", 429548846),
        }.ToImmutableDictionary();

        private readonly Dictionary<DynamicDataName, byte[]> _map = [];
        private object _sync = new();

        public string? GetFileName(DynamicDataName name)
        {
            if (g_map.TryGetValue(name, out var entry))
            {
                var (fileName, _) = entry;
                return fileName;
            }
            return null;
        }

        public byte[]? GetData(DynamicDataName name)
        {
            lock (_sync)
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
        }

        public T[] GetCsv<T>(DynamicDataName name) => Csv.Deserialize<T>(GetData(name)!);

        private static byte[] Download(string url)
        {
            using var httpClient = new HttpClient();
            return httpClient.GetByteArrayAsync(url).Result;
        }
    }

    public enum DynamicDataName
    {
        Recipe,
        WeaponBase,
        WeaponRng,
        Gimmicks,
        CasePerks,
        Enemies,
        Items,
        Events,
        Messages,
        StageLimits,
        Costumes,
    }
}
