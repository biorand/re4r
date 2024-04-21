using System.Globalization;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }
    }
}
