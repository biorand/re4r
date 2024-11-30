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
            get => transform.Get<Vector4>("v0").ToVector3();
            set => transform.Set("v0", new Vector4(value, 0));
        }

        public Quaternion Rotation
        {
            get => transform.Get<Vector4>("v1").ToQuaternion();
            set => transform.Set("v1", value.ToVector4());
        }

        public Vector3 Scale
        {
            get => transform.Get<Vector4>("v2").ToVector3();
            set => transform.Set("v2", new Vector4(value, 0));
        }

        public EulerAngles Eular
        {
            get => Rotation.ToEuler();
            set => Rotation = value.ToQuaternion();
        }
    }
}
