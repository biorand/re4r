using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
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
        private Rng.Table<int>? _parasiteRngTable;
        private Queue<EnemyClassDefinition> _enemyClassQueue = new Queue<EnemyClassDefinition>();
        private int _uniqueHp = 1;
        private int _contextId = 5000;

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

            // Supplement files
            var supplementZip = new ZipArchive(new MemoryStream(Resources.supplement));
            foreach (var entry in supplementZip.Entries)
            {
                if (entry.Length == 0)
                    continue;

                var data = entry.GetData();
                _fileRepository.SetGameFileData(entry.FullName, data);
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

#if DEBUG
            LogAllEnemies(areas, e => e.Kind.Key == "armadura");
#endif
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

        private void LogAllEnemies(List<Area> areas, Predicate<Enemy> predicate)
        {
            var all = new List<Dictionary<string, object?>>();
            foreach (var area in areas)
            {
                foreach (var enemy in area.Enemies)
                {
                    if (!predicate(enemy)) continue;

                    var enemyDict = GetRszDictionary(enemy.MainComponent);
                    enemyDict["area"] = area.FileName;
                    enemyDict["guid"] = enemy.Guid;
                    all.Add(enemyDict);
                }
            }
            var jsonOutput = JsonSerializer.Serialize(all, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            jsonOutput.WriteToFile(@"C:\Users\Ted\.biorand\logs\enemy_attributes.json");
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

        private void LogAreas(RandomizerLogger logger, List<Area> areas)
        {
            logger.LogHr();
            logger.LogVersion();
            logger.LogHr();
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

            oldEnemies = oldEnemies
                .Where(x => !x.Kind.Closed)
                .ToArray();

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
                    area.Duplicate(e, GetNextContextId());
                }
            }

            var enemies = area.Enemies;
            var numEnemies = enemies.Length;
            var enemyClasses = new HashSet<EnemyClassDefinition>();
            foreach (var enemy in enemies)
            {
                var e = enemy;
                if (e.Kind.Closed)
                    continue;

                var ecd = GetRandomEnemyClass(enemyClasses, rng);
                e = area.ConvertTo(e, ecd.Kind.ComponentName);

                // Reset various fields
                e.SetFieldValue("_RandamizeMontageID", false);
                e.SetFieldValue("_RandomMontageID", 0);
                e.SetFieldValue("_MontageID", 0);
                e.SetFieldValue("_FixedVoiceID", 0);

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

                RandomizeHealth(e, ecd, rng);
                RandomizeDrop(e, ecd, rng);

                // If there are a lot of enemies, plaga seems to randomly crash the game
                // E.g. village, 360 zealots, 25 plaga will crash
                if (numEnemies < 100)
                {
                    RandomizeParasite(e, rng);
                }
            }
        }

        private int GetNextContextId()
        {
            return _contextId++;
        }

        private void RandomizeHealth(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var debugUniqueHp = GetConfigOption<bool>("debug-unique-enemy-hp");
            if (debugUniqueHp)
            {
                enemy.Health = _uniqueHp++;
            }
            else
            {
                var randomHealth = GetConfigOption<bool>("enemy-custom-health");
                if (randomHealth)
                {
                    var minHealth = GetConfigOption<int>($"enemy-health-min-{ecd.Key}");
                    var maxHealth = GetConfigOption<int>($"enemy-health-max-{ecd.Key}");
                    minHealth = Math.Clamp(minHealth, 1, 100000);
                    maxHealth = Math.Clamp(maxHealth, minHealth, 100000);
                    enemy.Health = rng.Next(minHealth, maxHealth + 1);
                }
                else
                {
                    enemy.Health = null;
                }
            }
        }

        private void RandomizeParasite(Enemy enemy, Rng rng)
        {
            if (_parasiteRngTable == null)
            {
                var table = rng.CreateProbabilityTable<int>();
                table.Add(0, GetConfigOption<double>("parasite-ratio-none"));
                table.Add(1, GetConfigOption<double>("parasite-ratio-a"));
                table.Add(2, GetConfigOption<double>("parasite-ratio-b"));
                _parasiteRngTable = table;
            }
            if (enemy.ParasiteKind != null)
            {
                var kind = _parasiteRngTable.Next();
                if (kind == 0)
                {
                    enemy.ParasiteKind = 0;
                    enemy.ForceParasiteAppearance = false;
                    enemy.ParasiteAppearanceProbability = 0;
                }
                else
                {
                    enemy.ParasiteKind = kind;
                    enemy.ForceParasiteAppearance = true;
                    enemy.ParasiteAppearanceProbability = 100;
                }
            }
        }

        private void RandomizeDrop(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
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

            if (ecd.Class < 6)
                enemy.ItemDrop = GetRandomValuableItem(enemy, ecd, rng);
            else
                enemy.ItemDrop = GetRandomItem(enemy, rng);
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

        private EnemyClassDefinition GetRandomEnemyClass(HashSet<EnemyClassDefinition> enemyClasses, Rng rng)
        {
            if (_enemyRngTable == null)
            {
                var table = rng.CreateProbabilityTable<EnemyClassDefinition>();
                foreach (var enemyClass in EnemyClassFactory.Classes)
                {
                    var ratio = GetConfigOption<double>($"enemy-ratio-{enemyClass.Key}");
                    if (ratio != 0)
                    {
                        table.Add(enemyClass, ratio);
                    }
                }
                _enemyRngTable = table;
            }

            if (_enemyClassQueue.Count == 0)
            {
                EnemyClassDefinition ecd;
                var variety = GetConfigOption<int>($"enemy-variety");
                if (variety != 0 && enemyClasses.Count >= variety)
                {
                    ecd = enemyClasses.Shuffle(rng).First();
                }
                else
                {
                    ecd = _enemyRngTable.Next();
                    enemyClasses.Add(ecd);
                }

                var packCount = GetPackCount(ecd, rng);
                for (var i = 0; i < packCount; i++)
                {
                    _enemyClassQueue.Enqueue(ecd);
                }
            }
            return _enemyClassQueue.Dequeue();
        }

        private int GetPackCount(EnemyClassDefinition ecd, Rng rng)
        {
            return rng.Next(1, ecd.Class);
        }

        private Item? GetRandomItem(Enemy enemy, Rng rng)
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
                    foreach (var itemDef in itemsForThisKind)
                    {
                        if (string.IsNullOrEmpty(itemDef.Mode))
                        {
                            table.Add(itemDef, p);
                        }
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
                var amount = 1;
                if (def.Kind == ItemKinds.Money)
                {
                    var multiplier = GetConfigOption<double>("money-quantity");
                    amount = Math.Max(1, (int)(rng.Next(100, 2000) * multiplier));
                }
                else if (def.Kind == ItemKinds.Ammo)
                {
                    var multiplier = GetConfigOption<double>("ammo-quantity");
                    amount = Math.Max(1, (int)(rng.Next(10, 50) * multiplier));
                }
                else if (def.Kind == ItemKinds.Gunpowder)
                {
                    amount = Math.Max(1, 10);
                }
                var item = new Item(def.Id, amount);
                return item;
            }
        }

        private Item? GetRandomValuableItem(Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var kinds = itemRepo.Kinds
                .Where(x => GetConfigOption<bool>($"drop-valuable-{x}"))
                .ToArray();

            if (kinds.Length == 0)
                return GetRandomItem(enemy, rng);

            var kind = rng.Next(kinds);
            var itemPool = itemRepo.KindToItemMap[kind];

            var minValue = (10 - ecd.Class) * 800;
            var maxValue = minValue * 3;
            if (kind == ItemKinds.Money)
            {
                var amount = rng.Next(minValue, maxValue + 1);
                return new Item(itemPool[0].Id, amount);
            }
            else
            {
                var filteredItems = itemPool
                    .Where(x => string.IsNullOrEmpty(x.Mode))
                    .Where(x => x.Value >= minValue && x.Value <= maxValue)
                    .ToImmutableArray();

                if (filteredItems.Length == 0)
                    filteredItems = itemPool;

                var chosenItem = rng.Next(filteredItems);
                return new Item(chosenItem.Id, 1);
            }
        }
    }
}
