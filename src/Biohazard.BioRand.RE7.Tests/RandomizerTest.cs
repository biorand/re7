using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Services;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using System.Collections.Immutable;
using System.IO.Compression;

namespace Biohazard.BioRand.RE7.Tests;

public static class RandomizerTest {
    private const string TestPAKPathEnvVariable = "BIORAND_RE7_TEST_PAK";
    private const string PAKName = "biorand-re7.pak";

    private static readonly string BiorandDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".biorand"
    );

    private static readonly string LocalPAKPath = Path.Combine(
        BiorandDirectory,
        PAKName
    );

    private static readonly Lazy<string> PakPath = new(ResolvePAKPath);
    private static readonly Lazy<RandomizerExecutor> Executor = new(() => new(PakPath.Value, new EmptyReporter()));
    private const int DefaultTestingSeed = 0x42424242;

    public static string InputPakPath => PakPath.Value;

    public static RandomizerConfiguration CreateFeatureTestConfiguration(
        Action<RandomizerConfiguration>? configure = null) {
        var configuration = RandomizerExecutor.DefaultConfiguration;

        configuration["debug-download-data"] = false;

        configuration["allow-bonus-items"] = false;
        configuration["allow-dlc-items"] = true;

        configuration["randomized-messages"] = false;
        configuration["random-enemies"] = false;
        configuration["extra-enemy-amount"] = 0.0;
        configuration["enemy-multiplier"] = 1.0;
        configuration["random-enemy-speed"] = false;
        configuration["random-enemy-damage"] = false;
        configuration["random-enemy-drops"] = false;
        configuration["boss-random-health"] = false;
        configuration["enemy-random-health"] = false;
        foreach (var drop in ItemDrops.HighValueDrops) {
            configuration[$"enemy-drop-valuable-{drop}"] = false;
        }

        configuration["random-items"] = false;
        configuration["random-key-item-locations"] = false;
        configuration["madhouse-normal-saves"] = false;
        configuration["disable-mia-opening-damage"] = false;
        configuration["pause-inventory"] = false;
        configuration["inventory-unrestricted-management"] = false;
        configuration["replace-madhouse-tapes"] = false;
        configuration["replace-weapons"] = false;
        configuration["random-bird-cage-magnum"] = false;
        configuration["random-bird-cage-drugs-coins"] = false;
        configuration["additional-items"] = false;
        configuration["additional-wooden-crates"] = false;
        foreach (var drop in ItemDrops.GenericDrops) {
            configuration[$"item-drop-ratio-{drop.ToLowerInvariant()}"] = ItemDrops.GetDefaultGenericDropRatio(drop);
        }

        foreach (var drop in ItemDrops.HighValueDrops) {
            configuration[$"item-drop-valuable-{drop}"] = false;
        }

        configuration["random-starting-inventory-ethan"] = false;
        configuration["random-starting-inventory-mia"] = false;
        configuration["random-starting-inventory-additional-gun-ethan"] = false;
        configuration["random-starting-inventory-additional-gun-mia"] = false;
        configuration["random-starting-inventory-vhs"] = false;
        configuration["random-starting-inventory-skills-ethan"] = false;
        configuration["random-starting-inventory-skills-mia"] = false;
        configuration["random-starting-inventory-size-ethan"] = "12";
        configuration["random-starting-inventory-size-mia"] = "12";
        foreach (var item in ItemDefinitionRepository.Default.Items.Where(item => item.IsStackLimitConfigurable)) {
            configuration[item.StackLimitConfigId] = item.MaxStack;
        }

        configuration["player-random-max-health"] = false;
        configuration["player-random-reload-speed"] = false;
        configuration["player-random-psychostimulants"] = false;

        configuration["recipes-add-new"] = false;

        configuration["weapon-mod-damage"] = false;
        configuration["weapon-mod-ammo-capacity"] = false;
        configuration["weapon-mod-reload-speed"] = false;
        foreach (var stat in WeaponModifier.GunStatRandomizations) {
            configuration[stat.ToggleConfigId] = false;
        }

        configuration["random-events"] = false;

        configure?.Invoke(configuration);
        return configuration;
    }

    public static (ZipArchive, PakFile) Run(string configJson, int seed = DefaultTestingSeed) {
        var output = RunOutput(configJson, seed);

        var zipAsset = output.Assets.FirstOrDefault(asset => asset.Key == "1-patch")?.Data.Unzip();
        Assert.NotNull(zipAsset);

        return (
            zipAsset,
            new PakFile(
                zipAsset
                    .Entries
                    .Single(entry => entry.Name == "re_chunk_000.pak.patch_001.pak")
                    .GetBytes()
            )
        );
    }

    public static IntelOrca.Biohazard.BioRand.RandomizerOutput RunOutput(
        string configJson,
        int seed = DefaultTestingSeed) {
        var input = new RandomizerInput(){
            Seed = seed,
            Configuration = RandomizerConfiguration.FromJson(configJson)
        };

        var output = Executor.Value.Randomize(input);
        Assert.NotNull(output);
        return output;
    }

    internal static RandomizerRunResult RunState(
        Action<RandomizerConfiguration>? configure = null,
        int seed = DefaultTestingSeed,
        Action<Randomizer>? prepareRandomizer = null) {
        var configuration = CreateFeatureTestConfiguration(configure);
        var input = new RandomizerInput(){
            Seed = seed,
            UserName = "behavior-tests",
            ProfileName = "Behavior Tests",
            ProfileAuthor = "xUnit",
            ProfileDescription = "Randomizer behavior test profile.",
            Configuration = configuration
        };

        var randomizer = new Randomizer(input, InputPakPath, new EmptyReporter());
        randomizer.CaptureStateLogs = false;
        randomizer.DynamicData.SetData(
            DynamicDataName.EnemyLimits,
            System.Text.Encoding.UTF8.GetBytes("SceneFile,MaxEnemies,Comment\r\n"));
        // Tagged debugger runs fetch these sheets in production. Tests always use
        // local snapshots and may override them in prepareRandomizer.
        randomizer.DynamicData.SetData(DynamicDataName.DebugRecipes, EmbeddedData.GetFile("debug_recipes.csv"));
        randomizer.DynamicData.SetData(DynamicDataName.DebugStartItems, EmbeddedData.GetFile("debug_start_items.csv"));
        prepareRandomizer?.Invoke(randomizer);
        var beforeRepository = new FileRepository(randomizer, InputPakPath, randomizer.DynamicData);
        // Behavior assertions read serialized repository files. Archive and
        // executor tests use Run/RunOutput to exercise the full packaging path.
        randomizer.RandomizeFiles();

        return new RandomizerRunResult(randomizer, beforeRepository);
    }

    private static string ResolvePAKPath() {
        var configuredPath = Environment.GetEnvironmentVariable(TestPAKPathEnvVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath)) {
            if (!File.Exists(configuredPath)) {
                throw new FileNotFoundException("Configured baseline PAK not found.", configuredPath);
            }

            return configuredPath;
        }

        if (File.Exists(LocalPAKPath)) {
            return LocalPAKPath;
        }

        var embeddedPAK = EmbeddedData.TryGetFile(PAKName);
        if (embeddedPAK is not null) {
            Directory.CreateDirectory(BiorandDirectory);
            File.WriteAllBytes(LocalPAKPath, embeddedPAK);
            return LocalPAKPath;
        }

        throw new FileNotFoundException(
            $"Baseline {PAKName} not found. Put it at {LocalPAKPath} or set {TestPAKPathEnvVariable}.");
    }
}

