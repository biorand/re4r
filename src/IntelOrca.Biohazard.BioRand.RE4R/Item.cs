namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct Item(int id, int count)
    {
        public int Id { get; } = id;
        public int Count { get; } = count;

        public override string ToString() => Id == -1 ? "(automatic)" : $"{Id} x{Count}";
    }
}
