using System;
using System.Numerics;

namespace IntelOrca.Biohazard.BioRand.RE4R.Extensions
{
    public static class NumericExtensions
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

        public static Vector3 ToEuler(this Quaternion rotation)
        {
            var x = rotation.X;
            var y = rotation.Y;
            var z = rotation.Z;
            var w = rotation.W;
            var yaw = MathF.Atan2(2 * (y * w + x * z), 1 - 2 * (y * y + z * z));
            var pitch = MathF.Asin(2 * (y * z - x * w));
            var roll = MathF.Atan2(2 * (x * y + z * w), 1 - 2 * (x * x + y * y));
            var yawDegrees = RadToDeg(yaw);
            var pitchDegrees = RadToDeg(pitch);
            var rollDegrees = RadToDeg(roll);
            return new Vector3(yawDegrees, pitchDegrees, rollDegrees);

            static float RadToDeg(float radians) => radians * (180f / MathF.PI);
        }
    }
}
