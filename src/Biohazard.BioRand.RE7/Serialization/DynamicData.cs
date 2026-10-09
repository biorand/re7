using System.Collections.Immutable;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Biohazard.BioRand.RE7.Serialization;

public enum DynamicDataName {
    ItemPlacements,
    Recipes,
    Enemies,
    ExtraEnemies,
    EnemyLimits,
    Messages,
    BirdCages,
    DebugStartItems,
    BirthdaySkills,
    DebugRecipes,
    SpawnGroups,
}

public sealed class DynamicData(bool download, bool downloadDebuggerData = false) {
    private const string GoogleSheetUrl =
        "https://docs.google.com/spreadsheets/d/1YNdX9LWrhh6KDKd8Mx7JpTCMq8XY8u6BfX20YYNx9jk/export?format=csv&gid={0}";

    private static readonly ImmutableDictionary<DynamicDataName, (string FileName, int? GoogleSheetId)> g_map =
        new Dictionary<DynamicDataName, (string, int?)>{
            [DynamicDataName.ItemPlacements] = ("item_placements.csv", 1561602125),
            [DynamicDataName.Recipes] = ("recipes.csv", 358865420),
            [DynamicDataName.Enemies] = ("enemies.csv", 2063646676),
            [DynamicDataName.ExtraEnemies] = ("extra_enemies.csv", 2063983386),
            [DynamicDataName.EnemyLimits] = ("enemy_limits.csv", 1254028764),
            [DynamicDataName.SpawnGroups] = ("spawn_groups.csv", 154763280),
            [DynamicDataName.Messages] = ("messages.csv", 1050646915),
            [DynamicDataName.BirdCages] = ("bird_cages.csv", 1920824337),
            [DynamicDataName.DebugStartItems] = ("debug_start_items.csv", 639198893),
            [DynamicDataName.BirthdaySkills] = ("birthday_skills.csv", 1933511558),
            [DynamicDataName.DebugRecipes] = ("debug_recipes.csv", 1413280780),
        }.ToImmutableDictionary();

    private static readonly HttpClient s_httpClient = new();
    private readonly ConcurrentDictionary<DynamicDataName, Lazy<byte[]?>> _map = [];

    public bool DownloadEnabled => download || downloadDebuggerData;

    internal bool ShouldDownload(DynamicDataName name)
        => download || (downloadDebuggerData && name is DynamicDataName.DebugStartItems or DynamicDataName.DebugRecipes);

    public static string? GetFileName(DynamicDataName name) {
        if (g_map.TryGetValue(name, out var entry)) {
            return entry.FileName;
        }

        return null;
    }

    public byte[]? GetData(DynamicDataName name) {
        return _map.GetOrAdd(
            name,
            key => new Lazy<byte[]?>(() => LoadData(key), LazyThreadSafetyMode.ExecutionAndPublication)
        ).Value;
    }

    public void PrefetchAll() {
        if (!DownloadEnabled)
            return;

        Parallel.ForEach(g_map.Keys.Where(ShouldDownload), name => _ = GetData(name));
    }

    internal void SetData(DynamicDataName name, byte[] data) {
        _map[name] = new Lazy<byte[]?>(() => data, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private byte[]? LoadData(DynamicDataName name) {
        var (fileName, gid) = g_map[name];
        if (ShouldDownload(name) && gid.HasValue) {
            var downloadUrl = string.Format(GoogleSheetUrl, gid.Value);
            return Download(downloadUrl);
        }

        return EmbeddedData.GetFile(fileName);
    }

    private static byte[] Download(string url) {
        return s_httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
    }
}
