using System;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dt)
        {
            var offset = new DateTimeOffset(dt);
            return offset.ToUnixTimeSeconds();
        }
    }
}
