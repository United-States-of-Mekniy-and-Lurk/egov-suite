using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ElectionService.Web.Services;

public sealed class ElectionApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public static async Task<ElectionApiException> FromResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ApiProblem? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: ct);
        }
        catch (JsonException)
        {
        }

        return new ElectionApiException(response.StatusCode,
            problem?.Detail ?? problem?.Title ?? "The election service could not complete the request.");
    }

    private sealed record ApiProblem(string? Title, string? Detail);
}