namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class DropTableModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;

            var files = new string[]
            {
                "droptableuserdata_commonboxenemy.user.2",
                "droptableuserdata_commonegg.user.2",
                "droptableuserdata_crow.user.2",
                "droptableuserdata_emd3z0_2.user.2",
                "droptableuserdata_emf6z0shell.user.2"
            };

            foreach (var f in files)
            {
                var path = $"natives/stm/_chainsaw/leveldesign/table/dropitem/randomtable/{f}";
                logger.Push(path);
                var table = randomizer.FileRepository.DeserializeUserFile<chainsaw.RandomDrop.CommonDropTableUserdata>(path);
                foreach (var g in table.DropGroups)
                {
                    var item = itemRepo.Find(g.ItemID);
                    var itemName = item?.ToString() ?? $"ITEM_{g.ItemID}";
                    logger.Push(itemName, g.DropType, g.PointType, g.MinPoint, g.MaxPoint, g.CalcOption);
                    foreach (var condition in g.Conditions)
                    {
                        logger.LogLine("CONDITION");
                    }
                    foreach (var block in g.Blocks)
                    {
                        logger.Push($"BLOCK {block.PointLevel}");
                        foreach (var box in block.Boxes)
                        {
                            logger.LogLine("BOX", box.Weight, box.Item.Count, box.Item.AmmoCount, box.Item.Durability);
                        }
                        logger.Pop();
                    }
                    logger.Pop();
                }
                logger.Pop();
            }
        }
    }
}
