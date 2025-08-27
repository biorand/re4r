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
        public ImmutableArray<CharacterSpawnController> SpawnControllers { get; private set; }

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

        public IEnumerable<EnemySpawn> Enemies => SpawnControllers.SelectMany(x => x.Enemies);

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
            Scene.VisitGameObjects(gameObject =>
            {
                if (CharacterSpawnController.IsSpawnController(gameObject))
                {
                    spawnControllers.Add(new CharacterSpawnController(this, gameObject));
                }
            });
            SpawnControllers = spawnControllers.ToImmutable();
        }

        public ScnFile Apply()
        {
            Scene = Scene.VisitGameObjects(go =>
            {
                var spawnController = SpawnControllers.FirstOrDefault(x => x.GameObject.Guid == go.Guid);
                return spawnController == null ? go : spawnController.Apply();
            });

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

        public CharacterSpawnController CreateSpawnController(RszGameObject gameObject)
        {
            var spawnController = new CharacterSpawnController(this, gameObject);
            BioRandFolder = BioRandFolder.Add(gameObject);
            return spawnController;
        }

        public EnemySpawn CreateEnemySpawn(CharacterSpawnController spawnController, RszGameObject gameObject)
        {
            return null;
        }

        public EnemySpawn Duplicate(EnemySpawn enemy, int contextId)
        {
            // var newGameObject = enemy.Enemy.GameObject.Clone();
            // var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            // var newEnemy = new Enemy(this, enemy.Enemy.SpawnController, newGameObject, newComponent);
            // newEnemy.ContextId = newEnemy.ContextId.WithIndex(contextId);
            // var newEnemySpawn = new EnemySpawn(this, enemy.Enemy, newEnemy);
            // _enemySpawns.Add(newEnemySpawn);
            // return newEnemySpawn;
            return null;
        }
    }
}
