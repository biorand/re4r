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

        public ImmutableArray<EnemySpawn> EnemySpawns { get; set; }

        private RszInstance? GetMainEnemyComponent(ScnFile.GameObjectData gameObject)
        {
            return gameObject.Components.FirstOrDefault(x => EnemyClassFactory.FindEnemyKind(x.Name) != null);
        }

        public Enemy ConvertTo(Enemy enemy, string type)
        {
            var gameObject = enemy.GameObject;
            var oldComponent = enemy.MainComponent;
            if (oldComponent.RszClass.name == type)
                return enemy;

            ScnFile.AddComponent(gameObject, type);
            gameObject.Components.Remove(oldComponent);
            var newComponent = gameObject.Components.Last();

            var newEnemy = new Enemy(this, gameObject, newComponent);

            // Copy fields over
            foreach (var f in oldComponent.Fields)
            {
                var oldValue = enemy.GetFieldValue(f.name);
                newEnemy.SetFieldValue(f.name, oldValue!);
            }

            return Reset(newEnemy);
        }

        public Enemy Duplicate(Enemy enemy, int contextId)
        {
            var newGameObject = ScnFile.DuplicateGameObject(enemy.GameObject);
            var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            var newEnemy = new Enemy(this, newGameObject, newComponent);
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(contextId);
            return Reset(newEnemy);
        }

        public Enemy Reset(Enemy enemy)
        {
            enemy.Weapon = 0;
            enemy.SecondaryWeapon = 0;
            enemy.MontageId = 0;
            return enemy;
        }
    }
}
