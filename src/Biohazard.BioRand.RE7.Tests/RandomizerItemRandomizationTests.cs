using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Services;
using Enums.app.Item;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;
using System.Numerics;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class RandomizerItemRandomizationTests {
    private const string ForcedDropId = "Herb";
    private const string BirdCageScenePath = "environment/scene/chapter3/c03_trailerhouse.scn";
    private const string MiaPastVhsItemScenePath = "natives/stm/leveldesign/itemset/ff050/bf/bf.scn.20";
    private const string MainHouseHallScenePath = "natives/stm/environment/scene/chapter3/c03_mainhousehall.scn.20";
    private static readonly HashSet<string> BlasterIds = ["BlueBlaster", "HyperBlaster", "RedBlaster"];
    private static readonly Guid MiaPastVhsChemicalGuid = new("e3b64592-382a-4446-8753-ab6bf1eefeb8");
    private static readonly Guid MainHouseHallDrawerCoinGuid = new("ccd5a2ee-49f5-485b-97a8-42cf8282da07");

    [Fact]
    public void ItemRandomizer_DefaultFilters_RejectStoryUnlockableAndDlcItems() {
        using var result = RandomizerTest.RunState();
        var itemRandomizer = result.ItemRandomizer;
        var items = ItemDefinitionRepository.Default.Items;

        Assert.All(items.Where(x => x.IsStoryProgressionItem),
            item => Assert.False(itemRandomizer.IsItemAllowed(item)));
        Assert.All(items.Where(x => x.IsUnlockable), item => Assert.False(itemRandomizer.IsItemAllowed(item)));
        //Assert.All(items.Where(x => x.IsDlcItem), item => Assert.False(itemRandomizer.IsItemAllowed(item)));
    }

    [Fact]
    public void ItemRandomizer_ConfigFlags_AllowUnlockableAndDlcItems() {
        using var result = RandomizerTest.RunState(config => {
            config["allow-bonus-items"] = true;
            config["allow-dlc-items"] = true;
        });

        var unlockable = ItemDefinitionRepository.Default.Items.First(x => x.IsUnlockable && !x.IsStoryProgressionItem);
        var dlcItem = ItemDefinitionRepository.Default.Items.First(x => x.IsDlcItem && !x.IsStoryProgressionItem);

        Assert.True(result.ItemRandomizer.IsItemAllowed(unlockable));
        Assert.True(result.ItemRandomizer.IsItemAllowed(dlcItem));
    }

    [Fact]
    public void ItemRandomizer_NoRecurrence_DoesNotRepeatDrawnItems() {
        using var result = RandomizerTest.RunState();
        var rng = result.Randomizer.GetRng("tests/no-recurrence");
        var itemRandomizer = result.ItemRandomizer;
        var poolSize = ItemDefinitionRepository.Default
            .GetAll(ItemCategoryType.Drug)
            .Count(itemRandomizer.IsItemAllowed);

        Assert.True(poolSize >= 3);

        var draws = Enumerable.Range(0, Math.Min(5, poolSize))
            .Select(_ => itemRandomizer.GetRandomItemDefinition(rng, ItemCategoryType.Drug, allowReoccurance: false))
            .ToArray();

        Assert.DoesNotContain(draws, item => item == null);
        Assert.Equal(draws.Length, draws.Select(x => x!.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void ItemRandomizer_AllowedGunDrops_HaveItemTemplates() {
        using var result = RandomizerTest.RunState();
        var rng = result.Randomizer.GetRng("tests/allowed-gun-templates");
        var guns = new List<ItemDefinition>();

        for (var gun = result.ItemRandomizer.GetRandomGun(rng, allowReoccurance: false);
             gun != null;
             gun = result.ItemRandomizer.GetRandomGun(rng, allowReoccurance: false)) {
            guns.Add(gun);
            result.Randomizer.TemplateService.GetItemTemplate(gun.Id);
        }

        Assert.NotEmpty(guns);
        Assert.Empty(guns.Select(gun => gun.Id).Intersect(BlasterIds));
    }

    [Fact]
    public void ItemRandomizer_AllowedGunDrops_ExcludeBlastersEvenWhenDlcItemsAreAllowed() {
        using var result = RandomizerTest.RunState(config => { config["allow-dlc-items"] = true; });
        var rng = result.Randomizer.GetRng("tests/allowed-gun-excludes-blasters");
        var guns = new List<ItemDefinition>();

        for (var gun = result.ItemRandomizer.GetRandomGun(rng, allowReoccurance: false);
             gun != null;
             gun = result.ItemRandomizer.GetRandomGun(rng, allowReoccurance: false)) {
            guns.Add(gun);
        }

        Assert.NotEmpty(guns);
        Assert.Empty(guns.Select(gun => gun.Id).Intersect(BlasterIds));
    }

    [Fact]
    public void ItemTemplates_KeyItemPickupInteractions_AreReadyForFreshPlacement() {
        using var result = RandomizerTest.RunState();
        var checkedTemplateIds = new List<string>();

        foreach (var item in ItemDefinitionRepository.Default.Items
                     .Where(IsKeyItem)
                     .OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase)) {
            if (!TryGetItemTemplate(result.Randomizer.TemplateService, item.Id, out var template)) {
                continue;
            }

            if (!HasPickupInteractions(template)) {
                continue;
            }

            checkedTemplateIds.Add(item.Id);
            AssertPickupInteractionsAreReadyForFreshPlacement(template, item.Id);
        }

        Assert.Contains("3CrestKeyA", checkedTemplateIds);
    }

    [Fact]
    public void ItemPlacementService_FromSceneGuid_FiltersDuplicateGuidsToCurrentScene() {
        using var result = RandomizerTest.RunState();

        var duplicatedGuid = new Guid("3e6a9272-9495-44b5-963e-299206d95e16");
        var chapter3Scene = "natives/stm/environment/scene/chapter3/c03_mainhouse1fliving.scn.20";
        var chapter4Scene = "natives/stm/environment/scene/chapter4/c04_mainhouse1flivingjack.scn.20";

        var allPlacements = result.ItemPlacementService.FromGuid(duplicatedGuid)
            .Where(x => x.Dlc == null)
            .ToList();
        var chapter3Placements = result.ItemPlacementService.FromSceneGuid(chapter3Scene, duplicatedGuid);
        var chapter4Placements = result.ItemPlacementService.FromSceneGuid(chapter4Scene, duplicatedGuid);

        Assert.True(allPlacements.Count >= 2);
        Assert.Single(chapter3Placements);
        Assert.Single(chapter4Placements);
        Assert.All(chapter3Placements, placement => Assert.Equal(chapter3Scene, placement.SceneFile));
        Assert.All(chapter4Placements, placement => Assert.Equal(chapter4Scene, placement.SceneFile));
    }

    [Fact]
    public void ItemRandomizer_ZeroDropRatios_FallsBackToEthanLeg() {
        using var result = RandomizerTest.RunState();

        var drop = result.ItemRandomizer.GetNextGeneralDrop(
            result.Randomizer.GetRng("tests/zero-ratios"),
            new RandomItemSettings{
                MinAmmoQuantity = 0.5,
                MaxAmmoQuantity = 0.5,
                ItemRatioKeyFunc = _ => 0
            });

        Assert.Equal("EthanLeg", drop.Id);
        Assert.Equal(1, drop.CountEasy);
        Assert.Equal(1, drop.CountNormal);
        Assert.Equal(1, drop.CountMadhouse);
    }

    [Fact]
    public void ItemRandomizer_CreateGeneralItemPool_PreservesFractionalWeightPrecision() {
        using var result = RandomizerTest.RunState();

        var bag = result.ItemRandomizer.CreateGeneralItemPool(
            new RandomItemSettings{
                ItemRatioKeyFunc = id => id switch{
                    "Herb" => 0.03,
                    "Gunpowder" => 0.04,
                    _ => 0
                }
            },
            result.Randomizer.GetRng("tests/pool-precision"));
        var draws = bag.Next(bag.Count);

        Assert.Equal(7, bag.Count);
        Assert.Equal(3, draws.Count(x => x == "Herb"));
        Assert.Equal(4, draws.Count(x => x == "Gunpowder"));
    }

    [Fact]
    public void ItemRandomizer_DetermineDropAmount_RespectsDifficultyScalingForAmmo() {
        using var respecting = RandomizerTest.RunState(config => config["item-drop-respect-difficulty"] = true);
        using var flat = RandomizerTest.RunState(config => config["item-drop-respect-difficulty"] = false);

        var ammo = ItemDefinitionRepository.Default.FromId("ShotgunBullet")!;
        var baseAmount = (uint)Math.Max(1, (int)Math.Round(Math.Min(ammo.MaxStack, 150) * 0.5));

        var respectingAmounts = respecting.ItemRandomizer.DetermineDropAmount(
            ammo.Id,
            0.5,
            0.5,
            respecting.Randomizer.GetRng("tests/respect-difficulty"));
        var flatAmounts = flat.ItemRandomizer.DetermineDropAmount(
            ammo.Id,
            0.5,
            0.5,
            flat.Randomizer.GetRng("tests/flat-difficulty"));

        Assert.Equal(respecting.ItemRandomizer.ApplyDifficultyToDropAmount(baseAmount), respectingAmounts);
        Assert.Equal((baseAmount, baseAmount, baseAmount), flatAmounts);
    }

    [Fact]
    public void RandomItems_SingleDropPool_ReplacesEligibleNormalPlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (definition, placement) = FindRandomizedPlacement(
            result,
            (item, _) => !item.IsWeapon && item.Id != "SaveTape" && !item.IsStoryProgressionItem &&
                         item.Id != ForcedDropId);

        var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
        var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.Equal(definition.Id, beforeItem.ItemDataID);
        Assert.Equal(ForcedDropId, afterItem.ItemDataID);
        Assert.Equal(1, afterItem.ItemStackNum);
        Assert.True(afterItem._IsOverwriteDifficultItemNumSetting);
        Assert.Equal(1, afterItem._DifficultItemNumSetting.EasyNum);
        Assert.Equal(1, afterItem._DifficultItemNumSetting.HardNum);
        Assert.NotEqual(beforeItem.SaveGUID, afterItem.SaveGUID);
    }

    [Theory]
    [InlineData("BurnerBullet")]
    [InlineData("Flower")]
    [InlineData("AlloyClay")]
    [InlineData("Magnesium")]
    [InlineData("SyntheticDetergent")]
    public void RandomItems_SingleDropPool_UsesReplacementTemplateInteractionChildren(string replacementId) {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            ConfigureSingleDrop(config, replacementId);
        });

        var (_, placement, beforeGameObject) = FindRandomizedPlacementWithBeforeObject(
            result,
            (item, placement, gameObject, scene) =>
                !item.IsWeapon &&
                item.Id != replacementId &&
                gameObject.Children.Length > 0 &&
                !HasFsmInHierarchy(scene, placement.Guid));
        var afterGameObject = result.ReadAfterScene(placement.SceneFile).FindGameObject(placement.Guid);
        var template = result.Randomizer.TemplateService.GetItemTemplate(replacementId);

        Assert.NotNull(afterGameObject);
        Assert.Equal(replacementId, afterGameObject!.FindComponent<app.Item>()!.ItemDataID);
        AssertTemplateChildShape(template, afterGameObject);
        AssertNoSharedDescendantGuids(beforeGameObject, afterGameObject);
        AssertNoSharedDescendantGuids(template, afterGameObject);
        AssertNoSharedSaveGuids(template, afterGameObject);
        AssertPickupInteractionsAreReadyForFreshPlacement(template, replacementId);
        AssertPickupInteractionsAreReadyForFreshPlacement(afterGameObject, replacementId);
    }

    [Fact]
    public void RandomItems_FsmControlledPlacement_PreservesOriginalPickupShape() {
        const string replacementId = "MachineGunBullet";
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            ConfigureSingleDrop(config, replacementId);
        });

        var beforeScene = result.ReadBeforeScene(MainHouseHallScenePath);
        var afterScene = result.ReadAfterScene(MainHouseHallScenePath);
        var beforeGameObject = beforeScene.FindGameObject(MainHouseHallDrawerCoinGuid);
        var afterGameObject = afterScene.FindGameObject(MainHouseHallDrawerCoinGuid);
        var template = result.Randomizer.TemplateService.GetItemTemplate(replacementId);

        Assert.NotNull(beforeGameObject);
        Assert.NotNull(afterGameObject);
        Assert.True(HasFsmInHierarchy(beforeScene, MainHouseHallDrawerCoinGuid));
        Assert.Equal(replacementId, afterGameObject!.FindComponent<app.Item>()!.ItemDataID);
        Assert.Equal(beforeGameObject!.Name, afterGameObject.Name);
        Assert.Equal(
            beforeGameObject.Components.Select(component => component.Type.Name),
            afterGameObject.Components.Select(component => component.Type.Name));
        Assert.Equal(
            beforeGameObject.Children.Select(child => child.Name),
            afterGameObject.Children.Select(child => child.Name));
        AssertVisualResourcesMatch(template, afterGameObject);
        Assert.NotEqual(
            GetVisualResource(beforeGameObject, "Mesh"),
            GetVisualResource(afterGameObject, "Mesh"));
    }

    [Fact]
    public void RandomItems_BirthdaySkillValuableDrop_UsesVisibleTemplateWithSkillItemDataId() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["allow-dlc-items"] = true;
            config["item-drop-valuable-birthday-skill"] = true;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (placement, beforeItem, afterItem) = FindChangedPlacementByAfterItem(result, ItemDrops.IsBirthdaySkill);
        var afterScene = result.ReadAfterScene(placement.SceneFile);
        var afterGameObject = afterScene.FindGameObject(placement.Guid);

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.NotEqual(beforeItem.ItemDataID, afterItem.ItemDataID);
        Assert.True(ItemDrops.IsBirthdaySkill(afterItem.ItemDataID));
        Assert.Equal(1, afterItem.ItemStackNum);
        Assert.NotNull(afterGameObject);
        Assert.NotNull(afterGameObject!.FindComponent("via.render.Mesh"));
        Assert.True(BirthdaySkillVisuals.TryGetResources(afterItem.ItemDataID, out var visuals));
        Assert.Equal(visuals.Mesh, GetVisualResource(afterGameObject, "Mesh"));
        Assert.Equal(visuals.Material, GetVisualResource(afterGameObject, "Material"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RandomItems_DlcCoinValuableDrops_PreserveEachRewardAndUsablePickup(bool preserveItemModels) {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["allow-dlc-items"] = true;
            config["item-drop-valuable-dlc-coin"] = true;
            config["preserve-item-models"] = preserveItemModels;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var templateService = result.Randomizer.TemplateService;
        var coinTemplate = templateService.GetItemTemplate("Coin");
        foreach (var (coinId, _, _) in ItemDrops.DlcCoinDrops) {
            var template = templateService.GetItemTemplate(coinId);
            Assert.Equal(coinId, template.FindComponent<app.Item>()!.ItemDataID);
            AssertVisualResourcesMatch(coinTemplate, template);
            Assert.True(HasPickupInteractions(template));

            var (placement, beforeItem, afterItem) = FindChangedPlacementByAfterItem(result, id => id == coinId);
            var beforeScene = result.ReadBeforeScene(placement.SceneFile);
            var afterGameObject = result.ReadAfterScene(placement.SceneFile).FindGameObject(placement.Guid)!;
            Assert.Equal(coinId, afterItem.ItemDataID);
            Assert.Equal(1, afterItem.ItemStackNum);
            Assert.Equal(1, afterItem._DifficultItemNumSetting.EasyNum);
            Assert.Equal(1, afterItem._DifficultItemNumSetting.HardNum);
            Assert.NotEqual(beforeItem.SaveGUID, afterItem.SaveGUID);
            AssertVisualResourcesMatch(
                preserveItemModels ? beforeScene.FindGameObject(placement.Guid)! : template,
                afterGameObject);

            if (!HasFsmInHierarchy(beforeScene, placement.Guid)) {
                AssertTemplateChildShape(template, afterGameObject);
                AssertNoSharedDescendantGuids(template, afterGameObject);
                AssertNoSharedSaveGuids(template, afterGameObject);
                AssertPickupInteractionsAreReadyForFreshPlacement(afterGameObject, coinId);
            }
        }

        Assert.Equal("Coin", coinTemplate.FindComponent<app.Item>()!.ItemDataID);
    }

    [Fact]
    public void RandomItems_ValuableWeaponDrop_PlacesWeaponBeforeGeneralPool() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["item-drop-valuable-weapon"] = true;
            config["item-drop-valuable-birthday-skill"] = false;
            config["item-drop-valuable-lock-pick"] = false;
            config["item-drop-valuable-repair-kit"] = false;
            config["item-drop-valuable-dlc-coin"] = false;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (placement, beforeItem, afterItem) = FindChangedPlacementByAfterItem(
            result,
            itemId => ItemDefinitionRepository.Default.FromId(itemId)?.IsWeapon == true);
        var afterGameObject = result.ReadAfterScene(placement.SceneFile).FindGameObject(placement.Guid);

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.NotEqual(beforeItem.ItemDataID, afterItem.ItemDataID);
        Assert.True(ItemDefinitionRepository.Default.FromId(afterItem.ItemDataID)!.IsWeapon);
        Assert.Equal(1, afterItem.ItemStackNum);
        Assert.NotNull(afterGameObject);
        AssertWeaponPickupInteractionGameObjectsAreReady(afterGameObject!);
    }

    [Fact]
    public void RandomItems_ReplaceWeaponsDisabled_PreservesWeaponPlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["replace-weapons"] = false;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (definition, placement) = FindRandomizedPlacement(result, (item, _) => item.IsWeapon);

        var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
        var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);

        Assert.True(definition.IsWeapon);
        Assert.Equal(beforeItem.ItemDataID, afterItem.ItemDataID);
        Assert.Equal(beforeItem.ItemStackNum, afterItem.ItemStackNum);
        Assert.Equal(beforeItem.SaveGUID, afterItem.SaveGUID);
    }

    [Fact]
    public void RandomItems_ReplaceWeaponsEnabled_ReplacesWeaponPlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["replace-weapons"] = true;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (definition, placement) =
            FindRandomizedPlacement(result, (item, _) => item.IsWeapon && item.Id != ForcedDropId);

        var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
        var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);

        Assert.True(definition.IsWeapon);
        Assert.NotEqual(beforeItem.ItemDataID, afterItem.ItemDataID);
        Assert.Equal(ForcedDropId, afterItem.ItemDataID);
    }

    [Fact]
    public void RandomItems_ReplaceMadhouseTapesDisabled_PreservesSaveTapePlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["replace-madhouse-tapes"] = false;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (_, placement) = FindRandomizedPlacement(result, (item, _) => item.Id == "SaveTape");

        var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
        var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);

        Assert.Equal("SaveTape", beforeItem.ItemDataID);
        Assert.Equal(beforeItem.ItemDataID, afterItem.ItemDataID);
        Assert.Equal(beforeItem.SaveGUID, afterItem.SaveGUID);
    }

    [Fact]
    public void RandomItems_ReplaceMadhouseTapesEnabled_ReplacesSaveTapePlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["replace-madhouse-tapes"] = true;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var (_, placement) = FindRandomizedPlacement(result, (item, _) => item.Id == "SaveTape");

        var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
        var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);

        Assert.Equal("SaveTape", beforeItem.ItemDataID);
        Assert.Equal(ForcedDropId, afterItem.ItemDataID);
        Assert.NotEqual(beforeItem.SaveGUID, afterItem.SaveGUID);
    }

    [Fact]
    public void RandomItems_MiaPastVhsItemScene_IsLoadedAndRandomized() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var area = Assert.Single(result.AreaService.Areas, area => area.Path == MiaPastVhsItemScenePath);
        Assert.Contains(area.Items, item => item.Guid == MiaPastVhsChemicalGuid);

        var beforeItem = GetItem(result.ReadBeforeScene(MiaPastVhsItemScenePath), MiaPastVhsChemicalGuid);
        var afterItem = GetItem(result.ReadAfterScene(MiaPastVhsItemScenePath), MiaPastVhsChemicalGuid);

        Assert.True(result.WasFileModified(MiaPastVhsItemScenePath));
        Assert.Equal("ChemicalS", beforeItem.ItemDataID);
        Assert.Equal(ForcedDropId, afterItem.ItemDataID);
        Assert.NotEqual(beforeItem.SaveGUID, afterItem.SaveGUID);
    }

    [Fact]
    public void AdditionalItems_Enabled_AddsRandomExtraItemAtConfiguredPlacement() {
        const string forcedDropId = "MachineGunBullet";
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["additional-items"] = true;
            ConfigureSingleDrop(config, forcedDropId);
        });

        var placement = result.ItemPlacementService.ItemPlacements.First(x =>
            x.Enabled &&
            x.IsExtra &&
            !string.IsNullOrEmpty(x.SceneFile) &&
            x.Tags.Contains(ExtraPlacementModifier.RandomItemTag));

        var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(placement.SceneFile));
        var afterDynamic = GetDynamicParent(result.ReadAfterScene(placement.SceneFile));
        var newChildren = GetNewChildren(beforeDynamic, afterDynamic);
        var newItem = Assert.Single(newChildren, child => child.FindComponent<app.Item>() != null);
        var item = newItem.FindComponent<app.Item>()!;
        var transform = newItem.FindComponent<GeneratedViaTransform>()!;
        var template = result.Randomizer.TemplateService.GetItemTemplate(forcedDropId);

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.Equal(beforeDynamic.Children.Count() + 1, afterDynamic.Children.Count());
        Assert.Equal(forcedDropId, item.ItemDataID);
        Assert.True(item.ItemStackNum > 0);
        AssertPositionMatchesPlacement(transform, placement);
        AssertTemplateChildShape(template, newItem);
        AssertNoSharedDescendantGuids(template, newItem);
        AssertNoSharedSaveGuids(template, newItem);
        AssertPickupInteractionsAreReadyForFreshPlacement(template, forcedDropId);
        AssertPickupInteractionsAreReadyForFreshPlacement(newItem, forcedDropId);
    }

    [Fact]
    public void AdditionalWeaponChests_Enabled_AddsDrawerOwnedWeaponPickup() {
        using var result = RandomizerTest.RunState(config => { config["additional-items"] = true; });

        var placement = result.ItemPlacementService.ItemPlacements.First(x =>
            x.Enabled &&
            x.IsExtra &&
            !string.IsNullOrEmpty(x.SceneFile) &&
            x.SceneFile.EndsWith("c03_rightareab1ffreezer.scn.20", StringComparison.OrdinalIgnoreCase) &&
            x.Tags.Contains(ExtraPlacementModifier.WeaponChestTag));

        var beforeScene = result.ReadBeforeScene(placement.SceneFile);
        var afterScene = result.ReadAfterScene(placement.SceneFile);
        var beforeRootGuids = beforeScene.Children
            .OfType<RszGameObject>()
            .Select(child => child.Guid)
            .ToHashSet();
        var newRootObjects = afterScene.Children
            .OfType<RszGameObject>()
            .Where(child => !beforeRootGuids.Contains(child.Guid))
            .ToArray();
        var chest = Assert.Single(newRootObjects, child =>
            child.Children.Any(grandChild => grandChild.FindComponent<app.InteractDrawer>() != null));
        var drawerObject = Assert.Single(chest.Children, child => child.FindComponent<app.InteractDrawer>() != null);
        Assert.True(drawerObject.Settings.Get<bool>("Update"));
        Assert.True(drawerObject.Settings.Get<bool>("Draw"));
        var drawer = drawerObject.FindComponent<app.InteractDrawer>()!;
        var weapon = afterScene.FindGameObject(drawer.DirectSetGameObject);
        Assert.NotNull(weapon);
        var weaponItem = weapon.FindComponent<app.Item>();
        var weaponInteractions = GetWeaponInteractionObjects(weapon);

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.NotNull(weaponItem);
        Assert.Equal("", drawer.SetItemID);
        Assert.Equal(-1, drawer.ChangeStackNum);
        Assert.False(drawer.UseDrawerPos);
        Assert.True(drawer.IsDirectGameObjectSet);
        Assert.NotEqual(Guid.Empty, drawer.DirectSetGameObject);
        Assert.Contains(drawerObject.Children, child => child.Guid == drawer.DirectSetGameObject);
        var weaponTransform = weapon.FindComponent<GeneratedViaTransform>()!;
        Assert.Equal(System.Numerics.Vector3.Zero, weaponTransform.Position);
        Assert.Equal(System.Numerics.Quaternion.Identity, weaponTransform.Rotation);
        Assert.False(weapon.Settings.Get<bool>("Update"));
        Assert.False(weapon.Settings.Get<bool>("Draw"));
        Assert.NotEmpty(weaponInteractions);
        Assert.Single(chest.Children);
        chest.Visit(node => {
            if (node is RszObjectNode obj && obj.Type.Name == "app.fsm.InteractTest")
                Assert.Equal(drawerObject.Guid, obj.Get<Guid>("InteractObj"));
            if (node is RszValueNode value && value.Type == RszFieldType.GameObjectRef) {
                var reference = RszSerializer.Deserialize<Guid>(value);
                if (reference != Guid.Empty) Assert.NotNull(afterScene.FindGameObject(reference));
            }
        });
        Assert.All(weaponInteractions, interaction => {
            Assert.True(interaction.GameObject.Settings.Get<bool>("Update"));
            Assert.False(interaction.GameObject.Settings.Get<bool>("Draw"));
            Assert.False(interaction.Component.IsCheckAngle);
            Assert.False(interaction.Component.IsGetEventEnabled);
            Assert.False(interaction.Component.IsForceEquip);
            Assert.True(interaction.Component.UsePickupSE);
        });
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AdditionalWeaponChests_ExcludeEthansStartingWeapons(bool randomizeInventory, bool additionalGun) {
        using var result = RandomizerTest.RunState(config => {
            config["additional-items"] = true;
            config["random-starting-inventory-ethan"] = randomizeInventory;
            config["random-starting-inventory-additional-gun-ethan"] = additionalGun;
            foreach (var category in Enum.GetValues<StartingWeaponCategory>())
                config[$"inventory-weapon-{category.ToString().ToLowerInvariant()}-ethan"] =
                    category == StartingWeaponCategory.MachineGun;
        }, seed: 776198);

        var inventory = result.ReadAfterUserFile<app.AddItemListData>(RandomizerTestPaths.EthanInventoryPath)._AddItems;
        Assert.Contains(inventory, item => item.ItemDataID == "MachineGun");
        var chestWeapons = new List<string>();
        foreach (var path in result.ItemPlacementService.ItemPlacements
                     .Where(p => p.Enabled && p.IsExtra && p.Tags.Contains(ExtraPlacementModifier.WeaponChestTag))
                     .Select(p => p.SceneFile).Distinct()) {
            var before = result.ReadBeforeScene(path);
            var after = result.ReadAfterScene(path);
            foreach (var root in after.Children.OfType<RszGameObject>()
                         .Where(go => before.FindGameObject(go.Guid) == null)) {
                foreach (var drawer in root.Children.Select(go => go.FindComponent<app.InteractDrawer>())
                             .OfType<app.InteractDrawer>()) {
                    var item = after.FindGameObject(drawer.DirectSetGameObject)?.FindComponent<app.Item>();
                    if (item != null) chestWeapons.Add(item.ItemDataID);
                }
            }
        }
        Assert.NotEmpty(chestWeapons);
        Assert.DoesNotContain("MachineGun", chestWeapons);
    }

    [Fact]
    public void AdditionalWoodenCrates_Enabled_AddsCrateAtConfiguredPlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["additional-wooden-crates"] = true;
            config["additional-wooden-crates-fakes"] = false;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var placement = result.ItemPlacementService.ItemPlacements.First(x =>
            x.Enabled &&
            x.IsExtra &&
            !string.IsNullOrEmpty(x.SceneFile) &&
            x.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag) &&
            x.Tags.Contains(ExtraPlacementModifier.NotFakeCrateTag));
        var expectedAdditions = result.ItemPlacementService.ItemPlacements.Count(x =>
            x.Enabled &&
            x.IsExtra &&
            x.SceneFile == placement.SceneFile &&
            (x.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag)
             || x.Tags.Contains(ExtraPlacementModifier.ItemBoxTag)));

        var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(placement.SceneFile));
        var afterDynamic = GetDynamicParent(result.ReadAfterScene(placement.SceneFile));
        var newChildren = GetNewChildren(beforeDynamic, afterDynamic);
        var newChild = Assert.Single(newChildren, child => {
            var transform = child.FindComponent<GeneratedViaTransform>();
            return transform != null && TransformMatchesPlacement(transform, placement);
        });
        var transform = newChild.FindComponent<GeneratedViaTransform>()!;
        var destruct = newChild.FindComponent<app.ItemDropDestruct>();

        Assert.True(result.WasFileModified(placement.SceneFile));
        Assert.Equal(expectedAdditions, newChildren.Count);
        Assert.Equal(beforeDynamic.Children.Count() + expectedAdditions, afterDynamic.Children.Count());
        Assert.NotNull(destruct);
        Assert.True(destruct!.Enabled);
        Assert.Equal(ForcedDropId, destruct.SetItemID);
        Assert.Equal(1, destruct.ChangeStackNum);
        AssertPositionMatchesPlacement(transform, placement);
    }

    [Fact]
    public void AdditionalCrates_InSameScene_HaveIndependentPersistentIds() {
        using var result = RandomizerTest.RunState(config => {
            config["additional-wooden-crates"] = true;
            config["additional-wooden-crates-fakes"] = false;
        }, seed: 100583);
        var scenes = result.ItemPlacementService.ItemPlacements
            .Where(p => p.Enabled && p.IsExtra && p.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag))
            .GroupBy(p => p.SceneFile).Where(g => g.Count() > 1);
        var checkedCrates = 0;
        foreach (var group in scenes) {
            var before = GetDynamicParent(result.ReadBeforeScene(group.Key));
            var after = GetDynamicParent(result.ReadAfterScene(group.Key));
            var ids = new HashSet<Guid>();
            foreach (var crate in GetNewChildren(before, after).Where(go => go.Name == "ItemBox_VLong")) {
                checkedCrates++;
                var ownIds = new HashSet<Guid>();
                crate.Visit(node => {
                    if (node is not RszObjectNode obj) return;
                    foreach (var name in new[] { "SaveGUID", "InstanceGuid" }) {
                        if (obj.Type.FindFieldIndex(name) >= 0 && obj.Get<Guid>(name) is var id && id != Guid.Empty)
                            ownIds.Add(id);
                    }
                });
                Assert.NotEmpty(ownIds);
                Assert.All(ownIds, id => Assert.True(ids.Add(id), $"Two crates share persistent ID {id} in {group.Key}"));
            }
        }
        Assert.True(checkedCrates >= 2);
    }

    [Fact]
    public void AdditionalWoodenCrates_SkipsScriptedShipFlashbackPlacement() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["additional-wooden-crates"] = true;
        });

        var placement = result.ItemPlacementService.ItemPlacements.Single(x =>
            x.Enabled &&
            x.IsExtra &&
            x.SceneFile.EndsWith("c04_shipb2engineroom01past.scn.20", StringComparison.OrdinalIgnoreCase) &&
            x.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag));
        var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(placement.SceneFile));
        var afterDynamic = GetDynamicParent(result.ReadAfterScene(placement.SceneFile));
        var newChildren = GetNewChildren(beforeDynamic, afterDynamic);

        Assert.DoesNotContain(newChildren, child => {
            var transform = child.FindComponent<GeneratedViaTransform>();
            return transform != null && TransformMatchesPlacement(transform, placement);
        });
        Assert.Contains("scripted flashback scene", result.ProcessLog);
    }

    [Fact]
    public void AdditionalWoodenCrates_FakeProbability_UsesFractionalConfigValue() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["additional-wooden-crates"] = true;
            config["additional-wooden-crates-fakes"] = true;
            config["additional-wooden-crates-fakes-pct-min"] = 1.0;
            config["additional-wooden-crates-fakes-pct-max"] = 1.0;
            ConfigureSingleDrop(config, ForcedDropId);
        });

        var placement = result.ItemPlacementService.ItemPlacements.First(x =>
            x.Enabled &&
            x.IsExtra &&
            !string.IsNullOrEmpty(x.SceneFile) &&
            x.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag) &&
            !x.Tags.Contains(ExtraPlacementModifier.NotFakeCrateTag));

        var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(placement.SceneFile));
        var afterDynamic = GetDynamicParent(result.ReadAfterScene(placement.SceneFile));
        var newChildren = GetNewChildren(beforeDynamic, afterDynamic);
        var newChild = Assert.Single(newChildren, child => {
            var transform = child.FindComponent<GeneratedViaTransform>();
            return transform != null && TransformMatchesPlacement(transform, placement);
        });

        Assert.Equal("ItemBox_Fake", newChild.Name);
    }

    [Fact]
    public void AdditionalWoodenCrates_UnsupportedRuntimeDrops_AreExcluded() {
        using var result = RandomizerTest.RunState(config => {
            config["random-items"] = true;
            config["additional-wooden-crates"] = true;
            config["additional-wooden-crates-fakes"] = false;

            foreach (var dropId in ItemDrops.GenericDrops) {
                config[$"item-drop-ratio-{dropId.ToLowerInvariant()}"] = 0.0;
            }

            config["item-drop-ratio-stimulant"] = 1.0;
            config["item-drop-ratio-depressant"] = 1.0;
        });

        var placement = result.ItemPlacementService.ItemPlacements.First(x =>
            x.Enabled &&
            x.IsExtra &&
            !string.IsNullOrEmpty(x.SceneFile) &&
            x.Tags.Contains(ExtraPlacementModifier.WoodenCrateTag) &&
            x.Tags.Contains(ExtraPlacementModifier.NotFakeCrateTag));

        var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(placement.SceneFile));
        var afterDynamic = GetDynamicParent(result.ReadAfterScene(placement.SceneFile));
        var newChildren = GetNewChildren(beforeDynamic, afterDynamic);
        var newChild = Assert.Single(newChildren, child => {
            var transform = child.FindComponent<GeneratedViaTransform>();
            return transform != null && TransformMatchesPlacement(transform, placement);
        });
        var destruct = newChild.FindComponent<app.ItemDropDestruct>();

        Assert.NotNull(destruct);
        Assert.Equal("Herb", destruct!.SetItemID);
        Assert.DoesNotContain(destruct.SetItemID, ItemDrops.UnsupportedRuntimeDropIds);
    }

    [Fact]
    public void ExtraPlacements_SameSeed_ProducesStableChangedFiles() {
        static void Configure(RandomizerConfiguration config) {
            config["random-items"] = true;
            config["additional-items"] = true;
            config["additional-wooden-crates"] = true;
            config["additional-wooden-crates-fakes"] = true;
            config["additional-wooden-crates-fakes-pct-min"] = 1.0;
            config["additional-wooden-crates-fakes-pct-max"] = 1.0;
            ConfigureSingleDrop(config, ForcedDropId);
        }

        using var first = RandomizerTest.RunState(Configure);
        using var second = RandomizerTest.RunState(Configure);

        Assert.Equal(first.ChangedFiles.Keys.Order(StringComparer.OrdinalIgnoreCase),
            second.ChangedFiles.Keys.Order(StringComparer.OrdinalIgnoreCase));
        foreach (var path in first.ChangedFiles.Keys) {
            Assert.Equal(first.ChangedFiles[path], second.ChangedFiles[path]);
        }

        Assert.Equal(
            first.Randomizer.FileRepository.GetOutputPakFile().ToByteArray(),
            second.Randomizer.FileRepository.GetOutputPakFile().ToByteArray());
    }

    [Fact]
    public void ItemBoxes_AreAddedEvenWhenRandomAndAdditionalItemsAreDisabled() {
        using var result = RandomizerTest.RunState();

        var placements = result.ItemPlacementService.ItemPlacements
            .Where(x =>
                x.Enabled &&
                x.IsExtra &&
                !string.IsNullOrWhiteSpace(x.SceneFile) &&
                x.Tags.Contains(ExtraPlacementModifier.ItemBoxTag))
            .ToList();

        // Locations belong to the CSV. The fixture test below keeps behavior
        // covered even if all optional item boxes are removed from that data.
        foreach (var sceneGroup in placements.GroupBy(x => x.SceneFile, StringComparer.OrdinalIgnoreCase)) {
            var beforeDynamic = GetDynamicParent(result.ReadBeforeScene(sceneGroup.Key));
            var afterDynamic = GetDynamicParent(result.ReadAfterScene(sceneGroup.Key));
            var newChildren = GetNewChildren(beforeDynamic, afterDynamic);

            Assert.True(result.WasFileModified(sceneGroup.Key));
            Assert.Equal(beforeDynamic.Children.Count() + sceneGroup.Count(), afterDynamic.Children.Count());

            foreach (var placement in sceneGroup) {
                var newChild = Assert.Single(newChildren, child => {
                    var transform = child.FindComponent<GeneratedViaTransform>();
                    return transform != null && TransformMatchesPlacement(transform, placement);
                });
                AssertUsableItemBox(newChild);
                AssertPositionMatchesPlacement(newChild.FindComponent<GeneratedViaTransform>()!, placement);
            }
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ItemBoxes_RespectPlacementPresenceAndEnabledState(bool includePlacement, bool enabled) {
        const string scenePath = "natives/stm/environment/scene/chapter3/c03_leftarea1fpuzzleroom1.scn.20";
        // Own the placement input so additions, moves, and removals in the
        // production CSV do not change what this behavior test exercises.
        var csv = "IsExtra,Tags,Id,Enabled,Chapter,PosX,PosY,PosZ,RotY,RotW,SceneFile\n";
        if (includePlacement) {
            csv += $"TRUE,item_box,,{enabled},3,50,1,20,1,0,{scenePath}\n";
        }
        using var result = RandomizerTest.RunState(prepareRandomizer: randomizer =>
            randomizer.DynamicData.SetData(DynamicDataName.ItemPlacements, Encoding.UTF8.GetBytes(csv)));

        var existing = result.ReadBeforeScene(scenePath).GetGameObjects().Select(go => go.Guid).ToHashSet();
        var addedBoxes = result.ReadAfterScene(scenePath).GetGameObjects()
            .Where(go => !existing.Contains(go.Guid) && go.Name == "ItemBox").ToList();
        if (!includePlacement || !enabled) {
            Assert.Empty(addedBoxes);
            return;
        }

        var box = Assert.Single(addedBoxes);
        AssertUsableItemBox(box);
        var transform = box.FindComponent<GeneratedViaTransform>()!;
        Assert.Equal(new Vector3(50, 1, 20), transform.Position);
        Assert.Equal(new Quaternion(0, 1, 0, 0), transform.Rotation);
    }

    private static void AssertUsableItemBox(RszGameObject box) {
        Assert.Equal("ItemBox", box.Name);
        Assert.Contains(box.Children, child => child.FindComponent<app.InteractSendFsm>() is { Enabled: true, IsEnable: true });
        Assert.Contains(box.Components, component => component.Type.Name == "via.fsm.Fsm");
    }

    [Fact]
    public void BirdCageModifier_Enabled_ChangesRewardDataInBirdCageScene() {
        using var result = RandomizerTest.RunState(config => { config["random-bird-cage-drugs-coins"] = true; });

        var path = BirdCageScenePath.SceneFile();
        var beforeStates = GetBirdCageStates(result.ReadBeforeScene(path));
        var afterStates = GetBirdCageStates(result.ReadAfterScene(path));

        Assert.True(result.WasFileModified(path));
        Assert.NotEmpty(beforeStates);
        Assert.Equal(beforeStates.Count, afterStates.Count);
        Assert.Contains(afterStates, after => {
            var before = beforeStates.Single(x => x.ContainerGuid == after.ContainerGuid);
            return before.ItemId != after.ItemId ||
                   before.ItemCount != after.ItemCount ||
                   before.CoinCount != after.CoinCount;
        });
    }

    private static void ConfigureSingleDrop(RandomizerConfiguration configuration, string itemId) {
        foreach (var dropId in ItemDrops.GenericDrops) {
            configuration[$"item-drop-ratio-{dropId.ToLowerInvariant()}"] = dropId == itemId ? 1.0 : 0.0;
        }
    }

    private static (ItemDefinition Definition, ItemPlacement Placement) FindRandomizedPlacement(
        RandomizerRunResult result,
        Func<ItemDefinition, ItemPlacement, bool> predicate) {
        var itemRandomizer = result.ItemRandomizer;

        var match = result.AreaService.Areas
            .SelectMany(area => area.Items.Select(gameObject => (AreaPath: area.Path, GameObject: gameObject)))
            .SelectMany(x => result.ItemPlacementService.FromSceneGuid(x.AreaPath, x.GameObject.Guid))
            .DistinctBy(placement => (placement.SceneFile, placement.Guid))
            .Select(placement => (Definition: ItemDefinitionRepository.Default.FromId(placement.Id)!,
                Placement: placement))
            .Where(tuple =>
                tuple.Definition != null &&
                tuple.Placement.Dlc == null &&
                !tuple.Placement.IsExtra &&
                tuple.Placement.Enabled &&
                !tuple.Placement.Tags.Contains(ItemPlacement.ExcludeTag) &&
                itemRandomizer.IsItemAllowed(tuple.Definition) &&
                !BirdCageModifier.Guids.Contains(tuple.Placement.Guid) &&
                predicate(tuple.Definition, tuple.Placement))
            .FirstOrDefault();

        Assert.NotNull(match.Definition);
        return match;
    }

    private static (ItemDefinition Definition, ItemPlacement Placement, RszGameObject BeforeGameObject)
        FindRandomizedPlacementWithBeforeObject(
            RandomizerRunResult result,
            Func<ItemDefinition, ItemPlacement, RszGameObject, RszScene, bool> predicate) {
        var itemRandomizer = result.ItemRandomizer;
        var sceneCache = new Dictionary<string, RszScene>();
        var placements = result.AreaService.Areas
            .SelectMany(area => area.Items.Select(gameObject => (AreaPath: area.Path, GameObject: gameObject)))
            .SelectMany(x => result.ItemPlacementService.FromSceneGuid(x.AreaPath, x.GameObject.Guid))
            .DistinctBy(placement => (placement.SceneFile, placement.Guid));

        foreach (var placement in placements) {
            var definition = ItemDefinitionRepository.Default.FromId(placement.Id);
            if (definition == null ||
                placement.Dlc != null ||
                placement.IsExtra ||
                !placement.Enabled ||
                placement.Tags.Contains(ItemPlacement.ExcludeTag) ||
                !itemRandomizer.IsItemAllowed(definition) ||
                BirdCageModifier.Guids.Contains(placement.Guid)) {
                continue;
            }

            if (!sceneCache.TryGetValue(placement.SceneFile, out var scene)) {
                scene = result.ReadBeforeScene(placement.SceneFile);
                sceneCache[placement.SceneFile] = scene;
            }

            var gameObject = scene.FindGameObject(placement.Guid);
            if (gameObject != null && predicate(definition, placement, gameObject, scene)) {
                return (definition, placement, gameObject);
            }
        }

        Assert.Fail("No randomized item placement matched the expected before-object predicate.");
        throw new InvalidOperationException();
    }

    private static (ItemPlacement Placement, app.Item BeforeItem, app.Item AfterItem) FindChangedPlacementByAfterItem(
        RandomizerRunResult result,
        Func<string, bool> predicate) {
        var itemRandomizer = result.ItemRandomizer;
        var placements = result.AreaService.Areas
            .SelectMany(area => area.Items.Select(gameObject => (AreaPath: area.Path, GameObject: gameObject)))
            .SelectMany(x => result.ItemPlacementService.FromSceneGuid(x.AreaPath, x.GameObject.Guid))
            .DistinctBy(placement => (placement.SceneFile, placement.Guid))
            .Where(placement => {
                var definition = ItemDefinitionRepository.Default.FromId(placement.Id);
                return definition != null &&
                       placement.Dlc == null &&
                       !placement.IsExtra &&
                       placement.Enabled &&
                       !placement.Tags.Contains(ItemPlacement.ExcludeTag) &&
                       itemRandomizer.IsItemAllowed(definition) &&
                       !BirdCageModifier.Guids.Contains(placement.Guid);
            });

        foreach (var placement in placements) {
            if (!result.WasFileModified(placement.SceneFile)) {
                continue;
            }

            var beforeItem = GetItem(result.ReadBeforeScene(placement.SceneFile), placement.Guid);
            var afterItem = GetItem(result.ReadAfterScene(placement.SceneFile), placement.Guid);
            if (beforeItem.ItemDataID != afterItem.ItemDataID && predicate(afterItem.ItemDataID)) {
                return (placement, beforeItem, afterItem);
            }
        }

        Assert.Fail("No changed item placement matched the expected replacement item.");
        throw new InvalidOperationException();
    }

    private static app.Item GetItem(RszScene scene, Guid guid) {
        var gameObject = scene.FindGameObject(guid);
        Assert.NotNull(gameObject);

        var item = gameObject!.FindComponent<app.Item>();
        Assert.NotNull(item);
        return item!;
    }

    private static bool HasFsmInHierarchy(RszScene scene, Guid guid)
        => scene.FindGameObjectsByGuidWithFsmContext([guid]).TryGetValue(guid, out var match) &&
           match.HasFsmInHierarchy;

    private static RszGameObject GetDynamicParent(RszScene scene) {
        var gameObject = scene.FindGameObject(go => go.Name.EndsWith("_dynamic", StringComparison.Ordinal));
        Assert.NotNull(gameObject);
        return gameObject!;
    }

    private static IReadOnlyList<RszGameObject> GetNewChildren(RszGameObject before, RszGameObject after) {
        var beforeGuids = before.Children.Select(child => child.Guid).ToHashSet();
        return after.Children.Where(child => !beforeGuids.Contains(child.Guid)).ToArray();
    }

    private static void AssertTemplateChildShape(RszGameObject template, RszGameObject actual) {
        Assert.Equal(template.Children.Length, actual.Children.Length);
        for (var i = 0; i < template.Children.Length; i++) {
            AssertTemplateShape(template.Children[i], actual.Children[i]);
        }
    }

    private static void AssertTemplateShape(RszGameObject template, RszGameObject actual) {
        Assert.Equal(template.Name, actual.Name);
        Assert.Equal(template.Prefab, actual.Prefab);
        Assert.Equal(
            template.Components.Select(component => component.Type.Name),
            actual.Components.Select(component => component.Type.Name));
        AssertTemplateChildShape(template, actual);
    }

    private static void AssertNoSharedDescendantGuids(RszGameObject left, RszGameObject right) {
        var leftGuids = GetDescendantGuids(left);
        var rightGuids = GetDescendantGuids(right);
        Assert.Empty(leftGuids.Intersect(rightGuids));
    }

    private static HashSet<Guid> GetDescendantGuids(RszGameObject gameObject) {
        var result = new HashSet<Guid>();
        foreach (var child in gameObject.Children) {
            child.VisitGameObjects(descendant => result.Add(descendant.Guid));
        }

        return result;
    }

    private static void AssertNoSharedSaveGuids(RszGameObject left, RszGameObject right) {
        var leftGuids = GetSaveGuids(left);
        var rightGuids = GetSaveGuids(right);
        Assert.Empty(leftGuids.Intersect(rightGuids));
    }

    private static bool IsKeyItem(ItemDefinition item)
        => item.CategoryType is ItemCategoryType.KeyItem
            or ItemCategoryType.UsableKeyItem
            or ItemCategoryType.DiscardableKeyItem;

    private static bool TryGetItemTemplate(TemplateService templateService, string itemId, out RszGameObject template) {
        try {
            template = templateService.GetItemTemplate(itemId);
            return true;
        }
        catch {
            template = null!;
            return false;
        }
    }

    private static bool HasPickupInteractions(RszGameObject gameObject) {
        var result = false;
        gameObject.VisitGameObjects(child => { result |= child.FindComponent<app.InteractDetailSearch>() != null; });
        return result;
    }

    private static List<(RszGameObject GameObject, app.InteractWeapon Component)> GetWeaponInteractionObjects(
        RszGameObject gameObject) {
        var result = new List<(RszGameObject, app.InteractWeapon)>();
        gameObject.VisitGameObjects(child => {
            var interact = child.FindComponent<app.InteractWeapon>();
            if (interact != null) {
                result.Add((child, interact));
            }
        });
        return result;
    }

    private static void AssertWeaponPickupInteractionGameObjectsAreReady(RszGameObject gameObject) {
        var interactions = GetWeaponInteractionObjects(gameObject);
        Assert.NotEmpty(interactions);
        Assert.All(interactions, interaction => {
            Assert.True(interaction.GameObject.Settings.Get<bool>("Update"));
            Assert.False(interaction.GameObject.Settings.Get<bool>("Draw"));
            Assert.False(interaction.Component.IsCheckAngle);
            Assert.False(interaction.Component.IsGetEventEnabled);
            Assert.False(interaction.Component.IsForceEquip);
            Assert.True(interaction.Component.UsePickupSE);
        });
    }

    private static void AssertPickupInteractionsAreReadyForFreshPlacement(RszGameObject gameObject, string itemId) {
        var interactions = new List<app.InteractDetailSearch>();
        gameObject.VisitGameObjects(child => {
            var interact = child.FindComponent<app.InteractDetailSearch>();
            if (interact != null) {
                interactions.Add(interact);
            }
        });

        Assert.True(interactions.Count > 0, $"{itemId} template has no InteractDetailSearch pickup interaction.");
        Assert.All(interactions, interact => Assert.False(interact.IsCheckAngle));
        Assert.All(interactions, interact => Assert.False(interact.IsItemGet));
    }

    private static void AssertVisualResourcesMatch(RszGameObject expected, RszGameObject actual) {
        Assert.Equal(GetVisualResource(expected, "Mesh"), GetVisualResource(actual, "Mesh"));
        Assert.Equal(GetVisualResource(expected, "Material"), GetVisualResource(actual, "Material"));
    }

    private static string GetVisualResource(RszGameObject gameObject, string fieldName) {
        var mesh = gameObject.FindComponent("via.render.Mesh");
        Assert.NotNull(mesh);
        return mesh![fieldName].ToString() ?? "";
    }

    private static List<Guid> GetSaveGuids(RszGameObject gameObject) {
        var result = new List<Guid>();
        gameObject.Visit(node => {
            if (node is not RszObjectNode objectNode)
                return;

            var saveGuidIndex = objectNode.Type.FindFieldIndex("SaveGUID");
            if (saveGuidIndex == -1 ||
                objectNode.Children[saveGuidIndex] is not RszValueNode saveGuidNode ||
                saveGuidNode.Type != RszFieldType.Guid) {
                return;
            }

            var saveGuid = RszSerializer.Deserialize<Guid>(saveGuidNode);
            if (saveGuid != Guid.Empty) {
                result.Add(saveGuid);
            }
        });

        return result;
    }

    private static void AssertPositionMatchesPlacement(GeneratedViaTransform transform, ItemPlacement placement) {
        Assert.True(TransformMatchesPlacement(transform, placement));
    }

    private static bool TransformMatchesPlacement(GeneratedViaTransform transform, ItemPlacement placement) {
        const float tolerance = 0.001f;
        return Math.Abs(transform.Position.X - placement.PosX) <= tolerance
               && Math.Abs(transform.Position.Y - placement.PosY) <= tolerance
               && Math.Abs(transform.Position.Z - placement.PosZ) <= tolerance;
    }

    private static List<BirdCageState> GetBirdCageStates(RszScene scene) {
        var states = new List<BirdCageState>();

        scene.VisitGameObjects(gameObject => {
            if (!gameObject.Name.Contains("CoinBox", StringComparison.OrdinalIgnoreCase))
                return;

            var gimmick = gameObject.Children.FirstOrDefault(child =>
                child.Name.EndsWith("_Gimmick", StringComparison.Ordinal) &&
                child.FindComponent<app.CoinCounter>() != null);
            var itemHolder = gameObject.Children.FirstOrDefault(child => child.FindComponent<app.Item>() != null);
            if (gimmick == null || itemHolder == null)
                return;

            var item = itemHolder.FindComponent<app.Item>()!;
            var coinCounter = gimmick.FindComponent<app.CoinCounter>()!;
            states.Add(new BirdCageState(gameObject.Guid, item.ItemDataID, item.ItemStackNum, coinCounter.CoinMax));
        });

        return states;
    }

    private sealed record BirdCageState(Guid ContainerGuid, string ItemId, int ItemCount, int CoinCount);
}
