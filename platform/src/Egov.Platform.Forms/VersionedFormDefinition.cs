namespace Egov.Platform.Forms;

public sealed record VersionedFormDefinition(
    string Name,
    int Version,
    string DefinitionJson,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record FormDefinitionDraft(
    string Name,
    string DefinitionJson,
    DateTimeOffset UpdatedAt);