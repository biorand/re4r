using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static Guid GetGuidHash(this string s)
        {
            var hash = MD5.HashData(Encoding.ASCII.GetBytes(s));
            hash[8] = (byte)(0x40 | (hash[8] & 0x0F));
            return new Guid(hash);
        }
    }
}
