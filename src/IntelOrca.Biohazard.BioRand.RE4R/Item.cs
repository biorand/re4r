namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct Item(int id, int count)
    {
        public int Id { get; } = id;
        public int Count { get; } = count;
        public bool IsAutomatic => Id == -1;

        public override string ToString() => IsAutomatic ? "(automatic)" : $"{Id} x{Count}";
    }
}
