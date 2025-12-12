using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Cryptography;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer : IDisposable
    {
        private FileRepository _fileRepository = new FileRepository();
        private RandomizerInput _input = new RandomizerInput();
        private bool _supplementApplied;
        private ValuableDistributor? _valuableDistributor;
        private ItemRandomizer? _itemRandomizer;
        private ImmutableArray<Modifier> _modifiers = GetModifiers();

        public EnemyClassFactory EnemyClassFactory { get; }
        public IProgressReporter Reporter { get; }
        public FileRepository FileRepository => _fileRepository;
        public DynamicData DynamicData { get; }

        public AreaService AreaService { get; private set; }
        public ValuableDistributor ValuableDistributor => _valuableDistributor!;
        public ItemRandomizer ItemRandomizer => _itemRandomizer!;
        public EnemyService EnemyService { get; private set; }
        public ItemService ItemService { get; private set; }
        public FlagService FlagService { get; private set; }
        public Campaign Campaign { get; private set; }

        public ChainsawRandomizer(EnemyClassFactory enemyClassFactory, RandomizerInput input, IProgressReporter reporter)
        {
            EnemyClassFactory = enemyClassFactory;
            _input = input;
            Reporter = reporter;

            DynamicData = new DynamicData(_input.Configuration.GetValueOrDefault<bool>("debug-download-data"));
            AreaService = new AreaService(this);
            EnemyService = new EnemyService(DynamicData);
            ItemService = new ItemService(DynamicData);
            FlagService = new FlagService(this);
        }

        public void Dispose()
        {
            _fileRepository?.Dispose();
        }

        public RandomizerOutput Randomize()
        {
            var input = _input;
            if (input.GamePath != null)
            {
                _fileRepository = new FileRepository(this, input.GamePath, DynamicData);
            }

            var logFiles = new Dictionary<string, string>();

            var campaigns = new[] { Campaign.Leon };
            if (input.Configuration.GetValueOrDefault("separate-ways", false))
            {
                campaigns = [Campaign.Leon, Campaign.Ada];
            }

            foreach (var campaign in campaigns)
            {
                var log = Randomize(input, campaign);

                var name = campaign.ToString().ToLowerInvariant();
                logFiles[$"input_{name}.log"] = log.Input.Output;
                logFiles[$"process_{name}.log"] = log.Process.Output;
                logFiles[$"output_{name}.log"] = log.Output.Output;
            }

            RandomizerOutput? result = null;
            Reporter.RunTask("Building mod", () =>
            {
                var output = new ChainsawRandomizerOutput(input, _fileRepository.GetOutputPakFile(), logFiles);
                result = new RandomizerOutput(
                    [
                        new RandomizerOutputAsset(
                            "1-patch",
                            "Patch",
                            "Simply drop this file into your RE 4 install folder.",
                            $"biorand-re4r-{input.Seed}.zip",
                            output.GetOutputZip()),
                        new RandomizerOutputAsset(
                            "2-fluffy",
                            "Fluffy Mod",
                            "Drop this zip file into Fluffy Mod Manager's mod folder and enable it.",
                            $"biorand-re4r-{input.Seed}-mod.zip",
                            output.GetOutputMod())
                    ],
                    """
                    <p class="mt-3">What should I do if my game crashes?</p>
                    <ol class="ml-8 list-decimal text-gray-300">
                      <li>Reload from last checkpoint and try again.</li>
                      <li>Alter the enemy sliders slightly or reduce the number temporarily. This will reshuffle the enemies. Reload from last checkpoint and try again.</li> <li>As a last resort, change your seed, and reload from last checkpoint.</li>
                    </ol>
                    """,
                    logFiles);
            });
            return result!;
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

            _itemRandomizer = new ItemRandomizer(this, logger.Process);

            _valuableDistributor = new ValuableDistributor(this);
            _valuableDistributor.Setup(_itemRandomizer, GetRng("service/valuabledistributor"), logger.Process);

            // Patches
            if (campaign != Campaign.Ada)
            {
                Reporter.RunTask("Applying patches", () => ExportedMods.ApplyAll(this, FileRepository));
            }

            // Create areas after patches
            Reporter.RunTask("Loading scenes", () => AreaService.LoadAreas(campaign));

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
                Reporter.RunTask($"Running modifier: {n}", () => m.Apply(this, logger.Process));
                logger.Process.Pop();
                logger.Process.LogHr();
            });

            FlagService.Save(logger.Process);
            Reporter.RunTask("Rebuilding scenes", () => AreaService.Save(logger.Process));

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
                FileRepository.ApplyOverlay(EmbeddedData.GetFile("supplement.zip"));
            }
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
                new CasePerkModifier(),
                new InventoryModifier(),
                new RecipeModifier(),
                new MerchantShopModifier(),
                new WeaponModifier(),
                new ItemModifier(),
                new BattleModifier(),
                new GimmickPlaceModifier(),
                new GimmickModifier(),
                new DropItemPlaceModifier(),
                new DropItemModifier(),
                new EnemyPlaceModifier(),
                new EnemyMultiplierModifier(),
                new EnemyWaveModifier(),
                new EnemyModifier(),
                new FixesModifier(),
            }.ToImmutableArray();
        }

        public Rng GetRng(params object[] key)
        {
            var hashInput = string.Concat([_input.Seed, .. key]);
            var seed = MurMur3.HashData(hashInput);
            return new Rng(seed);
        }

        public T? GetConfigOption<T>(string key, T? defaultValue = default)
        {
            if (_input.Configuration == null)
                return defaultValue;
            return _input.Configuration.GetValueOrDefault<T>(key, defaultValue);
        }

        public bool HasSpecialTouch(string kind)
        {
            if (!GetConfigOption("enable-special", true))
                return false;

            var special = GetConfigOption<string>("special");
            var present = special?.Split(',').Contains(kind) == true;
            return present;
        }
    }
}
