using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct ContextId(sbyte category, byte kind, int group, int index) : IEquatable<ContextId>
    {
        public sbyte Category { get; } = category;
        public byte Kind { get; } = kind;
        public int Group { get; } = group;
        public int Index { get; } = index;

        public bool IsEmpty => Category == 0 && Kind == 0 && Group == 0 && Index == 0;

        internal static ContextId FromRszValue(chainsaw.ContextID rszValue)
        {
            return new ContextId(rszValue._Category, rszValue._Kind, rszValue._Group, rszValue._Index);
        }

        public static ContextId FromRsz(IRszNode node)
        {
            var category = node.Get<sbyte>("_Category")!;
            var kind = node.Get<byte>("_Kind")!;
            var group = node.Get<int>("_Group")!;
            var index = node.Get<int>("_Index")!;
            return new ContextId(category, kind, group, index);
        }

        public ContextId WithIndex(int value) => new ContextId(Category, Kind, Group, value);

        public IRszNode ToRsz(RszTypeRepository repo)
        {
            var node = repo.Create("chainsaw.ContextID");
            node = node.SetField("_Category", Category);
            node = node.SetField("_Kind", Kind);
            node = node.SetField("_Group", Group);
            node = node.SetField("_Index", Index);
            return node;
        }

        internal chainsaw.ContextID ToRszValue()
        {
            return new chainsaw.ContextID()
            {
                _Category = Category,
                _Kind = Kind,
                _Group = Group,
                _Index = Index,
            };
        }

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
