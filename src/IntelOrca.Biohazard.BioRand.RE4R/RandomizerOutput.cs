using System.Text;
using REE;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class RandomizerOutput
    {
        private byte[]? _zipFile;
        private byte[]? _modFile;

        public PakFileBuilder PakFile { get; }
        public LogFiles LogFiles { get; }

        internal RandomizerOutput(PakFileBuilder pakFile, LogFiles logFiles)
        {
            PakFile = pakFile;
            LogFiles = logFiles;
        }

        public byte[] GetOutputZip()
        {
            if (_zipFile != null)
                return _zipFile;

            _zipFile = BuildZipFile()
                .AddEntry($"re_chunk_000.pak.patch_004.pak", PakFile.ToByteArray())
                .Build();
            return _zipFile;
        }

        public byte[] GetOutputMod()
        {
            if (_modFile != null)
                return _modFile;

            var zipFile = BuildZipFile();
            foreach (var entry in PakFile.Entries)
            {
                zipFile.AddEntry($"Biorand/{entry.Key}", entry.Value);
            }
            _modFile = zipFile
                .AddEntry("Biorand/pic.jpg", Resources.modimage)
                .AddEntry("Biorand/modinfo.ini",
                    Encoding.UTF8.GetBytes("name=Biorand\nversion=1.5\ndescription=RE4R randomizer\nscreenshot=pic.jpg\nauthor=IntelOrca\ncategory=!Other > Misc\n"))
                .Build();
            return _modFile;
        }

        private ZipFileBuilder BuildZipFile(string logPrefix = "")
        {
            return new ZipFileBuilder()
                .AddEntry($"{logPrefix}input.log", Encoding.UTF8.GetBytes(LogFiles.Input))
                .AddEntry($"{logPrefix}process.log", Encoding.UTF8.GetBytes(LogFiles.Process))
                .AddEntry($"{logPrefix}output.log", Encoding.UTF8.GetBytes(LogFiles.Output));
        }
    }
}
