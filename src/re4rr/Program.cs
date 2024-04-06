using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var dataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "data");

            // var originalFile = @"M:\git\REE.PAK.Tool\REE.Unpacker\REE.Unpacker\bin\Release\out\natives\stm\_chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";
            // var originalFile = @"C:\Users\Ted\Desktop\backup\level_cp10_chp1_1_010.scn.20";
            // var targetFile = @"F:\games\re4r\fluffy\Games\RE4R\Mods\orca_test\natives\STM\_Chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";
            // var targetFile = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4\natives\STM\_Chainsaw\leveldesign\chapter\cp10_chp1_1\level_cp10_chp1_1_010.scn.20";

            var gamePath = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4";
            var inputGameData = @$"{gamePath}\re_chunk_000.pak.patch_003.pak";
            var outputFile = @$"{gamePath}\re_chunk_000.pak.patch_004.pak";

            // var ogPakFile = new PatchedPakFile(inputGameData);
            // var fileRepository = new FileRepository(ogPakFile);
            var fileRepository = new FileRepository(@"G:\re4r\extract\patch_003");

            var enemyClassFactory = EnemyClassFactory.Create(Path.Combine(dataPath, "enemies.json"));
            var rszFileOption = CreateRszFileOption(dataPath);
            var randomizer = new ChainsawRandomizer(fileRepository, enemyClassFactory, rszFileOption);
            randomizer.Randomize(0);

            // fileRepository.WriteOutputPakFile(outputFile);
            fileRepository.WriteOutputFolder(gamePath);

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

        private static RszFileOption CreateRszFileOption(string dataPath)
        {
            var gameName = GameName.re4;

            var dataFile = Path.Combine($"rsz{gameName}.json");
            var enumFile = Path.Combine($"Enums/{gameName}_enum.json");
            var pathFile = Path.Combine($"RszPatch/rsz{gameName}_patch.json");

            var tempPath = Path.Combine(Path.GetTempPath(), "re4rr");
            Directory.CreateDirectory(tempPath);
            Ungzip(Path.Combine(dataPath, dataFile + ".gz"), Path.Combine(tempPath, dataFile));
            CopyFile(Path.Combine(dataPath, enumFile), Path.Combine(tempPath, "Data", enumFile));
            CopyFile(Path.Combine(dataPath, pathFile), Path.Combine(tempPath, "Data", pathFile));

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

        private static void Ungzip(string sourcePath, string targetPath)
        {
            using var inputStream = File.OpenRead(sourcePath);
            using var outputStream = File.OpenWrite(targetPath);
            using var deflateStream = new GZipStream(inputStream, CompressionMode.Decompress);
            deflateStream.CopyTo(outputStream);
        }

        private static void CopyFile(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.Copy(sourcePath, targetPath, overwrite: true);
        }
    }
}
