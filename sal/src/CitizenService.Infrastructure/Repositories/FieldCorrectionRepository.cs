using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Infrastructure.Repositories;

public sealed class FieldCorrectionRepository : IFieldCorrectionRepository
{
    private readonly CitizenDbContext _context;

    public FieldCorrectionRepository(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<FieldCorrectionRequest?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.FieldCorrectionRequests.FindAsync([id], ct);

    public async Task<FieldCorrectionRequest?> GetPendingAsync(
        Guid citizenId,
        Guid fieldDefinitionId,
        CancellationToken ct)
        => await _context.FieldCorrectionRequests.FirstOrDefaultAsync(request =>
            request.CitizenId == citizenId &&
            request.FieldDefinitionId == fieldDefinitionId &&
            request.Status == FieldCorrectionStatus.Submitted, ct);

    public async Task<IReadOnlyList<FieldCorrectionRequest>> ListByCitizenAsync(
        Guid citizenId,
        CancellationToken ct)
        => await _context.FieldCorrectionRequests
            .Where(request => request.CitizenId == citizenId)
            .OrderByDescending(request => request.SubmittedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FieldCorrectionRequest>> ListAsync(
        FieldCorrectionStatus? status,
        int skip,
        int take,
        CancellationToken ct)
    {
        var query = _context.FieldCorrectionRequests.AsQueryable();
        if (status.HasValue)
            query = query.Where(request => request.Status == status.Value);
        return await query
            .OrderBy(request => request.Status == FieldCorrectionStatus.Submitted ? 0 : 1)
            .ThenBy(request => request.SubmittedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);
    }

    public async Task<FieldCorrectionRequest> AddAsync(FieldCorrectionRequest request, CancellationToken ct)
    {
        _context.FieldCorrectionRequests.Add(request);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "A correction request for this field is already awaiting review.", exception);
        }
        return request;
    }

    public async Task<FieldCorrectionRequest> UpdateAsync(FieldCorrectionRequest request, CancellationToken ct)
    {
        _context.FieldCorrectionRequests.Update(request);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException(
                "The correction request was already reviewed by another clerk.", exception);
        }
        return request;
    }
}