using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Interfaces;

public interface IFieldCorrectionRepository
{
    Task<FieldCorrectionRequest?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<FieldCorrectionRequest?> GetPendingAsync(Guid citizenId, Guid fieldDefinitionId, CancellationToken ct);
    Task<IReadOnlyList<FieldCorrectionRequest>> ListByCitizenAsync(Guid citizenId, CancellationToken ct);
    Task<IReadOnlyList<FieldCorrectionRequest>> ListAsync(
        FieldCorrectionStatus? status,
        int skip,
        int take,
        CancellationToken ct);
    Task<FieldCorrectionRequest> AddAsync(FieldCorrectionRequest request, CancellationToken ct);
    Task<FieldCorrectionRequest> UpdateAsync(FieldCorrectionRequest request, CancellationToken ct);
}