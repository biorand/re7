using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.REEngine;
using Biohazard.BioRand.RE7.Patches;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Services;
using Enums.app;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.REE;
using ReplacementData =
    (string Id, int Quantity, int Coins, System.Collections.Immutable.ImmutableArray<string> ValidItemIDs);

namespace Biohazard.BioRand.RE7.Modifiers;

internal class BirdCageModifier : Modifier {
    private readonly Randomizer _randomizer;

    public BirdCageModifier(Randomizer randomizer) {
        _randomizer = randomizer;
    }

    private const string RandomizerKey = "modifier/bird-cage";

    private readonly List<string> _birdCageScnFiles =[
        "environment/scene/chapter3/c03_trailerhouse.scn", // Trailer
        "leveldesign/itemset/chapter3/mainhouse_hall/hard.scn", // Main Hall (Madhouse)
        "environment/scene/chapter4/c04_cottage.scn", // Swamp Cottage
        "leveldesign/itemset/chapter4/shipoutside/hard.scn", // Swamp Cottage (Madhouse) (?)
    ];

    // To avoid randomizing bird cage contents several times this global lookup is needed.
    // TODO: Use Order attribute instead
    public static readonly List<Guid> Guids =[
        new("87007bf8-48b7-052c-1065-2fcb385ee0a4"),
        new("7bd613f5-b8fb-01bc-0cac-80f3c19b60cc"),
        new("7ee83c9f-e776-0e37-0030-ef6c7b7928a0"),
        new("4fcc3365-45cb-0da5-3cd2-544fd3319b14"),
        new("f244e480-71ce-0179-3f29-f5aef5d572b7"),
        new("473357f4-4397-03f0-3666-e22b9480bcd7"),
        new("59cba3ca-5e50-48e8-b1ed-c3801bb964d3"),
        new("eb642b44-ea23-42c5-9308-eb8791401d0f"),
        new("eba4e638-5fb3-47ac-a561-5a8a87163ce4"),
        new("73297c81-9232-086a-2322-7f32bcbb0e68"),
        new("79dc7b86-d066-058b-3037-204aa7216c9b"),
    ];

    private static readonly Guid MadhouseScorpionKeyBirdCageGuid = new("c5f2b3fd-0732-468a-b8d6-017a8f1f20f2");

    private readonly Regex _birdCageRegex = new("^sm.*CoinBox((?!Interact).)*$", RegexOptions.Compiled);
    private static readonly ItemDefinitionRepository Items = ItemDefinitionRepository.Default;
    private static readonly string[] DefaultInsertItems = ["Coin", "CoinOld"];

    private record BirdCageReplacement {
        public bool Enabled { get; init; }
        public string ItemId { get; init; } = "";
        public int MinAmount { get; init; }
        public int MaxAmount { get; init; }
        public int CoinsMin { get; init; }
        public int CoinsMax { get; init; }
        public ImmutableArray<string> InputItemIds { get; init; }
        public string? Comment { get; init; }

        public BirdCageReplacement() { }
    }

    private sealed class BirdCageReplacementPicker {
        private readonly ImmutableList<BirdCageReplacement> _replacements;
        private readonly HashSet<string> _selectedItemIds = new(StringComparer.OrdinalIgnoreCase);

        public BirdCageReplacementPicker(ImmutableList<BirdCageReplacement> replacements) {
            _replacements = replacements;
        }

        public bool TryGetReplacement(Rng rng, ItemRandomizer itemRandomizer, out ReplacementData result) {
            if (_replacements.Count == 0) {
                result = default;
                return false;
            }

            var candidates = _replacements
                .Where(replacement => !_selectedItemIds.Contains(replacement.ItemId))
                .ToList();
            var weaponSafeCandidates = candidates
                .Where(replacement => !IsAlreadyPlacedWeapon(replacement, itemRandomizer))
                .ToList();

            if (weaponSafeCandidates.Count > 0) {
                candidates = weaponSafeCandidates;
            } else if (candidates.Count == 0) {
                candidates = _replacements
                    .Where(replacement => !IsAlreadyPlacedWeapon(replacement, itemRandomizer))
                    .ToList();
            }

            if (candidates.Count == 0) {
                candidates = _replacements.ToList();
            }

            var replacement = rng.Next(candidates);
            var itemId = replacement.ItemId;
            _selectedItemIds.Add(itemId);
            itemRandomizer.MarkItemPlaced(itemId);
            result = (replacement.ItemId, rng.NextInclusive(replacement.MinAmount, replacement.MaxAmount),
                rng.NextInclusive(replacement.CoinsMin, replacement.CoinsMax), replacement.InputItemIds);
            return true;
        }

