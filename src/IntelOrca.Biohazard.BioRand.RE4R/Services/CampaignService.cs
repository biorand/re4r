using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class CampaignService
    {
        private readonly ChainsawRandomizer _randomizer;

        public ImmutableArray<Chapter> Chapters { get; }
        public ImmutableArray<Chapter> EnabledChapters { get; }

        public CampaignService(ChainsawRandomizer randomizer)
        {
            _randomizer = randomizer;
            var minChapter = 1;
            var maxChapter = randomizer.Campaign != Campaign.Ada ? 16 : 7;
            var startChapter = Math.Clamp(_randomizer.GetConfigOption("start-chapter", 1), minChapter, maxChapter);

            Chapters = (randomizer.Campaign != Campaign.Ada ? g_leonChapters : g_adaChapters)
                .Select(x => x.Clone())
                .ToImmutableArray();
            EnabledChapters = Enumerable.Range(startChapter, maxChapter - startChapter + 1)
                .Select(num => Chapters.First(x => x.Number == num))
                .ToImmutableArray();

            for (var i = 0; i < EnabledChapters.Length; i++)
            {
                EnabledChapters[i].ProgressStart = i / (double)EnabledChapters.Length;
                EnabledChapters[i].ProgressEnd = (i + 1) / (double)EnabledChapters.Length;
            }
        }

        public int StartChapter => EnabledChapters[0].Number;
        public int EndChapter => EnabledChapters[^1].Number;

        public Chapter GetChapter(int chapter)
        {
            return Chapters.First(x => x.Number == chapter);
        }

        public bool HasChapter(int chapter)
        {
            return Chapters.Any(x => x.Number == chapter);
        }

        public class Chapter(int num, string internalNumber, string area, string description, float lengthMultiplier, int id)
        {
            public int Number => num;
            public string InternalNumber => internalNumber;
            public string Area => area;
            public string Description => description;
            public float LengthMultiplier => lengthMultiplier;
            public int Id => id;

            // Progress at start and end of the chapter based on start and end chapter
            public double ProgressStart { get; set; }
            public double ProgressEnd { get; set; }

            public int StartStage { get; set; }
            public Vector3 StartPosition { get; set; }
            public EulerAngles StartEuler { get; set; }

            public Chapter Clone()
            {
                return new Chapter(num, internalNumber, area, description, lengthMultiplier, id);
            }
        }

        private readonly static ImmutableArray<Chapter> g_leonChapters = [
            // ------ village ------
            new Chapter(1, "1-1", "village", "hunting lodge -> luis", 1.0f, 21100),
            new Chapter(2, "1-2", "village", "factory -> mendez", 0.5f, 21200),
            new Chapter(3, "1-3", "village", "mendez -> del lago", 1.0f, 21300),
            new Chapter(4, "2-1", "village", "boat house -> church", 2.0f, 22100),
            new Chapter(5, "2-2", "village", "church -> cabin", 0.5f, 22200),
            new Chapter(6, "2-3", "village", "cabin -> castle", 1.0f, 22300),
            // ------ castle ------
            new Chapter(7, "3-1", "castle", "castle -> courtyard", 2.0f, 23100),
            new Chapter(8, "3-2", "castle", "courtyard -> chamber", 2.0f, 23200),
            new Chapter(9, "3-3", "castle", "chamber -> ballroom", 2.0f, 23300),
            // new Chapter(9a, "3-3-a", "castle", "ashley section", 0.5f),
            new Chapter(10, "4-1", "castle", "ballroom -> mines", 0.5f, 24100),
            new Chapter(11, "4-2", "castle", "mines -> krauser", 2.0f, 24200),
            new Chapter(12, "4-3", "castle", "krauser -> island", 0.5f, 24300),
            // ------ island ------
            new Chapter(13, "5-1", "island", "island -> ashley", 2.0f, 25100),
            new Chapter(14, "5-2", "island", "ashley -> krauser", 2.0f, 25200),
            new Chapter(15, "5-3", "island", "krauser -> ashley", 2.0f, 25300),
            new Chapter(16, "5-4", "island", "ashley -> end", 0.5f, 25400),
        ];

        private readonly static ImmutableArray<Chapter> g_adaChapters = [
            // ------ castle ------
            new Chapter(1, "0-1", "castle", "pesanta -> catapults", 1.0f, 30100),
            // ------ village ------
            new Chapter(2, "1-1", "village", "checkpoint -> church", 1.0f, 31100),
            // new Chapter(2b, "1-2", "village", "church -> mendez", 1.0f, 31200),
            new Chapter(3, "2-1", "village", "mendez -> factory", 1.0f, 32100),
            // new Chapter(3b, "2-2", "village", "mendez -> el gigante", 1.0f, 32200),
            // ------ castle ------
            new Chapter(4, "3-1", "castle", "tower -> embattlements", 1.0f, 33100),
            new Chapter(5, "3-2", "castle", "embattlements -> mines -> boat", 1.0f, 33200),
            // ------ island ------
            new Chapter(6, "4-1", "island", "camp -> laser lab", 1.0f, 34100),
            new Chapter(7, "5-1", "island", "boat -> sadler", 1.0f, 35100),
        ];
    }
}
