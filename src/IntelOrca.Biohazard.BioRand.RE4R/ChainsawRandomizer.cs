using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer : IChainsawRandomizer
    {
        private readonly FileRepository _fileRepository;
        private readonly RszFileOption _rszFileOption;

        public EnemyClassFactory EnemyClassFactory { get; }

        public ChainsawRandomizer(FileRepository fileRepository, EnemyClassFactory enemyClassFactory, RszFileOption rszFileOption)
        {
            _fileRepository = fileRepository;
            EnemyClassFactory = enemyClassFactory;
            _rszFileOption = rszFileOption;
        }

        private void LogRoomEnemies(EnemyClassFactory enemyClassFactory)
        {
            var files = Directory.GetFiles(@"G:\re4r\extract\patch_003\natives\stm\_chainsaw\leveldesign", "*.scn.20", SearchOption.AllDirectories);
            var map = new Dictionary<string, string[]>();
            foreach (var file in files)
            {
                try
                {
                    var area = new Area(enemyClassFactory, file);
                    var enemyNames = new List<string>();
                    foreach (var go in area.ScnFile.IterAllGameObjects(true))
                    {
                        foreach (var component in go.Components)
                        {
                            var enemyKind = enemyClassFactory.FindEnemyKind(component.Name);
                            if (enemyKind != null)
                            {
                                enemyNames.Add(enemyKind.Name);
                            }
                        }
                    }
                    if (enemyNames.Count != 0)
                    {
                        map[file] = enemyNames.ToArray();
                    }
                }
                catch
                {
                }
            }
            foreach (var m in map)
            {
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"{m.Key}:");
                Console.WriteLine("---------------------------------------");
                foreach (var e in m.Value)
                {
                    Console.WriteLine(e);
                }
                Console.WriteLine();
            }
        }

        private string? FindFile(string fileName)
        {
            var basePath = @"G:\re4r\extract\patch_003";
            var path = Path.Combine(basePath, fileName);
            if (File.Exists(path))
            {
                return path;
            }
            return null;
        }

        private string GetOutputPath(string fileName)
        {
            var basePath = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4";
            return Path.Combine(basePath, fileName);
        }

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            var random = new Random(input.Seed);
            foreach (var areaPath in _areaPaths)
            {
                var areaData = _fileRepository.GetGameFileData(areaPath);
                if (areaData == null)
                    continue;

                var area = new Area(EnemyClassFactory, _rszFileOption, areaPath, areaData);
                RandomizeArea(area, random);
                _fileRepository.SetGameFileData(areaPath, area.SaveData());
            }
            return new RandomizerOutput(_fileRepository);
        }

        private void RandomizeArea(Area area, Random random)
        {
            var oldEnemies = area.Enemies.Select(GetEnemySummary).ToArray();
            var multiplier = 1.5;
            var enemies = area.Enemies;
            var newEnemyCount = (int)enemies.Length * multiplier;
            var delta = (int)Math.Round(newEnemyCount - enemies.Length);
            if (delta != 0)
            {
                var bag = new EndlessBag<Enemy>(random, enemies);
                var enemiesToCopy = bag.Next(delta);
                foreach (var e in enemiesToCopy)
                {
                    area.Duplicate(e);
                }
            }

            foreach (var enemy in area.Enemies)
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

            LogEnemyTable(area, oldEnemies);
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

        private static string[] _areaPaths = new[] {
            // Chapter 1
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_010.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_020.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_030.scn.20",

            // Chapter 2
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_2/level_cp10_chp1_2.scn.20",
            "natives/stm/_chainsaw/leveldesign/location/loc44/level_loc44.scn.20",

            // Chapter 3
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_3/level_cp10_chp1_3.scn.20",
            "natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_3/level_cp10_chp1_3_000.scn.20",
        };
    }
}