        private static bool IsAlreadyPlacedWeapon(BirdCageReplacement replacement, ItemRandomizer itemRandomizer) {
            var itemId = replacement.ItemId;
            return Items.FromId(itemId)?.IsWeapon == true && itemRandomizer.IsItemPlaced(itemId);
        }
    }

    private bool RandomizeBirdCageContent(BirdCageReplacementPicker replacementPicker, Rng rng, BirdCage birdCage,
        ItemRandomizer itemRandomizer) {
        if (!replacementPicker.TryGetReplacement(rng, itemRandomizer, out var replacement))
            return false;

        var (Id, Quantity, Coins, ValidItemIDs) = replacement;
        birdCage.Item.ItemDataID = Id;
        birdCage.Item.ItemStackNum = Quantity;
        birdCage.Item.SaveGUID = rng.NextGuid(); // IMPORTANT!
        birdCage.CoinCounter.CoinMax = Coins;

        if (!ValidItemIDs.SequenceEqual(DefaultInsertItems)) {
            birdCage.ItemSelectReaction.ReactionSettings.Clear();

            foreach (var id in ValidItemIDs) {
                birdCage.ItemSelectReaction.ReactionSettings.Add(new app.ItemSelectReaction.ReactionSetting(){
                    ItemID = id,
                    StateName = "InsertCoin",
                    Result = Enums.app.ItemSelectReaction.Result.Success,
                });
            }

            birdCage.ItemSelectReaction.Enabled = true;
        }

        return true;
    }

    public override void Apply(RandomizerLogger logger) {
        var randomizer = _randomizer;
        var csv = randomizer.DynamicData.GetData(DynamicDataName.BirdCages) ??
                  throw new Exception("Unable to get bird cage data");
        var replacements = Csv.Deserialize<BirdCageReplacement>(csv)
            .Where(b => b.Enabled)
            .ToImmutableList();

        var randomizeMagnum = randomizer.GetConfigOption<bool>("random-bird-cage-magnum");
        var randomizeDrugsAndPowerCoins = randomizer.GetConfigOption<bool>("random-bird-cage-drugs-coins");
        var preserveItemModels = randomizer.GetConfigOption<bool>("preserve-item-models");
        var rng = randomizer.GetRng(RandomizerKey);
        var replacementPicker = new BirdCageReplacementPicker(replacements);
        foreach (var file in _birdCageScnFiles) {
            logger.Push(file);
            var path = file.SceneFile();
            var scnFile = randomizer.FileRepository.GetScnFile(path)
                .ToBuilder(randomizer.FileRepository.TypeRepository);
            var changedBirdCages = new List<BirdCage>();

            scnFile.Scene.VisitGameObjects(gameObject => {
                if (IsBirdCage(gameObject)) {
                    if (gameObject.Guid == MadhouseScorpionKeyBirdCageGuid)
                        return; // Don't randomize scorpion key on Madhouse

                    var birdCage = new BirdCage(randomizer, path, gameObject, preserveItemModels);

                    var isMagnum = birdCage.Item.ItemDataID == ItemID.Magnum.ToString();
                    if ((isMagnum && randomizeMagnum) || (!isMagnum && randomizeDrugsAndPowerCoins)) {
                        var (beforeItemCount, beforeItemId, beforeCoinCounter) =
                            (birdCage.Item.ItemStackNum, birdCage.Item.ItemDataID, birdCage.CoinCounter.CoinMax);
                        var beforeName = Items.FromId(beforeItemId)!.Name;

                        if (!RandomizeBirdCageContent(replacementPicker, rng, birdCage, randomizer.ItemRandomizer)) {
                            logger.LogLine(
                                $"Skipped {beforeItemCount}x {beforeName} bird cage reward because no enabled replacements are available.");
                            return;
                        }

                        changedBirdCages.Add(birdCage);
                        var afterName = Items.FromId(birdCage.Item.ItemDataID)!.Name;

                        logger.LogLine(
                            $"Replaced {beforeItemCount}x {beforeName} that cost {beforeCoinCounter} antique coins in bird cage with " +
                            $"{birdCage.Item.ItemStackNum}x {afterName} that costs {birdCage.CoinCounter.CoinMax} antique coins");
                    }
                }
            });

            if (changedBirdCages.Count > 0) {
                var scene = scnFile.Scene;
                foreach (var birdCage in changedBirdCages) {
                    scene = birdCage.ApplyToScene(scene, randomizer);
                }

                scnFile.Scene = scene;
                randomizer.FileRepository.SetScnFile(path, scnFile.AddMissingResources().Build());
            }

            logger.Pop();
        }
    }

