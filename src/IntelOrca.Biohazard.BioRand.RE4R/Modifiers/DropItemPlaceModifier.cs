using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class DropItemPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var areaService = randomizer.AreaService;
            var itemService = randomizer.ItemService;
            var dropItemTemplate = GimmickTemplate.Get("Biorand_DropItem");
            foreach (var placement in itemService.ItemPlacements)
            {
                if (placement.Campaign != randomizer.Campaign || !placement.IsExtra || placement.Chapter == -1)
                    continue;

                var area = FindBestArea(areaService.Areas, placement.Stage, placement.Tags.Contains(ItemTags.ChapterOnly));
                if (area == null)
                    continue;

                placement.ContextId = randomizer.FlagService.AllocateContextId(2, 3);
                var transform = new Transform
                {
                    Position = new Vector3(placement.X, placement.Y, placement.Z),
                    Eular = new EulerAngles(placement.Yaw, placement.Pitch, placement.Roll),
                    Scale = Vector3.One
                };

                var gameObject = dropItemTemplate.Clone();
                gameObject = gameObject.WithName($"Biorand_DropItem_{placement.Row}");
                gameObject = gameObject.WithGuid(placement.GuidOrAuto);
                gameObject = gameObject.AddOrUpdateComponent(transform.ToComponent());
                gameObject = gameObject.AddOrUpdateComponent(
                    gameObject
                        .FindComponent("chainsaw.DropItem")!
                        .Set("_ID", placement.ContextId)
                        .Set("_ItemData.StageID", placement.Stage));

                var userdata = new chainsaw.DropItemSaveDataTable.Data();
                userdata.ID = placement.ContextId;
                userdata.ItemData.StageID = placement.Stage;

                area.Scene = area.Scene.Add(gameObject);
                area.ItemSaveData.Add(userdata);
            }
        }

        private Area? FindBestArea(IEnumerable<Area> areas, int stage, bool chapterOnly)
        {
            Area? best = null;
            var bestDiff = int.MaxValue;
            foreach (var area in areas)
            {
                if (area.Definition.Kind != AreaKind.Items)
                    continue;

                if (area.Definition.ChapterOnly != chapterOnly)
                    continue;

                if (!chapterOnly)
                {
                    var location = stage / 1000;
                    if (location != area.Definition.Location)
                        continue;
                }

                var items = area.ItemSaveData.Items;
                var stageDiff = items.Any()
                    ? items.Min(x => Math.Abs(x.ItemData.StageID - stage))
                    : int.MaxValue;
                if (best == null || bestDiff > stageDiff)
                {
                    best = area;
                    bestDiff = stageDiff;
                }
            }
            return best;
        }
    }
}
