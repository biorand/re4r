using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class DropItemPlaceModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("random-items"))
                return;

            var itemService = randomizer.ItemService;
            var filePairs = ItemFilePair.GetPairs(randomizer);
            var dropItemTemplate = GimmickTemplate.Get("Biorand_DropItem");
            foreach (var placement in itemService.ItemPlacements)
            {
                if (placement.Campaign != randomizer.Campaign || !placement.IsExtra)
                    continue;

                var f = FindBestFile(filePairs, placement.Stage, placement.Tags.Contains(ItemTags.ChapterOnly));
                if (f == null)
                    continue;

                placement.ContextId = itemService.GetNextContextId();
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
                        .Set("_ID", placement.ContextId.ToRszValue())
                        .Set("_ItemData.StageID", placement.Stage));

                var userdata = new DropItemSaveDataTable.Data();
                userdata.ID = placement.ContextId.ToRszValue();
                userdata.ItemData.StageID = placement.Stage;

                f.AddItem(gameObject, userdata);
            }

            foreach (var f in filePairs)
            {
                f.Save();
            }
        }

        private ItemFilePair? FindBestFile(IEnumerable<ItemFilePair> files, int stage, bool chapterOnly)
        {
            ItemFilePair? best = null;
            var bestDiff = int.MaxValue;
            foreach (var f in files)
            {
                if (f.ChapterOnly != chapterOnly)
                    continue;

                if (!chapterOnly)
                {
                    var location = stage / 1000;
                    if (location != f.Location)
                        continue;
                }

                var items = f.GetItemDatas();
                var stageDiff = items.Any()
                    ? items.Min(x => Math.Abs(x.ItemData.StageID - stage))
                    : int.MaxValue;
                if (best == null || bestDiff > stageDiff)
                {
                    best = f;
                    bestDiff = stageDiff;
                }
            }
            return best;
        }
    }
}
