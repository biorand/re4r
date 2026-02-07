using System.Globalization;
using System.Text;
using System.Threading;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Package;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Re4rRandomizer(string inputGamePath, IProgressReporter reporter)
    {
        public static string BuildVersion => ChainsawRandomizerFactory.Default.GitHash;
        public static RandomizerConfigurationDefinition ConfigurationDefinition => Re4rRandomizerConfigurationDefinition.Create(EnemyClassFactory.Default);
        public static RandomizerConfiguration DefaultConfiguration => Re4rRandomizerConfigurationDefinition.Create(EnemyClassFactory.Default).GetDefault();

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            // We swap to invariant culture so , is decimal point
            var backupCulture = Thread.CurrentThread.CurrentCulture;
            var backupCultureUi = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try
            {
                var enemyClassFactory = EnemyClassFactory.Create();
                using var randomizer = new ChainsawRandomizer(enemyClassFactory, input, inputGamePath, reporter);
                return randomizer.Randomize();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = backupCulture;
                Thread.CurrentThread.CurrentUICulture = backupCultureUi;
            }
        }

        public static PakList GetDefaultPakList()
        {
            var pakListBytes = EmbeddedData.GetFile("pakcontents.txt.gz").Ungzip();
            var pakListText = Encoding.UTF8.GetString(pakListBytes);
            return new PakList(pakListText);
        }
    }
}
