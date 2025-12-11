using System;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class BattleModifier : Modifier
    {
        private int _contextId;

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var chapter = 1;

            var areaService = randomizer.AreaService;
            var area = areaService.Areas
                .FirstOrDefault(x => x.Definition.ChapterOnly && x.Definition.Chapter == chapter);
            if (area == null)
                return;

            var flag = randomizer.FlagService.Allocate();
            // AddAreaHit(area, new Vector3(60.8f, 9.5f, -161.2f), 1.5f, flag);
            AddAreaHit(area, new Vector3(-248.1f, 6.5f, 55.7f), 1.5f, flag);
        }

        private void AddAreaHit(Area area, Vector3 position, float radius, Guid flag)
        {
            var contextId = AllocateContextId();

            var gimmick = GimmickTemplate
                .Get("Biorand_AreaHit")
                .Clone()
                .WithName("BioRand_AreaHit_1");

            var transform = new Transform(gimmick)
            {
                Position = position
            };
            gimmick = gimmick.AddOrUpdateComponent(transform.ToComponent());

            var gimmickCore = gimmick.FindComponent("chainsaw.GimmickCore")!;
            gimmickCore = gimmickCore.Set("_ID", contextId);
            gimmick = gimmick.AddOrUpdateComponent(gimmickCore);

            var colliders = gimmick.FindComponent("via.physics.Colliders")!;
            colliders = colliders.Set("Colliders[0].Shape.Radius", radius);
            gimmick = gimmick.AddOrUpdateComponent(colliders);

            var setFlagComponent = gimmick.FindComponent("chainsaw.SetFlagSettings")!;
            setFlagComponent = setFlagComponent.Set("_Params._Params[0]._SetFlags[0]._Flag", flag);
            gimmick = gimmick.AddOrUpdateComponent(setFlagComponent);

            area.Scene = area.Scene.Add(gimmick);
            area.GimmickSaveData.AddBasic(contextId);
        }

        private void AddDoorLock()
        {

        }

        private ContextID AllocateContextId()
        {
            return new ContextID
            {
                _Category = 5,
                _Kind = 0,
                _Group = 2,
                _Index = _contextId++
            };
        }
    }
}
