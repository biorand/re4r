using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;
using RszInstance = RszTool.RszInstance;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public readonly struct ContextId(sbyte category, byte kind, int group, int index) : IEquatable<ContextId>
    {
        public sbyte Category { get; } = category;
        public byte Kind { get; } = kind;
        public int Group { get; } = group;
        public int Index { get; } = index;

        internal static ContextId FromRszValue(chainsaw.ContextID rszValue)
        {
            return new ContextId(rszValue._Category, rszValue._Kind, rszValue._Group, rszValue._Index);
        }

        public static ContextId FromRsz(RszInstance instance)
        {
            var category = instance.Get<sbyte>("_Category")!;
            var kind = instance.Get<byte>("_Kind")!;
            var group = instance.Get<int>("_Group")!;
            var index = instance.Get<int>("_Index")!;
            return new ContextId(category, kind, group, index);
        }

        public static ContextId FromRsz(REE.Rsz.IRszNode node)
        {
            var category = node.Get<sbyte>("_Category")!;
            var kind = node.Get<byte>("_Kind")!;
            var group = node.Get<int>("_Group")!;
            var index = node.Get<int>("_Index")!;
            return new ContextId(category, kind, group, index);
        }

        public ContextId WithIndex(int value) => new ContextId(Category, Kind, Group, value);

        public void CopyTo(RszInstance instance)
        {
            instance.Set("_Category", Category);
            instance.Set("_Kind", Kind);
            instance.Set("_Group", Group);
            instance.Set("_Index", Index);
        }

        public REE.Rsz.IRszNode ToRsz(RszTypeRepository repo)
        {
            var node = repo.Create("chainsaw.ContextID");
            node = node.SetField("_Category", Category);
            node = node.SetField("_Kind", Kind);
            node = node.SetField("_Group", Group);
            node = node.SetField("_Index", Index);
            return node;
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
