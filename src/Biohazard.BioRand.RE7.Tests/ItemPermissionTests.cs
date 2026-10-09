using Biohazard.BioRand.RE7.Items;
using IntelOrca.Biohazard.BioRand;

namespace Biohazard.BioRand.RE7.Tests;

public sealed class ItemPermissionTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DlcAndUnlockablePermissionsAreIndependent(bool allowDlc, bool allowBonus) {
        using var randomizer = new Randomizer(new RandomizerInput {
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(config => {
                config["allow-dlc-items"] = allowDlc;
                config["allow-bonus-items"] = allowBonus;
            }),
        }, "unused", new EmptyReporter());

        foreach (var isDlc in new[] { false, true }) {
            foreach (var isBonus in new[] { false, true }) {
                var item = new ItemDefinition {
                    Id = "PermissionTest",
                    Dlc = isDlc ? DlcType.NotAHero : null,
                    IsUnlockable = isBonus,
                };
                Assert.Equal((!isDlc || allowDlc) && (!isBonus || allowBonus),
                    randomizer.ItemRandomizer.IsItemAllowed(item));

                item.Tags = ItemDefinition.StoryProgressionTag;
                Assert.False(randomizer.ItemRandomizer.IsItemAllowed(item));
            }
        }
    }
}
