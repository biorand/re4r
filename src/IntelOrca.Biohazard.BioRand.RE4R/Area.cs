using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Area
    {
        private List<EnemySpawn> _enemySpawns = [];

        public ChainsawRandomizer Randomizer { get; }
        public AreaDefinition Definition { get; }
        public EnemyClassFactory EnemyClassFactory { get; }
        public string Path => Definition.Path;
        public string FileName => System.IO.Path.GetFileName(Path);
        public ScnFile.Builder ScnFile { get; }
        public RszScene Scene
        {
            get => ScnFile.Scene;
            set => ScnFile.Scene = value;
        }

        public Area(ChainsawRandomizer randomizer, AreaDefinition definition, EnemyClassFactory enemyClassFactory, ScnFile scn)
        {
            Randomizer = randomizer;
            Definition = definition;
            EnemyClassFactory = enemyClassFactory;
            ScnFile = scn.ToBuilder(FileRepository.RszRepository);
        }

        public ScnFile Apply()
        {
            ApplyEnemies();
            return ScnFile.AddMissingResources().Build();
        }

        private void ApplyEnemies()
        {
            var enemySpawns = GetEnemySpawns();
            foreach (var spawn in enemySpawns)
            {
                spawn.Enemy.ApplyComponent();
                Scene = Scene.UpdateGameObject(spawn.Enemy.GameObject);
            }
        }

        public Enemy[] Enemies
        {
            get
            {
                var result = new List<Enemy>();
                Scene.VisitGameObjects(gameObject =>
                {
                    var spawnController = GetSpawnController(gameObject);
                    if (spawnController == null)
                        return;

                    foreach (var child in gameObject.Children)
                    {
                        var mainComponent = GetMainEnemyComponent(child);
                        if (mainComponent != null)
                        {
                            result.Add(new Enemy(this, gameObject, child, mainComponent));
                        }
                    }
                });
                return result.ToArray();
            }
        }

        public ImmutableArray<EnemySpawn> GetEnemySpawns()
        {
            if (_enemySpawns.Count == 0)
            {
                var enemyClasses = Randomizer.EnemyClassFactory.GetClasses(Randomizer);
                foreach (var enemy in Enemies)
                {
                    var spawn = new EnemySpawn(this, enemy, enemy);
                    SetClassPool(enemyClasses, spawn);
                    _enemySpawns.Add(spawn);
                }
            }
            return _enemySpawns.ToImmutableArray();
        }

        private RszStructNode? GetMainEnemyComponent(RszGameObject gameObject)
        {
            return gameObject.Components.FirstOrDefault(x => EnemyClassFactory.FindEnemyKind(x.Type.Name) != null);
        }

        public Enemy ConvertTo(Enemy enemy, EnemyKindDefinition kind)
        {
            var gameObject = enemy.GameObject;
            var oldComponent = enemy.MainComponent;
            if (oldComponent.Type.Name == kind.ComponentName)
                return enemy;

            var newComponent = FileRepository.RszRepository.Create(kind.ComponentName);

            var components = gameObject.Components.ToBuilder();
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i].Type == oldComponent.Type)
                {
                    components[i] = newComponent;
                    break;
                }
            }
            gameObject = gameObject
                .WithPrefab(kind.Prefab)
                .WithComponents(components.ToImmutable());

            var newEnemy = new Enemy(this, enemy.SpawnController, gameObject, newComponent);

            // Copy fields over
            foreach (var f in oldComponent.Type.Fields)
            {
                var oldValue = enemy.GetFieldValue(f.Name);
                newEnemy.SetFieldValue(f.Name, oldValue!);
            }

            // Clear certain fields
            newEnemy.Weapon = 0;
            newEnemy.SecondaryWeapon = 0;
            newEnemy.MontageId = 0;

            return newEnemy;
        }

        public EnemySpawn Duplicate(EnemySpawn enemy, int contextId)
        {
            var newGameObject = enemy.Enemy.GameObject.Clone();
            var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            var newEnemy = new Enemy(this, enemy.Enemy.SpawnController, newGameObject, newComponent);
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(contextId);
            var newEnemySpawn = new EnemySpawn(this, enemy.Enemy, newEnemy);
            _enemySpawns.Add(newEnemySpawn);
            return newEnemySpawn;
        }

        private void SetClassPool(ImmutableArray<EnemyClassDefinition> enemyClasses, EnemySpawn spawn)
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

            if (Randomizer.GetConfigOption<bool>("enemy-strong-mini-boss") && !string.IsNullOrEmpty(spawn.MiniBoss))
            {
                // Mini boss should be an elite enemy
                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => x.Class <= 4)
                    .ToImmutableArray();
            }
            else if (IsEnemyRanged(Randomizer, spawn.OriginalEnemy))
            {
                // Prefer a ranged enemy
                spawn.PreferredClassPool = spawn.ClassPool
                    .Where(x => x.Ranged)
                    .ToImmutableArray();
            }

            if (Randomizer.GetConfigOption<bool>("nice-mendez-hill"))
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

        private static RszStructNode? GetSpawnController(RszGameObject gameObject)
        {
            foreach (var component in gameObject.Components)
            {
                if (component.Type.Name == "chainsaw.CharacterSpawnController" ||
                    component.Type.Name == "chainsaw.CharacterSpawnPointController" ||
                    component.Type.Name == "chainsaw.CharacterSpawnWaveController")
                {
                    return component;
                }
            }
            return null;
        }
    }
}
