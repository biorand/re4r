using System.IO;
using System.IO.Compression;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    internal static class ZipExtensions
    {
        public static byte[] GetData(this ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
