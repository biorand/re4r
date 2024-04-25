using System;
using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EnemySpawn
    {
        public Area Area { get; }
        public Enemy OriginalEnemy { get; }
        public Enemy Enemy { get; private set; }
        public bool Horde { get; set; }
        public bool PreventDuplicate { get; set; }
        public ImmutableArray<EnemyClassDefinition> PreferredClassPool { get; set; } = [];
        public ImmutableArray<EnemyClassDefinition> ClassPool { get; set; } = [];
        public EnemyClassDefinition? ChosenClass { get; set; }

        public EnemySpawn(Area area, Enemy originalEnemy, Enemy enemy)
        {
            Area = area;
            OriginalEnemy = originalEnemy;
            Enemy = enemy;
        }

        public Guid OriginalGuid => OriginalEnemy.Guid;
        public Guid Guid => Enemy.Guid;

        public void ConvertType(Area area, EnemyKindDefinition kind)
        {
            Enemy = area.ConvertTo(Enemy, kind.ComponentName);
        }

        public bool Prefers(EnemyClassDefinition ecd)
        {
            if (PreferredClassPool.IsDefaultOrEmpty)
                return ClassPool.Contains(ecd);
            return PreferredClassPool.Contains(ecd);
        }

        public EnemySpawn Duplicate(int contextId)
        {
            var newEnemy = Area.Duplicate(Enemy, contextId);
            var result = new EnemySpawn(Area, Enemy, newEnemy);
            result.Horde = Horde;
            result.ClassPool = ClassPool;
            result.PreferredClassPool = PreferredClassPool;
            result.ChosenClass = ChosenClass;
            return result;
        }

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
    }
}
