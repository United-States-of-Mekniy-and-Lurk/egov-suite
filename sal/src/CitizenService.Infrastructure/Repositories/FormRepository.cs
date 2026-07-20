using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Infrastructure.Repositories;

public class FormRepository : IFormRepository
{
    private readonly CitizenDbContext _context;

    public FormRepository(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationForm?> GetFormAsync(string name, int version, CancellationToken ct)
        => await _context.ApplicationForms.FirstOrDefaultAsync(f => f.Name == name && f.Version == version, ct);

    public async Task<ApplicationForm?> GetLatestFormAsync(string name, CancellationToken ct)
        => await _context.ApplicationForms.Where(f => f.Name == name && f.IsActive).OrderByDescending(f => f.Version).FirstOrDefaultAsync(ct);

    public async Task<ApplicationFormDraft?> GetDraftAsync(string name, CancellationToken ct)
        => await _context.ApplicationFormDrafts.FirstOrDefaultAsync(draft => draft.Name == name, ct);

    public async Task<IEnumerable<ApplicationForm>> ListFormsAsync(CancellationToken ct)
        => await _context.ApplicationForms.OrderBy(f => f.Name).ThenByDescending(f => f.Version).ToListAsync(ct);

    public async Task<ApplicationFormDraft> SaveDraftAsync(
        string name,
        string definitionJson,
        Guid updatedByPersonId,
        CancellationToken ct)
    {
        var draft = await _context.ApplicationFormDrafts.FindAsync([name], ct);
        if (draft == null)
        {
            draft = new ApplicationFormDraft { Name = name };
            _context.ApplicationFormDrafts.Add(draft);
        }

        draft.DefinitionJson = definitionJson;
        draft.UpdatedAt = DateTime.UtcNow;
        draft.UpdatedByPersonId = updatedByPersonId;
        await _context.SaveChangesAsync(ct);
        return draft;
    }

    public async Task<ApplicationForm> PublishDraftAsync(string name, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var draft = await _context.ApplicationFormDrafts.FirstOrDefaultAsync(item => item.Name == name, ct)
            ?? throw new InvalidOperationException($"Form draft '{name}' not found.");
        var published = await AddVersionCoreAsync(name, draft.DefinitionJson, ct);
        _context.ApplicationFormDrafts.Remove(draft);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return published;
    }

    public async Task<ApplicationForm> AddVersionAsync(string name, string definitionJson, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var newForm = await AddVersionCoreAsync(name, definitionJson, ct);
        await transaction.CommitAsync(ct);
        return newForm;
    }

    private async Task<ApplicationForm> AddVersionCoreAsync(
        string name,
        string definitionJson,
        CancellationToken ct)
    {
        var existing = await _context.ApplicationForms
            .Where(form => form.Name == name)
            .ToListAsync(ct);

        foreach (var form in existing)
            form.IsActive = false;

        var nextVersion = existing.Count == 0 ? 1 : existing.Max(form => form.Version) + 1;
        var newForm = new ApplicationForm
        {
            Id = Guid.NewGuid(),
            Name = name,
            Version = nextVersion,
            DefinitionJson = definitionJson,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ApplicationForms.Add(newForm);
        await _context.SaveChangesAsync(ct);
        return newForm;
    }
}
