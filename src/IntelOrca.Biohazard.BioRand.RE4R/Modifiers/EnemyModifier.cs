using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyModifier : Modifier
    {
        private int _contextId;
        private int _uniqueHp;
        private Rng.Table<EnemyClassDefinition>? _allEnemyRngTable;
        private Rng.Table<ItemDefinition?>? _itemRngTable;
        private Rng.Table<int>? _parasiteRngTable;
        private Queue<EnemyClassDefinition> _enemyClassQueue = new Queue<EnemyClassDefinition>();
        private ImmutableArray<EnemyClassDefinition> _allEnemyClasses;

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
            if (!randomizer.GetConfigOption<bool>("random-enemies"))
                return;

            _contextId = 5000;
            _uniqueHp = 1;
            _allEnemyClasses = randomizer.EnemyClassFactory.Classes
                .Where(x => GetClassRatio(randomizer, x) > 0)
                .ToImmutableArray();

            var rng = randomizer.CreateRng();
            foreach (var area in randomizer.Areas)
            {
                logger.Push(area.FileName);
                RandomizeArea(randomizer, area, rng, logger);
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
            spawns = DuplicateEnemies(randomizer, spawns, rng);

            // Randomize classes
            ChooseClasses(randomizer, spawns, rng);

            // Randomize
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

                    RandomizeHealth(randomizer, e, ecd, healthRng);
                    RandomizeDrop(randomizer, e, ecd, dropRng);

                    // If there are a lot of enemies, plaga seems to randomly crash the game
                    // E.g. village, 360 zealots, 25 plaga will crash
                    if (ecd.Plaga && spawns.Length < 100)
                    {
                        RandomizeParasite(randomizer, e, parasiteRng);
                    }

                    logger.LogLine($"{e.Guid} {ecd.Name} Health = {e.Health}");
                }
                else
                {
                    logger.LogLine($"{spawn.Enemy.Guid} {spawn.Enemy.Kind} Health = {spawn.Enemy.Health}");
                }
            }
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
                var excludedClasses = restrictionBlock?.Exclude;
                if (restrictionBlock != null)
                {
                    spawn.PreventDuplicate = restrictionBlock.PreventDuplicate;
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

        private ImmutableArray<EnemySpawn> DuplicateEnemies(ChainsawRandomizer randomizer, ImmutableArray<EnemySpawn> spawns, Rng rng)
        {
            var multiplier = randomizer.GetConfigOption<double>("enemy-multiplier", 1);
            var newEnemyCount = Math.Min(spawns.Length * multiplier, 200);
            var delta = (int)Math.Round(newEnemyCount - spawns.Length);
            if (delta != 0)
            {
                var newList = spawns.ToBuilder();
                var duplicatableEnemies = spawns
                    .Where(x => !x.PreventDuplicate)
                    .ToArray();

                if (duplicatableEnemies.Length != 0)
                {
                    var bag = new EndlessBag<EnemySpawn>(rng, duplicatableEnemies);
                    var enemiesToCopy = bag.Next(delta);
                    foreach (var spawn in enemiesToCopy)
                    {
                        var newEnemy = spawn.Duplicate(GetNextContextId());
                        newList.Add(newEnemy);
                    }
                    return newList.ToImmutable();
                }
            }
            return spawns;
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

        private void RandomizeHealth(ChainsawRandomizer randomizer, Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var debugUniqueHp = randomizer.GetConfigOption<bool>("debug-unique-enemy-hp");
            if (debugUniqueHp)
            {
                enemy.Health = _uniqueHp++;
            }
            else
            {
                var randomHealth = randomizer.GetConfigOption<bool>("enemy-custom-health");
                if (randomHealth)
                {
                    var minHealth = randomizer.GetConfigOption<int>($"enemy-health-min-{ecd.Key}");
                    var maxHealth = randomizer.GetConfigOption<int>($"enemy-health-max-{ecd.Key}");
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

        private void RandomizeDrop(ChainsawRandomizer randomizer, Enemy enemy, EnemyClassDefinition ecd, Rng rng)
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
                enemy.ItemDrop = GetRandomValuableItem(randomizer, enemy, ecd, rng);
            else
                enemy.ItemDrop = GetRandomItem(randomizer, enemy, rng);
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
                maxPackSize = ecd.Class;
            maxPackSize = Math.Clamp(maxPackSize, 1, ecd.Class);
            return rng.Next(1, maxPackSize + 1);
        }

        private Item? GetRandomItem(ChainsawRandomizer randomizer, Enemy enemy, Rng rng)
        {
            if (_itemRngTable == null)
            {
                var table = rng.CreateProbabilityTable<ItemDefinition?>();

                var repo = ItemDefinitionRepository.Default;
                var kindRatios = new List<(string, double)>();
                foreach (var itemKind in repo.Kinds)
                {
                    var ratio = randomizer.GetConfigOption<double>($"drop-ratio-{itemKind}");
                    if (ratio != 0)
                    {
                        kindRatios.Add((itemKind, ratio));
                    }
                }

                var total = kindRatios.Select(x => x.Item2).Sum();

                var autoRatio = randomizer.GetConfigOption<double>("drop-ratio-automatic");
                if (autoRatio != 0)
                {
                    total += autoRatio;
                    table.Add(ItemDefinition.Automatic, autoRatio / total);
                }

                var noneRatio = randomizer.GetConfigOption<double>("drop-ratio-none");
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
                var amount = GetRandomItemQuantity(randomizer, def, rng);
                var item = new Item(def.Id, amount);
                return item;
            }
        }

        private int GetRandomItemQuantity(ChainsawRandomizer randomizer, ItemDefinition def, Rng rng)
        {
            var amount = 1;
            if (def.Kind == ItemKinds.Money)
            {
                var multiplier = randomizer.GetConfigOption<double>("money-quantity");
                amount = Math.Max(1, (int)(rng.Next(100, 2000) * multiplier));
            }
            else if (def.Kind == ItemKinds.Ammo)
            {
                var multiplier = randomizer.GetConfigOption<double>("ammo-quantity");
                amount = Math.Max(1, (int)(rng.Next(10, 50) * multiplier));
            }
            else if (def.Kind == ItemKinds.Gunpowder)
            {
                amount = Math.Max(1, 10);
            }
            return amount;
        }

        private Item? GetRandomValuableItem(ChainsawRandomizer randomizer, Enemy enemy, EnemyClassDefinition ecd, Rng rng)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var kinds = itemRepo.Kinds
                .Where(x => randomizer.GetConfigOption<bool>($"drop-valuable-{x}"))
                .ToArray();

            if (kinds.Length == 0)
                return GetRandomItem(randomizer, enemy, rng);

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

        private static double GetClassRatio(ChainsawRandomizer randomizer, EnemyClassDefinition ecd)
        {
            return randomizer.GetConfigOption<double>($"enemy-ratio-{ecd.Key}");
        }

        private class EnemySpawn
        {
            public Area Area { get; }
            public Enemy OriginalEnemy { get; }
            public Enemy Enemy { get; private set; }
            public bool PreventDuplicate { get; set; }
            public ImmutableArray<EnemyClassDefinition> PreferredClassPool { get; set; } = [];
            public ImmutableArray<EnemyClassDefinition> ClassPool { get; set; } = [];
            public EnemyClassDefinition? ChosenClass { get; set; }

            public EnemySpawn(Area area, Enemy originalEnemy, Enemy enemy)
            {
                Area = area;
                OriginalEnemy = originalEnemy;
                Enemy = enemy;
            }

            public Guid OriginalGuid => OriginalEnemy.Guid;

            public void ConvertType(Area area, EnemyKindDefinition kind)
            {
                Enemy = area.ConvertTo(Enemy, kind.ComponentName);
            }

            public bool Prefers(EnemyClassDefinition ecd)
            {
                if (PreferredClassPool.IsDefaultOrEmpty)
                    return ClassPool.Contains(ecd);
                return PreferredClassPool.Contains(ecd);
            }

            public EnemySpawn Duplicate(int contextId)
            {
                var newEnemy = Area.Duplicate(Enemy, contextId);
                var result = new EnemySpawn(Area, Enemy, newEnemy);
                result.ClassPool = ClassPool;
                result.PreferredClassPool = PreferredClassPool;
                result.ChosenClass = ChosenClass;
                return result;
            }

            public override string ToString()
            {
                return $"{Enemy.Guid} ({Enemy.Kind})";
            }
        }
    }
}
