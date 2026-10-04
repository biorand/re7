using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Serialization;
using Biohazard.BioRand.RE7.Services;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Cryptography;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading;

namespace Biohazard.BioRand.RE7;

internal class Randomizer : IDisposable {
    private readonly string _inputGamePath;
    private FileRepository _fileRepository = new();
    private ImmutableArray<Modifier> _modifiers = [];
    private readonly Dictionary<Type, object> _services = [];
    private readonly Lock _servicesLock = new();
    private readonly Dictionary<string, string> _logFiles = [];
    private readonly Dictionary<string, RandomizerOutputAsset> _seedOutputAssets = [];
    private DebugRecipeTable? _debugRecipes;

    internal bool IsDebugger => UserTags.Contains("re7:debugger");
    internal DebugRecipeTable DebugRecipes => _debugRecipes ??= IsDebugger
        ? DebugRecipeTable.Parse(DynamicData.GetData(DynamicDataName.DebugRecipes)
            ?? throw new InvalidDataException("Unable to get Debug Recipes data."))
        : DebugRecipeTable.Disabled;

    public int PakVersion { get; set; } = 1;
    public RandomizerInput Input { get; }
    public IRandomizerProgress Reporter { get; }
    public FileRepository FileRepository => _fileRepository;
    public DynamicData DynamicData { get; }
    internal bool CaptureStateLogs { get; set; } = true;
    internal RandomizerLoggerIO? LastLog { get; private set; }

    public static string BuildVersion => RandomizerFactory.Default.GitHash;

    public static IntelOrca.Biohazard.BioRand.RandomizerConfigurationDefinition ConfigurationDefinition =>
        RandomizerConfigurationDefinition.Create();

    public static RandomizerConfiguration DefaultConfiguration =>
        RandomizerConfigurationDefinition.Create().GetDefault();

    private static readonly string[] _optionsThatRequireREFramework =[
        "debug-force-reframework",
        "madhouse-normal-saves",
        "inventory-unrestricted-management",
        "random-events",
        "random-starting-inventory-skills-ethan",
        "random-starting-inventory-skills-mia",
        "recipes-add-new",
        "weapon-mod-reload-speed",
        "random-starting-inventory-ethan",
        "random-starting-inventory-mia",
        "random-starting-inventory-additional-gun-ethan",
        "random-starting-inventory-additional-gun-mia",
        "additional-items",
        "random-bird-cage-magnum",
        "random-bird-cage-drugs-coins",
    ];

    internal bool IsREFrameworkRequired()
        => DebugRecipes.Enabled
           || GetConfigOption<bool>("random-enemies")
           || GetConfigOption<double>("extra-enemy-amount") > 0
           || (GetConfigOption<bool>("random-items") && GetConfigOption<bool>("replace-weapons"))
           || GetConfigOption<bool>("random-enemy-drops")
           || GetConfigOption<bool>("allow-dlc-items")
           || _optionsThatRequireREFramework.Any(option => GetConfigOption<bool>(option))
           || GetConfigOption<string>("random-starting-inventory-size-ethan") != "12"
           || GetConfigOption<string>("random-starting-inventory-size-mia") != "12";

    public Randomizer(RandomizerInput input, string inputGamePath, IRandomizerProgress reporter) {
        Input = input;
        _inputGamePath = inputGamePath;
        Reporter = reporter;

        DynamicData = new DynamicData(Input.Configuration.GetValueOrDefault<bool>("debug-download-data"),
            downloadDebuggerData: IsDebugger);
        _modifiers = GetModifiers();
    }

    public void Dispose() {
        _fileRepository?.Dispose();
    }

