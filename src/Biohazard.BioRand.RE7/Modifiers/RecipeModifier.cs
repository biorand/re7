using app;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using System.Collections.Immutable;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

/// <summary>
/// Using this modifier properly requires REFramework (_Data/REF_Scripts/RecipeMods.lua).
/// This is because the game has very strict limitations for what is shown in the combine GUI.
/// </summary>
internal class RecipeModifier : Modifier {
    private readonly Randomizer _randomizer;

    public RecipeModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    // The combine GUI only allows 20 slots, even in a modded state (4 cols, 5 rows).
    public const int MaxRecipeCount = 20;

    private const string RandomizerKey = "modifier/recipes";

    private static readonly string DictionaryCombineDataPath =
        "prefab/item/dictionarycombinedata.user".UserFile();

    private static readonly string ItemCombineDataPath = "prefab/item/itemcombinedata.user".UserFile();

    private static readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;
    private List<Recipe> _originalRecipes = new();
    private List<DictionaryCombineData.Data> _originalDictCombineData = new();

    private static void LogRecipeState(RandomizerLogger logger, List<Recipe> recipes,
        List<DictionaryCombineData.Data> dict, bool beforeModifications) {
        var adjective = beforeModifications ? "Original" : "Modded";
        logger.Push($"{adjective} crafting recipes");

        foreach (var recipe in recipes) {
            logger.LogLine(recipe.Format());
        }

        logger.Pop();

        logger.Push($"{adjective} crafting dictionary");
        foreach (var itemId in dict.Select(d => _itemDefinitions.FromId(d.ItemDataID)?.Name).Choose()) {
            logger.LogLine(itemId);
        }

        logger.Pop();
    }

    public override void LogState(RandomizerLogger logger) {
        var randomizer = _randomizer;
        _originalRecipes = randomizer.FileRepository.DeserializeUserFile<ItemCombineData>(ItemCombineDataPath)._Datas;
        _originalDictCombineData = randomizer.FileRepository
            .DeserializeUserFile<DictionaryCombineData>(DictionaryCombineDataPath)._Datas;
        LogRecipeState(logger, _originalRecipes, _originalDictCombineData, beforeModifications: true);
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var debug = randomizer.DebugRecipes;
        if (debug.Enabled) {
            ApplyDebugRecipes(debug, logger);
            return;
        }
        var addNewRecipes = randomizer.GetConfigOption<bool>("recipes-add-new");

        if (!addNewRecipes) {
            return;
        }

        var mode = randomizer.GetConfigOption<string>("recipes-randomization-mode");
        var rng = randomizer.GetRng(RandomizerKey);
        if (mode == "No crafting") {
            logger.LogLine("User chose to disable crafting entirely. Removing all recipes now.");
            AddRecipes(randomizer, [], clear: true);
            RebuildDictionary(randomizer, []);
            return;
        }

        var csv = randomizer.DynamicData.GetData(DynamicDataName.Recipes) ??
                  throw new Exception("Unable to get recipe data");
        var recipes = Csv.Deserialize<RecipeModel>(csv)
            .Where(r => r.Enabled)
            .Where(r => IsValidRecipe(r, logger))
            .ToImmutableList();

        // Apply config
        var recipePool = (mode switch{
            "Easy" => recipes.Where(r => r.Pool == RecipePool.Easy),
            "Balanced" => recipes.Where(r => r.Pool == RecipePool.Balanced),
            "Chaos" => recipes.Where(r => r.Pool == RecipePool.Chaos),
            "Crazy" => recipes.Where(r => r.Pool == RecipePool.Crazy),
            _ => throw new ArgumentException($"Invalid recipe randomization mode '{mode}' supplied!")
        }).ToList();

        if (!randomizer.GetConfigOption<bool>("recipes-allow-stabilizers-and-steroids")) {
            recipePool.RemoveAll(recipe => _itemDefinitions.NameToId(recipe.OutputItem) is "Depressant" or "Stimulant");
        }

        // Some items are always added, no matter the mode.
        var alwaysAdded = recipes
            .Where(r => r.Pool == RecipePool.AlwaysEnabled)
            .Select(r => CreateRecipe(r, rng))
            .ToList();

        var addedRecipes = new List<Recipe>();
        var minRecipeAmount = randomizer.GetConfigOption<int>("recipes-new-min");
        var maxRecipeAmount = randomizer.GetConfigOption<int>("recipes-new-max");
        var amount = rng.Next(minRecipeAmount, maxRecipeAmount + 1);

        var toBeAdded = recipePool
            .OrderBy(_ => rng.Next())
            .Take(amount)
            .Select(r => CreateRecipe(r, rng))
            .ToList();

        if (randomizer.GetConfigOption<bool>("recipes-random-item-quantities")) {
            var minQuantity = randomizer.GetConfigOption<double>("recipes-count-min");
            var maxQuantity = randomizer.GetConfigOption<double>("recipes-count-max");

            double Scale() => Math.Round(rng.NextDouble(minQuantity, maxQuantity), 1);

            foreach (var r in toBeAdded) {
                var src1 = r.SrcItemNum1;
                var src2 = r.SrcItemNum2;
                var result = r.ResultItemNum;

                r.SrcItemNum1 = Math.Max(1, (int)Math.Round(src1 * Scale()));
                r.SrcItemNum2 = Math.Max(1, (int)Math.Round(src2 * Scale()));
                r.ResultItemNum = ClampRecipeResultNum(r.ResultItemID, (int)Math.Round(result * Scale()));
            }
        }

        addedRecipes.AddRange(toBeAdded);
        AddRecipes(randomizer, addedRecipes.Concat(alwaysAdded).ToList(), clear: false);

        // Rebuild dictionarycombinedata.user.2
        // This file holds the result item IDs of the items that are displayed in the combine GUI.
        var newDict = RebuildDictionary(randomizer, alwaysAdded.Concat(addedRecipes).ToList());

        // Finally, write the spoiler log.
        LogRecipeState(logger, addedRecipes, newDict, beforeModifications: false);
    }

