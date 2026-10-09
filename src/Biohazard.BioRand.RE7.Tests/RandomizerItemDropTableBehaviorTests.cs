using Biohazard.BioRand.RE7.Items;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerItemDropTableBehaviorTests {
    [Theory]
    [InlineData("Flower", 0.05)]
    [InlineData("Flower", 0.0)]
    [InlineData("AlloyClay", 0.05)]
    [InlineData("AlloyClay", 0.0)]
    [InlineData("Magnesium", 0.05)]
    [InlineData("Magnesium", 0.0)]
    [InlineData("SyntheticDetergent", 0.05)]
    [InlineData("SyntheticDetergent", 0.0)]
    public void ItemDropTable_CraftingMaterials_RespectConfiguredWeight(string itemId, double ratio) {
        using var result = RandomizerTest.RunState(config => {
            config[$"item-drop-ratio-{itemId.ToLowerInvariant()}"] = ratio;
        });
        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);

        if (ratio == 0) {
            Assert.DoesNotContain(table.DataList, drop => drop.ItemID == itemId);
            return;
        }

        var drop = Assert.Single(table.DataList, drop => drop.ItemID == itemId);
        Assert.Equal((5u, 5u, 5u), (drop.EasyDropRate, drop.NormalDropRate, drop.HardDropRate));
        Assert.Equal((1u, 1u, 1u), (drop.ReliefNum, drop.NormalDropNum, drop.ReliefDropNum));
    }

    [Fact]
    public void ItemDropTable_AvailableWeaponsOnly_FiltersUnavailableAmmoAndAddsLockPick() {
        using var result = RandomizerTest.RunState(config => {
            RandomizerTestHelpers.ConfigureSingleDropRate(config, "HandgunBullet", 0.5);
            RandomizerTestHelpers.ConfigureSingleDropRate(config, "MachineGunBullet", 0.25);
            config["item-drop-ammo-only-available-weapons"] = true;
            config["item-drop-valuable-lock-pick"] = true;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-weapon"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
        });

        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);

        Assert.True(result.WasFileModified(RandomizerTestPaths.Chapter4DropTablePath));
        Assert.DoesNotContain(table.DataList, x => x.ItemID == "HandgunBullet");
        Assert.Contains(table.DataList, x => x.ItemID == "MachineGunBullet" && x.NormalDropRate == 25);
        Assert.Contains(table.DataList, x => x.ItemID == "CylinderKey"
                                             && x.NormalDropRate == ItemDrops.GetValuableDropRate(ItemDrops.LockPick)
                                             && x.NormalDropNum == ItemDrops.GetValuableDropCount(ItemDrops.LockPick));
    }

    [Fact]
    public void ItemDropTable_WeaponValuableDrop_AddsRandomAllowedWeapon() {
        using var result = RandomizerTest.RunState(config => {
            config["item-drop-valuable-weapon"] = true;
            config["item-drop-valuable-birthday-skill"] = false;
            config["item-drop-valuable-lock-pick"] = false;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
        });

        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);
        var weaponDrop = Assert.Single(table.DataList, x =>
            ItemDefinitionRepository.Default.FromId(x.ItemID)?.IsWeapon == true &&
            x.NormalDropRate == ItemDrops.GetValuableDropRate(ItemDrops.Weapon));

        Assert.True(result.WasFileModified(RandomizerTestPaths.Chapter4DropTablePath));
        Assert.NotEqual("NoName", weaponDrop.ItemID);
        Assert.False(new[]{ "BlueBlaster", "HyperBlaster", "RedBlaster" }.Contains(weaponDrop.ItemID));
        Assert.True(result.ItemRandomizer.IsItemAllowed(ItemDefinitionRepository.Default.FromId(weaponDrop.ItemID)!));
        Assert.Equal((uint)ItemDrops.GetValuableDropCount(ItemDrops.Weapon), weaponDrop.NormalDropNum);
    }

    [Fact]
    public void ItemDropTable_AmmoAmounts_MapToDifficultyFields() {
        using var result = RandomizerTest.RunState(config => {
            RandomizerTestHelpers.ConfigureSingleDropRate(config, "ShotgunBullet", 1.0);
            config["item-drop-ammo-only-available-weapons"] = false;
            config["item-drop-respect-difficulty"] = true;
            config["item-drop-ammo-min"] = 0.5;
            config["item-drop-ammo-max"] = 0.5;
            config["item-drop-valuable-lock-pick"] = false;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-weapon"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
            config["item-drop-valuable-birthday-skill"] = false;
        });

        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);
        var shotgunDrop = Assert.Single(table.DataList, x => x.ItemID == "ShotgunBullet");
        var baseAmount = (uint)Math.Round(ItemDefinitionRepository.Default.FromId("ShotgunBullet")!.MaxStack * 0.5);

        Assert.Equal(result.ItemRandomizer.ApplyDifficultyToDropAmount(baseAmount), (
            shotgunDrop.ReliefNum,
            shotgunDrop.NormalDropNum,
            shotgunDrop.ReliefDropNum));
    }

    [Fact]
    public void ItemDropTable_BirthdaySkillValuableDrop_AddsRealSkillWhenDlcItemsAreAllowed() {
        using var result = RandomizerTest.RunState(config => {
            config["allow-dlc-items"] = true;
            config["item-drop-valuable-birthday-skill"] = true;
            config["item-drop-valuable-lock-pick"] = false;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-weapon"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
        });

        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);
        var skillDrop = Assert.Single(table.DataList, x => ItemDrops.IsBirthdaySkill(x.ItemID));

        Assert.True(result.WasFileModified(RandomizerTestPaths.Chapter4DropTablePath));
        Assert.False(skillDrop.ItemID.EndsWith("no", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(ItemDrops.GetValuableDropRate(ItemDrops.BirthdaySkill), skillDrop.NormalDropRate);
        Assert.Equal((uint)ItemDrops.GetValuableDropCount(ItemDrops.BirthdaySkill), skillDrop.NormalDropNum);
    }

    [Fact]
    public void ItemDropTable_UnsupportedRuntimePickupItems_AreExcluded() {
        using var result = RandomizerTest.RunState(config => {
            foreach (var drop in ItemDrops.GenericDrops) {
                config[$"item-drop-ratio-{drop.ToLowerInvariant()}"] = 0.0;
            }

            config["item-drop-ratio-stimulant"] = 1.0;
            config["item-drop-ratio-depressant"] = 1.0;
            config["item-drop-valuable-birthday-skill"] = false;
            config["item-drop-valuable-lock-pick"] = false;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-weapon"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
        });

        var table = result.ReadAfterUserFile<app.ReliefItemTable>(RandomizerTestPaths.Chapter4DropTablePath);

        Assert.DoesNotContain(table.DataList, x => x.ItemID == "Stimulant");
        Assert.DoesNotContain(table.DataList, x => x.ItemID == "Depressant");
    }
}
