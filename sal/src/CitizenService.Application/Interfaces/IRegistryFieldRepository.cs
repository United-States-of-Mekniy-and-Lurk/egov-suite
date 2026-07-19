using CitizenService.Domain.Entities;

namespace CitizenService.Application.Interfaces;

public interface IRegistryFieldRepository
{
    Task<IReadOnlyList<RegistryFieldDefinition>> ListDefinitionsAsync(bool includeInactive, CancellationToken ct);
    Task<RegistryFieldDefinition?> GetDefinitionByIdAsync(Guid id, CancellationToken ct);
    Task<RegistryFieldDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken ct);
    Task<RegistryFieldDefinition> AddDefinitionAsync(RegistryFieldDefinition definition, CancellationToken ct);
    Task<RegistryFieldDefinition> UpdateDefinitionAsync(RegistryFieldDefinition definition, CancellationToken ct);
    Task<IReadOnlyList<CitizenFieldValue>> ListValuesAsync(Guid citizenId, CancellationToken ct);
    Task<IReadOnlyList<CitizenFieldValue>> ListValuesByDefinitionAsync(Guid fieldDefinitionId, CancellationToken ct);
    Task<CitizenFieldValue?> GetValueAsync(Guid citizenId, Guid fieldDefinitionId, CancellationToken ct);
    Task<CitizenFieldValue> SaveValueAsync(CitizenFieldValue value, CancellationToken ct);
}