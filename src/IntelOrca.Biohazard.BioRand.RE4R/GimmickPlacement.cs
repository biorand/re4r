using System;
using System.Collections.Immutable;
using System.Numerics;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class GimmickPlacement
    {
        [RowNumber]
        public int Row { get; set; }
        public string Kind { get; set; } = "";
        public Guid Guid { get; set; }
        public int Stage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
        public Guid Condition { get; set; }
        public int Chapter { get; set; }
        public Campaign Campaign { get; set; }
        public ImmutableArray<string> Tags { get; set; } = [];
        public ImmutableArray<string> Events { get; set; } = [];
        public string Param1 { get; set; } = "";
        public string Param2 { get; set; } = "";

        public bool Vanilla { get; set; }

        public Guid GuidOrAuto => Guid == default ? $"item_{Row}".GetGuidHash() : Guid;
        public Vector3 Position => new(X, Y, Z);
        public EulerAngles Eular => new(Yaw, Pitch, Roll);

        public override string ToString()
        {
            return $"{Row}_{Kind}";
        }
    }
}
