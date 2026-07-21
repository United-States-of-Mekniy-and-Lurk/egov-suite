using CitizenService.Application.Interfaces;
using CitizenService.Application.Models;
using CitizenService.Domain.Entities;
using CitizenService.Domain.Enums;

namespace CitizenService.Application.Services;

public sealed class FieldCorrectionService
{
    private readonly IFieldCorrectionRepository _correctionRepository;
    private readonly IRegistryFieldRepository _registryRepository;
    private readonly ICitizenRepository _citizenRepository;
    private readonly RegistryFieldService _registryFieldService;
    private readonly ICurrentActor _currentActor;
    private readonly IUnitOfWork _unitOfWork;

    public FieldCorrectionService(
        IFieldCorrectionRepository correctionRepository,
        IRegistryFieldRepository registryRepository,
        ICitizenRepository citizenRepository,
        RegistryFieldService registryFieldService,
        ICurrentActor currentActor,
        IUnitOfWork unitOfWork)
    {
        _correctionRepository = correctionRepository;
        _registryRepository = registryRepository;
        _citizenRepository = citizenRepository;
        _registryFieldService = registryFieldService;
        _currentActor = currentActor;
        _unitOfWork = unitOfWork;
    }

    public async Task<FieldCorrectionRequestDto> SubmitAsync(
        Guid personId,
        string fieldKey,
        SubmitFieldCorrectionInput input,
        CancellationToken ct)
    {
        if (_currentActor.PersonId != personId)
            throw new UnauthorizedAccessException("Correction requests can only be submitted for your own record.");
        if (string.IsNullOrWhiteSpace(input.RequestReason))
            throw new ArgumentException("A reason for the correction is required.");

        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {personId}.");
        var definition = await _registryRepository.GetDefinitionByKeyAsync(fieldKey, ct)
            ?? throw new KeyNotFoundException($"Registry field '{fieldKey}' was not found.");
        if (!definition.IsActive || !definition.UserEditable)
            throw new ArgumentException($"Registry field '{fieldKey}' does not accept citizen correction requests.");
        if (await _correctionRepository.GetPendingAsync(citizen.Id, definition.Id, ct) != null)
            throw new InvalidOperationException("A correction request for this field is already awaiting review.");

        var proposedValue = _registryFieldService.NormalizeValue(definition, input.ProposedValue);
        var currentValue = await _registryRepository.GetValueAsync(citizen.Id, definition.Id, ct);
        if (currentValue?.Value == proposedValue)
            throw new ArgumentException("The proposed value is already current.");

        var now = DateTime.UtcNow;
        var request = new FieldCorrectionRequest
        {
            Id = Guid.NewGuid(),
            CitizenId = citizen.Id,
            FieldDefinitionId = definition.Id,
            RequestedByPersonId = personId,
            CurrentValue = currentValue?.Value,
            ProposedValue = proposedValue,
            RequestReason = input.RequestReason.Trim(),
            Status = FieldCorrectionStatus.Submitted,
            SubmittedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _correctionRepository.AddAsync(request, ct);
        return ToDto(request, citizen, definition);
    }

    public async Task<IReadOnlyList<FieldCorrectionRequestDto>> ListForPersonAsync(
        Guid personId,
        CancellationToken ct)
    {
        var citizen = await _citizenRepository.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Citizen not found for PersonId {personId}.");
        var requests = await _correctionRepository.ListByCitizenAsync(citizen.Id, ct);
        return await MapAsync(requests, ct);
    }

    public async Task<IReadOnlyList<FieldCorrectionRequestDto>> ListAsync(
        FieldCorrectionStatus? status,
        int skip,
        int take,
        CancellationToken ct)
        => await MapAsync(await _correctionRepository.ListAsync(status, skip, take, ct), ct);

    public async Task<FieldCorrectionRequestDto> ApproveAsync(
        Guid requestId,
        ReviewFieldCorrectionInput input,
        CancellationToken ct)
    {
        ValidateReviewReason(input);
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var request = await GetSubmittedAsync(requestId, ct);
            var citizen = await _citizenRepository.GetByIdAsync(request.CitizenId, ct)
                ?? throw new KeyNotFoundException($"Citizen {request.CitizenId} was not found.");
            var definition = await _registryRepository.GetDefinitionByIdAsync(request.FieldDefinitionId, ct)
                ?? throw new KeyNotFoundException($"Registry field {request.FieldDefinitionId} was not found.");

            await _registryFieldService.SetCitizenFieldAsync(
                citizen.PersonId,
                definition.Key,
                request.ProposedValue,
                null,
                request.Id,
                ct);
            CompleteReview(request, FieldCorrectionStatus.Approved, input.Reason);
            await _correctionRepository.UpdateAsync(request, ct);
            return ToDto(request, citizen, definition);
        }, ct);
    }

    public async Task<FieldCorrectionRequestDto> RejectAsync(
        Guid requestId,
        ReviewFieldCorrectionInput input,
        CancellationToken ct)
    {
        ValidateReviewReason(input);
        var request = await GetSubmittedAsync(requestId, ct);
        var citizen = await _citizenRepository.GetByIdAsync(request.CitizenId, ct)
            ?? throw new KeyNotFoundException($"Citizen {request.CitizenId} was not found.");
        var definition = await _registryRepository.GetDefinitionByIdAsync(request.FieldDefinitionId, ct)
            ?? throw new KeyNotFoundException($"Registry field {request.FieldDefinitionId} was not found.");
        CompleteReview(request, FieldCorrectionStatus.Rejected, input.Reason);
        await _correctionRepository.UpdateAsync(request, ct);
        return ToDto(request, citizen, definition);
    }

    private async Task<FieldCorrectionRequest> GetSubmittedAsync(Guid requestId, CancellationToken ct)
    {
        var request = await _correctionRepository.GetByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"Correction request {requestId} was not found.");
        if (request.Status != FieldCorrectionStatus.Submitted)
            throw new InvalidOperationException("Only submitted correction requests can be reviewed.");
        return request;
    }

    private void CompleteReview(
        FieldCorrectionRequest request,
        FieldCorrectionStatus status,
        string reason)
    {
        var now = DateTime.UtcNow;
        request.Status = status;
        request.ReviewedByPersonId = _currentActor.PersonId;
        request.ReviewedAt = now;
        request.ReviewReason = reason.Trim();
        request.Version++;
        request.UpdatedAt = now;
    }

    private async Task<IReadOnlyList<FieldCorrectionRequestDto>> MapAsync(
        IReadOnlyList<FieldCorrectionRequest> requests,
        CancellationToken ct)
    {
        var result = new List<FieldCorrectionRequestDto>(requests.Count);
        foreach (var request in requests)
        {
            var citizen = await _citizenRepository.GetByIdAsync(request.CitizenId, ct)
                ?? throw new KeyNotFoundException($"Citizen {request.CitizenId} was not found.");
            var definition = await _registryRepository.GetDefinitionByIdAsync(request.FieldDefinitionId, ct)
                ?? throw new KeyNotFoundException($"Registry field {request.FieldDefinitionId} was not found.");
            result.Add(ToDto(request, citizen, definition));
        }
        return result;
    }

    private static FieldCorrectionRequestDto ToDto(
        FieldCorrectionRequest request,
        Citizen citizen,
        RegistryFieldDefinition definition)
        => new(
            request.Id,
            request.CitizenId,
            citizen.PersonId,
            RegistryFieldService.ToDto(definition),
            request.CurrentValue,
            request.ProposedValue,
            request.RequestReason,
            request.Status,
            request.RequestedByPersonId,
            request.SubmittedAt,
            request.ReviewedByPersonId,
            request.ReviewedAt,
            request.ReviewReason);

    private static void ValidateReviewReason(ReviewFieldCorrectionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason))
            throw new ArgumentException("A review reason is required.");
    }
}