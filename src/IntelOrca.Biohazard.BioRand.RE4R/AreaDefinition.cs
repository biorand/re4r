namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinition
    {
        public string Path
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    Location = StageIds.GetLocationFromPath(value);
                }
            }
        } = "";

        public int Chapter { get; set; }
        public bool ChapterOnly { get; set; }
        public int? Location { get; set; }
    }
}
