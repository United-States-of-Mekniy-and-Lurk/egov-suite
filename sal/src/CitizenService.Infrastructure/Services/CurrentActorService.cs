using CitizenService.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace CitizenService.Infrastructure.Services;

public class CurrentActorService : ICurrentActor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentActorService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid PersonId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("person_id")?.Value;
            if (claim != null && Guid.TryParse(claim, out var personId))
                return personId;
            return Guid.Empty;
        }
    }
}
