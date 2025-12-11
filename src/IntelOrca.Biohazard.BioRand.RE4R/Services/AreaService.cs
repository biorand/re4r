using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class AreaService(ChainsawRandomizer randomizer)
    {
        private readonly Dictionary<Guid, Area> _guidToArea = [];

        public ImmutableArray<Area> Areas { get; private set; } = [];

        public void LoadAreas(Campaign campaign)
        {
            var areaRepo = campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;

            Areas = areaRepo.All
                .AsParallel()
                .Select(d => new Area(randomizer, d, EnemyClassFactory.Default))
                .ToImmutableArray();

            // Map initial guids
            foreach (var area in Areas)
            {
                area.Scene.VisitGameObjects(gameObject =>
                {
                    _guidToArea[gameObject.Guid] = area;
                });
            }
        }

        public Area? FindAreaContainingGameObject(Guid guid)
        {
            _guidToArea.TryGetValue(guid, out var area);
            return area;
        }

        public void Save(RandomizerLogger process)
        {
            Parallel.ForEach(Areas, area => area.Save());
        }
    }
}
