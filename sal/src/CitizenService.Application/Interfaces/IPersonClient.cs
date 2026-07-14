namespace CitizenService.Application.Interfaces;

public interface IPersonClient
{
    Task<PersonDto?> GetPersonAsync(Guid personId, CancellationToken ct);
    Task<bool> PersonExistsAsync(Guid personId, CancellationToken ct);
}

public class PersonDto
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
