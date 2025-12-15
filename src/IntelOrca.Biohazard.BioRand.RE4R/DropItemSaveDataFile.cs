using System.Collections.Generic;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class DropItemSaveDataFile
    {
        private chainsaw.DropItemSaveDataTable _table = new();
        private bool _dirty;

        public IPatchContext Context { get; }
        public string Path { get; }

        public DropItemSaveDataFile(IPatchContext context, string path)
        {
            Context = context;
            Path = path;

            if (context.Exists(path))
            {
                _table = context.DeserializeUserFile<chainsaw.DropItemSaveDataTable>(path);
            }
        }

        public void Apply()
        {
            if (!_dirty)
                return;

            var rszType = Context.TypeRepository.FromName("chainsaw.DropItemSaveDataTable")!;
            var userFile = Context.GetUserFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/item_cp10_chp1_1_itemdata.user.2").ToBuilder(Context.TypeRepository);
            userFile.Objects = [(RszObjectNode)RszSerializer.Serialize(rszType, _table)];
            Context.SetUserFile(Path, userFile.Build());
        }

        public IEnumerable<chainsaw.DropItemSaveDataTable.Data> Items => _table.Datas;

        public void Add(chainsaw.DropItemSaveDataTable.Data item)
        {
            _table.Datas.Add(item);
            _dirty = true;
        }

        public void Update(Dictionary<chainsaw.ContextID, Item> placements)
        {
            foreach (var data in _table.Datas)
            {
                if (placements.TryGetValue(data.ID, out var item))
                {
                    UpdateItem(data, item);
                    _dirty = true;
                }
            }
        }

        public void Remove(chainsaw.ContextID contextId)
        {
            _table.Datas.RemoveAll(x => x.ID == contextId);
        }

        private static void UpdateItem(chainsaw.DropItemSaveDataTable.Data data, Item newItem)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var newItemDef = itemRepo.Find(newItem.Id);
            if (newItemDef == null)
                return;

            ItemDefinition? ammoDefinition = null;
            if (newItemDef.Kind == ItemKinds.Weapon)
            {
                ammoDefinition = itemRepo.GetAmmo(newItemDef);
            }

            var itemData = data.ItemData;
            if (itemData == null)
                return;

            itemData.ItemID = newItem.Id;
            if (ammoDefinition == null)
            {
                itemData.Count = newItem.Count;
                itemData.AmmoItemID = 0;
                itemData.AmmoCount = 0;
            }
            else
            {
                itemData.Count = 1;
                itemData.AmmoItemID = ammoDefinition.Id;
                itemData.AmmoCount = newItem.Count;
            }
        }
    }
}
