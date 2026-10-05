using System.Text;
using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using Biohazard.BioRand.RE7.Modifiers;
using IntelOrca.Biohazard.REE.Rsz;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerStartingInventoryAdditionalBehaviorTests {
    [Theory]
    [InlineData("Main House")]
    [InlineData("Normal")]
    public void MainHouseStart_InitializesConfiguredLoadoutOnce(string start) {
        using var result = RandomizerTest.RunState(config => {
            config["start-chapter"] = start;
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-give-ammo"] = true;
            config["random-starting-inventory-skills-ethan"] = true;
            config["random-starting-inventory-additional-gun-ethan"] = true;
            DisableStartingWeapons(config, "ethan");
            config["inventory-weapon-handgun-ethan"] = true;
        });
        var inventory = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        Assert.True(inventory.Count(item => item.ItemDataID.StartsWith("Handgun_")) >= 2);
        Assert.Contains(inventory, item => item.ItemDataID == "HandgunBullet");
        Assert.Contains(inventory, item => item.ItemDataID == "RemedyM");
        Assert.Contains(inventory, item => item.ItemDataID == "RemedyL");
        Assert.Contains(inventory, item => item.ItemDataID.StartsWith("skl"));

        var before = result.ReadBeforeScene(StartingInventoryModifier.MainHouseStartScene)
            .FindGameObject(go => go.Name == StartingInventoryModifier.MainHouseStartObject)!;
        var after = result.ReadAfterScene(StartingInventoryModifier.MainHouseStartScene)
            .FindGameObject(go => go.Name == StartingInventoryModifier.MainHouseStartObject)!;
        var beforeActions = new List<RszObjectNode>();
        var afterActions = new List<RszObjectNode>();
        before.Visit(node => { if (node is RszObjectNode obj) beforeActions.Add(obj); });
        after.Visit(node => { if (node is RszObjectNode obj) afterActions.Add(obj); });
        var transfer = Assert.Single(beforeActions, obj => obj.Type.Name == "app.fsm.CopySaveItem2ItemBox");
        Assert.Contains(afterActions, obj => obj.Type.Name == "app.ForceFsmSave");
        if (start == "Main House") {
            Assert.DoesNotContain(afterActions, obj => obj.Type.Name == "app.fsm.CopySaveItem2ItemBox");
            var add = Assert.Single(afterActions, obj => obj.Type.Name == "app.AddItem");
            Assert.Equal(transfer.Get<uint>("v2_UID"), add.Get<uint>("v2_UID"));
            Assert.Contains("Ch1_StartInventory.user", add["Inventory"].ToString());
        } else {
            Assert.DoesNotContain(afterActions, obj => obj.Type.Name == "app.AddItem");
            Assert.Single(afterActions, obj => obj.Type.Name == "app.fsm.CopySaveItem2ItemBox");
        }
    }

    private static readonly string[] InventoryPaths =[
        RandomizerTestPaths.EthanInventoryPath,
        RandomizerTestPaths.MiaInventoryPath,
        RandomizerTestPaths.ClancyInventoryPath,
        RandomizerTestPaths.MiaVhsInventoryPath,
    ];

    public static IEnumerable<object[]> StarterAmmoLoadouts() {
        yield return [
            StartingWeaponCategory.Handgun,
            new (string ItemId, int Count)[]{ ("HandgunBullet", 20), ("HandgunBulletL", 20) }
        ];
        yield return [StartingWeaponCategory.Shotgun, new (string ItemId, int Count)[]{ ("ShotgunBullet", 15) }];
        yield return [StartingWeaponCategory.MachineGun, new (string ItemId, int Count)[]{ ("MachineGunBullet", 150) }];
        yield return [StartingWeaponCategory.Burner, new (string ItemId, int Count)[]{ ("BurnerBullet", 150) }];
        yield return [
            StartingWeaponCategory.GrenadeLauncher,
            new (string ItemId, int Count)[]{ ("FlameBulletS", 3), ("AcidBulletS", 3) }
        ];
        yield return [StartingWeaponCategory.Magnum, new (string ItemId, int Count)[]{ ("MagnumBullet", 10) }];
    }

    [Fact]
    public void StartingInventory_VhsEnabled_RandomizesVhsInventories() {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-mia"] = false;
            config["random-starting-inventory-vhs"] = true;

            foreach (var category in Enum.GetValues<StartingWeaponCategory>()) {
                config[$"inventory-weapon-{category.ToString().ToLowerInvariant()}-ethan"] = false;
            }

            config["inventory-weapon-handgun-ethan"] = true;
            config["random-starting-inventory-give-ammo"] = false;
        });

        var beforeClancy = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.ClancyInventoryPath)
            ._AddItems;
        var afterClancy = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.ClancyInventoryPath)
            ._AddItems;
        var beforeMiaVhs = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.MiaVhsInventoryPath)
            ._AddItems;
        var afterMiaVhs = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.MiaVhsInventoryPath)
            ._AddItems;

        Assert.True(result.WasFileModified(RandomizerTestPaths.ClancyInventoryPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.MiaVhsInventoryPath));
        Assert.True(afterClancy.Count > beforeClancy.Count);
        Assert.Equal(beforeMiaVhs.Count, afterMiaVhs.Count);
    }

    [Theory]
    [MemberData(nameof(StarterAmmoLoadouts))]
    public void StartingInventory_GiveAmmo_AddsPresetAmmoForPrimaryWeapon(
        StartingWeaponCategory category,
        (string ItemId, int Count)[] expectedAmmo
    ) {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-mia"] = false;
            config["random-starting-inventory-vhs"] = false;
            config["random-starting-inventory-give-ammo"] = true;

            DisableStartingWeapons(config, "ethan");
            config[$"inventory-weapon-{category.ToString().ToLowerInvariant()}-ethan"] = true;
        });

        var before = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)
            ._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)
            ._AddItems;
        var newItems = after.Skip(before.Count).ToArray();
        var categoryItemIds = category.GetItemIds()
            .Select(itemId => itemId.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(newItems, item => categoryItemIds.Contains(item.ItemDataID));
        foreach (var (itemId, count) in expectedAmmo) {
            Assert.Contains(newItems, item => item.ItemDataID == itemId && item.Num == count);
        }
    }

    [Fact]
    public void StartingInventory_Enabled_EnsuresFirstAidAndStrongFirstAid() {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-mia"] = false;
            config["random-starting-inventory-vhs"] = false;
            config["random-starting-inventory-give-ammo"] = false;

            DisableStartingWeapons(config, "ethan");
        });

        var after = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)
            ._AddItems;

        Assert.True(GetInventoryCount(after, "RemedyM") >= 1);
        Assert.True(GetInventoryCount(after, "RemedyL") >= 1);
    }

    [Theory]
    [InlineData("re7:debugger", true)]
    [InlineData("re7:debugstartitems", false)]
    [InlineData("", false)]
    public void StartingInventory_DebugItems_RequireUserTag(string tags, bool authorized) {
        var debugCsv = """
                       ItemId,Quantity
                       Coin,2
                       Herb,1
                       """;

        using var result = RandomizerTest.RunState(
            config => {
                config["username"] = "captainezekiel";
                config["tags"] = tags;
                config["random-starting-inventory-ethan"] = true;
                config["inventory-weapon-handgun-ethan"] = false;
                config["random-starting-inventory-give-ammo"] = false;
            },
            prepareRandomizer: randomizer => {
                randomizer.DynamicData.SetData(DynamicDataName.DebugStartItems, Encoding.UTF8.GetBytes(debugCsv));
            });

        var ethanInventory = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)
            ._AddItems;

        Assert.Equal(authorized, ethanInventory.Any(x => x.ItemDataID == "Herb" && x.Num == 1));
    }

    [Fact]
    public void StartingInventory_EthanRandomSkillsEnabled_AddsBirthdaySkillToEthan() {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = false;
            config["random-starting-inventory-mia"] = false;
            config["random-starting-inventory-vhs"] = false;
            config["random-starting-inventory-skills-ethan"] = true;
        });

        Assert.True(result.WasFileModified(RandomizerTestPaths.EthanInventoryPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.MiaInventoryPath));
        AssertRandomSkillItems(result, RandomizerTestPaths.EthanInventoryPath);
    }

    [Fact]
    public void StartingInventory_MiaRandomSkillsEnabled_AddsBirthdaySkillToMia() {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = false;
            config["random-starting-inventory-mia"] = false;
            config["random-starting-inventory-vhs"] = false;
            config["random-starting-inventory-skills-mia"] = true;
        });

        Assert.False(result.WasFileModified(RandomizerTestPaths.EthanInventoryPath));
        Assert.True(result.WasFileModified(RandomizerTestPaths.MiaInventoryPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.MiaVhsInventoryPath));
        AssertRandomSkillItems(result, RandomizerTestPaths.MiaInventoryPath);
    }

    [Theory]
    [InlineData("ethan", false)]
    [InlineData("ethan", true)]
    [InlineData("mia", false)]
    [InlineData("mia", true)]
    public void StartingInventory_AdditionalGun_AddsAllowedGunOnlyToSelectedCharacter(
        string character,
        bool randomizeWeapons
    ) {
        using var result = RandomizerTest.RunState(config => {
            config[$"random-starting-inventory-{character}"] = randomizeWeapons;
            config[$"random-starting-inventory-additional-gun-{character}"] = true;
            config["random-starting-inventory-give-ammo"] = false;
            DisableStartingWeapons(config, character);
            config[$"inventory-weapon-handgun-{character}"] = true;
            config[$"inventory-weapon-bladed-{character}"] = true;
        });

        var inventoryPath = character == "ethan"
            ? RandomizerTestPaths.EthanInventoryPath
            : RandomizerTestPaths.MiaInventoryPath;
        var before = result.ReadBeforeUserFile<app.AddItemListData>(inventoryPath)._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(inventoryPath)._AddItems;
        var newItems = after.Skip(before.Count).ToArray();
        var handguns = StartingWeaponCategory.Handgun.GetItemIds().Select(id => id.ToString()).ToHashSet();
        var guns = newItems.Where(item => handguns.Contains(item.ItemDataID)).ToArray();
        var blades = StartingWeaponCategory.Bladed.GetItemIds().Select(id => id.ToString()).ToHashSet();

        Assert.Equal(randomizeWeapons ? 2 : 1, guns.Length);
        Assert.Equal(guns.Length, guns.Select(item => item.ItemDataID).Distinct().Count());
        Assert.All(guns, gun => Assert.Equal(1, gun.Num));
        Assert.Equal(randomizeWeapons ? 1 : 0, newItems.Count(item => blades.Contains(item.ItemDataID)));
        Assert.DoesNotContain(newItems, item => item.ItemDataID is "HandgunBullet" or "HandgunBulletL");
        Assert.Equal(before.Select(item => (item.ItemDataID, item.Num)),
            after.Take(before.Count).Select(item => (item.ItemDataID, item.Num)));
        foreach (var otherPath in InventoryPaths.Where(path => path != inventoryPath)) {
            Assert.False(result.WasFileModified(otherPath));
        }
    }

    [Theory]
    [MemberData(nameof(StarterAmmoLoadouts))]
    public void StartingInventory_AdditionalGun_ProvidesAmmoForBothGuns(
        StartingWeaponCategory category,
        (string ItemId, int Count)[] expectedAmmo
    ) {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-additional-gun-ethan"] = true;
            config["random-starting-inventory-give-ammo"] = true;
            DisableStartingWeapons(config, "ethan");
            config[$"inventory-weapon-{category.ToString().ToLowerInvariant()}-ethan"] = true;
        });

        var before = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        var newItems = after.Skip(before.Count).ToArray();
        var allowedGuns = category.GetItemIds().Select(id => id.ToString()).ToHashSet();

        Assert.Equal(2, newItems.Count(item => allowedGuns.Contains(item.ItemDataID)));
        foreach (var (itemId, count) in expectedAmmo) {
            Assert.Equal(2 * count, GetInventoryCount(newItems, itemId));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartingInventory_AdditionalGun_WithoutAllowedGuns_DoesNotAddOtherWeapons(bool allowNonGuns) {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-additional-gun-ethan"] = true;
            DisableStartingWeapons(config, "ethan");
            config["inventory-weapon-bladed-ethan"] = allowNonGuns;
            config["inventory-weapon-circularsaw-ethan"] = allowNonGuns;
            config["inventory-weapon-bomb-ethan"] = allowNonGuns;
        });

        var before = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;

        Assert.Equal(before.Select(item => (item.ItemDataID, item.Num)),
            after.Select(item => (item.ItemDataID, item.Num)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartingInventory_MiaAdditionalGun_RespectsVhsToggle(bool randomizeVhs) {
        using var result = RandomizerTest.RunState(config => {
            config["random-starting-inventory-additional-gun-mia"] = true;
            config["random-starting-inventory-vhs"] = randomizeVhs;
            config["random-starting-inventory-give-ammo"] = false;
            DisableStartingWeapons(config, "mia");
            config["inventory-weapon-handgun-mia"] = true;
        });

        var before = result.ReadBeforeUserFile<app.AddItemListData>(RandomizerTestPaths.MiaVhsInventoryPath)._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.MiaVhsInventoryPath)._AddItems;
        var newItems = after.Skip(before.Count).ToArray();

        Assert.Equal(randomizeVhs, result.WasFileModified(RandomizerTestPaths.MiaVhsInventoryPath));
        if (randomizeVhs) {
            Assert.Contains(Assert.Single(newItems).ItemDataID,
                StartingWeaponCategory.Handgun.GetItemIds().Select(id => id.ToString()));
        } else {
            Assert.Empty(newItems);
        }
        Assert.False(result.WasFileModified(RandomizerTestPaths.EthanInventoryPath));
        Assert.False(result.WasFileModified(RandomizerTestPaths.ClancyInventoryPath));
    }

    [Fact]
    public void StartingInventory_AdditionalGuns_AreDeterministicAndPreserveOtherStartingItems() {
        static void Configure(RandomizerConfiguration config, bool additionalGuns) {
            config["random-starting-inventory-ethan"] = true;
            config["random-starting-inventory-mia"] = true;
            config["random-starting-inventory-vhs"] = true;
            config["random-starting-inventory-additional-gun-ethan"] = additionalGuns;
            config["random-starting-inventory-additional-gun-mia"] = additionalGuns;
        }

        using var withoutGuns = RandomizerTest.RunState(config => Configure(config, false));
        using var withGuns = RandomizerTest.RunState(config => Configure(config, true));
        using var repeated = RandomizerTest.RunState(config => Configure(config, true));

        foreach (var path in InventoryPaths) {
            var original = withoutGuns.ReadAfterUserFile<app.AddItemListData>(path)._AddItems;
            var extra = withGuns.ReadAfterUserFile<app.AddItemListData>(path)._AddItems;

            Assert.Equal(original.Select(item => (item.ItemDataID, item.Num)),
                extra.Take(original.Count).Select(item => (item.ItemDataID, item.Num)));
            Assert.Equal(withGuns.ReadAfterBytes(path), repeated.ReadAfterBytes(path));
            if (path == RandomizerTestPaths.ClancyInventoryPath) {
                Assert.Equal(original.Count, extra.Count);
            } else {
                Assert.True(extra.Count > original.Count);
            }
        }
    }

    private static void AssertRandomSkillItems(RandomizerRunResult result, string inventoryPath) {
        var before = result.ReadBeforeUserFile<app.AddItemListData>(inventoryPath)._AddItems;
        var after = result.ReadAfterUserFile<app.AddItemListData>(inventoryPath)._AddItems;
        var newItems = after.Skip(before.Count).ToArray();
        var skillItems = newItems
            .Where(item => ItemDrops.IsBirthdaySkill(item.ItemDataID))
            .ToArray();

        Assert.InRange(skillItems.Length, 1, 2);
        Assert.Equal(skillItems.Length,
            skillItems.Select(item => item.ItemDataID).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(skillItems, item => Assert.Equal(1, item.Num));
        Assert.All(skillItems,
            item => Assert.False(item.ItemDataID.EndsWith("no", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(skillItems.Length, newItems.Length);
    }

    private static void DisableStartingWeapons(RandomizerConfiguration config, string character) {
        foreach (var category in Enum.GetValues<StartingWeaponCategory>()) {
            config[$"inventory-weapon-{category.ToString().ToLowerInvariant()}-{character}"] = false;
        }
    }

    private static int GetInventoryCount(IEnumerable<app.AddItemListData.Data> items, string itemId)
        => items
            .Where(item => string.Equals(item.ItemDataID, itemId, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.Num);
}
