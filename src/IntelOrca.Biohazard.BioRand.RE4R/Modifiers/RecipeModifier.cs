using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class RecipeModifier : Modifier
    {
        private static string GetPath(Campaign campaign)
        {
            return campaign == Campaign.Leon
                ? "natives/stm/_chainsaw/appsystem/ui/userdata/itemcraftsettinguserdata.user.2"
                : "natives/stm/_anotherorder/appsystem/ui/userdata/itemcraftsettinguserdata_ao.user.2";
        }

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var path = GetPath(randomizer.Campaign);
            var fileRepository = randomizer.FileRepository;
            var userFile = fileRepository.DeserializeUserFile<ItemCraftSettingUserdata>(path);
            var ids = userFile._RecipeIdOrders.ToArray();
            var itemRepo = ItemDefinitionRepository.Default;
            foreach (var id in ids)
            {
                var data = userFile._Datas.FirstOrDefault(x => x._RecipeID == id);
                if (data == null)
                    continue;

                var inputs = data._RequiredItems.Select(x => new Item(x._ItemID, x._RequiredNum)).ToArray();
                logger.Push($"Recipe {id}: Category = {data._Category}, Craft Time = {data._CraftTime}, Draw Wave = {data._DrawWave}, Requires = {string.Join(" + ", inputs)}");
                foreach (var output in data._ResultSettings)
                {
                    var itemName = itemRepo.GetName(output._Result._ItemID);
                    var min = output._Result._GeneratedNumMin;
                    var max = output._Result._GeneratedNumMax;

                    logger.Push($"Result: {itemName}, Difficulty = {output._Difficulty}, Min = {min}, Max = {max}");
                    if (output._Result._GenerateNumUniqueSetting._ItemId != -1)
                    {
                        var uniqueName = itemRepo.GetName(output._Result._GenerateNumUniqueSetting._ItemId);
                        var uniqueGenerateMin = output._Result._GenerateNumUniqueSetting._GenerateNumMin;
                        var uniqueGenerate = output._Result._GenerateNumUniqueSetting._GenerateNum;
                        var durability = output._Result._GenerateNumUniqueSetting._Durability;
                        logger.LogLine($"Unique: {uniqueName}, Generate = {uniqueGenerate}, Generate Min = {uniqueGenerateMin}, Durability = {durability}");
                    }
                    logger.Pop();

                }
                foreach (var b in data._BonusSetting._Datas)
                {
                    logger.LogLine($"Bonus: Count = {b._BonusCount}, Has Count = {b._HasCount}, Probability = {b._Probability}");
                }

                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            if (!randomizer.GetConfigOption<bool>("inventory-additional-recipes"))
                return;

            var recipeData = randomizer.DynamicData.GetData(DynamicDataName.Recipe)!;
            var recipes = Csv.Deserialize<Recipe>(recipeData);

            var path = GetPath(randomizer.Campaign);
            var fileRepository = randomizer.FileRepository;
            fileRepository.ModifyUserFile(path, root =>
            {
                var craft = RszSerializer.Deserialize<ItemCraftSettingUserdata>(root)!;
                foreach (var recipe in recipes)
                {
#if ENABLE_BETA_FEATURES
                    if (recipe.InputItemId1 == ItemIds.AmmoFuel ||
                        recipe.InputItemId2 == ItemIds.AmmoFuel ||
                        recipe.OutputItemId == ItemIds.AmmoFuel)
                    {
                        continue;
                    }
#endif

                    var outputCount = recipe.Output.Count == 0 ? 1 : recipe.Output.Count;
                    var newCraft = new ItemCraftRecipe()
                    {
                        _RecipeID = recipe.Id,
                        _Category = recipe.Category,
                        _CraftTime = 1,
                        _RequiredItems = [
                            ..recipe.Input.Select(x => new ItemCraftMaterial()
                            {
                                _ItemID = x.Id,
                                _RequiredNum = x.Count
                            })
                        ],
                        _ResultSettings = [
                            new ItemCraftResultSetting()
                            {
                                _Difficulty = 10,
                                _Result = new ItemCraftResult()
                                {
                                    _ItemID = recipe.Output.Id,
                                    _GeneratedNumMin = recipe.Output.Count,
                                    _GeneratedNumMax = recipe.Output.Count,
                                    _GenerateNumUniqueSetting = new ItemCraftGenerateNumUniqueSetting()
                                    {
                                        _ItemId = -1,
                                        _Durability = -1,
                                        _GenerateNum = -1,
                                        _GenerateNumMin = -1
                                    }
                                }
                            },
                            new ItemCraftResultSetting()
                            {
                                _Difficulty = 20,
                                _Result = new ItemCraftResult()
                                {
                                    _ItemID = recipe.Output.Id,
                                    _GeneratedNumMin = recipe.Output.Count,
                                    _GeneratedNumMax = recipe.Output.Count,
                                    _GenerateNumUniqueSetting = new ItemCraftGenerateNumUniqueSetting()
                                    {
                                        _ItemId = -1,
                                        _Durability = -1,
                                        _GenerateNum = -1,
                                        _GenerateNumMin = -1
                                    }
                                }
                            }
                        ]
                    };

                    craft._Datas.RemoveAll(x => x._RecipeID == recipe.Id);
                    craft._RecipeIdOrders.Remove(recipe.Id);
                    var lastExistingSameType = craft._Datas
                        .FindLast(x => x._ResultSettings[0]._Result._ItemID == recipe.Output.Id);
                    if (lastExistingSameType == null)
                    {
                        craft._RecipeIdOrders.Add(recipe.Id);
                    }
                    else
                    {
                        var insertIndex = craft._RecipeIdOrders.IndexOf(lastExistingSameType._RecipeID) + 1;
                        craft._RecipeIdOrders.Insert(insertIndex, recipe.Id);
                    }
                    craft._Datas.Add(newCraft);
                }
                return (RszObjectNode)RszSerializer.Serialize(root.Type, craft);
            });
        }

        internal sealed class Recipe
        {
            public string Name { get; set; } = "";
            public int Id { get; set; }
            public int Category { get; set; }
            public int InputItemId0 { get; set; }
            public int InputItemCount0 { get; set; }
            public int InputItemId1 { get; set; }
            public int InputItemCount1 { get; set; }
            public int InputItemId2 { get; set; }
            public int InputItemCount2 { get; set; }
            public int OutputItemId { get; set; }
            public int OutputItemCount { get; set; }

            public ImmutableArray<RecipeInputOutput> Input =>
                new[] {
                    new RecipeInputOutput() { Id = InputItemId0, Count = InputItemCount0 },
                    new RecipeInputOutput() { Id = InputItemId1, Count = InputItemCount1 },
                    new RecipeInputOutput() { Id = InputItemId2, Count = InputItemCount2 }
                }
                .Where(x => x.Count != 0)
                .ToImmutableArray();

            public RecipeInputOutput Output => new RecipeInputOutput() { Id = OutputItemId, Count = OutputItemCount };
        }

        internal sealed class RecipeInputOutput
        {
            public int Id { get; init; }
            public int Count { get; init; }
        }
    }
}
