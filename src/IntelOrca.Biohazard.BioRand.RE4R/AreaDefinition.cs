using System.Text.RegularExpressions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class AreaDefinition
    {
        private static Regex _locationRegex = new(@"/loc(\d\d)/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Path
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;

                    var m = _locationRegex.Match(value);
                    Location = m.Success ? int.Parse(m.Groups[1].Value) : null;
                }
            }
        } = "";

        public int Chapter { get; set; }
        public bool ChapterOnly { get; set; }
        public int? Location { get; set; }
    }
}
