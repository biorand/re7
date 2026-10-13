using Biohazard.BioRand.RE7.Items;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.BioRand;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using System.Text;
using System.Text.RegularExpressions;

namespace Biohazard.BioRand.RE7.Tests;

[Trait("Category", "RequiresPak")]
public class BaseGameCompatibilityTests : IClassFixture<WithoutDlcBaselineFixture> {
    private readonly WithoutDlcBaselineFixture _baseline;

    public BaseGameCompatibilityTests(WithoutDlcBaselineFixture baseline) => _baseline = baseline;

    [Fact]
    public void BaseGameTemplates_DoNotReferenceKnownDlcAssetsOrComponents() {
        using var randomizer = new Randomizer(new RandomizerInput {
            Configuration = RandomizerTest.CreateFeatureTestConfiguration(config => config["base-game-only"] = true),
        }, _baseline.Path, new EmptyReporter());
        randomizer.CaptureStateLogs = false;
        // Initialize the source repository before asking for native campaign
        // template donors. The two integration cases below cover full logging.
        randomizer.RandomizeFiles();
        var templates = new ScnFile(FileVersions.SceneFileVersion, EmbeddedData.GetFile("template.scn.20"))
            .ReadScene(FileRepository.RszRepository);
        var selected = new List<RszGameObject>();
        templates.VisitGameObjects(gameObject => {
            const string itemPrefix = "ItemTemplate_";
            if (gameObject.Name.StartsWith(itemPrefix, StringComparison.Ordinal)
                && ItemDefinitionRepository.Default.FromId(gameObject.Name[itemPrefix.Length..]) is { IsDlcItem: false }) {
                selected.Add(randomizer.TemplateService.GetItemTemplate(gameObject.Name[itemPrefix.Length..]));
            }
        });
        selected.AddRange(EnemyDefinitions.Instance.Randomizable.Where(enemy => enemy.Dlc == null)
            .Select(enemy => randomizer.TemplateService.GetEnemyTemplate(enemy.EnemyId.ToString())));
        selected.AddRange(EnemyDefinitions.Instance.Randomizable.Where(enemy => enemy.Dlc == null)
            .Select(enemy => randomizer.TemplateService.TryGetEnemySpawnInfo(enemy.EnemyId.ToString()))
            .OfType<RszGameObject>());
        selected.AddRange(new[] { "Chest", "ItemBox", "ItemBox_VLong", "ItemBox_Fake", "EnemyGenerator", "FsmGenerator" }
            .Select(randomizer.TemplateService.GetObject));
        var violations = new List<string>();
        foreach (var template in selected) {
            template.VisitGameObjects(gameObject => {
                if (!string.IsNullOrEmpty(gameObject.Prefab)
                    && WithoutDlcBaselineFixture.IsKnownDlcPath(gameObject.Prefab)) {
                    violations.Add($"{template.Name}: prefab {gameObject.Prefab}");
                }
            });
            template.Visit(node => {
                if (node is RszObjectNode obj
                    && (obj.Type.Name.Contains("Birthday", StringComparison.OrdinalIgnoreCase)
                        || obj.Type.Name.StartsWith("app.CH8", StringComparison.Ordinal)
                        || obj.Type.Name.StartsWith("app.CH9", StringComparison.Ordinal))) {
                    violations.Add($"{template.Name}: DLC component {obj.Type.Name}");
                }
                var resource = node switch {
                    RszResourceNode value => value.Value,
                    RszUserDataNode value => value.Path,
                    _ => null,
                };
                if (resource != null && WithoutDlcBaselineFixture.IsKnownDlcPath(resource)) {
                    violations.Add($"{template.Name}: {resource}");
                }
                return node;
            });
        }
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations.Distinct()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BaseGameOnly_GeneratesWithoutKnownDlcSourceAssets_AndAddsNoDlcResourceReferences(bool requestDlcFeatures) {
        var configuration = RandomizerExecutor.DefaultConfiguration;
        configuration["base-game-only"] = true;
        configuration["debug-download-data"] = false;
        if (requestDlcFeatures) {
            // A legacy or hand-written profile must not override compatibility mode.
            configuration["allow-dlc-items"] = true;
            configuration["dlc-campaign-weapons"] = true;
            configuration["random-starting-inventory-skills-ethan"] = true;
            configuration["random-starting-inventory-skills-mia"] = true;
            configuration["random-bird-cage-drugs-coins"] = true;
            configuration["random-bird-cage-magnum"] = true;
            configuration["additional-items"] = true;
            configuration["additional-wooden-crates"] = true;
            configuration["random-key-item-locations"] = true;
            configuration["random-starting-inventory-ethan"] = true;
            configuration["random-starting-inventory-mia"] = true;
            configuration["random-starting-inventory-vhs"] = true;
            configuration["recipes-add-new"] = true;
            configuration["allow-bonus-items"] = true;
            configuration["boss-random-health"] = true;
            configuration["enemy-random-health"] = true;
            configuration["random-enemy-speed"] = true;
            configuration["random-enemy-damage"] = true;
            configuration["weapon-mod-damage"] = true;
            configuration["weapon-mod-ammo-capacity"] = true;
            configuration["weapon-mod-reload-speed"] = true;
            configuration["random-events"] = true;
            configuration["extra-enemy-amount"] = 1.0;
            configuration["enemy-multiplier"] = 1.5;
            foreach (var drop in ItemDrops.HighValueDrops) {
                configuration[$"item-drop-valuable-{drop}"] = true;
                configuration[$"enemy-drop-valuable-{drop}"] = true;
            }
        }

        using var randomizer = new Randomizer(new RandomizerInput {
            Seed = 895914,
            UserName = "base-game-compatibility-tests",
            Configuration = configuration,
        }, _baseline.Path, new EmptyReporter());
        randomizer.DynamicData.SetData(DynamicDataName.EnemyLimits,
            Encoding.UTF8.GetBytes("SceneFile,MaxEnemies,Comment\r\n"));
        randomizer.DynamicData.SetData(DynamicDataName.DebugRecipes, EmbeddedData.GetFile("debug_recipes.csv"));
        randomizer.DynamicData.SetData(DynamicDataName.DebugStartItems, EmbeddedData.GetFile("debug_start_items.csv"));
        using var before = new FileRepository(randomizer, _baseline.Path, randomizer.DynamicData);

        Assert.True(_baseline.RemovedFileCount > 0);
        Assert.Null(before.GetFile("natives/stm/prefab/item/birthdayskillitemsetting.user.2"));
        Assert.Null(before.GetFile("natives/stm/prefab/skill/skl001/skl001.pfb.17"));
        // Campaign weapon adapters use the shared base registry; only the
        // original DLC registries are expected to be absent.
        foreach (var sourceFile in ItemDefinitionRepository.Default.Items.Where(item => item.IsDlcItem)
                     .Select(item => $"natives/stm/prefab/item/{item.SourceUserFile}")
                     .Where(WithoutDlcBaselineFixture.IsKnownDlcPath).Distinct()) {
            Assert.Null(before.GetFile(sourceFile));
        }
        var output = randomizer.Randomize();
        using var patchArchive = output.Assets.Single(asset => asset.Key == "1-patch").Data.Unzip();
        using var fluffyArchive = output.Assets.Single(asset => asset.Key == "2-fluffy").Data.Unzip();
        Assert.NotNull(patchArchive.GetEntry("input.log"));
        Assert.NotNull(patchArchive.GetEntry("output.log"));
        Assert.Contains(fluffyArchive.Entries, entry => entry.FullName.StartsWith("natives/", StringComparison.Ordinal));

        var changed = randomizer.FileRepository.GetOutputFilesSnapshot();
        var additional = randomizer.FileRepository.GetAdditionalOutputFilesSnapshot();
        Assert.NotEmpty(changed);
        var violations = new List<string>();
        foreach (var (path, bytes) in changed.Concat(additional)) {
            if (WithoutDlcBaselineFixture.IsKnownDlcPath(path)) {
                violations.Add($"Output file: {path}");
            }

            // Baseline registries may already mention unmounted DLC roots. Only
            // references introduced by this generation create a new dependency.
            var original = before.GetFile(path);
            var originalResources = original == null ? [] : GetResources(path, original);
            foreach (var resource in GetResources(path, bytes)
                         .Except(originalResources, StringComparer.OrdinalIgnoreCase)
                         .Where(WithoutDlcBaselineFixture.IsKnownDlcPath)) {
                violations.Add($"New resource in {path}: {resource}");
            }
        }

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> GetResources(string path, byte[] bytes) {
        if (path.EndsWith(".scn.20", StringComparison.OrdinalIgnoreCase)) {
            var file = new ScnFile(FileVersions.SceneFileVersion, bytes);
            return file.Resources.Concat(file.Prefabs).Concat(GetUserDataPaths(file.ReadScene(FileRepository.RszRepository)));
        }
        if (path.EndsWith(".pfb.17", StringComparison.OrdinalIgnoreCase)) {
            var file = new PfbFile(FileVersions.PfbFileVersion, bytes);
            return file.Resources.Concat(GetUserDataPaths(file.ReadScene(FileRepository.RszRepository)));
        }
        if (path.EndsWith(".user.2", StringComparison.OrdinalIgnoreCase)) {
            var resources = new List<string>();
            foreach (var root in new UserFile(bytes).GetObjects(FileRepository.RszRepository)) {
                root.Visit(node => {
                    if (node is RszResourceNode { Value: { } value }) resources.Add(value);
                    if (node is RszUserDataNode { Path: { } userPath }) resources.Add(userPath);
                    return node;
                });
            }
            return resources;
        }
        return [];
    }

    private static IEnumerable<string> GetUserDataPaths(RszScene scene) {
        var paths = new List<string>();
        scene.Visit(node => {
            if (node is RszUserDataNode { Path: { } userPath }) paths.Add(userPath);
            return node;
        });
        return paths;
    }
}

/// <summary>
/// Removes known DLC paths from the source PAK so generation cannot accidentally
/// rely on DLC files merely because the developer's baseline contains them.
/// This is a conservative path audit, not a substitute for a clean-install playtest
/// or an authoritative per-depot ownership manifest.
/// </summary>
public sealed class WithoutDlcBaselineFixture : IDisposable {
    private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
        $"biorand-base-game-test-{Guid.NewGuid():N}");

    private static readonly Regex DlcPathPattern = new(
        @"(^|[/_])(?:ch0?[89]|chapter0?[789]|c0?[789])(?:[/_.-]|$)|" +
        @"birthday|nightmare|cardgame|/survival/|/dlc/|[/_](?:emd|imd)(?:[/_.-]|$)|" +
        @"(?:dlcitem|coin)_0[1-5]|/prefab/skill/skl\d|/resources/skl\d|/sm9958_skillpatch01/",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string Path { get; }
    public int RemovedFileCount { get; }

    public WithoutDlcBaselineFixture() {
        Directory.CreateDirectory(_directory);
        Path = System.IO.Path.Combine(_directory, "without-dlc.pak");
        using var source = new PakFile(RandomizerTest.InputPakPath);
        var builder = new PakFileBuilder();
        foreach (var path in RandomizerExecutor.GetDefaultPakList().Entries.Distinct(StringComparer.OrdinalIgnoreCase)) {
            var bytes = source.GetEntryData(path);
            if (bytes == null) continue;
            if (IsKnownDlcPath(path)) {
                RemovedFileCount++;
            } else {
                builder.AddEntry(path, bytes);
            }
        }
        builder.Save(Path, CompressionKind.Zstd);
    }

    internal static bool IsKnownDlcPath(string? path)
        => path != null && DlcPathPattern.IsMatch("/" + path.Replace('\\', '/').TrimStart('/'));

    public void Dispose() {
        var directory = System.IO.Path.GetFullPath(_directory);
        var temporaryRoot = System.IO.Path.TrimEndingDirectorySeparator(
            System.IO.Path.GetFullPath(System.IO.Path.GetTempPath())) + System.IO.Path.DirectorySeparatorChar;
        if (!directory.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
            || !System.IO.Path.GetFileName(directory).StartsWith("biorand-base-game-test-", StringComparison.Ordinal)) {
            throw new InvalidOperationException("Refusing to remove a fixture outside its temporary directory.");
        }
        Directory.Delete(directory, true);
    }
}
