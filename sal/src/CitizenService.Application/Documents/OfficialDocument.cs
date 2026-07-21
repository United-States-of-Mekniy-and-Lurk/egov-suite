namespace CitizenService.Application.Documents;

public sealed record OfficialDocument(
    string DocumentNumber,
    string Title,
    string Subtitle,
    DateTimeOffset IssuedAt,
    IReadOnlyList<OfficialDocumentField> Fields,
    IReadOnlyList<OfficialDocumentAuditEntry> AuditTrail,
    OfficialDocumentSignature Signature);

public sealed record OfficialDocumentField(string Label, string Value);

public sealed record OfficialDocumentAuditEntry(
    DateTimeOffset OccurredAt,
    string ActorName,
    string Action,
    string? Note);

public sealed record OfficialDocumentSignature(string Name, string Role);

public sealed record GeneratedDocument(byte[] Content, string ContentType, string FileName);