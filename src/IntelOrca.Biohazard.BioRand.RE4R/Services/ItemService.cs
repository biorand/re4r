using System;
using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class ItemService
    {
        private readonly Dictionary<Guid, ItemPlacement> _guidToItemPlacement;

        public List<ItemPlacement> ItemPlacements { get; private set; }

        public ItemService(ChainsawRandomizer randomizer)
        {
            var itemsCsv = randomizer.DynamicData.GetData(DynamicDataName.Items) ?? throw new Exception("Unable to get item data");
            ItemPlacements = Csv.Deserialize<ItemPlacement>(itemsCsv)
                .Where(x => x.Chapter != 0)
                .ToList();

            _guidToItemPlacement = ItemPlacements.ToDictionary(x => x.GuidOrAuto);
        }

        public ItemPlacement? FromGuid(Guid guid)
        {
            return _guidToItemPlacement.GetValueOrDefault(guid);
        }
    }
}
