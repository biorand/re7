#:package IntelOrca.Biohazard.REE@1.5.6
#:property PublishAot=false

// Run from the repository root. Reads local assets; writes metadata CSV only.
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.REE.Fsm;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;
using IntelOrca.Biohazard.REE.Variables;

if (args.Length != 6 || args[0] != "--scene-pak" || args[2] != "--resource-pak" || args[4] != "--output") {
    Console.Error.WriteLine("Usage: dotnet run --file tools/generate-spawn-group-states.cs -- " +
        "--scene-pak <biorand-re7.pak> --resource-pak <vanilla RT re_chunk_000.pak> --output <metadata.csv>");
    return 1;
}
foreach (var inputPath in new[] { args[1], args[3] }) {
    if (!File.Exists(inputPath)) {
        Console.Error.WriteLine($"Input PAK not found: {inputPath}. Supply the baseline and vanilla RT PAKs described in spawn_group_states.md.");
        return 1;
    }
}

var repo = RszRepositorySerializer.Default.FromJson(Ungzip("src/Biohazard.BioRand.RE7/_Data/rszre7rt.json.gz"));
var paths = Encoding.UTF8.GetString(Ungzip("src/Biohazard.BioRand.RE7/_Data/pakcontentsrt.txt.gz"))
    .Split('\n').Select(x => x.Trim()).Where(x => x.Length != 0).Order(StringComparer.Ordinal);
using var scenes = new PakFile(args[1]);
using var resources = new PakFile(args[3]);
var variableNames = new Dictionary<Guid, string>();
ReadVariables(new UvarFile(Required(resources, "natives/stm/userdata/globalvariables.uvar.2")));
var rows = new List<string[]>();
var controllers = 0;
foreach (var path in paths.Where(p => p.EndsWith(".scn.20", StringComparison.Ordinal) &&
    Regex.IsMatch(p, @"/leveldesign/fsm/chapter[134]/"))) {
    var bytes = scenes.GetEntryData(path);
    if (bytes == null) continue;
    Walk(new ScnFile(20, bytes).ReadScene(repo), path, "");
}
if (controllers == 0 || rows.Count == 0) throw new InvalidDataException("No main-flow controllers found; check the input PAKs.");
var header = new[] { "ChapterSection", "Phase", "PhaseHint", "State", "OwnerGuid", "CoreId", "StateName",
    "StateIdHex", "SceneFile", "OwnerPath", "FsmResource", "GatePackage", "GateFlags", "GateFlagGuids", "Evidence" };
var lines = new[] { header }.Concat(rows.OrderBy(r => r[0], StringComparer.Ordinal)
    .ThenBy(r => int.Parse(r[1], CultureInfo.InvariantCulture)));
File.WriteAllText(args[5], string.Join("\n", lines.Select(r => string.Join(",", r.Select(Csv)))) + "\n", new UTF8Encoding(false));
Console.WriteLine($"Wrote {rows.Count} phase states from {controllers} controllers.");
return 0;

void Walk(IRszSceneNode node, string scenePath, string objectPath) {
    if (node is RszFolder folder) objectPath += "/" + folder.Name;
    if (node is RszGameObject go) {
        objectPath += "/" + go.Name;
        // Curate persistent campaign main-flow owners, not thousands of tiny gimmick FSMs.
        if (objectPath.StartsWith("/MainFlow/", StringComparison.Ordinal) &&
            go.Components.Any(c => c.Type.Name is "app.GameFlowActiveControl" or "app.GameSubFlowActiveControl")) {
            var fsms = go.Components.Where(c => c.Type.Name == "via.fsm.Fsm").ToArray();
            if (fsms.Length != 1) throw new InvalidDataException($"Expected one FSM on {scenePath}:{objectPath}.");
            ReadController(go, fsms[0], scenePath, objectPath);
        }
    }
    foreach (var child in node.Children) Walk(child, scenePath, objectPath);
}

