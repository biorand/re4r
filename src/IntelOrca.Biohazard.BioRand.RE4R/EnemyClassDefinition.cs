using System.Collections.Generic;
using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyClassDefinition(
        string key,
        string name,
        string category,
        int classification,
        int maxPack,
        int minHealth,
        int maxHealth,
        bool plaga,
        bool ranged,
        EnemyKindDefinition kind,
        ImmutableArray<WeaponChoice> weapon,
        List<EnemyFieldDefinition> fields)
    {
        public string Key => key;
        public string Name => name;
        public int Class => classification;
        public int MaxPack => maxPack;
        public int MinHealth => minHealth;
        public int MaxHealth => maxHealth;
        public bool Plaga => plaga;
        public bool Ranged => ranged;
        public EnemyKindDefinition Kind => kind;
        public ImmutableArray<WeaponChoice> Weapon => weapon;
        public List<EnemyFieldDefinition> Fields => fields;

        public ConfigCategory Category
        {
            get
            {
                return category switch
                {
                    "Villager" => new ConfigCategory(category, "#457c45", "#fff"),
                    "Zealot" => new ConfigCategory(category, "#222", "#fff"),
                    "Soldier" => new ConfigCategory(category, "#6b64ad", "#fff"),
                    "Colmillos" => new ConfigCategory(category, "#00f", "#fff"),
                    "Novistador" => new ConfigCategory(category, "#2f4f2f", "#fff"),
                    "Araña" => new ConfigCategory(category, "#0c0", "#000"),
                    "Brute" => new ConfigCategory(category, "#000080", "#fff"),
                    "JJ" => new ConfigCategory(category, "#c00", "#fff"),
                    "Armadura" => new ConfigCategory(category, "#dd0", "#000"),
                    "Chainsaw" => new ConfigCategory(category, "#a67b5b", "#fff"),
                    "Garrador" => new ConfigCategory(category, "#555", "#fff"),
                    "Regenerador" => new ConfigCategory(category, "#b9b8b5", "#000"),
                    "Mendez" => new ConfigCategory(category, "#c08081", "#fff"),
                    "Krauser" => new ConfigCategory(category, "#2f4f2f", "#fff"),
                    "Verdugo" => new ConfigCategory(category, "4e007f", "#fff"),
                    "Pesanta" => new ConfigCategory(category, "#800080", "#fff"),
                    "U3" => new ConfigCategory(category, "#333", "#fff"),
                    "Animal" => new ConfigCategory(category, "#fff", "#000"),
                    _ => new ConfigCategory(category, "#ddd", "#000"),
                };
            }
        }

        public override string ToString() => Name;
    }
}
