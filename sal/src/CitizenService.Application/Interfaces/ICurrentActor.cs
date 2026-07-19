namespace CitizenService.Application.Interfaces;

public interface ICurrentActor
{
    Guid PersonId { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);
}
