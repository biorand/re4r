using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyClassFactory
    {
        public static EnemyClassFactory Default { get; } = Create();

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

        public EnemyClassDefinition Next(Rng rng)
        {
            var index = rng.Next(0, Classes.Length);
            return Classes[index];
        }

        public static EnemyClassFactory Create()
        {
            var kindDefinitions = new List<EnemyKindDefinition>();
            var weaponDefinitions = new List<WeaponDefinition>();
            var classDefinitions = new List<EnemyClassDefinition>();

            var jsonDocument = JsonDocument.Parse(Resources.enemies, new JsonDocumentOptions()
            {
                CommentHandling = JsonCommentHandling.Skip
            });

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
}
