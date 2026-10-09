using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Weapons;
using Enums.app.GameFlowFsmManager;
using Enums.app.Item;
using System.Globalization;
using RandomizerUserException = IntelOrca.Biohazard.BioRand.RandomizerUserException;

namespace Biohazard.BioRand.RE7.Services;

internal class ItemRandomizer {
    private static readonly HashSet<string> ExcludedGunDropIds =[
        "BlueBlaster",
        "HyperBlaster",
        "RedBlaster",
    ];

    private readonly Randomizer _randomizer;
    private readonly ItemDefinitionRepository _itemDefinitions = ItemDefinitionRepository.Default;
    private readonly HashSet<string> _placedItemIds = [];
    private readonly bool _allowUnlockables;
    private readonly bool _allowDlcItems;
    private readonly Dictionary<ItemCategoryType, ItemDefinition[]> _allowedItemsByCategory;
    private readonly ItemDefinition[] _allowedGuns;
    private readonly Dictionary<RandomItemSettings, EndlessBag<string>> _generalDrops = new();

    public string[] PlacedItemIds => _placedItemIds.ToArray();

    public ItemDefinition[] PlacedItems => _placedItemIds
        .Select(x => ItemDefinitionRepository.Default.FromId(x)!)
        .ToArray();

    public readonly Dictionary<GameFlowKindEnum, List<string>> ChapterAmmoMap = new(){
        {
            GameFlowKindEnum.C03_1_Main, [
                "HandgunBullet", "HandgunBulletL"
            ]
        },{
            GameFlowKindEnum.C03_2_Main, [
                "HandgunBullet", "HandgunBulletL", "ShotgunBullet"
            ]
        },{
            GameFlowKindEnum.C03_3_Main, [
                "HandgunBullet", "HandgunBulletL", "ShotgunBullet",
                "MagnumBullet",
                "AcidBulletS", "FlameBulletS"
            ]
        },{
            GameFlowKindEnum.C03_4_Main, [
                "HandgunBullet", "HandgunBulletL", "ShotgunBullet",
                "MagnumBullet",
                "AcidBulletS", "FlameBulletS",
                "BurnerBullet"
            ]
        },{
            GameFlowKindEnum.C03_5_Main, [
                "HandgunBullet", "HandgunBulletL", "ShotgunBullet",
                "MagnumBullet",
                "AcidBulletS", "FlameBulletS",
                "BurnerBullet"
            ]
        },
        { GameFlowKindEnum.C04_1_Main, ["MachineGunBullet"] },
        { GameFlowKindEnum.C04_2_Main, ["MachineGunBullet"] },
        { GameFlowKindEnum.C04_3_Main, ["MachineGunBullet"] },
    };

    private const int ItemStackCeiling = 150;
    private const double EasyAmmoDropAmountFactor = 1.5f;
    private const double NormalAmmoDropAmountFactor = 1f;
    private const double MadhouseAmmoDropAmountFactor = 0.75f;

    public ItemRandomizer(Randomizer randomizer) {
        _randomizer = randomizer;
        _allowUnlockables = randomizer.GetConfigOption<bool>("allow-bonus-items");
        _allowDlcItems = randomizer.GetConfigOption<bool>("allow-dlc-items");
        _allowedItemsByCategory = _itemDefinitions.Kinds
            .ToDictionary(
                kind => kind,
                kind => _itemDefinitions.GetAll(kind)
                    .Where(IsItemAllowed)
                    .ToArray());
        _allowedGuns = _allowedItemsByCategory.GetValueOrDefault(ItemCategoryType.Weapon, [])
            .Where(IsGunCandidate)
            .ToArray();
    }

    // (easy #, normal #, madhouse #)
    public (uint, uint, uint) ApplyDifficultyToDropAmount(uint amount)
        => (
            (uint)Math.Max(1, Math.Round(amount * EasyAmmoDropAmountFactor)),
            (uint)Math.Max(1, Math.Round(amount * NormalAmmoDropAmountFactor)),
            (uint)Math.Max(1, Math.Round(amount * MadhouseAmmoDropAmountFactor))
        );

    // (easy #, normal #, madhouse #)
    public (uint, uint, uint) DetermineDropAmount(string id, double min, double max, Rng rng) {
        var item = _itemDefinitions.FromId(id)!;
        var respectDifficulty = _randomizer.GetConfigOption<bool>("item-drop-respect-difficulty");

        if (item.CategoryType == ItemCategoryType.Shell) {
            var stack = Math.Min(item.MaxStack, ItemStackCeiling); // Avoid overly generous drops
            var minAmount = Math.Max(1, (int)Math.Round(min * stack));
            var maxAmount = Math.Min(stack, (int)Math.Round(max * stack));
            var result = (uint)rng.Next(minAmount, maxAmount + 1);
            return respectDifficulty ? ApplyDifficultyToDropAmount(result) : (result, result, result);
        } else {
            return (1u, 1u, 1u);
        }
    }

