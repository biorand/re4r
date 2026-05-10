using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class AreaService
    {
        private readonly ChainsawRandomizer _randomizer;
        private readonly Dictionary<Guid, Area> _guidToArea = [];
        private bool _loaded;

        public ImmutableArray<Area> Areas { get; private set; } = [];

        public AreaService(ChainsawRandomizer randomizer)
        {
            _randomizer = randomizer;
            LoadAreas(randomizer.Campaign);
        }

        public void LoadAreas(Campaign campaign)
        {
            if (_loaded)
                return;

            var areaRepo = AreaDefinitionRepository.GetRepository(campaign);
            Areas = areaRepo.All
                .Select(d => new Area(_randomizer, d))
                .OrderBy(x => x.Path, StringComparer.Ordinal)
                .ToImmutableArray();

            // Map initial guids
            foreach (var area in Areas)
            {
                area.Scene.VisitGameObjects(gameObject =>
                {
                    _guidToArea[gameObject.Guid] = area;
                });
            }
            _loaded = true;
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
                    .Where(x => x.Definition.Kind == kind)
                    .Where(x => x.Definition.Chapter == chapter)
                    .First();
            }
            else
            {
                return Areas
                    .Where(x => x.Definition.Kind == kind)
                    .Where(x => x.Definition.Location == (stage / 1000))
                    .OrderBy(x => Math.Abs((x.Definition.Stage ?? 0) - stage))
                    .ThenBy(x => x.Path, StringComparer.Ordinal)
                    .First();
            }
        }

        public void Save()
        {
            if (!_loaded)
                return;
            Parallel.ForEach(Areas, area => area.Save());
        }
    }
}
