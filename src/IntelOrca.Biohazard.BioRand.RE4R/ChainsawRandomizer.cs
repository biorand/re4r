using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer
    {
        private FileRepository _fileRepository = new FileRepository();
        private RandomizerInput _input = new RandomizerInput();
        private bool _supplementApplied;
        private ValuableDistributor? _valuableDistributor;
        private ItemRandomizer? _itemRandomizer;
        private ImmutableArray<Modifier> _modifiers = GetModifiers();
        private ImmutableArray<Area> _areas;
        private Rng _rng = new Rng();

        public EnemyClassFactory EnemyClassFactory { get; }
        public FileRepository FileRepository => _fileRepository;

        public ValuableDistributor ValuableDistributor => _valuableDistributor!;
        public ItemRandomizer ItemRandomizer => _itemRandomizer!;
        public ImmutableArray<Area> Areas => _areas;
        public Campaign Campaign { get; private set; }

        public ChainsawRandomizer(EnemyClassFactory enemyClassFactory)
        {
            EnemyClassFactory = enemyClassFactory;
        }

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            _input = input;
            if (input.GamePath != null)
            {
                _fileRepository = new FileRepository(input.GamePath);
            }

            var logFiles = new Dictionary<string, string>();

#if DEBUG
            var campaigns = new[] { Campaign.Leon, Campaign.Ada };
#else
            var campaigns = new[] { Campaign.Leon };
#endif
            foreach (var campaign in campaigns)
            {
                var log = Randomize(input, campaign);

                var name = campaign.ToString().ToLowerInvariant();
                logFiles[$"input_{name}.log"] = log.Input.Output;
                logFiles[$"process_{name}.log"] = log.Process.Output;
                logFiles[$"output_{name}.log"] = log.Output.Output;
            }

            var output = new ChainsawRandomizerOutput(input, _fileRepository.GetOutputPakFile(), logFiles);
            return new RandomizerOutput(
                output.GetOutputZip(),
                output.GetOutputMod(),
                logFiles);
        }

        public RandomizerLoggerIO Randomize(RandomizerInput input, Campaign campaign)
        {
            Campaign = campaign;
            _modifiers = GetModifiers();

            var logger = new RandomizerLoggerIO();
            foreach (var l in new[] { logger.Input, logger.Process, logger.Output })
            {
                l.LogHr();
                l.LogVersion();
                l.LogLine($"Seed = {input.Seed}");
                l.LogLine($"Campaign = {campaign}");
                l.LogHr();
            }

            ApplySupplement();

            var areas = GetAreas(campaign);

            _itemRandomizer = new ItemRandomizer(this, logger.Process);

            var rng = new Rng(input.Seed);
            _rng = rng;

            _valuableDistributor = new ValuableDistributor(this);
            _valuableDistributor.Setup(_itemRandomizer, _rng, logger.Process);

            var inventoryRng = CreateRng();
            var merchantRng = CreateRng();
            var enemyRng = CreateRng();
            var itemRng = CreateRng();

            var itemData = ChainsawItemData.FromData(_fileRepository);

            // Input
            IterateModifiers((n, m) =>
            {
                logger.Input.Push(n);
                m.LogState(this, logger.Input);
                logger.Input.Pop();
                logger.Input.LogHr();
            });

            // Apply modifiers
            IterateModifiers((n, m) =>
            {
                logger.Process.Push(n);
                m.Apply(this, logger.Process);
                logger.Process.Pop();
                logger.Process.LogHr();
            });

            // Save area files
            Parallel.ForEach(areas, area =>
            {
                _fileRepository.SetGameFileData(area.Definition.Path, area.SaveData());
            });

            // Output
            IterateModifiers((n, m) =>
            {
                logger.Output.Push(n);
                m.LogState(this, logger.Output);
                logger.Output.Pop();
                logger.Output.LogHr();
            });

            return logger;
        }

        private void ApplySupplement()
        {
            // Supplement files
            if (!_supplementApplied)
            {
                _supplementApplied = true;
                ApplyOverlay(Resources.supplement);
                ApplyOverlay(Resources.delorca);
            }
        }

        private void ApplyOverlay(byte[] zipData)
        {
            var supplementZip = new ZipArchive(new MemoryStream(zipData));
            foreach (var entry in supplementZip.Entries)
            {
                if (entry.Length == 0)
                    continue;

                var data = entry.GetData();
                _fileRepository.SetGameFileData(entry.FullName, data);
            }
        }

        private List<Area> GetAreas(Campaign campaign)
        {
            var areaRepo = campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;
            var areas = new List<Area>();
            foreach (var areaDef in areaRepo.Areas)
            {
                var areaData = _fileRepository.GetGameFileData(areaDef.Path);
                if (areaData == null)
                    continue;

                var area = new Area(areaDef, EnemyClassFactory, areaData);
                areas.Add(area);
            }
            _areas = areas.ToImmutableArray();
            return areas;
        }

        private void IterateModifiers(Action<string, Modifier> action)
        {
            foreach (var modifier in _modifiers)
            {
                var name = modifier.GetType().Name.Replace("Modifier", "");
                action(name, modifier);
            }
        }

        private static ImmutableArray<Modifier> GetModifiers()
        {
            return new Modifier[]
            {
                new FixesModifier(),
                new InventoryModifier(),
                new MerchantShopModifier(),
                new WeaponModifier(),
                new ItemModifier(),
                new LevelItemModifier(),
                new EnemyPlaceModifier(),
                new EnemyModifier(),
            }.ToImmutableArray();
        }

        private Dictionary<string, object?> GetRszDictionary(RszInstance instance)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var field in instance.Fields)
            {
                var value = instance.GetFieldValue(field.name);
                if (value is RszInstance child)
                {
                    value = GetRszDictionary(child);
                }
                dict[field.name] = value;
            }
            return dict;
        }

        public T? GetConfigOption<T>(string key, T? defaultValue = default)
        {
            if (_input.Configuration == null)
                return defaultValue;
            return _input.Configuration.GetValueOrDefault<T>(key, defaultValue);
        }

        public Rng CreateRng()
        {
            return _rng.NextFork();
        }
    }
}
