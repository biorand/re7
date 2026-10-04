using Biohazard.BioRand.RE7.Serialization;
using System.Text;
using System.Text.Json;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerDebugRecipeBehaviorTests {
    [Theory]
    [InlineData(false, "Balanced", 1234)]
    [InlineData(true, "No crafting", 9876)]
    public void ExactMode_ReplacesBaseRecipesAndIgnoresNormalRecipeOptions(bool addNew, string mode, int seed) {
        var flag = "eab670f7-4475-4258-b900-cbe91664c9a3";
        var csv = DebugRecipeTableTests.Csv(
            $"true,true,RemedyM,Herb,2,ChemicalS,3,RemedyM,2,2,{flag},true,true,Edited vanilla\n" +
            "true,true,debug_ammo,Herb,3,Herb,4,HandgunBullet,7,1,,false,false,New\n" +
            "true,false,debug_ammo_alt,Herb,1,Gunpowder,1,HandgunBullet,5,,,false,false,Hidden variant\n" +
            "false,true,Burner,BurnerPartsA,1,BurnerPartsB,1,Burner,1,,,false,false,Disabled original\n" +
            "true,false,debug_hidden,Herb,1,Herb,2,ChemicalS,1,,,false,false,Hidden output");
        using var result = RandomizerTest.RunState(config => {
            config["tags"] = "re7:debugger";
            config["recipes-add-new"] = addNew;
            config["recipes-randomization-mode"] = mode;
            config["recipes-random-item-quantities"] = true;
            config["recipes-count-min"] = 9.0;
            config["recipes-count-max"] = 9.0;
            config["recipes-unlock-from-start"] = true;
        }, seed: seed, prepareRandomizer: r => r.DynamicData.SetData(DynamicDataName.DebugRecipes, csv));

        var table = DebugRecipeTable.Parse(csv);
        var recipes = result.ReadAfterUserFile<app.ItemCombineData>(RandomizerTestPaths.ItemCombineDataPath)._Datas;
        var dictionary = result.ReadAfterUserFile<app.DictionaryCombineData>(RandomizerTestPaths.DictionaryCombineDataPath)._Datas;
        Assert.Equal(JsonSerializer.Serialize(table.Entries.Select(e => e.Recipe)), JsonSerializer.Serialize(recipes));
        Assert.Equal(["HandgunBullet", "RemedyM"], dictionary.Select(d => d.ItemDataID));
        Assert.DoesNotContain(recipes, r => r.DataID == "Burner");
        Assert.Contains("exact table mode", result.ProcessLog);
        Assert.True(result.Randomizer.IsREFrameworkRequired());
        var beforeFlag = RandomizerTestHelpers.ReadGlobalVariableGroups(result, before: true)
            .SelectMany(g => g.Variables).Single(v => v.Guid == Guid.Parse(flag));
        var afterFlag = RandomizerTestHelpers.ReadGlobalVariableGroups(result, before: false)
            .SelectMany(g => g.Variables).Single(v => v.Guid == Guid.Parse(flag));
        Assert.Equal(beforeFlag.BooleanValue, afterFlag.BooleanValue);
    }

    [Fact]
    public void EmbeddedTemplate_IsOffAndReproducesAllOriginalRecipesAndDictionary() {
        var bytes = EmbeddedData.GetFile("debug_recipes.csv");
        Assert.False(DebugRecipeTable.Parse(bytes).Enabled);
        var enabled = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"DebugModeEnabled\",FALSE", "\"DebugModeEnabled\",TRUE"));
        using var result = RandomizerTest.RunState(config => {
            config["tags"] = "re7:debugger";
            config["recipes-unlock-from-start"] = false;
        }, prepareRandomizer: r => r.DynamicData.SetData(DynamicDataName.DebugRecipes, enabled));

        var before = result.ReadBeforeUserFile<app.ItemCombineData>(RandomizerTestPaths.ItemCombineDataPath);
        var after = result.ReadAfterUserFile<app.ItemCombineData>(RandomizerTestPaths.ItemCombineDataPath);
        Assert.Equal(23, after._Datas.Count);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));
        Assert.Equal(JsonSerializer.Serialize(result.ReadBeforeUserFile<app.DictionaryCombineData>(RandomizerTestPaths.DictionaryCombineDataPath)),
            JsonSerializer.Serialize(result.ReadAfterUserFile<app.DictionaryCombineData>(RandomizerTestPaths.DictionaryCombineDataPath)));
    }
}
