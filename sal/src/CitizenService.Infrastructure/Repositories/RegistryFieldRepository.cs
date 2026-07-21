using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Infrastructure.Repositories;

public class RegistryFieldRepository : IRegistryFieldRepository
{
    private readonly CitizenDbContext _context;

    public RegistryFieldRepository(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RegistryFieldDefinition>> ListDefinitionsAsync(
        bool includeInactive, CancellationToken ct)
        => await _context.RegistryFieldDefinitions
            .Where(definition => includeInactive || definition.IsActive)
            .OrderBy(definition => definition.SortOrder)
            .ThenBy(definition => definition.Key)
            .ToListAsync(ct);

    public async Task<RegistryFieldDefinition?> GetDefinitionByIdAsync(Guid id, CancellationToken ct)
        => await _context.RegistryFieldDefinitions.FindAsync([id], ct);

    public async Task<RegistryFieldDefinition?> GetDefinitionByKeyAsync(string key, CancellationToken ct)
        => await _context.RegistryFieldDefinitions
            .FirstOrDefaultAsync(definition => definition.Key == key, ct);

    public async Task<RegistryFieldDefinition> AddDefinitionAsync(
        RegistryFieldDefinition definition, CancellationToken ct)
    {
        _context.RegistryFieldDefinitions.Add(definition);
        await _context.SaveChangesAsync(ct);
        return definition;
    }

    public async Task<RegistryFieldDefinition> UpdateDefinitionAsync(
        RegistryFieldDefinition definition, CancellationToken ct)
    {
        _context.RegistryFieldDefinitions.Update(definition);
        await _context.SaveChangesAsync(ct);
        return definition;
    }

    public async Task<IReadOnlyList<CitizenFieldValue>> ListValuesAsync(Guid citizenId, CancellationToken ct)
        => await _context.CitizenFieldValues
            .Where(value => value.CitizenId == citizenId &&
                value.ValidFrom <= DateTime.UtcNow &&
                (value.ValidTo == null || value.ValidTo > DateTime.UtcNow))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CitizenFieldValue>> ListValueHistoryAsync(
        Guid citizenId,
        CancellationToken ct)
        => await _context.CitizenFieldValues
            .Where(value => value.CitizenId == citizenId)
            .OrderBy(value => value.FieldDefinitionId)
            .ThenByDescending(value => value.ValidFrom)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CitizenFieldValue>> ListValuesByDefinitionAsync(
        Guid fieldDefinitionId, CancellationToken ct)
        => await _context.CitizenFieldValues
            .Where(value => value.FieldDefinitionId == fieldDefinitionId &&
                value.ValidFrom <= DateTime.UtcNow &&
                (value.ValidTo == null || value.ValidTo > DateTime.UtcNow))
            .ToListAsync(ct);

    public async Task<CitizenFieldValue?> GetValueAsync(
        Guid citizenId, Guid fieldDefinitionId, CancellationToken ct)
        => await _context.CitizenFieldValues.FirstOrDefaultAsync(
            value => value.CitizenId == citizenId &&
                value.FieldDefinitionId == fieldDefinitionId &&
                value.ValidFrom <= DateTime.UtcNow &&
                (value.ValidTo == null || value.ValidTo > DateTime.UtcNow), ct);

    public async Task<CitizenFieldValue> ReplaceCurrentValueAsync(
        CitizenFieldValue? currentValue,
        CitizenFieldValue replacement,
        CancellationToken ct)
    {
        if (currentValue != null)
        {
            currentValue.ValidTo = replacement.ValidFrom;
            currentValue.UpdatedAt = replacement.CreatedAt;
        }
        _context.CitizenFieldValues.Add(replacement);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "The registry value changed concurrently. Reload the record and try again.", exception);
        }
        return replacement;
    }
}