using System;

namespace IntelOrca.Biohazard.BioRand.Server.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixTimeSeconds(this DateTime dt)
        {
            var offset = new DateTimeOffset(dt);
            return offset.ToUnixTimeSeconds();
        }

        public static DateTime ToDateTime(this int unixTimeSeconds)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
            return offset.UtcDateTime;
        }
    }
}
