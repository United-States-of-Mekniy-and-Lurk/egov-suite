using Refit;

namespace CitizenService.Infrastructure.Http;

public interface IPersonRegistryApi
{
    [Get("/me")]
    Task<ApiResponse<PersonApiResponse>> GetCurrentPersonAsync(
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    [Get("/persons/{id}")]
    Task<ApiResponse<PersonApiResponse>> GetPersonByIdAsync(Guid id, CancellationToken ct = default);
}

public class PersonApiResponse
{
    public Guid Id { get; set; }
    public string IdentitySubject { get; set; } = string.Empty;
    public string PreferredUsername { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
