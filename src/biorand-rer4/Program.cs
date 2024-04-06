using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R;

namespace biorand_rer4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var gamePath = @"F:\games\steamapps\common\RESIDENT EVIL 4  BIOHAZARD RE4";

            var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
            var randomizer = chainsawRandomizerFactory.Create();

            var input = new RandomizerInput()
            {
                Seed = 0
            };
            var output = randomizer.Randomize(input);
            // output.WriteOutputPakFile(outputFile);
            output.WriteOutputFolder(gamePath);

        }
    }
}
