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
            var areaRepo = AreaDefinitionRepository.GetRepository(campaign);
            Areas = areaRepo.All
                .AsParallel()
                .Select(d => new Area(randomizer, d, EnemyClassFactory.Default))
                .OrderBy(x => x.Path)
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

        public void RemoveGuid(Guid guid)
        {
            _guidToArea.Remove(guid);
        }

        public void AddGuidToArea(Guid guid, Area area)
        {
            _guidToArea[guid] = area;
        }

        public Area FindBestArea(AreaKind kind, int stage, int? chapter = null)
        {
            if (chapter != null)
            {
                return Areas
                    .Where(x => x.Definition.Chapter == chapter)
                    .First();
            }
            else
            {
                return Areas
                    .Where(x => x.Definition.Kind == kind)
                    .Where(x => x.Definition.Location == (stage / 1000))
                    .OrderBy(x => Math.Abs((x.Definition.Stage ?? 0) - stage))
                    .First();
            }
        }

        public void Save(RandomizerLogger process)
        {
            Parallel.ForEach(Areas, area => area.Save());
        }
    }
}
