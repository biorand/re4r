namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    internal static class NumericExtensions
    {
        public static int RoundPrice(this double value)
        {
            if (value >= 100000)
                return (int)(value / 100000) * 100000;
            if (value >= 10000)
                return (int)(value / 1000) * 1000;
            if (value >= 1000)
                return (int)(value / 1000) * 1000;
            if (value >= 100)
                return (int)(value / 100) * 100;
            if (value >= 10)
                return (int)(value / 10) * 10;
            return (int)value;
        }
    }
}