    private void ApplyDebugRecipes(DebugRecipeTable table, RandomizerLogger logger) {
        var files = _randomizer.FileRepository;
        var recipes = files.DeserializeUserFile<ItemCombineData>(ItemCombineDataPath);
        var dictionary = files.DeserializeUserFile<DictionaryCombineData>(DictionaryCombineDataPath);
        table.ValidateItems(recipes._Datas);

        recipes._Datas = table.Entries.Select(x => x.Recipe).ToList();
        dictionary._Datas = table.VisibleResultIds.Select(id => new DictionaryCombineData.Data { ItemDataID = id }).ToList();
        files.ModifyUserFile<ItemCombineData>(ItemCombineDataPath, _ => recipes);
        files.ModifyUserFile<DictionaryCombineData>(DictionaryCombineDataPath, _ => dictionary);
        _randomizer.AddLogFile("debug-recipes.csv", table.SourceCsv);
        logger.LogLine($"Debug Recipes: exact table mode ({recipes._Datas.Count} recipes, {dictionary._Datas.Count} visible result items). Normal recipe options are bypassed.");
        LogRecipeState(logger, recipes._Datas, dictionary._Datas, beforeModifications: false);
    }

    private static List<DictionaryCombineData.Data> RebuildDictionary(Randomizer randomizer, List<Recipe> newRecipes) {
        var result = new List<DictionaryCombineData.Data>();
        randomizer.FileRepository.ModifyUserFile<DictionaryCombineData>(
            DictionaryCombineDataPath,
            root => {
                root._Datas.Clear();

                for (int i = 0; i < newRecipes.Count && i < MaxRecipeCount; i++) {
                    root._Datas.Add(new(){ ItemDataID = newRecipes[i].ResultItemID });
                }

                root._Datas = root._Datas
                    .DistinctBy(d => d.ItemDataID)
                    //.OrderBy(d => _itemDefinitions.FromId(d.ItemDataID)!.CategoryType)
                    .ToList();
                result = root._Datas;
                return root;
            });
        return result;
    }

