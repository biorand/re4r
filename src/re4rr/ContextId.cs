namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct ContextId(sbyte category, byte kind, int group, int index)
    {
        public sbyte Category { get; } = category;
        public byte Kind { get; } = kind;
        public int Group { get; } = group;
        public int Index { get; } = index;

        public ContextId WithIndex(int value) => new ContextId(Category, Kind, Group, value);

        public override string ToString() => $"{Category},{Kind},{Group},{Index}";
    }
}
