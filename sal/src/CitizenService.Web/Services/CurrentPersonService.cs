using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using CitizenService.Web.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CitizenService.Web.Services;

public sealed class CurrentPersonService(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache,
    ILogger<CurrentPersonService> logger)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public async Task<PersonViewModel?> GetAsync(CancellationToken ct)
    {
        var subject = httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            return null;

        var cacheKey = $"current-person:{subject}";
        if (cache.TryGetValue(cacheKey, out PersonViewModel? cachedPerson))
            return cachedPerson;

        try
        {
            var client = httpClientFactory.CreateClient("PersonRegistry");
            using var response = await client.GetAsync("/me", ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new DownstreamUnauthorizedException("Person Registry");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Person Registry /me returned {StatusCode}", response.StatusCode);
                return null;
            }

            var person = await response.Content.ReadFromJsonAsync<PersonViewModel>(cancellationToken: ct);
            if (person == null || person.Id == Guid.Empty)
                return null;

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
            logger.LogError(exception, "Failed to resolve the current person");
            return null;
        }
    }
}