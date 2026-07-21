namespace Egov.Platform.Documents;

public sealed class OfficialDocumentPdfOptions
{
    public string Issuer { get; init; } = "United States of Mekniy and Lurk";
    public string Masthead { get; init; } = "MKLU";
    public string MastheadSubtitle { get; init; } = "UNITED STATES OF MEKNIY AND LURK";
    public string DocumentKind { get; init; } = "OFFICIAL DECISION";
    public string IssueLocation { get; init; } = "Yeoju, Mekniy";
    public string AuditTrailTitle { get; init; } = "Application history";
    public string FileNamePrefix { get; init; } = "citizenship-decision";
}