    public IntelOrca.Biohazard.BioRand.RandomizerOutput Randomize() {
        var input = Input;
        _fileRepository = new FileRepository(this, _inputGamePath, DynamicData);

        var log = Randomize(input);
        AddLogFile($"input.log", log.Input.Output);
        AddLogFile($"process.log", log.Process.Output);
        AddLogFile($"output.log", log.Output.Output);

        IntelOrca.Biohazard.BioRand.RandomizerOutput? result = null;
        Reporter.RunTask("Building mod", () => {
            var output = new RandomizerOutput(
                input,
                _fileRepository.GetOutputPakFile(),
                _fileRepository.GetAdditionalOutputPakFile(),
                _logFiles,
                PakVersion,
                IsREFrameworkRequired(),
                DebugRecipes.Enabled
            );
            var assets = new List<RandomizerOutputAsset>{
                new(
                    "1-patch",
                    "Patch",
                    "Extract all contents of this ZIP into the folder containing re7.exe. Includes all required shared assets. " +
                    "Install REFramework separately when required, and remove any previous BioRand seed first. " +
                    "Use this instead of the Fluffy Mod ZIP.",
                    $"biorand-re7-{input.Seed}.zip",
                    output.GetOutputZip()),
                new(
                    "2-fluffy",
                    "Fluffy Mod",
                    "Enable this ZIP in Fluffy Mod Manager for RE7 RT/DX12. Includes shared assets. " +
                    "Install REFramework separately when required, and disable any previous BioRand seed first. " +
                    "Use this instead of the Patch ZIP.",
                    $"biorand-re7-{input.Seed}-mod.zip",
                    output.GetOutputMod())
            };
            assets.AddRange(_seedOutputAssets.Values.OrderBy(asset => asset.Key, StringComparer.Ordinal));

            result = new IntelOrca.Biohazard.BioRand.RandomizerOutput(
                assets.ToImmutableArray(),
                """
                <p class="mt-3">What should I do if my game crashes?</p>
                <ol class="list-decimal text-gray-300" style="margin-left: 3rem;">
                  <li>Reload from last checkpoint and try again.</li>
                  <li>Alter the enemy sliders slightly or reduce the number temporarily. This will reshuffle the enemies. Reload from last checkpoint and try again.</li> <li>As a last resort, change your seed, and reload from last checkpoint.</li>
                </ol>
                """);
        });
        return result!;
    }

    public RandomizerLoggerIO Randomize(RandomizerInput input) {
        _modifiers = GetModifiers();

        var logger = new RandomizerLoggerIO();
        foreach (var l in new[]{ logger.Input, logger.Process, logger.Output }) {
            l.LogHr();
            l.LogVersionTimeInfo(
                RandomizerFactory.Default.CurrentVersionInfo,
                "by IntelOrca, Descole & BioRand Team");
            l.LogLine($"Seed = {input.Seed}");
            l.LogHr();
        }

        if (DynamicData.DownloadEnabled) {
            Reporter.RunTask("Downloading latest spreadsheet data from Google Sheets", DynamicData.PrefetchAll);
        }

        // Patches
        Reporter.RunTask("Applying patches", () => ExportedMods.ApplyAll(this, FileRepository));

        if (CaptureStateLogs) {
            // Input
            IterateModifiers((n, m) => {
                logger.Input.Push(n);
                m.LogState(logger.Input);
                logger.Input.Pop();
                logger.Input.LogHr();
            });
        } else {
            logger.Input.LogLine("State logging disabled.");
            logger.Input.LogHr();
        }

        // Apply modifiers
        IterateModifiers((n, m) => {
            logger.Process.Push(n);
            Reporter.RunTask($"Running modifier: {n}", () => m.Apply(logger.Process));
            logger.Process.Pop();
            logger.Process.LogHr();
        });

        // Save Flags
        FlagService.Save(logger.Process);

        if (CaptureStateLogs) {
            // Output
            IterateModifiers((n, m) => {
                logger.Output.Push(n);
                m.LogState(logger.Output);
                logger.Output.Pop();
                logger.Output.LogHr();
            });
        } else {
            logger.Output.LogLine("State logging disabled.");
            logger.Output.LogHr();
        }

        LastLog = logger;
        return logger;
    }

