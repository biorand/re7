using Biohazard.BioRand.RE7;
using Biohazard.BioRand.RE7.DataGen;
using Biohazard.BioRand.RE7.Enemies;
using Biohazard.BioRand.RE7.Modifiers;
using Biohazard.BioRand.RE7.Extensions;
using Biohazard.BioRand.RE7.Serialization;
using IntelOrca.Biohazard.REE.Compression;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using Spectre.Console;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using static Biohazard.BioRand.RE7.DataGen.Commands.GenerateCommand;

internal class EnemyPlacementGenerator : IFileGenerator {
    public string Id => "enemies";
    public bool CopyToDataDirectory => true;

    private readonly RszTypeRepository _rszRepository =
        RszRepositorySerializer.Default.FromJson(EmbeddedData.GetFile("rszre7rt.json.gz").Ungzip());

    private readonly PakFile _pakFile = Constants.BioRandPakFile;

    private readonly PakList _pakList =
        new(Encoding.UTF8.GetString(Gzip.DecompressData(EmbeddedData.GetFile("pakcontentsrt.txt.gz"))));

    private static readonly string[] _pathExclusions =[
        "/alphatest/", "/vfx/", "/animation/", "/copyasset/", "/light/",
        "lightset", "/ui/", "/sound/", "/vr/", "/install/",
        "/preloading/", "/loadtemp/", "/mainmenu/", "cubemap"
    ];

    private static readonly Regex _enemyRegexPath =
        new(@"Character/Enemy/(Em\d{4})\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);


    private static readonly Regex _enemyRegexGameObjectName =
        new(@"(Em\d{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private List<EnemyPlacement> ReadEnemyPlacements(ulong hash) {
        var path = _pakList.GetPath(hash)!;

        if (IsExcluded(path))
            return [];

        var scene = new ScnFile(FileVersions.SceneFileVersion, _pakFile.GetEntryData(hash))
            .ReadScene(_rszRepository);

        var results = new List<EnemyPlacement>();

        scene.VisitGameObjects(go => {
            TryExtractFromMesh(go, path, results);
            TryExtractFromSpawnInfo(go, path, results);
        });

        return results;
    }

    private static bool IsExcluded(string path) =>
        _pathExclusions.Any(ex => path.Contains(ex, StringComparison.OrdinalIgnoreCase));

    private void TryExtractFromMesh(RszGameObject go, string path, List<EnemyPlacement> results) {
        var mesh = go.FindComponent("via.render.Mesh");
        if (mesh == null)
            return;

        var meshPath = mesh.Children[2]?.ToString();
        if (meshPath?.StartsWith("Character/Enemy", StringComparison.OrdinalIgnoreCase) != true)
            return;

        var enemyId = ParseEnemyId(meshPath, go.Name, path, "EnemyGameObject");
        if (enemyId == null)
            return;

        var definition = EnemyDefinitions.Instance.FromId(enemyId.Value);
        if (definition == null)
            return;

        AddPlacement(
            results,
            go,
            path,
            enemyId.Value,
            definition,
            isSpawnInfo: false,
            enabled: go.Settings.Get<bool>("Update") && go.Settings.Get<bool>("Draw")
        );
    }

    private void TryExtractFromSpawnInfo(RszGameObject go, string path, List<EnemyPlacement> results) {
        var spawn = go.FindComponent<app.EnemySpawnInfo>();
        if (spawn == null)
            return;

        var enemyId = ParseEnemyId(spawn.UnitAlias, spawn.UnitAlias, path, "EnemySpawnInfo");
        if (enemyId == null)
            return;

        var definition = EnemyDefinitions.Instance.FromId(enemyId.Value);
        if (definition == null)
            return;

        AddPlacement(
            results,
            go,
            path,
            enemyId.Value,
            definition,
            isSpawnInfo: true,
            enabled: spawn.Enabled
        );
    }

    private EnemyID? ParseEnemyId(string source, string debugName, string path, string context) {
        Regex regexToBeUsed = context == "EnemySpawnInfo" ? _enemyRegexGameObjectName : _enemyRegexPath;
        var match = regexToBeUsed.Match(source);
        if (!match.Success)
            return null;

        var emIdInPath = match.Groups[1].Value;
        if (!Enum.TryParse(emIdInPath, true, out EnemyID enemyId)) {
            AnsiConsole.MarkupLine($"[yellow]{context} -- Unknown Enemy ID {debugName} in {path}[/].");
            return null;
        }

        return enemyId;
    }

    private void AddPlacement(
        List<EnemyPlacement> results,
        RszGameObject go,
        string path,
        EnemyID enemyId,
        IEnemyDefinition definition,
        bool isSpawnInfo,
        bool enabled) {
        var dlc = DlcTypeExtensions.FromPakFileName(path);
        if (dlc != null)
            return;

        var transform = go.FindComponent<GeneratedViaTransform>()!;
        var chapter = GetChapterFromPath(path);

        var name = go.Name.Contains("blade", StringComparison.OrdinalIgnoreCase)
            ? "Molded (Blade)"
            : definition.Name;

        var directives = EnemySpawnInfoRules.GetDefaultPlacementDirectives(
            path, go, definition, isSpawnInfo, enabled);

        results.Add(new EnemyPlacement{
            EnemyID = enemyId,
            Chapter = chapter,
            Dlc = dlc,
            Name = name,
            Enabled = enabled,
            IsSpawnInfo = isSpawnInfo,
            PosX = transform.Position.X,
            PosY = transform.Position.Y,
            PosZ = transform.Position.Z,
            RotX = transform.Rotation.X,
            RotY = transform.Rotation.Y,
            RotZ = transform.Rotation.Z,
            RotW = transform.Rotation.W,
            SceneFile = path,
            Guid = go.Guid,
            Tags = directives.Tags,
            Include = directives.Include,
            Exclude = directives.Exclude,
        });
    }

    private static int GetChapterFromPath(string path) => path switch{
        var p when p.Contains("chapter0") => 0,
        var p when p.Contains("chapter1") || p.Contains("c01") => 1,
        var p when p.Contains("chapter3") || p.Contains("c03") => 3,
        var p when p.Contains("chapter4") || p.Contains("c04") || p.Contains("ff050") => 4,
        var p when p.Contains("chapter7") || p.Contains("c07") => 7,
        var p when p.Contains("chapter8") || p.Contains("c08") => 8,
        var p when p.Contains("chapter9") || p.Contains("c09") => 9,
        _ => -1
    };

    private List<EnemyPlacement> GetEnemyPlacements() {
        var results = new ConcurrentBag<EnemyPlacement>();

        var hashes = _pakFile.FileHashes
            .Where(h => _pakList.GetPath(h)?.EndsWith($".scn.{FileVersions.SceneFileVersion}") == true)
            .ToList();

        Parallel.ForEach(hashes, hash => {
            var path = _pakList.GetPath(hash)!;
            var placements = ReadEnemyPlacements(hash);

            if (placements.Count == 0)
                return;

            foreach (var p in placements)
                results.Add(p);

            AnsiConsole.MarkupLine($"[green]Extracted {placements.Count} enemy placements from {path}[/].");
        });

        return results.ToList();
    }

    public object Generate(GenerateSettings settings) {
        var placements = GetEnemyPlacements();
        AnsiConsole.MarkupLine($"[green]Generated {placements.Count} enemy placements.[/]");
        return placements.OrderBy(p => p.Chapter);
    }
}
