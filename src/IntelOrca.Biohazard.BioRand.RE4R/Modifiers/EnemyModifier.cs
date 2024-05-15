using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyModifier : Modifier
    {
        private int _contextId;
        private int _uniqueHp;
        private Rng.Table<EnemyClassDefinition>? _allEnemyRngTable;
        private Rng.Table<int>? _parasiteRngTable;
        private ImmutableArray<EnemyClassDefinition> _allEnemyClasses;

        private Dictionary<int, int> _stageEnemyCount = new();

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            foreach (var area in randomizer.Areas)
            {
                logger.Push(area.FileName);
                foreach (var enemy in area.Enemies)
                {
                    LogEnemy(enemy, logger);
                }
                logger.Pop();
            }
        }

        private static void LogEnemy(Enemy enemy, RandomizerLogger logger)
        {
            var weapons = "";
            foreach (var w in new[] { enemy.Weapon, enemy.SecondaryWeapon })
            {
                if (w != 0)
                {
                    var ecf = EnemyClassFactory.Default;
                    var weaponDef = ecf.Weapons.FirstOrDefault(x => x.Id == w);
                    if (weaponDef != null)
                    {
                        if (weapons.Length != 0)
                            weapons += " | ";
                        weapons += weaponDef.Key;
                    }
                }
            }

            var itemDrop = ".";
            if (enemy.ItemDrop is Item drop)
            {
                itemDrop = "*";
                if (!drop.IsAutomatic)
                {
                    var itemRepo = ItemDefinitionRepository.Default;
                    var itemDef = itemRepo.Find(drop.Id);
                    if (itemDef != null)
                    {
                        itemDrop = itemDef.Name ?? itemDef.Id.ToString();
                        itemDrop += $" x{drop.Count}";
                    }
                }
            }

            var parasite = "";
            if ((enemy.ParasiteKind ?? 0) != 0)
            {
                if (enemy.ParasiteKind == 1)
                    parasite = "pA(";
                else if (enemy.ParasiteKind == 2)
                    parasite = "pB(";
                else if (enemy.ParasiteKind == 3)
                    parasite = "pC(";
                if (enemy.ForceParasiteAppearance)
                    parasite += "100%)";
                else
                    parasite += $"{enemy.ParasiteAppearanceProbability}%)";
            }

            logger.LogLine(
                enemy.Guid,
                enemy.StageID,
                enemy.Kind.Key,
                weapons,
                enemy.Health?.ToString() ?? "*",
                parasite,
                itemDrop);
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var randomItemSettings = new RandomItemSettings
            {
                ItemRatioKeyFunc = (dropKind) => randomizer.GetConfigOption<double>($"enemy-drop-ratio-{dropKind}"),
                MinAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-min", 0.1),
                MaxAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-max", 1.0),
                MinMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-min", 100),
                MaxMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-max", 1000),
            };

            _contextId = 5000;
            _uniqueHp = 1;
            _allEnemyClasses = randomizer.EnemyClassFactory.Classes
                .Where(x => GetClassRatio(randomizer, x) > 0)
                .ToImmutableArray();

            var rng = randomizer.CreateRng();
            var areaByChapter = randomizer.Areas.GroupBy(x => x.Definition.Chapter);

            // Randomize enemies
            if (randomizer.GetConfigOption<bool>("random-enemies"))
            {
                logger.Push("Randomizing enemies");
                foreach (var chapterAreas in areaByChapter)
                {
                    logger.Push($"Chapter {chapterAreas.Key}");
                    foreach (var area in chapterAreas)
                    {
                        logger.Push(area.FileName);
                        RandomizeArea(randomizer, area, rng, logger);
                        logger.Pop();
                    }
                    _stageEnemyCount.Clear();
                    logger.Pop();
                }
                logger.Pop();
            }

            // Randomize enemy health
            logger.Push("Randomizing health");
            foreach (var group in areaByChapter)
            {
                var chapter = group.Key;
                var enemies = group
                    .SelectMany(x => x.EnemySpawns)
                    .ToImmutableArray();
                RandomizeEnemyHealth(randomizer, chapter, enemies, rng, logger);
            }
            logger.Pop();

            if (randomizer.GetConfigOption<bool>("random-enemy-drops"))
            {
                // Randomize enemy drops
                logger.Push("Randomizing drops");
                foreach (var group in areaByChapter)
                {
                    var chapter = group.Key;
                    var enemies = group
                        .SelectMany(x => x.EnemySpawns)
                        .Where(x => !x.Enemy.Kind.NoItemDrop)
                        .Where(x => !x.HasKeyItem)
                        .Where(x => x.Guid != new Guid("a61c62f3-52e7-4d78-a167-c99b84fcada9")) // Mendez (phase 1)
                        .ToImmutableArray();
                    RandomizeEnemyDrops(randomizer, randomItemSettings, chapter, enemies, rng, logger);
                }
                logger.Pop();
            }
        }

        private void RandomizeArea(ChainsawRandomizer randomizer, Area area, Rng rng, RandomizerLogger logger)
        {
            var healthRng = rng.NextFork();
            var dropRng = rng.NextFork();
            var parasiteRng = rng.NextFork();

            // Create initial list of enemy spawns
            var spawns = area.Enemies.Select(e => new EnemySpawn(area, e, e)).ToImmutableArray();
            foreach (var spawn in spawns)
            {
                SetClassPool(randomizer, area, spawn);
            }

            // Duplicate enemy spawns
            foreach (var spawn in spawns)
            {
                var stageId = spawn.Enemy.StageID;
                _stageEnemyCount.TryGetValue(stageId, out var count);
                _stageEnemyCount[stageId] = ++count;
            }
            spawns = DuplicateEnemies(randomizer, area, spawns, rng);

            // Randomize classes
            ChooseClasses(randomizer, spawns, rng);

            // Randomize
            area.EnemySpawns = spawns;
            foreach (var spawn in spawns)
            {
                if (spawn.ChosenClass is EnemyClassDefinition ecd)
                {
                    spawn.ConvertType(area, ecd.Kind);

                    // Reset various fields
                    var e = spawn.Enemy;
                    e.SetFieldValue("_RandamizeMontageID", false);
                    e.SetFieldValue("_RandomMontageID", 0);
                    e.SetFieldValue("_MontageID", 0);
                    e.SetFieldValue("_FixedVoiceID", 0);
                    e.ParasiteKind = 0;
                    e.ForceParasiteAppearance = false;
                    e.ParasiteAppearanceProbability = 0;

                    // Set weapon
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

                    // Set any other custom fields
                    foreach (var fd in ecd.Fields)
                    {
                        var fieldValue = rng.Next(fd.Values);
                        e.SetFieldValue(fd.Name, fieldValue);
                    }

                    // If there are a lot of enemies, plaga seems to randomly crash the game
                    // E.g. village, 360 zealots, 25 plaga will crash
                    if (ecd.Plaga && spawns.Length < 100)
                    {
                        RandomizeParasite(randomizer, e, parasiteRng);
                    }

                    logger.LogLine($"{e.Guid} {e.StageID} {ecd.Name}");
                }
                else
                {
                    logger.LogLine($"{spawn.Enemy.Guid} {spawn.StageID} {spawn.Enemy.Kind}");
                }
            }
        }

        private void RandomizeEnemyHealth(
            ChainsawRandomizer randomizer,
            int chapter,
            ImmutableArray<EnemySpawn> chapterSpawns,
            Rng rng,
            RandomizerLogger logger)
        {
            var progressiveDifficulty = randomizer.GetConfigOption("enemy-health-progressive-difficulty", false);
            var windowStart = 0.0;
            var windowEnd = 1.0;
            if (progressiveDifficulty)
            {
                windowStart = (chapter - 1) / 16.0;
                windowEnd = chapter / 16.0;
            }

            logger.Push($"Chapter {chapter}");
            foreach (var spawn in chapterSpawns)
            {
                RandomizeHealth(randomizer, spawn, windowStart, windowEnd, rng, logger);
            }
            logger.Pop();
        }

        private void RandomizeEnemyDrops(
            ChainsawRandomizer randomizer,
            RandomItemSettings randomItemSettings,
            int chapter,
            ImmutableArray<EnemySpawn> chapterSpawns,
            Rng rng,
            RandomizerLogger logger)
        {
            logger.Push($"Chapter {chapter}");
            var spawnsLeft = chapterSpawns
                .OrderByDescending(x => x.Enemy.Health ?? 0)
                .ToList();

            // Valuables
            var weaponDrops = randomizer.ValuableDistributor
                .GetItems(chapter, ItemDiscovery.Enemy)
                .Shuffle(rng);
            logger.Push("Valuables");
            foreach (var weapon in weaponDrops)
            {
                if (spawnsLeft.Count == 0)
                    break;

                var i = GetRandomHighClassEnemy(spawnsLeft, rng, noHorde: true);
                var spawn = spawnsLeft[i];
                spawnsLeft.RemoveAt(i);
                var item = new Item(weapon.Definition.Id, 1);
                spawn.Enemy.ItemDrop = item;
                logger.LogLine(spawn.Guid, spawn.Enemy.Kind, item);
            }
            logger.Pop();

            // Treasure
            var treasureRatio = randomizer.GetConfigOption<double>("enemy-treasure-drop-ratio");
            var treasureCount = (int)(spawnsLeft.Count * treasureRatio);
            logger.Push("Treasure");
            for (var i = 0; i < treasureCount; i++)
            {
                if (spawnsLeft.Count == 0)
                    break;

                var j = GetRandomHighClassEnemy(spawnsLeft, rng);
                var spawn = spawnsLeft[j];
                spawnsLeft.RemoveAt(j);

                var classNumber = spawn.ChosenClass?.Class ?? 5;
                spawn.Enemy.ItemDrop = randomizer.ItemRandomizer.GetRandomTreasure(rng, classNumber);
                logger.LogLine(spawn.Guid, spawn.Enemy.Kind, spawn.Enemy.ItemDrop!);
            }
            logger.Pop();

            logger.Push("General");
            var itemRandomizer = randomizer.ItemRandomizer;
            foreach (var spawn in spawnsLeft)
            {
                spawn.Enemy.ItemDrop = itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
                logger.LogLine(spawn.Guid, (object?)spawn.Enemy.ItemDrop ?? "(none)");
            }
            logger.Pop();
            logger.Pop();
        }

        private int GetRandomHighClassEnemy(List<EnemySpawn> chapterSpawns, Rng rng, bool noHorde = false)
        {
            var possibleClassNumbers = chapterSpawns
                .Where(x => !(noHorde && x.Horde))
                .Select(GetEnemyClass)
                .Distinct()
                .Order()
                .ToArray();

            if (possibleClassNumbers.Length == 0)
            {
                var index = rng.Next(0, chapterSpawns.Count);
                return index;
            }
            else
            {
                var classNumber = possibleClassNumbers.Last();
                for (var i = 0; i < possibleClassNumbers.Length - 1; i++)
                {
                    if (rng.NextProbability(75))
                    {
                        classNumber = possibleClassNumbers[i];
                        break;
                    }
                }
                var spawn = chapterSpawns
                    .Where(x => GetEnemyClass(x) == classNumber)
                    .Shuffle(rng)
                    .First();
                return chapterSpawns.IndexOf(spawn);
            }
        }

        private static int GetEnemyClass(EnemySpawn spawn)
        {
            var boss = Bosses.GetBoss(spawn.Guid);
            if (boss != null)
                return 1;

            return spawn.ChosenClass?.Class ?? 6;
        }

        private int GetNextContextId()
        {
            return _contextId++;
        }

        private void SetClassPool(ChainsawRandomizer randomizer, Area area, EnemySpawn spawn)
        {
            // Get all allowed enemy classes
            var enemyClasses = _allEnemyClasses;
            var restrictions = area.Definition.Restrictions;
            if (restrictions != null)
            {
                var restrictionBlock = restrictions
                    .FirstOrDefault(x => x.Guids == null || x.Guids.Contains(spawn.OriginalGuid));

                if (restrictionBlock != null)
                {
                    spawn.Horde = restrictionBlock.Horde;
                    spawn.PreventDuplicate = restrictionBlock.PreventDuplicate;

                    var includedClasses = restrictionBlock.Include;
                    if (includedClasses == null)
                    {
                        var excludedClasses = restrictionBlock.Exclude;
                        if (excludedClasses == null)
                        {
                            enemyClasses = ImmutableArray<EnemyClassDefinition>.Empty;
                            spawn.PreventDuplicate = true;
                        }
                        else
                        {
                            enemyClasses = enemyClasses.Where(x => !excludedClasses.Contains(x.Key)).ToImmutableArray();
                        }
                    }
                    else
                    {
                        enemyClasses = enemyClasses.Where(x => includedClasses.Contains(x.Key)).ToImmutableArray();
                    }
                }
            }
            spawn.ClassPool = enemyClasses;

            // Prefer a ranged enemy
            if (IsEnemyRanged(randomizer, spawn.OriginalEnemy))
            {
                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => x.Ranged)
                    .ToImmutableArray();
            }
        }

        private ImmutableArray<EnemySpawn> DuplicateEnemies(ChainsawRandomizer randomizer, Area area, ImmutableArray<EnemySpawn> spawns, Rng rng)
        {
            var multiplier = randomizer.GetConfigOption<double>("enemy-multiplier", 1);
            var maxPerStage = randomizer.GetConfigOption("debug-stage-enemy-limit-default", 25);
            var newList = spawns.ToBuilder();
            foreach (var g in spawns.GroupBy(x => x.StageID))
            {
                var enemyLimit = randomizer.GetConfigOption($"debug-stage-enemy-limit-{g.Key}", 0);
                if (enemyLimit == 0)
                {
                    enemyLimit = maxPerStage;
                }

                var stageSpawns = g.ToArray();
                var newEnemyCount = Math.Min(enemyLimit, stageSpawns.Length * multiplier);
                var delta = (int)Math.Round(newEnemyCount - stageSpawns.Length);
                if (delta != 0)
                {
                    var duplicatableEnemies = stageSpawns
                        .Where(x => !x.PreventDuplicate)
                        .ToArray();
                    if (duplicatableEnemies.Length != 0)
                    {
                        var bag = new EndlessBag<EnemySpawn>(rng, duplicatableEnemies);
                        while (delta > 0)
                        {
                            var enemyToDuplicate = bag.Next();
                            var stageId = enemyToDuplicate.Enemy.StageID;
                            if (!_stageEnemyCount.TryGetValue(stageId, out var currentStageIdCount))
                                _stageEnemyCount[stageId] = 0;

                            if (currentStageIdCount < maxPerStage)
                            {
                                var newEnemy = enemyToDuplicate.Duplicate(GetNextContextId());
                                newList.Add(newEnemy);
                                _stageEnemyCount[stageId]++;
                                delta--;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return newList.ToImmutable();
        }

        private void ChooseClasses(ChainsawRandomizer randomizer, ImmutableArray<EnemySpawn> spawns, Rng rng)
        {
            var enemyVariety = randomizer.GetConfigOption("enemy-variety", 50);

            // Randomize classes from least to most restricted
            var orderedSpawns = spawns
               .OrderByDescending(x => x.ClassPool.Length)
               .ToArray();

            var classList = new HashSet<EnemyClassDefinition>();
            var classQueue = new Queue<EnemyClassDefinition>();
            foreach (var spawn in orderedSpawns)
            {
                if (classList.Count >= enemyVariety)
                {
                    // Variety limit hit, reduce class pool
                    var newClassPool = spawn.ClassPool.Intersect(classList).ToImmutableArray();
                    if (!newClassPool.IsEmpty)
                    {
                        spawn.ClassPool = newClassPool;
                    }
                }

                classQueue.TryDequeue(out var nextClass);
                if (nextClass == null || !spawn.Prefers(nextClass))
                {
                    classQueue.Clear();
                    nextClass = GetRandomEnemyClass(randomizer, spawn, rng);
                    if (nextClass != null)
                    {
                        var count = GetPackCount(randomizer, nextClass, rng);
                        for (var i = 1; i < count; i++)
                        {
                            classQueue.Enqueue(nextClass);
                        }
                    }
                }
                if (nextClass != null)
                {
                    spawn.ChosenClass = nextClass;
                    classList.Add(nextClass);
                }
                else
                {
                }
            }
        }

        private void RandomizeHealth(
            ChainsawRandomizer randomizer,
            EnemySpawn spawn,
            double windowStart,
            double windowEnd,
            Rng rng,
            RandomizerLogger logger)
        {
            var enemy = spawn.Enemy;
            var debugUniqueHp = randomizer.GetConfigOption<bool>("debug-unique-enemy-hp");
            if (debugUniqueHp)
            {
                enemy.Health = _uniqueHp++;
                logger.LogLine(spawn.Guid, spawn.Enemy.Kind, enemy.Health);
            }
            else if (Bosses.GetBoss(enemy.Guid) is Boss boss)
            {
                var randomHealth = randomizer.GetConfigOption<bool>("boss-random-health");
                if (randomHealth)
                {
                    var minHealth = randomizer.GetConfigOption<int>($"boss-health-min-{boss.Key}");
                    var maxHealth = randomizer.GetConfigOption<int>($"boss-health-max-{boss.Key}");
                    minHealth = Math.Clamp(minHealth, 1, 1_000_000);
                    maxHealth = Math.Clamp(maxHealth, minHealth, 1_000_000);
                    enemy.Health = rng.Next(minHealth, maxHealth + 1);
                    logger.LogLine("Boss", spawn.Guid, boss.Name, enemy.Health);
                }
            }
            else if (spawn.ChosenClass is EnemyClassDefinition ecd)
            {
                var randomHealth = randomizer.GetConfigOption<bool>("enemy-random-health");
                if (randomHealth)
                {
                    var minHealth = randomizer.GetConfigOption<int>($"enemy-health-min-{ecd.Key}");
                    var maxHealth = randomizer.GetConfigOption<int>($"enemy-health-max-{ecd.Key}");
                    minHealth = Math.Clamp(minHealth, 1, 100000);
                    maxHealth = Math.Clamp(maxHealth, minHealth, 100000);

                    var range = maxHealth - minHealth;
                    var wMinHealth = (int)Math.Round(minHealth + (range * windowStart));
                    var wMaxHealth = (int)Math.Round(minHealth + (range * windowEnd));

                    enemy.Health = rng.Next(wMinHealth, wMaxHealth + 1);
                    logger.LogLine(spawn.Guid, ecd.Name, enemy.Health);
                }
                else if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    enemy.Health = null;
                    logger.LogLine(spawn.Guid, ecd.Name, "Automatic");
                }
            }
        }

        private void RandomizeParasite(ChainsawRandomizer randomizer, Enemy enemy, Rng rng)
        {
            if (_parasiteRngTable == null)
            {
                var table = rng.CreateProbabilityTable<int>();
                table.Add(0, randomizer.GetConfigOption<double>("parasite-ratio-none"));
                table.Add(1, randomizer.GetConfigOption<double>("parasite-ratio-a"));
                table.Add(2, randomizer.GetConfigOption<double>("parasite-ratio-b"));
                _parasiteRngTable = table;
            }
            if (enemy.ParasiteKind != null)
            {
                var kind = 0;
                if (!_parasiteRngTable.IsEmpty)
                    kind = _parasiteRngTable.Next();
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

        private bool IsEnemyRanged(ChainsawRandomizer randomizer, Enemy enemy)
        {
            var weaponDef = randomizer.EnemyClassFactory.Weapons.FirstOrDefault(x => x.Id == enemy.Weapon);
            if (weaponDef != null)
                return weaponDef.Ranged;
            return false;
        }


        private EnemyClassDefinition? GetRandomEnemyClass(
            ChainsawRandomizer randomizer,
            EnemySpawn spawn,
            Rng rng)
        {
            var classPool = spawn.PreferredClassPool;
            if (classPool.IsDefaultOrEmpty)
                classPool = spawn.ClassPool;
            if (classPool.IsDefaultOrEmpty)
                return null;

            Rng.Table<EnemyClassDefinition>? table = null;
            if (classPool == _allEnemyClasses)
            {
                table = _allEnemyRngTable;
            }

            if (table == null)
            {
                table = rng.CreateProbabilityTable<EnemyClassDefinition>();
                foreach (var enemyClass in classPool)
                {
                    var ratio = randomizer.GetConfigOption<double>($"enemy-ratio-{enemyClass.Key}");
                    if (ratio != 0)
                    {
                        table.Add(enemyClass, ratio);
                    }
                }
                if (classPool == _allEnemyClasses)
                {
                    _allEnemyRngTable = table;
                }
            }

            return table.Next();
        }

        private int GetPackCount(ChainsawRandomizer randomizer, EnemyClassDefinition ecd, Rng rng)
        {
            var maxPackSize = randomizer.GetConfigOption<int>("enemy-pack-max");
            if (maxPackSize == 0)
                maxPackSize = ecd.MaxPack;
            maxPackSize = Math.Clamp(maxPackSize, 1, ecd.MaxPack);
            return rng.Next(1, maxPackSize + 1);
        }

        private static double GetClassRatio(ChainsawRandomizer randomizer, EnemyClassDefinition ecd)
        {
            return randomizer.GetConfigOption<double>($"enemy-ratio-{ecd.Key}");
        }
    }
}
