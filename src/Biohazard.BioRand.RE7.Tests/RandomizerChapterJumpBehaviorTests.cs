using Enums.app.GameManager;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerChapterJumpBehaviorTests {
    [Fact]
    public void ChapterJumpData_StartChapterMainHouse_ChangesGuestHouseJumpToChapter3() {
        using var result = RandomizerTest.RunState(config => { config["start-chapter"] = "Main House"; });

        var before = RandomizerTestHelpers.GetChapterJump(
            result.ReadBeforeScene(RandomizerTestPaths.ChapterJumpScenePath), RandomizerTestPaths.GuestHouseJumpGuid);
        var after = RandomizerTestHelpers.GetChapterJump(
            result.ReadAfterScene(RandomizerTestPaths.ChapterJumpScenePath), RandomizerTestPaths.GuestHouseJumpGuid);

        Assert.True(result.WasFileModified(RandomizerTestPaths.ChapterJumpScenePath));
        Assert.Equal(ChapterNo.Chapter1, before.JumpChapter);
        Assert.Equal(ChapterNo.Chapter3, after.JumpChapter);
    }

    [Fact]
    public void ChapterJumpData_StartChapterWreckedShip_ChangesGuestHouseJumpToChapter4() {
        using var result = RandomizerTest.RunState(config => { config["start-chapter"] = "Wrecked Ship"; });

        var before = RandomizerTestHelpers.GetChapterJump(
            result.ReadBeforeScene(RandomizerTestPaths.ChapterJumpScenePath), RandomizerTestPaths.GuestHouseJumpGuid);
        var after = RandomizerTestHelpers.GetChapterJump(
            result.ReadAfterScene(RandomizerTestPaths.ChapterJumpScenePath), RandomizerTestPaths.GuestHouseJumpGuid);

        Assert.True(result.WasFileModified(RandomizerTestPaths.ChapterJumpScenePath));
        Assert.Equal(ChapterNo.Chapter1, before.JumpChapter);
        Assert.Equal(ChapterNo.Chapter4, after.JumpChapter);
    }

    [Theory]
    [InlineData("Normal", false)]
    [InlineData("Normal", true)]
    [InlineData("Main House", false)]
    public void ChapterShuffle_RejectsUnsafeLegacyProfiles(string start, bool footage) {
        var error = Assert.Throws<IntelOrca.Biohazard.BioRand.RandomizerUserException>(() => {
            using var result = RandomizerTest.RunState(config => {
                config["start-chapter"] = start;
                config["shuffle-chapters"] = true;
                config["shuffle-chapters-with-ff"] = footage;
            });
        });
        Assert.Contains("VHS returns", error.Message);
    }
}