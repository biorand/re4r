using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using REE;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Re4rRandomizer : IRandomizer
    {
        public string BuildVersion => ChainsawRandomizerFactory.Default.GitHash;
        public object ConfigurationDefinition => RandomizerConfigurationDefinition.Create(EnemyClassFactory.Default);
        public RandomizerConfiguration DefaultConfiguration => RandomizerConfigurationDefinition.Create(EnemyClassFactory.Default).GetDefault();

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            var randomizer = ChainsawRandomizerFactory.Default.Create();
            return randomizer.Randomize(input);
        }

        public static PakList GetDefaultPakList()
        {
            var pakListBytes = Resources.pakcontents_txt.Ungzip();
            var pakListText = Encoding.UTF8.GetString(pakListBytes);
            return new PakList(pakListText);
        }

        public static ScnFile ReadScnFile(byte[] data)
        {
            return ChainsawRandomizerFactory.Default.ReadScnFile(data);
        }
    }
}
