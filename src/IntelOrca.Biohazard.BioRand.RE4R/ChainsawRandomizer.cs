using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;
using IntelOrca.Biohazard.BioRand.RE4R.Services;
using IntelOrca.Biohazard.REE;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizer : ReeRandomizerGenerator, IReeRandomizerContext
    {
        public bool Beta => Options.Beta;
        public int Version => Re4rRandomizer.GetGameVersion(Input.Configuration);
        public int PakVersion => Version + 1;
        public ChainsawRandomizer FileRepository => this;
        public DynamicData DynamicData { get; }
        public Campaign Campaign { get; private set; }

        public AreaService AreaService => GetService<AreaService>();
        public ValuableDistributor ValuableDistributor => GetService<ValuableDistributor>();
        public ItemRandomizer ItemRandomizer => GetService<ItemRandomizer>();
        public EnemyService EnemyService => GetService<EnemyService>();
        public GimmickService GimmickService => GetService<GimmickService>();
        public FlagService FlagService => GetService<FlagService>();

        public ChainsawRandomizer(IReeRandomizer randomizer, RandomizerInput input, RandomizerOptions options, IRandomizerProgress progress)
            : base(randomizer, input, options, progress)
        {
            DynamicData = new DynamicData(download: options.Beta && input.Configuration.GetValueOrDefault<bool>("debug-download-data"));
        }

        public override RszTypeRepository TypeRepository => Re4rTypeRepository.FromVersion(Version);
        public override string PakName => $"re_chunk_000.pak.patch_{PakVersion:000}.pak";
        public override byte[]? GetSupplementFile(string path) => EmbeddedData.TryGetFile(path);

        public override void GenerateCampaigns()
        {
            Campaign = Input.Configuration.GetValueOrDefault("campaign", "") == "Separate Ways"
                ? Campaign.Ada
                : Campaign.Leon;
            this.ApplyOverlay(EmbeddedData.GetFile("supplement.zip"));
            GenerateCampaign(Campaign.ToString().ToLowerInvariant());
        }

        protected override void OnAfterModify()
        {
            FlagService.Save();
            AreaService.Save();
        }

        protected override void OnBuildMod(ModBuilder builder)
        {
            builder.ScreenshotFileName = "pic.jpg";
            builder.ScreenshotFileContent = EmbeddedData.GetFile("modimage.jpg");
        }

        protected override string Instructions =>
            """
            <p class="mt-3">What should I do if my game crashes?</p>
            <ol class="list-decimal text-gray-300" style="margin-left: 3rem;">
              <li>Reload from last checkpoint and try again.</li>
              <li>Alter the enemy sliders slightly or reduce the number temporarily. This will reshuffle the enemies. Reload from last checkpoint and try again.</li> <li>As a last resort, change your seed, and reload from last checkpoint.</li>
            </ol>
            """;

        public void EnsureValuableDistribution()
        {
            ValuableDistributor.Setup(ItemRandomizer, GetRng("service/valuabledistributor"));
        }

        bool IReeRandomizerContext.ExportingMod => false;

        public string User => GetConfigOption<string>("username") ?? "player";
        public int Seed => Input.Seed;

        public bool HasSpecialTouch(string kind)
        {
            if (!GetConfigOption("enable-special", true))
                return false;

            var special = GetConfigOption<string>("special");
            var present = special?.Split(',').Contains(kind) == true;
            return present;
        }
    }
}
