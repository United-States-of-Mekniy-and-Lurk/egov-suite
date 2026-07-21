namespace Egov.Platform.Documents;

public interface IOfficialDocumentRenderer
{
    GeneratedDocument Render(OfficialDocument document);
}