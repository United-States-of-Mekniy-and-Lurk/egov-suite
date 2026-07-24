using System.Net;
using System.Net.Http.Json;
using CitizenService.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenService.Web.Services;

public sealed class PersonDirectoryService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<PersonDirectoryService> logger)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<PersonViewModel?> GetAsync(Guid personId, CancellationToken ct)
    {
        var cacheKey = $"person:{personId}";
        if (cache.TryGetValue(cacheKey, out PersonViewModel? cachedPerson))
            return cachedPerson;

        try
        {
            var client = httpClientFactory.CreateClient("PersonRegistry");
            using var response = await client.GetAsync($"/persons/{personId}", ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new DownstreamUnauthorizedException("Person Registry");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Person Registry lookup for {PersonId} returned {StatusCode}", personId, response.StatusCode);
                return null;
            }

            var person = await response.Content.ReadFromJsonAsync<PersonViewModel>(cancellationToken: ct);
            if (person is not null)
                cache.Set(cacheKey, person, CacheDuration);
            return person;
        }
        catch (DownstreamUnauthorizedException)
        {
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not load person {PersonId} for the citizen directory", personId);
            return null;
        }
    }
}