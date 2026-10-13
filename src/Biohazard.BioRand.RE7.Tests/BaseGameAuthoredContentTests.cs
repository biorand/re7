using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Rsz;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class BaseGameAuthoredContentTests {
    [Fact]
    public void BaseGameOnly_FiltersAuthoredItemsScenesAndRecipeIngredients() {
        const string scene = "natives/stm/environment/scene/chapter3/c03_leftarea1fpuzzleroom1.scn.20";
        const string missingDlcScene = "natives/stm/ch08/base_game_test_missing.scn.20";
        var extras = "IsExtra,Tags,Id,Enabled,StackNum,EasyNum,HardNum,Chapter,PosX,PosY,PosZ,RotW,SceneFile\n" +
            $"TRUE,,skl002,TRUE,1,1,1,3,50,1,20,1,{scene}\n" +
            $"TRUE,,Herb,TRUE,1,1,1,3,52,1,20,1,{scene}\n" +
            $"TRUE,item_box,,TRUE,1,1,1,8,50,1,20,1,{missingDlcScene}\n";
        const string recipes = """
            Enabled,Pool,Count1_Min,Count1_Max,Item1,Count2_Min,Count2_Max,Item2,OutputCount_Min,OutputCount_Max,OutputItem,Comment
            TRUE,AlwaysEnabled,1,1,Herb,1,1,Herb,1,1,Strong Chem Fluid,Base recipe
            TRUE,AlwaysEnabled,1,1,Herb,1,1,Herb,1,1,Tactical Knife,DLC result
            TRUE,AlwaysEnabled,1,1,Tactical Knife,1,1,Herb,1,1,Strong Chem Fluid,DLC first ingredient
            TRUE,AlwaysEnabled,1,1,Herb,1,1,Tactical Knife,1,1,Strong Chem Fluid,DLC second ingredient
            """;
        using var result = RandomizerTest.RunState(config => {
            config[DlcContent.BaseGameOnlyConfigKey] = true;
            config["additional-items"] = true;
            config["recipes-add-new"] = true;
            config["recipes-randomization-mode"] = "Balanced";
        }, prepareRandomizer: randomizer => {
            randomizer.DynamicData.SetData(DynamicDataName.ItemPlacements, Encoding.UTF8.GetBytes(extras));
            randomizer.DynamicData.SetData(DynamicDataName.Recipes, Encoding.UTF8.GetBytes(recipes));
        });
        var existing = result.ReadBeforeScene(scene).GetGameObjects().Select(go => go.Guid).ToHashSet();
        var added = result.ReadAfterScene(scene).GetGameObjects().Where(go => !existing.Contains(go.Guid))
            .Select(go => go.FindComponent<app.Item>()).Where(item => item != null).ToArray();
        Assert.Contains(added, item => item!.ItemDataID == "Herb");
        Assert.DoesNotContain(added, item => DlcContent.IsDlcItem(item!.ItemDataID));
        Assert.DoesNotContain(missingDlcScene, result.ChangedFiles.Keys);
        var generated = result.ReadAfterUserFile<app.ItemCombineData>(RandomizerTestPaths.ItemCombineDataPath)._Datas;
        Assert.Contains(generated, recipe => recipe._Comment == "Base recipe");
        Assert.DoesNotContain(generated, recipe => (recipe._Comment ?? "").StartsWith("DLC", StringComparison.Ordinal));
    }
}
