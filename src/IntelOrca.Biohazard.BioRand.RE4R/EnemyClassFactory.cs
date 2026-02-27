using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class EnemyClassFactory
    {
        private readonly ChainsawRandomizer? _randomizer;

        public ImmutableArray<EnemyKindDefinition> EnemyKinds { get; }
        public ImmutableArray<WeaponDefinition> Weapons { get; }
        public ImmutableArray<EnemyClassDefinition> Classes { get; }

        public EnemyClassFactory(ChainsawRandomizer randomizer) : this()
        {
            _randomizer = randomizer;
        }

        public EnemyClassFactory()
        {
            var kindDefinitions = new List<EnemyKindDefinition>();
            var weaponDefinitions = new List<WeaponDefinition>();
            var classDefinitions = new List<EnemyClassDefinition>();

            var jsonDocument = JsonDocument.Parse(EmbeddedData.GetFile("enemies.json"), new JsonDocumentOptions()
            {
                CommentHandling = JsonCommentHandling.Skip
            });

            var kinds = jsonDocument.RootElement.GetProperty("kinds");
            foreach (var k in kinds.EnumerateArray())
            {
                var key = k.GetProperty("key").GetString() ?? throw new InvalidDataException();
                var componentName = k.GetProperty("componentName").GetString() ?? throw new InvalidDataException();
                var prefab = k.GetProperty("prefab").GetString() ?? throw new InvalidDataException();
                var closed = k.GetBooleanProperty("closed") ?? false;
                var noItemDrop = k.GetBooleanProperty("noItemDrop") ?? false;
                kindDefinitions.Add(new EnemyKindDefinition(key, componentName, prefab, closed, noItemDrop));
            }

            var weapons = jsonDocument.RootElement.GetProperty("weapons");
            foreach (var w in weapons.EnumerateArray())
            {
                var key = w.GetProperty("key").GetString() ?? throw new InvalidDataException();
                var id = w.GetProperty("id").GetInt32();
                var ranged = w.GetBooleanProperty("ranged") ?? false;
                weaponDefinitions.Add(new WeaponDefinition(key, id, ranged));
            }

            var classes = jsonDocument.RootElement.GetProperty("classes");
            foreach (var c in classes.EnumerateArray())
            {
                var key = c.GetProperty("key").GetString() ?? throw new InvalidDataException();
                var kindKey = c.GetProperty("kind").GetString() ?? throw new InvalidDataException();
                var kind = kindDefinitions.First(x => x.Key == kindKey);
                var name = c.GetProperty("name").GetString() ?? throw new InvalidDataException();
                var category = c.GetProperty("category").GetString() ?? throw new InvalidDataException();
                var classification = c.GetProperty("class").GetInt32();
                var maxPack = c.GetInt32Property("maxPack");
                var minHealth = c.GetProperty("minHealth").GetInt32();
                var maxHealth = c.GetProperty("maxHealth").GetInt32();
                var plaga = c.GetBooleanProperty("plaga") ?? false;
                var ranged = c.GetBooleanProperty("ranged") ?? false;

                var groups = new List<string>();
                if (c.TryGetProperty("groups", out var groupsElement))
                {
                    foreach (var groupElement in groupsElement.EnumerateArray())
                    {
                        groups.Add(groupElement.GetString() ?? throw new InvalidDataException());
                    }
                }

                var weaponChoices = new List<WeaponChoice>();
                if (c.TryGetProperty("weapon", out var weapon))
                {
                    foreach (var w in weapon.EnumerateArray())
                    {
                        var wKindKey = w.GetStringProperty("kind");
                        var wKind = wKindKey == null ? null : kindDefinitions.First(x => x.Key == wKindKey);

                        var primary = w.GetStringProperty("primary");
                        var secondary = w.GetStringProperty("secondary");
                        var primaryWeaponDef = primary == null
                            ? null
                            : weaponDefinitions.First(x => x.Key == primary);
                        var secondaryWeaponDef = secondary == null
                            ? null
                            : weaponDefinitions.First(x => x.Key == secondary);
                        weaponChoices.Add(new WeaponChoice(wKind, primaryWeaponDef, secondaryWeaponDef));
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

                classDefinitions.Add(new EnemyClassDefinition(
                    key,
                    name,
                    category,
                    groups.ToImmutableArray(),
                    classification,
                    maxPack ?? classification,
                    minHealth,
                    maxHealth,
                    plaga,
                    ranged,
                    kind,
                    weaponChoices.ToImmutableArray(),
                    fields));
            }


            EnemyKinds = kindDefinitions.ToImmutableArray();
            Weapons = weaponDefinitions.ToImmutableArray();
            Classes = classDefinitions.ToImmutableArray();
        }

        public EnemyKindDefinition? FindEnemyKind(string componentName)
        {
            return EnemyKinds.FirstOrDefault(x => componentName.Contains(x.ComponentName));
        }

        public ImmutableArray<EnemyClassDefinition> UserChosenClasses
        {
            get
            {
                var classes = Classes;
                if (_randomizer != null)
                {
                    classes = classes
                        .Where(x => GetClassRatio(_randomizer, x) > 0)
                        .ToImmutableArray();
                }
                return classes;
            }
        }

        private static double GetClassRatio(ChainsawRandomizer randomizer, EnemyClassDefinition ecd)
        {
            return randomizer.GetConfigOption<double>($"enemy-ratio-{ecd.Key}");
        }
    }
}
