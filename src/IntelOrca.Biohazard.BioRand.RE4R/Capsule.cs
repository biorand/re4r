using System.Numerics;
using System.Runtime.InteropServices;
using IntelOrca.Biohazard.REE.Rsz;

namespace via
{
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    internal struct Capsule
    {
        [FieldOffset(0)]
        public Vector3 Start;
        [FieldOffset(16)]
        public Vector3 End;
        [FieldOffset(32)]
        public float Radius;

        public readonly RszValueNode ToNode()
        {
            var data = new byte[48];
            MemoryMarshal.Write(data, in this);
            return new RszValueNode(RszFieldType.Capsule, data);
        }

        public static Capsule FromNode(IRszNode node) => FromNode((RszValueNode)node);
        public static Capsule FromNode(RszValueNode node)
        {
            var result = MemoryMarshal.Cast<byte, Capsule>(node.Data.Span);
            return result[0];
        }
    }
}
