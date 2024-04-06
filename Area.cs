﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Area
    {
        private static int _ctxIdIndex = 5000;

        public EnemyClassFactory EnemyClassFactory { get; }
        public string FileName { get; }
        public ScnFile ScnFile { get; }

        public Area(EnemyClassFactory enemyClassFactory, string path)
            : this(enemyClassFactory, path, File.ReadAllBytes(path))
        {
        }

        public Area(EnemyClassFactory enemyClassFactory, string path, byte[] data)
        {
            EnemyClassFactory = enemyClassFactory;
            FileName = Path.GetFileName(path);
            ScnFile = new ScnFile(new RszFileOption(GameName.re4), new FileHandler(new MemoryStream(data)));
            ScnFile.Read();
            ScnFile.SetupGameObjects();
        }

        public void Save(string path)
        {
            ScnFile.SaveAs(path);
        }

        public byte[] SaveData()
        {
            var ms = new MemoryStream();
            var fileHandler = new FileHandler(ms);
            ScnFile.WriteTo(fileHandler);
            return ms.ToArray();
        }

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

        public Enemy Duplicate(Enemy enemy)
        {
            var newGameObject = ScnFile.DuplicateGameObject(enemy.GameObject);
            var newComponent = GetMainEnemyComponent(newGameObject) ?? throw new Exception("Unable to find new enemy component for duplicated enemy.");
            var newEnemy = new Enemy(this, newGameObject, newComponent);
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(_ctxIdIndex++);
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
