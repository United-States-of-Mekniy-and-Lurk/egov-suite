using CitizenService.Domain.Entities;

namespace CitizenService.Application.Interfaces;

public interface IFormRepository
{
    Task<ApplicationForm?> GetFormAsync(string name, int version, CancellationToken ct);
    Task<ApplicationForm?> GetLatestFormAsync(string name, CancellationToken ct);
    Task<ApplicationFormDraft?> GetDraftAsync(string name, CancellationToken ct);
    Task<IEnumerable<ApplicationForm>> ListFormsAsync(CancellationToken ct);
    Task<ApplicationFormDraft> SaveDraftAsync(
        string name, string definitionJson, Guid updatedByPersonId, CancellationToken ct);
    Task<ApplicationForm> PublishDraftAsync(string name, CancellationToken ct);
    Task<ApplicationForm> AddVersionAsync(string name, string definitionJson, CancellationToken ct);
}