    public ItemDefinition? GetRandomGun(Rng rng, bool allowReoccurance = true)
        => GetRandomItemFromPool(rng, _allowedGuns, allowReoccurance);

    public ItemDefinition? GetRandomItemDefinition(Rng rng, ItemCategoryType kind, bool allowReoccurance = true,
        Func<ItemDefinition, bool>? extraCheck = null)
        => GetRandomItemFromPool(rng, _allowedItemsByCategory.GetValueOrDefault(kind, []), allowReoccurance,
            extraCheck);

    public bool IsItemAllowed(ItemDefinition itemDefinition) {
        if (itemDefinition.IsStoryProgressionItem)
            return false;
        if (itemDefinition.IsUnlockable)
            return _allowUnlockables;
        if (itemDefinition.Dlc != null)
            return _allowDlcItems;

        return true;
    }

    public Item GetNextGeneralDrop(Rng rng, RandomItemSettings settings) {
        var bag = CreateGeneralItemPool(settings, rng);

        var id = bag.Next();
        for (var i = 0; i < 1000; i++) {
            if (settings.ValidateFunc?.Invoke(id) != false) {
                break;
            }

            id = bag.Next();
        }

        var (easyAmount, normalAmount, madhouseAmount) =
            DetermineDropAmount(id, settings.MinAmmoQuantity, settings.MaxAmmoQuantity, rng);
        return new Item(){
            Id = id,
            CountEasy = (int)easyAmount,
            CountNormal = (int)normalAmount,
            CountMadhouse = (int)madhouseAmount,
        };
    }

    public EndlessBag<string> CreateGeneralItemPool(RandomItemSettings settings, Rng rng) {
        if (!_generalDrops.TryGetValue(settings, out var result)) {
            var weights = new Dictionary<string, decimal>();
            foreach (var dropKind in ItemDrops.GenericDrops) {
                var ratio = settings.GetItemRatio(dropKind);
                // Profiles can be submitted without using the slider UI. Bound weights before
                // expanding the bag: a large ratio and a small fractional ratio otherwise
                // allocate billions of entries and can terminate the public generation agent.
                if (!double.IsFinite(ratio) || ratio < 0 || ratio > 1) {
                    throw new RandomizerUserException(
                        $"Item drop ratio for '{dropKind}' must be a finite number between 0 and 1.");
                }

                var weight = ConvertToWeight(ratio);
                if (weight > 0) {
                    weights.Add(dropKind, weight);
                }
            }

            if (weights.Count == 0)
                return new EndlessBag<string>(rng, ["EthanLeg"]);

            var scale = (decimal)Math.Pow(10, weights.Values.Max(GetDecimalPlaces));
            var entries = weights
                .Select(kvp => (Id: kvp.Key,
                    Count: Math.Max(1, (int)decimal.Round(kvp.Value * scale, 0, MidpointRounding.AwayFromZero))))
                .ToList();
            var divisor = entries
                .Select(x => x.Count)
                .Aggregate(GetGreatestCommonDivisor);

            var pool = new List<string>();
            foreach (var (id, count) in entries) {
                var repeats = count / divisor;
                for (var i = 0; i < repeats; i++) {
                    pool.Add(id);
                }
            }

            result = new EndlessBag<string>(rng, pool);
            _generalDrops[settings] = result;
        }

        return result;
    }

    public string GetItemTemplateIdForDrop(string itemDataId, Rng rng, RandomItemSettings settings) {
        return itemDataId;
    }

    public IReadOnlyList<ValuableDrop> GetValuableDrops(Rng rng, string configPrefix) {
        var result = new List<ValuableDrop>();

        foreach (var type in ItemDrops.HighValueDrops) {
            if (!_randomizer.GetConfigOption<bool>($"{configPrefix}-valuable-{type}")) {
                continue;
            }

            switch (type) {
                case ItemDrops.Weapon:
                    var weapon = GetRandomGun(rng, allowReoccurance: false);
                    if (weapon != null) {
                        result.Add(CreateValuableDrop(weapon.Id, type));
                    }

                    break;
                case ItemDrops.DlcCoin:
                    foreach (var (coinId, minDropRate, maxDropRate) in ItemDrops.DlcCoinDrops) {
                        result.Add(CreateValuableDrop(
                            coinId,
                            type,
                            (uint)rng.Next((int)minDropRate, (int)maxDropRate + 1)));
                    }

                    break;
                case ItemDrops.BirthdaySkill:
                    if (_allowDlcItems) {
                        var skillId = rng.Next(ItemDrops.BirthdaySkillIds);
                        BirthdaySkillVisuals.CopyRequiredFiles(_randomizer.FileRepository, skillId);
                        result.Add(CreateValuableDrop(skillId, type));
                    }

                    break;
                default:
                    var itemId = ItemDrops.ToItemID(type);
                    if (itemId != Enums.app.ItemID.NoName.ToString()) {
                        result.Add(CreateValuableDrop(itemId, type));
                    }

                    break;
            }
        }

        return result;
    }

