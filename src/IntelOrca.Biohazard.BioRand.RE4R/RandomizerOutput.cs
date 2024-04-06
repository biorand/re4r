namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class RandomizerOutput
    {
        private readonly FileRepository _fileRepository;

        internal RandomizerOutput(FileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public byte[] GetOutputPakFile()
        {
            return _fileRepository.GetOutputPakFile();
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
