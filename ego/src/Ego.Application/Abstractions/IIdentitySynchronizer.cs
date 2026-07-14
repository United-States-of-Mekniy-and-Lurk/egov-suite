using Ego.Application.Models;
using Ego.Domain.Entities;

namespace Ego.Application.Abstractions;

public interface IIdentitySynchronizer
{
    Task<Person> SynchronizeAsync(IdentityClaims claims, CancellationToken ct = default);
}
