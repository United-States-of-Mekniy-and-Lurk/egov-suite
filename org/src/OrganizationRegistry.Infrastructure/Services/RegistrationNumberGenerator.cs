using System.Security.Cryptography;
using OrganizationRegistry.Application.Abstractions;

namespace OrganizationRegistry.Infrastructure.Services;

public sealed class RegistrationNumberGenerator(IOrganizationRegistryStore store) : IRegistrationNumberGenerator
{
    public async Task<string> NextAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = $"ORG-{DateTime.UtcNow:yyyy}-{RandomNumberGenerator.GetInt32(0, 100_000_000):D8}";
            if (!await store.RegistrationNumberExistsAsync(candidate, ct)) return candidate;
        }
        throw new InvalidOperationException("Could not generate a unique organization registration number.");
    }
}