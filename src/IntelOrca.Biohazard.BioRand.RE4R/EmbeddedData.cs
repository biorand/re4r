using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class EmbeddedData
    {
        public static Stream? GetStream(string name)
        {
            var exeDirectory = AppContext.BaseDirectory;
            var dataDirectory = Path.Combine(exeDirectory, "data");
            var dataPath = Path.Combine(dataDirectory, name);
            if (File.Exists(dataPath))
                return new MemoryStream(File.ReadAllBytes(dataPath));

            var assembly = Assembly.GetExecutingAssembly()!;
            var resourceName = $"IntelOrca.Biohazard.BioRand.RE4R.data.{name}";
            return assembly.GetManifestResourceStream(resourceName);
        }

        public static byte[] GetFile(string name)
        {
            return TryGetFile(name) ?? throw new FileNotFoundException($"{name} not found");
        }

        public static byte[]? TryGetFile(string name)
        {
            using var stream = GetStream(name);
            if (stream == null)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        public static byte[]? GetCompressedFile(string name)
        {
            using var stream = GetStream(name);
            if (stream == null)
                return null;

            using var gzipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var ms = new MemoryStream();
            gzipStream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
