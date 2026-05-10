using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IntelOrca.Biohazard.REE.Package;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Re4rRandomizer : IReeRandomizer
    {
        private static readonly EnemyClassFactory s_defaultEnemyClassFactory = new();
        private readonly string _inputGamePath;
        private readonly bool _beta;
        private readonly IRandomizerProgress _progress;
        private PakList? _pakList;

        public Re4rRandomizer()
            : this("", beta: false, DummyRandomizerProgress.Default)
        {
        }

        public Re4rRandomizer(string inputGamePath, bool beta, IRandomizerProgress progress)
        {
            _inputGamePath = inputGamePath;
            _beta = beta;
            _progress = progress;
        }

        public static string BuildVersion => VersionHelper.GetGitHashShort(Assembly.GetExecutingAssembly());
        public string Version => $"{VersionHelper.GetVersion(Assembly.GetExecutingAssembly())} ({BuildVersion})";
        public string Author => "IntelOrca & BioRand Team";
        public string GameMoniker => "re4r";
        public string? ProcessName => "re4";
        public RandomizerConfigurationDefinition ConfigurationDefinition => Re4rRandomizerConfigurationDefinition.Create(s_defaultEnemyClassFactory, _beta);

        public PakList PakList
        {
            get
            {
                if (_pakList == null)
                {
                    var pakListBytes = EmbeddedData.GetFile("pakcontents.txt.gz").Ungzip();
                    var pakListText = Encoding.UTF8.GetString(pakListBytes);
                    _pakList = new PakList(pakListText);
                }
                return _pakList;
            }
        }

        public ImmutableArray<string> PakExtractFileNamePatterns => [
            @"natives/stm/.*\.gui\.540034",
            @"natives/stm/.*\.motfsm2\.43",
            @"natives/stm/.*\.msg\.22",
            @"natives/stm/.*\.pfb\.17",
            @"natives/stm/.*\.scn\.20",
            @"natives/stm/.*\.user\.2",
            @"natives/stm/.*\.uvar\.3"
        ];

        public RandomizerOutput Randomize(RandomizerInput input)
        {
            // We swap to invariant culture so , is decimal point
            var backupCulture = Thread.CurrentThread.CurrentCulture;
            var backupCultureUi = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            try
            {
                var generator = (ChainsawRandomizer)CreateGeneratorAsync(
                    input,
                    new RandomizerOptions()
                    {
                        Beta = _beta,
                        GameInputPath = _inputGamePath,
                        UserTags = []
                    },
                    _progress).Result;
                return generator.GenerateAsync().Result;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = backupCulture;
                Thread.CurrentThread.CurrentUICulture = backupCultureUi;
            }
        }

        public Task<RandomizerConfigurationDefinition> GetConfigurationDefinitionAsync(RandomizerOptions options)
        {
            return Task.FromResult(Re4rRandomizerConfigurationDefinition.Create(s_defaultEnemyClassFactory, options.Beta));
        }

        public static PakList GetDefaultPakList() => new Re4rRandomizer().PakList;

        public Task<IReeRandomizerGenerator> CreateGeneratorAsync(RandomizerInput input, RandomizerOptions options, IRandomizerProgress progress)
        {
            var normalizedOptions = new RandomizerOptions
            {
                Beta = options.Beta,
                UserTags = options.UserTags,
                GameInputPath = NormalizeInputPath(input.Configuration, options.GameInputPath)
            };
            return Task.FromResult<IReeRandomizerGenerator>(new ChainsawRandomizer(this, input, normalizedOptions, progress));
        }

        internal static int GetGameVersion(RandomizerConfiguration configuration)
        {
            var gameVersionString = configuration.GetValueOrDefault("game-version", "31 Mar 2026");
            return gameVersionString switch
            {
                "31 Mar 2026" => 6,
                "3 Feb 2026" => 5,
                "4 Mar 2025" => 4,
                _ => throw new RandomizerUserException("Game version not supported")
            };
        }

        private static string NormalizeInputPath(RandomizerConfiguration configuration, string inputGamePath)
        {
            var paths = inputGamePath.Split(Path.PathSeparator);
            if (paths.Length <= 1)
                return inputGamePath;

            return GetGameVersion(configuration) switch
            {
                6 => paths[0],
                5 => paths[1],
                _ => paths[2]
            };
        }
    }
}
