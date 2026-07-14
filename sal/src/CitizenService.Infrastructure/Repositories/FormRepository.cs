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

    public async Task<IEnumerable<ApplicationForm>> ListFormsAsync(CancellationToken ct)
        => await _context.ApplicationForms.OrderBy(f => f.Name).ThenByDescending(f => f.Version).ToListAsync(ct);
}
