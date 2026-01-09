using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
#pragma warning disable CS9113 // Parameter is unread.
    internal class CampaignService(ChainsawRandomizer randomizer)
#pragma warning restore CS9113 // Parameter is unread.
    {
        private readonly List<Chapter?> _chapters = [];

        public ImmutableArray<Chapter> Chapters => _chapters
            .Choose()
            .ToImmutableArray();

        public Chapter GetChapter(int num)
        {
            var index = num - 1;
            while (_chapters.Count <= index)
            {
                _chapters.Add(null);
            }

            var chapter = _chapters[index];
            if (chapter == null)
            {
                chapter = new Chapter(num);
                _chapters[index] = chapter;
            }
            return chapter;
        }

        public class Chapter(int num)
        {
            public int Number => num;
            public int StartStage { get; set; }
            public Vector3 StartPosition { get; set; }
            public EulerAngles StartEuler { get; set; }
        }
    }
}
