using System.Numerics;
using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class GimmickSaveDataFile
    {
        public IPatchContext Context { get; }
        public string Path { get; }

        private readonly UserFile.Builder _userFileBuilder;
        private bool _dirty;

        private RszObjectNode Root
        {
            get => _userFileBuilder.Objects[0];
            set => _userFileBuilder.Objects = [value];
        }

        public GimmickSaveDataFile(IPatchContext context, string path)
        {
            Context = context;
            Path = path;

            var data = context.GetFile(path);
            if (data == null)
            {
                // Use an empty savedata file as a base
                _userFileBuilder = context
                    .GetUserFile("natives/stm/_chainsaw/leveldesign/chapter/cp10_chp1_1/level_cp10_chp1_1_savedata.user.2")
                    .ToBuilder(context.TypeRepository);
            }
            else
            {
                _userFileBuilder = new UserFile(data).ToBuilder(Context.TypeRepository);
            }
        }

        public void Apply()
        {
            if (!_dirty)
                return;

            var userFile = _userFileBuilder.Build();
            Context.SetUserFile(Path, userFile);
        }

        public void AddBasic(string contextType, ContextID contextId)
        {
            Add(new chainsaw.GimmickSaveDataTable.Data()
            {
                ContextType = contextType,
                ID = contextId,
                Save = new GimmickContext.SaveData()
                {
                    Attr = [0, 0, 0, 0]
                }
            });
        }

        public void AddExtended(string contextType, ContextID contextId, string mapName, int stage, Vector3 position)
        {
            Add(new chainsaw.GimmickSaveDataTable.Data()
            {
                ContextType = contextType,
                ID = contextId,
                Save = new GimmickContext.SaveData()
                {
                    Attr = [0, 0, 0, 0]
                },
                Maps =
                [
                    new chainsaw.GimmickContext.MapData()
                    {
                        MapName = mapName,
                        _StageID = stage,
                        MapPosition = position
                    }
                ]
            });
        }

        public void Add(chainsaw.GimmickSaveDataTable.Data data)
        {
            Add((RszObjectNode)RszSerializer.Serialize(
                Context.TypeRepository.FromName("chainsaw.GimmickSaveDataTable.Data")!,
                data));
        }

        public void Add(RszObjectNode data)
        {
            var datas = Root.Get<RszArrayNode>("Datas");
            Root = Root.Set("Datas", datas.Add(data));
            _dirty = true;
        }

        public RszObjectNode? Remove(ContextID contextId)
        {
            var datas = Root.Get<RszArrayNode>("Datas");
            var children = datas.Children;
            for (var i = 0; i < children.Length; i++)
            {
                var c = children[i];
                if (c.Get<chainsaw.ContextID>("ID") == contextId)
                {
                    Root = Root.Set("Datas", datas
                        .WithChildren(
                            children.RemoveAt(i)));
                    _dirty = true;
                    return (RszObjectNode)c;
                }
            }
            return null;
        }
    }
}
