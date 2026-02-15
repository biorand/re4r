using System;
using System.Buffers.Binary;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class GameCompatibility(ChainsawRandomizer randomizer)
    {
        public int PakVersion { get; set; }

        public void Apply()
        {
            var gameVersion = randomizer.GetConfigOption<string>("game-version", "3 Feb 2026");
            if (gameVersion == "3 Feb 2026")
            {
                randomizer.PakVersion = 6;
            }
            else if (gameVersion == "4 Mar 2025")
            {
                randomizer.PakVersion = 5;
                ApplyV4();
            }
            else
            {
                throw new RandomizerUserException("Unsupported game version");
            }
        }

        private void ApplyV4()
        {
            var context = randomizer.FileRepository;
            context.ApplyOverlay(EmbeddedData.GetFile("overlay_v4.zip"));

            var path = "natives/stm/_chainsaw/appsystem/ui/userdata/guiparamholdersettinguserdata.user.2";
            var guiparamholdersettinguserdata = context.GetFile(path)!;
            var span = new Span<byte>(guiparamholdersettinguserdata);

            // Set user defined hold buy time
            var holdBuyTime = Math.Clamp(context.GetConfigOption<double>("merchant-buy-hold-time", 0.6), 0, 1);
            BinaryPrimitives.WriteSingleLittleEndian(span.Slice(0x4E8, 4), (float)holdBuyTime);

            // Fix jet ski time
            var offsets = new[] { 0x714, 0x720, 0x724, 0x730, 0x734, 0x740, 0x744, 0x750, 0x770, 0x77C };
            foreach (var o in offsets)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span.Slice(o, 4), 420.0f);
            }

            context.SetFile(path, guiparamholdersettinguserdata);
        }
    }
}
