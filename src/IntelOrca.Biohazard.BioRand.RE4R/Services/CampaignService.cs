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

        public CampaignService(ChainsawRandomizer randomizer)
        {
            _randomizer = randomizer;
            var minChapter = randomizer.Campaign != Campaign.Ada ? 1 : 1;
            var maxChapter = randomizer.Campaign != Campaign.Ada ? 16 : 7;
            var startChapter = Math.Clamp(_randomizer.GetConfigOption("start-chapter", 1), minChapter, maxChapter);

            var multiplierTable = randomizer.Campaign != Campaign.Ada ? g_leonLengthMutiplier : g_adaLengthMutiplier;
            Chapters = Enumerable.Range(startChapter, maxChapter - startChapter + 1)
                .Select(num => new Chapter(num, multiplierTable[num - 1]))
                .ToImmutableArray();
        }

        public int StartChapter => Chapters[0].Number;
        public int EndChapter => Chapters[^1].Number;

        public Chapter GetChapter(int chapter)
        {
            return Chapters.First(x => x.Number == chapter);
        }

        public bool HasChapter(int chapter)
        {
            return Chapters.Any(x => x.Number == chapter);
        }

        public class Chapter(int num, float lengthMultiplier)
        {
            public int Number => num;
            public int StartStage { get; set; }
            public Vector3 StartPosition { get; set; }
            public EulerAngles StartEuler { get; set; }
            public float LengthMultiplier => lengthMultiplier;
        }

        private readonly static ImmutableArray<float> g_leonLengthMutiplier = [
            // ------ village ------
            1.0f, //  1
            0.5f, //  2
            1.0f, //  3
            2.0f, //  4
            0.5f, //  5
            1.0f, //  6
            // ------ castle ------
            2.0f, //  7
            2.0f, //  8
            2.0f, //  9
            1.0f, // 10
            2.0f, // 11
            0.5f, // 12
            // ------ island ------
            2.0f, // 13
            2.0f, // 14
            2.0f, // 15
            0.5f, // 16
        ];

        private readonly static ImmutableArray<float> g_adaLengthMutiplier = [
            1.0f,
            1.0f,
            1.0f,
            1.0f,
            1.0f,
            1.0f,
            1.0f,
        ];

        // ------ village ------
        //  1, 1-1,   [hunting lodge -> luis] *    [1.0]
        //  2, 1-2,   [factory -> mendez]          [0.5]
        //  3, 1-3,   [mendez -> del lago]         [1.0]
        //  4, 2-1,   [boat house -> church]       [2.0]
        //  5, 2-2,   [church -> cabin]            [0.5]
        //  6, 2-3,   [cabin -> castle]            [1.0]
        // ------ castle ------
        //  7, 3-1,   [castle -> courtyard] *      [2.0]
        //  8, 3-2,   [courtyard -> chamber]       [2.0]
        //  9, 3-3,   [chamber -> ballroom]        [2.0]
        //   , 3-3-a, [ashley]
        // 10, 4-1,   [ballroom -> mines]          [0.5]
        // 11, 4-2,   [mines -> krauser]           [2.0]
        // 12, 4-3,   [krauser -> island]          [0.5]
        // ------ island ------
        // 13, 5-1,   [island -> ashley] *         [2.0]
        // 14, 5-2,   [ashley -> krauser]          [2.0]
        // 15, 5-3,   [krauser -> ashley]          [2.0]
        // 16, 5-4,   [ashley -> end]              [0.5]
    }
}
