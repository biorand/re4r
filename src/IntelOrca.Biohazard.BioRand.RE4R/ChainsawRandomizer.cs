using System;
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

            var random = new Random(input.Seed);
            var areaRepo = AreaDefinitionRepository.Default;
            var areas = new List<Area>();
            foreach (var areaDef in areaRepo.Areas)
            {
                var areaPath = areaDef.Path;
                var areaData = _fileRepository.GetGameFileData(areaPath);
                if (areaData == null)
                    continue;

                var area = new Area(areaDef, EnemyClassFactory, _rszFileOption, areaPath, areaData);
                areas.Add(area);
            }

            LogAreas(_loggerInput, areas);
            foreach (var area in areas)
            {
                RandomizeArea(area, random);
            };
            Parallel.ForEach(areas, area =>
            {
                _fileRepository.SetGameFileData(area.FileName, area.SaveData());
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

        private void RandomizeArea(Area area, Random random)
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
                var bag = new EndlessBag<Enemy>(random, oldEnemies);
                var enemiesToCopy = bag.Next(delta);
                foreach (var e in enemiesToCopy)
                {
                    area.Duplicate(e);
                }
            }

            foreach (var enemy in oldEnemies)
            {
                var e = enemy;
                var ecd = area.EnemyClassFactory.Next(random);
                e = area.ConvertTo(e, ecd.Kind.ComponentName);

                if (ecd.Weapon.Length == 0)
                {
                    e.Weapon = 0;
                    e.SecondaryWeapon = 0;
                }
                else
                {
                    var weaponChoice = random.NextOf(ecd.Weapon);
                    e.Weapon = weaponChoice.Primary?.Id ?? 0;
                    e.SecondaryWeapon = weaponChoice.Secondary?.Id ?? 0;
                }

                foreach (var fd in ecd.Fields)
                {
                    var fieldValue = random.NextOf(fd.Values);
                    e.SetFieldValue(fd.Name, fieldValue);
                }

                e.Health = random.Next(400, 1000);
            }
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
    }
}
