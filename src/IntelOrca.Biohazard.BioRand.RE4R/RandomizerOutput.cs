using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class RandomizerOutput
    {
        private readonly LogFiles _logFiles;
        private byte[]? _zipFile;
        private byte[]? _modFile;

        public byte[] PakFile { get; }

        internal RandomizerOutput(byte[] pakFile, LogFiles logFiles)
        {
            PakFile = pakFile;
            _logFiles = logFiles;
        }

        public byte[] GetOutputZip()
        {
            if (_zipFile != null)
                return _zipFile;

            _zipFile = BuildZipFile()
                .Build();
            return _zipFile;
        }

        public byte[] GetOutputMod()
        {
            if (_modFile != null)
                return _modFile;

            _modFile = BuildZipFile("Biorand/")
                .AddEntry("Biorand/pic.jpg", Resources.modimage)
                .AddEntry("Biorand/modinfo.ini",
                    Encoding.UTF8.GetBytes("name=Biorand\nversion=1.5\ndescription=RE4R randomizer\nscreenshot=pic.jpg\nauthor=IntelOrca\ncategory=!Other > Misc\n"))
                .Build();
            return _modFile;
        }

        private ZipFileBuilder BuildZipFile(string prefix = "")
        {
            return new ZipFileBuilder()
                .AddEntry($"{prefix}re_chunk_000.pak.patch_004.pak", PakFile)
                .AddEntry($"{prefix}input.log", Encoding.UTF8.GetBytes(_logFiles.Input))
                .AddEntry($"{prefix}process.log", Encoding.UTF8.GetBytes(_logFiles.Process))
                .AddEntry($"{prefix}output.log", Encoding.UTF8.GetBytes(_logFiles.Output));
        }
    }
}
