using OrganizationRegistry.Domain.Enums;

namespace OrganizationRegistry.Domain.StateMachine;

public static class RegistrationStateMachine
{
    private static readonly IReadOnlyDictionary<RegistrationApplicationStatus, IReadOnlySet<RegistrationApplicationStatus>> ValidTransitions =
        new Dictionary<RegistrationApplicationStatus, IReadOnlySet<RegistrationApplicationStatus>>
        {
            [RegistrationApplicationStatus.Draft] = Set(RegistrationApplicationStatus.Submitted, RegistrationApplicationStatus.Withdrawn),
            [RegistrationApplicationStatus.Submitted] = Set(RegistrationApplicationStatus.UnderReview, RegistrationApplicationStatus.Withdrawn),
            [RegistrationApplicationStatus.UnderReview] = Set(RegistrationApplicationStatus.MoreInformationRequired, RegistrationApplicationStatus.Approved, RegistrationApplicationStatus.Rejected),
            [RegistrationApplicationStatus.MoreInformationRequired] = Set(RegistrationApplicationStatus.Submitted, RegistrationApplicationStatus.Withdrawn),
            [RegistrationApplicationStatus.Approved] = Set(),
            [RegistrationApplicationStatus.Rejected] = Set(),
            [RegistrationApplicationStatus.Withdrawn] = Set()
        };

    public static bool IsValidTransition(RegistrationApplicationStatus from, RegistrationApplicationStatus to) =>
        ValidTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlySet<RegistrationApplicationStatus> GetValidNextStates(RegistrationApplicationStatus status) =>
        ValidTransitions.TryGetValue(status, out var targets) ? targets : Set();

    private static IReadOnlySet<RegistrationApplicationStatus> Set(params RegistrationApplicationStatus[] statuses) =>
        new HashSet<RegistrationApplicationStatus>(statuses);
}