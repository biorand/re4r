using System;
using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class CampaignModifier : Modifier
    {
        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            // Ada currently unsupported
            if (randomizer.Campaign != Campaign.Leon)
                return;

            var startChapter = Math.Clamp(randomizer.GetConfigOption("start-chapter", 1), 1, _chapterFileNames.Length);
            if (startChapter <= 1)
                return;

            var ch1Path = GetChapterPath(1);
            var startChPath = GetChapterPath(startChapter);

            var fileRepository = randomizer.FileRepository;
            var templateData = fileRepository.DeserializeUserFile<chainsaw.CampaignInitialSettingUserData>(startChPath)!
                ._CampaignInitialSettingList[0];
            fileRepository.ModifyUserFile<chainsaw.CampaignInitialSettingUserData>(ch1Path, userData =>
            {
                var entry = userData._CampaignInitialSettingList[0];
                entry._Campaign = templateData._Campaign;
                entry._Chapter = templateData._Chapter;
                entry._SpecialJumpSequence = templateData._SpecialJumpSequence;
                entry._CharacterList = templateData._CharacterList;
                entry._FlagList = templateData._FlagList;
                return userData;
            });
        }

        private static string GetChapterPath(int num)
        {
            var fileName = _chapterFileNames[num - 1];
            return $"natives/stm/_chainsaw/leveldesign/initialsettings/{fileName}";
        }

        private static readonly ImmutableArray<string> _chapterFileNames = [
            // ------ village ------
            "chp1-1.user.2", //  1 [hunting lodge -> luis] *
            "chp1-2.user.2", //  2 [factory -> mendez]
            "chp1-3.user.2", //  3 [mendez -> del lago]
            "chp2-1.user.2", //  4 [boat house -> church]
            "chp2-2.user.2", //  5 [church -> cabin]
            "chp2-3.user.2", //  6 [cabin -> castle]
            // ------ castle ------
            "chp3-1.user.2", //  7 [castle -> courtyard] *
            "chp3-2.user.2", //  8 [courtyard -> chamber]
            "chp3-3.user.2", //  9 [chamber -> ballroom]
            // "chp3-3-a.user.2", // Ashley
            "chp4-1.user.2", // 10 [ballroom -> mines]
            "chp4-2.user.2", // 11 [mines -> krauser]
            "chp4-3.user.2", // 12 [krauser -> island]
            // ------ island ------
            "chp5-1.user.2", // 13 [island -> ashley] *
            "chp5-2.user.2", // 14 [ashley -> krauser]
            "chp5-3.user.2", // 15 [krauser -> ashley]
            "chp5-4.user.2", // 16 [ashley -> end]
        ];
    }
}
