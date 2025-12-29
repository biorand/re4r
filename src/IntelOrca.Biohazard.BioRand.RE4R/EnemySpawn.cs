using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EnemySpawn
    {
        public Area Area { get; }
        public CharacterSpawnController? SpawnController { get; }
        public Enemy OriginalEnemy { get; }
        public Enemy Enemy { get; private set; }
        public ImmutableArray<EnemyClassDefinition> PreferredClassPool { get; set; } = [];
        public ImmutableArray<EnemyClassDefinition> ClassPool { get; set; } = [];
        public EnemyClassDefinition? ChosenClass { get; set; }
        public EnemyPlacement EnemyPlacement { get; }

        public EnemySpawn(Area area, CharacterSpawnController? spawnController, Enemy originalEnemy, Enemy enemy, EnemyPlacement enemyPlacement)
        {
            Area = area;
            SpawnController = spawnController;
            OriginalEnemy = originalEnemy;
            Enemy = enemy;
            EnemyPlacement = enemyPlacement;
        }

        public RszGameObject Apply()
        {
            return Enemy.Apply();
        }

        public Guid OriginalGuid => OriginalEnemy.Guid;
        public Guid Guid => Enemy.Guid;
        public int StageID => Enemy.StageID;

        public void ConvertType(EnemyKindDefinition kind)
        {
            var newEnemy = Area.ConvertTo(Enemy, kind);
            if (newEnemy != Enemy)
                EnemyPlacement.Tags = EnemyPlacement.Tags.Remove(EnemyTags.LockWeapon);
            Enemy = newEnemy;
        }

        public bool Prefers(EnemyClassDefinition ecd)
        {
            if (PreferredClassPool.IsDefaultOrEmpty)
                return ClassPool.Contains(ecd);
            return PreferredClassPool.Contains(ecd);
        }

        public EnemySpawn Duplicate(chainsaw.ContextID contextId)
        {
            var result = Area.Duplicate(this, contextId);
            result.ClassPool = ClassPool;
            result.PreferredClassPool = PreferredClassPool;
            result.ChosenClass = ChosenClass;
            return result;
        }

        public bool IsOrphan => SpawnController == null;
        public bool HasStaticSpawn =>
            SpawnController != null &&
            SpawnController.Kind == SpawnControllerKind.Standard &&
            SpawnController.SpawnCondition._CheckFlags.Count == 0;
        public bool HasSimpleController => SpawnController != null && SpawnController.Kind == SpawnControllerKind.Standard;

        public bool HasKeyItem
        {
            get
            {
                if (Guid == OriginalGuid && Enemy.ItemDrop is Item item)
                {
                    var itemRepo = ItemDefinitionRepository.Default;
                    var itemDef = itemRepo.Find(item.Id);
                    if (itemDef != null && itemDef.Kind == ItemKinds.Key)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override string ToString()
        {
            return $"{Enemy.Guid} ({Enemy.Kind})";
        }

        public void SetClassPool()
        {
            var spawn = this;
            var randomizer = Area.Randomizer;
            var allEnemyClasses = randomizer.EnemyClassFactory.GetClasses(randomizer);
            var enemyClasses = allEnemyClasses;
            var keyToEnemyClass = enemyClasses.ToDictionary(x => x.Key);

            IncludeExclude(essential: true);

            if (spawn.EnemyPlacement.HasTag(EnemyTags.Guardian))
            {
                enemyClasses = enemyClasses
                    .Where(x => !x.Groups.Contains("noguardian"))
                    .ToImmutableArray();
            }

            // Mini bosses should not be invincible
            if (!string.IsNullOrEmpty(spawn.EnemyPlacement.MiniBoss))
            {
                enemyClasses = enemyClasses
                    .Where(x => !x.Groups.Contains("invincible"))
                    .ToImmutableArray();
            }

            // Pigs crash the game if a conditional spawn
            if (!spawn.HasStaticSpawn)
            {
                enemyClasses = enemyClasses.RemoveAll(x => x.Key == "pig");
            }

            // Set possible classes
            spawn.ClassPool = enemyClasses;

            // Now set preferred classes
            if (randomizer.GetConfigOption<bool>("balanced-enemies"))
            {
                IncludeExclude(essential: false);
            }
            if (spawn.EnemyPlacement.HasTag(EnemyTags.NoToxic))
            {
                if (randomizer.GetConfigOption<bool>("nice-mendez-hill"))
                {
                    enemyClasses = enemyClasses
                        .Where(x => !x.Groups.Contains("toxic"))
                        .ToImmutableArray();
                }
            }
            if (spawn.EnemyPlacement.HasTag(EnemyTags.AshleySafe))
            {
                if (randomizer.GetConfigOption<bool>("ashley-safe-enemies"))
                {
                    enemyClasses = enemyClasses
                        .Where(x => !x.Groups.Contains("ashleyunsafe"))
                        .ToImmutableArray();
                }
            }
            if (randomizer.GetConfigOption<bool>("enemy-strong-mini-boss") && !string.IsNullOrEmpty(spawn.EnemyPlacement.MiniBoss))
            {
                // Mini boss should be an elite enemy
                enemyClasses = enemyClasses
                    .Where(x => x.Groups.Contains("strongminiboss"))
                    .ToImmutableArray();
            }
            else if (spawn.EnemyPlacement.HasTag(EnemyTags.Ranged))
            {
                // Prefer a ranged enemy
                enemyClasses = enemyClasses
                    .Where(x => x.Ranged)
                    .ToImmutableArray();
            }
            spawn.PreferredClassPool = enemyClasses;

            void IncludeExclude(bool essential)
            {
                var include = MapClasses1(spawn.EnemyPlacement.Include, essential);
                var exclude = MapClasses1(spawn.EnemyPlacement.Exclude, essential);
                if (!include.IsDefaultOrEmpty)
                {
                    enemyClasses = enemyClasses
                        .Intersect(include)
                        .ToImmutableArray();
                }
                else if (!exclude.IsDefaultOrEmpty)
                {
                    enemyClasses = enemyClasses
                        .Except(exclude)
                        .ToImmutableArray();
                }
            }

            ImmutableArray<EnemyClassDefinition> MapClasses1(ImmutableArray<string> list, bool essential)
            {
                return essential
                    ? list
                        .Where(x => x.EndsWith('*'))
                        .Select(x => x[..^1])
                        .SelectMany(MapClasses2)
                        .ToImmutableArray()
                    : list
                        .Where(x => !x.EndsWith('*'))
                        .SelectMany(MapClasses2)
                        .ToImmutableArray();

                EnemyClassDefinition[] MapClasses2(string className)
                {
                    var result = allEnemyClasses
                        .Where(x => x.Groups.Contains(className))
                        .Select(x => x.Key)
                        .Append(className)
                        .Select(x => keyToEnemyClass.GetValueOrDefault(x)!)
                        .Where(x => x != null)
                        .ToArray();
                    return result;
                }
            }
        }
    }
}
