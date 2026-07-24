using Refit;

namespace OrganizationRegistry.Api.Services;

public interface IPersonRegistryApi
{
    [Get("/me")]
    Task<ApiResponse<PersonResponse>> GetCurrentPersonAsync(
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);
}

public sealed class PersonResponse
{
    public Guid Id { get; set; }
}