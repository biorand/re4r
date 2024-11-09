using System.Globalization;
using System.Text;
using System.Threading;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using REE;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Re4rRandomizer : IRandomizer
    {
        public string BuildVersion => ChainsawRandomizerFactory.Default.GitHash;
        public RandomizerConfigurationDefinition ConfigurationDefinition => Re4rRandomizerConfigurationDefinition.Create(EnemyClassFactory.Default);
        public RandomizerConfiguration DefaultConfiguration => Re4rRandomizerConfigurationDefinition.Create(EnemyClassFactory.Default).GetDefault();

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            // We swap to invariant culture so , is decimal point
            var backupCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                var randomizer = ChainsawRandomizerFactory.Default.Create();
                return randomizer.Randomize(input);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = backupCulture;
            }
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
