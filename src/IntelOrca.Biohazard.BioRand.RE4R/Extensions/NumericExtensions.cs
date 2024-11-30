using System.Numerics;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    internal static class NumericExtensions
    {
        public static int RoundPrice(this double value)
        {
            if (value >= 100000)
                return (int)(value / 100000) * 100000;
            if (value >= 10000)
                return (int)(value / 1000) * 1000;
            if (value >= 1000)
                return (int)(value / 1000) * 1000;
            if (value >= 100)
                return (int)(value / 100) * 100;
            if (value >= 10)
                return (int)(value / 10) * 10;
            return (int)value;
        }

        public static Vector3 ToVector3(this Vector4 v) => new(v.X, v.Y, v.Z);
        public static Vector4 ToVector4(this Quaternion q) => new(q.X, q.Y, q.Z, q.W);
        public static Quaternion ToQuaternion(this Vector4 v) => new(v.X, v.Y, v.Z, v.W);
        public static EulerAngles ToEuler(this Quaternion q) => new(q);
    }
}
