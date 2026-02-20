using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var gimmicks = randomizer.DynamicData.GetData(DynamicDataName.Gimmicks) ?? throw new Exception("Failed to get gimmick data");
            var placements = randomizer.GimmickService.GimmickPlacements
                .Where(x => x.Campaign == randomizer.Campaign && x.Chapter != -1)
                .ToImmutableArray();

            var rng = randomizer.GetRng("modifier/gimmickplace");

            var bawk = randomizer.HasSpecialTouch("bawk");
            var extraMerchants = randomizer.GetConfigOption("extra-merchants", true);
            var enableGimmicks = randomizer.GetConfigOption("ea-extra-gimmicks", false);
            var numBreakableContainers = randomizer.GetConfigOption<double>("gimmicks-breakable-containers", 1);

            var factory = new GimmickFactory(randomizer);

            if (!bawk)
                placements = placements.RemoveAll(x => x.Kind == "bawk");

            if (enableGimmicks)
            {
                placements = TakeRandomGimmicks(placements, rng, numBreakableContainers, "Biorand_SmallWoodenBox", "Biorand_WoodenBox", "Biorand_WoodenBarrel", "Biorand_Vase");
            }
            else
            {
                var allowed = new[]
                {
                    "bawk",
                    "Biorand_Merchant",
                    "Biorand_MerchantTorch",
                    "Biorand_Typewriter",
                    "Biorand_TableDrawer"
                };
                placements = placements
                    .Where(x => allowed.Contains(x.Kind))
                    .ToImmutableArray();
            }

            if (!extraMerchants)
            {
                placements = placements
                    .Where(x => x.Kind != "Biorand_Merchant" && x.Kind != "Biorand_MerchantTorch")
                    .ToImmutableArray();
            }

            foreach (var placement in placements)
            {
                if (placement.Vanilla)
                    continue;

                factory.AddGimmick(rng, placement, logger);
            }
        }

        private static ImmutableArray<GimmickPlacement> TakeRandomGimmicks(ImmutableArray<GimmickPlacement> placements, Rng rng, double amount, params string[] kinds)
        {
            var maybe = placements.Where(x => !x.Tags.Contains(GimmickTags.Always)).ToArray();
            var breakables = maybe.Where(x => kinds.Contains(x.Kind)).Shuffle(rng).ToArray();
            var remove = breakables.Take((int)(Math.Clamp(1 - amount, 0, 1) * breakables.Length)).ToArray();
            if (remove.Length == 0)
                return placements;
            return placements.Except(remove).ToImmutableArray();
        }

        private class GimmickFactory(ChainsawRandomizer randomizer)
        {
            private readonly Dictionary<int, Area> _stageToArea = new();

            public FileRepository FileRepository => randomizer.FileRepository;

            private Area GetScnForStage(int stage)
            {
                if (!_stageToArea.TryGetValue(stage, out var area))
                {
                    _stageToArea[stage] = area = randomizer.AreaService.Areas
                        .Where(x => x.Definition.Kind == AreaKind.Gimmicks)
                        .Where(x => x.Definition.Location == (stage / 1000))
                        .OrderBy(x => Math.Abs((x.Definition.Stage ?? 0) - stage))
                        .First();
                }
                return area;
            }

            public void AddGimmick(Rng rng, GimmickPlacement placement, RandomizerLogger logger)
            {
                var contextId = randomizer.FlagService.AllocateContextId(5, 1, 50_000);
                var area = GetScnForStage(placement.Stage);

                var kind = placement.Kind;
                if (kind == "bawk") kind = "Biorand_Chicken";

                var gimmick = CloneGimmickFromTemplate(kind, rng);
                gimmick = gimmick
                    .WithName($"{gimmick.Name}_{placement.Row}")
                    .WithGuid(placement.GuidOrAuto);

                var gimmickComponent = gimmick.FindComponent("chainsaw.GimmickCore");
                if (gimmickComponent != null)
                {
                    gimmick = gimmick.AddOrUpdateComponent(gimmickComponent
                        .Set("_ID", contextId));
                }

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("via.Transform")!
                    .Set("Position", placement.Position)
                    .Set("Rotation", placement.Eular.ToQuaternion())
                    .Set("Scale", Vector3.One));

                if (kind == "Biorand_SW_StoneCrusher")
                {
                    var component = gimmick.FindComponent("chainsaw.GmStonePestle");
                    if (component != null)
                    {
                        var capsule = via.Capsule.FromNode(component["_SensorForCamera"]);
                        capsule.Start += placement.Position;
                        capsule.End += placement.Position;
                        gimmick = gimmick.AddOrUpdateComponent(
                            component.SetField("_SensorForCamera", capsule.ToNode()));
                    }

                    var triggerFlags = placement.Param1.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToArray();
                    if (triggerFlags.Length > 0)
                    {
                        gimmick = gimmick.AddOrUpdateComponent(randomizer.FileRepository.TypeRepository.Serialize(new chainsaw.CheckFlagSettings()
                        {
                            Enabled = true,
                            _Params = new OptionSettings<CheckFlagSettings.Param>()
                            {
                                _Params =
                                [
                                    new chainsaw.CheckFlagSettings.Param()
                                {
                                    _KeyHash = 236162618,
                                    _BindTriggerNameHash = 2180083513,
                                    _FlagCondition = new FlagCondition()
                                    {
                                        _CheckFlags =
                                        [
                                            ..triggerFlags.Select(x => new CheckFlagInfo()
                                            {
                                                _CheckFlag = x,
                                                _CompareValue = true
                                            })
                                        ]
                                    }
                                }
                                ]
                            }
                        }));
                    }
                }

                gimmick = AddCondition(gimmick, placement);

                if (kind == "Biorand_BearTrap")
                {
                    area.GimmickSaveData.AddExtended("chainsaw.GmContextLegHoldTrap", contextId, "LegHoldTrap", 43300, placement.Position);
                }
                else if (kind == "Biorand_FallenShelves")
                {
                    area.GimmickSaveData.Add(new chainsaw.GimmickSaveDataTable.Data()
                    {
                        ID = contextId,
                        Save = new chainsaw.GimmickContext.SaveData()
                        {
                            Attr = [0, 0, 0, 0]
                        },
                        AIMapData =
                        [
                            new chainsaw.GmContextAIMapEff.ShapeData()
                            {
                                Position = placement.Position,
                                RotationY = placement.Yaw,
                                ShapeName = "FallenShelf",
                                Stage = [placement.Stage]
                            }
                        ],
                        ContextType = "chainsaw.GmContextAIMapEff"
                    });
                }
                else if (kind == "Biorand_Ladder1" || kind == "Biorand_Ladder2")
                {
                    var leaningLadder = gimmick.FindComponent("chainsaw.GmLeaningLadder")!;
                    leaningLadder = leaningLadder
                        .Set("_StageBottom", placement.Stage)
                        .Set("_StageTop", placement.Stage);
                    gimmick = gimmick.AddOrUpdateComponent(leaningLadder);
                    area.GimmickSaveData.Add(new chainsaw.GimmickSaveDataTable.Data()
                    {
                        ID = contextId,
                        Save = new chainsaw.GimmickContext.SaveData()
                        {
                            Attr = [0, 0, 0, 0]
                        },
                        Static = new chainsaw.GmContextLadder.StaticDataLadder()
                        {
                            PointTop = new chainsaw.GmContextLadder.StaticDataLadder.Point()
                            {
                                Position = placement.Position + new Vector3(0, 4.228616319f, 0),
                                Rotation = placement.Yaw,
                                Stage = placement.Stage
                            },
                            PointBottom = new chainsaw.GmContextLadder.StaticDataLadder.Point()
                            {
                                Position = placement.Position,
                                Rotation = placement.Yaw,
                                Stage = placement.Stage
                            }
                        },
                        AccessPoints =
                        [
                            new chainsaw.GimmickManager.AccessPoint()
                            {
                                Position = placement.Position,
                                Access = 1
                            },
                            new chainsaw.GimmickManager.AccessPoint()
                            {
                                Position = placement.Position + new Vector3(0, 4.228616319f, 0),
                                Access = 1
                            }
                        ],
                        ContextType = "chainsaw.GmContextLadder"
                    });
                }
                else
                {
                    var contextType = kind == "Biorand_AshleyLocker" ? "chainsaw.GmContextHidingLocker" : "";
                    area.GimmickSaveData.AddBasic(contextType, contextId);
                }
                area.Scene = area.Scene.Add(gimmick);
                randomizer.AreaService.AddGuidToArea(gimmick.Guid, area);
                logger.LogLine(area.Path, gimmick.Guid, gimmick.Name, placement.Stage, placement.X, placement.Y, placement.Z);
            }

            private RszGameObject AddCondition(RszGameObject gimmick, GimmickPlacement placement)
            {
                if (placement.Condition == default && placement.Chapter == 0)
                    return gimmick;

                var factory = randomizer.GetService<RszFactory>();
                var repo = FileRepository.TypeRepository;
                var paramObject = gimmick.FindGameObject("ParamObject");
                if (paramObject == null)
                {
                    paramObject = factory.CreateGameObject("ParamObject", "", [
                        factory .CreateTransform(),
                        repo.Create("chainsaw.ParamObject")
                            .Set("Enabled", true)]);
                }

                var stratumBool = repo
                    .Create("chainsaw.RuleStratum.StratumBool")
                    .Set("Value", true)
                    .Set("_Enable.Logic", 1);

                if (placement.Condition != default)
                {
                    stratumBool = stratumBool.Set("_Enable.Matters", stratumBool
                        .Get<RszArrayNode>("_Enable.Matters")
                        .Add(repo
                            .Create("chainsaw.RuleStratum.Container")
                            .Set("_Data", repo
                                .Create("chainsaw.RuleStratum.ParticleFlag")
                                .Set("Flags", new chainsaw.FlagCondition()
                                {
                                    _CheckFlags =
                                    {
                                        new chainsaw.CheckFlagInfo()
                                        {
                                            _CheckFlag = placement.Condition,
                                            _CompareValue = false
                                        }
                                    }
                                }))));
                }
                if (placement.Chapter != 0)
                {
                    var chapter = randomizer.GetService<CampaignService>().GetChapter(placement.Chapter);
                    stratumBool = stratumBool.Set("_Enable.Matters", stratumBool
                        .Get<RszArrayNode>("_Enable.Matters")
                        .Add(repo
                            .Create("chainsaw.RuleStratum.Container")
                            .Set("_Data", repo
                                .Create("chainsaw.RuleStratum.ParticleChapter")
                                .Set("Compare", 1)
                                .Set("Chapter", chapter.Id))));
                }

                paramObject = paramObject.AddOrUpdateComponent(repo
                    .Create("chainsaw.GmOptionHide")
                    .Set("Enabled", true)
                    .Set("Rule", new[] { stratumBool }));

                if (!gimmick.Name.StartsWith("Biorand_Merchant_"))
                {
                    paramObject = paramObject.AddOrUpdateComponent(repo
                        .Create("chainsaw.ObjectHide")
                        .Set("Enabled", true)
                        .Set("Settings", new[] { stratumBool }));
                }

                return gimmick.AddOrUpdateChild(paramObject);
            }

            private RszGameObject CloneGimmickFromTemplate(string kind, Rng rng)
            {
                var map = new Dictionary<Guid, Guid>();

                // Create new guids for all game objects
                var root = randomizer.GetService<GimmickTemplate>()
                    .Get(kind)
                    .VisitGameObjects(gameObject =>
                    {
                        // Change to new guid (keep map of old to new)
                        var newGuid = rng.NextGuid();
                        map[gameObject.Guid] = newGuid;
                        return gameObject.WithGuid(newGuid);
                    });

                // Fix references
                return root.Visit(node =>
                {
                    if (node is RszValueNode valueNode && valueNode.Type == RszFieldType.GameObjectRef)
                    {
                        var refGuid = valueNode.Get<Guid>();
                        if (map.TryGetValue(refGuid, out var newGuid))
                        {
                            return RszSerializer.Serialize(RszFieldType.GameObjectRef, newGuid);
                        }
                    }
                    return node;
                });
            }
        }
    }
}
