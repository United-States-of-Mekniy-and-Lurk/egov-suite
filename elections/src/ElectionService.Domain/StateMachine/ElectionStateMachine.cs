using ElectionService.Domain.Enums;

namespace ElectionService.Domain.StateMachine;

public static class ElectionStateMachine
{
    private static readonly IReadOnlyDictionary<ElectionStatus, IReadOnlySet<ElectionStatus>> ValidTransitions =
        new Dictionary<ElectionStatus, IReadOnlySet<ElectionStatus>>
        {
            [ElectionStatus.Draft] = Set(ElectionStatus.Published),
            [ElectionStatus.Published] = Set(ElectionStatus.Closed),
            [ElectionStatus.Closed] = Set(ElectionStatus.Finalized),
            [ElectionStatus.Finalized] = Set(ElectionStatus.Certified, ElectionStatus.Archived),
            [ElectionStatus.Certified] = Set(ElectionStatus.Archived),
            [ElectionStatus.Archived] = Set()
        };

    public static bool IsValidTransition(ElectionStatus from, ElectionStatus to) =>
        ValidTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    private static IReadOnlySet<ElectionStatus> Set(params ElectionStatus[] statuses) =>
        new HashSet<ElectionStatus>(statuses);
}