public sealed class DefaultRandomizerRunFixture : IDisposable {
    private readonly Lazy<RandomizerRunResult> _result = new(() => RandomizerTest.RunState());

    public RandomizerRunResult Result => _result.Value;

    public void Dispose() {
        if (_result.IsValueCreated) {
            _result.Value.Dispose();
        }
    }
}

public sealed class RandomizerRunResult : IDisposable {
    private readonly Randomizer _randomizer;
    private readonly FileRepository _beforeRepository;

    internal RandomizerRunResult(Randomizer randomizer, FileRepository beforeRepository) {
        _randomizer = randomizer;
        _beforeRepository = beforeRepository;
        ChangedFiles = _randomizer.FileRepository.GetOutputFilesSnapshot();
        AdditionalAssetFiles = _randomizer.FileRepository.GetAdditionalOutputFilesSnapshot();
    }

    public ImmutableDictionary<string, byte[]> ChangedFiles { get; }
    public ImmutableDictionary<string, byte[]> AdditionalAssetFiles { get; }
    internal Randomizer Randomizer => _randomizer;
    internal ItemRandomizer ItemRandomizer => _randomizer.ItemRandomizer;
    internal ItemPlacementService ItemPlacementService => _randomizer.ItemPlacementService;
    internal AreaService AreaService => _randomizer.AreaService;
    public string ProcessLog => _randomizer.LastLog?.Process.Output ?? string.Empty;

    public byte[] ReadBeforeBytes(string path)
        => _beforeRepository.GetFile(path) ?? throw new InvalidOperationException($"Missing baseline file '{path}'.");

    public byte[] ReadAfterBytes(string path)
        => _randomizer.FileRepository.GetFile(path) ??
           throw new InvalidOperationException($"Missing output file '{path}'.");

    public T ReadBeforeUserFile<T>(string path) => _beforeRepository.DeserializeUserFile<T>(path);

    public T ReadAfterUserFile<T>(string path) => _randomizer.FileRepository.DeserializeUserFile<T>(path);

    public MsgFile ReadBeforeMsgFile(string path) => _beforeRepository.GetMsgFile(path);

    public MsgFile ReadAfterMsgFile(string path) => _randomizer.FileRepository.GetMsgFile(path);

    public RszScene ReadBeforeScene(string path)
        => _beforeRepository.GetScnFile(path).ReadScene(_beforeRepository.TypeRepository);

    public RszScene ReadAfterScene(string path)
        => _randomizer.FileRepository.GetScnFile(path).ReadScene(_randomizer.FileRepository.TypeRepository);

    public RszScene ReadAfterPfb(string path)
        => new PfbFile(FileVersions.PfbFileVersion, ReadAfterBytes(path))
            .ReadScene(_randomizer.FileRepository.TypeRepository);

    public bool WasFileModified(string path) => ChangedFiles.ContainsKey(path);

    public void Dispose() {
        _beforeRepository.Dispose();
        _randomizer.Dispose();
    }
}
