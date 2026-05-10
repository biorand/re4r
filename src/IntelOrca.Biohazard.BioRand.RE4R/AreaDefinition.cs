namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinition
    {
        public AreaKind Kind { get; set; }

        public string Path
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    Location = StageIds.GetLocationFromPath(value);
                    Stage = StageIds.GetStageFromPath(value);
                }
            }
        } = "";

        public string DropItemSaveDataPath => GetDataPath("itemdata");
        public string GimmickSaveDataPath => GetDataPath("savedata");

        public int Chapter { get; set; }
        public bool ChapterOnly { get; set; }
        public int? Location { get; set; }
        public int? Stage { get; set; }

        private string GetDataPath(string type)
        {
            var index = Path.LastIndexOf(".scn.20");
            return Path[..index] + $"_{type}.user.2";
        }
    }

    public enum AreaKind
    {
        General,
        Items,
        Gimmicks,
    }
}
