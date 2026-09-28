using IntelOrca.Biohazard.BioRand;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Biohazard.BioRand.RE7.Serialization;

public static class Csv {
    public static T[] Deserialize<T>(byte[] utf8Data) {
        var tokens = ReadTokens(utf8Data);

        var typ = typeof(T);
        var propertyMap = new List<PropertyInfo?>();
        var keyProperty = typ.GetProperties()
            .FirstOrDefault(static x => x.GetCustomAttribute<RowNumberAttribute>() != null);
        var x = 0;
        var y = 0;

        var results = new List<T>();
        T? element = default;
        var rowHasValue = false;
        foreach (var t in tokens) {
            if (t.Kind == TokenKind.EOF) {
                if (element != null && rowHasValue) {
                    results.Add(element);
                }

                break;
            } else if (t.Kind == TokenKind.Comma) {
                x++;
            } else if (t.Kind == TokenKind.NewLine) {
                if (element != null && rowHasValue) {
                    results.Add(element);
                }

                y++;
                x = 0;
                element = Activator.CreateInstance<T>();
                rowHasValue = false;
                keyProperty?.SetValue(element, y + 1);
            } else if (t.Kind == TokenKind.Text) {
                if (y == 0) {
                    while (propertyMap.Count <= x) {
                        propertyMap.Add(null);
                    }

                    var propertyName = t.Text.Trim().TrimStart('\uFEFF');
                    propertyMap[x] = typ.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                } else {
                    var prop = propertyMap[x];
                    if (prop != null) {
                        if (!string.IsNullOrWhiteSpace(t.Text)) {
                            rowHasValue = true;
                            prop.SetValue(element, ParseValue(t.Text, prop.PropertyType));
                        }
                    }
                }
            }
        }

        return results.ToArray();
    }

    internal static readonly string[] g_separator = ["\r\n", "\n"];

    private static object? ParseValue(string input, Type targetType) {
        if (string.IsNullOrWhiteSpace(input)) {
            if (IsNullable(targetType))
                return null;

            return Activator.CreateInstance(targetType);
        }

        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsGenericType &&
            underlyingType.GetGenericTypeDefinition() == typeof(ImmutableArray<>)) {
            Type elementType = underlyingType.GenericTypeArguments[0];
            string[] source = input.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            return ToImmutableArray(
                elementType,
                source.Select(x => ParseValue(x, elementType))
            );
        }

        if (underlyingType == typeof(Guid)) {
            return Guid.Parse(input);
        }

        if (underlyingType.IsEnum) {
            return Enum.Parse(underlyingType, input, ignoreCase: true);
        }

        return Convert.ChangeType(input, underlyingType, CultureInfo.InvariantCulture);
    }

    private static bool IsNullable(Type type) {
        return Nullable.GetUnderlyingType(type) != null;
    }

    private static object ToImmutableArray(Type elementType, IEnumerable<object?> items) {
        var castMethod = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.Cast))
            !.MakeGenericMethod(elementType);

        var typedEnumerable = castMethod.Invoke(null, [items]);
        var toImmutable = typeof(ImmutableArray)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is{ Name: "ToImmutableArray", IsGenericMethod: true })
            .First(m => {
                var p = m.GetParameters();
                return p is[{ ParameterType.IsGenericType: true }] &&
                       p[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
            })
            .MakeGenericMethod(elementType);

        var result = toImmutable.Invoke(null, [typedEnumerable])!;
        return result;
    }

    public static string[,] Read(string data) {
        var rows = new List<string[]>();
        var columns = new List<string>();

        var sb = new StringBuilder();
        var inQuote = false;
        for (var i = 0; i <= data.Length; i++) {
            var c = i == data.Length ? '\0' : data[i];
            if (c == '"') {
                if (!inQuote) {
                    inQuote = true;
                } else {
                    if (i < data.Length - 1 && data[i + 1] == '"') {
                        sb.Append('"');
                        i++;
                    } else {
                        inQuote = false;
                    }
                }

                continue;
            } else if (c == ',') {
                if (inQuote) {
                    sb.Append(c);
                } else {
                    columns.Add(sb.ToString());
                    sb.Clear();
                }

                continue;
            } else if (!inQuote && c == '\r') {
                // Check if next char is \n
                if (data.Length > i + 1) {
                    if (data[i + 1] == '\n') {
                        // leave handling to next char
                        continue;
                    }
                }
            }

            if ((!inQuote && (c == '\r' || c == '\n')) || c == '\0') {
                columns.Add(sb.ToString());
                sb.Clear();
                rows.Add(columns.ToArray());
                columns.Clear();
            } else {
                sb.Append(c);
            }
        }

        var numRows = rows.Count;
        var numColumns = rows.Max(x => x.Length);
        var result = new string[numColumns, numRows];
        for (var y = 0; y < numRows; y++) {
            var row = rows[y];
            for (var x = 0; x < numColumns; x++) {
                result[x, y] = row.Length > x ? row[x] : "";
            }
        }

        return result;
    }

    private static ImmutableArray<Token> ReadTokens(byte[] buffer) {
        var tokens = ImmutableArray.CreateBuilder<Token>();
        var inQuotes = false;
        var cellFinished = false;
        var textStart = -1;
        var start = buffer.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }) ? 3 : 0;
        for (var i = start; i < buffer.Length; i++) {
            var b = buffer[i];
            if (!inQuotes) {
                if (b == ',') {
                    Finish(i);
                    tokens.Add(new Token(TokenKind.Comma, ","));
                    cellFinished = false;
                } else if (b == '\n') {
                    Finish(i);
                    tokens.Add(new Token(TokenKind.NewLine, "\n"));
                    cellFinished = false;
                } else if (b == '\r') {
                    Finish(i);
                    if (i + 1 < buffer.Length && buffer[i + 1] == '\n') {
                        i++;
                    }
                    tokens.Add(new Token(TokenKind.NewLine, "\n"));
                    cellFinished = false;
                } else if (cellFinished) {
                    // Tolerate whitespace following a closing quote without
                    // carrying its offset into the next cell.
                    continue;
                } else if (b == '"') {
                    inQuotes = true;
                } else if (textStart == -1) {
                    textStart = i;
                }
            } else {
                if (b == '"') {
                    if (i + 1 < buffer.Length) {
                        if (buffer[i + 1] == '"') {
                            if (textStart == -1) textStart = i;
                            i++;
                            continue;
                        }
                    }

                    Finish(i);
                    inQuotes = false;
                } else if (textStart == -1) {
                    textStart = i;
                }
            }
        }

        Finish(buffer.Length);
        tokens.Add(new Token(TokenKind.EOF, "\0"));
        return tokens.ToImmutable();

        void Finish(int position) {
            if (cellFinished) return;
            cellFinished = true;
            if (textStart == -1) {
                tokens.Add(new Token(TokenKind.Text, ""));
            } else {
                var length = position - textStart;
                var text = Encoding.UTF8.GetString(buffer, textStart, length)
                    .Replace("\"\"", "\"");
                tokens.Add(new Token(TokenKind.Text, text));
                textStart = -1;
            }
        }
    }

    [DebuggerDisplay("{Kind} | {Text}")]
    private readonly struct Token(TokenKind kind, string text) {
        public TokenKind Kind { get; } = kind;
        public string Text { get; } = text;
    }

    private enum TokenKind {
        Text,
        Comma,
        NewLine,
        EOF
    }
}
