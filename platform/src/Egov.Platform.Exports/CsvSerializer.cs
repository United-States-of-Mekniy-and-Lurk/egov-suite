using System.Reflection;
using System.Text;

namespace Egov.Platform.Exports;

/// <summary>
/// Serializes collections of records/objects to CSV format.
/// Handles quoting, escaping, and header generation from public properties.
/// </summary>
public static class CsvSerializer
{
    /// <summary>
    /// Serializes a collection of items to CSV with headers derived from public properties.
    /// </summary>
    public static string Serialize<T>(IEnumerable<T> items, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();

        var sb = new StringBuilder();

        // Header row
        if (options.IncludeHeader)
        {
            sb.AppendLine(string.Join(options.Delimiter,
                properties.Select(p => Escape(p.Name, options.Delimiter))));
        }

        // Data rows
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(options.Delimiter,
                properties.Select(p => FormatValue(p.GetValue(item), options.Delimiter))));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes a collection using explicit column definitions for custom mapping.
    /// </summary>
    public static string Serialize<T>(IEnumerable<T> items, IReadOnlyList<CsvColumn<T>> columns, CsvOptions? options = null)
    {
        options ??= CsvOptions.Default;
        var sb = new StringBuilder();

        if (options.IncludeHeader)
        {
            sb.AppendLine(string.Join(options.Delimiter,
                columns.Select(c => Escape(c.Header, options.Delimiter))));
        }

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(options.Delimiter,
                columns.Select(c => FormatValue(c.ValueSelector(item), options.Delimiter))));
        }

        return sb.ToString();
    }

    private static string FormatValue(object? value, string delimiter)
    {
        if (value is null) return string.Empty;
        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");
        if (value is DateOnly d) return d.ToString("yyyy-MM-dd");
        if (value is decimal dec) return dec.ToString("F2");
        return Escape(value.ToString() ?? string.Empty, delimiter);
    }

    private static string Escape(string value, string delimiter)
    {
        if (value.Contains(delimiter, StringComparison.Ordinal) ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

public sealed record CsvOptions
{
    public static readonly CsvOptions Default = new();
    public string Delimiter { get; init; } = ",";
    public bool IncludeHeader { get; init; } = true;
}

public sealed record CsvColumn<T>(string Header, Func<T, object?> ValueSelector);
