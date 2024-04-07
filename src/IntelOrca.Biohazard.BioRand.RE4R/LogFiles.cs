namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct LogFiles(string input, string process, string output)
    {
        public string Input { get; } = input;
        public string Process { get; } = process;
        public string Output { get; } = output;
    }
}
