using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

public class DebugRecipeTableTests {
    internal const string Header = "Enabled,ShowInUI,DataID,SrcItemID1,SrcItemNum1,SrcItemID2,SrcItemNum2,ResultItemID,ResultItemNum,UIOrder,EnableFlag,IsTutorialTarget,IsTrophyTarget,Comment";
    internal const string ValidRow = "true,true,debug_test,Herb,2,ChemicalS,1,RemedyM,1,2,,true,false,Test";
    internal static byte[] Csv(string rows, bool enabled = true)
        => Encoding.UTF8.GetBytes($"DebugModeEnabled,{enabled}\r\nInstructions\r\nInstructions\r\n{Header}\r\n{rows}\r\n");

    [Theory]
    [InlineData("", true, false)]
    [InlineData("re7:debugstartitems", true, false)]
    [InlineData("re7:debugger", false, false)]
    [InlineData("admin,re7:debugger", true, true)]
    public void Mode_RequiresNewTagAndGlobalCheckbox(string tags, bool checkbox, bool expected) {
        using var randomizer = new Randomizer(new RandomizerInput {
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
                config["tags"] = tags;
                config["allow-dlc-items"] = false;
                config["debug-force-reframework"] = false;
            }),
        }, "unused", new EmptyReporter());
        randomizer.DynamicData.SetData(DynamicDataName.DebugRecipes, Csv(ValidRow, checkbox));

        Assert.Equal(expected, randomizer.DebugRecipes.Enabled);
        Assert.Equal(expected, randomizer.IsREFrameworkRequired());
    }

    [Fact]
    public void DisabledControl_DoesNotValidateDraftRows() {
        Assert.False(DebugRecipeTable.Parse("DebugModeEnabled,FALSE\nbroken draft"u8.ToArray()).Enabled);
        var table = DebugRecipeTable.Parse(Csv("false,true,draft,Invalid,NaN"));
        Assert.Empty(table.Entries);
        Assert.Empty(table.VisibleResultIds);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DebuggerDownloads_OnlySelectDebugSheetsUnlessFullDownloadIsRequested(bool all, bool debugger) {
        var data = new DynamicData(all, debugger);
        Assert.Equal(all || debugger, data.DownloadEnabled);
        foreach (var name in Enum.GetValues<DynamicDataName>()) {
            var debugSheet = name is DynamicDataName.DebugRecipes or DynamicDataName.DebugStartItems;
            Assert.Equal(all || (debugger && debugSheet), data.ShouldDownload(name));
        }
    }

    [Theory]
    [InlineData("SrcItemNum1", "0", "positive whole number")]
    [InlineData("SrcItemNum2", "1.5", "positive whole number")]
    [InlineData("ResultItemNum", "2147483648", "positive whole number")]
    [InlineData("UIOrder", "-1", "positive whole number")]
    [InlineData("Enabled", "yes", "TRUE or FALSE")]
    [InlineData("ShowInUI", "yes", "TRUE or FALSE")]
    [InlineData("EnableFlag", "invalid", "GUID")]
    [InlineData("DataID", "", "must not be empty")]
    public void MalformedRows_ReportTheirSheetRow(string column, string value, string reason) {
        var cells = ValidRow.Split(',');
        cells[Array.IndexOf(Header.Split(','), column)] = value;
        var error = Assert.Throws<InvalidDataException>(() => DebugRecipeTable.Parse(Csv(string.Join(',', cells))));
        Assert.Contains("row 5", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Fact]
    public void EnabledMode_RejectsMissingHeadersAndDuplicateDataIds() {
        Assert.Throws<InvalidDataException>(() => DebugRecipeTable.Parse("DebugModeEnabled,true"u8.ToArray()));
        var error = Assert.Throws<InvalidDataException>(() => DebugRecipeTable.Parse(Csv(ValidRow + "\n" + ValidRow)));
        Assert.Contains("row 6", error.Message);
        Assert.Contains("Duplicate enabled DataID", error.Message);
    }

    [Theory]
    [InlineData("SrcItemID1", "NotAnItem", "unknown item ID")]
    [InlineData("SrcItemID2", "", "unknown item ID")]
    [InlineData("ResultItemID", "NotAnItem", "unknown item ID")]
    [InlineData("ResultItemNum", "999", "safe catalog maximum")]
    public void InvalidItemData_IsRejectedBeforeWriting(string column, string value, string reason) {
        var cells = ValidRow.Split(',');
        cells[Array.IndexOf(Header.Split(','), column)] = value;
        var table = DebugRecipeTable.Parse(Csv(string.Join(',', cells)));
        var error = Assert.Throws<InvalidDataException>(() => table.ValidateItems([]));
        Assert.Contains("row 5", error.Message);
        Assert.Contains(reason, error.Message);
    }

    [Fact]
    public void Dictionary_UsesResultIdsAndExplicitOrderAndDeduplicatesVariants() {
        var table = DebugRecipeTable.Parse(Csv(ValidRow + "\n" +
            "true,true,variant,Herb,1,ChemicalS,2,RemedyM,2,1,,false,false,Alternate\n" +
            "true,false,hidden,Herb,1,Herb,1,ChemicalS,1,1,,false,false,Hidden\n" +
            "true,true,ammo,Herb,1,Herb,1,HandgunBullet,10,1,,false,false,Ammo"));
        table.ValidateItems([]);
        Assert.Equal(["RemedyM", "HandgunBullet"], table.VisibleResultIds);
        Assert.Equal(4, table.Entries.Count);
    }

    [Fact]
    public void UiLimit_CountsDistinctVisibleResultsInsteadOfRecipeRows() {
        var variants = string.Join('\n', Enumerable.Range(0, 25).Select(i => ValidRow.Replace("debug_test", $"variant_{i}")));
        Assert.Equal(25, DebugRecipeTable.Parse(Csv(variants)).Entries.Count);
        var distinct = string.Join('\n', Enumerable.Range(0, 21).Select(i =>
            ValidRow.Replace("debug_test", $"variant_{i}").Replace("RemedyM", $"output_{i}")));
        Assert.Contains("at most 20", Assert.Throws<InvalidDataException>(() => DebugRecipeTable.Parse(Csv(distinct))).Message);
    }

    [Fact]
    public void QuotedComments_PreserveCommasQuotesAndLineBreaks() {
        var row = ValidRow[..ValidRow.LastIndexOf(',')] + ",\"Line 1, \"\"quoted\"\"\nLine 2\"";
        var table = DebugRecipeTable.Parse(Csv(row));
        Assert.Equal("Line 1, \"quoted\"\nLine 2", Assert.Single(table.Entries).Recipe._Comment);
    }
}
