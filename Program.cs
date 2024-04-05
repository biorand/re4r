using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    class Program
    {
        private static EnemyClassFactory _enemyClassFactory = EnemyClassFactory.Create();
        private static Random _random = new Random(0);

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

#if false
            var files = Directory.GetFiles(@"G:\re4r\extract\patch_003\natives\stm\_chainsaw\leveldesign", "*.scn.20", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var area = new Area(file);
                    foreach (var go in area.ScnFile.IterAllGameObjects(true))
                    {
                        foreach (var component in go.Components)
                        {
                            if (component.Name.Contains("SpawnParam"))
                            {
                                var kind = Enemy.GetKind(component);
                                if (kind == EnemyKind.Unknown)
                                {
                                    Console.WriteLine(component.Name + ":" + file);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
#endif


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
            var oldEnemies = area.Enemies.Select(GetEnemySummary).ToArray();

            // LogEnemies(area);
            // Console.WriteLine("----------------------------------");
            var multiplier = 1.5;
            var enemies = area.Enemies;
            var newEnemyCount = (int)enemies.Length * multiplier;
            var delta = (int)Math.Round(newEnemyCount - enemies.Length);
            if (delta != 0)
            {
                var bag = new EndlessBag<Enemy>(_random, enemies);
                var enemiesToCopy = bag.Next(delta);
                foreach (var e in enemiesToCopy)
                {
                    area.Duplicate(e);
                }
            }

            foreach (var enemy in area.Enemies)
            {
                var e = enemy;
                var ecd = _enemyClassFactory.Next(_random);
                e = area.ConvertTo(e, ecd.Kind.ComponentName);

                if (ecd.Weapon.Length == 0)
                {
                    e.Weapon = 0;
                    e.SecondaryWeapon = 0;
                }
                else
                {
                    var weaponChoice = _random.NextOf(ecd.Weapon);
                    e.Weapon = weaponChoice.Primary?.Id ?? 0;
                    e.SecondaryWeapon = weaponChoice.Secondary?.Id ?? 0;
                }

                foreach (var fd in ecd.Fields)
                {
                    var fieldValue = _random.NextOf(fd.Values);
                    e.MainComponent.SetFieldValue(fd.Name, fieldValue);
                }

                e.Health = _random.Next(400, 1000);

                // var count = _random.Next(0, 4);
                // for (var i = 0; i <= count; i++)
                // {
                //     var e = enemy;
                //     if (i != 0)
                //         e = area.Duplicate(enemy);
                // 
                //     if (true)
                //     {
                //         e = area.ConvertTo(e, EnemyKind.Knight);
                //         e.Health = _random.Next(400, 1000);
                //     }
                //     else if (_random.Next(0, 4) == 0)
                //     {
                //         e = area.ConvertTo(e, EnemyKind.Zealot);
                //         e.Health = _random.Next(400, 2000);
                //         e.MontageId = _random.NextOf(_zealotMontageIds);
                //         e.Weapon = 5807;
                //         e.SecondaryWeapon = 5808;
                //         break;
                //     }
                //     else if (_random.Next(0, 8) == 0)
                //     {
                //         e = area.ConvertTo(e, EnemyKind.Dog);
                //         e.Health = _random.Next(400, 2000);
                //         e.MontageId = _random.NextOf(_dogMontageIds);
                //         e.ItemDrop = new Item(120830400, 1);
                //         e.ShouldDropItemAtRandom = false;
                //         break;
                //     }
                //     else if (_random.Next(0, 4) == 0)
                //     {
                //         e = area.ConvertTo(e, EnemyKind.Chainsaw);
                //         e.Health = _random.Next(1000, 8000);
                //         e.MontageId = _random.NextOf(_chainsawMontageIds);
                //         e.ItemDrop = new Item(120830400, 1);
                //         e.ShouldDropItemAtRandom = false;
                //         break;
                //     }
                //     else if (enemy.MainComponent.Name.Contains("Ch1c0SpawnParam"))
                //     {
                //         e.Health = _random.Next(500, 2000);
                //         e.Weapon = _random.NextOf(new[] { 5801, 5802, 5803, 5804, 5805, 5806, 5810, 5814, 5815, 5817, 5821, 5822 });
                //         e.MontageId = _random.NextOf(_villagerMontageIds);
                // 
                //         if (_random.Next(0, 2) == 0)
                //         {
                //             e.ItemDrop = new Item(114416000, 1);
                //             e.ShouldDropItemAtRandom = false;
                //         }
                //         e.IsLeftHanded = _random.Next(0, 10) == 0;
                //     }
                // }
            }
            LogEnemies(area);

            var newEnemies = area.Enemies.Select(GetEnemySummary).ToArray();

            var enemyCount = Math.Max(oldEnemies.Length, newEnemies.Length);
            if (enemyCount != 0)
            {
                var lhColumn = oldEnemies.Max(x => x.Length);
                var rhColumn = newEnemies.Max(x => x.Length);
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine($"Area: {area.FileName}");
                Console.WriteLine($"------------------------------------------------------");
                for (var i = 0; i < enemyCount; i++)
                {
                    var oldE = i < oldEnemies.Length ? oldEnemies[i] : "";
                    var newE = i < newEnemies.Length ? newEnemies[i] : "";
                    Console.WriteLine($"| {oldE.PadRight(lhColumn)} | {newE.PadRight(rhColumn)} |");
                }
                Console.WriteLine($"------------------------------------------------------");
                Console.WriteLine();
            }
        }

        private static void LogEnemies(Area area)
        {
            foreach (var enemy in area.Enemies)
            {
                // Console.WriteLine($"Enemy: {enemy}");
                // Console.WriteLine($"  Health: {enemy.Health}");
                // Console.WriteLine($"  Montage ID: {enemy.MontageId}");
                // Console.WriteLine($"  Weapon: {GetWeaponName(enemy.Weapon)}");
                // Console.WriteLine($"  Item drop: {enemy.ItemDrop}");
                // Console.WriteLine($"  Should drop item at random: {enemy.ShouldDropItemAtRandom}");
                // Console.WriteLine($"  Is left handed: {enemy.IsLeftHanded}");
            }
        }

        private static string GetEnemySummary(Enemy enemy)
        {
            return $"{enemy.Kind} ({enemy.Health})";
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
            2366387651 // brute
        };

        private readonly static uint[] _zealotMontageIds = new uint[]
        {
            1002902302,
            1017464743,
            1018592117,
            1062154600,
            1344021415,
            1356451679,
            1480734989,
            1579850443,
            1609598768,
            1652389416,
            1790819418,
            1795353310,
            1835549671,
            1835752926,
            1874208518,
            1973329980,
            2126434953,
            2133529107,
            2185290760,
            2193359846,
            2240595251,
            2253052265,
            2348057730,
            2353979389,
            2363529758,
            2366387651,
            244681229,
            2496141582,
            2530473261,
            2543973566,
            2550630687,
            2655811582,
            2686445109,
            272648290,
            2847517058,
            2877752045,
            2915949180,
            3013601816,
            308095703,
            3218068109,
            3377551140,
            346319577,
            3510933723,
            3654826922,
            3722087590,
            381442320,
            381516727,
            3818312488,
            3825917684,
            3916455954,
            3980194655,
            4010527508,
            4077528276,
            4104682830,
            4119352667,
            412031697,
            4121631600,
            4160361988,
            4191550041,
            711204927,
            720240351,
            72376362,
            763052872,
            784358110,
            852951712,
            897984333,
            904993397,
        };

        private readonly static uint[] _militiaMontageIds = new uint[]
        {
            1017464743,
            1018592117,
            1139658603,
            1579850443,
            1609598768,
            1790233674,
            1790819418,
            1903465655,
            2001761790,
            2126434953,
            2133529107,
            2240595251,
            2253052265,
            2348057730,
            2353979389,
            2366387651,
            2438733595,
            244681229,
            2491554104,
            253025149,
            2543973566,
            2655811582,
            2915949180,
            2968040625,
            308095703,
            3218068109,
            3255288962,
            3594914692,
            3680364053,
            3692766905,
            3722087590,
            3825917684,
            3916455954,
            3980194655,
            4119352667,
            412031697,
            4191550041,
            4264763154,
            763052872,
            781075099,
            897984333,
        };

        private readonly static uint[] _chainsawMontageIds = new uint[]
        {
            1106175613U,
            3313117636U,
            2693883502U,
        };

        private readonly static uint[] _dogMontageIds = new uint[]
        {
            1106175613U,
            842441658U
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

    public class Area
    {
        private static int _ctxIdIndex = 5000;

        public string FileName { get; }
        public ScnFile ScnFile { get; }

        public Area(string path)
        {
            FileName = Path.GetFileName(path);
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

        public Enemy ConvertTo(Enemy enemy, string type)
        {
            var gameObject = enemy.GameObject;
            var oldComponent = enemy.MainComponent;
            if (oldComponent.Name == type)
                return enemy;

            ScnFile.AddComponent(gameObject, type);
            gameObject.Components.Remove(oldComponent);
            var newComponent = gameObject.Components.Last();
            foreach (var f in oldComponent.Fields)
            {
                var oldValue = oldComponent.GetFieldValue(f.name);
                newComponent.SetFieldValue(f.name, oldValue!);
            }

            return new Enemy(gameObject, newComponent);
        }

        public Enemy ConvertTo(Enemy enemy, EnemyKind kind)
        {
            var newEnemy = ConvertTo(enemy, Enemy.GetEnemyComponentName(kind));
            switch (kind)
            {
                case EnemyKind.Zealot:
                    newEnemy.MontageId = 381516727U;
                    break;
                case EnemyKind.Chainsaw:
                    newEnemy.RolePatternHash = 2180083513U;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513U;
                    newEnemy.MontageId = 1106175613U;
                    break;
                case EnemyKind.Dog:
                    newEnemy.RolePatternHash = 4266714029U;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513U;
                    newEnemy.MontageId = 1106175613U;
                    break;
                case EnemyKind.Novistador:
                    newEnemy.RolePatternHash = 1965048383;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    newEnemy.MontageId = 0;
                    newEnemy.Weapon = 0;
                    newEnemy.SecondaryWeapon = 0;
                    break;
                case EnemyKind.BruteWithGun:
                    newEnemy.MontageId = 1106175613;
                    newEnemy.RolePatternHash = 3727710285;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    break;
                case EnemyKind.PlagasSpider:
                    newEnemy.MontageId = 1106175613;
                    break;
                case EnemyKind.Militia:
                    newEnemy.MontageId = 1017464743;
                    break;
                case EnemyKind.Garrador:
                    newEnemy.MontageId = 0;
                    newEnemy.RolePatternHash = 3727710285;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    break;
                case EnemyKind.Regenerador:
                    newEnemy.MontageId = 1948795948; // 363312158;
                    newEnemy.RolePatternHash = 3727710285;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    break;
                case EnemyKind.Knight:
                    newEnemy.MontageId = 3367147326;
                    newEnemy.RolePatternHash = 2910380095;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    break;
                case EnemyKind.Sadler:
                    newEnemy.MontageId = 1106175613;
                    newEnemy.RolePatternHash = 3727710285;
                    newEnemy.PreFirstForceMovePatternHash = 2180083513;
                    break;
            }
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
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));
            if (mainComponent == null)
                throw new ArgumentNullException(nameof(mainComponent));

            GameObject = gameObject;
            MainComponent = mainComponent;
        }

        public Guid Guid => GameObject.Guid;
        public EnemyKind Kind => GetKind(MainComponent);

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

        private string MontageIdFieldName =>
            Kind switch
            {
                EnemyKind.Zealot => "_Ch1c0z1MontageID",
                EnemyKind.Militia => "_Ch1c0z2MontageID",
                _ => "_MontageID"
            };

        public uint MontageId
        {
            get => (uint?)MainComponent.GetFieldValue(MontageIdFieldName) ?? 0;
            set
            {
                MainComponent.SetFieldValue("_MontageID", 0U);
                MainComponent.SetFieldValue(MontageIdFieldName, value);
            }
        }

        public int Weapon
        {
            get => MainComponent.GetFieldValue("_EquipWeapon") as int? ?? 0;
            set => MainComponent.SetFieldValue("_EquipWeapon", value);
        }

        public int SecondaryWeapon
        {
            get => MainComponent.GetFieldValue("_SubWeapon") as int? ?? 0;
            set => MainComponent.SetFieldValue("_SubWeapon", value);
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

        public static bool IsEnemyComponent(RszInstance component) => GetKind(component) != EnemyKind.Unknown;

        public static string GetEnemyComponentName(EnemyKind kind)
        {
            if (kind == EnemyKind.Chainsaw)
                return "chainsaw.Ch1d1z1SpawnParamMercenaries";
            return "chainsaw." + GetEnemyKindName(kind) + "SpawnParam";
        }

        public static EnemyKind GetKind(RszInstance component)
        {
            foreach (var kind in EnemyKinds)
            {
                var componentName = $"{GetEnemyKindName(kind)}SpawnParam";
                if (component.Name.Contains(componentName))
                {
                    return kind;
                }
            }
            return EnemyKind.Unknown;
        }

        public static string GetEnemyKindName(EnemyKind kind)
        {
            return kind switch
            {
                EnemyKind.Villager => "Ch1c0",
                EnemyKind.BruteWithGun => "Ch1c8z0",
                EnemyKind.Zealot => "Ch1c0z1",
                EnemyKind.Chainsaw => "Ch1d1z1",
                EnemyKind.PlagasSpider => "Ch1e0z0",
                EnemyKind.Dog => "Ch1d2z0",
                EnemyKind.Novistador => "Ch1d3z0",
                EnemyKind.Militia => "Ch1c0z2",
                EnemyKind.Garrador => "Ch1d0z0",
                EnemyKind.Regenerador => "Ch1d4z0",
                EnemyKind.Knight => "Ch1d6z0",
                EnemyKind.Sadler => "Ch1f8z0",
                _ => throw new Exception(),
            };
        }

        private readonly static EnemyKind[] EnemyKinds = new[]
        {
            EnemyKind.Villager,
            EnemyKind.BruteWithGun,
            EnemyKind.PlagasSpider,
            EnemyKind.Zealot,
            EnemyKind.Chainsaw,
            EnemyKind.Dog,
            EnemyKind.Novistador,
            EnemyKind.Militia,
            EnemyKind.Garrador,
            EnemyKind.Regenerador,
            EnemyKind.Knight,
            EnemyKind.Sadler
        };
    }
}
