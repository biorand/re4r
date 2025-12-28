using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Cryptography;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            foreach (var area in randomizer.AreaService.Areas)
            {
                var gimmicks = area.Gimmicks.ToArray();
                if (gimmicks.Length == 0)
                    continue;

                logger.Push(area.FileName);
                foreach (var gameObject in gimmicks)
                {
                    var gimmick = new Gimmick(area, gameObject, null);
                    var position = gimmick.Transform.Position;
                    logger.LogLine(
                        gimmick.Guid,
                        gimmick.ContextId,
                        gimmick.Name,
                        gimmick.Kind,
                        position.X.ToString("0.0"),
                        position.Y.ToString("0.0"),
                        position.Z.ToString("0.0"));
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var enableGimmickModification = randomizer.GetConfigOption<bool>("ea-extra-gimmicks");
            var hidingLockers = randomizer.GetConfigOption<double>("gimmicks-hiding-lockers");
            var traps = randomizer.GetConfigOption<double>("gimmicks-traps");
            var explodingContainers = randomizer.GetConfigOption<double>("gimmicks-exploding-containers");

            var rng = randomizer.GetRng("modifier/gimmick");
            var randomItemSettings = new RandomItemSettings
            {
                ItemRatioKeyFunc = (dropKind) => randomizer.GetConfigOption<double>($"enemy-drop-ratio-{dropKind}"),
                MinAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-min", 0.1),
                MaxAmmoQuantity = randomizer.GetConfigOption("enemy-drop-ammo-max", 1.0),
                MinMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-min", 100),
                MaxMoneyQuantity = randomizer.GetConfigOption("enemy-drop-money-max", 1000),
            };

            // Get all gimmicks and modify
            var gimmickService = randomizer.GimmickService;
            var gimmicks = randomizer.AreaService.Areas
                .SelectMany(area => area.Gimmicks.Select(gameObject => new Gimmick(area, gameObject, gimmickService.FromGuid(gameObject.Guid))))
                .ToImmutableArray();

            // Removal
            if (enableGimmickModification)
            {
                gimmicks = RemoveSomeGimmicks(gimmicks, rng, hidingLockers, GimmickKinds.HidingLocker);
                gimmicks = RemoveSomeGimmicks(gimmicks, rng, traps, GimmickKinds.BearTrap, GimmickKinds.TripWire);
            }
            gimmicks = RemoveCertainGimmicks(gimmicks);

            // Modification
            foreach (var g in gimmicks)
            {
                if (g.Placement is GimmickPlacement placement && placement.Vanilla)
                {
                    if (placement.X != 0 || placement.Y != 0 || placement.Z != 0)
                    {
                        var transform = g.Transform;
                        transform.Position = placement.Position;
                        transform.Eular = placement.Eular;
                        g.Transform = transform;
                    }
                }

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
                            if (g.Guid == new Guid("510ff2ca-9c59-445d-bdd1-aa7e9196fb54") ||
                                g.Guid == new Guid("c333c9be-4eae-4f1f-8bc1-bcfc0a9c2bbf"))
                                continue;

                            if (rng.NextProbability((int)Math.Round(Math.Clamp(explodingContainers, 0, 1) * 100)))
                            {
                                AddExplosion(g);
                            }
                        }
                        break;

                    case GimmickKinds.Door:
                    case GimmickKinds.BigDoor:
                        LockDoor(g);
                        break;
                    case GimmickKinds.FallShutter:
                        LockShutter(g);
                        break;
                }
            }
        }

        private static ImmutableArray<Gimmick> RemoveSomeGimmicks(ImmutableArray<Gimmick> gimmicks, Rng rng, double amount, params string[] kinds)
        {
            var shuffledGimmicks = gimmicks
                .Where(x => kinds.Contains(x.Kind) && x.Placement?.Tags.Contains(GimmickTags.Always) != true)
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
            return gimmicks.RemoveRange(remove);
        }

        private static ImmutableArray<Gimmick> RemoveCertainGimmicks(ImmutableArray<Gimmick> gimmicks)
        {
            var remove = gimmicks
                .Where(x => x.Placement?.Tags.Contains(GimmickTags.Never) == true)
                .ToArray();
            foreach (var g in remove)
            {
                g.Remove();
            }
            return gimmicks.RemoveRange(remove);
        }

        private static void FixHidingLocker(Gimmick g)
        {
            if (g.ParamObject is RszGameObject paramObject)
            {
                paramObject = paramObject.WithComponents(
                    paramObject.Components
                        .RemoveAll(x => x.Type.Name == "chainsaw.CheckFlagSettings"));
                g.Area.Scene = g.Area.Scene.UpdateGameObject(paramObject);
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
            original.Area.Scene = original.Area.Scene
                .RemoveGameObject(original.GameObject.Guid)
                .Add(GimmickTemplate
                    .Get(kind)
                    .AddOrUpdateComponent(original.GameObject.FindComponent("via.Transform")!)
                    .AddOrUpdateComponent(original.GameObject.FindComponent("chainsaw.GimmickCore")!));
        }

        private static void RandomizeGmOptionDropItem(ChainsawRandomizer randomizer, Gimmick g, RandomItemSettings randomItemSettings, Rng rng)
        {
            var itemRandomizer = randomizer.ItemRandomizer;
            var paramObject = g.ParamObject;
            if (paramObject != null)
            {
                var gmOptionDropItem = paramObject.FindComponent("chainsaw.GmOptionDropItem");
                if (gmOptionDropItem != null)
                {
                    if (itemRandomizer.GetNextGeneralDrop(rng, randomItemSettings) is Item drop)
                    {
                        g.Area.Scene = g.Area.Scene.UpdateGameObject(
                            paramObject.AddOrUpdateComponent(gmOptionDropItem
                                .Set("ID", drop.Id)
                                .Set("Count", drop.Count)));
                    }
                }
            }
        }

        private static void SetChickenDropItem(Gimmick gimmick, Item item)
        {
            var gmChicken = gimmick.GameObject.FindComponent("chainsaw.GmChicken");
            if (gmChicken != null)
            {
                var paramObject = gimmick.ParamObject;
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

        private static void LockShutter(Gimmick g)
        {
            if (g.Placement == null)
                return;

            var paramObject = g.ParamObject;
            if (paramObject == null)
                return;

            var lockFlags = g.Placement.Param1.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();
            var unlockFlags = g.Placement.Param2.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();

            g.Area.Scene = g.Area.Scene.UpdateGameObject(
                paramObject.AddOrUpdateComponent(g.Area.Randomizer.FileRepository.TypeRepository.Serialize(new chainsaw.CheckFlagSettings()
                {
                    Enabled = true,
                    _Params = new OptionSettings<CheckFlagSettings.Param>()
                    {
                        _Params =
                            [
                                new chainsaw.CheckFlagSettings.Param()
                                    {
                                        _KeyHash = (uint)MurMur3.HashData("Fall"),
                                        _BindTriggerNameHash = (uint)MurMur3.HashData(""),
                                        _FlagCondition = new FlagCondition()
                                        {
                                            _CheckFlags =
                                            [
                                                .. lockFlags.Select(x => new CheckFlagInfo()
                                                {
                                                    _CheckFlag = x,
                                                    _CompareValue = true
                                                })
                                            ]
                                        }
                                    },
                                    new chainsaw.CheckFlagSettings.Param()
                                    {
                                        _KeyHash = (uint)MurMur3.HashData("Open"),
                                        _BindTriggerNameHash = (uint)MurMur3.HashData(""),
                                        _FlagCondition = new FlagCondition()
                                        {
                                            _CheckFlags =
                                            [
                                                .. unlockFlags.Select(x => new CheckFlagInfo()
                                                {
                                                    _CheckFlag = x,
                                                    _CompareValue = true
                                                })
                                            ]
                                        }
                                    }
                            ]
                    }
                })));
        }

        private static void LockDoor(Gimmick g)
        {
            if (g.Placement == null)
                return;

            var paramObject = g.ParamObject;
            if (paramObject == null)
                return;

            var lockFlags = g.Placement.Param1.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();
            var unlockFlags = g.Placement.Param2.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();

            var repo = g.Area.Randomizer.FileRepository.TypeRepository;
            var isBigDoor = g.Kind == "GmBigDoor";
            var lockComponentName = isBigDoor ? "chainsaw.GmOptionBigDoorLock" : "chainsaw.GmOptionDoorLock";
            var gmOptionDoorLock = paramObject.FindComponent(lockComponentName);
            gmOptionDoorLock ??= repo.Create(lockComponentName);
            gmOptionDoorLock = gmOptionDoorLock.Set("Enabled", true);

            var lockRule = gmOptionDoorLock.Get<RszArrayNode>("LockRule");

            for (var i = 0; i < lockRule.Length; i++)
            {
                if (!lockRule[i].Get<bool>("Value"))
                {
                    lockRule = lockRule.RemoveAt(i);
                    i--;
                }
            }

            lockRule = lockRule.Add(RszSerializer.Serialize(
                repo.FromName("chainsaw.RuleStratum.StratumBool")!,
                new chainsaw.RuleStratum.StratumBool()
                {
                    Value = true,
                    _Enable = new chainsaw.RuleStratum.Rule()
                    {
                        Logic = 0,
                        Matters =
                        [
                            new chainsaw.RuleStratum.Container()
                            {
                                _Data = new chainsaw.RuleStratum.ParticleFlag()
                                {
                                    Flags = new FlagCondition()
                                    {
                                        _Logic = 0,
                                        _CheckFlags =
                                        [
                                            .. lockFlags.Select(x => new CheckFlagInfo()
                                            {
                                                _CheckFlag = x,
                                                _CompareValue = true
                                            })
                                        ]
                                    }
                                }
                            },
                            new chainsaw.RuleStratum.Container()
                            {
                                _Data = new chainsaw.RuleStratum.ParticleFlag()
                                {
                                    Flags = new FlagCondition()
                                    {
                                        _Logic = 1,
                                        _CheckFlags =
                                        [
                                            .. unlockFlags.Select(x => new CheckFlagInfo()
                                            {
                                                _CheckFlag = x,
                                                _CompareValue = false
                                            })
                                        ]
                                    }
                                }
                            }
                        ]
                    }
                }));
            gmOptionDoorLock = gmOptionDoorLock.Set("LockRule", lockRule);
            paramObject = paramObject.AddOrUpdateComponent(gmOptionDoorLock);
            g.Area.Scene = g.Area.Scene.UpdateGameObject(paramObject);
        }

        [DebuggerDisplay("{Name}")]
        private class Gimmick(Area area, RszGameObject gameObject, GimmickPlacement? placement)
        {
            public Area Area => area;
            public RszGameObject GameObject => gameObject;
            public GimmickPlacement? Placement => placement;

            public string Name => GameObject.Name;
            public Guid Guid => GameObject.Guid;
            public string Kind => DetectKind();
            public chainsaw.ContextID ContextId => GetContextId(GameObject);
            public Transform Transform
            {
                get => new(GameObject);
                set => Area.Scene = Area.Scene.UpdateGameObject(GameObject.AddOrUpdateComponent(value.ToComponent()));
            }
            public ImmutableDictionary<string, object> Properties => GetProperties();

            public void Remove()
            {
                area.Scene = area.Scene.RemoveGameObject(Guid);
            }

            private ImmutableDictionary<string, object> GetProperties()
            {
                var properties = new Dictionary<string, object>();
                if (GameObject.FindComponent("chainsaw.GmSmoothWoodBox") is RszObjectNode gmSmoothWoodBox)
                {
                    properties["DropCount"] = gmSmoothWoodBox.Get<int>("_RandomDropItemNum");
                }
                if (ParamObject is RszGameObject paramObject)
                {
                    if (paramObject.FindComponent("chainsaw.GmOptionDropItem") is RszObjectNode gmOptionDropItem)
                    {
                        properties["Item"] = new Item(
                            gmOptionDropItem.Get<int>("ID"),
                            gmOptionDropItem.Get<int>("Count"));
                    }
                    if (paramObject.FindComponent("chainsaw.GmOptionSmoothWoodBox") is RszObjectNode gmOptionSmoothWoodBox)
                    {
                        var enemyContextId = gmOptionSmoothWoodBox.Get<chainsaw.ContextID>("_EnemyContextID");
                        if (enemyContextId._Category != -1)
                        {
                            properties["EnemyContextId"] = enemyContextId;
                        }
                        properties["NoDropWoodBoxB"] = gmOptionSmoothWoodBox.Get<bool>("_NoDropWoodBoxB"); ;
                    }
                }
                return properties.ToImmutableDictionary();
            }

            public RszGameObject? ParamObject => GameObject.FindGameObject("ParamObject");

            private static chainsaw.ContextID GetContextId(RszGameObject gameObject)
            {
                var coreComponent = gameObject.FindComponent("chainsaw.GimmickCore");
                if (coreComponent == null)
                    return new chainsaw.ContextID();

                return coreComponent.Get<chainsaw.ContextID>("_ID");
            }

            private string DetectKind()
            {
                if (GetKindFromPrefab(GameObject.Prefab) is string kind)
                    return kind;

                foreach (var component in GameObject.Components)
                {
                    if (GetKindFromComponent(component.Type.Name) is string componentKind)
                    {
                        return componentKind;
                    }
                }

                return Path.GetFileNameWithoutExtension(GameObject.Prefab ?? Name);
            }

            private static string? GetKindFromComponent(string componentType)
            {
                if (componentType.StartsWith("chainsaw.Gm"))
                {
                    return componentType.Substring(9);
                }
                return null;
            }

            private static string? GetKindFromPrefab(string? prefab)
            {
                var shorten = Path.GetFileNameWithoutExtension(prefab ?? "");
                return shorten switch
                {
                    "gm84_500_00_0" => GimmickKinds.WoodenBarrel,
                    "gm84_505_00_0" => GimmickKinds.WoodenBox,
                    "gm84_506_00_0" => GimmickKinds.SmallWoodenBox,
                    "gm84_520_00_0" => GimmickKinds.Vase,
                    "gm84_521_00_0" => GimmickKinds.TableDrawer,
                    "gm84_598_00_0" => GimmickKinds.MerchantTorch,
                    "gm84_855_00_0" => GimmickKinds.WoodenBarrel,
                    "gm84_899_00_0" => GimmickKinds.Ladder,
                    "gm91_300_00_0" => GimmickKinds.HookShot,
                    _ => null
                };
            }
        }
    }
}
