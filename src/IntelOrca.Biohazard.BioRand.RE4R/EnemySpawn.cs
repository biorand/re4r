using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EnemySpawn
    {
        public Area Area { get; }
        public CharacterSpawnController? SpawnController { get; }
        public Enemy OriginalEnemy { get; }
        public Enemy Enemy { get; private set; }
        public bool Horde { get; set; }
        public bool LockWeapon { get; set; }
        public bool PreventDuplicate { get; set; }
        public string? MiniBoss { get; set; }
        public ImmutableArray<EnemyClassDefinition> PreferredClassPool { get; set; } = [];
        public ImmutableArray<EnemyClassDefinition> ClassPool { get; set; } = [];
        public EnemyClassDefinition? ChosenClass { get; set; }

        public EnemySpawn(Area area, CharacterSpawnController? spawnController, Enemy originalEnemy, Enemy enemy)
        {
            Area = area;
            SpawnController = spawnController;
            OriginalEnemy = originalEnemy;
            Enemy = enemy;
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
                LockWeapon = false;
            Enemy = newEnemy;
        }

        public bool Prefers(EnemyClassDefinition ecd)
        {
            if (PreferredClassPool.IsDefaultOrEmpty)
                return ClassPool.Contains(ecd);
            return PreferredClassPool.Contains(ecd);
        }

        public EnemySpawn Duplicate(int contextId)
        {
            var result = Area.Duplicate(this, contextId);
            result.Horde = Horde;
            result.LockWeapon = LockWeapon;
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
            var area = Area;
            var spawn = this;
            var randomizer = area.Randomizer;
            var enemyClasses = randomizer.EnemyClassFactory.GetClasses(randomizer);

            // Get all allowed enemy classes
            if (!spawn.HasStaticSpawn)
            {
                enemyClasses = enemyClasses.RemoveAll(x => x.Key == "pig");
            }

            var restrictions = Area.Definition.Restrictions;
            if (restrictions != null)
            {
                var restrictionBlock = restrictions
                    .FirstOrDefault(x => x.Guids == null || x.Guids.Contains(spawn.OriginalGuid));

                if (restrictionBlock != null)
                {
                    spawn.Horde = restrictionBlock.Horde;
                    spawn.LockWeapon = restrictionBlock.LockWeapon;
                    spawn.PreventDuplicate = restrictionBlock.PreventDuplicate;
                    spawn.MiniBoss = restrictionBlock.MiniBoss;

                    var includedClasses = restrictionBlock.Include;
                    if (includedClasses == null)
                    {
                        var excludedClasses = restrictionBlock.Exclude;
                        if (excludedClasses == null)
                        {
                            if (!spawn.Horde && !spawn.LockWeapon && !spawn.PreventDuplicate)
                            {
                                enemyClasses = ImmutableArray<EnemyClassDefinition>.Empty;
                                spawn.PreventDuplicate = true;
                            }
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

            if (randomizer.GetConfigOption<bool>("enemy-strong-mini-boss") && !string.IsNullOrEmpty(spawn.MiniBoss))
            {
                // Mini boss should be an elite enemy
                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => x.Class <= 4)
                    .ToImmutableArray();
            }
            else if (IsEnemyRanged(randomizer, spawn.OriginalEnemy))
            {
                // Prefer a ranged enemy
                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => x.Ranged)
                    .ToImmutableArray();
            }

            if (randomizer.GetConfigOption<bool>("nice-mendez-hill"))
            {
                // Mendez hill
                AvoidClasses(spawn, "level_loc47_003.scn.20",
                    "chainsaw_mad",
                    "garrador",
                    "krauser_1",
                    "krauser_2",
                    "mendez_2",
                    "pesanta",
                    "super_iron_maiden",
                    "super-colmillos",
                    "u3",
                    "verdugo");

                // Krauser 1 fight
                AvoidClasses(spawn, "level_loc55_004.scn.20",
                    "chainsaw",
                    "chainsaw_mad",
                    "krauser_2",
                    "mendez_2",
                    "pesanta",
                    "super_iron_maiden",
                    "super-colmillos",
                    "u3",
                    "verdugo");
            }

            static void AvoidClasses(EnemySpawn spawn, string fileName, params string[] avoidClasses)
            {
                if (!spawn.Area.FileName.EndsWith(fileName))
                    return;

                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => !avoidClasses.Contains(x.Key))
                    .ToImmutableArray();
            }

            static bool IsEnemyRanged(ChainsawRandomizer randomizer, Enemy enemy)
            {
                var weaponDef = randomizer.EnemyClassFactory.Weapons.FirstOrDefault(x => x.Id == enemy.Weapon);
                if (weaponDef != null)
                    return weaponDef.Ranged;
                return false;
            }
        }
    }
}
