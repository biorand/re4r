using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct ContextId(sbyte category, byte kind, int group, int index) : IEquatable<ContextId>
    {
        public sbyte Category { get; } = category;
        public byte Kind { get; } = kind;
        public int Group { get; } = group;
        public int Index { get; } = index;

        public static ContextId FromRsz(RszInstance instance)
        {
            var category = instance.Get<sbyte>("_Category")!;
            var kind = instance.Get<byte>("_Kind")!;
            var group = instance.Get<int>("_Group")!;
            var index = instance.Get<int>("_Index")!;
            return new ContextId(category, kind, group, index);
        }

        public ContextId WithIndex(int value) => new ContextId(Category, Kind, Group, value);

        public override string ToString() => $"CTXID({Category},{Kind},{Group},{Index})";

        public override bool Equals(object? obj)
        {
            return obj is ContextId id && Equals(id);
        }

        public bool Equals(ContextId other)
        {
            return Category == other.Category &&
                   Kind == other.Kind &&
                   Group == other.Group &&
                   Index == other.Index;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Category, Kind, Group, Index);
        }

        public static bool operator ==(ContextId left, ContextId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContextId left, ContextId right)
        {
            return !(left == right);
        }
    }
}
