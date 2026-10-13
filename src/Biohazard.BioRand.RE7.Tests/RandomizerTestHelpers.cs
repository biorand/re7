using Biohazard.BioRand.RE7.Items;
using Enums.app.GameManager;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Rsz;
using IntelOrca.Biohazard.REE.Variables;

namespace Biohazard.BioRand.RE7.Tests;

internal static class RandomizerTestHelpers {
    public static void ConfigureSingleDropRate(RandomizerConfiguration configuration, string id, double value) {
        configuration[$"item-drop-ratio-{ItemDrops.GetConfigId(id)}"] = value;
    }

    public static Dictionary<string, app.Collision.AttackUserData> ReadAttackUserDataByRequestSet(
        RandomizerRunResult result, string path, bool before) {
        var bytes = before ? result.ReadBeforeBytes(path) : result.ReadAfterBytes(path);
        var builder = new RcolFile(FileVersions.RcolFileVersion, bytes)
            .ToBuilder(result.Randomizer.FileRepository.TypeRepository);

        return builder.RequestSets
            .Where(x => x.UserData?.Type.Name == "app.Collision.AttackUserData")
            .ToDictionary(
                x => x.Name,
                x => RszSerializer.Deserialize<app.Collision.AttackUserData>(x.UserData!)!);
    }

    public static app.ChapterJumpData GetChapterJump(RszScene scene, Guid guid) {
        var gameObject = scene.FindGameObject(guid);
        Assert.NotNull(gameObject);
        var jump = gameObject!.FindComponent<app.ChapterJumpData>();
        Assert.NotNull(jump);
        return jump!;
    }

    public static IReadOnlyList<(Guid Guid, ChapterNo JumpChapter)> GetChapterJumps(RszScene scene) {
        var result = new List<(Guid, ChapterNo)>();
        scene.VisitGameObjects(gameObject => {
            var jump = gameObject.FindComponent<app.ChapterJumpData>();
            if (jump != null) {
                result.Add((gameObject.Guid, jump.JumpChapter));
            }
        });
        return result;
    }

    public static RszGameObject GetDynamicParent(RszScene scene) {
        var gameObject = scene.FindGameObject(go => go.Name.EndsWith("_dynamic", StringComparison.Ordinal));
        Assert.NotNull(gameObject);
        return gameObject!;
    }

    public static List<BirdCageState> GetBirdCageStates(RszScene scene) {
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

    public static List<UvarGroupState> ReadGlobalVariableGroups(RandomizerRunResult result, bool before) {
        var bytes = before
            ? result.ReadBeforeBytes(RandomizerTestPaths.GlobalVariablesPath)
            : result.ReadAfterBytes(RandomizerTestPaths.GlobalVariablesPath);
        return ReadGlobalVariableGroups(bytes);
    }

    public static List<UvarGroupState> ReadGlobalVariableGroups(byte[] bytes) {
        var uvar = new UvarFile(bytes);
        var result = new List<UvarGroupState>();

        for (int i = 0; i < uvar.EmbeddedCount; i++) {
            var embeddedFile = uvar.GetEmbedded(i);
            var variables = embeddedFile
                .ToBuilder()
                .Variables
                .Select(variable => new UvarVariableState(
                    embeddedFile.Name,
                    variable.Guid,
                    variable.Name,
                    variable.Value,
                    variable.TypeVal))
                .ToList();

            result.Add(new UvarGroupState(embeddedFile.Name, variables));
        }

        return result;
    }
}

internal sealed record BirdCageState(Guid ContainerGuid, string ItemId, int ItemCount, int CoinCount);

internal sealed record UvarGroupState(string Name, IReadOnlyList<UvarVariableState> Variables);

internal sealed record UvarVariableState(string GroupName, Guid Guid, string Name, float Value, int TypeVal) {
    public bool BooleanValue => Value != 0;
}
