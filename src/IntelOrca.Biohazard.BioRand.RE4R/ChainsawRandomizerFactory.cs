using System.IO;
using System.IO.Compression;
using System.Text;
using REE;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class ChainsawRandomizerFactory
    {
        public static ChainsawRandomizerFactory Default => new ChainsawRandomizerFactory();

        private ChainsawRandomizerFactory()
        {
        }

        public PakList GetDefaultPakList()
        {
            var pakListBytes = Ungzip(Resources.pakcontents_txt);
            var pakListText = Encoding.UTF8.GetString(pakListBytes);
            return new PakList(pakListText);
        }

        public IChainsawRandomizer Create()
        {
            var enemyClassFactory = EnemyClassFactory.Create();
            var rszFileOption = CreateRszFileOption();
            var randomizer = new ChainsawRandomizer(enemyClassFactory, rszFileOption);
            return randomizer;
        }

        private static RszFileOption CreateRszFileOption()
        {
            var gameName = GameName.re4;

            var dataFile = Path.Combine($"rsz{gameName}.json");
            var enumFile = Path.Combine($"Enums/{gameName}_enum.json");
            var pathFile = Path.Combine($"RszPatch/rsz{gameName}_patch.json");

            var tempPath = Path.Combine(Path.GetTempPath(), "re4rr");
            Directory.CreateDirectory(tempPath);
            Ungzip(Resources.rszre4_json, Path.Combine(tempPath, dataFile));
            CopyFile(Resources.re4_enum, Path.Combine(tempPath, "Data", enumFile));
            CopyFile(Resources.rszre4_patch, Path.Combine(tempPath, "Data", pathFile));

            var cwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(tempPath);
                return new RszFileOption(gameName);
            }
            finally
            {
                Directory.SetCurrentDirectory(cwd);
            }
        }

        private static void Ungzip(byte[] input, string targetPath)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = File.OpenWrite(targetPath);
            using var deflateStream = new GZipStream(inputStream, CompressionMode.Decompress);
            deflateStream.CopyTo(outputStream);
        }

        private static byte[] Ungzip(byte[] input)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = new MemoryStream();
            using var deflateStream = new GZipStream(inputStream, CompressionMode.Decompress);
            deflateStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        private static void CopyFile(byte[] input, string targetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllBytes(targetPath, input);
        }
    }
}
