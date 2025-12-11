using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class GimmickSaveDataTable
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

        public GimmickSaveDataTable(IPatchContext context, string path)
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

        public void AddBasic(ContextID contextId)
        {
            var datas = Root.Get<RszArrayNode>("Datas");
            datas = datas.Add(RszSerializer.Serialize(
                Context.TypeRepository.FromName("chainsaw.GimmickSaveDataTable.Data")!,
                new chainsaw.GimmickSaveDataTable.Data()
                {
                    ID = contextId,
                    Save = new GimmickContext.SaveData()
                    {
                        Attr = [0, 0, 0, 0]
                    }
                }));
            Root = Root.Set("Datas", datas);
            _dirty = true;
        }
    }
}
