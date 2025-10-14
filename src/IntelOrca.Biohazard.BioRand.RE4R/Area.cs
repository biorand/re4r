using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Models;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Area
    {
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

        public RszFolder BioRandFolder
        {
            get
            {
                var biorandFolder = Scene.Children.OfType<RszFolder>().FirstOrDefault(x => x.Name == "BioRand");
                if (biorandFolder == null)
                {
                    biorandFolder = new RszFolder(FileRepository.RszRepository
                        .Create("via.Folder")
                            .Set("Name", "BioRand")
                            .Set("Update", true)
                            .Set("Draw", true)
                            .Set("Startup", true), []);
                    Scene = Scene.Add(biorandFolder);
                }
                return biorandFolder;
            }
            set
            {
                Scene = Scene.WithChildren(
                    Scene.Children.Replace(BioRandFolder, value));
            }
        }

        public ImmutableArray<CharacterSpawnController> SpawnControllers { get; private set; }
        public ImmutableArray<EnemySpawn> OrphanEnemies { get; private set; }
        public IEnumerable<EnemySpawn> Enemies => SpawnControllers.SelectMany(x => x.Enemies).Concat(OrphanEnemies);

        public Area(ChainsawRandomizer randomizer, AreaDefinition definition, EnemyClassFactory enemyClassFactory, ScnFile scn)
        {
            Randomizer = randomizer;
            Definition = definition;
            EnemyClassFactory = enemyClassFactory;
            ScnFile = scn.ToBuilder(FileRepository.RszRepository);
            Scan();
        }

        private void Scan()
        {
            var spawnControllers = ImmutableArray.CreateBuilder<CharacterSpawnController>();
            var orphanEnemies = ImmutableArray.CreateBuilder<EnemySpawn>();
            ScanInner(Scene);
            SpawnControllers = spawnControllers.ToImmutable();
            OrphanEnemies = orphanEnemies.ToImmutable();

            void ScanInner(IRszSceneNode node)
            {
                if (node is RszGameObject gameObject)
                {
                    if (CharacterSpawnController.IsSpawnController(gameObject))
                    {
                        spawnControllers.Add(new CharacterSpawnController(this, gameObject));
                        return;
                    }

                    var enemyComponent = GetMainEnemyComponent(gameObject);
                    if (enemyComponent != null)
                    {
                        var enemy = new Enemy(this, gameObject, enemyComponent);
                        var enemySpawn = new EnemySpawn(this, null, enemy, enemy);
                        enemySpawn.SetClassPool();
                        orphanEnemies.Add(enemySpawn);
                    }
                }

                foreach (var child in node.Children)
                {
                    ScanInner(child);
                }
            }
        }

        public ScnFile Apply()
        {
            var appliedSpawnControllers = SpawnControllers
                .Select(x => x.Apply())
                .ToDictionary(x => x.Guid);

            Scene = Scene.VisitGameObjects(go => appliedSpawnControllers.GetValueOrDefault(go.Guid) ?? go);
            return ScnFile.AddMissingResources().Build();
        }

        public CharacterSpawnController? FindSpawnController(Guid gameObjectGuid)
        {
            return SpawnControllers.FirstOrDefault(x => x.GameObject.Guid == gameObjectGuid);
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

            var newEnemy = new Enemy(this, gameObject, newComponent);

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

        public CharacterSpawnController AddSpawnController(RszGameObject gameObject)
        {
            var controller = new CharacterSpawnController(this, gameObject);
            SpawnControllers = SpawnControllers.Add(controller);
            BioRandFolder = BioRandFolder.Add(gameObject);
            return controller;
        }

        public EnemySpawn Duplicate(EnemySpawn enemy, int contextId)
        {
            var newGameObject = enemy.Enemy.GameObject.Clone();
            var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            var newEnemy = new Enemy(this, newGameObject, newComponent);
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(contextId);
            var newEnemySpawn = new EnemySpawn(enemy.Area, enemy.SpawnController, enemy.Enemy, newEnemy);
            return newEnemySpawn;
        }

        private RszObjectNode? GetMainEnemyComponent(RszGameObject gameObject)
        {
            return gameObject.Components.FirstOrDefault(x => EnemyClassFactory.FindEnemyKind(x.Type.Name) != null);
        }

        public override string ToString() => FileName;
    }
}
