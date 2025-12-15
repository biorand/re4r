using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class EnemyModifier : Modifier
    {
        private int _uniqueHp;
        private Rng.Table<EnemyClassDefinition>? _allEnemyRngTable;
        private Rng.Table<int>? _parasiteRngTable;
        private ImmutableArray<EnemyClassDefinition> _allEnemyClasses;

        private Dictionary<int, int> _stageEnemyCount = new();

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            foreach (var area in randomizer.AreaService.Areas)
            {
                var enemies = area.Enemies.ToArray();
                if (enemies.Length == 0)
                    continue;

                logger.Push(area.FileName);
                foreach (var enemy in enemies)
                {
                    LogEnemy(enemy.Enemy, logger);
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
                enemy.Transform.Position.X.ToString("0.0"),
                enemy.Transform.Position.Y.ToString("0.0"),
                enemy.Transform.Position.Z.ToString("0.0"),
                enemy.Kind.Key,
                enemy.MontageId,
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
            var ammoOnlyAvailableWeapons = randomizer.GetConfigOption("enemy-drop-ammo-only-available-weapons", true);

            _uniqueHp = 1;
            _allEnemyClasses = randomizer.EnemyClassFactory.GetClasses(randomizer);

            var rng = randomizer.GetRng("modifier/enemy");
            var areaByChapter = randomizer.AreaService.Areas
                .Where(x => x.Definition.Kind == AreaKind.General)
                .GroupBy(x => x.Definition.Chapter);

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

            logger.Push("Randomizing health");
            foreach (var group in areaByChapter)
            {
                var chapter = group.Key;
                var enemies = group
                    .SelectMany(x => x.Enemies)
                    .ToImmutableArray();
                RandomizeEnemyHealth(randomizer, chapter, enemies, rng, logger);
            }
            logger.Pop();

            if (randomizer.GetConfigOption<bool>("random-enemy-drops"))
            {
                logger.Push("Randomizing drops");
                foreach (var group in areaByChapter)
                {
                    var chapter = group.Key;
                    var enemies = group
                        .SelectMany(x => x.Enemies)
                        .Where(x => x.EnemyPlacement.ItemId == 0)
                        .Where(x => !x.Enemy.Kind.NoItemDrop)
                        .Where(x => !x.HasKeyItem)
                        .Where(x => x.OriginalEnemy.Kind.Key != "mendez_2") // Mendez (phase 1)
                        .Where(x => x.OriginalEnemy.Kind.Key != "mendez_3") // Mendez (phase 2)
                        .Where(x => x.OriginalEnemy.Kind.Key != "krauser_2") // Krauser
                        .Where(x => x.OriginalEnemy.Kind.Key != "pesanta_phantom") // Pesanta (phantom)
                        .Where(x => x.OriginalEnemy.Kind.Key != "pesanta") // Pesanta
                        .ToImmutableArray();

                    if (ammoOnlyAvailableWeapons)
                    {
                        randomItemSettings.ValidateDropKind = (drop) =>
                        {
                            var ammoType = DropKinds.GetAmmoType(drop);
                            return ammoType == null || randomizer.ValuableDistributor.IsAmmoAvailableYet(ammoType.Value, chapter);
                        };
                    }
                    RandomizeEnemyDrops(randomizer, randomItemSettings, chapter, enemies, rng, logger);
                }
                logger.Pop();
            }

            var enemyScaleProbability = randomizer.GetConfigOption<double>("enemy-scale-probability", 0);
            if (enemyScaleProbability > 0)
            {
                logger.Push("Randomizing scales");
                var spawns = areaByChapter
                    .SelectMany(x => x)
                    .SelectMany(x => x.Enemies)
                    .ToImmutableArray();
                if (enemyScaleProbability < 1)
                {
                    var count = (int)(spawns.Length * enemyScaleProbability);
                    spawns = spawns.Shuffle(rng).Take(count).ToImmutableArray();
                }
                RandomizeEnemyScales(randomizer, spawns, rng, logger);
                logger.Pop();
            }
        }

        private void RandomizeArea(ChainsawRandomizer randomizer, Area area, Rng rng, RandomizerLogger logger)
        {
            var healthRng = rng.NextFork();
            var dropRng = rng.NextFork();
            var parasiteRng = rng.NextFork();

            // Get all the enemy spawns for this area
            var spawns = area.Enemies.ToImmutableArray();

            // Randomize classes
            ChooseClasses(randomizer, spawns, rng);

            if (randomizer.GetConfigOption<bool>("enemy-strong-mini-boss"))
            {
                var miniBossGroups = spawns
                    .Where(x => !string.IsNullOrEmpty(x.EnemyPlacement.MiniBoss))
                    .GroupBy(x => x.EnemyPlacement.MiniBoss);
                foreach (var g in miniBossGroups)
                {
                    var first = g.First();
                    foreach (var other in g.Skip(1))
                    {
                        other.ChosenClass = first.ChosenClass;
                    }
                }
            }

            // Randomize
            foreach (var spawn in spawns)
            {
                if (spawn.EnemyPlacement.HasTag(EnemyTags.Preserve))
                    continue;

                if (spawn.ChosenClass is EnemyClassDefinition ecd)
                {
                    // Determine weapon
                    WeaponChoice? weaponChoice = null;
                    if (!spawn.EnemyPlacement.HasTag(EnemyTags.LockWeapon) && ecd.Weapon.Length != 0)
                    {
                        weaponChoice = rng.Next(ecd.Weapon);
                    }

                    spawn.ConvertType(weaponChoice?.Kind ?? ecd.Kind);

                    // Reset various fields
                    var e = spawn.Enemy;
                    e.SetFieldValue("_RandamizeMontageID", false);
                    e.SetFieldValue("_RandomMontageID", 0);
                    e.SetFieldValue("_MontageID", 0);
                    e.SetFieldValue("_FixedVoiceID", 0);
                    e.ParasiteKind = 0;
                    e.ForceParasiteAppearance = false;
                    e.ParasiteAppearanceProbability = 0;

                    // Fix first plaga mandibula zealot which is invincible
                    // unless role is reset
                    if (e.RolePatternHash == 3243946825)
                        e.RolePatternHash = 1615772969;

                    // Update position (if vanilla enemy)
                    if (!spawn.EnemyPlacement.IsExtra)
                    {
                        var transform = e.Transform;
                        if (!spawn.EnemyPlacement.HasEmptyPosition)
                        {
                            transform.Position = spawn.EnemyPlacement.Position;
                        }
                        else
                        {
                            // Reset orientation (when converting sideways novistadors)
                            var euler = transform.Rotation.ToEuler();
                            if (MathF.Round(euler.Pitch) != 0 || MathF.Round(euler.Roll) != 0)
                            {
                                transform.Rotation = new EulerAngles(euler.Yaw, 0, 0).ToQuaternion();
                            }
                        }
                        e.Transform = transform;
                    }

                    // Set weapon
                    if (!spawn.EnemyPlacement.HasTag(EnemyTags.LockWeapon))
                    {
                        if (weaponChoice == null)
                        {
                            e.Weapon = 0;
                            e.SecondaryWeapon = 0;
                        }
                        else
                        {
                            e.Weapon = weaponChoice.Primary?.Id ?? 0;
                            e.SecondaryWeapon = weaponChoice.Secondary?.Id ?? 0;
                        }
                    }

                    // Set any other custom fields
                    foreach (var fd in ecd.Fields)
                    {
                        var fieldValue = rng.Next(fd.Values);
                        e.SetFieldValue(fd.Name, fieldValue);
                    }

                    // Arana latch
                    if (ecd.Key == "arana")
                    {
                        var latchProbability = area.Randomizer.GetConfigOption<double>("arana-latch-probability");
                        var shouldLatch = rng.NextDouble() <= latchProbability;
                        e.SetFieldValue("_EnableGannardParent", shouldLatch);
                    }

                    if (ecd.Plaga)
                    {
                        RandomizeParasite(randomizer, spawn, parasiteRng);
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
                var numChapters = ChapterId.GetCount(randomizer.Campaign);
                windowStart = (chapter - 1) / (double)numChapters;
                windowEnd = chapter / (double)numChapters;
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
            var goldBarOnly = randomizer.HasSpecialTouch("goldbar");

            logger.Push($"Chapter {chapter}");
            var spawnsLeft = chapterSpawns
                .OrderByDescending(x => x.Enemy.Health ?? 0)
                .ToList();

            // Vipers always have viper drop
            for (var i = 0; i < spawnsLeft.Count; i++)
            {
                if (spawnsLeft[i].Enemy.Kind.Key == "viper")
                {
                    spawnsLeft[i].Enemy.ItemDrop = new Item(ItemIds.Viper, 1);
                    spawnsLeft.RemoveAt(i);
                    i--;
                }
            }

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

                var classNumber = spawn.ChosenClass?.Class ?? 1;
                spawn.Enemy.ItemDrop = goldBarOnly
                    ? new Item(120840000, 1)
                    : randomizer.ItemRandomizer.GetRandomTreasure(rng, classNumber);
                logger.LogLine(spawn.Guid, spawn.Enemy.Kind, spawn.Enemy.ItemDrop!);
            }
            logger.Pop();

            logger.Push("General");
            var itemRandomizer = randomizer.ItemRandomizer;
            foreach (var spawn in spawnsLeft)
            {
                spawn.Enemy.ItemDrop = goldBarOnly
                    ? new Item(120840000, 1)
                    : itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
                logger.LogLine(spawn.Guid, (object?)spawn.Enemy.ItemDrop ?? "(none)");
            }
            logger.Pop();
            logger.Pop();
        }

        private void RandomizeEnemyScales(
            ChainsawRandomizer randomizer,
            ImmutableArray<EnemySpawn> spawns,
            Rng rng,
            RandomizerLogger logger)
        {
            var min = Math.Clamp(randomizer.GetConfigOption("enemy-scale-min", 0.25f), 0.1f, 10.0f);
            var max = Math.Clamp(randomizer.GetConfigOption("enemy-scale-max", 2.00f), 0.1f, 10.0f);
            foreach (var spawn in spawns)
            {
                if (Bosses.IsBoss(spawn.Guid))
                    continue;

                var scale = MathF.Round(rng.NextFloat(min, max) * 100) / 100;
                spawn.Enemy.SetFieldValue("_BodyScale", new
                {
                    _IsFixedScale = true,
                    _FixedScale = scale,
                    _RandomScaleSeed = 0,
                    _RandomCreateContextID = 0
                });
            }
        }

        private int GetRandomHighClassEnemy(List<EnemySpawn> chapterSpawns, Rng rng, bool noHorde = false)
        {
            var possibleClassNumbers = chapterSpawns
                .Where(x => !(noHorde && x.EnemyPlacement.HasTag(EnemyTags.Horde)))
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
            if (!string.IsNullOrEmpty(spawn.EnemyPlacement.MiniBoss))
                return 2;

            return spawn.ChosenClass?.Class ?? 6;
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

                    if (boss.Key == "krauser-1")
                    {
                        // Krauser seems to have unlimited HP if too low
                        minHealth = Math.Max(minHealth, 10);
                    }

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

                    if (!string.IsNullOrEmpty(spawn.EnemyPlacement.MiniBoss))
                    {
                        // Mini bosses get 2x chapter health
                        enemy.Health = wMaxHealth * 2;
                    }
                    else
                    {
                        enemy.Health = rng.Next(wMinHealth, wMaxHealth + 1);
                    }

                    logger.LogLine(spawn.Guid, ecd.Name, enemy.Health);
                }
                else if (randomizer.GetConfigOption<bool>("random-enemies"))
                {
                    enemy.Health = null;
                    logger.LogLine(spawn.Guid, ecd.Name, "Automatic");
                }
            }
        }

        private void RandomizeParasite(ChainsawRandomizer randomizer, EnemySpawn spawn, Rng rng)
        {
            var enemy = spawn.Enemy;
            if (_parasiteRngTable == null)
            {
                var table = rng.CreateProbabilityTable<int>();
                table.Add(0, randomizer.GetConfigOption<double>("parasite-ratio-none"));
                table.Add(1, randomizer.GetConfigOption<double>("parasite-ratio-a"));
                table.Add(2, randomizer.GetConfigOption<double>("parasite-ratio-b"));
                table.Add(3, randomizer.GetConfigOption<double>("parasite-ratio-c"));
                _parasiteRngTable = table;
            }
            if (enemy.ParasiteKind != null)
            {
                var kind = 0;
                if (!spawn.EnemyPlacement.HasTag(EnemyTags.NoPlaga) && !_parasiteRngTable.IsEmpty)
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
                    if (kind == 3)
                    {
                        var aranaSpawn = CreateOrphanArana(spawn.Area, $"PlagaC for {enemy.GameObject.Name}", enemy.StageID, rng);
                        enemy.ParasiteSpawn = aranaSpawn.Guid;
                    }
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
                    if (ratio != 0 && IsEnemySupported(enemyClass))
                    {
                        table.Add(enemyClass, ratio);
                    }
                }
                if (classPool == _allEnemyClasses)
                {
                    _allEnemyRngTable = table;
                }
            }

            if (table.IsEmpty)
                return null;

            return table.Next();

            bool IsEnemySupported(EnemyClassDefinition ecd)
            {
                var notSupported = randomizer.Campaign == Campaign.Leon
                    ? new[] {
                        "sadler_human",
                    }
                    : new[] {
                        "colmillos",
                        "krauser_2",
                        "mendez_2",
                        "verdugo" };
                return !notSupported.Contains(ecd.Kind.Key);
            }
        }

        private int GetPackCount(ChainsawRandomizer randomizer, EnemyClassDefinition ecd, Rng rng)
        {
            var maxPackSize = randomizer.GetConfigOption<int>("enemy-pack-max");
            if (maxPackSize == 0)
                maxPackSize = ecd.MaxPack;
            maxPackSize = Math.Clamp(maxPackSize, 1, ecd.MaxPack);
            return rng.Next(1, maxPackSize + 1);
        }

        private static EnemySpawn CreateOrphanArana(Area area, string name, int stageId, Rng rng)
        {
            var latchProbability = area.Randomizer.GetConfigOption<double>("arana-latch-probability");
            var shouldLatch = rng.NextDouble() <= latchProbability;

            var contextId = area.Randomizer.FlagService.AllocateContextId(0, 0);
            var transform = RszFactory.CreateTransform();
            var spawnParam = FileRepository.RszRepository.Create("chainsaw.Ch1e0z0SpawnParam")
                .Set("Enabled", true)
                .Set("_StageID", stageId)
                .Set("_SpawmRadius", 20.0f)
                .Set("_ContextID", contextId)
                .Set("_RoleType", 3)
                .Set("_IsEnableUnreachable", true)
                .Set("_RolePatternHash", 3152132219U)
                .Set("_SegmentID", 1)
                .Set("_FirstForceMoveEndTime", -1.0f)
                .Set("_FirstForceMoveEndRadius", 0.2f)
                .Set("_PreFirstForceMovePatternHash", 3152132219U)
                .Set("_RoleActionEndOnDamage", true)
                .Set("_CriticalResistRate", 0.25f)
                .Set("_MontageID", 1106175613U)
                .Set("_EnableGannardParent", shouldLatch);
            var gameObject = RszFactory.CreateGameObject(name, "_Chainsaw/AppSystem/Prefab/ch1e0z0SpawnParam.pfb", [transform, spawnParam]);
            return area.AddOrphanEnemy(gameObject);
        }
    }
}
