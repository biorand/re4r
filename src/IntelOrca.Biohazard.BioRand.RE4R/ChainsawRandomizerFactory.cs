using System.IO;
using System.IO.Compression;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class ChainsawRandomizerFactory
    {
        public static ChainsawRandomizerFactory Default => new ChainsawRandomizerFactory();

        private ChainsawRandomizerFactory()
        {
        }

        public IChainsawRandomizer Create()
        {
            var gamePath = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4";
            var inputGameData = @$"{gamePath}\re_chunk_000.pak.patch_003.pak";
            var outputFile = @$"{gamePath}\re_chunk_000.pak.patch_004.pak";

            // var ogPakFile = new PatchedPakFile(inputGameData);
            // var fileRepository = new FileRepository(ogPakFile);
            var fileRepository = new FileRepository(@"G:\re4r\extract\patch_003");

            var enemyClassFactory = EnemyClassFactory.Create();
            var rszFileOption = CreateRszFileOption();
            var randomizer = new ChainsawRandomizer(fileRepository, enemyClassFactory, rszFileOption);
            return randomizer;

            // LogRoomEnemies(enemyClassFactory);
            // 
            // for (var i = 0; i < areas.Length; i++)
            // {
            //     var src = FindFile(areas[i]);
            //     if (src == null)
            //         continue;
            // 
            //     var dst = GetOutputPath(areas[i]);
            //     Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            // 
            //     var area = new Area(enemyClassFactory, src);
            //     RandomizeArea(area, random);
            //     Console.WriteLine($"Writing {dst}...");
            //     area.Save(dst);
            // }
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

        private static void CopyFile(byte[] input, string targetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllBytes(targetPath, input);
        }
    }
}
