using System;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class CharacterSpawnController
    {
        public Area Area { get; }
        public RszGameObject GameObject { get; private set; }
        public RszObjectNode Component { get; private set; }
        public SpawnControllerKind Kind { get; }
        public ImmutableArray<EnemySpawn> Enemies { get; private set; }

        public CharacterSpawnController(Area area, RszGameObject gameObject)
        {
            Area = area;
            GameObject = gameObject;
            Component = FindComponent(gameObject) ?? throw new Exception("Component not found for spawn controller");
            Kind = GetKindFromComponent(Component);
            Enemies = ScanEnemies();
        }

        private ImmutableArray<EnemySpawn> ScanEnemies()
        {
            var enemies = ImmutableArray.CreateBuilder<EnemySpawn>();
            GameObject.VisitGameObjects(go =>
            {
                var enemyComponent = GetMainEnemyComponent(go);
                if (enemyComponent != null)
                {
                    var enemy = new Enemy(Area, go, enemyComponent);
                    var enemyPlacement = Area.Randomizer.EnemyService.Find(enemy.Guid) ?? throw new RandomizerUserException($"Failed to find enemy placement for {enemy.Guid}");
                    var spawn = new EnemySpawn(Area, this, enemy, enemy, enemyPlacement);
                    spawn.SetClassPool();
                    enemies.Add(spawn);
                }
            });
            return enemies.ToImmutableArray();
        }

        public RszGameObject Apply()
        {
            GameObject = GameObject
                .AddOrUpdateComponent(Component)
                .VisitGameObjects(go =>
                {
                    var enemy = Enemies.FirstOrDefault(x => x.Guid == go.Guid);
                    return enemy != null ? enemy.Apply() : go;
                });
            return GameObject;
        }

        public void AddEnemy(EnemySpawn enemy)
        {
            GameObject = GameObject.AddOrUpdateChild(enemy.Enemy.GameObject);
            Enemies = Enemies.Add(enemy);
        }

        public bool Enabled
        {
            get => Component.Get<bool>("Enabled");
            set => Component = Component.Set("Enabled", value);
        }

        public uint Difficulty
        {
            get => Component.Get<uint>("_DifficutyParam");
            set => Component = Component.Set("_DifficutyParam", value);
        }

        public Guid Guid
        {
            get => Component.Get<Guid>("_GUID");
            set => Component = Component.Set("_GUID", value);
        }

        public FlagCondition SpawnCondition
        {
            get => Component.Get<FlagCondition>("_SpawnCondition");
            set => Component = Component.Set("_SpawnCondition", value);
        }

        public FlagConditionStrict SpawnSkipCondition
        {
            get => Component.Get<FlagConditionStrict>("_SpawnSkipCondition");
            set => Component = Component.Set("_SpawnSkipCondition", value);
        }

        private static RszObjectNode? FindComponent(RszGameObject gameObject)
        {
            foreach (var component in gameObject.Components)
            {
                var kind = GetKindFromComponent(component);
                if (kind != SpawnControllerKind.None)
                {
                    return component;
                }
            }
            return null;
        }

        private static SpawnControllerKind GetKindFromComponent(RszObjectNode component)
        {
            return component.Type.Name switch
            {
                "chainsaw.CharacterSpawnController" => SpawnControllerKind.Standard,
                "chainsaw.CharacterSpawnPointController" => SpawnControllerKind.Point,
                "chainsaw.CharacterSpawnWaveController" => SpawnControllerKind.Wave,
                _ => SpawnControllerKind.None,
            };
        }

        public static bool IsSpawnController(RszGameObject gameObject)
        {
            foreach (var component in gameObject.Components)
            {
                var kind = GetKindFromComponent(component);
                if (kind != SpawnControllerKind.None)
                {
                    return true;
                }
            }
            return false;
        }

        private RszObjectNode? GetMainEnemyComponent(RszGameObject gameObject)
        {
            return gameObject.Components.FirstOrDefault(x => Area.Randomizer.GetService<EnemyClassFactory>().FindEnemyKind(x.Type.Name) != null);
        }

        public override string ToString() => GameObject.Name;
    }
}
