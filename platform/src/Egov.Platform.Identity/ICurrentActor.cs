namespace Egov.Platform.Identity;

public interface ICurrentActor
{
    Guid PersonId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}