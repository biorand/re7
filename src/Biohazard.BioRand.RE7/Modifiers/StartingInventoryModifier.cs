using app;
using Biohazard.BioRand.RE7.Inventory;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Serialization;
using Enums.app;
using IntelOrca.Biohazard.BioRand.REE;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;

namespace Biohazard.BioRand.RE7.Modifiers;

internal class StartingInventoryModifier : Modifier {
    private readonly Randomizer _randomizer;

    public StartingInventoryModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/inventory";
    internal const string MainHouseStartScene = "natives/stm/leveldesign/fsm/chapter3/chapter3_1/levelfsm_c03_1.scn.20";
    internal const string MainHouseStartObject = "0_Ch1Item_ItemBoxIn_FFS";
    private const int AntiqueCoinsProbabilityPct = 1;
    private const int AntiqueCoinsCount = 2;

    private static readonly (ItemID ItemId, int Count)[] StarterHealing =[
        (ItemID.RemedyM, 1),
        (ItemID.RemedyL, 1),
    ];

    private static readonly Dictionary<WeaponID, (ItemID ItemId, int Count)[]> StarterAmmoLoadouts = new(){
        [WeaponID.Handgun] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.Handgun_M19] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.Handgun_G17] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.Handgun_MPM] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.Handgun_Albert] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.Handgun_Albert_Reward] =[(ItemID.HandgunBullet, 20), (ItemID.HandgunBulletL, 20)],
        [WeaponID.ShotGun] =[(ItemID.ShotgunBullet, 15)],
        [WeaponID.Shotgun_M37] =[(ItemID.ShotgunBullet, 15)],
        [WeaponID.Shotgun_M37S] =[(ItemID.ShotgunBullet, 15)],
        [WeaponID.Shotgun_DB] =[(ItemID.ShotgunBullet, 15)],
        [WeaponID.MachineGun] =[(ItemID.MachineGunBullet, 150)],
        [WeaponID.Magnum] =[(ItemID.MagnumBullet, 10)],
        [WeaponID.GrenadeLauncher] =[(ItemID.FlameBulletS, 3), (ItemID.AcidBulletS, 3)],
        [WeaponID.Burner] =[(ItemID.BurnerBullet, 150)],
    };

    private static readonly string[] StartingSkillLevelOneIds =[
        "skl009", // Defense I
        "skl011", // Speed Up I
        "skl013", // Firepower Up I
        "skl015", // Impact I
        "skl017", // Toughness I
        "skl018", // Guard Up
        "skl019", // Quick Reload
        "skl021", // Vengeance
        "skl022", // Narrow Escape
    ];

    private readonly Dictionary<MainCampaignCharacter, string> _paths = new(){
        { MainCampaignCharacter.Ethan, "leveldesign/fsm/chapter1/other/ch1_startinventory.user".UserFile() },{
            MainCampaignCharacter.ClancyVHS, "leveldesign/fsm/ff000/other/startinventory_ff000.user".UserFile()
        }, // "Derelict House Footage" (Guest House)
        {
            MainCampaignCharacter.Mia,
            "leveldesign/fsm/chapter4/chapter4_1/other/4-1startinventory.user".UserFile()
        },{
            MainCampaignCharacter.MiaVHS, "leveldesign/fsm/ff050/other/ff050_startinventory.user".UserFile()
        }, // Old Videotape (Ship)
    };

    private static readonly ItemDefinitionRepository itemDefinitions = ItemDefinitionRepository.Default;

    private List<StartingInventoryItem> GetInventory(Randomizer randomizer, MainCampaignCharacter character)
        => randomizer.FileRepository.DeserializeUserFile<app.AddItemListData>(_paths[character])._AddItems;

    private record DebugStartItem {
        public string ItemId { get; init; } = "";
        public int Quantity { get; init; }
        public string Comment { get; init; } = "";
        public DebugStartItem() { }
    }

    private static void LogVanillaInventory(RandomizerLogger logger, MainCampaignCharacter character,
        List<StartingInventoryItem> items) {
        logger.Push($"{character}'s starting inventory");

        foreach (var item in items)
            logger.LogLine(itemDefinitions.FromId(item.ItemDataID)!.Name!);

        logger.Pop();
    }

    public override void LogState(RandomizerLogger logger) {
        var randomizer = _randomizer;
        foreach (var character in Enum.GetValues<MainCampaignCharacter>()) {
            LogVanillaInventory(logger, character, GetInventory(randomizer, character));
        }
    }

    // Returns a random gun and bladed weapon
    private (ItemID?, ItemID?) PickRandomWeaponPair(Rng rng, List<StartingWeaponCategory> weapons) {
        if (weapons.Count == 0)
            return (null, null);

        ItemID? primaryWeapon = null;
        ItemID? secondaryWeapon = null;

        var allowedWeapons = weapons.ToDictionary(
            category => category,
            category => category.GetItemIds()
        );

        var primaryCandidates = allowedWeapons.Keys
            .Where(cat => cat != StartingWeaponCategory.Bladed)
            .ToList();

        if (primaryCandidates.Count > 0) {
            var primaryCategory = rng.Next(primaryCandidates);
            primaryWeapon = rng.Next(allowedWeapons[primaryCategory]);
        }

        if (allowedWeapons.TryGetValue(StartingWeaponCategory.Bladed, out var bladedItems)) {
            secondaryWeapon = rng.Next(bladedItems);
        }

        return (primaryWeapon, secondaryWeapon);
    }

    private static ItemID? PickAdditionalGun(
        Rng rng,
        List<StartingWeaponCategory> weapons,
        List<StartingInventoryItem> inventory
    ) {
        var allowedGuns = weapons
            .Where(category => category is StartingWeaponCategory.Handgun or StartingWeaponCategory.MachineGun
                or StartingWeaponCategory.Shotgun or StartingWeaponCategory.Burner
                or StartingWeaponCategory.Magnum or StartingWeaponCategory.GrenadeLauncher)
            .ToDictionary(category => category, category => category.GetItemIds());
        if (allowedGuns.Count == 0)
            return null;

        var existingItems = inventory.Select(item => item.ItemDataID).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unusedGuns = allowedGuns
            .ToDictionary(pair => pair.Key,
                pair => pair.Value.Where(id => !existingItems.Contains(id.ToString())).ToList())
            .Where(pair => pair.Value.Count > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var candidates = unusedGuns.Count > 0 ? unusedGuns : allowedGuns;
        var category = rng.Next(candidates.Keys.Order().ToList());
        return rng.Next(candidates[category]);
    }

    private void RandomizeStartingInventory(
        Randomizer randomizer,
        RandomizerLogger logger,
        Rng inventoryRng,
        Rng skillRng,
        Rng additionalGunRng,
        MainCampaignCharacter character,
        bool randomizeInventory,
        bool giveRandomSkill,
        bool giveAdditionalGun,
        List<StartingWeaponCategory> weapons,
        IReadOnlyList<StartingInventoryItem> debugItems
    ) {
        var giveAmmo = randomizer.GetConfigOption<bool>("random-starting-inventory-give-ammo");
        var (primary, secondary) = randomizeInventory
            ? PickRandomWeaponPair(inventoryRng, weapons)
            : ((ItemID?)null, (ItemID?)null);
        var path = _paths[character];
        logger.Push($"{character} @ {path}");
        randomizer.FileRepository.ModifyUserFile<AddItemListData>(path, root => {
            if (randomizeInventory && primary != null) {
                logger.LogLine($"Primary weapon: {primary}");
                AddWeapon(root._AddItems, primary.Value, giveAmmo, logger);
            }

            if (randomizeInventory && secondary != null) {
                logger.LogLine($"Secondary weapon: {secondary}");
                root._AddItems.Add(
                    new StartingInventoryItem(){ ItemDataID = secondary.Value.ToString()!, Num = 1 }
                );
            }

            if (randomizeInventory && inventoryRng.NextProbability(AntiqueCoinsProbabilityPct)) {
                logger.LogLine($"Nice! {AntiqueCoinsCount}x extra antique coin(s)!");
                root._AddItems.Add(new StartingInventoryItem(){ ItemDataID = "Coin", Num = AntiqueCoinsCount });
            }

            if (randomizeInventory) {
                foreach (var (itemId, count) in StarterHealing) {
                    EnsureMinimumItem(root._AddItems, itemId, count, logger);
                }
            }

            if (randomizeInventory && randomizer.UserTags.Contains("re7:debugstartitems")) {
                if (debugItems.Count > 0) {
                    logger.LogLine(
                        $"Adding debug items: {string.Join(", ", debugItems.Select(x => $"{x.Num}x {x.ItemDataID}"))}");
                    root._AddItems.AddRange(debugItems.Select(CloneInventoryItem));
                }
            }

            if (giveAdditionalGun) {
                var additionalGun = PickAdditionalGun(additionalGunRng, weapons, root._AddItems);
                if (additionalGun != null) {
                    logger.LogLine($"Additional gun: {additionalGun}");
                    AddWeapon(root._AddItems, additionalGun.Value, giveAmmo, logger);
                } else {
                    logger.LogLine("Additional gun skipped: no gun categories are allowed.");
                }
            }

            if (giveRandomSkill) {
                var skillId = skillRng.Next(StartingSkillLevelOneIds);
                logger.LogLine($"Random starting skill: {skillId}");
                BirthdaySkillVisuals.CopyRequiredFiles(randomizer.FileRepository, skillId);
                root._AddItems.Add(new StartingInventoryItem{
                    ItemDataID = skillId,
                    Num = 1,
                });
            }

            return root;
        });
        logger.Pop();
    }

    private static void AddWeapon(
        List<StartingInventoryItem> items,
        ItemID weapon,
        bool giveAmmo,
        RandomizerLogger logger
    ) {
        var id = weapon.ToString();
        items.Add(new StartingInventoryItem(){ ItemDataID = id, Num = 1 });

        if (giveAmmo && Enum.TryParse(id, out WeaponID wpId) &&
            StarterAmmoLoadouts.TryGetValue(wpId, out var ammoLoadout)) {
            foreach (var (ammoType, ammoCount) in ammoLoadout) {
                logger.LogLine($"Extra ammo: {ammoCount}x {ammoType}");
                items.Add(new StartingInventoryItem(){
                    ItemDataID = ammoType.ToString(),
                    Num = ammoCount,
                });
            }
        }
    }

    private static void EnsureMinimumItem(
        List<StartingInventoryItem> items,
        ItemID itemId,
        int minimumCount,
        RandomizerLogger logger
    ) {
        var id = itemId.ToString();
        var existingCount = items
            .Where(item => string.Equals(item.ItemDataID, id, StringComparison.OrdinalIgnoreCase))
            .Sum(item => item.Num);
        var countToAdd = minimumCount - existingCount;
        if (countToAdd <= 0)
            return;

        logger.LogLine($"Starter item: {countToAdd}x {itemId}");
        items.Add(new StartingInventoryItem(){
            ItemDataID = id,
            Num = countToAdd,
        });
    }

    private static StartingInventoryItem CloneInventoryItem(StartingInventoryItem item) {
        return new StartingInventoryItem(){
            ItemDataID = item.ItemDataID,
            Num = item.Num,
        };
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var randomizeEthansInventory = randomizer.GetConfigOption<bool>("random-starting-inventory-ethan");
        var randomizeMiasInventory = randomizer.GetConfigOption<bool>("random-starting-inventory-mia");
        var giveAdditionalGunEthan = randomizer.GetConfigOption<bool>("random-starting-inventory-additional-gun-ethan");
        var giveAdditionalGunMia = randomizer.GetConfigOption<bool>("random-starting-inventory-additional-gun-mia");
        var randomizeVhs = randomizer.GetConfigOption<bool>("random-starting-inventory-vhs");
        var giveRandomSkillEthan = randomizer.GetConfigOption<bool>("random-starting-inventory-skills-ethan");
        var giveRandomSkillMia = randomizer.GetConfigOption<bool>("random-starting-inventory-skills-mia");

        if (!randomizeEthansInventory && !randomizeMiasInventory && !giveRandomSkillEthan && !giveRandomSkillMia &&
            !giveAdditionalGunEthan && !giveAdditionalGunMia) {
            return;
        }

        var rng = randomizer.GetRng(RandomizerKey);
        var debugItems = randomizeEthansInventory || randomizeMiasInventory
            ? LoadDebugStartItems(randomizer)
            : [];

        // Starter weapons
        var categories = Enum.GetValues<StartingWeaponCategory>();
        foreach (var character in Enum.GetValues<MainCampaignCharacter>()) {
            var shouldRandomizeInventory = character switch{
                MainCampaignCharacter.Ethan => randomizeEthansInventory,
                MainCampaignCharacter.Mia => randomizeMiasInventory,
                MainCampaignCharacter.ClancyVHS => randomizeVhs && (randomizeEthansInventory || randomizeMiasInventory),
                MainCampaignCharacter.MiaVHS => randomizeMiasInventory && randomizeVhs,
                _ => false,
            };
            var shouldGiveRandomSkills = character switch{
                MainCampaignCharacter.Ethan => giveRandomSkillEthan,
                MainCampaignCharacter.Mia => giveRandomSkillMia,
                _ => false,
            };
            var shouldGiveAdditionalGun = character switch{
                MainCampaignCharacter.Ethan => giveAdditionalGunEthan,
                MainCampaignCharacter.Mia => giveAdditionalGunMia,
                MainCampaignCharacter.MiaVHS => giveAdditionalGunMia && randomizeVhs,
                _ => false,
            };

            if (!shouldRandomizeInventory && !shouldGiveRandomSkills && !shouldGiveAdditionalGun)
                continue;

            var configuredCategories = new List<StartingWeaponCategory>();
            if (shouldRandomizeInventory &&
                character is MainCampaignCharacter.ClancyVHS) // Allow all weapons for Clancy, it doesn't really matter
            {
                configuredCategories = Enum.GetValues<StartingWeaponCategory>().ToList();
            } else if (shouldRandomizeInventory || shouldGiveAdditionalGun) {
                foreach (var category in categories) {
                    var characterStr = character is MainCampaignCharacter.MiaVHS
                        ? "mia"
                        : character.ToString().ToLowerInvariant();
                    if (randomizer.GetConfigOption<bool>(
                            $"inventory-weapon-{category.ToString().ToLowerInvariant()}-{characterStr}")
                       ) {
                        configuredCategories.Add(category);
                    }
                }
            }


            RandomizeStartingInventory(
                randomizer,
                logger,
                rng,
                randomizer.GetRng(RandomizerKey, "skills", character),
                randomizer.GetRng(RandomizerKey, "additional-gun", character),
                character,
                shouldRandomizeInventory,
                shouldGiveRandomSkills,
                shouldGiveAdditionalGun,
                configuredCategories,
                debugItems);
        }

        if (randomizer.GetConfigOption<string>(ChapterJumpDataModifier.StartChapterConfigKey) == "Main House" &&
            (randomizeEthansInventory || giveRandomSkillEthan || giveAdditionalGunEthan)) {
            ApplyMainHouseInventory(randomizer);
            logger.LogLine("Main House start: initialize Ethan's configured loadout in the saved opening FSM.");
        }
    }

    private static void ApplyMainHouseInventory(Randomizer randomizer) {
        // This single-use, saved opening FSM normally transfers the previous chapter's inventory
        // into the item box. A Main House new game has no preceding chapter to provide that data.
        // Reuse the native Chapter 1 AddItem action, including its inventory userdata reference.
        var source = randomizer.FileRepository
            .GetScnFile("natives/stm/leveldesign/fsm/chapter1/levelfsm_c01.scn.20")
            .ReadScene(randomizer.FileRepository.TypeRepository)
            .FindGameObject(go => go.Name == "0_MiaPhoto_AddItem")!;
        RszObjectNode? addItem = null;
        source.Visit(node => {
            if (node is RszObjectNode obj && obj.Type.Name == "app.AddItem") addItem ??= obj;
        });
        if (addItem == null) throw new InvalidDataException("Missing starting inventory action.");

        var replaced = 0;
        randomizer.FileRepository.ModifyScnFile(MainHouseStartScene, scene => scene.VisitGameObjects(go => {
            if (go.Name != MainHouseStartObject) return go;
            return go.WithComponents(go.Components.Select(component => component.Visit(node => {
                if (node is not RszObjectNode obj || obj.Type.Name != "app.fsm.CopySaveItem2ItemBox") return node;
                replaced++;
                return addItem
                    .SetField("v0_Enabled", obj.Get<bool>("v0_Enabled"))
                    .SetField("v1_Modified", true)
                    .SetField("v2_UID", obj.Get<uint>("v2_UID"))
                    .SetField("v3_ListNo", obj.Get<byte>("v3_ListNo"));
            })).ToImmutableArray());
        }));
        if (replaced != 1) throw new InvalidDataException("Unexpected Main House inventory initialization layout.");
    }

    private static IReadOnlyList<StartingInventoryItem> LoadDebugStartItems(Randomizer randomizer) {
        if (!randomizer.UserTags.Contains("re7:debugstartitems"))
        {
            return [];
        }

        return Csv.Deserialize<DebugStartItem>(randomizer.DynamicData.GetData(DynamicDataName.DebugStartItems)!)
            .Where(x => x.Quantity > 0)
            .Select(x => new StartingInventoryItem(){ ItemDataID = x.ItemId, Num = x.Quantity })
            .ToArray();
    }
}
