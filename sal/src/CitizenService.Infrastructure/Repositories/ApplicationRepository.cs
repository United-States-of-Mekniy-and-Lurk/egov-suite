using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly CitizenDbContext _context;

    public ApplicationRepository(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<CitizenshipApplication?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Applications.FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<CitizenshipApplication>> ListByPersonIdAsync(Guid personId, CancellationToken ct)
        => await _context.Applications.Where(a => a.PersonId == personId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

    public async Task<IEnumerable<CitizenshipApplication>> ListAsync(ApplicationStatus? statusFilter, int skip, int take, CancellationToken ct)
    {
        var query = _context.Applications.AsQueryable();
        if (statusFilter.HasValue)
            query = query.Where(a => a.Status == statusFilter.Value);
        return await query.OrderByDescending(a => a.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<CitizenshipApplication> AddAsync(CitizenshipApplication application, CancellationToken ct)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync(ct);
        return application;
    }

    public async Task<CitizenshipApplication> UpdateAsync(CitizenshipApplication application, CancellationToken ct)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync(ct);
        return application;
    }

    public async Task<int> CountByStatusAsync(ApplicationStatus status, CancellationToken ct)
        => await _context.Applications.CountAsync(a => a.Status == status, ct);

    public async Task<IEnumerable<ApplicationTransition>> GetTransitionsAsync(Guid applicationId, CancellationToken ct)
        => await _context.ApplicationTransitions.Where(t => t.ApplicationId == applicationId).OrderBy(t => t.ChangedAt).ToListAsync(ct);

    public async Task AddTransitionAsync(ApplicationTransition transition, CancellationToken ct)
    {
        _context.ApplicationTransitions.Add(transition);
        await _context.SaveChangesAsync(ct);
    }
}
