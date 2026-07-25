using ElectionService.Application.Abstractions;
using ElectionService.Application.Exceptions;
using ElectionService.Application.Models;
using ElectionService.Domain.Entities;
using ElectionService.Domain.Enums;
using ElectionService.Domain.StateMachine;
using Egov.Platform.Identity;

namespace ElectionService.Application.Services;

public sealed class AdminElectionService(
    IElectionStore store,
    ICredentialHashService hashes,
    IOrganizationRegistryClient organizations,
    IPersonRegistryClient persons,
    ICurrentActor actor)
{
    public async Task<IReadOnlyList<AdminElectionView>> ListAsync(CancellationToken ct)
    {
        EnsureAdmin();
        return (await store.ListAllAsync(ct)).Select(ToAdminView).ToList();
    }

    public async Task<AdminElectionView> GetAsync(Guid electionId, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found.");
        return ToAdminView(election);
    }

    public async Task<AdminElectionView> CreateAsync(ElectionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        ValidateElectionInput(input);
        var slug = NormalizeSlug(input.Slug);
        if (await store.SlugExistsAsync(slug, null, ct))
            throw new ElectionConflictException("Election slug already exists.");

        var now = DateTime.UtcNow;
        var election = new Election
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Type = input.Type,
            EligibilityMode = input.EligibilityMode,
            CredentialHashKeyVersion = hashes.ActiveKeyVersion,
            VotingStartsAt = input.VotingStartsAt.ToUniversalTime(),
            VotingEndsAt = input.VotingEndsAt.ToUniversalTime(),
            TerritoryCode = NormalizeOptional(input.TerritoryCode),
            EligibleVoterCount = input.EligibleVoterCount,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByPersonId = actor.PersonId
        };
        await store.AddElectionAsync(election, ct);
        return ToAdminView(election);
    }

    public async Task<AdminElectionView> UpdateAsync(Guid electionId, ElectionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        ValidateElectionInput(input);
        var election = await DraftAsync(electionId, ct);
        var slug = NormalizeSlug(input.Slug);
        if (await store.SlugExistsAsync(slug, electionId, ct))
            throw new ElectionConflictException("Election slug already exists.");
        if (election.Type != input.Type && (election.PartyLists.Count != 0 || election.ReferendumOptions.Count != 0))
            throw new ElectionValidationException("Election type cannot change after voting targets are added.");

        election.Slug = slug;
        election.Title = input.Title.Trim();
        election.Description = input.Description.Trim();
        election.Type = input.Type;
        election.EligibilityMode = input.EligibilityMode;
        election.VotingStartsAt = input.VotingStartsAt.ToUniversalTime();
        election.VotingEndsAt = input.VotingEndsAt.ToUniversalTime();
        election.TerritoryCode = NormalizeOptional(input.TerritoryCode);
        election.EligibleVoterCount = input.EligibleVoterCount;
        election.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return ToAdminView(election);
    }

    public async Task<AdminElectionView> ImportHistoricalAsync(HistoricalElectionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        ValidateHistoricalInput(input);
        var slug = NormalizeSlug(input.Slug);
        if (await store.SlugExistsAsync(slug, null, ct))
            throw new ElectionConflictException("Election slug already exists.");

        var now = DateTime.UtcNow;
        var votingStartsAt = input.VotingStartsAt.ToUniversalTime();
        var votingEndsAt = input.VotingEndsAt.ToUniversalTime();
        var election = new Election
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Type = input.Type,
            Status = ElectionStatus.Archived,
            EligibilityMode = EligibilityMode.AllActiveCitizens,
            CredentialHashKeyVersion = hashes.ActiveKeyVersion,
            VotingStartsAt = votingStartsAt,
            VotingEndsAt = votingEndsAt,
            TerritoryCode = NormalizeOptional(input.TerritoryCode),
            EligibleVoterCount = input.EligibleVoterCount,
            HistoricalParticipatingVoterCount = input.ParticipatingVoterCount,
            HistoricalInvalidBallotCount = input.InvalidBallotCount,
            IsHistorical = true,
            HistoricalSourceReference = input.SourceReference.Trim(),
            ImportedAt = now,
            ImportedByPersonId = actor.PersonId,
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = votingStartsAt,
            ClosedAt = votingEndsAt,
            FinalizedAt = votingEndsAt,
            CreatedByPersonId = actor.PersonId
        };
        var results = new List<ElectionResult>();
        foreach (var item in input.PartyLists ?? [])
        {
            var partyList = new PartyList
            {
                Id = Guid.NewGuid(),
                ElectionId = election.Id,
                Election = election,
                PartyOrganizationId = item.PartyOrganizationId,
                PartyRegistrationNumber = item.PartyRegistrationNumber.Trim(),
                PartyName = item.PartyName.Trim(),
                ListName = item.ListName.Trim(),
                SortOrder = item.SortOrder
            };
            foreach (var candidateInput in item.Candidates ?? [])
                partyList.Candidates.Add(new Candidate
                {
                    Id = Guid.NewGuid(),
                    PartyListId = partyList.Id,
                    PartyList = partyList,
                    PersonId = candidateInput.PersonId,
                    DisplayName = candidateInput.DisplayName.Trim(),
                    Description = NormalizeOptional(candidateInput.Description),
                    Position = candidateInput.Position
                });
            election.PartyLists.Add(partyList);
            results.Add(NewHistoricalResult(election, SelectionType.PartyList, partyList.Id, partyList.ListName, item.VoteCount, votingEndsAt));
        }
        foreach (var item in input.ReferendumOptions ?? [])
        {
            var option = new ReferendumOption
            {
                Id = Guid.NewGuid(), ElectionId = election.Id, Election = election,
                Code = item.Code.Trim(), Label = item.Label.Trim(),
                Description = NormalizeOptional(item.Description), SortOrder = item.SortOrder
            };
            election.ReferendumOptions.Add(option);
            results.Add(NewHistoricalResult(election, SelectionType.ReferendumOption, option.Id, option.Label, item.VoteCount, votingEndsAt));
        }
        var transition = new ElectionTransition
        {
            Id = Guid.NewGuid(), ElectionId = election.Id,
            FromStatus = ElectionStatus.Finalized, ToStatus = ElectionStatus.Archived,
            ChangedByPersonId = actor.PersonId, ChangedAt = now,
            Reason = $"Historical import: {election.HistoricalSourceReference}"
        };
        await store.ImportHistoricalAsync(election, results, transition, ct);
        return ToAdminView(election);
    }

    public async Task<PartyListView> AddPartyListAsync(Guid electionId, PartyListInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        if (election.Type != ElectionType.PartyList)
            throw new ElectionValidationException("Party lists are only valid for party-list elections.");
        if (string.IsNullOrWhiteSpace(input.ListName))
            throw new ElectionValidationException("List name is required.");
        if (election.PartyLists.Any(item => item.PartyOrganizationId == input.PartyOrganizationId || item.SortOrder == input.SortOrder))
            throw new ElectionConflictException("Party organization and sort order must be unique in an election.");

        var organization = await organizations.GetAsync(input.PartyOrganizationId, ct)
            ?? throw new ElectionValidationException("Party organization was not found.");
        if (organization.Status != "Active" || !organization.ClassificationCodes.Contains("political-party", StringComparer.OrdinalIgnoreCase))
            throw new ElectionValidationException("Organization must be active and classified as political-party.");
        var partyList = new PartyList
        {
            Id = Guid.NewGuid(),
            ElectionId = electionId,
            PartyOrganizationId = organization.Id,
            PartyRegistrationNumber = organization.RegistrationNumber,
            PartyName = organization.LegalName,
            ListName = input.ListName.Trim(),
            SortOrder = input.SortOrder
        };
        await store.AddPartyListAsync(partyList, ct);
        return new PartyListView(partyList.Id, partyList.PartyOrganizationId, partyList.PartyRegistrationNumber,
            partyList.PartyName, partyList.ListName, partyList.SortOrder, []);
    }

    public async Task<PartyListView> UpdatePartyListAsync(Guid electionId, Guid partyListId, PartyListInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var partyList = election.PartyLists.SingleOrDefault(item => item.Id == partyListId)
            ?? throw new ElectionNotFoundException("Party list was not found.");
        if (string.IsNullOrWhiteSpace(input.ListName))
            throw new ElectionValidationException("List name is required.");
        if (election.PartyLists.Any(item => item.Id != partyListId &&
            (item.PartyOrganizationId == input.PartyOrganizationId || item.SortOrder == input.SortOrder)))
            throw new ElectionConflictException("Party organization and sort order must be unique in an election.");

        var organization = await GetPoliticalPartyAsync(input.PartyOrganizationId, ct);
        partyList.PartyOrganizationId = organization.Id;
        partyList.PartyRegistrationNumber = organization.RegistrationNumber;
        partyList.PartyName = organization.LegalName;
        partyList.ListName = input.ListName.Trim();
        partyList.SortOrder = input.SortOrder;
        election.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return new PartyListView(partyList.Id, partyList.PartyOrganizationId, partyList.PartyRegistrationNumber,
            partyList.PartyName, partyList.ListName, partyList.SortOrder,
            partyList.Candidates.OrderBy(item => item.Position).Select(ToView).ToList());
    }

    public async Task DeletePartyListAsync(Guid electionId, Guid partyListId, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var partyList = election.PartyLists.SingleOrDefault(item => item.Id == partyListId)
            ?? throw new ElectionNotFoundException("Party list was not found.");
        await store.RemovePartyListAsync(partyList, ct);
    }

    public async Task<CandidateView> AddCandidateAsync(Guid electionId, Guid partyListId, CandidateInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var partyList = election.PartyLists.SingleOrDefault(item => item.Id == partyListId)
            ?? throw new ElectionNotFoundException("Party list was not found.");
        if (partyList.Candidates.Any(item => item.Position == input.Position || input.PersonId.HasValue && item.PersonId == input.PersonId))
            throw new ElectionConflictException("Candidate person and position must be unique in a party list.");

        string displayName;
        if (input.PersonId.HasValue)
        {
            var person = await persons.GetAsync(input.PersonId.Value, ct)
                ?? throw new ElectionValidationException("Candidate person was not found.");
            displayName = person.DisplayName;
        }
        else
        {
            displayName = input.DisplayName?.Trim() ?? string.Empty;
            if (displayName.Length == 0)
                throw new ElectionValidationException("Display name is required when PersonId is not supplied.");
        }

        var candidate = new Candidate
        {
            Id = Guid.NewGuid(),
            PartyListId = partyListId,
            PersonId = input.PersonId,
            DisplayName = displayName,
            Description = NormalizeOptional(input.Description),
            Position = input.Position
        };
        await store.AddCandidateAsync(candidate, ct);
        return ToView(candidate);
    }

    public async Task<CandidateView> UpdateCandidateAsync(Guid electionId, Guid partyListId, Guid candidateId, CandidateInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var partyList = election.PartyLists.SingleOrDefault(item => item.Id == partyListId)
            ?? throw new ElectionNotFoundException("Party list was not found.");
        var candidate = partyList.Candidates.SingleOrDefault(item => item.Id == candidateId)
            ?? throw new ElectionNotFoundException("Candidate was not found.");
        if (partyList.Candidates.Any(item => item.Id != candidateId &&
            (item.Position == input.Position || input.PersonId.HasValue && item.PersonId == input.PersonId)))
            throw new ElectionConflictException("Candidate person and position must be unique in a party list.");

        candidate.PersonId = input.PersonId;
        candidate.DisplayName = await ResolveCandidateDisplayNameAsync(input, ct);
        candidate.Description = NormalizeOptional(input.Description);
        candidate.Position = input.Position;
        election.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return ToView(candidate);
    }

    public async Task DeleteCandidateAsync(Guid electionId, Guid partyListId, Guid candidateId, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var partyList = election.PartyLists.SingleOrDefault(item => item.Id == partyListId)
            ?? throw new ElectionNotFoundException("Party list was not found.");
        var candidate = partyList.Candidates.SingleOrDefault(item => item.Id == candidateId)
            ?? throw new ElectionNotFoundException("Candidate was not found.");
        await store.RemoveCandidateAsync(candidate, ct);
    }

    public async Task<ReferendumOptionView> AddReferendumOptionAsync(Guid electionId, ReferendumOptionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        if (election.Type != ElectionType.Referendum)
            throw new ElectionValidationException("Referendum options are only valid for referendum elections.");
        var code = input.Code.Trim();
        if (code.Length == 0 || string.IsNullOrWhiteSpace(input.Label))
            throw new ElectionValidationException("Option code and label are required.");
        if (election.ReferendumOptions.Any(item => item.Code.Equals(code, StringComparison.OrdinalIgnoreCase) || item.SortOrder == input.SortOrder))
            throw new ElectionConflictException("Option code and sort order must be unique in an election.");
        var option = new ReferendumOption
        {
            Id = Guid.NewGuid(), ElectionId = electionId, Code = code, Label = input.Label.Trim(),
            Description = NormalizeOptional(input.Description), SortOrder = input.SortOrder
        };
        await store.AddReferendumOptionAsync(option, ct);
        return new ReferendumOptionView(option.Id, option.Code, option.Label, option.Description, option.SortOrder);
    }

    public async Task<ReferendumOptionView> UpdateReferendumOptionAsync(Guid electionId, Guid optionId, ReferendumOptionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var option = election.ReferendumOptions.SingleOrDefault(item => item.Id == optionId)
            ?? throw new ElectionNotFoundException("Referendum option was not found.");
        var code = input.Code.Trim();
        if (code.Length == 0 || string.IsNullOrWhiteSpace(input.Label))
            throw new ElectionValidationException("Option code and label are required.");
        if (election.ReferendumOptions.Any(item => item.Id != optionId &&
            (item.Code.Equals(code, StringComparison.OrdinalIgnoreCase) || item.SortOrder == input.SortOrder)))
            throw new ElectionConflictException("Option code and sort order must be unique in an election.");
        option.Code = code;
        option.Label = input.Label.Trim();
        option.Description = NormalizeOptional(input.Description);
        option.SortOrder = input.SortOrder;
        election.UpdatedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return new ReferendumOptionView(option.Id, option.Code, option.Label, option.Description, option.SortOrder);
    }

    public async Task DeleteReferendumOptionAsync(Guid electionId, Guid optionId, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        var option = election.ReferendumOptions.SingleOrDefault(item => item.Id == optionId)
            ?? throw new ElectionNotFoundException("Referendum option was not found.");
        await store.RemoveReferendumOptionAsync(option, ct);
    }

    public async Task<IReadOnlyList<VoterRollEntryView>> ListVoterRollAsync(Guid electionId, CancellationToken ct)
    {
        EnsureAdmin();
        await RequireElectionAsync(electionId, ct);
        return (await store.ListVoterRollAsync(electionId, ct))
            .Select(item => new VoterRollEntryView(item.PersonId, item.AddedAt, item.AddedByPersonId)).ToList();
    }

    public async Task AddVoterAsync(Guid electionId, VoterRollInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        if (election.EligibilityMode != EligibilityMode.SpecificVoterRoll)
            throw new ElectionValidationException("Voter roll entries require SpecificVoterRoll eligibility.");
        if (await store.IsOnVoterRollAsync(electionId, input.PersonId, ct))
            throw new ElectionConflictException("Person is already on the voter roll.");
        await store.AddVoterRollEntryAsync(new VoterRollEntry
        {
            ElectionId = electionId, PersonId = input.PersonId, AddedAt = DateTime.UtcNow, AddedByPersonId = actor.PersonId
        }, ct);
    }

    public async Task<int> BulkAddVotersAsync(Guid electionId, BulkVoterRollInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await DraftAsync(electionId, ct);
        if (election.EligibilityMode != EligibilityMode.SpecificVoterRoll)
            throw new ElectionValidationException("Voter roll entries require SpecificVoterRoll eligibility.");
        if (input.PersonIds.Count == 0)
            throw new ElectionValidationException("At least one person ID is required.");
        var personIds = input.PersonIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var existing = (await store.ListVoterRollAsync(electionId, ct)).Select(item => item.PersonId).ToHashSet();
        var now = DateTime.UtcNow;
        var entries = personIds.Where(id => !existing.Contains(id)).Select(id => new VoterRollEntry
        {
            ElectionId = electionId, PersonId = id, AddedAt = now, AddedByPersonId = actor.PersonId
        }).ToList();
        await store.AddVoterRollEntriesAsync(entries, ct);
        return entries.Count;
    }

    public async Task RemoveVoterAsync(Guid electionId, Guid personId, CancellationToken ct)
    {
        EnsureAdmin();
        await DraftAsync(electionId, ct);
        if (!await store.RemoveVoterRollEntryAsync(electionId, personId, ct))
            throw new ElectionNotFoundException("Voter roll entry was not found.");
    }

    public async Task<IReadOnlyList<InvitationAdminView>> ListInvitationsAsync(Guid electionId, CancellationToken ct)
    {
        EnsureAdmin();
        await RequireElectionAsync(electionId, ct);
        return (await store.ListInvitationsAsync(electionId, ct)).Select(ToAdminView).ToList();
    }

    public async Task<InvitationCreated> CreateInvitationAsync(Guid electionId, InvitationInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await RequireInvitationMutableAsync(electionId, ct);
        var generated = hashes.CreateInvitation(electionId, election.CredentialHashKeyVersion);
        var invitation = new VotingInvitation
        {
            Id = Guid.NewGuid(), ElectionId = electionId, TokenHash = generated.Hash,
            Label = NormalizeOptional(input.Label), PersonId = input.PersonId,
            CreatedAt = DateTime.UtcNow, CreatedByPersonId = actor.PersonId
        };
        await store.AddInvitationAsync(invitation, ct);
        return new InvitationCreated(invitation.Id, generated.Token, invitation.Label);
    }

    public async Task<IReadOnlyList<InvitationCreated>> BulkCreateInvitationsAsync(Guid electionId, BulkInvitationInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await RequireInvitationMutableAsync(electionId, ct);
        if (input.Items.Count == 0 || input.Items.Count > 500)
            throw new ElectionValidationException("Bulk invitation input must contain between 1 and 500 items.");
        var now = DateTime.UtcNow;
        var results = new List<InvitationCreated>(input.Items.Count);
        var invitations = new List<VotingInvitation>(input.Items.Count);
        foreach (var item in input.Items)
        {
            var generated = hashes.CreateInvitation(electionId, election.CredentialHashKeyVersion);
            var invitation = new VotingInvitation
            {
                Id = Guid.NewGuid(), ElectionId = electionId, TokenHash = generated.Hash,
                Label = NormalizeOptional(item.Label), PersonId = item.PersonId,
                CreatedAt = now, CreatedByPersonId = actor.PersonId
            };
            invitations.Add(invitation);
            results.Add(new InvitationCreated(invitation.Id, generated.Token, invitation.Label));
        }
        await store.AddInvitationsAsync(invitations, ct);
        return results;
    }

    public async Task<InvitationAdminView> RevokeInvitationAsync(Guid electionId, Guid invitationId, CancellationToken ct)
    {
        EnsureAdmin();
        await RequireInvitationMutableAsync(electionId, ct);
        var invitation = await store.GetInvitationByIdAsync(electionId, invitationId, ct)
            ?? throw new ElectionNotFoundException("Invitation was not found.");
        if (invitation.UsedOn is not null)
            throw new ElectionConflictException("A used invitation cannot be revoked.");
        if (invitation.RevokedAt is not null)
            throw new ElectionConflictException("Invitation has already been revoked.");
        invitation.RevokedAt = DateTime.UtcNow;
        await store.SaveChangesAsync(ct);
        return ToAdminView(invitation);
    }

    public async Task<AdminElectionView> TransitionAsync(Guid electionId, TransitionInput input, CancellationToken ct)
    {
        EnsureAdmin();
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        if (!ElectionStateMachine.IsValidTransition(election.Status, input.Status))
            throw new ElectionValidationException($"Transition from {election.Status} to {input.Status} is not allowed.");
        if (input.Status == ElectionStatus.Published)
            ValidateReadyToPublish(election);
        var now = DateTime.UtcNow;
        if (input.Status == ElectionStatus.Finalized)
            await store.FinalizeAsync(electionId, actor.PersonId, input.Reason, now, ct);
        else
            await store.TransitionAsync(electionId, input.Status, actor.PersonId, input.Reason, now, ct);
        var updated = await store.GetAsync(electionId, ct)
            ?? throw new ElectionNotFoundException("Election was not found after the transition.");
        return ToAdminView(updated);
    }

    private async Task<Election> DraftAsync(Guid electionId, CancellationToken ct)
    {
        var election = await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");
        if (election.Status != ElectionStatus.Draft)
            throw new ElectionValidationException("Only draft elections can be modified.");
        return election;
    }

    private async Task<Election> RequireElectionAsync(Guid electionId, CancellationToken ct) =>
        await store.GetAsync(electionId, ct) ?? throw new ElectionNotFoundException("Election was not found.");

    private async Task<Election> RequireInvitationMutableAsync(Guid electionId, CancellationToken ct)
    {
        var election = await RequireElectionAsync(electionId, ct);
        if (election.Status is not (ElectionStatus.Draft or ElectionStatus.Published))
            throw new ElectionValidationException("Invitations can only be managed for draft or published elections.");
        return election;
    }

    private async Task<OrganizationSnapshot> GetPoliticalPartyAsync(Guid organizationId, CancellationToken ct)
    {
        var organization = await organizations.GetAsync(organizationId, ct)
            ?? throw new ElectionValidationException("Party organization was not found.");
        if (organization.Status != "Active" || !organization.ClassificationCodes.Contains("political-party", StringComparer.OrdinalIgnoreCase))
            throw new ElectionValidationException("Organization must be active and classified as political-party.");
        return organization;
    }

    private async Task<string> ResolveCandidateDisplayNameAsync(CandidateInput input, CancellationToken ct)
    {
        if (input.PersonId.HasValue)
        {
            var person = await persons.GetAsync(input.PersonId.Value, ct)
                ?? throw new ElectionValidationException("Candidate person was not found.");
            return person.DisplayName;
        }
        var displayName = input.DisplayName?.Trim() ?? string.Empty;
        if (displayName.Length == 0)
            throw new ElectionValidationException("Display name is required when PersonId is not supplied.");
        return displayName;
    }

    private static CandidateView ToView(Candidate candidate) =>
        new(candidate.Id, candidate.DisplayName, candidate.Description, candidate.Position);

    private static AdminElectionView ToAdminView(Election election) => new(
        election.Id,
        election.Slug,
        election.Title,
        election.Description,
        election.Type,
        election.Status,
        election.EligibilityMode,
        election.VotingStartsAt,
        election.VotingEndsAt,
        election.TerritoryCode,
        election.PartyLists.OrderBy(item => item.SortOrder).Select(item => new PartyListAdminView(
            item.Id,
            item.PartyOrganizationId,
            item.PartyRegistrationNumber,
            item.PartyName,
            item.ListName,
            item.SortOrder,
            item.Candidates.OrderBy(candidate => candidate.Position).Select(candidate => new CandidateAdminView(
                candidate.Id, candidate.PersonId, candidate.DisplayName, candidate.Description, candidate.Position)).ToList())).ToList(),
        election.ReferendumOptions.OrderBy(item => item.SortOrder).Select(item => new ReferendumOptionView(
            item.Id, item.Code, item.Label, item.Description, item.SortOrder)).ToList(),
        election.EligibleVoterCount,
        election.IsHistorical,
        election.HistoricalSourceReference,
        election.ImportedAt,
        election.ImportedByPersonId);

    private static InvitationAdminView ToAdminView(VotingInvitation invitation) =>
        new(invitation.Id, invitation.Label, invitation.PersonId, invitation.CreatedAt,
            invitation.CreatedByPersonId, invitation.UsedOn, invitation.RevokedAt);

    private void EnsureAdmin()
    {
        if (!actor.IsInRole("election-service:admin"))
            throw new ElectionForbiddenException("Election administrator role is required.");
    }

    private static void ValidateElectionInput(ElectionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Title))
            throw new ElectionValidationException("Slug and title are required.");
        if (input.VotingStartsAt.Kind == DateTimeKind.Unspecified || input.VotingEndsAt.Kind == DateTimeKind.Unspecified)
            throw new ElectionValidationException("Voting dates must include a time zone.");
        if (input.VotingStartsAt >= input.VotingEndsAt)
            throw new ElectionValidationException("Voting start must be before voting end.");
        if (input.EligibleVoterCount is < 0)
            throw new ElectionValidationException("Eligible voter count cannot be negative.");
    }

    private static void ValidateHistoricalInput(HistoricalElectionInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Title) || input.Description is null)
            throw new ElectionValidationException("Slug, title, and description are required.");
        if (string.IsNullOrWhiteSpace(input.SourceReference))
            throw new ElectionValidationException("Historical source reference is required.");
        if (!Enum.IsDefined(input.Type))
            throw new ElectionValidationException("Historical election type is invalid.");
        if (input.VotingStartsAt.Kind == DateTimeKind.Unspecified || input.VotingEndsAt.Kind == DateTimeKind.Unspecified)
            throw new ElectionValidationException("Voting dates must include a time zone.");
        if (input.VotingStartsAt >= input.VotingEndsAt)
            throw new ElectionValidationException("Voting start must be before voting end.");
        if (input.EligibleVoterCount is < 0 || input.ParticipatingVoterCount < 0 || input.InvalidBallotCount < 0)
            throw new ElectionValidationException("Historical election counts cannot be negative.");
        if (input.EligibleVoterCount.HasValue && input.ParticipatingVoterCount > input.EligibleVoterCount.Value)
            throw new ElectionValidationException("Participating voter count cannot exceed eligible voter count.");

        var partyLists = input.PartyLists ?? [];
        var options = input.ReferendumOptions ?? [];
        if (input.Type == ElectionType.PartyList && (partyLists.Count == 0 || options.Count != 0))
            throw new ElectionValidationException("A historical party-list election requires party lists only.");
        if (input.Type == ElectionType.Referendum && (options.Count < 2 || partyLists.Count != 0))
            throw new ElectionValidationException("A historical referendum requires at least two referendum options only.");

        if (partyLists.Any(item => item.VoteCount < 0 || string.IsNullOrWhiteSpace(item.PartyRegistrationNumber) ||
            string.IsNullOrWhiteSpace(item.PartyName) || string.IsNullOrWhiteSpace(item.ListName)))
            throw new ElectionValidationException("Historical party snapshots and vote counts are required and cannot be negative.");
        if (partyLists.Select(item => item.SortOrder).Distinct().Count() != partyLists.Count ||
            partyLists.Where(item => item.PartyOrganizationId.HasValue).Select(item => item.PartyOrganizationId).Distinct().Count() !=
            partyLists.Count(item => item.PartyOrganizationId.HasValue))
            throw new ElectionConflictException("Historical party organization and sort order must be unique.");
        foreach (var candidates in partyLists.Select(item => item.Candidates ?? []))
        {
            if (candidates.Any(item => string.IsNullOrWhiteSpace(item.DisplayName)))
                throw new ElectionValidationException("Historical candidate display names are required.");
            if (candidates.Select(item => item.Position).Distinct().Count() != candidates.Count ||
                candidates.Where(item => item.PersonId.HasValue).Select(item => item.PersonId).Distinct().Count() !=
                candidates.Count(item => item.PersonId.HasValue))
                throw new ElectionConflictException("Historical candidate person and position must be unique within a party list.");
        }
        if (options.Any(item => item.VoteCount < 0 || string.IsNullOrWhiteSpace(item.Code) || string.IsNullOrWhiteSpace(item.Label)))
            throw new ElectionValidationException("Historical referendum option snapshots and vote counts are required and cannot be negative.");
        if (options.Select(item => item.SortOrder).Distinct().Count() != options.Count ||
            options.Select(item => item.Code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            throw new ElectionConflictException("Historical referendum option code and sort order must be unique.");

        var validBallotCount = partyLists.Sum(item => (long)item.VoteCount) + options.Sum(item => (long)item.VoteCount);
        if (validBallotCount + input.InvalidBallotCount > input.ParticipatingVoterCount)
            throw new ElectionValidationException("Valid and invalid ballot counts cannot exceed participating voter count.");
    }

    private static ElectionResult NewHistoricalResult(Election election, SelectionType selectionType,
        Guid selectionId, string selectionLabel, int voteCount, DateTime finalizedAt) => new()
        {
            Id = Guid.NewGuid(), ElectionId = election.Id, SelectionType = selectionType,
            SelectionId = selectionId, SelectionLabel = selectionLabel,
            TerritoryCode = election.TerritoryCode, VoteCount = voteCount, FinalizedAt = finalizedAt
        };

    private static void ValidateReadyToPublish(Election election)
    {
        if (election.VotingStartsAt >= election.VotingEndsAt)
            throw new ElectionValidationException("Election voting dates are invalid.");
        if (election.Type == ElectionType.PartyList && election.PartyLists.Count == 0)
            throw new ElectionValidationException("A party-list election requires at least one party list.");
        if (election.Type == ElectionType.Referendum && election.ReferendumOptions.Count < 2)
            throw new ElectionValidationException("A referendum requires at least two options.");
    }

    private static string NormalizeSlug(string value) => value.Trim().ToLowerInvariant();
    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}