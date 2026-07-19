using System.Text.Json;
using CitizenService.Application.Interfaces;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;
using CitizenService.Domain.StateMachine;

namespace CitizenService.Application.Services;

public class ApplicationAppService
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IFormRepository _formRepository;
    private readonly ICurrentActor _currentActor;
    private readonly IPersonClient _personClient;

    public ApplicationAppService(
        IApplicationRepository applicationRepository,
        IFormRepository formRepository,
        ICurrentActor currentActor,
        IPersonClient personClient)
    {
        _applicationRepository = applicationRepository;
        _formRepository = formRepository;
        _currentActor = currentActor;
        _personClient = personClient;
    }

    public async Task<CitizenshipApplication> CreateDraftAsync(Guid personId, string formName, int formVersion, CancellationToken ct)
    {
        var form = await _formRepository.GetFormAsync(formName, formVersion, ct)
            ?? throw new InvalidOperationException($"Form '{formName}' version {formVersion} not found.");

        var application = new CitizenshipApplication
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            Status = ApplicationStatus.Draft,
            FormName = formName,
            FormVersion = formVersion,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByPersonId = _currentActor.PersonId
        };

        return await _applicationRepository.AddAsync(application, ct);
    }

    public async Task<CitizenshipApplication> SaveAnswersAsync(Guid applicationId, JsonDocument answers, CancellationToken ct)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        application.FormAnswers?.Dispose();
        application.FormAnswers = answers;
        application.UpdatedAt = DateTime.UtcNow;

        return await _applicationRepository.UpdateAsync(application, ct);
    }

    public async Task<CitizenshipApplication> TransitionAsync(Guid applicationId, ApplicationStatus targetStatus, string? reason, CancellationToken ct)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        if (!ApplicationStateMachine.IsValidTransition(application.Status, targetStatus))
            throw new InvalidOperationException($"Cannot transition from {application.Status} to {targetStatus}.");

        var transition = new ApplicationTransition
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            FromStatus = application.Status,
            ToStatus = targetStatus,
            ChangedByPersonId = _currentActor.PersonId,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };

        await _applicationRepository.AddTransitionAsync(transition, ct);

        application.Status = targetStatus;
        application.UpdatedAt = DateTime.UtcNow;

        if (targetStatus == ApplicationStatus.Submitted)
            application.SubmittedAt = DateTime.UtcNow;

        if (targetStatus is ApplicationStatus.Approved or ApplicationStatus.Rejected)
        {
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewerPersonId = _currentActor.PersonId;
            application.DecisionReason = reason;
        }

        return await _applicationRepository.UpdateAsync(application, ct);
    }

    public async Task<IEnumerable<CitizenshipApplication>> ListAsync(ApplicationStatus? statusFilter, int skip, int take, CancellationToken ct)
        => await _applicationRepository.ListAsync(statusFilter, skip, take, ct);

    public async Task<IEnumerable<CitizenshipApplication>> ListByPersonIdAsync(Guid personId, CancellationToken ct)
        => await _applicationRepository.ListByPersonIdAsync(personId, ct);

    public async Task<CitizenshipApplication?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _applicationRepository.GetByIdAsync(id, ct);
}
