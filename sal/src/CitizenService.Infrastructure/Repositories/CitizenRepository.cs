using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Infrastructure.Repositories;

public class CitizenRepository : ICitizenRepository
{
    private readonly CitizenDbContext _context;

    public CitizenRepository(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<Citizen?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Citizens.FindAsync(new object[] { id }, ct);

    public async Task<Citizen?> GetByPersonIdAsync(Guid personId, CancellationToken ct)
        => await _context.Citizens.FirstOrDefaultAsync(c => c.PersonId == personId, ct);

    public async Task<IEnumerable<Citizen>> ListAsync(int skip, int take, CancellationToken ct)
        => await _context.Citizens.OrderBy(c => c.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);

    public async Task<Citizen> AddAsync(Citizen citizen, CancellationToken ct)
    {
        _context.Citizens.Add(citizen);
        await _context.SaveChangesAsync(ct);
        return citizen;
    }

    public async Task<Citizen> UpdateAsync(Citizen citizen, CancellationToken ct)
    {
        _context.Citizens.Update(citizen);
        await _context.SaveChangesAsync(ct);
        return citizen;
    }

    public async Task<int> CountAsync(CancellationToken ct)
        => await _context.Citizens.CountAsync(ct);
}