    private static ValuableDrop CreateValuableDrop(
        string id,
        string highValueDrop,
        uint? dropRate = null) {
        var count = ItemDrops.GetValuableDropCount(highValueDrop);
        return new ValuableDrop(
            new Item(id, count),
            dropRate ?? ItemDrops.GetValuableDropRate(highValueDrop));
    }

    private Item? GetRandomSingleItem(Rng rng, ItemCategoryType kind, bool allowReoccurance = false) {
        ItemDefinition? itemDefinition = kind switch{
            ItemCategoryType.Weapon => GetRandomGun(rng, allowReoccurance),
            _ => GetRandomItemDefinition(rng, kind, allowReoccurance),
        };
        if (itemDefinition != null)
            return new Item(itemDefinition.Id, 1);
        return null;
    }

    public Item? GetRandomAmmo(string? itemId, Rng rng, RandomItemSettings settings) {
        var itemDef = itemId == null
            ? GetRandomItemDefinition(rng, ItemCategoryType.Shell)
            : ItemDefinitionRepository.Default.FromId(itemId);
        if (itemDef == null)
            return null;

        var min = settings.MinAmmoQuantity;
        var max = settings.MaxAmmoQuantity;
        var minAmount = Math.Max(1, (int)Math.Round(min * itemDef.MaxStack));
        var maxAmount = Math.Min(itemDef.MaxStack, (int)Math.Round(max * itemDef.MaxStack));
        var amount = rng.Next(minAmount, maxAmount + 1);
        return new Item(itemDef.Id, amount);
    }

    public void MarkItemPlaced(string id) => _placedItemIds.Add(id);

    public bool IsItemPlaced(string id) => _placedItemIds.Contains(id);

    private static bool IsGunCandidate(ItemDefinition item) {
        if (item.WeaponId == null)
            return false;

        if (ExcludedGunDropIds.Contains(item.Id) ||
            ExcludedGunDropIds.Contains(item.WeaponId.Value.ToString())) {
            return false;
        }

        if (WeaponDefinitionRepository.Default.IsRestricted(item.WeaponId.Value))
            return false;

        var definition = WeaponDefinitionRepository.Default.FromWeaponId(item.WeaponId.Value.ToString());
        return definition.UserType == Enums.app.CharacterDefine.Type.Player
               && definition.IsGun;
    }

    private ItemDefinition? GetRandomItemFromPool(
        Rng rng,
        IReadOnlyList<ItemDefinition> pool,
        bool allowReoccurance,
        Func<ItemDefinition, bool>? extraCheck = null) {
        if (pool.Count == 0)
            return null;

        if (allowReoccurance && extraCheck == null) {
            var chosen = pool[rng.Next(0, pool.Count)];
            _placedItemIds.Add(chosen.Id);
            return chosen;
        }

        var availableCount = 0;
        foreach (var item in pool) {
            if ((allowReoccurance || !_placedItemIds.Contains(item.Id))
                && (extraCheck?.Invoke(item) != false)) {
                availableCount++;
            }
        }

        if (availableCount == 0)
            return null;

        var index = rng.Next(0, availableCount);
        foreach (var item in pool) {
            if ((allowReoccurance || !_placedItemIds.Contains(item.Id))
                && (extraCheck?.Invoke(item) != false)
                && index-- == 0) {
                _placedItemIds.Add(item.Id);
                return item;
            }
        }

        return null;
    }

    private static decimal ConvertToWeight(double ratio)
        => decimal.Parse(ratio.ToString("0.####", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static int GetDecimalPlaces(decimal value)
        => (decimal.GetBits(value)[3] >> 16) & 0xFF;

    private static int GetGreatestCommonDivisor(int left, int right) {
        while (right != 0) {
            (left, right) = (right, left % right);
        }

        return Math.Abs(left);
    }
}

internal readonly record struct ValuableDrop(Item Item, uint DropRate);

public class RandomItemSettings {
    public double MinAmmoQuantity { get; set; }
    public double MaxAmmoQuantity { get; set; }
    public Func<string, double>? ItemRatioKeyFunc { get; set; }
    public Func<string, bool>? ValidateFunc { get; set; }

    public double GetItemRatio(string id) {
        return ItemRatioKeyFunc?.Invoke(id) ?? 0;
    }
}
