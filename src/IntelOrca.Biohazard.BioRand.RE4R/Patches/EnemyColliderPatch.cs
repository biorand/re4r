using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class EnemyColliderPatch(IPatchContext context) : IPatch
    {
        private readonly List<ColliderSettings> _colliderSettings =
        [
            new ColliderSettings
            {
                CharacterId = "ch1f2z0",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.36f,
                DefaultShapeRadius = 0.41f,
                DefaultShapeHeight = 1.7f,
                DefaultAdjustShapeRadius = 0.41f
            },
            new ColliderSettings
            {
                CharacterId = "ch1d0z0",
                BasePaths = ["_chainsaw", "_anotherorder"],
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.40f,
                DefaultShapeHeight = 1.75f,
                DefaultAdjustShapeRadius = 0.40f
            },
            new ColliderSettings
            {
                CharacterId = "ch1d3z0",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.35f,
                DefaultShapeHeight = 1.6f,
                DefaultAdjustShapeRadius = 0.35f
            },
            new ColliderSettings
            {
                CharacterId = "ch1b5z1",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.40f,
                DefaultShapeHeight = 1.7f,
                DefaultAdjustShapeRadius = 0.40f
            },
            new ColliderSettings
            {
                CharacterId = "ch1d2z0",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.35f,
                DefaultShapeHeight = 1.6f,
                DefaultAdjustShapeRadius = 0.35f
            },
            new ColliderSettings
            {
                CharacterId = "ch4fbz0",
                BasePaths = ["_anotherorder"],
                CharacterControllerRadius = 0.36f,
                DefaultShapeRadius = 0.6f,
                DefaultShapeHeight = 2.0f,
                DefaultAdjustShapeRadius = 0.5f
            },
            new ColliderSettings
            {
                CharacterId = "Ch1f4z1",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.36f,
                DefaultShapeRadius = 0.6f,
                DefaultShapeHeight = 2.0f,
                DefaultAdjustShapeRadius = 0.5f
            },
            new ColliderSettings
            {
                CharacterId = "Ch1f7z0",
                BasePaths = ["_chainsaw"],
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.40f,
                DefaultShapeHeight = 1.75f,
                DefaultAdjustShapeRadius = 0.40f
            }
        ];

        public void Apply()
        {
            var enemiesUnleashed = context.GetConfigOption<bool>("enemies-unleashed", false);

            foreach (var settings in _colliderSettings)
            {
                if (!enemiesUnleashed && settings.CharacterId != "ch4fbz0")
                    continue;

                foreach (var basePath in settings.BasePaths)
                {
                    var path = $"natives/stm/{basePath}/appsystem/character/{settings.CharacterId}/{settings.CharacterId}_body.pfb.17";
                    
                    if (context.GetFile(path) == null)
                    {
                        path = $"natives/stm/{basePath}/appsystem/character/{settings.CharacterId}/{settings.CharacterId}_body_ao.pfb.17";
                        if (context.GetFile(path) == null)
                            continue;
                    }

                    context.ModifyPfbFile(path, scene =>
                    {
                        var gameObject = scene.Children.OfType<RszGameObject>().FirstOrDefault();
                        if (gameObject == null)
                            return scene;

                        var characterController = gameObject.FindComponent("via.physics.CharacterController");
                        if (characterController != null)
                        {
                            characterController = characterController.Set("Radius", settings.CharacterControllerRadius);
                            gameObject = gameObject.AddOrUpdateComponent(characterController);
                        }

                        var characterControllerSupporter = gameObject.FindComponent("chainsaw.CharacterControllerSupporter");
                        if (characterControllerSupporter != null)
                        {
                            characterControllerSupporter = characterControllerSupporter
                                .Set("_DefaultShape._Radius", settings.DefaultShapeRadius)
                                .Set("_DefaultShape._Height", settings.DefaultShapeHeight);

                            characterControllerSupporter = characterControllerSupporter
                                .Set("_DefaultAdjustShape._Radius", settings.DefaultAdjustShapeRadius);

                            gameObject = gameObject.AddOrUpdateComponent(characterControllerSupporter);
                        }

                        scene = scene.UpdateGameObject(gameObject);
                        return scene;
                    });
                }
            }
        }

        private class ColliderSettings
        {
            public required string CharacterId { get; set; }
            public required string[] BasePaths { get; set; }
            public float CharacterControllerRadius { get; set; }
            public float DefaultShapeRadius { get; set; }
            public float DefaultShapeHeight { get; set; }
            public float DefaultAdjustShapeRadius { get; set; }
        }
    }
}
