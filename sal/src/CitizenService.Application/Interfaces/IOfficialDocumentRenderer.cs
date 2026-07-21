using CitizenService.Application.Documents;

namespace CitizenService.Application.Interfaces;

public interface IOfficialDocumentRenderer
{
    GeneratedDocument Render(OfficialDocument document);
}