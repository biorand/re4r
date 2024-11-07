using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal sealed class RecipeDefinitionFile
    {
        public static RecipeDefinitionFile Default { get; } = Resources.recipe.DeserializeJson<RecipeDefinitionFile>();

        public Recipe[] Recipes { get; set; } = [];

        public class Recipe
        {
            public string Name { get; set; } = "";
            public int Id { get; set; }
            public int Category { get; set; }
            public RecipeInputOutput[] Input { get; set; } = [];
            public RecipeInputOutput Output { get; set; } = new();
        }

        public class RecipeInputOutput
        {
            public int Id { get; set; }
            public int Count { get; set; }
        }
    }
}
