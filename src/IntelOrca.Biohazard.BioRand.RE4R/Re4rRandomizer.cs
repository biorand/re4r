using System.Globalization;
using System.Text;
using System.Threading;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Package;
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
            var backupCultureUi = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try
            {
                var randomizer = ChainsawRandomizerFactory.Default.Create();
                return randomizer.Randomize(input);
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

        public static ScnFile ReadScnFile(byte[] data)
        {
            return ChainsawRandomizerFactory.Default.ReadScnFile(data);
        }

        public static UserFile ReadUserFile(byte[] data)
        {
            return ChainsawRandomizerFactory.Default.ReadUserFile(data);
        }
    }
}
