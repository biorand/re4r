using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class GimmickService
    {
        private readonly Dictionary<Guid, GimmickPlacement> _guidToGimmickPlacement;

        public List<GimmickPlacement> GimmickPlacements { get; private set; }

        public GimmickService(ChainsawRandomizer randomizer)
        {
            var gimmicksCsv = randomizer.DynamicData.GetData(DynamicDataName.Gimmicks) ?? throw new Exception("Unable to get item data");
            GimmickPlacements = Csv.Deserialize<GimmickPlacement>(gimmicksCsv)
                .Where(x => x.Kind.Trim() is string s && !string.IsNullOrEmpty(s) && !s.StartsWith('#'))
                .ToList();

            _guidToGimmickPlacement = GimmickPlacements.ToDictionary(x => x.GuidOrAuto);
        }

        public void AddPlacement(GimmickPlacement placement)
        {
            GimmickPlacements.Add(placement);
            _guidToGimmickPlacement[placement.Guid] = placement;
        }

        public GimmickPlacement? FromGuid(Guid guid)
        {
            return _guidToGimmickPlacement.GetValueOrDefault(guid);
        }
    }
}
