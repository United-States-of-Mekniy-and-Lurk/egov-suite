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
            .Where(value => value.CitizenId == citizenId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CitizenFieldValue>> ListValuesByDefinitionAsync(
        Guid fieldDefinitionId, CancellationToken ct)
        => await _context.CitizenFieldValues
            .Where(value => value.FieldDefinitionId == fieldDefinitionId)
            .ToListAsync(ct);

    public async Task<CitizenFieldValue?> GetValueAsync(
        Guid citizenId, Guid fieldDefinitionId, CancellationToken ct)
        => await _context.CitizenFieldValues.FirstOrDefaultAsync(
            value => value.CitizenId == citizenId && value.FieldDefinitionId == fieldDefinitionId, ct);

    public async Task<CitizenFieldValue> SaveValueAsync(CitizenFieldValue value, CancellationToken ct)
    {
        if (_context.Entry(value).State == EntityState.Detached)
            _context.CitizenFieldValues.Add(value);

        await _context.SaveChangesAsync(ct);
        return value;
    }
}