    private bool IsBirdCage(RszGameObject gameObject) {
        return gameObject.Name.StartsWith("sm", StringComparison.OrdinalIgnoreCase)
               && gameObject.Name.Contains("CoinBox", StringComparison.OrdinalIgnoreCase)
               && !gameObject.Name.Contains("Interact", StringComparison.OrdinalIgnoreCase)
               && _birdCageRegex.IsMatch(gameObject.Name);
    }
}

internal class BirdCage {
    public Randomizer Randomizer { get; }
    public string PakPath { get; }
    public bool PreserveItemModels { get; }
    public Guid ContainerGuid { get; }

    public app.Item Item { get; }
    public app.ItemSelectReaction ItemSelectReaction { get; }
    public app.CoinCounter CoinCounter { get; }

    public BirdCage(Randomizer randomizer, string path, RszGameObject container, bool preserveItemModels) {
        Randomizer = randomizer;
        PakPath = path;
        PreserveItemModels = preserveItemModels;
        ContainerGuid = container.Guid;

        var gimmick = container.Children.First(child => child.Name.EndsWith("_Gimmick"));
        ItemSelectReaction = gimmick.FindComponent<app.ItemSelectReaction>()!;
        CoinCounter = gimmick.FindComponent<app.CoinCounter>()!;

        Item = container.Children
            .Single(child => child.FindComponent<app.Item>() != null)
            .FindComponent<app.Item>()!;
    }

    public RszScene ApplyToScene(RszScene scene, Randomizer randomizer) {
        var container = scene.FindGameObject(go => go.Guid == ContainerGuid)!;

        var gimmick = container.Children
            .First(child => child.Name.EndsWith("_Gimmick"));

        var itemHolder = container.Children
            .First(child => child.FindComponent<app.Item>() != null);

        var newGimmick = gimmick
            .AddOrUpdateComponent(ItemSelectReaction)
            .AddOrUpdateComponent(CoinCounter);

        var newItemHolder = itemHolder
            .AddOrUpdateComponent(Item);

        var hasBirthdaySkillVisuals = BirthdaySkillVisuals.TryGetResources(Item.ItemDataID, out var skillVisuals);
        if (hasBirthdaySkillVisuals) {
            BirthdaySkillVisuals.CopyRequiredFiles(randomizer.FileRepository, Item.ItemDataID);
        }

        if (!PreserveItemModels) {
            var mesh = newItemHolder.FindComponent("via.render.Mesh")!;
            if (hasBirthdaySkillVisuals) {
                mesh = mesh
                    .Set("Mesh", new RszResourceNode(skillVisuals.Mesh))
                    .Set("Material", new RszResourceNode(skillVisuals.Material));

                newItemHolder = newItemHolder.AddOrUpdateComponent(mesh);
                newItemHolder = BirthdaySkillVisuals.ApplyRotationCorrection(newItemHolder);
            } else if (Item.ItemDataID == GoldenCrowbarInventoryPatch.ItemId) {
                // Birthday weapons have no campaign placement row to supply their model.
                mesh = mesh
                    .Set("Mesh", new RszResourceNode(GoldenCrowbarInventoryPatch.MeshPath))
                    .Set("Material", new RszResourceNode(GoldenCrowbarInventoryPatch.MaterialPath));
                newItemHolder = newItemHolder.AddOrUpdateComponent(mesh);
            } else {
                var newItem = randomizer.ItemPlacementService.FromId(Item.ItemDataID)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Mesh) && !string.IsNullOrWhiteSpace(x.Material));

                if (newItem != null) {
                    mesh = mesh
                        .Set("Mesh", new RszResourceNode(newItem.Mesh))
                        .Set("Material", new RszResourceNode(newItem.Material));

                    newItemHolder = newItemHolder.AddOrUpdateComponent(mesh);
                }
            }
        }

        //var fsmItemGet = container.Children.FirstOrDefault(c => c.Name == "Fsm_ItemGet", null);
        //if (fsmItemGet != null)
        //{
        //    var fsm = fsmItemGet.FindComponent("via.fsm.Fsm")!;
        //    fsm = fsm.Set("Enabled", false);
        //    container = container.AddOrUpdateComponent(fsm);
        //}

        container = container.AddOrUpdateChild(newGimmick);
        container = container.AddOrUpdateChild(newItemHolder);
        return scene.UpdateGameObject(container);
    }
}
