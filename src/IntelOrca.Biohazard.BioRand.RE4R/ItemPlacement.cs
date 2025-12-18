using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    [DebuggerDisplay("{GuidOrAuto}")]
    internal class ItemPlacement
    {
        [Key]
        public int Row { get; set; }
        public Campaign Campaign { get; set; }
        public int Chapter { get; set; }
        public Guid Guid { get; set; }
        public string Description { get; set; } = "";
        public int Stage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
        public string Container { get; set; } = "";
        public ImmutableArray<string> Tags { get; set; } = [];
        public ImmutableArray<string> Include { get; set; } = [];
        public ImmutableArray<string> Exclude { get; set; } = [];
        public ImmutableArray<string> Events { get; set; } = [];

        public Item OldItem { get; set; }
        public chainsaw.ContextID ContextId { get; set; } = new();

        public Guid GuidOrAuto => Guid == default ? $"item_{Row}".GetGuidHash() : Guid;
        public bool IsExtra => Guid == default || Description.StartsWith("[EXTRA]");
    }
}
