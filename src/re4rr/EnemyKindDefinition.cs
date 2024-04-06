using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EnemyKindDefinition(string name, string componentName)
    {
        public string Name { get; set; } = name;
        public string ComponentName { get; } = componentName;

        public override string ToString() => Name;
    }

    internal class WeaponDefinition(string name, int id)
    {
        public string Name { get; } = name;
        public int Id { get; } = id;

        public override string ToString() => Name;
    }

    internal class EnemyClassDefinition(string name, EnemyKindDefinition kind, ImmutableArray<WeaponChoice> weapon, List<EnemyFieldDefinition> fields)
    {
        public string Name { get; } = name;
        public EnemyKindDefinition Kind { get; } = kind;
        public ImmutableArray<WeaponChoice> Weapon { get; } = weapon;
        public List<EnemyFieldDefinition> Fields { get; } = fields;

        public override string ToString() => Name;
    }

    internal class WeaponChoice
    {
        public WeaponDefinition? Primary { get; }
        public WeaponDefinition? Secondary { get; }

        public WeaponChoice(WeaponDefinition? primary = null, WeaponDefinition? secondary = null)
        {
            if (primary == null && secondary != null)
                throw new ArgumentNullException(nameof(primary));

            Primary = primary;
            Secondary = secondary;
        }

        public bool IsNone => Primary == null;

        public override string ToString()
        {
            if (Primary == null)
                return "(none)";
            if (Secondary == null)
                return $"{Primary}";
            return $"{Primary}, {Secondary}";
        }
    }

    internal class EnemyFieldDefinition(string name, ImmutableArray<object> values)
    {
        public string Name { get; } = name;
        public ImmutableArray<object> Values { get; } = values;

        public override string ToString()
        {
            if (Values.Length == 1)
                return $"{Name} = {Values[0]}";
            else
                return $"{Name} = {{{Values.Length} possible values}}";
        }
    }

    internal class EnemyClassFactory
    {
        public ImmutableArray<EnemyKindDefinition> EnemyKinds { get; }
        public ImmutableArray<WeaponDefinition> Weapons { get; }
        public ImmutableArray<EnemyClassDefinition> Classes { get; }

        private EnemyClassFactory(
            ImmutableArray<EnemyKindDefinition> enemyKinds,
            ImmutableArray<WeaponDefinition> weapons,
            ImmutableArray<EnemyClassDefinition> classes)
        {
            EnemyKinds = enemyKinds;
            Weapons = weapons;
            Classes = classes;
        }

        public EnemyKindDefinition? FindEnemyKind(string componentName)
        {
            return EnemyKinds.FirstOrDefault(x => componentName.Contains(x.ComponentName));
        }

        public EnemyClassDefinition Next(Random rng)
        {
            var index = rng.Next(0, Classes.Length);
            return Classes[index];
        }

        public static EnemyClassFactory Create(string dataPath)
        {
            var kindDefinitions = new List<EnemyKindDefinition>();
            var weaponDefinitions = new List<WeaponDefinition>();
            var classDefinitions = new List<EnemyClassDefinition>();

            var jsonDocument = JsonDocument.Parse(File.ReadAllText(dataPath));

            var kinds = jsonDocument.RootElement.GetProperty("kinds");
            foreach (var k in kinds.EnumerateArray())
            {
                var name = k.GetProperty("name").GetString() ?? throw new InvalidDataException();
                var componentName = k.GetProperty("componentName").GetString() ?? throw new InvalidDataException();
                kindDefinitions.Add(new EnemyKindDefinition(name, componentName));
            }

            var weapons = jsonDocument.RootElement.GetProperty("weapons");
            foreach (var w in weapons.EnumerateArray())
            {
                var name = w.GetProperty("name").GetString() ?? throw new InvalidDataException();
                var id = w.GetProperty("id").GetInt32();
                weaponDefinitions.Add(new WeaponDefinition(name, id));
            }

            var classes = jsonDocument.RootElement.GetProperty("classes");
            foreach (var c in classes.EnumerateArray())
            {
                var name = c.GetProperty("name").GetString() ?? throw new InvalidDataException();
                var kindName = c.GetProperty("kind").GetString() ?? throw new InvalidDataException();
                var kind = kindDefinitions.First(x => x.Name == kindName);

                var weaponChoices = new List<WeaponChoice>();
                if (c.TryGetProperty("weapon", out var weapon))
                {
                    foreach (var w in weapon.EnumerateArray())
                    {
                        var primary = w.GetStringProperty("primary");
                        var secondary = w.GetStringProperty("secondary");
                        var primaryWeaponDef = primary == null
                            ? null
                            : weaponDefinitions.First(x => x.Name == primary);
                        var secondaryWeaponDef = secondary == null
                            ? null
                            : weaponDefinitions.First(x => x.Name == secondary);
                        weaponChoices.Add(new WeaponChoice(primaryWeaponDef, secondaryWeaponDef));
                    }
                }

                var fields = new List<EnemyFieldDefinition>();
                foreach (var p in c.EnumerateObject())
                {
                    if (p.Name.StartsWith("field:"))
                    {
                        var fieldName = p.Name.Substring(6);
                        var values = new List<object>();
                        if (p.Value.ValueKind == JsonValueKind.Array)
                        {
                            values.AddRange(p.Value.EnumerateArray()
                                .Select(x => x.GetValue()!));
                        }
                        else
                        {
                            values.Add(p.Value.GetValue()!);
                        }
                        fields.Add(new EnemyFieldDefinition(fieldName, values.ToImmutableArray()));
                    }
                }

                classDefinitions.Add(new EnemyClassDefinition(name, kind, weaponChoices.ToImmutableArray(), fields));
            }

            return new EnemyClassFactory(
                kindDefinitions.ToImmutableArray(),
                weaponDefinitions.ToImmutableArray(),
                classDefinitions.ToImmutableArray());
        }
    }

    public static class JsonElementExtensions
    {
        public static string? GetStringProperty(this JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) ? value.GetString() : null;
        }

        public static object? GetValue(this JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => true,
                JsonValueKind.Number => (uint)element.GetUInt32(),
                JsonValueKind.Null => null,
                _ => throw new NotSupportedException()
            };
        }
    }
}
