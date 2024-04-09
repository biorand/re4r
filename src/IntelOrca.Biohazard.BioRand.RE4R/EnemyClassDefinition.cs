using System.Collections.Generic;
using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyClassDefinition(string key, string name, int classification, EnemyKindDefinition kind, ImmutableArray<WeaponChoice> weapon, List<EnemyFieldDefinition> fields)
    {
        public string Key { get; } = key;
        public string Name { get; } = name;
        public int Class { get; } = classification;
        public EnemyKindDefinition Kind { get; } = kind;
        public ImmutableArray<WeaponChoice> Weapon { get; } = weapon;
        public List<EnemyFieldDefinition> Fields { get; } = fields;

        public override string ToString() => Name;
    }
}
