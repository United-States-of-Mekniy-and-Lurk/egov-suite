using CitizenService.Domain.Enums;

namespace CitizenService.Domain.Events;

public record CitizenCreated(Guid ActorPersonId, Guid SubjectPersonId, Guid CitizenId, DateTime Timestamp);
public record CitizenStatusChanged(Guid ActorPersonId, Guid SubjectPersonId, Guid CitizenId, CitizenStatus PreviousStatus, CitizenStatus NewStatus, DateTime Timestamp);
public record CitizenshipApplicationCreated(Guid ActorPersonId, Guid SubjectPersonId, Guid ApplicationId, DateTime Timestamp);
public record CitizenshipApplicationSubmitted(Guid ActorPersonId, Guid SubjectPersonId, Guid ApplicationId, DateTime Timestamp);
public record CitizenshipApplicationApproved(Guid ActorPersonId, Guid SubjectPersonId, Guid ApplicationId, DateTime Timestamp);
public record CitizenshipApplicationRejected(Guid ActorPersonId, Guid SubjectPersonId, Guid ApplicationId, string Reason, DateTime Timestamp);
