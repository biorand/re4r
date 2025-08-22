using System;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal struct Transform(RszInstance transform)
    {
        public Transform(ScnFile.GameObjectData gameObject)
            : this(gameObject.FindComponent("via.Transform") ?? throw new Exception("Game object has no transform"))
        {
        }

        public Vector3 Position
        {
            get => transform.Get<Vector3>("Position");
            set => transform.Set("Position", value);
        }

        public Quaternion Rotation
        {
            get => transform.Get<Quaternion>("Rotation");
            set => transform.Set("Rotation", value.ToVector4().ToQuaternion());
        }

        public Vector3 Scale
        {
            get => transform.Get<Vector3>("Scale");
            set => transform.Set("Scale", value);
        }

        public EulerAngles Eular
        {
            get => Rotation.ToEuler();
            set => Rotation = value.ToQuaternion();
        }

        public RszTool.via.mat4 Matrix
        {
            get
            {
                var position = Matrix4x4.CreateTranslation(Position);
                var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
                var scale = Matrix4x4.CreateScale(Scale);
                var result = scale * rotation * position;

                var mat4 = new RszTool.via.mat4();
                mat4.m00 = result.M11;
                mat4.m10 = result.M21;
                mat4.m20 = result.M31;
                mat4.m30 = result.M41;
                mat4.m01 = result.M12;
                mat4.m11 = result.M22;
                mat4.m21 = result.M32;
                mat4.m31 = result.M42;
                mat4.m02 = result.M13;
                mat4.m12 = result.M23;
                mat4.m22 = result.M33;
                mat4.m32 = result.M43;
                mat4.m03 = result.M14;
                mat4.m13 = result.M24;
                mat4.m23 = result.M34;
                mat4.m33 = result.M44;
                return mat4;
            }
        }
    }
}
