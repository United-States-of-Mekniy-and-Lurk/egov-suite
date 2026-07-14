using CitizenService.Domain.Enums;

namespace CitizenService.Domain.StateMachine;

public static class ApplicationStateMachine
{
    private static readonly Dictionary<ApplicationStatus, IEnumerable<ApplicationStatus>> _validTransitions = new()
    {
        [ApplicationStatus.Draft] = new[] { ApplicationStatus.Submitted, ApplicationStatus.Withdrawn },
        [ApplicationStatus.Submitted] = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Draft, ApplicationStatus.Withdrawn },
        [ApplicationStatus.UnderReview] = new[] { ApplicationStatus.Approved, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn },
        [ApplicationStatus.Approved] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Rejected] = Array.Empty<ApplicationStatus>(),
        [ApplicationStatus.Withdrawn] = Array.Empty<ApplicationStatus>(),
    };

    public static bool IsValidTransition(ApplicationStatus from, ApplicationStatus to)
    {
        if (!_validTransitions.TryGetValue(from, out var validTargets))
            return false;
        return validTargets.Contains(to);
    }

    public static IEnumerable<ApplicationStatus> GetValidNextStates(ApplicationStatus current)
    {
        if (_validTransitions.TryGetValue(current, out var validTargets))
            return validTargets;
        return Array.Empty<ApplicationStatus>();
    }
}
