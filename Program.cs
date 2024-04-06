using System;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

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

            var enemyClassFactory = EnemyClassFactory.Create();
            var randomizer = new ChainsawRandomizer(fileRepository, enemyClassFactory);
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
    }
}