    private static Recipe CreateRecipe(RecipeModel model, Rng rng) {
        var resultItemId = _itemDefinitions.NameToId(model.OutputItem);
        return new(){
            _Comment = model.Comment,
            DataID = model.OutputItem,
            SrcItemID1 = _itemDefinitions.NameToId(model.Item1),
            SrcItemNum1 = rng.NextInclusive(model.Count1_Min, model.Count1_Max),
            SrcItemID2 = _itemDefinitions.NameToId(model.Item2),
            SrcItemNum2 = rng.NextInclusive(model.Count2_Min, model.Count2_Max),
            ResultItemID = resultItemId,
            ResultItemNum = ClampRecipeResultNum(
                resultItemId,
                rng.NextInclusive(model.OutputCount_Min, model.OutputCount_Max)),
            EnableFlag = Guid.Empty,
            IsTutorialTarget = false,
            IsTrophyTarget = false,
        };
    }

    private static bool IsValidRecipe(RecipeModel recipe, RandomizerLogger logger) {
        var valid = recipe.Count1_Min > 0
                    && recipe.Count1_Max > 0
                    && recipe.Count1_Min <= recipe.Count1_Max
                    && recipe.Count2_Min > 0
                    && recipe.Count2_Max > 0
                    && recipe.Count2_Min <= recipe.Count2_Max
                    && recipe.OutputCount_Min > 0
                    && recipe.OutputCount_Max > 0
                    && recipe.OutputCount_Min <= recipe.OutputCount_Max
                    && _itemDefinitions.NameToItemMap.ContainsKey(recipe.Item1)
                    && _itemDefinitions.NameToItemMap.ContainsKey(recipe.Item2)
                    && _itemDefinitions.NameToItemMap.ContainsKey(recipe.OutputItem);

        if (!valid) {
            logger.LogLine("Skipping bad CSV recipe! Please report this. Recipe: " + recipe);
        }

        return valid;
    }

    private static int ClampRecipeResultNum(string? itemId, int count) {
        var result = Math.Max(1, count);
        return itemId != null && _itemDefinitions.FromId(itemId) is { MaxStack: > 0 } definition
            ? Math.Min(result, definition.MaxStack)
            : result;
    }

    private static void AddRecipes(Randomizer randomizer, List<Recipe> recipes, bool clear) {
        randomizer.FileRepository.ModifyUserFile<ItemCombineData>(ItemCombineDataPath, root => {
            if (clear) {
                root._Datas.Clear();
            }

            // Instead of appending we are prepending new recipes.
            // Our new recipes are then prioritized in the combine GUI when there are multiple possible ingredients for the same target item.
            root._Datas.PrependValues(recipes);
            return root;
        });
    }

    internal enum RecipePool {
        AlwaysEnabled,
        Easy,
        Balanced,
        Chaos,
        Crazy
    }

    internal sealed class RecipeModel {
        public bool Enabled { get; init; }
        public RecipePool Pool { get; init; }
        public int Count1_Min { get; init; }
        public int Count1_Max { get; init; }
        public string Item1 { get; init; } = "";
        public int Count2_Min { get; init; }
        public int Count2_Max { get; init; }
        public string Item2 { get; init; } = "";
        public int OutputCount_Min { get; init; }
        public int OutputCount_Max { get; init; }
        public string OutputItem { get; init; } = "";
        public string Comment { get; init; } = "";

        public override string ToString()
            =>
                $"[{Count1_Min}-{Count1_Max}]x {Item1} + [{Count2_Min}-{Count2_Max}]x {Item2} -> [{OutputCount_Min}-{OutputCount_Max}]x {OutputItem} ({Pool})";
    }
}