    private void IterateModifiers(Action<string, Modifier> action) {
        foreach (var modifier in _modifiers) {
            var name = modifier.GetType().Name.Replace("Modifier", "");
            action(name, modifier);
        }
    }

    private ImmutableArray<Modifier> GetModifiers() {
        return[
            // Enemies
            new EnemyDirectiveModifier(this),
            new EnemyModifier(this),
            new EnemyMultiplierModifier(this),

            // Player
            new PlayerModifier(this),

            // Inventory
            new StartingInventoryModifier(this),
            new RecipeModifier(this),
            new ItemStackModifier(this),

            // Items
            new ExtraPlacementModifier(this),
            new ItemModifier(this),
            new BirdCageModifier(this),
            new ItemDropTableModifier(this),
            new LucasPuzzleInventoryModifier(this),
            new KeyItemLocationModifier(this),

            // Weapons
            new WeaponModifier(this),

            // Misc.
            new MadhouseSaveModifier(this),
            new ChapterJumpDataModifier(this),
            new MessageModifier(this),
            new PlayerGuideModifier(this),
            new UvarDefaultsModifier(this),
        ];
    }

    public string User => GetConfigOption<string>("username") ?? "player";
    public List<string> UserTags => GetConfigOption<string>("tags")?.Split(",").ToList() ?? [];
    public int Seed => Input.Seed;

    public Rng GetRng(params object[] key) {
        var seed = MurMur3.HashData(FormatRngKey(Input.Seed, key));
        return new Rng(seed);
    }

    private static string FormatRngKey(int seed, object[] key) {
        var result = new StringBuilder();
        AppendPart("seed", seed.ToString(CultureInfo.InvariantCulture));
        foreach (var part in key) {
            var value = Convert.ToString(part, CultureInfo.InvariantCulture) ?? "";
            AppendPart(part?.GetType().FullName ?? "<null>", value);
        }

        return result.ToString();

        void AppendPart(string type, string value) {
            result
                .Append(type.Length)
                .Append(':')
                .Append(type)
                .Append('=')
                .Append(value.Length)
                .Append(':')
                .Append(value)
                .Append(';');
        }
    }

    public T? GetConfigOption<T>(string key, T? defaultValue = default) {
        return Input.Configuration.GetValueOrDefault(key, defaultValue);
    }

    public T? GetEnumConfigOption<T>(string key) where T : struct, Enum {
        var value = Input.Configuration.GetValueOrDefault<string>(key);
        return value == null ? null : EnumExtensions.ParseOrNull<T>(value);
    }

    public bool HasSpecialTouch(string kind) {
        if (!GetConfigOption("enable-special", true))
            return false;

        var special = GetConfigOption<string>("special");
        var present = special?.Split(',').Contains(kind) == true;
        return present;
    }

    public T GetService<T>() {
        using var scope = _servicesLock.EnterScope();
        var type = typeof(T);
        _services.TryGetValue(type, out var service);
        if (service == null) {
            service = Activator.CreateInstance(type, [this])!;
            _services[type] = service;
        }

        return (T)service;
    }

    public TemplateService TemplateService => GetService<TemplateService>();
    public AreaService AreaService => GetService<AreaService>();
    public ItemRandomizer ItemRandomizer => GetService<ItemRandomizer>();
    public ItemPlacementService ItemPlacementService => GetService<ItemPlacementService>();

    public StaticItemRandomizationService StaticItemRandomizationService =>
        GetService<StaticItemRandomizationService>();

    public FlagService FlagService => GetService<FlagService>();
    public ChestService ChestService => GetService<ChestService>();
    public EnemySceneLimitService EnemySceneLimitService => GetService<EnemySceneLimitService>();

    public void AddLogFile(string name, string content) {
        _logFiles[name] = content;
    }

    public void AddOutputAsset(RandomizerOutputAsset asset) {
        _seedOutputAssets[asset.Key] = asset;
    }
}
