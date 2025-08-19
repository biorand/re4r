using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Area
    {
        private List<EnemySpawn> _enemySpawns = [];

        public AreaDefinition Definition { get; }
        public EnemyClassFactory EnemyClassFactory { get; }
        public string Path => Definition.Path;
        public string FileName => System.IO.Path.GetFileName(Path);
        public ScnFile ScnFile { get; }

        public Area(AreaDefinition definition, EnemyClassFactory enemyClassFactory, byte[] data)
        {
            Definition = definition;
            EnemyClassFactory = enemyClassFactory;
            ScnFile = ChainsawRandomizerFactory.Default.ReadScnFile(data);
        }

        public void Save(string path)
        {
            ScnFile.SaveAs(path);
        }

        public byte[] SaveData() => ScnFile.ToByteArray();

        public Enemy[] Enemies
        {
            get
            {
                var result = new List<Enemy>();
                var objs = ScnFile.IterAllGameObjects(true).ToArray();
                foreach (var gameObject in objs)
                {
                    var mainComponent = GetMainEnemyComponent(gameObject);
                    if (mainComponent != null)
                    {
                        result.Add(new Enemy(this, gameObject, mainComponent));
                    }
                }
                return result.ToArray();
            }
        }

        public ImmutableArray<EnemySpawn> GetEnemySpawns(ChainsawRandomizer randomizer)
        {
            if (_enemySpawns.Count == 0)
            {
                var enemyClasses = randomizer.EnemyClassFactory.GetClasses(randomizer);
                foreach (var enemy in Enemies)
                {
                    var spawn = new EnemySpawn(this, enemy, enemy);
                    SetClassPool(randomizer, enemyClasses, spawn);
                    _enemySpawns.Add(spawn);
                }
            }
            return _enemySpawns.ToImmutableArray();
        }

        private RszInstance? GetMainEnemyComponent(ScnFile.GameObjectData gameObject)
        {
            return gameObject.Components.FirstOrDefault(x => EnemyClassFactory.FindEnemyKind(x.Name) != null);
        }

        public Enemy ConvertTo(Enemy enemy, EnemyKindDefinition kind)
        {
            var gameObject = enemy.GameObject;
            var oldComponent = enemy.MainComponent;
            if (oldComponent.RszClass.name == kind.ComponentName)
                return enemy;

            ScnFile.AddComponent(gameObject, kind.ComponentName);
            gameObject.Components.Remove(oldComponent);
            var newComponent = gameObject.Components.Last();

            if (gameObject.Prefab != null)
            {
                gameObject.Prefab.Path = kind.Prefab;
            }

            var newEnemy = new Enemy(this, gameObject, newComponent);

            // Copy fields over
            foreach (var f in oldComponent.Fields)
            {
                var oldValue = enemy.GetFieldValue(f.name);
                newEnemy.SetFieldValue(f.name, oldValue!);
            }

            // Clear certain fields
            newEnemy.Weapon = 0;
            newEnemy.SecondaryWeapon = 0;
            newEnemy.MontageId = 0;

            return newEnemy;
        }

        public EnemySpawn Duplicate(EnemySpawn enemy, int contextId)
        {
            var newGameObject = ScnFile.DuplicateGameObject(enemy.Enemy.GameObject);
            var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            var newEnemy = new Enemy(this, newGameObject, newComponent);
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(contextId);
            var newEnemySpawn = new EnemySpawn(this, enemy.Enemy, newEnemy);
            _enemySpawns.Add(newEnemySpawn);
            return newEnemySpawn;
        }

        private void SetClassPool(ChainsawRandomizer randomizer, ImmutableArray<EnemyClassDefinition> enemyClasses, EnemySpawn spawn)
        {
            // Get all allowed enemy classes
            if (!spawn.HasStaticSpawn)
            {
                enemyClasses = enemyClasses.RemoveAll(x => x.Key == "pig");
            }

            var restrictions = Definition.Restrictions;
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
        }

        private static void AvoidClasses(EnemySpawn spawn, string fileName, params string[] avoidClasses)
        {
            if (!spawn.Area.FileName.EndsWith(fileName))
                return;

            spawn.PreferredClassPool = spawn.ClassPool
                .Where(x => !avoidClasses.Contains(x.Key))
                .ToImmutableArray();
        }

        private static bool IsEnemyRanged(ChainsawRandomizer randomizer, Enemy enemy)
        {
            var weaponDef = randomizer.EnemyClassFactory.Weapons.FirstOrDefault(x => x.Id == enemy.Weapon);
            if (weaponDef != null)
                return weaponDef.Ranged;
            return false;
        }
    }
}
