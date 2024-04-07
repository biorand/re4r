using System;
using System.IO;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class MemoryExtensions
    {
        public static void WriteToFile(this string data, string path)
        {
            File.WriteAllText(path, data);
        }

        public static void WriteToFile(this byte[] data, string path)
            => data.AsSpan().WriteToFile(path);

        public static void WriteToFile(this Span<byte> data, string path)
        {
            using var fs = File.OpenWrite(path);
            fs.Write(data);
        }

        public static void WriteToFile(this ReadOnlyMemory<byte> data, string path)
            => data.Span.WriteToFile(path);

        public static void WriteToFile(this ReadOnlySpan<byte> data, string path)
        {
            using var fs = File.OpenWrite(path);
            fs.Write(data);
        }
    }
}
