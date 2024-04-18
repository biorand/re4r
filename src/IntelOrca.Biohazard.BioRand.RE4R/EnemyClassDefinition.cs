using System.Collections.Generic;
using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyClassDefinition(
        string key,
        string name,
        int classification,
        int minHealth,
        int maxHealth,
        bool plaga,
        bool ranged,
        EnemyKindDefinition kind,
        ImmutableArray<WeaponChoice> weapon,
        List<EnemyFieldDefinition> fields)
    {
        public string Key { get; } = key;
        public string Name { get; } = name;
        public int Class { get; } = classification;
        public int MinHealth { get; } = minHealth;
        public int MaxHealth { get; } = maxHealth;
        public bool Plaga { get; } = plaga;
        public bool Ranged { get; } = ranged;
        public EnemyKindDefinition Kind { get; } = kind;
        public ImmutableArray<WeaponChoice> Weapon { get; } = weapon;
        public List<EnemyFieldDefinition> Fields { get; } = fields;

        public override string ToString() => Name;
    }
}
