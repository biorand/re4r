using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var gimmicks = GetAllGimmicks(randomizer);
            foreach (var grouping in gimmicks.GroupBy(x => x.GimmickFile.Path).OrderBy(x => x.Key))
            {
                var path = grouping.Key;
                logger.Push($"{Path.GetFileName(path)}");
                foreach (var g in grouping)
                {
                    var props = g.Properties.OrderBy(x => x.Key).ToArray();
                    logger.LogLine($"{g.ContextId} {g.Kind} {string.Join(" ", props.Select(x => $"{x.Key} = {x.Value}"))}");
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (randomizer.Campaign != Campaign.Leon)
                return;

            var enableGimmickModification = randomizer.GetConfigOption<bool>("ea-extra-gimmicks");
            var hidingLockers = randomizer.GetConfigOption<double>("gimmicks-hiding-lockers");
            var hidingTraps = randomizer.GetConfigOption<double>("gimmicks-traps");
            var explosionProbability = 5;

            var rng = randomizer.CreateRng();
            var randomItemSettings = new RandomItemSettings
            {
                ItemRatioKeyFunc = (dropKind) => randomizer.GetConfigOption<double>($"enemy-drop-ratio-{dropKind}"),
                MinAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-min", 0.1),
                MaxAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-max", 1.0),
                MinMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-min", 100),
                MaxMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-max", 1000),
            };

            // Get all gimmicks and modify
            var gimmicks = GetAllGimmicks(randomizer);

            // Removal
            if (enableGimmickModification)
            {
                gimmicks = RemoveSomeGimmicks(gimmicks, rng, hidingLockers, GimmickKinds.HidingLocker);
                gimmicks = RemoveSomeGimmicks(gimmicks, rng, hidingTraps, GimmickKinds.BearTrap, GimmickKinds.TripWire);
            }

            // Modification
            foreach (var g in gimmicks)
            {
                switch (g.Kind)
                {
                    case GimmickKinds.Crow:
                        RandomizeGmOptionDropItem(randomizer, g, randomItemSettings, rng);
                        break;
                    case GimmickKinds.HidingLocker:
                        FixHidingLocker(g);
                        break;
                    case GimmickKinds.WoodenBarrel:
                    case GimmickKinds.WoodenBox:
                    case GimmickKinds.SmallWoodenBox:
                    case GimmickKinds.Vase:
                        if (enableGimmickModification)
                        {
                            // Exclude wooden box and barrel in factory (no gun)
                            if (g.ContextId == new ContextId(1, 0, 12, 1750) ||
                                g.ContextId == new ContextId(1, 0, 12, 1053))
                                continue;

                            if (rng.NextProbability(explosionProbability))
                            {
                                AddExplosion(g);
                            }
                        }
                        break;
                }
            }

            // Save all gimmick files
            var files = gimmicks.Select(x => x.GimmickFile).Distinct();
            foreach (var f in files)
            {
                f.Save();
            }
        }

        private static ImmutableArray<Gimmick> GetAllGimmicks(ChainsawRandomizer randomizer)
        {
            var areaRepo = randomizer.Campaign == Campaign.Leon
                ? AreaDefinitionRepository.Leon
                : AreaDefinitionRepository.Ada;

            var gimmickFiles = areaRepo.Gimmicks
                .Select(x => new GimmickFile(randomizer, x))
                .ToArray();

            var gimmicks = gimmickFiles
                .SelectMany(x => x.Gimmicks)
                .ToImmutableArray();

            return gimmicks;
        }

        private static ImmutableArray<Gimmick> RemoveSomeGimmicks(ImmutableArray<Gimmick> gimmicks, Rng rng, double amount, params string[] kinds)
        {
            var shuffledGimmicks = gimmicks
                .Where(x => kinds.Contains(x.Kind))
                .Shuffle(rng)
                .ToArray();
            var count = shuffledGimmicks.Length;
            var removeCount = (int)(Math.Clamp((1 - amount), 0, 1) * count);
            if (removeCount == 0)
                return gimmicks;

            var remove = shuffledGimmicks.Take(removeCount).ToArray();
            foreach (var g in remove)
            {
                g.Remove();
            }
            return gimmicks.Except(remove).ToImmutableArray();
        }

        private static void FixHidingLocker(Gimmick g)
        {
            if (g.GameObject.GetChildren().FirstOrDefault(x => x.Name == "ParamObject") is ScnFile.GameObjectData paramObject)
            {
                g.GimmickFile.Scene.RemoveGameObject(paramObject);
            }
        }

        private static void AddExplosion(Gimmick g)
        {
            switch (g.Kind)
            {
                case GimmickKinds.WoodenBarrel:
                    ReplaceGimmick(g, "Biorand_WoodenBarrelExplosion");
                    break;
                case GimmickKinds.WoodenBox:
                    ReplaceGimmick(g, "Biorand_WoodenBoxExplosion");
                    break;
                case GimmickKinds.SmallWoodenBox:
                    ReplaceGimmick(g, "Biorand_SmallWoodenBoxExplosion");
                    break;
                case GimmickKinds.Vase:
                    ReplaceGimmick(g, "Biorand_VaseExplosion");
                    break;
            }
        }

        private static void ReplaceGimmick(Gimmick original, string kind)
        {
            var scene = original.GimmickFile.Scene;
            var gimmick = scene.ImportGameObject(GimmickTemplate.Get(kind));
            gimmick.Components.Remove(gimmick.FindComponent("via.Transform")!);
            gimmick.Components.Remove(gimmick.FindComponent("chainsaw.GimmickCore")!);
            gimmick.Components.Insert(0, (RszInstance)original.GameObject.FindComponent("via.Transform")!.Clone());
            gimmick.Components.Insert(1, (RszInstance)original.GameObject.FindComponent("chainsaw.GimmickCore")!.Clone());
            scene.RemoveGameObject(original.GameObject);
        }

        private static void RandomizeGmOptionDropItem(ChainsawRandomizer randomizer, Gimmick g, RandomItemSettings randomItemSettings, Rng rng)
        {
            var itemRandomizer = randomizer.ItemRandomizer;
            var paramObject = g.GameObject.GetChildren().FirstOrDefault(x => x.Name == "ParamObject");
            if (paramObject != null)
            {
                var gmOptionDropItem = paramObject.FindComponent("chainsaw.GmOptionDropItem");
                if (gmOptionDropItem != null)
                {
                    if (itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings) is Item drop)
                    {
                        gmOptionDropItem.Set("ID", drop.Id);
                        gmOptionDropItem.Set("Count", drop.Count);
                    }
                }
            }
        }

        private static void SetChickenDropItem(Gimmick gimmick, Item item)
        {
            var gmChicken = gimmick.GameObject.FindComponent("chainsaw.GmChicken");
            if (gmChicken != null)
            {
                var paramObject = gimmick.GameObject
                    .GetChildren()
                    .FirstOrDefault(x => x.Name == "ParamObject");
                if (paramObject != null)
                {
                    var gmOptionDropItem = paramObject.FindComponent("chainsaw.GmOptionDropItem");
                    if (gmOptionDropItem != null)
                    {
                        gmChicken.Set("_LaysEgg", true);
                        gmOptionDropItem.Set("ID", item.Id);
                        gmOptionDropItem.Set("Count", item.Count);
                    }
                }
            }
        }

        private class GimmickFile
        {
            private readonly ChainsawRandomizer _randomizer;

            public string Path { get; }
            public ScnFile Scene { get; }
            public ImmutableArray<Gimmick> Gimmicks { get; private set; }

            public GimmickFile(ChainsawRandomizer randomizer, string path)
            {
                _randomizer = randomizer;
                Scene = randomizer.FileRepository.GetScnFile(path);

                Path = path;
                Gimmicks = GetGimmicksFromScn(Scene);
            }

            public void Remove(Gimmick gimmick)
            {
                Scene.RemoveGameObject(gimmick.GameObject);
                Gimmicks = Gimmicks.Remove(gimmick);
            }

            public void Save()
            {
                _randomizer.FileRepository.SetScnFile(Path, Scene);
            }

            private ImmutableArray<Gimmick> GetGimmicksFromScn(ScnFile scnFile)
            {
                var result = ImmutableArray.CreateBuilder<Gimmick>();
                var gameObjects = scnFile.IterAllGameObjects(false).ToArray();
                foreach (var go in gameObjects)
                {
                    var coreComponent = go.FindComponent("chainsaw.GimmickCore");
                    if (coreComponent == null)
                        continue;

                    var gimmick = new Gimmick(this, go);
                    if (gimmick != null)
                    {
                        result.Add(gimmick);
                    }
                }
                return result.ToImmutable();
            }
        }

        private class Gimmick(GimmickFile gimmickFile, ScnFile.GameObjectData gameObject)
        {
            public GimmickFile GimmickFile { get; } = gimmickFile;
            public ScnFile.GameObjectData GameObject { get; } = gameObject;
            public string Kind => GetKindFromPrefab(GameObject);
            public ContextId ContextId => GetContextId(GameObject);
            public ImmutableDictionary<string, object> Properties => GetProperties();

            public void Remove()
            {
                GimmickFile.Remove(this);
            }

            private static IEnumerable<RszInstance> FindAllComponents(IGameObjectData gameObject, string name)
            {
                var component = gameObject.FindComponent(name);
                if (component != null)
                    yield return component;

                foreach (var child in gameObject.GetChildren())
                {
                    foreach (var c in FindAllComponents(child, name))
                        yield return c;
                }
            }

            private ImmutableDictionary<string, object> GetProperties()
            {
                var properties = new Dictionary<string, object>();
                if (GameObject.FindComponent("chainsaw.GmSmoothWoodBox") is RszInstance gmSmoothWoodBox)
                {
                    properties["DropCount"] = gmSmoothWoodBox.Get<int>("_RandomDropItemNum");
                }
                if (ParamObject is IGameObjectData paramObject)
                {
                    if (paramObject.FindComponent("chainsaw.GmOptionDropItem") is RszInstance gmOptionDropItem)
                    {
                        properties["Item"] = new Item(
                            gmOptionDropItem.Get<int>("ID"),
                            gmOptionDropItem.Get<int>("Count"));
                    }
                    if (paramObject.FindComponent("chainsaw.GmOptionSmoothWoodBox") is RszInstance gmOptionSmoothWoodBox)
                    {
                        var enemyContextId = ContextId.FromRsz(gmOptionSmoothWoodBox.Get<RszInstance>("_EnemyContextID")!);
                        if (enemyContextId.Category != -1)
                        {
                            properties["EnemyContextId"] = enemyContextId;
                        }
                        properties["NoDropWoodBoxB"] = gmOptionSmoothWoodBox.Get<bool>("_NoDropWoodBoxB"); ;
                    }
                }
                return properties.ToImmutableDictionary();
            }

            private IGameObjectData? ParamObject => GameObject.GetChildren().FirstOrDefault(x => x.Name == "ParamObject");

            private static ContextId GetContextId(IGameObjectData gameObject)
            {
                var coreComponent = gameObject.FindComponent("chainsaw.GimmickCore");
                if (coreComponent == null)
                    return default;

                return ContextId.FromRsz(coreComponent.Get<RszInstance>("_ID")!);
            }

            private static string GetKindFromPrefab(IGameObjectData gameObject)
            {
                if (gameObject is ScnFile.GameObjectData gameObjectData)
                {
                    if (gameObjectData.Prefab?.Path is string path)
                    {
                        return GetKindFromPrefab(path);
                    }
                }
                return "";
            }

            private static string GetKindFromPrefab(string prefab)
            {
                var shorten = Path.GetFileNameWithoutExtension(prefab);
                return shorten switch
                {
                    "gm03_002_00_0" => GimmickKinds.Crow,
                    "gm03_002_00_1" => GimmickKinds.Crow,
                    "gm03_000_02_0" => GimmickKinds.Chicken,
                    "gm84_500_00_0" => GimmickKinds.WoodenBarrel,
                    "gm84_502_00_0" => GimmickKinds.Typewriter,
                    "gm84_504_00_0" => GimmickKinds.TripWire,
                    "gm84_505_00_0" => GimmickKinds.WoodenBox,
                    "gm84_506_00_0" => GimmickKinds.SmallWoodenBox,
                    "gm84_515_00_0" => GimmickKinds.BearTrap,
                    "gm84_520_00_0" => GimmickKinds.Vase,
                    "gm84_521_00_0" => GimmickKinds.TableDrawer,
                    "gm84_567_00_0" => GimmickKinds.OilDrum,
                    "gm84_598_00_0" => GimmickKinds.MerchantTorch,
                    "gm84_623_00_0" => GimmickKinds.Merchant,
                    "gm84_855_00_0" => GimmickKinds.WoodenBarrel,
                    "gm84_859_00_0" => GimmickKinds.HidingLocker,
                    "gm84_899_00_0" => GimmickKinds.Ladder,
                    _ => shorten
                };
            }
        }
    }
}
