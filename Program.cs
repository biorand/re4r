using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RszTool;

namespace MyNamespace
{
    class Program
    {
        private static Random _random = new Random();

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // var originalFile = @"M:\git\REE.PAK.Tool\REE.Unpacker\REE.Unpacker\bin\Release\out\natives\stm\_chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";
            // var originalFile = @"C:\Users\Ted\Desktop\backup\level_cp10_chp1_1_010.scn.20";
            // var targetFile = @"F:\games\re4r\fluffy\Games\RE4R\Mods\orca_test\natives\STM\_Chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";
            // var targetFile = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4\natives\STM\_Chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";

            var areas = new[] {
                // Chapter 1
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_1\\level_cp10_chp1_1_010.scn.20",
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_1\\level_cp10_chp1_1_020.scn.20",
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_1\\level_cp10_chp1_1_030.scn.20",

                // Chapter 2
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_2\\level_cp10_chp1_2.scn.20",
                "natives\\stm\\_chainsaw\\leveldesign\\location\\loc44\\level_loc44.scn.20",

                // Chapter 3
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_3\\level_cp10_chp1_3.scn.20",
                "natives\\stm\\_chainsaw\\leveldesign\\chapter\\cp10_chp1_3\\level_cp10_chp1_3_000.scn.20",
            };

            for (var i = 0; i < areas.Length; i++)
            {
                var src = FindFile(areas[i]);
                var dst = GetOutputPath(areas[i]);
                Directory.CreateDirectory(Path.GetDirectoryName(dst));

                var area = new Area(src);
                RandomizeArea(area);
                Console.WriteLine($"Writing {dst}...");
                area.Save(dst);
            }
        }

        private static string FindFile(string fileName)
        {
            var basePath = @"G:\re4r\extract\patch_003";
            var path = Path.Combine(basePath, fileName);
            if (File.Exists(path))
            {
                return path;
            }
            return null;
        }

        private static string GetOutputPath(string fileName)
        {
            var basePath = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4";
            return Path.Combine(basePath, fileName);
        }

        private static void RandomizeArea(Area area)
        {
            // LogEnemies(area);
            // Console.WriteLine("----------------------------------");
            foreach (var enemy in area.Enemies)
            {
                var count = _random.Next(0, 4);
                for (var i = 0; i <= count; i++)
                {
                    var e = enemy;
                    if (i != 0)
                        e = area.Duplicate(enemy);

                    if (_random.Next(0, 4) == 0)
                    {
                        e = area.ConvertToChainsaw(e);
                        e.Health = _random.Next(1000, 8000);
                        e.MontageId = _random.NextOf(_chainsawMontageIds);
                        e.ItemDrop = new Item(120830400, 1);
                        e.ShouldDropItemAtRandom = false;
                        break;
                    }
                    else
                    {
                        if (e.MainComponent.Name.Contains("z"))
                        {
                        }
                        e.Health = _random.Next(500, 2000);
                        e.Weapon = _random.NextOf(new[] { 5801, 5802, 5803, 5804, 5805, 5806, 5810, 5814, 5815, 5817, 5821, 5822 });
                        e.MontageId = _random.NextOf(_villagerMontageIds);
                        if (_random.Next(0, 2) == 0)
                        {
                            e.ItemDrop = new Item(114416000, 1);
                            e.ShouldDropItemAtRandom = false;
                        }
                        e.IsLeftHanded = _random.Next(0, 10) == 0;
                    }
                }
            }
            // LogEnemies(area);
        }

        private static void LogEnemies(Area area)
        {
            foreach (var enemy in area.Enemies)
            {
                Console.WriteLine($"Enemy: {enemy}");
                Console.WriteLine($"  Health: {enemy.Health}");
                Console.WriteLine($"  Montage ID: {enemy.MontageId}");
                Console.WriteLine($"  Weapon: {GetWeaponName(enemy.Weapon)}");
                Console.WriteLine($"  Item drop: {enemy.ItemDrop}");
                Console.WriteLine($"  Should drop item at random: {enemy.ShouldDropItemAtRandom}");
                Console.WriteLine($"  Is left handed: {enemy.IsLeftHanded}");
            }
        }

        private static string GetWeaponName(int id)
        {
            _weapons.TryGetValue(id, out var name);
            return $"{id} ({name})";
        }

        private readonly static uint[] _villagerMontageIds = new uint[]
        {
            1017464743,
            1480734989,
            1973329980,
            2240595251,
            2253052265,
            244681229,
            2655811582,
            2877752045,
            2915949180,
            3218068109,
            3377551140,
            3510933723,
            3722087590,
            3980194655,
            412031697,
            720240351,
            852951712,
            897984333,
        };

