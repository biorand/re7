using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Services;
using Enums.app;
using IntelOrca.Biohazard.REE.Rsz;
using System.Globalization;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.REE;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class ExtraPlacementModifier : Modifier {
    private readonly Randomizer _randomizer;

    public ExtraPlacementModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    #region Tags

    internal const string WoodenCrateTag = "crate";
    internal const string NotFakeCrateTag = "not_fake";
    internal const string FakeCrateTag = "fake";
    internal const string WeaponChestTag = "weapon_chest";
    internal const string ItemBoxTag = "item_box";
    internal const string RandomItemTag = "random";

    #endregion

    private const string WoodenCrateGameObjectName = "ItemBox_VLong";
    private const string FakeWoodenCrateGameObjectName = "ItemBox_Fake";
    private const string ItemBoxGameObjectName = "ItemBox";
    private const int PreferredHealingDropProbability = 50; // TODO Config?

    private static readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;

    private enum ExtraPlacementKind {
        Item,
        WoodenCrate,
        WeaponChest,
        ItemBox
    }

    internal static bool IsPlainExtraItemPlacement(ItemPlacement placement)
        => placement.IsExtra &&
           !placement.Tags.Contains(WoodenCrateTag) &&
           !placement.Tags.Contains(WeaponChestTag) &&
           !placement.Tags.Contains(ItemBoxTag);

    internal static Guid GetGeneratedItemGuid(ItemPlacement placement) {
        if (placement.Guid != Guid.Empty)
            return placement.Guid;

        var key = string.Join("|", [
            "extra-item",
            placement.SceneFile,
            placement.Id,
            string.Join(",", placement.Tags.Order(StringComparer.Ordinal)),
            placement.Comment ?? "",
            Format(placement.PosX),
            Format(placement.PosY),
            Format(placement.PosZ),
            Format(placement.RotX),
            Format(placement.RotY),
            Format(placement.RotZ),
            Format(placement.RotW),
        ]);
        return key.GetGuidHash();

        static string Format(float value)
            => value.ToString("R", CultureInfo.InvariantCulture);
    }

    private RszScene AddExtraChest(
        RszScene scene,
        Randomizer randomizer,
        RandomizerLogger logger,
        ItemPlacement placement)
        => randomizer.ChestService.PlaceWeaponChest(logger, scene, placement);

    private RszGameObject AddExtraCrate(
        RszGameObject parentGameObject,
        Randomizer randomizer,
        RandomizerLogger logger,
        ItemPlacement placement,
        Rng rng,
        RandomItemSettings randomItemSettings) {
        var allowFakeCrates = randomizer.GetConfigOption<bool>("additional-wooden-crates-fakes");
        RszGameObject template;
        var isFake = false;
        var newGuid = rng.NextGuid();
        var minFakePct = randomizer.GetConfigOption<double>("additional-wooden-crates-fakes-pct-min");
        var maxFakePct = randomizer.GetConfigOption<double>("additional-wooden-crates-fakes-pct-max");
        var fakeProbability = rng.NextDouble(minFakePct, maxFakePct);

        if ((allowFakeCrates && placement.Tags.Contains(FakeCrateTag)) ||
            (!placement.Tags.Contains(NotFakeCrateTag) && allowFakeCrates && rng.NextProbability(fakeProbability))) {
            isFake = true;
            template = randomizer.TemplateService
                .GetObject(FakeWoodenCrateGameObjectName)
                .CloneWithNewGuids(
                    randomizer.GetRng("modifier/extra-placement/crate-template-instances", placement.SceneFile,
                        GetGeneratedItemGuid(placement), true),
                    newGuid);
        } else {
            template = randomizer.TemplateService
                .GetObject(WoodenCrateGameObjectName)
                .CloneWithNewGuids(
                    randomizer.GetRng("modifier/extra-placement/crate-template-instances", placement.SceneFile,
                        GetGeneratedItemGuid(placement), false),
                    newGuid);
            var drop = GetNextWoodenCrateDrop(randomizer.ItemRandomizer, rng, randomItemSettings);
            var itemDropDestruct = template.FindComponent<app.ItemDropDestruct>()!;
            itemDropDestruct.Enabled = true;
            itemDropDestruct.SetItemID = drop.Id;
            itemDropDestruct.ChangeStackNum = drop.CountNormal;
            itemDropDestruct.SaveGUID =
                itemDropDestruct.SaveGUID != Guid.Empty ? itemDropDestruct.SaveGUID : rng.NextGuid();
            template = template.AddOrUpdateComponent(itemDropDestruct);

            var name = _itemDefinitions.FromId(drop.Id)?.Name ?? drop.Id;
            logger.LogLine($"[EXTRA] Wooden crate drop: {drop.CountNormal}x {name}");
        }

        var transform = template.FindComponent<GeneratedViaTransform>()!;
        transform.Position = placement.Position;
        transform.Scale = Vector3.One;
        template = template.AddOrUpdateComponent(transform);

        parentGameObject = parentGameObject.AddOrUpdateChild(template);
        logger.LogLine(
            $"[EXTRA] {(isFake ? "FAKE " : "")}Wooden crate at {placement.Position} in {placement.SceneFile}");
        logger.LogLine($"GUID: {newGuid}");

        return parentGameObject;
    }

    private static Item GetNextWoodenCrateDrop(
        ItemRandomizer itemRandomizer,
        Rng rng,
        RandomItemSettings randomItemSettings) {
        if (!ItemDrops.GenericRuntimeDrops.Any(id => randomItemSettings.GetItemRatio(id) > 0)) {
            return new Item(ItemID.Herb.ToString(), 1);
        }

        return itemRandomizer.GetNextGeneralDrop(
            rng,
            new RandomItemSettings{
                MinAmmoQuantity = randomItemSettings.MinAmmoQuantity,
                MaxAmmoQuantity = randomItemSettings.MaxAmmoQuantity,
                ItemRatioKeyFunc = id => ItemDrops.GenericRuntimeDrops.Contains(id)
                    ? randomItemSettings.GetItemRatio(id)
                    : 0.0,
                ValidateFunc = randomItemSettings.ValidateFunc,
            });
    }

    private RszGameObject AddExtraItemBox(
        RszGameObject parentGameObject,
        Randomizer randomizer,
        RandomizerLogger logger,
        ItemPlacement placement,
        Rng rng) {
        var newGuid = rng.NextGuid();
        var template = randomizer.TemplateService
            .GetObject(ItemBoxGameObjectName)
            .CloneWithNewGuids(
                randomizer.GetRng("modifier/extra-placement/item-box-template-instances", placement.SceneFile,
                    GetGeneratedItemGuid(placement)),
                newGuid);
        var interactGameObject =
            template.Children.FirstOrDefault(child => child.FindComponent<app.InteractSendFsm>() != null);
        if (interactGameObject != null) {
            var interact = interactGameObject.FindComponent<app.InteractSendFsm>()!;
            interact.SaveGUID = rng.NextGuid();
            interactGameObject = interactGameObject.AddOrUpdateComponent(interact);
            template = template.AddOrUpdateChild(interactGameObject);
        }

        var transform = template.FindComponent<GeneratedViaTransform>()!;
        transform.Position = placement.Position;
        transform.Rotation = placement.Rotation;
        transform.Scale = Vector3.One;
        template = template.AddOrUpdateComponent(transform);

        parentGameObject = parentGameObject.AddOrUpdateChild(template);
        logger.LogLine($"[EXTRA] Item box at {placement.Position} in {placement.SceneFile}");
        logger.LogLine($"GUID: {newGuid}");

        return parentGameObject;
    }

    private RszGameObject AddExtraItem(
        RszGameObject parentGameObject,
        Randomizer randomizer,
        RandomizerLogger logger,
        ItemPlacement placement,
        Rng rng,
        bool isRandom,
        RandomItemSettings randomItemSettings) {
        RszGameObject template;
        Item drop;
        app.Item item;
        Guid newGuid;

        if (isRandom) {
            var preferHealing = randomizer.GetConfigOption<bool>("additional-items-prefer-healing");

            if (preferHealing && rng.NextProbability(PreferredHealingDropProbability)) {
                var heal = randomizer.ItemRandomizer.GetRandomItemDefinition(rng, Enums.app.Item.ItemCategoryType.Drug,
                    true);
                drop = new Item(heal?.Id ?? ItemID.Herb.ToString(), 1);
            } else {
                drop = randomizer.ItemRandomizer.GetNextGeneralDrop(rng, randomItemSettings);
            }

            var templateItemId = randomizer.ItemRandomizer.GetItemTemplateIdForDrop(drop.Id, rng, randomItemSettings);
            newGuid = GetGeneratedItemGuid(placement);
            var templateInstanceRng = randomizer.GetRng(
                "modifier/extra-placement/template-instances",
                placement.SceneFile,
                GetGeneratedItemGuid(placement),
                templateItemId);
            template = randomizer.TemplateService
                .GetItemTemplate(templateItemId)
                .CloneWithNewGuids(templateInstanceRng, newGuid);
            item = template.FindComponent<app.Item>()!;

            item.ItemDataID = drop.Id;
            item.ItemStackNum = drop.CountNormal;
            item._IsOverwriteDifficultItemNumSetting = true;
            item._DifficultItemNumSetting.EasyNum = drop.CountEasy;
            item._DifficultItemNumSetting.HardNum = drop.CountMadhouse;

            var name = _itemDefinitions.FromId(drop.Id)!.Name;
            logger.LogLine($"[RANDOM EXTRA] [{drop.CountEasy}, {drop.CountNormal}, {drop.CountMadhouse}]x {name} " +
                           $"at {placement.Position} in {placement.SceneFile}");
        } else {
            var templateItemId =
                randomizer.ItemRandomizer.GetItemTemplateIdForDrop(placement.Id, rng, randomItemSettings);
            newGuid = GetGeneratedItemGuid(placement);
            var templateInstanceRng = randomizer.GetRng(
                "modifier/extra-placement/template-instances",
                placement.SceneFile,
                GetGeneratedItemGuid(placement),
                templateItemId);
            template = randomizer.TemplateService
                .GetItemTemplate(templateItemId)
                .CloneWithNewGuids(templateInstanceRng, newGuid);
            item = template.FindComponent<app.Item>()!;

            item.ItemDataID = placement.Id;
            item.ItemStackNum = placement.StackNum;
            item._IsOverwriteDifficultItemNumSetting = true;
            item._DifficultItemNumSetting.EasyNum = placement.EasyNum;
            item._DifficultItemNumSetting.HardNum = placement.HardNum;

            var name = _itemDefinitions.FromId(placement.Id)!.Name;
            logger.LogLine(
                $"[EXTRA] [{placement.EasyNum}, {placement.StackNum}, {placement.HardNum}]x {name} at {placement.Position} in {placement.SceneFile}");
        }

        logger.LogLine($"GUID: {newGuid}");

        item.SaveGUID = placement.SaveGuid != Guid.Empty ? placement.SaveGuid : rng.NextGuid();
        item.RoomId = 0;
        item.Enabled = true;
        template = template.AddOrUpdateComponent(item);

        var transform = template.FindComponent<GeneratedViaTransform>()!;
        transform.Position = placement.Position;
        transform.Rotation = placement.Rotation;
        template = template.AddOrUpdateComponent(transform);
        template = template
            .PreparePickupInteractionsForPlacement()
            .PrepareWeaponPickupInteractionGameObjects();

        parentGameObject = parentGameObject.AddOrUpdateChild(template);
        return parentGameObject;
    }

    private static ExtraPlacementKind GetPlacementKind(ItemPlacement placement) {
        var specialKinds = new List<ExtraPlacementKind>(3);
        if (placement.Tags.Contains(WoodenCrateTag)) {
            specialKinds.Add(ExtraPlacementKind.WoodenCrate);
        }

        if (placement.Tags.Contains(WeaponChestTag)) {
            specialKinds.Add(ExtraPlacementKind.WeaponChest);
        }

        if (placement.Tags.Contains(ItemBoxTag)) {
            specialKinds.Add(ExtraPlacementKind.ItemBox);
        }

        return specialKinds.Count switch{
            0 => ExtraPlacementKind.Item,
            1 => specialKinds[0],
            _ => throw new Exception(
                $"Extra placement at {placement.Position} in {placement.SceneFile} has conflicting special tags: {string.Join(", ", placement.Tags)}")
        };
    }

    private static bool IsPlacementEnabled(
        ExtraPlacementKind kind,
        bool allowExtraItems,
        bool allowExtraCrates)
        => kind switch{
            ExtraPlacementKind.ItemBox => true,
            ExtraPlacementKind.WoodenCrate => allowExtraCrates,
            ExtraPlacementKind.WeaponChest => allowExtraItems,
            _ => allowExtraItems
        };

    private (RszScene Scene, RszGameObject ParentGameObject, bool ParentChanged) ApplyPlacementToScene(
        RszScene scene,
        RszGameObject parentGameObject,
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        ItemPlacement placement,
        RandomItemSettings randomItemSettings,
        bool allowExtraItems,
        bool allowExtraCrates) {
        var kind = GetPlacementKind(placement);
        if (!IsPlacementEnabled(kind, allowExtraItems, allowExtraCrates))
            return (scene, parentGameObject, false);

        if (kind != ExtraPlacementKind.Item &&
            !ScriptedSceneSafety.AllowsCollisionBearingExtra(placement.SceneFile)) {
            logger.LogLine(
                $"[SKIP EXTRA] {kind} at {placement.Position} in {placement.SceneFile}: scripted flashback scene.");
            return (scene, parentGameObject, false);
        }

        return kind switch{
            ExtraPlacementKind.WoodenCrate => (scene,
                AddExtraCrate(parentGameObject, randomizer, logger, placement, rng, randomItemSettings), true),
            ExtraPlacementKind.WeaponChest => (AddExtraChest(scene, randomizer, logger, placement), parentGameObject,
                false),
            ExtraPlacementKind.ItemBox => (scene, AddExtraItemBox(parentGameObject, randomizer, logger, placement, rng),
                true),
            _ => AddPlacementItem(scene, parentGameObject, randomizer, logger, placement, rng, randomItemSettings)
        };
    }

    private (RszScene Scene, RszGameObject ParentGameObject, bool ParentChanged) AddPlacementItem(
        RszScene scene,
        RszGameObject parentGameObject,
        Randomizer randomizer,
        RandomizerLogger logger,
        ItemPlacement placement,
        Rng rng,
        RandomItemSettings randomItemSettings) {
        var isRandom = placement.Tags.Contains(RandomItemTag);
        var hasFixedItem = !string.IsNullOrWhiteSpace(placement.Id);

        if (!isRandom && !hasFixedItem) {
            logger.LogLine(
                $"[SKIP EXTRA] Placement at {placement.Position} in {placement.SceneFile} has no item id and is not marked random.");
            return (scene, parentGameObject, false);
        }

        return (scene, AddExtraItem(parentGameObject, randomizer, logger, placement, rng, isRandom, randomItemSettings),
            true);
    }

    private void HandleExtraItemsForScene(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng rng,
        List<ItemPlacement> placements,
        RandomItemSettings randomItemSettings,
        bool allowExtraItems,
        bool allowExtraCrates) {
        if (placements.Count == 0)
            return;

        randomizer.FileRepository.ModifyScnFile(placements[0].SceneFile, scene => {
            var parentGameObject = scene.FindGameObject(go => go.Name.EndsWith("_dynamic"))
                                   ?? throw new Exception("Failed to obtain \"_dynamic\" parent GameObject!");
            var parentChanged = false;

            foreach (var placement in placements) {
                var result = ApplyPlacementToScene(
                    scene,
                    parentGameObject,
                    randomizer,
                    logger,
                    rng,
                    placement,
                    randomItemSettings,
                    allowExtraItems,
                    allowExtraCrates);
                scene = result.Scene;
                parentGameObject = result.ParentGameObject;
                parentChanged |= result.ParentChanged;
            }

            if (parentChanged) {
                scene = scene.UpdateGameObject(parentGameObject);
            }

            return scene;
        });
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var itemPlacementService = randomizer.ItemPlacementService;
        var context = randomizer.StaticItemRandomizationService;
        var randomItemsEnabled = randomizer.GetConfigOption<bool>("random-items");
        var allowExtraItems = randomizer.GetConfigOption<bool>("additional-items");
        var allowExtraCrates = randomizer.GetConfigOption<bool>("additional-wooden-crates");
        var extraPlacements = itemPlacementService.ItemPlacements
            .Where(placement => placement.Enabled && placement.IsExtra && !string.IsNullOrEmpty(placement.SceneFile))
            .ToList();
        var hasAlwaysOnItemBoxes = extraPlacements.Any(placement => placement.Tags.Contains(ItemBoxTag));

        if (!allowExtraItems && !allowExtraCrates && !hasAlwaysOnItemBoxes)
            return;

        extraPlacements
            .GroupBy(placement => placement.SceneFile, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .ForEach(group => HandleExtraItemsForScene(
                randomizer,
                logger,
                context.Rng,
                group.ToList(),
                context.RandomItemSettings,
                allowExtraItems,
                allowExtraCrates));
    }
}
