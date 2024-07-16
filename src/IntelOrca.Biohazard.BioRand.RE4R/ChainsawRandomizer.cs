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
    internal class ChainsawRandomizer : IRandomizer
    {
        private FileRepository _fileRepository = new FileRepository();
        private readonly RandomizerLogger _loggerInput = new RandomizerLogger();
        private readonly RandomizerLogger _loggerProcess = new RandomizerLogger();
        private readonly RandomizerLogger _loggerOutput = new RandomizerLogger();
        private RandomizerInput _input = new RandomizerInput();
        private ValuableDistributor? _valuableDistributor;
        private ItemRandomizer? _itemRandomizer;
        private readonly ImmutableArray<Modifier> _modifiers = GetModifiers();
        private ImmutableArray<Area> _areas;
        private Rng _rng = new Rng();

        public RandomizerLogger LoggerProcess => _loggerProcess;

        public EnemyClassFactory EnemyClassFactory { get; }
        public FileRepository FileRepository => _fileRepository;

        public ValuableDistributor ValuableDistributor => _valuableDistributor!;
        public ItemRandomizer ItemRandomizer => _itemRandomizer!;
        public ImmutableArray<Area> Areas => _areas;

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

            foreach (var logger in new[] { _loggerInput, _loggerProcess, _loggerOutput })
            {
                logger.LogHr();
                logger.LogVersion();
                logger.LogLine($"Seed = {input.Seed}");
                logger.LogHr();
            }
            var areas = GetAreas();

            _itemRandomizer = new ItemRandomizer(this);

            // Supplement files
            ApplyOverlay(Resources.supplement);
            ApplyOverlay(Resources.delorca);

            var rng = new Rng(input.Seed);
            _rng = rng;

            _valuableDistributor = new ValuableDistributor(this);
            _valuableDistributor.Setup(_itemRandomizer, _rng, _loggerProcess);

            var inventoryRng = CreateRng();
            var merchantRng = CreateRng();
            var enemyRng = CreateRng();
            var itemRng = CreateRng();

            var itemData = ChainsawItemData.FromData(_fileRepository);

            // Input
            IterateModifiers((n, m) =>
            {
                _loggerInput.Push(n);
                m.LogState(this, _loggerInput);
                _loggerInput.Pop();
                _loggerInput.LogHr();
            });

            // Apply modifiers
            IterateModifiers((n, m) =>
            {
                _loggerProcess.Push(n);
                m.Apply(this, _loggerProcess);
                _loggerProcess.Pop();
                _loggerProcess.LogHr();
            });

            // Save area files
            Parallel.ForEach(areas, area =>
            {
                _fileRepository.SetGameFileData(area.Definition.Path, area.SaveData());
            });

            // Output
            IterateModifiers((n, m) =>
            {
                _loggerOutput.Push(n);
                m.LogState(this, _loggerOutput);
                _loggerOutput.Pop();
                _loggerOutput.LogHr();
            });

            var logFiles = new LogFiles(_loggerInput.Output, _loggerProcess.Output, _loggerOutput.Output);
            var output = new ChainsawRandomizerOutput(input, _fileRepository.GetOutputPakFile(), logFiles);
            return new RandomizerOutput(
                output.GetOutputZip(),
                output.GetOutputMod(),
                new Dictionary<string, string>()
                {
                    ["input.log"] = _loggerInput.Output,
                    ["process.log"] = _loggerProcess.Output,
                    ["output.log"] = _loggerOutput.Output
                });
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

        private List<Area> GetAreas()
        {
            var areaRepo = AreaDefinitionRepository.Default;
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
            if (_input.Configuration != null && _input.Configuration.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else
            {
                return defaultValue;
            }
        }

        public Rng CreateRng()
        {
            return _rng.NextFork();
        }

        public object ConfigurationDefinition
        {
            get
            {
                var configDefinition = RandomizerConfigurationDefinition.Create(EnemyClassFactory);
                return configDefinition;
            }
        }

        public RandomizerConfiguration DefaultConfiguration
        {
            get
            {
                var def = RandomizerConfigurationDefinition.Create(EnemyClassFactory);
                return def.GetDefault();
            }
        }

        public string BuildVersion => ChainsawRandomizerFactory.Default.GitHash;
    }
}
