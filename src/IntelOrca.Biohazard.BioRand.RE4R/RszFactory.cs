using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal static class RszFactory
    {
        public static RszTypeRepository Repository = FileRepository.RszRepository;

        public static RszGameObject CreateGameObject(string name, string prefab, ImmutableArray<RszObjectNode> components)
        {
            return new RszGameObject(
                Guid.NewGuid(),
                "_Chainsaw/AppSystem/Prefab/ch1c0SpawnParam.pfb",
                Repository.Create("via.GameObject")
                    .Set("Name", name)
                    .Set("UpdateSelf", true)
                    .Set("DrawSelf", true)
                    .Set("Timescale", -1),
                components,
                []);
        }

        public static RszObjectNode CreateTransform(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
        {
            return Repository.Create("via.Transform")
                .Set("Position", position ?? Vector3.Zero)
                .Set("Rotation", rotation ?? Quaternion.Identity)
                .Set("Scale", scale ?? Vector3.One);
        }

        public static RszGameObject CreateSpawnController(string name)
        {
            var transform = CreateTransform();
            var characterSpawnControllerComponent = Repository
                .Create("chainsaw.CharacterSpawnController")
                    .Set("_Enabled", true)
                    .Set("_DifficutyParam", 63U)
                    .Set("_GUID", Guid.NewGuid());

            return CreateGameObject(
                name,
                "_Chainsaw/AppSystem/Prefab/CharacterSpawnController.pfb",
                [transform, characterSpawnControllerComponent]);
        }

        public static RszGameObject CreateSpawnPointController(Guid guid, string name, float spawnDistanceMin, object[] enemies)
        {
            var transform = CreateTransform();
            var characterSpawnControllerComponent = Repository
                .Create("chainsaw.CharacterSpawnPointController")
                    .Set("_Enabled", true)
                    .Set("_DifficutyParam", 63U)
                    .Set("_GUID", guid)
                    .Set("_ActiveCountLimit", 100)
                    .Set("_ActiveCountType", 0)
                    .Set("_IntervalTime", 30.0f)
                    .Set("_SpawnDistanceMin", spawnDistanceMin)
                    .Set("_SpawnPoints",
                        enemies.Select(e =>
                        {
                            return Repository.Create("chainsaw.CharacterSpawnPoint")
                                .Set("_Transform", CreateMatrix(e))
                                .Set("_IsOutOfCameraOnly", false)
                                .Set("_CoolDownTime", 3.0f);
                        }).ToArray());

            return CreateGameObject(
                name,
                "_Chainsaw/AppSystem/Prefab/CharacterSpawnPointController.pfb",
                [transform, characterSpawnControllerComponent]);
        }

        private static Matrix4x4 CreateMatrix(object o)
        {
            if (o is Enemy e)
            {
                var transformComponent = e.GameObject.FindComponent("via.Transform")!;
                var position = Matrix4x4.CreateTranslation(transformComponent.Get<Vector3>("Position"));
                var rotation = Matrix4x4.CreateFromQuaternion(transformComponent.Get<Quaternion>("Rotation"));
                var scale = Matrix4x4.CreateScale(transformComponent.Get<Vector3>("Scale"));
                var result = scale * rotation * position;
                return result;
            }
            else if (o is AreaExtraEnemy ee)
            {
                return CreateMatrix(new Vector3(ee.X, ee.Y, ee.Z), ee.Direction);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static Matrix4x4 CreateMatrix(Vector3 position, float direction)
        {
            var translate = Matrix4x4.CreateTranslation(position);
            var rotation = Matrix4x4.CreateFromYawPitchRoll(direction * MathF.PI / 180.0f, 0, 0);
            return rotation * translate;
        }
    }
}
