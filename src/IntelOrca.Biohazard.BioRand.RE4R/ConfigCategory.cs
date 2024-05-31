namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class ConfigCategory(string text, string backgroundColor, string textColor)
    {
        public string Label => text;
        public string TextColor => textColor;
        public string BackgroundColor => backgroundColor;
    }
}
