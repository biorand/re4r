using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class GimmickPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var gimmicks = randomizer.DynamicData.GetData(DynamicDataName.Gimmicks) ?? throw new Exception("Failed to get gimmick data");
            var placements = Csv.Deserialize<GimmickPlacement>(gimmicks)
                .Select((x, i) => { x.Id = i + 2; return x; })
                .Where(x => x.Campaign == randomizer.Campaign)
                .Where(x => x.Kind.Trim() is string s && !string.IsNullOrEmpty(s) && !s.StartsWith('#'))
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
                factory.AddGimmick(rng, placement);
            }
        }

        private static ImmutableArray<GimmickPlacement> TakeRandomGimmicks(ImmutableArray<GimmickPlacement> placements, Rng rng, double amount, params string[] kinds)
        {
            var breakables = placements.Where(x => kinds.Contains(x.Kind)).Shuffle(rng).ToArray();
            var remove = breakables.Take((int)(Math.Clamp(1 - amount, 0, 1) * breakables.Length)).ToArray();
            if (remove.Length == 0)
                return placements;
            return placements.Except(remove).ToImmutableArray();
        }

        private class GimmickFactory(ChainsawRandomizer randomizer)
        {
            private readonly Dictionary<int, Area> _stageToArea = new();
            private int _contextId = 50_000;

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

            public void AddGimmick(Rng rng, GimmickPlacement placement)
            {
                var contextId = GetNewContextId();
                var area = GetScnForStage(placement.Stage);

                var kind = placement.Kind;
                if (kind == "bawk") kind = "Biorand_Chicken";

                var gimmick = CloneGimmickFromTemplate(kind, rng);
                gimmick = gimmick.WithName($"{gimmick.Name}_{placement.Id}");

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("chainsaw.GimmickCore")!
                    .Set("_ID", contextId));

                gimmick = gimmick.AddOrUpdateComponent(gimmick
                    .FindComponent("via.Transform")!
                    .Set("Position", placement.Position)
                    .Set("Rotation", placement.Eular.ToQuaternion())
                    .Set("Scale", Vector3.One));

                gimmick = AddCondition(gimmick, placement);

                area.Scene = area.Scene.Add(gimmick);
                area.GimmickSaveData.AddBasic(contextId);
            }

            private RszGameObject AddCondition(RszGameObject gimmick, GimmickPlacement placement)
            {
                if (string.IsNullOrEmpty(placement.Condition) && placement.Chapter == 0)
                    return gimmick;

                var repo = FileRepository.TypeRepository;
                var paramObject = gimmick.FindGameObject("ParamObject");
                if (paramObject == null)
                {
                    paramObject = RszFactory.CreateGameObject("ParamObject", "", [
                        RszFactory.CreateTransform(),
                        repo.Create("chainsaw.ParamObject")
                            .Set("Enabled", true)]);
                }

                var stratumBool = repo
                    .Create("chainsaw.RuleStratum.StratumBool")
                    .Set("Value", true)
                    .Set("_Enable.Logic", 1);

                if (!string.IsNullOrEmpty(placement.Condition))
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
                                            _CheckFlag = Guid.Parse(placement.Condition),
                                            _CompareValue = false
                                        }
                                    }
                                }))));
                }
                if (placement.Chapter != 0)
                {
                    stratumBool = stratumBool.Set("_Enable.Matters", stratumBool
                        .Get<RszArrayNode>("_Enable.Matters")
                        .Add(repo
                            .Create("chainsaw.RuleStratum.Container")
                            .Set("_Data", repo
                                .Create("chainsaw.RuleStratum.ParticleChapter")
                                .Set("Compare", 1)
                                .Set("Chapter", ChapterId.FromNumber(randomizer.Campaign, placement.Chapter)))));
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

            private static RszGameObject CloneGimmickFromTemplate(string kind, Rng rng)
            {
                var map = new Dictionary<Guid, Guid>();

                // Create new guids for all game objects
                var root = GimmickTemplate
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

            private chainsaw.ContextID GetNewContextId()
            {
                return new chainsaw.ContextID()
                {
                    _Category = 5,
                    _Kind = 0,
                    _Group = 1,
                    _Index = _contextId++
                };
            }
        }

        private sealed class GimmickPlacement
        {
            public int Id { get; set; }
            public string Kind { get; set; } = "";
            public int Stage { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public float Yaw { get; set; }
            public float Pitch { get; set; }
            public float Roll { get; set; }
            public string Condition { get; set; } = "";
            public int Chapter { get; set; }
            public Campaign Campaign { get; set; }

            public Vector3 Position => new(X, Y, Z);
            public EulerAngles Eular => new(Yaw, Pitch, Roll);

            public override string ToString()
            {
                return $"{Id}_{Kind}";
            }
        }
    }
}