        private readonly static uint[] _chainsawMontageIds = new uint[]
        {
            1106175613U,
            3313117636U,
            2693883502U,
        };

        private static readonly Dictionary<int, string> _weapons = new Dictionary<int, string>()
        {
            [0] = "none",
            [5801] = "hatchet",
            [5802] = "knife",
            [5803] = "hoe",
            [5804] = "pitchfork",
            [5805] = "torch",
            [5806] = "dynamite",
            [5807] = "morning star",
            [5808] = "wooden shield",
            [5809] = "scythe",
            [5810] = "brute hammer",
            [5811] = "shock baton",
            [5813] = "iron shield",
            [5814] = "molotov",
            [5815] = "3 - shot crossbow",
            [5816] = "machete",
            [5817] = "axe",
            [5818] = "knight sword",
            [5821] = "shovel",
            [5822] = "crowbar",
            [5823] = "spiked bat",
            [5824] = "RPG",
            [5825] = "krauser's bow",
            [5826] = "red zealot's staff",
            [5830] = "JJ's wrist gun",
        };
    }

    public static class Extensions
    {
        public static T NextOf<T>(this Random random, IEnumerable<T> items)
        {
            var count = items.Count();
            var index = random.Next(0, count);
            return items.ElementAt(index);
        }
    }

    public class Area
    {
        private static int _ctxIdIndex = 5000;

        public ScnFile ScnFile { get; }

        public Area(string path)
        {
            ScnFile = new ScnFile(new RszFileOption(GameName.re4), new FileHandler(path));
            ScnFile.Read();
            ScnFile.SetupGameObjects();
        }

        public Area(ScnFile scnFile)
        {
            ScnFile = scnFile;
        }

        public void Save(string path)
        {
            ScnFile.SaveAs(path);
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
                        result.Add(new Enemy(gameObject, mainComponent));
                    }
                }
                return result.ToArray();
            }
        }

        private static RszInstance GetMainEnemyComponent(ScnFile.GameObjectData gameObject)
        {
            return gameObject.Components.FirstOrDefault(Enemy.IsEnemyComponent);
        }

        public Enemy ConvertToChainsaw(Enemy enemy)
        {
            var gameObject = enemy.GameObject;
            var oldComponent = enemy.MainComponent;

            ScnFile.AddComponent(gameObject, "chainsaw.Ch1d1z1SpawnParamMercenaries");
            gameObject.Components.Remove(oldComponent);
            var newComponent = gameObject.Components.Last();
            foreach (var f in oldComponent.Fields)
            {
                newComponent.SetFieldValue(f.name, oldComponent.GetFieldValue(f.name));
            }

            var newEnemy = new Enemy(gameObject, newComponent);
            newEnemy.RolePatternHash = 2180083513U;
            newEnemy.PreFirstForceMovePatternHash = 2180083513U;
            newEnemy.MontageId = 1106175613U;
            return newEnemy;
        }

        public Enemy Duplicate(Enemy enemy)
        {
            var newGameObject = ScnFile.DuplicateGameObject(enemy.GameObject);
            var newEnemy = new Enemy(newGameObject, GetMainEnemyComponent(newGameObject));
            newEnemy.ContextId = newEnemy.ContextId.WithIndex(_ctxIdIndex++);
            return newEnemy;
        }
    }

    public class Enemy
    {
        public ScnFile.GameObjectData GameObject { get; }
        public RszInstance MainComponent { get; }

        public Enemy(ScnFile.GameObjectData gameObject, RszInstance mainComponent)
        {
            GameObject = gameObject;
            MainComponent = mainComponent;
        }

        public Guid Guid => GameObject.Guid;

        public ContextId ContextId
        {
            get
            {
                var contextId = (RszInstance)MainComponent.GetFieldValue("_ContextID");
                var category = (sbyte)contextId.GetFieldValue("_Category");
                var kind = (byte)contextId.GetFieldValue("_Kind");
                var group = (int)contextId.GetFieldValue("_Group");
                var index = (int)contextId.GetFieldValue("_Index");
                return new ContextId(category, kind, group, index);
            }
            set
            {
                var contextId = (RszInstance)MainComponent.GetFieldValue("_ContextID");
                contextId.SetFieldValue("_Category", value.Category);
                contextId.SetFieldValue("_Kind", value.Kind);
                contextId.SetFieldValue("_Group", value.Group);
                contextId.SetFieldValue("_Index", value.Index);
            }
        }

        public int? Health
        {
            get
            {
                var hasValue = MainComponent.GetFieldValue("STRUCT__HitPoint__HasValue");
                if (hasValue is not true)
                    return null;
                return (int)MainComponent.GetFieldValue("STRUCT__HitPoint__Value");
            }
            set
            {
                if (value == null)
                {
                    MainComponent.SetFieldValue("STRUCT__HitPoint__HasValue", false);
                    MainComponent.SetFieldValue("STRUCT__HitPoint__Value", 0);
                }
                else
                {
                    MainComponent.SetFieldValue("STRUCT__HitPoint__HasValue", true);
                    MainComponent.SetFieldValue("STRUCT__HitPoint__Value", value.Value);
                }
            }
        }

        public uint MontageId
        {
            get => (uint)MainComponent.GetFieldValue("_MontageID");
            set => MainComponent.SetFieldValue("_MontageID", value);
        }

        public int Weapon
        {
            get => MainComponent.GetFieldValue("_EquipWeapon") as int? ?? 0;
            set => MainComponent.SetFieldValue("_EquipWeapon", value);
        }

        public Item? ItemDrop
        {
            get
            {
                var shouldDropItem = MainComponent.GetFieldValue("_ShouldDropItem");
                if (shouldDropItem is true)
                {
                    var shouldDropItemAtRandom = MainComponent.GetFieldValue("_ShouldDropItemAtRandom");
                    var dropItemId = (int)MainComponent.GetFieldValue("_DropItemID");
                    var dropItemCount = (int)MainComponent.GetFieldValue("_DropItemCount");
                    return new Item(dropItemId, dropItemCount);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value is Item drop)
                {
                    MainComponent.SetFieldValue("_ShouldDropItem", true);
                    MainComponent.SetFieldValue("_ShouldDropItemAtRandom", false);
                    MainComponent.SetFieldValue("_DropItemID", drop.Id);
                    MainComponent.SetFieldValue("_DropItemCount", drop.Count);
                }
                else
                {
                    MainComponent.SetFieldValue("_ShouldDropItem", false);
                    MainComponent.SetFieldValue("_ShouldDropItemAtRandom", false);
                    MainComponent.SetFieldValue("_DropItemID", -1);
                    MainComponent.SetFieldValue("_DropItemCount", 0);
                }
            }
        }

        public bool ShouldDropItemAtRandom
        {
            get => MainComponent.GetFieldValue("_ShouldDropItemAtRandom") is true;
            set => MainComponent.SetFieldValue("_ShouldDropItemAtRandom", value);
        }

        public bool IsLeftHanded
        {
            get => MainComponent.GetFieldValue("_IsLeftHanded") is true;
            set => MainComponent.SetFieldValue("_IsLeftHanded", value);
        }

        public uint RolePatternHash
        {
            get => (uint)MainComponent.GetFieldValue("_RolePatternHash");
            set => MainComponent.SetFieldValue("_RolePatternHash", value);
        }

        public uint PreFirstForceMovePatternHash
        {
            get => (uint)MainComponent.GetFieldValue("_PreFirstForceMovePatternHash");
            set => MainComponent.SetFieldValue("_PreFirstForceMovePatternHash", value);
        }

        public override string ToString()
        {
            var componentName = MainComponent.Name;
            var cutOff = componentName.IndexOf("Spawn");
            if (cutOff != -1)
                componentName = componentName[..cutOff];
            cutOff = componentName.IndexOf(".");
            if (cutOff != -1)
                componentName = componentName[(cutOff + 1)..];
            return componentName.ToLower();
        }

        public static bool IsEnemyComponent(RszInstance component)
        {
            var allowed = new[]
            {
                "Ch1c0SpawnParam",
                "Ch1d1z1SpawnParam"
            };
            return allowed.Any(component.Name.Contains);
        }
    }

    public readonly struct Item(int id, int count)
    {
        public int Id { get; } = id;
        public int Count { get; } = count;

        public override string ToString() => Id == -1 ? "(automatic)" : $"{Id} x{Count}";
    }

    public readonly struct ContextId(sbyte category, byte kind, int group, int index)
    {
        public sbyte Category { get; } = category;
        public byte Kind { get; } = kind;
        public int Group { get; } = group;
        public int Index { get; } = index;

        public ContextId WithIndex(int value) => new ContextId(Category, Kind, Group, value);

        public override string ToString() => $"{Category},{Kind},{Group},{Index}";
    }
}
