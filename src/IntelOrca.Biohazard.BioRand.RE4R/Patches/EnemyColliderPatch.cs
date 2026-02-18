using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class EnemyColliderPatch(IPatchContext context) : IPatch
    {
        private readonly Dictionary<string, ColliderSettings> _colliderSettings = new()
        {
            ["ch1f2z0"] = new ColliderSettings
            {
                BasePath = "_chainsaw",
                CharacterControllerRadius = 0.36f,
                DefaultShapeRadius = 0.41f,
                DefaultShapeHeight = 1.8f,
                DefaultAdjustShapeRadius = 0.41f
            },
            ["ch1d0z0"] = new ColliderSettings
            {
                BasePath = "_chainsaw",
                CharacterControllerRadius = 0.35f,
                DefaultShapeRadius = 0.40f,
                DefaultShapeHeight = 1.75f,
                DefaultAdjustShapeRadius = 0.40f
            },
            ["ch4fbz0"] = new ColliderSettings
            {
                BasePath = "_anotherorder",
                CharacterControllerRadius = 0.36f,
                DefaultShapeRadius = 0.6f,
                DefaultShapeHeight = 2.0f,
                DefaultAdjustShapeRadius = 0.5f
            }
        };

        public void Apply()
        {
            var enemiesUnleashed = context.GetConfigOption<bool>("enemies-unleashed", false);
            
            foreach (var (characterId, settings) in _colliderSettings)
            {
                // If enemies-unleashed is not true, only process ch4fbz0
                if (!enemiesUnleashed && characterId != "ch4fbz0")
                    continue;

                var path = $"natives/stm/{settings.BasePath}/appsystem/character/{characterId}/{characterId}_body.pfb.17";

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

        private class ColliderSettings
        {
            public string BasePath { get; set; } = "_chainsaw";
            public float CharacterControllerRadius { get; set; }
            public float DefaultShapeRadius { get; set; }
            public float DefaultShapeHeight { get; set; }
            public float DefaultAdjustShapeRadius { get; set; }
        }
    }
}
