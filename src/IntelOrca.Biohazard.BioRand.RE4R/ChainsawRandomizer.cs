﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer : IChainsawRandomizer
    {
        private FileRepository _fileRepository = new FileRepository();
        private readonly RszFileOption _rszFileOption;
        private readonly RandomizerLogger _loggerInput = new RandomizerLogger();
        private readonly RandomizerLogger _loggerProcess = new RandomizerLogger();
        private readonly RandomizerLogger _loggerOutput = new RandomizerLogger();
        private RandomizerInput _input = new RandomizerInput();
        private Rng.Table<EnemyClassDefinition>? _enemyRngTable;
        private Rng.Table<ItemDefinition?>? _itemRngTable;

        public EnemyClassFactory EnemyClassFactory { get; }

        public ChainsawRandomizer(EnemyClassFactory enemyClassFactory, RszFileOption rszFileOption)
        {
            EnemyClassFactory = enemyClassFactory;
            _rszFileOption = rszFileOption;
        }

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            _input = input;
            if (input.GamePath != null)
            {
                _fileRepository = new FileRepository(input.GamePath);
            }

            var rng = new Rng(input.Seed);
            var areaRepo = AreaDefinitionRepository.Default;
            var areas = new List<Area>();
            foreach (var areaDef in areaRepo.Areas)
            {
                var areaData = _fileRepository.GetGameFileData(areaDef.Path);
                if (areaData == null)
                    continue;

                var area = new Area(areaDef, EnemyClassFactory, _rszFileOption, areaData);
                areas.Add(area);
            }

            LogAreas(_loggerInput, areas);
            foreach (var area in areas)
            {
                RandomizeArea(area, rng);
            };
            Parallel.ForEach(areas, area =>
            {
                _fileRepository.SetGameFileData(area.Definition.Path, area.SaveData());
            });
            LogAreas(_loggerOutput, areas);

            var logFiles = new LogFiles(_loggerInput.Output, _loggerProcess.Output, _loggerOutput.Output);
            return new RandomizerOutput(_fileRepository.GetOutputPakFile(), logFiles);
        }

        private void LogAreas(RandomizerLogger logger, List<Area> areas)
        {
            foreach (var area in areas)
            {
                logger.LogArea(area);
                foreach (var enemy in area.Enemies)
                {
                    logger.LogEnemy(enemy);
                }
            }
        }

        private T? GetConfigOption<T>(string key, T? defaultValue = default)
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

        private void RandomizeArea(Area area, Rng rng)
        {
            var oldEnemies = area.Enemies;
            var def = area.Definition;
            if (def.Exclude is string[] exclude)
            {
                var excludeGuidArray = exclude.Select(x => new Guid(x)).ToHashSet();
                oldEnemies = oldEnemies
                    .Where(x => !excludeGuidArray.Contains(x.Guid))
                    .ToArray();
            }

            var oldEnemiesSummary = area.Enemies.Select(GetEnemySummary).ToArray();
            var multiplier = GetConfigOption<double>("enemy-multiplier", 1);
            var newEnemyCount = (int)oldEnemies.Length * multiplier;
            var delta = (int)Math.Round(newEnemyCount - oldEnemies.Length);
            if (delta != 0)
            {
                var bag = new EndlessBag<Enemy>(rng, oldEnemies);
                var enemiesToCopy = bag.Next(delta);
                foreach (var e in enemiesToCopy)
                {
                    area.Duplicate(e);
                }
            }

            foreach (var enemy in area.Enemies)
            {
                var e = enemy;
                var ecd = GetRandomEnemyClass(rng);
                e = area.ConvertTo(e, ecd.Kind.ComponentName);

                if (ecd.Weapon.Length == 0)
                {
                    e.Weapon = 0;
                    e.SecondaryWeapon = 0;
                }
                else
                {
                    var weaponChoice = rng.Next(ecd.Weapon);
                    e.Weapon = weaponChoice.Primary?.Id ?? 0;
                    e.SecondaryWeapon = weaponChoice.Secondary?.Id ?? 0;
                }

                foreach (var fd in ecd.Fields)
                {
                    var fieldValue = rng.Next(fd.Values);
                    e.SetFieldValue(fd.Name, fieldValue);
                }

                RandomizeHealth(e, rng);
                RandomizeDrop(e, rng);
            }
        }

        private void RandomizeHealth(Enemy enemy, Rng rng)
        {
            var randomHealth = GetConfigOption<bool>("enemy-random-health");
            if (randomHealth)
            {
                // enemy.Health = rng.Next(400, 1000);
                enemy.Health = 100;
            }
            else
            {
                enemy.Health = null;
            }
        }

        private void RandomizeDrop(Enemy enemy, Rng rng)
        {
            var repo = ItemDefinitionRepository.Default;
            if (enemy.ItemDrop is Item drop && !drop.IsAutomatic)
            {
                var currentItemId = drop.Id;
                var itemDef = repo.Find(currentItemId);
                if (itemDef?.Kind == null || itemDef.Kind == ItemKinds.Key)
                {
                    // Don't change the drop for this enemy
                    return;
                }
            }

            enemy.ItemDrop = GetRandomItem(rng);
        }

        private void LogEnemyTable(Area area, string[] oldEnemies)
        {
            var newEnemies = area.Enemies.Select(GetEnemySummary).ToArray();
            var enemyCount = Math.Max(oldEnemies.Length, newEnemies.Length);
            if (enemyCount != 0)
            {
                var lhColumn = oldEnemies.Max(x => x.Length);
                var rhColumn = newEnemies.Max(x => x.Length);
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine($"Area: {area.FileName}");
                Console.WriteLine($"------------------------------------------------------");
                for (var i = 0; i < enemyCount; i++)
                {
                    var oldE = i < oldEnemies.Length ? oldEnemies[i] : "";
                    var newE = i < newEnemies.Length ? newEnemies[i] : "";
                    Console.WriteLine($"| {oldE.PadRight(lhColumn)} | {newE.PadRight(rhColumn)} |");
                }
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine();
            }
        }

        private static string GetEnemySummary(Enemy enemy)
        {
            return $"{enemy.Kind} ({enemy.Health})";
        }

        private EnemyClassDefinition GetRandomEnemyClass(Rng rng)
        {
            if (_enemyRngTable == null)
            {
                var table = rng.CreateProbabilityTable<EnemyClassDefinition>();
                foreach (var enemyClass in EnemyClassFactory.Classes)
                {
                    var ratio = GetConfigOption<double>($"enemy-ratio-{enemyClass.Name}");
                    if (ratio != 0)
                    {
                        table.Add(enemyClass, ratio);
                    }
                }
                _enemyRngTable = table;
            }
            return _enemyRngTable.Next();
        }

        private Item? GetRandomItem(Rng rng)
        {
            if (_itemRngTable == null)
            {
                var table = rng.CreateProbabilityTable<ItemDefinition?>();

                var repo = ItemDefinitionRepository.Default;
                var kindRatios = new List<(string, double)>();
                foreach (var itemKind in repo.Kinds)
                {
                    var ratio = GetConfigOption<double>($"drop-ratio-{itemKind}");
                    if (ratio != 0)
                    {
                        kindRatios.Add((itemKind, ratio));
                    }
                }

                var total = kindRatios.Select(x => x.Item2).Sum();

                var autoRatio = GetConfigOption<double>("drop-ratio-automatic");
                if (autoRatio != 0)
                {
                    total += autoRatio;
                    table.Add(ItemDefinition.Automatic, autoRatio / total);
                }

                var noneRatio = GetConfigOption<double>("drop-ratio-none");
                if (noneRatio != 0)
                {
                    total += noneRatio;
                    table.Add(null, noneRatio / total);
                }

                foreach (var (kind, ratio) in kindRatios)
                {
                    var itemsForThisKind = repo.KindToItemMap[kind];
                    var p = (ratio / total) / itemsForThisKind.Length;
                    foreach (var id in itemsForThisKind)
                    {
                        table.Add(id, p);
                    }
                }

                _itemRngTable = table;
            }

            var def = _itemRngTable.Next();
            if (def == null)
            {
                return null;
            }
            else
            {
                var item = new Item(def.Id, 1);
                return item;
            }
        }
    }
}
