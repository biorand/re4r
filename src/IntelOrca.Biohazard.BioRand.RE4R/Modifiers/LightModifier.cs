using System;
using System.Collections.Generic;
using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class LightModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {

        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            foreach (var l in _lighting)
            {
                var fullPath = $"natives/stm/_chainsaw/environment/scene/light/st40/{l}";
                randomizer.FileRepository.ModifyScnFile(fullPath, scene =>
                {
                    var gameObjectsToRemove = new List<Guid>();
                    scene = scene.VisitGameObjects(gameObject =>
                    {
                        gameObject = gameObject.VisitComponents(component =>
                        {
                            if (component.Type.Name == "via.render.SpotLight")
                            {
                                var i = component.Get<float>("Intensity");
                                component = component.Set("Intensity", i / 4);
                                component = component.Set("Color", new Vector3(0.544f, 0.8784f, 1));
                            }
                            else if (component.Type.Name == "via.render.IESLight")
                            {
                                var i = component.Get<float>("Intensity");
                                component = component.Set("Intensity", i / 4);
                                component = component.Set("Color", new Vector3(0.544f, 0.8784f, 1));
                            }
                            else if (component.Type.Name == "via.render.VolumetricFog")
                            {
                                gameObjectsToRemove.Add(gameObject.Guid);
                            }
                            return component;
                        });
                        return gameObject;
                    });
                    foreach (var gameObject in gameObjectsToRemove)
                    {
                        scene = scene.RemoveGameObject(gameObject);
                    }
                    return scene;
                });
            }
        }

        private static string[] _lighting = [
            "light_st40_000_cp10_chp1_0.scn.20",
            "light_st40_000_cp10_chp1_1.scn.20",
            "light_st40_000_cp10_chp1_2.scn.20",
            "light_st40_000_cp10_chp1_3.scn.20",
            "light_st40_000_cp10_chp2_1.scn.20",
            "light_st40_000_cp10_chp2_2.scn.20",
            "light_st40_000_cp10_chp2_3.scn.20",
            "light_st40_000.scn.20",
            "light_st40_010.scn.20",
            "light_st40_200_cp10_chp1_1.scn.20",
            "light_st40_200_cp10_chp1_3.scn.20",
            "light_st40_200_cp10_chp2_1.scn.20",
            "light_st40_200.scn.20",
            "light_st40_201_cp10_chp1_1.scn.20",
            "light_st40_201_cp10_chp1_3.scn.20",
            "light_st40_201_cp10_chp2_2.scn.20",
            "light_st40_201.scn.20",
            "light_st40_210_cp10_chp1_1.scn.20",
            "light_st40_210_cp10_chp1_3.scn.20",
            "light_st40_210_cp10_chp2_1.scn.20",
            "light_st40_210.scn.20",
            "light_st40_211_cp10_chp1_0.scn.20",
            "light_st40_211_cp10_chp1_1.scn.20",
            "light_st40_211_cp10_chp1_3.scn.20",
            "light_st40_211_cp10_chp2_1.scn.20",
            "light_st40_211.scn.20",
            "light_st40_212_cp10_chp1_1.scn.20",
            "light_st40_212_cp10_chp1_3.scn.20",
            "light_st40_212_cp10_chp2_2.scn.20",
            "light_st40_212.scn.20",
            "light_st40_213_cp10_chp1_1.scn.20",
            "light_st40_213_cp10_chp1_3.scn.20",
            "light_st40_213_cp10_chp2_2.scn.20",
            "light_st40_213.scn.20",
            "light_st40_220.scn.20",
            "light_st40_280.scn.20",
            "light_st40_500.scn.20",
            "light_st40_501.scn.20",
            "light_st40_502.scn.20",
            "light_st40_503.scn.20",
            "light_st40_504.scn.20",
            "light_st40_505_cp10_chp1_1.scn.20",
            "light_st40_505_cp10_chp1_3.scn.20",
            "light_st40_505_cp10_chp2_1.scn.20",
            "light_st40_505.scn.20",
            "light_st40_510.scn.20",
            "light_st40_530.scn.20",
            "light_st40_903.scn.20",
            "light_st40_904.scn.20",
            "light_st40_980.scn.20"
        ];
    }
}