void ReadController(RszGameObject go, RszObjectNode fsm, string scenePath, string objectPath) {
    var resourcePath = AssetPath(((RszResourceNode)fsm["Resource"]).Value!, ".16");
    var graph = new HfsmFile(Required(resources, resourcePath));
    // This catalog intentionally excludes nested graphs/parallel cores: every emitted
    // state is an immediate child of the only root in an ordinary via.fsm.Fsm.
    if (graph.StateEntries.Count(s => s.ParentIndex < 0) != 1 || graph.StateEntries.Any(s => s.Depth > 1))
        throw new InvalidDataException($"Cannot assign root core 0 to non-flat graph {resourcePath}.");
    var root = graph.StateEntries.Single(s => s.ParentIndex < 0);
    if (root.StateId != 0) throw new InvalidDataException($"Unexpected root identity in {resourcePath}.");
    var sceneData = (RszArrayNode)fsm["SceneData"];
    if (sceneData.Children.Length != 1) throw new InvalidDataException($"Expected one scene-data resource on {objectPath}.");
    var overrides = (RszArrayNode)((RszObjectNode)sceneData.Children[0])["v1_Actions"];
    var actions = graph.ActionData.ReadObjectList(repo);
    var references = graph.ActionReferences.ToDictionary(r => r.Key);
    var duplicateNames = graph.StateEntries.GroupBy(s => s.Name, StringComparer.Ordinal)
        .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
    controllers++;
    foreach (var state in graph.StateEntries.Where(s => s.ParentIndex == root.Index)) {
        // End/After/ActiveOff are lifecycle states, not a progression window.
        if (!int.TryParse(state.Name, CultureInfo.InvariantCulture, out var phase) || phase == 0) continue;
        if (duplicateNames.Contains(state.Name)) {
            Console.Error.WriteLine($"Excluded ambiguous state {go.Name}|{state.Name} ({state.StateId:X8}).");
            continue;
        }
        var gatePaths = new List<string>();
        var flagGuids = new List<Guid>();
        foreach (var actionRef in state.ActionReferences) {
            if (!references.TryGetValue(actionRef.Key, out var reference)) continue;
            var original = actions[reference.ObjectIndex];
            var candidates = overrides.Children.OfType<RszObjectNode>().Where(a =>
                a.Get<uint>("v2_UID") == actionRef.Uid && a.Get<byte>("v3_ListNo") == actionRef.ListNo).ToArray();
            if (candidates.Length > 1) throw new InvalidDataException($"Duplicate action override on {objectPath}:{state.Name}.");
            var action = candidates.SingleOrDefault() ?? original;
            if (action.Type.Name != "app.GameFlowNode") continue;
            if (action["Pkg"] is not RszUserDataNode gate || gate.IsEmpty) continue;
            var gatePath = AssetPath(gate.Path!, ".2");
            var gateObjects = new UserFile(Required(scenes, gatePath)).GetObjects(repo);
            var package = gateObjects.Single(o => o.Type.Name == "app.GameFlowFlagPkg");
            gatePaths.Add(gatePath);
            flagGuids.AddRange(((RszArrayNode)package["UseFlags"]).Children.Select(c => c.Get<Guid>()));
        }
        if (gatePaths.Count == 0) {
            Console.Error.WriteLine($"Excluded phase without a resolved gate: {go.Name}|{state.Name}.");
            continue;
        }
        var unknownFlags = flagGuids.Where(g => !variableNames.ContainsKey(g)).ToArray();
        if (unknownFlags.Length != 0) {
            Console.Error.WriteLine($"Excluded phase with unresolved flag names: {go.Name}|{state.Name}: {string.Join(", ", unknownFlags)}.");
            continue;
        }
        var flags = flagGuids.Select(g => variableNames[g]).ToArray();
        var section = Regex.Match(go.Name, @"^c\d+(?:_\d+[ABC]?)?", RegexOptions.IgnoreCase).Value;
        var hint = flags.Length == 0 ? "No flags in gate package; potentially transient" :
            "Waiting for: " + string.Join("; ", flags.Select(Humanize));
        rows.Add([section, phase.ToString(CultureInfo.InvariantCulture), hint, $"{go.Guid}|0|{state.Name}",
            go.Guid.ToString(), "0", state.Name, $"0x{state.StateId:X8}", scenePath, objectPath, resourcePath,
            string.Join("; ", gatePaths), string.Join("; ", flags), string.Join("; ", flagGuids),
            "Asset-linked: scene owner + flat root FSM + scene action UID + gate package + Uvar names; see Markdown for live lookup coverage"]);
    }
}

void ReadVariables(UvarFile file) {
    foreach (var variable in file.ToBuilder().Variables) variableNames[variable.Guid] = variable.Name;
    for (var i = 0; i < file.EmbeddedCount; i++) ReadVariables(file.GetEmbedded(i));
}
static string Humanize(string flag) {
    var name = Regex.Replace(flag, @"^c\d+(?:_\d+[ABC]?)?_(?:Main_)?", "", RegexOptions.IgnoreCase);
    name = Regex.Replace(name, @"([a-z0-9])([A-Z])", "$1 $2");
    return name.Replace('_', ' ').Trim();
}
static string AssetPath(string path, string version) => "natives/stm/" + path.ToLowerInvariant() + version;
static byte[] Required(PakFile pak, string path) => pak.GetEntryData(path) ??
    throw new FileNotFoundException($"Input PAK lacks required asset {path}.");
static string Csv(string text) => "\"" + text.Replace("\"", "\"\"") + "\"";
static byte[] Ungzip(string path) {
    using var input = File.OpenRead(path);
    using var gzip = new GZipStream(input, CompressionMode.Decompress);
    using var output = new MemoryStream(); gzip.CopyTo(output); return output.ToArray();
}
