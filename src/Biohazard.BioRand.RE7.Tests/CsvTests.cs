using Biohazard.BioRand.RE7.Serialization;
using System.Globalization;
using System.Text;

namespace Biohazard.BioRand.RE7.Tests;

public class CsvTests {
    [Fact]
    public void QuotedCells_PreserveLeadingAndTrailingLiteralQuotes() {
        const string csv = "Name,Value\n\"\"\"quoted\"\"\",7\n";

        var row = Assert.Single(Csv.Deserialize<BomHeaderRow>(Encoding.UTF8.GetBytes(csv)));

        Assert.Equal("\"quoted\"", row.Name);
        Assert.Equal(7, row.Value);
        Assert.Equal(row.Name, Csv.Read(csv)[0, 1]);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    public void Deserialize_AcceptsQuotedHeadersAndEscapedMultilineCells(string newline) {
        var csv = Encoding.UTF8.GetBytes(
            $"\uFEFF\"Name\",\"Value\"{newline}\"Main, \"\"Hall\"\"{newline}Upstairs\",\"7\"{newline}");

        var row = Assert.Single(Csv.Deserialize<BomHeaderRow>(csv));

        Assert.Equal($"Main, \"Hall\"{newline}Upstairs", row.Name);
        Assert.Equal(7, row.Value);
    }

    [Fact]
    public void Deserialize_UsesInvariantCultureForDecimalScalars() {
        var originalCulture = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var csv = Encoding.UTF8.GetBytes("""
                                             FloatValue,DoubleValue
                                             4.88,0.25
                                             """);

            var row = Assert.Single(Csv.Deserialize<DecimalRow>(csv));

            Assert.Equal(4.88f, row.FloatValue, 3);
            Assert.Equal(0.25, row.DoubleValue, 3);
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void Deserialize_MapsBomPrefixedFirstHeader() {
        var csv = Encoding.UTF8.GetBytes("\uFEFFName,Value\nMain Hall,7\n");

        var row = Assert.Single(Csv.Deserialize<BomHeaderRow>(csv));

        Assert.Equal("Main Hall", row.Name);
        Assert.Equal(7, row.Value);
    }

    private sealed class DecimalRow {
        public float FloatValue { get; init; }
        public double DoubleValue { get; init; }
    }

    private sealed class BomHeaderRow {
        public string Name { get; init; } = "";
        public int Value { get; init; }
    }
}
