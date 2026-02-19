using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Patches
{
    internal class LightingPatch(IPatchContext context) : IPatch
    {
        public void Apply()
        {
            CopyLighting(44_100, "1_2", "1_1");
            CopyLighting(44_101, "1_2", "1_1");
            CopyLighting(44_110, "1_2", "1_1");
            CopyLighting(44_200, "1_2", "1_1");
            CopyLighting(44_201, "1_2", "1_1");
            CopyLighting(44_210, "1_2", "1_1");
            CopyLighting(44_400, "1_2", "1_1");
            CopyLighting(44_410, "1_2", "1_1");
            CopyLighting(45_100, "1_3", "1_1");
            CopyLighting(45_200, "1_3", "1_1");
            CopyLighting(45_210, "1_3", "1_1");
            CopyLighting(45_300, "1_3", "1_1");
            CopyLighting(45_301, "1_3", "1_1");
            CopyLighting(45_302, "1_3", "1_1");
            CopyLighting(45_303, "1_3", "1_1");
            CopyLighting(45_400, "1_3", "1_1");
            CopyLighting(45_401, "1_3", "1_1");
            CopyLighting(46_111, "1_3", "1_1");
            CopyLighting(46_210, "1_3", "1_1");
            CopyLighting(43_400, "2_1", "1_3");
            CopyLighting(43_410, "2_1", "1_3");
            CopyLighting(43_500, "2_1", "1_3");
            CopyLighting(46_111, "2_1", "1_3");
            CopyLighting(46_210, "2_1", "1_3");
            CopyLighting(47_100, "2_1", "1_3");
        }

        private void CopyLighting(int stage, string sourceChapter, string targetChapter)
        {
            var containerPath = GetLightPath(stage);
            var sourcePath = GetLightPath(stage, sourceChapter);
            var targetPath = GetLightPath(stage, targetChapter);

            context.SetFile(targetPath, context.GetFile(sourcePath) ?? throw new Exception("Source scene not found"));
            context.ModifyScnFile(containerPath, scene =>
            {
                var repo = context.TypeRepository;
                var location = stage / 1000;
                var number = stage % 1000;
                var campaign = 10;
                var newFolderName = $"light_st{location:00}_{number:000}_cp{campaign:00}_chp{targetChapter}";
                if (scene.Children.OfType<RszFolder>().Any(x => x.Name.Equals(newFolderName, StringComparison.OrdinalIgnoreCase)))
                    return scene;

                var children = scene.Children.ToBuilder();
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is RszFolder folder)
                    {
                        if (folder.Name.CompareTo(newFolderName, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            children.Insert(i, new RszFolder(
                                repo.Create("via.Folder")
                                    .Set("Name", newFolderName)
                                    .Set("Update", true)
                                    .Set("Draw", true)
                                    .Set("ScenePath", $"_Chainsaw/Environment/Scene/Light/st{location:00}/light_st{location:00}_{number:000}_cp{campaign:00}_chp{targetChapter}.scn"),
                                []));
                            break;
                        }
                    }
                }
                scene = scene.WithChildren(children.ToImmutable());

                scene = scene.Visit(node =>
                {
                    if (node is RszFolder folder)
                    {
                        var sourceObjects = folder.Children
                            .OfType<RszGameObject>()
                            .Where(x => x.Name.StartsWith($"chp{sourceChapter}"))
                            .ToArray();
                        if (sourceObjects.Length > 0)
                        {
                            var builder = folder.Children.ToBuilder();
                            foreach (var sourceObject in sourceObjects)
                            {
                                builder.Add(sourceObject
                                    .Clone()
                                    .WithName($"chp{targetChapter}" + sourceObject.Name.Substring(6)));
                            }
                            node = folder.WithChildren(builder
                                .OrderBy(x => GetName(x))
                                .ToImmutableArray());
                        }
                    }
                    return node;
                });


                return scene;
            });
        }

        private static string GetLightPath(int stage)
        {
            var location = stage / 1000;
            var number = stage % 1000;
            return $"natives/stm/_chainsaw/environment/scene/light/st{location:00}/light_st{location:00}_{number:000}.scn.20";
        }

        private static string GetLightPath(int stage, string chapter)
        {
            var location = stage / 1000;
            var number = stage % 1000;
            var campaign = 10;
            return $"natives/stm/_chainsaw/environment/scene/light/st{location:00}/light_st{location:00}_{number:000}_cp{campaign:00}_chp{chapter}.scn.20";
        }

        private static string GetName(IRszNode node)
        {
            if (node is RszGameObject gameObject)
                return gameObject.Name;
            if (node is RszFolder folder)
                return folder.Name;
            return "";
        }
    }
}
