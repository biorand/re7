using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Graphics;
using IntelOrca.Biohazard.REE.Messages;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class PlayerGuideTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmailBecomesGuideRegardlessOfRandomizedDialogue(bool randomizeMessages) {
        using var result = RandomizerTest.RunState(config => config["randomized-messages"] = randomizeMessages);
        var items = result.ReadAfterMsgFile("natives/stm/message/ui_item_mes.msg.17");
        var files = result.ReadAfterMsgFile("natives/stm/message/ui_file_mes.msg.17");
        foreach (var language in items.ToBuilder().Languages) {
            Assert.Equal(PlayerGuideModifier.Title, items.GetString(items.FindMessage("Item_002")!.Guid, language));
            Assert.Equal(PlayerGuideModifier.Description, items.GetString(items.FindMessage("Item_002_Desc")!.Guid, language));
        }
        foreach (var language in files.ToBuilder().Languages) {
            Assert.Equal(PlayerGuideModifier.Title, files.GetString(files.FindMessage("File_Title_063")!.Guid, language));
            Assert.Equal(PlayerGuideModifier.Guide, files.GetString(files.FindMessage("File_Title_063_Desc")!.Guid, language));
        }
        // Keep native item IDs and file GUIDs: saves and the examine interaction use them.
        var before = result.ReadBeforeMsgFile("natives/stm/message/ui_file_mes.msg.17");
        Assert.Equal(before.FindMessage("File_Title_063")!.Guid, files.FindMessage("File_Title_063")!.Guid);
        Assert.Equal(before.FindMessage("File_Title_063_Desc")!.Guid, files.FindMessage("File_Title_063_Desc")!.Guid);
        Assert.Equal(before.GetString(before.FindMessage("File_Title_055_Desc")!.Guid, LanguageId.English),
            files.GetString(files.FindMessage("File_Title_055_Desc")!.Guid, LanguageId.English));
        var atlas = result.ReadAfterBytes(PlayerGuideModifier.IconPath);
        Assert.Equal(EmbeddedData.GetFile("player-guide-icon.tex.35"), atlas);
        var texture = new TextureFile(atlas!);
        Assert.Equal(512, texture.Width);
        Assert.Equal(256, texture.Height);
        Assert.Equal(99u, texture.FormatId);
    }

    [Fact]
    public void GuideUsesShortPagesAndIncludesRecoveryInformation() {
        var pages = PlayerGuideModifier.Guide.Split("<PAGE>");
        Assert.Equal(2, pages.Length);
        Assert.All(pages, page => Assert.True(page.Trim().Split("\r\n").Length <= 13));
        Assert.Contains("deputy's window cutscene", PlayerGuideModifier.Guide);
        Assert.Contains("morgue puzzle", PlayerGuideModifier.Guide);
        Assert.Contains("Mia/Lucas cutscene", PlayerGuideModifier.Guide);
        Assert.Contains("Lucas no longer takes your items", PlayerGuideModifier.Guide);
        Assert.Contains("Madhouse checkpoints", PlayerGuideModifier.Guide);
        Assert.Contains("does not require cassette tapes", PlayerGuideModifier.Guide);
        Assert.Contains("affected save", PlayerGuideModifier.Guide);
    }
}
