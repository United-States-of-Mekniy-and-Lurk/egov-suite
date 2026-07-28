using Microsoft.AspNetCore.Http;

namespace Egov.Platform.Exports;

/// <summary>
/// Extension methods for returning structured data exports from endpoints.
/// </summary>
public static class ExportResults
{
    /// <summary>
    /// Writes a CSV response with appropriate headers.
    /// </summary>
    public static async Task WriteCsvAsync<T>(HttpResponse response, IEnumerable<T> items,
        string filename, CsvOptions? options = null, CancellationToken ct = default)
    {
        var csv = CsvSerializer.Serialize(items, options);
        response.ContentType = "text/csv; charset=utf-8";
        response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
        response.Headers["Cache-Control"] = "public, max-age=60";
        await response.WriteAsync(csv, ct);
    }

    /// <summary>
    /// Writes a CSV response using explicit column definitions.
    /// </summary>
    public static async Task WriteCsvAsync<T>(HttpResponse response, IEnumerable<T> items,
        IReadOnlyList<CsvColumn<T>> columns, string filename, CsvOptions? options = null, CancellationToken ct = default)
    {
        var csv = CsvSerializer.Serialize(items, columns, options);
        response.ContentType = "text/csv; charset=utf-8";
        response.Headers["Content-Disposition"] = $"attachment; filename=\"{filename}\"";
        response.Headers["Cache-Control"] = "public, max-age=60";
        await response.WriteAsync(csv, ct);
    }

    /// <summary>
    /// Determines if the request prefers CSV format (via query param or Accept header).
    /// </summary>
    public static bool WantsCsv(HttpRequest request) =>
        request.Query.TryGetValue("format", out var fmt) && fmt.ToString().Equals("csv", StringComparison.OrdinalIgnoreCase) ||
        (request.Headers.Accept.ToString().Contains("text/csv", StringComparison.OrdinalIgnoreCase));
}
