using System;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.Extensions;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal struct Transform
    {
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }

        public Transform(RszGameObject gameObject)
            : this(gameObject.FindComponent("via.Transform") ?? throw new Exception("Game object has no transform"))
        {
        }

        public Transform(RszObjectNode node)
        {
            Position = node.Get<Vector3>("Position");
            Rotation = node.Get<Quaternion>("Rotation");
            Scale = node.Get<Vector3>("Scale");
        }

        public EulerAngles Eular
        {
            readonly get => Rotation.ToEuler();
            set => Rotation = value.ToQuaternion();
        }

        public readonly Matrix4x4 Matrix
        {
            get
            {
                var position = Matrix4x4.CreateTranslation(Position);
                var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
                var scale = Matrix4x4.CreateScale(Scale);
                return scale * rotation * position;
            }
        }

        public readonly RszObjectNode ToComponent(IReeRandomizerContext context)
        {
            return context.GetService<RszFactory>().CreateTransform(Position, Rotation, Scale);
        }
    }
}
