using System.IO;
using System.IO.Compression;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class RandomizerOutput
    {
        private readonly FileRepository _fileRepository;
        private byte[]? _pakFile;
        private byte[]? _modFile;

        internal RandomizerOutput(FileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public byte[] GetOutputPakFile()
        {
            if (_pakFile == null)
                _pakFile = _fileRepository.GetOutputPakFile();
            return _pakFile;
        }

        public byte[] GetOutputMod()
        {
            if (_modFile != null)
                return _modFile;

            var pakFile = _fileRepository.GetOutputPakFile();
            var tempDir = Directory.CreateTempSubdirectory()!;
            try
            {
                var subdir = tempDir.CreateSubdirectory("Biorand");
                File.WriteAllBytes(Path.Combine(subdir.FullName, "re_chunk_000.pak.patch_004.pak"), pakFile);
                File.WriteAllBytes(Path.Combine(subdir.FullName, "pic.jpg"), Resources.modimage);
                File.WriteAllText(Path.Combine(subdir.FullName, "modinfo.ini"),
                    "name=Biorand\nversion=1.5\ndescription=RE4R randomizer\nscreenshot=pic.jpg\nauthor=IntelOrca\ncategory=!Other > Misc");

                var ms = new MemoryStream();
                ZipFile.CreateFromDirectory(tempDir.FullName, ms);
                _modFile = ms.ToArray();
                return _modFile;
            }
            finally
            {
                tempDir.Delete(recursive: true);
            }
        }

        public void WriteOutputPakFile(string path)
        {
            _fileRepository.WriteOutputPakFile(path);
        }

        public void WriteOutputFolder(string path)
        {
            _fileRepository.WriteOutputFolder(path);
        }
    }
}
