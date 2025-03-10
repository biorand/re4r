﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using RszTool;

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

        public void Dispose()
        {
            _fileRepository?.Dispose();
        }

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            _input = input;
            if (input.GamePath != null)
            {
                _fileRepository = new FileRepository(input.GamePath);
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

            var output = new ChainsawRandomizerOutput(input, _fileRepository.GetOutputPakFile(), logFiles);
            return new RandomizerOutput(
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
            _valuableDistributor.Setup(_itemRandomizer, CreateRng(), logger.Process);

            var inventoryRng = CreateRng();
            var merchantRng = CreateRng();
            var enemyRng = CreateRng();
            var itemRng = CreateRng();

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
                ApplyOverlay(EmbeddedData.GetFile("supplement.zip"));
                ApplyOverlay(EmbeddedData.GetFile("delorca.zip"));
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
                new CasePerkModifier(),
                new InventoryModifier(),
                new RecipeModifier(),
                new MerchantShopModifier(),
                new WeaponModifier(),
                new ItemModifier(),
                new GimmickPlaceModifier(),
                new GimmickModifier(),
                new LevelItemModifier(),
                new EnemyPlaceModifier(),
                new EnemyModifier(),
                new FixesModifier(),
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
