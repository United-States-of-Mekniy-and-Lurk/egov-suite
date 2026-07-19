using CitizenService.Application.Interfaces;

namespace CitizenService.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly CitizenDbContext _context;

    public EfUnitOfWork(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}