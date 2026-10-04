using Biohazard.BioRand.RE7.Items;
using System.Globalization;

namespace Biohazard.BioRand.RE7.Serialization;

// The control cell is outside the sortable table: A1=DebugModeEnabled, B1=checkbox;
// row 4 contains the headers. Rows 2-3 are instructions, not recipe data.
internal sealed class DebugRecipeTable {
    public const int MaxVisibleResults = 20;
    public static DebugRecipeTable Disabled { get; } = new();

    public bool Enabled { get; private init; }
    public string SourceCsv { get; private init; } = "";
    public IReadOnlyList<Entry> Entries { get; private init; } = [];
    public string[] VisibleResultIds => Entries.Where(x => x.ShowInUI)
        .OrderBy(x => x.UIOrder ?? int.MaxValue)
        .ThenBy(x => x.Row)
        .Select(x => x.Recipe.ResultItemID)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public sealed record Entry(int Row, Recipe Recipe, bool ShowInUI, int? UIOrder);

    public static DebugRecipeTable Parse(byte[] data) {
        var source = Encoding.UTF8.GetString(data).TrimStart('\uFEFF');
        var cells = Csv.Read(source);
        if (cells.GetLength(0) < 2 || cells[0, 0].Trim() != "DebugModeEnabled") {
            throw Error(1, "A1 must contain DebugModeEnabled and B1 must contain the mode checkbox.");
        }
        if (!ReadBool(cells[1, 0], 1, "DebugModeEnabled")) return Disabled;
        if (cells.GetLength(1) < 4) throw Error(4, "Missing recipe headers.");

        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var x = 0; x < cells.GetLength(0); x++) {
            var name = cells[x, 3].Trim();
            if (name.Length != 0 && !headers.TryAdd(name, x)) throw Error(4, $"Duplicate header '{name}'.");
        }
        string[] required = ["Enabled", "ShowInUI", "DataID", "SrcItemID1", "SrcItemNum1",
            "SrcItemID2", "SrcItemNum2", "ResultItemID", "ResultItemNum", "UIOrder", "EnableFlag",
            "IsTutorialTarget", "IsTrophyTarget", "Comment"];
        foreach (var name in required) {
            if (!headers.ContainsKey(name)) throw Error(4, $"Missing header '{name}'.");
        }

        var entries = new List<Entry>();
        var dataIds = new HashSet<string>(StringComparer.Ordinal);
        for (var y = 4; y < cells.GetLength(1); y++) {
            var row = y + 1;
            string Cell(string name) => cells[headers[name], y].Trim();
            // Blank/unchecked rows are drafts and never participate in validation or output.
            if (!ReadBool(Cell("Enabled"), row, "Enabled", blank: false)) continue;
            var dataId = Cell("DataID");
            if (dataId.Length == 0) throw Error(row, "DataID must not be empty.");
            if (!dataIds.Add(dataId)) throw Error(row, $"Duplicate enabled DataID '{dataId}'.");
            var flagText = Cell("EnableFlag");
            var flag = Guid.Empty;
            if (flagText.Length != 0 && !Guid.TryParse(flagText, out flag)) {
                throw Error(row, "EnableFlag must be a GUID or blank (always available).");
            }
            var recipe = new Recipe {
                DataID = dataId,
                SrcItemID1 = Cell("SrcItemID1"),
                SrcItemNum1 = ReadCount(Cell("SrcItemNum1"), row, "SrcItemNum1"),
                SrcItemID2 = Cell("SrcItemID2"),
                SrcItemNum2 = ReadCount(Cell("SrcItemNum2"), row, "SrcItemNum2"),
                ResultItemID = Cell("ResultItemID"),
                ResultItemNum = ReadCount(Cell("ResultItemNum"), row, "ResultItemNum"),
                EnableFlag = flag,
                IsTutorialTarget = ReadBool(Cell("IsTutorialTarget"), row, "IsTutorialTarget", blank: false),
                IsTrophyTarget = ReadBool(Cell("IsTrophyTarget"), row, "IsTrophyTarget", blank: false),
                _Comment = cells[headers["Comment"], y],
            };
            entries.Add(new Entry(row, recipe,
                ReadBool(Cell("ShowInUI"), row, "ShowInUI", blank: false),
                Cell("UIOrder").Length == 0 ? null : ReadCount(Cell("UIOrder"), row, "UIOrder")));
        }

        var table = new DebugRecipeTable { Enabled = true, Entries = entries, SourceCsv = source };
        if (table.VisibleResultIds.Length > MaxVisibleResults) {
            throw Error(4, $"ShowInUI selects {table.VisibleResultIds.Length} distinct result items; the combine UI supports at most {MaxVisibleResults}.");
        }
        return table;
    }

    public void ValidateItems(IEnumerable<Recipe> originalRecipes) {
        var definitions = ItemDefinitionRepository.Default;
        // Vanilla contains legacy treasure-map fragments absent from the item catalog.
        // Preserve IDs proven by the input PAK instead of accepting arbitrary strings.
        var knownIds = originalRecipes.SelectMany(r => new[] { r.SrcItemID1, r.SrcItemID2, r.ResultItemID })
            .Concat(definitions.IdToItemMap.Keys).Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var entry in Entries) {
            var recipe = entry.Recipe;
            foreach (var (column, id) in new[] { ("SrcItemID1", recipe.SrcItemID1),
                         ("SrcItemID2", recipe.SrcItemID2), ("ResultItemID", recipe.ResultItemID) }) {
                if (!knownIds.Contains(id)) throw Error(entry.Row, $"{column}: unknown item ID '{id}'.");
            }
            if (definitions.FromId(recipe.ResultItemID) is { MaxStack: > 0 } item
                && recipe.ResultItemNum > item.MaxStack) {
                throw Error(entry.Row, $"ResultItemNum exceeds the safe catalog maximum {item.MaxStack} for '{item.Id}'.");
            }
        }
    }

    private static int ReadCount(string value, int row, string column) {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0) {
            throw Error(row, $"{column} must be a positive whole number.");
        }
        return count;
    }

    private static bool ReadBool(string value, int row, string column, bool? blank = null) {
        if (value.Length == 0 && blank.HasValue) return blank.Value;
        if (!bool.TryParse(value, out var result)) throw Error(row, $"{column} must be TRUE or FALSE.");
        return result;
    }

    private static InvalidDataException Error(int row, string message)
        => new($"Debug Recipes, row {row}: {message}");
}
